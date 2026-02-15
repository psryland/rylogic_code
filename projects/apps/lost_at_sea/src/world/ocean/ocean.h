//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Gerstner wave ocean simulation.
// GPU vertex shader handles wave displacement. CPU-side queries for physics.
#pragma once
#include "src/forward.h"
#include "src/world/ocean/gerstner_wave.h"

namespace las
{
	struct OceanShader;

	// Ocean simulation and rendering
	struct Ocean
	{
		struct Instance
		{
			#define PR_RDR_INST(x)\
			x(m4x4     , m_i2w  , EInstComp::I2WTransform)\
			x(ModelPtr , m_model, EInstComp::ModelPtr)
			PR_RDR12_INSTANCE_MEMBERS(Instance, PR_RDR_INST);
			#undef PR_RDR_INST
		};

		Instance m_inst;
		vector<GerstnerWave> m_waves;
		OceanShader* m_shader; // Owned by 'm_model'

		explicit Ocean(Renderer& rdr);

		// Physics queries (read-only, no rendering side effects)
		float HeightAt(float world_x, float world_y, float time) const;
		v4 DisplacedPosition(float world_x, float world_y, float time) const;
		v4 NormalAt(float world_x, float world_y, float time) const;

		// Rendering: update shader constants and add to the scene.
		void AddToScene(Scene& scene, v4 camera_world_pos, float time);
	};
}