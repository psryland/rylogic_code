//*****************************************************************************************
// Application Framework
//  Copyright © Rylogic Ltd 2012
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
//  extern std::shared_ptr<ATL::CWindow> pr::app::CreateGUI(LPTSTR lpstrCmdLine)
//  {
//      return CreateGUI<sol::MainGUI>(lpstrCmdLine);
//  }

#pragma once
#ifndef PR_APP_MAIN_H
#define PR_APP_MAIN_H

#include "pr/app/forward.h"

namespace pr
{
	namespace app
	{
		// The WTL app module singleton
		inline CAppModule& Module()
		{
			static CAppModule s_module;
			return s_module;
		}

		// Custom apps must implement this function.
		// Note: they can simply call the template version below for default creation
		std::shared_ptr<ATL::CWindow> CreateGUI(LPTSTR lpstrCmdLine);
		template <typename WinType> std::shared_ptr<ATL::CWindow> CreateGUI(LPTSTR)
		{
			WinType* gui;
			std::shared_ptr<ATL::CWindow> ptr(gui = new WinType());
			if (gui->Create(0) == 0) throw pr::Exception<HRESULT>(E_FAIL, "Main window creation failed");
			return ptr;
		}

		// This type is a default and example of a setup object for the app.
		// It can be subclassed, or used directly
		struct DefaultSetup
		{
			// Returns a type containing information needed to initialise the UserSettings type in Main
			// Note: in this case the user settings type must have a constructor taking a void* argument
			virtual void* UserSettings(HWND) { return 0; }
			
			// Return settings to configure the render
			virtual pr::rdr::RdrSettings RdrSettings(HWND hwnd, pr::iv2 const& client_area) { return pr::rdr::RdrSettings(hwnd, TRUE, client_area); }
		};
		
		// This type contains the main app logic. It's lifetime is controlled by the GUI.
		// Apps should inherit this type providing custom functionality where required
		template
		<
			typename UserSettings,
			typename MainGUI
		>
		struct Main
		{
			// Define this type as base as a helper for derived type constructors
			// so they can call: MyType(...) :base(..) {}
			typedef Main<UserSettings,MainGUI> base;
			
			UserSettings           m_settings;    // Application wide user settings
			pr::Renderer           m_rdr;         // The renderer
			pr::rdr::SceneForward  m_scene;       // The main view
			pr::Camera             m_cam;         // A camera
			MainGUI&               m_gui;         // The GUI that owns this app logic class
			bool                   m_rdr_pending; // Render call batching, true if render has been called at least once
			
			// Construct using a template setup object.
			template <typename Setup>
			Main(Setup setup, MainGUI& gui)
			:m_settings(setup.UserSettings(gui.m_hWnd))
			,m_rdr(setup.RdrSettings(gui.m_hWnd, pr::ClientArea(gui.m_hWnd).Size()))
			,m_scene(m_rdr)
			,m_cam()
			,m_gui(gui)
			,m_rdr_pending(false)
			{
				m_scene.m_background_colour.set(0.5f,0.5f,0.5f,1.0f);
				
				// Position the camera
				m_cam.Aspect(1.0f);
				m_cam.FovY(pr::maths::tau_by_8);
				m_cam.LookAt(
					pr::v4::make(0, 0, 1.0f / (float)tan(m_cam.m_fovY/2.0f), 1.0f),
					pr::v4Origin, 
					pr::v4YAxis, true);
				//m_view0.CameraToWorld(m_cam.CameraToWorld());
				
				//// Configure a light
				//pr::rdr::Light& light = m_rdr.m_light_mgr.m_light[0];
				//light.m_type           = pr::rdr::ELight::Directional;
				//light.m_direction      = -pr::v4ZAxis;
				//light.m_ambient        = pr::Colour32Zero;
				//light.m_diffuse        = pr::Colour32Gray;
				//light.m_specular       = pr::Colour32Zero;
				//light.m_specular_power = 0;
				//light.m_cast_shadows   = false;
			}
			
			virtual ~Main()
			{}
			
			// Mouse navigation
			virtual void Nav(pr::v2 const& pt, int btn_state, bool nav_start_stop)
			{
				if (nav_start_stop) m_cam.MoveRef(pt, btn_state);
				else                m_cam.Move   (pt, btn_state);
				Render();
			}
			virtual void NavZ(float delta)
			{
				m_cam.MoveZ(delta, true);
				Render();
			}
			
			// The size of the window has changed
			virtual void Resize(pr::iv2 const& size)
			{
				m_rdr.Resize(size);
				m_cam.Aspect(size.x / float(size.y));
			}
			
			// Request a render.
			// Note: this can be called many times per frame which minimal cost
			virtual void Render()
			{
				m_rdr_pending = true;
			}
			
			// The actual call to d3d present
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
				m_scene.ClearDrawlist();
				m_scene.UpdateDrawlist();
				
				// Render the viewports
				m_scene.Render();
				
				// Show the result
				m_rdr.Present();
			}
			
		private:
			Main(Main const&);
			Main& operator=(Main const&);
		};
	}
}
#endif
