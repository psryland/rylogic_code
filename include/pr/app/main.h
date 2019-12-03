//*****************************************************************************************
// Application Framework
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
// How To Use:
//  namespace ns
//  {
//      // Derive a application logic type from pr::app::Main
//      struct Main :pr::app::Main<UserSettings, MainGUI>
//      {
//          static wchar_t const* AppName() const { return L"MyNewApp"; };
//          Main(MainGUI& gui)
//              :base(pr::app::DefaultSetup(), gui)
//          {}
//      };
//
//      // Derive a GUI class from pr::app::MainGUI
//      struct MainGUI :pr::app::MainGUI<MainGUI, Main>
//      {
//          static wchar_t const* AppTitle() const { return L"My New App"; };
//          MainGUI()
//              :pr::app::MainGUI<MainGUI, Main>(Params().title(AppTitle()))
//          {}
//      };
//  }
//  namespace pr::app
//  {
//      // Create the GUI window
//      std::unique_ptr<IAppMainGui> CreateGUI(wchar_t const* lpstrCmdLine, int nCmdShow)
//      {
//          return std::unique_ptr<IAppMainGui>(new ns::MainGUI(lpstrCmdLine, nCmdShow));
//      }
//  }
//
// Notes:
//  - If you're trying to declare everything in a header file, you'll need to define
//    'PR_APP_MAIN_INCLUDE' to be the header file containing the CreateGUI implementation.

#pragma once
#include "pr/app/forward.h"

namespace pr::app
{
	// This type contains the main app logic. It's lifetime is controlled by the GUI.
	// Apps should inherit this type providing custom functionality where required
	template<typename UserSettings, typename MainGUI>
	struct alignas(16) Main
	{
		// Define this type as base as a helper for derived type constructors
		// so they can call: MyType(...) :base(..) {}
		using base = Main<UserSettings,MainGUI>;

		UserSettings    m_settings;    // Application-wide user settings
		pr::Renderer    m_rdr;         // The renderer
		pr::rdr::Window m_window;      // The window that will be rendered into
		pr::rdr::Scene  m_scene;       // The main view
		pr::Camera      m_cam;         // A camera
		MainGUI&        m_gui;         // The GUI that owns this app logic class
		bool            m_rdr_pending; // Render call batching, true if 'RenderNeeded' has been called

		// Construct using a template set up object.
		template <typename Setup>
		Main(Setup setup, MainGUI& gui)
			:m_settings(setup.UserSettings())
			,m_rdr(setup.RdrSettings())
			,m_window(m_rdr, setup.RdrWindowSettings(gui, pr::ClientArea(gui).Size()))
			,m_scene(m_window,{pr::rdr::ERenderStep::ForwardRender})
			//,m_scene(m_window,{pr::rdr::ERenderStep::ShadowMap, pr::rdr::ERenderStep::ForwardRender})
			//,m_scene(m_window,{pr::rdr::ERenderStep::GBufferCreate, pr::rdr::ERenderStep::DSLighting})
			,m_cam()
			,m_gui(gui)
			,m_rdr_pending(false)
		{
			// Bind the window to the OM
			m_window.RestoreRT();

			// Position the camera
			m_cam.Aspect(1.0f);
			m_cam.FovY(float(pr::maths::tau_by_8));
			m_cam.LookAt(
				pr::v4(0, 0, 1.0f / (float)tan(m_cam.m_fovY/2.0f), 1.0f),
				pr::v4Origin,
				pr::v4YAxis, true);

			// The first frame is needed
			RenderNeeded();
		}
		Main(Main const&) = delete;
		Main& operator=(Main const&) = delete;
		virtual ~Main() {}

		// Mouse navigation
		virtual void Nav(pr::v2 const& pt, pr::gui::EMouseKey btn_state, bool nav_start_stop)
		{
			auto op = pr::camera::MouseBtnToNavOp(int(btn_state));
			m_cam.MouseControl(pt, op, nav_start_stop);
			RenderNeeded();
		}
		virtual void NavZ(pr::v2 const& pt, float delta, bool along_ray)
		{
			m_cam.MouseControlZ(pt, delta, along_ray);
			RenderNeeded();
		}
		virtual void NavRevert()
		{
			m_cam.Revert();
			RenderNeeded();
		}

		// The size of the window has changed
		virtual void Resize(pr::IRect const& area)
		{
			// Change the render target size
			m_window.BackBufferSize(area.Size());

			// Adjust the viewport
			m_scene.m_viewport.TopLeftX = float(area.Left ());
			m_scene.m_viewport.TopLeftY = float(area.Top  ());
			m_scene.m_viewport.Width    = float(area.SizeX());
			m_scene.m_viewport.Height   = float(area.SizeY());

			// Update the camera
			m_cam.Aspect(area.Aspect());
		}

		// Request a render.
		// Note: this can be called many times per frame with minimal cost
		virtual void RenderNeeded()
		{
			m_rdr_pending = true;
		}

		// The actual call to d3d present.
		// This is left to the derived app to call when appropriate.
		// For game-style apps that use a pr::SimMsgLoop, DoRender can be called in a step context
		//  e.g.
		//   m_msg_loop.AddStepContext("render loop", [this](double){ m_main->DoRender(true); }, 60.0f, false);
		// For general apps, DoRender could be called from a Timer, or in Paint
		virtual void DoRender(bool force = false)
		{
			// Only render if asked to
			if (!m_rdr_pending && !force)
				return;

			// Allow new render requests now
			m_rdr_pending = false;

			// Set the camera position
			m_scene.SetView(m_cam);

			// Reset and rebuild the drawlist
			m_scene.ClearDrawlists();
			m_scene.UpdateDrawlists();

			// Render the viewports
			m_scene.Render();

			// Show the result
			Present();
		}

		// Show the result
		virtual void Present()
		{
			m_window.Present();
		}

	protected:

		//todo // Pre-scene render. Set up a simple default scene. Derived apps will override this
		//todo void OnEvent(pr::rdr::Evt_UpdateScene const& e) override
		//todo {
		//todo 	e.m_scene.m_bkgd_colour = Colour(0.5f,0.5f,0.5f,1.0f);
		//todo 	e.m_scene.m_global_light.m_on = true;
		//todo 	e.m_scene.SetView(m_cam);
		//todo }
	};
}
