//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/shaders/shader.h"
#include "pr/view3d-12/utility/ray_cast.h"

namespace pr::rdr12::shaders
{
	namespace ray_cast
	{
		enum class ERootParam
		{
			CBufFrame = 0,
			CBufNugget,
		};

		enum class ESampParam
		{
		};
	}

	struct RayCast :Shader
	{
		explicit RayCast(ID3D12Device* device);
		void SetupFrame(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& upload, std::span<HitTestRay const> rays, ESnapMode snap_mode, float snap_distance);
		void SetupElement(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& upload, DrawListElement const* dle);
	};
	struct RayCastVert :RayCast
	{
		explicit RayCastVert(ID3D12Device* device);
	};
	struct RayCastEdge :RayCast
	{
		explicit RayCastEdge(ID3D12Device* device);
	};
	struct RayCastFace :RayCast
	{
		explicit RayCastFace(ID3D12Device* device);
	};
}
