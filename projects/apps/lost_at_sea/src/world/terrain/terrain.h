//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// CDLOD terrain rendering using GPU-side Perlin noise displacement.
// World-axis-aligned grid patches are instanced at multiple LOD levels
// with geomorphing in the vertex shader to eliminate vertex swimming
// and LOD popping.
#pragma once
#include "src/forward.h"
#include "src/world/terrain/cdlod.h"

namespace las
{
	struct TerrainShader;

	// CDLOD terrain rendering
	struct Terrain
	{
		struct PatchInstance
		{
			#define PR_RDR_INST(x)\
			x(m4x4     , m_i2w  , EInstComp::I2WTransform)\
			x(ModelPtr , m_model, EInstComp::ModelPtr)
			PR_RDR12_INSTANCE_MEMBERS(PatchInstance, PR_RDR_INST);
			#undef PR_RDR_INST
		};

		ModelPtr m_grid_mesh;                      // Shared NxN grid mesh for all patches
		TerrainShader* m_shader;                   // Owned by 'm_grid_mesh'
		CDLODSelection m_lod_selection;            // Quadtree LOD selection
		std::vector<PatchInstance> m_instances;     // Pre-allocated instance pool

		explicit Terrain(Renderer& rdr);

		// Number of visible patches this frame
		int PatchCount() const;

		// Prepare shader constant buffers for rendering (thread-safe, no scene interaction).
		void PrepareRender(v4 camera_world_pos);

		// Add instances to the scene drawlist (NOT thread-safe, must be called serially).
		void AddToScene(Scene& scene);
	};
}