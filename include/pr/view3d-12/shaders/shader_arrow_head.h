//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/shaders/shader.h"

namespace pr::rdr12::shaders
{
	struct ArrowHeadGS :ShaderOverride
	{
		v2 m_size = {};
		bool m_depth = false;
		explicit ArrowHeadGS(v2 size, bool depth);
		void SetupOverride(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& upload, Scene const& scene, DrawListElement const* dle) override;
	};
}