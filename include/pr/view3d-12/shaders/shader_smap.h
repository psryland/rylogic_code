//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/shaders/shader.h"

namespace pr::rdr12::shaders
{
	namespace smap
	{
		enum class ERootParam
		{
			CBufFrame = 0,
			CBufNugget,
			DiffTexture,
			DiffTextureSampler,
		};

		enum class ESampParam
		{
		};
	}

	struct ShadowMap :Shader
	{
		explicit ShadowMap(ID3D12Device* device);

		// Config the shader.
		// This method may be called with:
		//  'dle == null' => Setup constants for the frame
		//  'dle != null' => Setup constants per nugget
		void Setup(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& cbuf, DrawListElement const* dle, ShadowCaster const& caster, SceneCamera const& cam);
	};
}