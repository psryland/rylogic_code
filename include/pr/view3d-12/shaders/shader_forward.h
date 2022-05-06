//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/shaders/shader.h"

namespace pr::rdr12::shaders
{
	namespace fwd
	{
		enum class ERootParam
		{
			CBufFrame = 0,
			CBufNugget,
			CBufFade,
			CBufScreenSpace,
			CBufDiag = CBufScreenSpace,
			DiffTexture,
			EnvMap,
			SMap,
			ProjTex,
		};

		enum class ESampParam
		{
			DiffTexture,
			EnvMap,
			SMap,
			ProjTex,
		};
	}

	struct Forward :Shader
	{
		explicit Forward(ID3D12Device* device);
		void Setup(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& cbuf, Scene const& scene, DrawListElement const* dle) override;
	};
}