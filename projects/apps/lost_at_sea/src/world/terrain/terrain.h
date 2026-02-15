//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Terrain rendering using GPU-side Perlin noise displacement.
// Uses the same radial mesh pattern as the ocean. The vertex shader
// reconstructs world positions from encoded ring/segment data and
// displaces them vertically using multi-octave Perlin noise.
#pragma once
#include "src/forward.h"

namespace las
{
	struct TerrainShader;

	// Terrain simulation and rendering
	struct Terrain
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
		TerrainShader* m_shader; // Owned by 'm_model'

		explicit Terrain(Renderer& rdr);

		// Rendering: update shader constants and add to the scene.
		void AddToScene(Scene& scene, v4 camera_world_pos);
	};
}