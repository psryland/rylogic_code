//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/shaders/shader.h"

namespace pr::rdr12::shaders
{
	struct ShowNormalsGS :Shader
	{
		D3DPtr<ID3D12Resource> m_cbuf;

		explicit ShowNormalsGS(ResourceManager& mgr, int bb_count);

		// Perform any setup of the shader state
		void Setup() override;
	};
}