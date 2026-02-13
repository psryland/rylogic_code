//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
#include "src/forward.h"
#include "src/world/ocean.h"

namespace las
{
	// GerstnerWave
	float GerstnerWave::Frequency() const { return maths::tauf / m_wavelength; }
	float GerstnerWave::WaveNumber() const { return maths::tauf / m_wavelength; }

	// Ocean
	Ocean::Ocean(Renderer& rdr)
		:m_waves()
		,m_inst()
		,m_cpu_data()
		,m_grid_origin(v4::Origin())
		,m_dirty(false)
	{
		InitDefaultWaves();
		BuildMesh(rdr);
	}

	// Initialise the ocean with a set of default wave components. These are arbitrary values that look good, but could be tweaked or made user-configurable.
	void Ocean::InitDefaultWaves()
	{
		m_waves = {
			{ Normalise(v4(1.0f, 0.3f, 0, 0)), 1.2f, 60.0f, 8.0f, 0.5f },  // Primary swell
			{ Normalise(v4(0.8f, -0.6f, 0, 0)), 0.6f, 30.0f, 5.5f, 0.4f },  // Secondary
			{ Normalise(v4(-0.3f, 1.0f, 0, 0)), 0.3f, 15.0f, 3.8f, 0.3f },  // Cross chop
			{ Normalise(v4(0.5f, 0.5f, 0, 0)), 0.15f, 8.0f, 2.8f, 0.2f },   // Small ripple
		};
	}

	// Compute the radius of a ring, blending from logarithmic spacing (camera near surface)
	// to linear spacing (camera far above). At low height, logarithmic spacing makes nearby
	// triangles small and distant triangles large, matching perspective foreshortening. As the
	// camera rises, perspective flattens and linear spacing produces more uniform screen-space
	// triangle sizes.
	static float RingRadius(int ring, float camera_height)
	{
		auto t = static_cast<float>(ring) / (Ocean::NumRings - 1);

		// Logarithmic: r = Inner * exp(log(Outer/Inner) * t)
		auto log_ratio = std::log(Ocean::OuterRadius / Ocean::InnerRadius);
		auto r_log = Ocean::InnerRadius * std::exp(log_ratio * t);

		// Linear: r = Inner + (Outer - Inner) * t
		auto r_lin = Ocean::InnerRadius + (Ocean::OuterRadius - Ocean::InnerRadius) * t;

		// Blend: 0 = fully logarithmic (near surface), 1 = fully linear (high altitude)
		// Transition height is roughly the outer radius — at that height, the whole mesh
		// subtends a similar angle regardless of ring distance.
		auto h = std::abs(camera_height);
		auto blend = std::min(h / Ocean::OuterRadius, 1.0f);

		return r_log + blend * (r_lin - r_log);
	}

	// Build a radial mesh with logarithmically spaced rings. Vertex density decreases
	// with distance so triangles appear approximately the same size on screen.
	void Ocean::BuildMesh(Renderer& rdr)
	{
		// Vertex layout: centre vertex (index 0) + NumRings * NumSegments ring vertices
		auto vcount = 1 + NumRings * NumSegments;
		m_cpu_data.Reset(vcount, 0, 1, sizeof(uint16_t));

		// Centre vertex
		{
			auto& v = m_cpu_data.m_vcont[0];
			v.m_vert = v4(0, 0, 0, 1);
			v.m_diff = Colour(0.06f, 0.25f, 0.50f, 1.0f);
			v.m_norm = v4(0, 0, 1, 0);
			v.m_tex0 = v2(0.5f, 0.5f);
			v.m_idx0 = iv2::Zero();
		}

		// Ring vertices
		for (int ring = 0; ring != NumRings; ++ring)
		{
			auto t = static_cast<float>(ring) / (NumRings - 1);
			auto r = RingRadius(ring, 0.0f);

			for (int seg = 0; seg != NumSegments; ++seg)
			{
				auto angle = maths::tauf * seg / NumSegments;
				auto idx = 1 + ring * NumSegments + seg;
				auto& v = m_cpu_data.m_vcont[idx];
				v.m_vert = v4(r * std::cos(angle), r * std::sin(angle), 0, 1);
				v.m_diff = Colour(0.06f, 0.25f, 0.50f, 1.0f);
				v.m_norm = v4(0, 0, 1, 0);
				v.m_tex0 = v2(0.5f + 0.5f * t * std::cos(angle), 0.5f + 0.5f * t * std::sin(angle));
				v.m_idx0 = iv2::Zero();
			}
		}

		// Index buffer: triangle fan from centre to first ring
		for (int seg = 0; seg != NumSegments; ++seg)
		{
			auto s0 = static_cast<uint16_t>(1 + seg);
			auto s1 = static_cast<uint16_t>(1 + (seg + 1) % NumSegments);
			m_cpu_data.m_icont.push_back(0);  // centre
			m_cpu_data.m_icont.push_back(s0);
			m_cpu_data.m_icont.push_back(s1);
		}

		// Quad strips between consecutive rings
		for (int ring = 0; ring != NumRings - 1; ++ring)
		{
			for (int seg = 0; seg != NumSegments; ++seg)
			{
				auto next_seg = (seg + 1) % NumSegments;
				auto i0 = static_cast<uint16_t>(1 + ring * NumSegments + seg);
				auto i1 = static_cast<uint16_t>(1 + ring * NumSegments + next_seg);
				auto i2 = static_cast<uint16_t>(1 + (ring + 1) * NumSegments + seg);
				auto i3 = static_cast<uint16_t>(1 + (ring + 1) * NumSegments + next_seg);
				m_cpu_data.m_icont.push_back(i0);
				m_cpu_data.m_icont.push_back(i2);
				m_cpu_data.m_icont.push_back(i1);
				m_cpu_data.m_icont.push_back(i1);
				m_cpu_data.m_icont.push_back(i2);
				m_cpu_data.m_icont.push_back(i3);
			}
		}

		// Compute bounding box from vertices
		m_cpu_data.m_bbox = BBox::Reset();
		for (auto const& v : m_cpu_data.m_vcont)
			Grow(m_cpu_data.m_bbox, v.m_vert);

		// Configure the nugget
		auto& nugget = m_cpu_data.m_ncont[0];
		nugget.m_topo = ETopo::TriList;
		nugget.m_geom = EGeom::Vert | EGeom::Colr | EGeom::Norm;
		nugget.m_vrange = rdr12::Range(0, vcount);
		nugget.m_irange = rdr12::Range(0, static_cast<int>(m_cpu_data.m_icont.size()));

		auto ocean_colour = Colour32(0xFF804010);
		auto opts = ModelGenerator::CreateOptions().colours({ &ocean_colour, 1 });

		ResourceFactory factory(rdr);
		ModelGenerator::Cache cache{m_cpu_data};
		m_inst.m_model = ModelGenerator::Create<Vert>(factory, cache, &opts);
		m_inst.m_i2w = m4x4::Identity();

		// Wireframe for debugging wave geometry
		m_inst.m_model->m_nuggets.front().FillMode(EFillMode::Wireframe);

		factory.FlushToGpu(EGpuFlush::Block);
	}

	// Query the height of the ocean surface at a world position and time, without computing the full displacement or normal.
	float Ocean::HeightAt(float world_x, float world_y, float time) const
	{
		auto h = 0.0f;
		for (auto& w : m_waves)
		{
			auto k = w.WaveNumber();
			auto phase = k * (w.m_direction.x * world_x + w.m_direction.y * world_y) - w.Frequency() * time;
			h += w.m_amplitude * std::sin(phase);
		}
		return h;
	}

	// Query the displaced position of the ocean surface at a world position and time, including horizontal displacement from the Gerstner wave formula.
	v4 Ocean::DisplacedPosition(float world_x, float world_y, float time) const
	{
		auto dx = 0.0f, dy = 0.0f, dz = 0.0f;
		for (auto& w : m_waves)
		{
			auto k = w.WaveNumber();
			auto phase = k * (w.m_direction.x * world_x + w.m_direction.y * world_y) - w.Frequency() * time;
			auto c = std::cos(phase);
			auto s = std::sin(phase);
			dx -= w.m_steepness * w.m_amplitude * w.m_direction.x * c;
			dy -= w.m_steepness * w.m_amplitude * w.m_direction.y * c;
			dz += w.m_amplitude * s;
		}
		return v4(world_x + dx, world_y + dy, dz, 1.0f);
	}

	// Query the normal of the ocean surface at a world position and time, including contributions from all wave components.
	v4 Ocean::NormalAt(float world_x, float world_y, float time) const
	{
		auto nx = 0.0f, ny = 0.0f, nz = 1.0f;
		for (auto& w : m_waves)
		{
			auto k = w.WaveNumber();
			auto phase = k * (w.m_direction.x * world_x + w.m_direction.y * world_y) - w.Frequency() * time;
			auto c = std::cos(phase);
			auto s = std::sin(phase);
			nx -= w.m_direction.x * k * w.m_amplitude * c;
			ny -= w.m_direction.y * k * w.m_amplitude * c;
			nz -= w.m_steepness * k * w.m_amplitude * s;
		}
		return Normalise(v4(nx, ny, nz, 0.0f));
	}

	// Simulation step: recompute CPU vertex positions for the current time and camera position.
	void Ocean::Update(float time, v4 camera_world_pos)
	{
		// The mesh is centred on the camera (grid_origin = camera XY, Z=0).
		// Vertices are in local space; the instance transform handles camera-relative offset.
		auto grid_origin = v4(camera_world_pos.x, camera_world_pos.y, 0, 1);

		// Centre vertex
		{
			auto displaced = DisplacedPosition(grid_origin.x, grid_origin.y, time);
			auto normal = NormalAt(grid_origin.x, grid_origin.y, time);
			auto& v = m_cpu_data.m_vcont[0];
			v.m_vert = v4(displaced.x - grid_origin.x, displaced.y - grid_origin.y, displaced.z, 1.0f);
			v.m_norm = normal;
		}

		// Ring vertices
		auto cam_height = camera_world_pos.z;
		for (int ring = 0; ring != NumRings; ++ring)
		{
			auto r = RingRadius(ring, cam_height);

			for (int seg = 0; seg != NumSegments; ++seg)
			{
				auto angle = maths::tauf * seg / NumSegments;
				auto lx = r * std::cos(angle);
				auto ly = r * std::sin(angle);

				auto wx = grid_origin.x + lx;
				auto wy = grid_origin.y + ly;

				auto displaced = DisplacedPosition(wx, wy, time);
				auto normal = NormalAt(wx, wy, time);

				auto idx = 1 + ring * NumSegments + seg;
				auto& v = m_cpu_data.m_vcont[idx];
				v.m_vert = v4(lx + (displaced.x - wx), ly + (displaced.y - wy), displaced.z, 1.0f);
				v.m_norm = normal;
			}
		}

		m_grid_origin = grid_origin;
		m_dirty = true;
	}

	// Rendering: upload dirty verts to GPU and add to the scene.
	void Ocean::AddToScene(Scene& scene, v4 camera_world_pos, GfxCmdList& cmd_list, GpuUploadBuffer& upload)
	{
		if (!m_inst.m_model)
			return;

		// Upload CPU vertex data to the GPU model if dirty
		if (m_dirty)
		{
			auto update = m_inst.m_model->UpdateVertices(cmd_list, upload);
			auto* dst = update.ptr<Vert>();
			std::memcpy(dst, m_cpu_data.m_vcont.data(), m_cpu_data.m_vcont.size() * sizeof(Vert));
			update.Commit();

			m_dirty = false;
		}

		// Camera-relative instance transform: grid origin minus camera position
		m_inst.m_i2w = m4x4::Translation(m_grid_origin - camera_world_pos);
		scene.AddInstance(m_inst);
	}
}
