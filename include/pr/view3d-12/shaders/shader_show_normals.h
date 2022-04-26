//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/shaders/shader.h"

namespace pr::rdr12::shaders
{
	namespace ss
	{
		struct CBufFrame;
	}

	struct ShowNormalsGS :Shader
	{
		ShowNormalsGS(ResourceManager& mgr, GpuSync& gsync);

		// Add shader constants to an upload buffer
		D3D12_GPU_VIRTUAL_ADDRESS Set(ss::CBufFrame const& cbuf, bool might_reuse);
	};
}