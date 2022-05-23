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
	BackBuffer::BackBuffer()
		:m_wnd()
		,m_bb_index()
		,m_sync_point()
		,m_render_target()
		,m_depth_stencil()
		,m_rtv()
		,m_dsv()
		,m_d2d_target()
	{}

	// Accessors
	Renderer& BackBuffer::rdr() const
	{
		return wnd().rdr();
	}
	Window& BackBuffer::wnd() const
	{
		return *m_wnd;
	}
}
