//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
// Procedural sky dome: renders atmospheric sky based on sun position.
// Replaces the static skybox with a cube viewed from inside.
#pragma once
#include "src/forward.h"

namespace las
{
	struct ProceduralSkyShader;

	struct ProceduralSky
	{
		struct Instance
		{
			#define PR_RDR_INST(x)\
			x(m4x4       , m_i2w  , EInstComp::I2WTransform)\
			x(ModelPtr   , m_model, EInstComp::ModelPtr)\
			x(SKOverride , m_sko  , EInstComp::SortkeyOverride)
			PR_RDR12_INSTANCE_MEMBERS(Instance, PR_RDR_INST);
			#undef PR_RDR_INST
		};

		static constexpr float DomeScale = 3500.0f;

		Instance m_inst;
		ProceduralSkyShader* m_shader;

		explicit ProceduralSky(Renderer& rdr);

		void PrepareRender(v4 sun_direction, v4 sun_colour, float sun_intensity);
		void AddToScene(Scene& scene);
	};
}
