//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_UTIL_EVENT_TYPES_H
#define PR_RDR_UTIL_EVENT_TYPES_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/render_step.h"

namespace pr
{
	namespace rdr
	{
		struct Evt_Resize
		{
			bool    m_done;  // True when the swap chain has resized it's buffers
			pr::iv2 m_area;  // The render target size before (m_done == false) or after (m_done == true) the swap chain buffer resize
			Evt_Resize(bool done, pr::iv2 const& area) :m_done(done) ,m_area(area) {}
		};
		struct Evt_SceneRender
		{
			RenderStep* m_rsteps; // The render step that requires it's drawlist updated
			Scene*      m_scene; // The scene that owns the render step
			explicit Evt_SceneRender(RenderStep& rstep) :m_rsteps(&rstep) ,m_scene(rstep.m_scene) {}
		};
	}
}

#endif
