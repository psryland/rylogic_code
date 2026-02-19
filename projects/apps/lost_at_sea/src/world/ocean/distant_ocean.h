//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
// Distance ocean: flat z=0 patches beyond the near Gerstner ocean.
// Uses CDLOD quadtree for LOD selection with inner cutout matching
// the near ocean's outer radius.
#pragma once
#include "src/forward.h"
#include "src/world/terrain/cdlod.h"

namespace las
{
	struct DistantOceanShader;

	struct DistantOcean
	{
		struct PatchInstance
		{
			#define PR_RDR_INST(x)\
			x(m4x4     , m_i2w  , EInstComp::I2WTransform)\
			x(ModelPtr , m_model, EInstComp::ModelPtr)
			PR_RDR12_INSTANCE_MEMBERS(PatchInstance, PR_RDR_INST);
			#undef PR_RDR_INST
		};

		static constexpr float MinDrawDist = 1000.0f;   // Inner cutout radius (matches near ocean OuterRadius)
		static constexpr float MaxDrawDist = 5000.0f;   // Maximum draw distance
		static constexpr int MaxPatches = 256;

		ModelPtr m_grid_mesh;
		DistantOceanShader* m_shader;
		CDLODSelection m_lod_selection;
		std::vector<PatchInstance> m_instances;

		explicit DistantOcean(Renderer& rdr);

		int PatchCount() const;
		void PrepareRender(v4 camera_world_pos, bool has_env_map, v4 sun_direction, v4 sun_colour);
		void AddToScene(Scene& scene);
	};
}
