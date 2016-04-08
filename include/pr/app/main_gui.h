//*****************************************************************************************
// Application Framework
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
// See pr/app/main.h for usage instructions
#pragma once

#include "pr/app/forward.h"
#pragma warning (disable:4355)

namespace pr
{
	namespace app
	{
		// App interface
		struct IAppMainGui
		{
			virtual ~IAppMainGui() {}
			virtual int Run() = 0;
		};

		// A base class for a main app window.
		// Provides the common code support for a main 3D graphics window
		template
		<
			typename DerivedGUI,
			typename Main,
			typename MessageLoop = pr::gui::MessageLoop // Alternatives are: SimMsgLoop
		>
		struct MainGUI :pr::gui::Form ,IAppMainGui
		{
			using base = pr::gui::Form;

			pr::Logger            m_log;                       // App log
			MessageLoop           m_msg_loop;                  // The message pump
			std::unique_ptr<Main> m_main;                      // The app logic object
			bool                  m_resizing;                  // True during a resize of the main window
			bool                  m_nav_enabled;               // True while a mouse button is down during default mouse navigation
			bool                  m_fullscreen_toggle_enabled; // Allow Alt+Enter to toggle between full screen and windowed
			LONG                  m_click_thres;               // Single click time threshold in ms
			LONG                  m_down_at[4];                // Button down timestamp
			int                   m_exit_code;                 // Exit code

			//static HBRUSH WndBackground() { return nullptr; }
			struct Params :pr::gui::FormParams<Params>
			{
				Params() { wndclass(RegisterWndClass<DerivedGUI>()).main_wnd(true); }
			};

			// Create the main application window.
			// This class is sub-classed from pr::gui::Form which actually does the 'CreateWindowEx' call in
			// it's constructor. This means the HWND is valid after the base class has been constructed.
			// If your window uses common controls, remember to call InitCtrls() before this constructor
			MainGUI(pr::gui::Params const& p = Params())
				:base(p)
				,m_log(DerivedGUI::AppName(), pr::log::ToFile(FmtS("%s.log", DerivedGUI::AppName())), 0)
				,m_msg_loop()
				,m_main(std::make_unique<Main>(*static_cast<DerivedGUI*>(this)))
				,m_resizing(false)
				,m_nav_enabled(false)
				,m_fullscreen_toggle_enabled(true)
				,m_click_thres(200)
				,m_down_at()
				,m_exit_code()
			{
				// Note: derived classes may need to set up a method for rendering,
				// By default, rendering occurs in OnPaint(), however if a SimMsgLoop
				// is used, the derived class will need to register a step context that
				// calls Render()
				Show();
			}
			virtual ~MainGUI()
			{
				m_main = nullptr;

				//// It's too late to call DestroyWindow here, because that posts a WM_DESTROY
				//// message to the thread queue which should be handled resulting in a WM_QUIT
				//// being posted which causes the msg loop to return.
				//PR_ASSERT(PR_DBG, m_main == nullptr && m_hwnd == 0, "Destructing MainGUI before DestroyWindow has been called");
				
				// This should only happen during an unhandled exception
				//PR_INFO_EXP(PR_DBG, m_main == 0, "Destructing MainGUI before DestroyWindow has been called");
				//if (IsWindow())
				//	DestroyWindow();
			}

			// Pump messages
			virtual int Run() override
			{
				return m_msg_loop.Run();
			}

			// Message map function
			bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				switch (message)
				{
				case WM_SYSKEYDOWN:
					{
						auto vk_key  = UINT(wparam);
						auto repeats = UINT(lparam & 0xFFFF);
						auto flags   = UINT((lparam & 0xFFFF0000) >> 16);
						OnSysKeyDown(vk_key, repeats, flags);
						break;
					}
				}
				return base::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
			}

			// Invalidate the control for redraw
			virtual void Invalidate(bool erase = false, pr::gui::Rect* rect = nullptr)
			{
				if (m_main) m_main->RenderNeeded();
				return base::Invalidate(erase, rect);
			}

			// Called when the system menu key command to switch between fullscreen and windowed is detected.
			// Derived types need to actually implement the mode switch as well as hide or show status bars, menus etc
			virtual void OnFullScreenToggle(bool enable_fullscreen)
			{
				(void)enable_fullscreen;
			}

			// Rendering the window
			virtual bool OnPaint(pr::gui::PaintEventArgs const& args) override
			{
				// Render the scene
				if (m_main)
				{
					m_main->DoRender(true); // We've been asked to paint, so paint, regardless of RenderNeeded()

					// Tell windows we're drawn the viewport area
					pr::gui::Rect cr = m_main->m_scene.m_viewport.AsRECT();
					Validate(&cr);
				}

				// Call the base to raise the paint event
				return base::OnPaint(args);
			}
			virtual bool OnEraseBkGnd(pr::gui::EmptyArgs const& args) override
			{
				// We paint the entire window so no need to erase
				return Minimised() ? base::OnEraseBkGnd(args) : true;
			}

			// Default mouse navigation behaviour
			virtual bool OnMouseButton(pr::gui::MouseEventArgs const& args) override
			{
				m_nav_enabled = args.m_down;
				m_main->Nav(pr::NormalisePoint(*this, args.m_point), args.m_down ? args.m_button : pr::gui::EMouseKey::None, true);
				Invalidate();
				return base::OnMouseButton(args);
			}
			virtual bool OnMouseClick(pr::gui::MouseEventArgs const& args) override
			{
				m_main->NavRevert(); // If a mouse single click is detected, revert any navigation
				Invalidate();
				return base::OnMouseClick(args);
			}
			virtual void OnMouseMove(pr::gui::MouseEventArgs const& args) override
			{
				if (m_nav_enabled)
				{
					m_main->Nav(pr::NormalisePoint(*this, args.m_point), args.m_keystate, false);
					Invalidate();
				}
				return base::OnMouseMove(args);
			}
			virtual bool OnMouseWheel(pr::gui::MouseWheelArgs const& args) override
			{
				m_main->NavZ(args.m_delta / (float)WHEEL_DELTA);
				Invalidate();
				return base::OnMouseWheel(args);
			}

			// Resizing handlers
			virtual void OnWindowPosChange(pr::gui::SizeEventArgs const& args) override
			{
				if (args.m_before)
				{
					m_resizing = true;
				}
				else
				{
					m_resizing = false;

					// Find the new client area
					auto area = ClientRect();//ClientRect(*this, true);
					if (m_main && area.width() > 0 && area.height() > 0)
					{
						m_main->Resize(pr::To<IRect>(area));
						m_main->RenderNeeded();
					}
				}
				base::OnWindowPosChange(args);
			}

			// Handle system menu keys
			virtual void OnSysKeyDown(uint vk_key, UINT repeats, UINT flags)
			{
				(void)repeats,flags;

				// Watch for full screen alt-enter transitions
				if (m_fullscreen_toggle_enabled && vk_key == VK_RETURN)
				{
					// If currently in full screen mode, switch to windowed or visa versa
					OnFullScreenToggle(!m_main->m_window.FullScreenMode());

					m_main->DoRender(true);
				}
			}
		};
	}
}

