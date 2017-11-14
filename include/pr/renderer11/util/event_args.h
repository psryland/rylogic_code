//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		// Event Args for the Window.RenderTargetSizeChanged event
		struct RenderTargetSizeChangedEventArgs
		{
			pr::iv2 m_area;   // The render target size before (m_done == false) or after (m_done == true) the swap chain buffer resize
			bool    m_done;   // True when the swap chain has resized it's buffers
			
			RenderTargetSizeChangedEventArgs(pr::iv2 const& area, bool done)
				:m_area(area)
				,m_done(done)
			{}
		};

		// Raised once just before a scene is rendered.
		// Observers of this event should add/remove instances to the scene or specific render steps as needed
		struct Evt_UpdateScene
		{
			Scene& m_scene; // The scene that owns the render step
			explicit Evt_UpdateScene(Scene& scene) :m_scene(scene)  {}
			Evt_UpdateScene(Evt_UpdateScene const&) = delete;
			Evt_UpdateScene& operator=(Evt_UpdateScene const&) = delete;
		};

		// Raised during a compatibility test. Compatibility failures should throw
		struct Evt_CompatibilityTest
		{
		};
	}
}
