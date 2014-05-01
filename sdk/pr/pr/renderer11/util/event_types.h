//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_UTIL_EVENT_TYPES_H
#define PR_RDR_UTIL_EVENT_TYPES_H

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		// Raised by the calling SetRenderTargetSize on the renderer
		// Called twice, once before resizing, and once afterward
		struct Evt_Resize
		{
			bool    m_done;  // True when the swap chain has resized it's buffers
			pr::iv2 m_area;  // The render target size before (m_done == false) or after (m_done == true) the swap chain buffer resize
			Evt_Resize(bool done, pr::iv2 const& area) :m_done(done) ,m_area(area) {}
		};

		// Raised once just before a scene is rendered.
		// Observers of this event should add/remove instances to the scene or specific render steps as needed
		struct Evt_UpdateScene
		{
			Scene& m_scene; // The scene that owns the render step
			explicit Evt_UpdateScene(Scene& scene) :m_scene(scene)  {}

		private:
			Evt_UpdateScene(Evt_UpdateScene const&);
			Evt_UpdateScene& operator=(Evt_UpdateScene const&);
		};

		// Raised before and after each render step during a scene render.
		struct Evt_RenderStepExecute
		{
			RenderStep& m_rstep;  // The render step begin executed
			bool m_complete;      // False before, true after
			explicit Evt_RenderStepExecute(RenderStep& rstep, bool complete) :m_rstep(rstep) ,m_complete(complete) {}

		private:
			Evt_RenderStepExecute(Evt_RenderStepExecute const&);
			Evt_RenderStepExecute& operator =(Evt_RenderStepExecute const&);
		};
	}
}

#endif
