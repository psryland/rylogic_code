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
		// Raised by the calling SetRenderTargetSize on the renderer
		// Called twice, once before resizing, and once afterwards
		struct Evt_Resize
		{
			Window* m_window; // The renderer window that is resizing
			bool    m_done;   // True when the swap chain has resized it's buffers
			pr::iv2 m_area;   // The render target size before (m_done == false) or after (m_done == true) the swap chain buffer resize
			
			Evt_Resize(Window* window, bool done, pr::iv2 const& area) :m_window(window) ,m_done(done) ,m_area(area) {}
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

		// Raised during a compatibility test. Compatibility failures should throw
		struct Evt_CompatibilityTest
		{
		};

		// Raised during shutdown when a scene is about to be destructed
		struct Evt_SceneDestroy
		{
			Scene& m_scene; // The scene being destructed
			explicit Evt_SceneDestroy(Scene& scene) :m_scene(scene) {}

		private:
			Evt_SceneDestroy(Evt_SceneDestroy const&);
			Evt_SceneDestroy& operator=(Evt_SceneDestroy const&);
		};

		// Raised during shutdown when the renderer is about to be destructed
		struct Evt_RendererDestroy
		{
			Renderer& m_rdr; // The scene being destructed
			explicit Evt_RendererDestroy(Renderer& rdr) :m_rdr(rdr) {}

		private:
			Evt_RendererDestroy(Evt_RendererDestroy const&);
			Evt_RendererDestroy& operator=(Evt_RendererDestroy const&);
		};
	}
}
