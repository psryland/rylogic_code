//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
#include "src/forward.h"
#include "src/world/terrain/terrain.h"
#include "src/world/terrain/shaders/terrain_shader.h"

namespace las
{
	// Radial mesh parameters — same structure as the ocean mesh
	static constexpr int NumRings = 80;
	static constexpr int NumSegments = 128;
	static constexpr float InnerRadius = 2.0f;
	static constexpr float OuterRadius = 1000.0f;

	// Terrain
	Terrain::Terrain(Renderer& rdr)
		: m_inst()
		, m_shader()
	{
		// Build a flat radial mesh with encoded vertex data for the GPU.
		// The vertex shader reconstructs world positions from ring/segment encoding
		// and displaces them vertically using Perlin noise.
		// Vertex layout (same as ocean):
		//   Centre vertex: m_vert = (0, 0, -1, 1) — sentinel value z=-1
		//   Ring vertices:  m_vert = (cos θ, sin θ, t, 1) where t = normalised ring index [0,1]
		auto vcount = 1 + NumRings * NumSegments;

		rdr12::ModelGenerator::Buffers<Vert> buf;
		buf.Reset(vcount, 0, 0, sizeof(uint16_t));

		// Centre vertex — sentinel z = -1
		{
			auto& v = buf.m_vcont[0];
			v.m_vert = v4(0, 0, -1, 1);
			v.m_diff = Colour(0.23f, 0.50f, 0.12f, 1.0f);
			v.m_norm = v4(0, 0, 1, 0);
			v.m_tex0 = v2(0.5f, 0.5f);
			v.m_idx0 = iv2::Zero();
		}

		// Ring vertices — encode direction and normalised ring index
		for (int ring = 0; ring != NumRings; ++ring)
		{
			auto t = static_cast<float>(ring) / (NumRings - 1);

			for (int seg = 0; seg != NumSegments; ++seg)
			{
				auto angle = maths::tauf * seg / NumSegments;
				auto idx = 1 + ring * NumSegments + seg;
				auto& v = buf.m_vcont[idx];
				v.m_vert = v4(std::cos(angle), std::sin(angle), t, 1);
				v.m_diff = Colour(0.23f, 0.50f, 0.12f, 1.0f);
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
			buf.m_icont.push_back(0);  // centre
			buf.m_icont.push_back(s0);
			buf.m_icont.push_back(s1);
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
				buf.m_icont.push_back(i0);
				buf.m_icont.push_back(i2);
				buf.m_icont.push_back(i1);
				buf.m_icont.push_back(i1);
				buf.m_icont.push_back(i2);
				buf.m_icont.push_back(i3);
			}
		}

		// Set a large bounding box since the VS will displace vertices far from their encoded positions.
		buf.m_bbox = BBox(v4::Origin(), v4(OuterRadius, OuterRadius, 100, 0));

		// Create the terrain shader
		auto shdr = Shader::Create<TerrainShader>(rdr);
		m_shader = shdr.get();

		// Configure the nugget with the custom terrain shader
		buf.m_ncont.push_back(NuggetDesc{ ETopo::TriList, EGeom::Vert | EGeom::Colr | EGeom::Norm }
			.use_shader_overlay(ERenderStep::RenderForward, shdr));

		auto terrain_colour = Colour32Green;
		auto opts = ModelGenerator::CreateOptions().colours({ &terrain_colour, 1 });

		ResourceFactory factory(rdr);
		ModelGenerator::Cache cache{buf};
		m_inst.m_model = ModelGenerator::Create<Vert>(factory, cache, &opts);
		m_inst.m_i2w = m4x4::Identity();

		factory.FlushToGpu(EGpuFlush::Block);
	}

	// Prepare shader constant buffers for rendering (thread-safe, no scene interaction).
	void Terrain::PrepareRender(v4 camera_world_pos)
	{
		if (!m_inst.m_model)
			return;

		m_shader->SetupFrame(camera_world_pos, InnerRadius, OuterRadius, NumRings, NumSegments);
	}

	// Add instance to the scene drawlist (NOT thread-safe, must be called serially).
	void Terrain::AddToScene(Scene& scene)
	{
		if (!m_inst.m_model)
			return;

		scene.AddInstance(m_inst);
	}
}
