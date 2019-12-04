//*****************************************************************************************
// Application Framework
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
// How To Use:
//  namespace ns
//  {
//      // Derive a application logic type from app::Main
//      struct Main :app::Main<UserSettings, MainUI>
//      {
//          static wchar_t const* AppName() const { return L"MyNewApp"; };
//          Main(MainUI& gui)
//              :base(app::DefaultSetup(), gui)
//          {}
//      };
//
//      // Derive a GUI class from app::MainUI
//      struct MainUI :app::MainUI<MainUI, Main>
//      {
//          static wchar_t const* AppTitle() const { return L"My New App"; };
//          MainUI()
//              :app::MainUI<MainUI, Main>(Params().title(AppTitle()))
//          {}
//      };
//  }
//  namespace pr::app
//  {
//      // Create the GUI window
//      std::unique_ptr<IAppMainGui> CreateGUI(wchar_t const* lpstrCmdLine, int nCmdShow)
//      {
//          return std::unique_ptr<IAppMainGui>(new ns::MainUI(lpstrCmdLine, nCmdShow));
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
	// The application business logic
	template<typename Derived, typename MainUI, typename UserSettings>
	struct alignas(16) Main
	{
		// Notes:
		//  - Apps should inherit this type providing custom functionality where required.
		//  - The App framework creates the UI first so that the HWND exists before 'Main'
		//    is created. This allows normal construction of the renderer etc.

		Logger       m_log;         // App log
		UserSettings m_settings;    // Application-wide user settings
		Renderer     m_rdr;         // The renderer
		rdr::Window  m_window;      // The window that will be rendered into
		rdr::Scene   m_scene;       // The main view
		Camera       m_cam;         // A camera
		MainUI&      m_ui;          // The GUI that owns this app logic class
		bool         m_rdr_pending; // Render call batching, true if 'RenderNeeded' has been called

		// Construct using a template set up object.
		template <typename Setup>
		Main(Setup setup, MainUI& ui)
			:m_log(Derived::AppName(), log::ToFile(FmtS(L"%s.log", Derived::AppName())), 0)
			,m_settings(setup.UserSettings())
			,m_rdr(setup.RdrSettings())
			,m_window(m_rdr, setup.RdrWindowSettings(ui, ClientArea(ui).Size()))
			,m_scene(m_window,{rdr::ERenderStep::ForwardRender})
			,m_cam()
			,m_ui(ui)
			,m_rdr_pending(false)
		{
			// Bind the window to the OM
			m_window.RestoreRT();

			// Position the camera
			m_cam.FovY(maths::tau_by_8f);
			m_cam.LookAt(
				v4(0, 0, 1.0f / (float)tan(m_cam.FovY()/2.0f), 1.0f),
				v4Origin,
				v4YAxis, true);

			// Initialise the viewport to the padded client area
			auto area = To<FRect>(m_ui.ClientRect());
			m_cam.Aspect(area.Aspect());
			m_scene.m_viewport.TopLeftX = area.Left();
			m_scene.m_viewport.TopLeftY = area.Top();
			m_scene.m_viewport.Width    = area.SizeX();
			m_scene.m_viewport.Height   = area.SizeY();

			// The first frame is needed
			RenderNeeded();
		}
		Main(Main const&) = delete;
		Main& operator=(Main const&) = delete;
		virtual ~Main() {}

		// Mouse navigation
		virtual void Nav(v2 const& pt, gui::EMouseKey btn_state, bool nav_start_stop)
		{
			auto op = camera::MouseBtnToNavOp(int(btn_state));
			m_cam.MouseControl(pt, op, nav_start_stop);
			RenderNeeded();
		}
		virtual void NavZ(v2 const& pt, float delta, bool along_ray)
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
		virtual void Resize(IRect const& area)
		{
			// 'area' is the client area of the form including padding.
			// The back buffer needs to match that actual window client area.
			auto area_unpadded = To<IRect>(m_ui.ClientRect(false));
			m_window.BackBufferSize(area_unpadded.Size());

			// Adjust the viewport to the padded client area
			auto areaf = To<FRect>(area);
			m_scene.m_viewport.TopLeftX = areaf.Left();
			m_scene.m_viewport.TopLeftY = areaf.Top();
			m_scene.m_viewport.Width    = areaf.SizeX();
			m_scene.m_viewport.Height   = areaf.SizeY();

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
		// For game-style apps that use a SimMsgLoop, DoRender can be called in a step context
		//  e.g.
		//   m_msg_loop.AddStepContext("render loop", [this](double){ m_main->DoRender(true); }, 60.0f, false);
		// For general apps, DoRender could be called from a Timer, or in Paint
		void DoRender(bool force = false)
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
		void Present()
		{
			m_window.Present();
		}

	protected:

		//todo // Pre-scene render. Set up a simple default scene. Derived apps will override this
		//todo void OnEvent(rdr::Evt_UpdateScene const& e) override
		//todo {
		//todo 	e.m_scene.m_bkgd_colour = Colour(0.5f,0.5f,0.5f,1.0f);
		//todo 	e.m_scene.m_global_light.m_on = true;
		//todo 	e.m_scene.SetView(m_cam);
		//todo }
	};
}
