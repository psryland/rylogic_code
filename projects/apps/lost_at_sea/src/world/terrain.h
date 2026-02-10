//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Terrain mesh rendering for visible land (height > 0).
// Generates a grid of terrain chunks around the camera.
#pragma once
#include "src/forward.h"
#include "src/world/height_field.h"

namespace las
{
	struct Terrain
	{
		// Terrain grid parameters
		static constexpr int GridDim = 128;         // Vertices per side
		static constexpr float GridExtent = 500.0f; // Half-extent in metres (same as ocean)

		struct Instance
		{
			#define PR_RDR_INST(x)\
			x(m4x4     , m_i2w  , EInstComp::I2WTransform)\
			x(ModelPtr , m_model, EInstComp::ModelPtr)
			PR_RDR12_INSTANCE_MEMBERS(Instance, PR_RDR_INST);
			#undef PR_RDR_INST
		};

		HeightField const* m_height_field;
		Instance m_inst;
		ResourceFactory m_factory;

		// Persistent buffers
		std::vector<v4> m_verts;
		std::vector<v4> m_norms;
		std::vector<Colour32> m_colours;
		std::vector<uint16_t> m_indices;

		explicit Terrain(Renderer& rdr, HeightField const& hf)
			:m_height_field(&hf)
			,m_inst()
			,m_factory(rdr)
			,m_verts(GridDim * GridDim)
			,m_norms(GridDim * GridDim)
			,m_colours(GridDim * GridDim)
			,m_indices()
		{
			BuildIndexBuffer();
		}

		// Rebuild the terrain mesh around the camera position
		void Update(v4 camera_world_pos)
		{
			auto cell_size = 2.0f * GridExtent / (GridDim - 1);
			auto any_land = false;

			for (int iy = 0; iy < GridDim; ++iy)
			{
				for (int ix = 0; ix < GridDim; ++ix)
				{
					auto idx = iy * GridDim + ix;

					// World space position of this vertex
					auto wx = camera_world_pos.x + (ix - GridDim / 2) * cell_size;
					auto wy = camera_world_pos.y + (iy - GridDim / 2) * cell_size;
					auto h = m_height_field->HeightAt(wx, wy);

					// Only render land (height > 0). Underwater terrain is clamped below ocean surface.
					auto render_h = h;
					auto is_land = h > 0.0f;
					if (is_land) any_land = true;

					// Render-space position (camera at origin)
					m_verts[idx] = v4(wx - camera_world_pos.x, wy - camera_world_pos.y, render_h, 1.0f);

					// Normal
					m_norms[idx] = m_height_field->NormalAt(wx, wy);

					// Colour based on height and slope
					m_colours[idx] = TerrainColour(h, m_norms[idx].z);
				}
			}

			// Only create the model if there's land to render
			if (!any_land)
			{
				m_inst.m_model = nullptr;
				return;
			}

			auto vcount = isize(m_verts);
			NuggetDesc nugget(ETopo::TriList, EGeom::Vert | EGeom::Colr | EGeom::Norm);
			nugget.m_vrange = rdr12::Range(0, vcount);
			nugget.m_irange = rdr12::Range(0, isize(m_indices));
			auto nug_span = std::span<NuggetDesc const>(&nugget, 1);

			MeshCreationData cdata;
			cdata.verts(m_verts).indices(std::span<uint16_t const>(m_indices)).colours(m_colours).normals(m_norms).nuggets(nug_span);
			m_inst.m_model = ModelGenerator::Mesh(m_factory, cdata);
			m_inst.m_i2w = m4x4::Identity();
		}

		// Add the terrain to the scene
		void AddToScene(Scene& scene)
		{
			if (m_inst.m_model)
				scene.AddInstance(m_inst);
		}

	private:

		// Determine terrain colour from height and slope (normal.z = how flat)
		static Colour32 TerrainColour(float height, float flatness)
		{
			if (height < 0.0f)
				return Colour32(0xFF804020); // Underwater: dark sandy (ABGR)

			if (height < 2.0f)
				return Colour32(0xFF60C0D0); // Beach: sandy yellow (ABGR)

			if (flatness < 0.7f)
				return Colour32(0xFF606060); // Steep slope: rocky grey

			if (height > 40.0f)
				return Colour32(0xFF808080); // High altitude: grey rock

			// Default: green vegetation
			auto t = std::clamp((height - 2.0f) / 38.0f, 0.0f, 1.0f);
			auto g = static_cast<uint8_t>(180 - static_cast<int>(t * 80));
			auto r = static_cast<uint8_t>(60 + static_cast<int>(t * 40));
			return Colour32(0xFF000000 | (g << 8) | r); // ABGR: dark green to brownish
		}

		void BuildIndexBuffer()
		{
			m_indices.reserve((GridDim - 1) * (GridDim - 1) * 6);
			for (int iy = 0; iy < GridDim - 1; ++iy)
			{
				for (int ix = 0; ix < GridDim - 1; ++ix)
				{
					auto i0 = static_cast<uint16_t>(iy * GridDim + ix);
					auto i1 = static_cast<uint16_t>(i0 + 1);
					auto i2 = static_cast<uint16_t>(i0 + GridDim);
					auto i3 = static_cast<uint16_t>(i2 + 1);
					m_indices.push_back(i0); m_indices.push_back(i2); m_indices.push_back(i1);
					m_indices.push_back(i1); m_indices.push_back(i2); m_indices.push_back(i3);
				}
			}
		}
	};
}
