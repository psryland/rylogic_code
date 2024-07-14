//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/wrappers.h"

namespace pr::rdr12
{
	// Data associated with a back buffer
	struct BackBuffer
	{
		// Notes:
		//  - The swap chain can contain multiple back buffers. There will be one of these per swap chain back buffer.
		//  - When rendering to an off-screen target, create one of these to represent the render target.

		Window*                     m_wnd;           // The owning window
		uint64_t                    m_sync_point;    // The sync point of the last render to this back buffer
		D3DPtr<ID3D12Resource>      m_render_target; // The back buffer render target
		D3DPtr<ID3D12Resource>      m_depth_stencil; // The back buffer depth stencil
		D3DPtr<ID2D1Bitmap1>        m_d2d_target;    // D2D render target
		D3D12_CPU_DESCRIPTOR_HANDLE m_rtv;           // The descriptor of the back buffer as a RTV
		D3D12_CPU_DESCRIPTOR_HANDLE m_dsv;           // The descriptor of the back buffer as a DSV
		MultiSamp                   m_multisamp;     // The multisampling mode of the back buffer

		BackBuffer()
			: m_wnd()
			, m_sync_point()
			, m_render_target()
			, m_depth_stencil()
			, m_d2d_target()
			, m_rtv()
			, m_dsv()
			, m_multisamp()
		{}

		// Accessors
		Renderer& rdr() const;
		Window& wnd() const;

		float4_t const& rt_clear() const;
		float ds_depth() const;
		uint8_t ds_stencil() const;
	};
}