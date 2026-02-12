//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
#include "src/forward.h"
#include "src/world/terrain.h"

namespace las
{
	Terrain::Terrain(Renderer& rdr, HeightField const& hf)
		:m_height_field(&hf)
		,m_inst()
		,m_factory(rdr)
		,m_cpu_data()
		,m_dirty(false)
	{
		BuildMesh();
	}

	void Terrain::Update(v4 camera_world_pos)
	{
		auto cell_size = 2.0f * GridExtent / (GridDim - 1);

		for (int iy = 0; iy < GridDim; ++iy)
		{
			for (int ix = 0; ix < GridDim; ++ix)
			{
				auto idx = iy * GridDim + ix;
				auto wx = camera_world_pos.x + (ix - GridDim / 2) * cell_size;
				auto wy = camera_world_pos.y + (iy - GridDim / 2) * cell_size;
				auto h = m_height_field->HeightAt(wx, wy);
				auto normal = m_height_field->NormalAt(wx, wy);

				auto& v = m_cpu_data.m_vcont[idx];
				v.m_vert = v4(wx - camera_world_pos.x, wy - camera_world_pos.y, h, 1.0f);
				v.m_norm = normal;
				v.m_diff = TerrainColour(h, normal.z);
			}
		}
		m_dirty = true;
	}

	void Terrain::AddToScene(Scene& scene, GfxCmdList& cmd_list, GpuUploadBuffer& upload)
	{
		if (!m_inst.m_model)
			return;

		if (m_dirty)
		{
			auto update = m_inst.m_model->UpdateVertices(cmd_list, upload);
			auto* dst = update.ptr<Vert>();
			std::memcpy(dst, m_cpu_data.m_vcont.data(), m_cpu_data.m_vcont.size() * sizeof(Vert));
			update.Commit();
			m_dirty = false;
		}

		m_inst.m_i2w = m4x4::Identity();
		scene.AddInstance(m_inst);
	}

	Colour Terrain::TerrainColour(float height, float flatness)
	{
		if (height < 0.0f)
			return Colour(0.13f, 0.25f, 0.50f, 1.0f); // Underwater: dark blue-grey

		if (height < 2.0f)
			return Colour(0.82f, 0.75f, 0.37f, 1.0f); // Beach: sandy yellow

		if (flatness < 0.7f)
			return Colour(0.38f, 0.38f, 0.38f, 1.0f); // Steep slope: rocky grey

		if (height > 40.0f)
			return Colour(0.50f, 0.50f, 0.50f, 1.0f); // High altitude: grey rock

		// Green vegetation, getting browner at higher elevations
		auto t = std::clamp((height - 2.0f) / 38.0f, 0.0f, 1.0f);
		return Colour(0.23f + t * 0.15f, 0.50f - t * 0.20f, 0.12f + t * 0.10f, 1.0f);
	}

	void Terrain::BuildMesh()
	{
		auto vcount = GridDim * GridDim;
		auto icount = (GridDim - 1) * (GridDim - 1) * 6;
		m_cpu_data.Reset(vcount, 0, 1, sizeof(uint16_t));

		// Initialise with a flat grid
		auto cell_size = 2.0f * GridExtent / (GridDim - 1);
		for (int iy = 0; iy < GridDim; ++iy)
		{
			for (int ix = 0; ix < GridDim; ++ix)
			{
				auto idx = iy * GridDim + ix;
				auto& v = m_cpu_data.m_vcont[idx];
				v.m_vert = v4((ix - GridDim / 2) * cell_size, (iy - GridDim / 2) * cell_size, 0, 1);
				v.m_diff = Colour(0.23f, 0.50f, 0.12f, 1.0f); // Default green
				v.m_norm = v4(0, 0, 1, 0);
				v.m_tex0 = v2(float(ix) / (GridDim - 1), float(iy) / (GridDim - 1));
				v.m_idx0 = iv2::Zero();
			}
		}

		// Build index buffer
		for (int iy = 0; iy < GridDim - 1; ++iy)
		{
			for (int ix = 0; ix < GridDim - 1; ++ix)
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

		auto terrain_colour = Colour32Green;
		auto opts = ModelGenerator::CreateOptions().colours({ &terrain_colour, 1 });

		ModelGenerator::Cache cache{m_cpu_data};
		m_inst.m_model = ModelGenerator::Create<Vert>(m_factory, cache, &opts);
		m_inst.m_i2w = m4x4::Identity();
	}
}
