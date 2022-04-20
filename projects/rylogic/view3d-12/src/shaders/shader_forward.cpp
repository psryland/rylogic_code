//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader_forward.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/main/window.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12::shaders
{
	Forward::Forward(ResourceManager& mgr, int bb_count)
		:Shader(mgr, ShaderCode
		{
			.VS = shader_code::forward_vs,
			.PS = shader_code::forward_ps,
			.GS = shader_code::none,
			.CS = shader_code::none,
			.DS = shader_code::none,
			.HS = shader_code::none,
		})
		,m_cbuf_frame()
		,m_cbuf_nugget()
	{
		Renderer::Lock lock(mgr.rdr());
		auto device = lock.D3DDevice();

		// Create resources for the constant buffers.
		// One for each BB so we can write to one while the other is in flight.
		auto desc0 = BufferDesc::CBuf(bb_count * cbuf_size_aligned_v<hlsl::fwd::CBufFrame>);
		Throw(device->CreateCommittedResource(
			&HeapProps::Upload(),
			D3D12_HEAP_FLAG_NONE,
			&desc0,
			D3D12_RESOURCE_STATE_GENERIC_READ,
			nullptr,
			__uuidof(ID3D12Resource),
			(void**)&m_cbuf_frame.m_ptr));
		Throw(m_cbuf_frame->SetName(L"Forward:CBufFrame"));

		auto desc1 = BufferDesc::CBuf(bb_count * cbuf_size_aligned_v<hlsl::fwd::CBufNugget>);
		Throw(device->CreateCommittedResource(
			&HeapProps::Upload(),
			D3D12_HEAP_FLAG_NONE,
			&desc1,
			D3D12_RESOURCE_STATE_GENERIC_READ,
			nullptr,
			__uuidof(ID3D12Resource),
			(void**)&m_cbuf_nugget.m_ptr));
		Throw(m_cbuf_frame->SetName(L"Forward:CBufNugget"));
	}

	// Perform any setup of the shader state
	void Forward::Setup()
	{
	}
}

