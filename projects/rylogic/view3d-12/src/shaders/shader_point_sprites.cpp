//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader_point_sprites.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12::shaders
{
	PointSpriteGS::PointSpriteGS(ResourceManager& mgr, int bb_count)
		:Shader(mgr, ShaderCode
		{
			.VS = shader_code::none,
			.PS = shader_code::none,
			.GS = shader_code::point_sprites_gs,
			.CS = shader_code::none,
			.DS = shader_code::none,
			.HS = shader_code::none,
		})
		,m_cbuf()
		,m_size(10.f, 10.f)
		,m_depth(true)
	{	
		Renderer::Lock lock(mgr.rdr());
		auto device = lock.D3DDevice();
		
		auto desc = BufferDesc::CBuf(bb_count * cbuf_size_aligned_v<hlsl::ss::CBufFrame>);
		Throw(device->CreateCommittedResource(
			&HeapProps::Upload(),
			D3D12_HEAP_FLAG_NONE,
			&desc,
			D3D12_RESOURCE_STATE_GENERIC_READ,
			nullptr,
			__uuidof(ID3D12Resource),
			(void**)&m_cbuf.m_ptr));
		Throw(m_cbuf->SetName(L"PointSpriteGS:CBuf"));
	}

	// Perform any setup of the shader state
	void PointSpriteGS::Setup()
	{
		// todo
	}
}
