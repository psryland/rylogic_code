//*****************************************************************************************
// Application Framework
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
// How To Use:
// #my_app.h - can define the whole app in one header if needed
//  namespace ns
//  {
//      // Create a UserSettings object for loading/saving app settings
//      struct UserSettings { UserSettings(void*) {} }; // dummy
//
//      // Derive a application logic type from pr::app::Main
//      struct Main :pr::app::Main<UserSettings, MainGUI>
//      {
//           wchar_t const* AppTitle() const { return L"My New App"; };
//
//           Main(MainGUI& gui)
//           :base(pr::app::DefaultSetup(), gui)
//           {}
//      };
//
//      // Derive a GUI class from pr::app::MainGUI
//      struct MainGUI :pr::app::MainGUI<MainGUI, Main>
//      {
//          overload methods to add custom behaviour
//      };
//  }
//  // Create the GUI window
//  extern std::shared_ptr<pr::app::IAppMainGui> pr::app::CreateGUI(wchar_t const* lpstrCmdLine)
//  {
//      return CreateGUI<ns::MainGUI>(lpstrCmdLine);
//  }

#pragma once

#include "pr/app/forward.h"

namespace pr
{
	namespace app
	{
		// Custom apps must implement this function.
		// Note: they can simply call the template version below for default creation
		std::shared_ptr<pr::app::IAppMainGui> CreateGUI(wchar_t const* lpstrCmdLine, int nCmdShow);
		template <typename WinType> std::shared_ptr<pr::app::IAppMainGui> CreateGUI(wchar_t const* cmdline, int nCmdShow)
		{
			WinType* gui;
			std::shared_ptr<pr::app::IAppMainGui> ptr(gui = new WinType(cmdline, nCmdShow));
			return ptr;
		}

		// This type is a default and example of a set up object for the app.
		struct DefaultSetup
		{
			// The Main object contains a user defined 'UserSettings' type which may be needed before
			// configuring the renderer. In order to construct the UserSettings instance a method with
			// the name 'UserSettings' is called with its return type provided to the user defined type.
			// The return type can be anything that the user defined settings type will accept.
			// e.g.
			//   Return an instance of the user defined type, to construct by copy constructor
			//   Return 'this' and allow the settings object to read members of this type
			//   Return a filepath that the settings can load from.
			//   Unfortunately it can't return void to pass to a parameterless constructor so use int:-/
			int UserSettings() { return 0; }

			static BOOL const GdiCompat = FALSE;

			// Return settings to configure the render
			pr::rdr::RdrSettings RdrSettings()
			{
				return pr::rdr::RdrSettings(GdiCompat);
			}

			// Return settings for the render window
			pr::rdr::WndSettings RdrWindowSettings(HWND hwnd, pr::iv2 const& client_area)
			{
				return pr::rdr::WndSettings(hwnd, TRUE, GdiCompat, client_area);
			}
		};

		// This type contains the main app logic. It's lifetime is controlled by the GUI.
		// Apps should inherit this type providing custom functionality where required
		template
		<
			typename UserSettings,
			typename MainGUI
		>
		struct Main
			:pr::AlignTo<16>
			,pr::events::IRecv<pr::rdr::Evt_UpdateScene>
			,pr::events::IRecv<pr::rdr::Evt_RenderStepExecute>
		{
			// Define this type as base as a helper for derived type constructors
			// so they can call: MyType(...) :base(..) {}
			typedef Main<UserSettings,MainGUI> base;

			UserSettings    m_settings;    // Application wide user settings
			pr::Renderer    m_rdr;         // The renderer
			pr::rdr::Window m_window;      // The window that will be rendered into
			pr::rdr::Scene  m_scene;       // The main view
			pr::Camera      m_cam;         // A camera
			MainGUI&        m_gui;         // The GUI that owns this app logic class
			bool            m_rdr_pending; // Render call batching, true if render has been called at least once

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
				m_cam.FovY(pr::maths::tau_by_8);
				m_cam.LookAt(
					pr::v4(0, 0, 1.0f / (float)tan(m_cam.m_fovY/2.0f), 1.0f),
					pr::v4Origin,
					pr::v4YAxis, true);

				// The first frame is needed
				RenderNeeded();
			}
			virtual ~Main()
			{}

			// Mouse navigation
			virtual void Nav(pr::v2 const& pt, pr::gui::EMouseKey btn_state, bool nav_start_stop)
			{
				// EMouseKey and ENavBtn are both enums based on the MK_ macros
				auto btnstate = static_cast<pr::camera::ENavBtn>(btn_state);
				m_cam.MouseControl(pt, btnstate, nav_start_stop);
				RenderNeeded();
			}
			virtual void NavZ(float delta)
			{
				m_cam.Translate(0, 0, delta);
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
				m_window.RenderTargetSize(area.Size());

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

			// Pre-scene render. Set up a simple default scene. Derived apps will override this
			void OnEvent(pr::rdr::Evt_UpdateScene const& e) override
			{
				e.m_scene.m_bkgd_colour = Colour(0.5f,0.5f,0.5f,1.0f);
				e.m_scene.m_global_light.m_on = true;
				e.m_scene.SetView(m_cam);
			}
			void OnEvent(pr::rdr::Evt_RenderStepExecute const&) override
			{
				// Inherited as most apps will use this event
			}

		private:
			Main(Main const&);
			Main& operator=(Main const&);
		};
	}
}
