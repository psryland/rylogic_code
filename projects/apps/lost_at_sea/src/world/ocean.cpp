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

	// Create the ocean mesh as a flat grid, with vertex positions and normals to be displaced by the Gerstner wave formula in the shader.
	void Ocean::BuildMesh(Renderer& rdr)
	{
		auto vcount = GridDim * GridDim;
		auto icount = (GridDim - 1) * (GridDim - 1) * 6;
		m_cpu_data.Reset(vcount, 0, 1, sizeof(uint16_t));

		// Initialise vertex data with a flat grid and ocean colour
		auto cell_size = 2.0f * GridExtent / (GridDim - 1);
		for (int iy = 0; iy < GridDim; ++iy)
		{
			for (int ix = 0; ix < GridDim; ++ix)
			{
				auto idx = iy * GridDim + ix;
				auto& v = m_cpu_data.m_vcont[idx];
				v.m_vert = v4((ix - GridDim / 2) * cell_size, (iy - GridDim / 2) * cell_size, 0, 1);
				v.m_diff = Colour(0.06f, 0.25f, 0.50f, 1.0f); // Ocean blue
				v.m_norm = v4(0, 0, 1, 0);
				v.m_tex0 = v2(float(ix) / (GridDim - 1), float(iy) / (GridDim - 1));
				v.m_idx0 = iv2::Zero();
			}
		}

		// Build index buffer
		for (int iy = 0; iy != GridDim - 1; ++iy)
		{
			for (int ix = 0; ix != GridDim - 1; ++ix)
			{
				auto i0 = static_cast<uint16_t>(iy * GridDim + ix);
				auto i1 = static_cast<uint16_t>(i0 + 1);
				auto i2 = static_cast<uint16_t>(i0 + GridDim);
				auto i3 = static_cast<uint16_t>(i2 + 1);
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

		// Configure the nugget (created by Reset with default values)
		auto& nugget = m_cpu_data.m_ncont[0];
		nugget.m_topo = ETopo::TriList;
		nugget.m_geom = EGeom::Vert | EGeom::Colr | EGeom::Norm;
		nugget.m_vrange = rdr12::Range(0, vcount);
		nugget.m_irange = rdr12::Range(0, icount);

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
		// Vertices are computed in local grid space (centred on the grid origin).
		// The instance transform (m_i2w) handles the camera-relative offset.
		auto cell_size = 2.0f * GridExtent / (GridDim - 1);

		// The grid origin in world space (snapped to camera XY, ocean surface at Z=0)
		auto grid_origin = v4(camera_world_pos.x, camera_world_pos.y, 0, 1);

		for (int iy = 0; iy != GridDim; ++iy)
		{
			for (int ix = 0; ix != GridDim; ++ix)
			{
				auto idx = iy * GridDim + ix;

				// Local grid position (relative to grid centre)
				auto lx = (ix - GridDim / 2) * cell_size;
				auto ly = (iy - GridDim / 2) * cell_size;

				// World position for wave phase computation
				auto wx = grid_origin.x + lx;
				auto wy = grid_origin.y + ly;

				auto displaced = DisplacedPosition(wx, wy, time);
				auto normal = NormalAt(wx, wy, time);

				// Store in local grid space (displacement relative to grid vertex position)
				auto& v = m_cpu_data.m_vcont[idx];
				v.m_vert = v4(lx + (displaced.x - wx), ly + (displaced.y - wy), displaced.z, 1.0f);
				v.m_norm = normal;
			}
		}

		// The instance transform places the grid at the camera position (camera-relative rendering)
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
