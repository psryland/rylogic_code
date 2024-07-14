//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/render/back_buffer.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/scene/scene.h"

namespace pr::rdr12
{
	// Accessors
	Renderer& BackBuffer::rdr() const
	{
		return wnd().rdr();
	}
	Window& BackBuffer::wnd() const
	{
		return *m_wnd;
	}
	float4_t const& BackBuffer::rt_clear() const
	{
		return wnd().m_rt_props.Color;
	}
	float BackBuffer::ds_depth() const
	{
		return wnd().m_ds_props.DepthStencil.Depth;
	}
	uint8_t BackBuffer::ds_stencil() const
	{
		return wnd().m_ds_props.DepthStencil.Stencil;
	}
}
