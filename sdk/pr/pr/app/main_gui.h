//*****************************************************************************************
// Application Framework
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
// See pr/app/main.h for usage instructions
#pragma once
#ifndef PR_APP_MAIN_GUI_H
#define PR_APP_MAIN_GUI_H

#include "pr/app/forward.h"

namespace pr
{
	namespace app
	{
		// A base class for a main app window.
		// Provides the common code support for a main 3D graphics window
		template
			<
			typename DerivedGUI,
			typename Main,
			typename MessageLoop = CMessageLoop // Alternatives are: SimMsgLoop
			>
		struct MainGUI
			:WTL::CFrameWindowImpl<DerivedGUI>
			,WTL::CMessageFilter
			,WTL::CIdleHandler
		{
			// Define this type as base as a helper for derived type constructors
			// so they can call: MyType(...) :base(..) {}
			typedef MainGUI<DerivedGUI, Main, MessageLoop> base;

			MessageLoop           m_msg_loop;     // The message pump
			std::unique_ptr<Main> m_main;         // The app logic object
			DWORD                 m_my_thread_id; // The thread this gui object was created on
			bool                  m_resizing;     // True during a resize of the main window
			bool                  m_nav_enabled;  // True to allow default mouse navigation

			MainGUI()
				:m_msg_loop()
				,m_main(nullptr)
				,m_my_thread_id(GetCurrentThreadId())
				,m_resizing(false)
				,m_nav_enabled(false)
			{
				// Register this class for message filtering and idle updates
				m_msg_loop.AddMessageFilter(this);
				m_msg_loop.AddIdleHandler(this);

				// The main window message loop
				// The app module maintains a map from thread id to message loop.
				// We could use this to add method loops for other threads if needed
				pr::app::Module().AddMessageLoop(&m_msg_loop);
			}
			virtual ~MainGUI()
			{
				PR_ASSERT(PR_DBG, m_main == 0, "Destructing MainGUI before DestroyWindow has been called");
				pr::app::Module().RemoveMessageLoop();
			}

			// Create/Destroy the main window
			virtual LRESULT OnCreate(LPCREATESTRUCT)
			{
				// Create the main app logic
				try { m_main.reset(new Main(static_cast<DerivedGUI&>(*this))); }
				catch (std::exception const& ex)
				{
					char const* err_msg = pr::FmtS("Failed to create application\nReturned error: %s", ex.what());
					MessageBox(err_msg, "Application Startup Error", MB_OK|MB_ICONERROR);
					//PR_LOG(Error, err_msg);
					CloseApp(E_FAIL);
					return E_FAIL;
				}
				catch (...)
				{
					char const* err_msg = pr::FmtS("Failed to create application due to an unknown exception");
					MessageBox(err_msg, "Application Startup Error", MB_OK|MB_ICONERROR);
					//PR_LOG(Error, err_msg);
					CloseApp(E_FAIL);
					return E_FAIL;
				}

				// Window title
				SetWindowTextW(m_hWnd, m_main->AppTitle());

				// Example derived class code:

				// Set icons
				//SetIcon((HICON)::LoadImage(create->hInstance, "MainIcon", IMAGE_ICON, ::GetSystemMetrics(SM_CXICON),   ::GetSystemMetrics(SM_CYICON),   LR_DEFAULTCOLOR) ,TRUE);
				//SetIcon((HICON)::LoadImage(create->hInstance, "MainIcon", IMAGE_ICON, ::GetSystemMetrics(SM_CXSMICON), ::GetSystemMetrics(SM_CYSMICON), LR_DEFAULTCOLOR) ,FALSE);

				// Load menu
				//SetMenu(LoadMenu(create->hInstance, MAKEINTRESOURCE(IDR_MENU_MAIN)));

				// Load accelerators (m_hAccel is a member of CFrameWindowImpl)
				//m_hAccel = (HACCEL)::LoadAccelerators(create->hInstance, MAKEINTRESOURCE(IDR_ACCELERATOR2));

				// Note: derived classes may need to set up a method for rendering,
				// By default, rendering occurs in OnPaint(), however if a SimMsgLoop
				// is used, the derived class will need to register a step context that
				// calls Render()
				return S_OK;
			}
			virtual void OnDestroy()
			{
				m_main = nullptr;
				CMessageLoop* loop = pr::app::Module().GetMessageLoop();
				loop->RemoveMessageFilter(this);
				loop->RemoveIdleHandler(this);
				SetMsgHandled(FALSE);
			}
			virtual void CloseApp(int exit_code)
			{
				DestroyWindow();
				::PostQuitMessage(exit_code);
				m_hWnd = 0;
			}

			// Idle handler
			BOOL OnIdle()
			{
				return FALSE;
			}
			BOOL PreTranslateMessage(MSG*)
			{
				return FALSE;
			}

			// Timers
			virtual void OnTimer(UINT_PTR nIDEvent)
			{
				(void)nIDEvent;
			}

			// Rendering the window
			virtual LRESULT OnEraseBkGnd(HDC hdc)
			{
				(void)hdc;
				if (m_resizing)
				{
					CBrush brush; brush.CreateSolidBrush(0xFF808080);
					CRect r; GetClientRect(&r);
					CPoint ctr = r.CenterPoint();
					CDCHandle dc(hdc);
					dc.FillRect(&r, brush);
					dc.SetTextAlign(TA_CENTER|TA_BASELINE);
					dc.SetBkMode(TRANSPARENT);
					dc.TextOutA(ctr.x, ctr.y, "...resizing...");
				}
				//if (m_sizing)
				//{
				//	HBRUSH hbrush = CreateSolidBrush(m_ldr->Settings().m_background_colour.GetColorRef());
				//	CRect r; GetClientRect(&r);
				//	CPoint ctr = r.CenterPoint();
				//	CDCHandle dc(GetDC());
				//	dc.FillRect(&r, hbrush);
				//	dc.SetTextAlign(TA_CENTER|TA_BASELINE);
				//	dc.SetBkMode(TRANSPARENT);
				//	dc.TextOutA(ctr.x, ctr.y, "...resizing...");
				//	DeleteObject(hbrush);
				//}
				return S_OK;
			}
			virtual void OnPaint(HDC)
			{
				if (m_main/* && !m_resizing*/) m_main->DoRender();
				SetMsgHandled(FALSE);
			}

			// Resizing handlers
			virtual void OnGetMinMaxInfo(LPMINMAXINFO lpMMI)
			{
				lpMMI->ptMinTrackSize.x = 160;
				lpMMI->ptMinTrackSize.x = 90;
			}
			virtual void OnSizing(UINT, LPRECT)
			{
				SetMsgHandled(FALSE);
				m_resizing = true;
			}
			virtual void OnExitSizeMove()
			{
				SetMsgHandled(FALSE);
				m_resizing = false;
				OnSize(0, CSize());
			}
			virtual void OnSize(UINT type, CSize)
			{
				SetMsgHandled(FALSE);
				if (m_resizing)
					return;
				if (type != SIZE_MINIMIZED)
				{
					// Find the new client area
					pr::IRect area = pr::ClientArea(m_hWnd);
					if (m_hWndStatusBar) area.m_max.y -= pr::WindowBounds(m_hWndStatusBar).SizeY();

					//SaveWindowBounds();
					//UpdateUI();
					UpdateLayout(true);
					if (m_main)
					{
						m_main->Resize(area.Size());
						m_main->RenderNeeded();
					}
				}
			}

			// Key down/up
			virtual void OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
			{
				(void)nChar;
				(void)nRepCnt;
				(void)nFlags;
				SetMsgHandled(FALSE);
			}
			virtual void OnKeyUp(UINT nChar, UINT nRepCnt, UINT nFlags)
			{
				(void)nChar;
				(void)nRepCnt;
				(void)nFlags;
				SetMsgHandled(FALSE);
			}

			// Default mouse navigation behaviour
			virtual void OnMouseDown(UINT flags, CPoint point)
			{
				m_nav_enabled = true;
				m_main->Nav(pr::NormalisePoint(m_hWnd, point), flags, true);
			}
			virtual void OnMouseUp(UINT, CPoint point)
			{
				m_nav_enabled = false;
				m_main->Nav(pr::NormalisePoint(m_hWnd, point), 0, true);
			}
			virtual void OnMouseMove(UINT flags, CPoint point)
			{
				if (m_nav_enabled)
					m_main->Nav(pr::NormalisePoint(m_hWnd, point), flags, false);
			}
			virtual BOOL OnMouseWheel(UINT, short delta, CPoint)
			{
				m_main->NavZ(delta / (float)WHEEL_DELTA);
				return FALSE; // ie. we handled this wheel message
			}

			// Drag-drop
			virtual void OnDropFiles(HDROP hDropInfo)
			{
				(void)hDropInfo;
			}

			// Message Map
			// Derived app should create there own msg map, and use CHAIN_MSG_MAP(base)
			BEGIN_MSG_MAP(x)
				MSG_WM_CREATE(OnCreate)
				MSG_WM_DESTROY(OnDestroy)
				MSG_WM_TIMER(OnTimer)
				MSG_WM_ERASEBKGND(OnEraseBkGnd)
				MSG_WM_PAINT(OnPaint)
				MSG_WM_GETMINMAXINFO(OnGetMinMaxInfo)
				MSG_WM_SIZING(OnSizing)
				MSG_WM_EXITSIZEMOVE(OnExitSizeMove)
				MSG_WM_SIZE(OnSize)
				MSG_WM_KEYDOWN(OnKeyDown)
				MSG_WM_KEYUP(OnKeyUp)
				MSG_WM_LBUTTONDOWN(OnMouseDown)
				MSG_WM_RBUTTONDOWN(OnMouseDown)
				MSG_WM_MBUTTONDOWN(OnMouseDown)
				MSG_WM_LBUTTONUP(OnMouseUp)
				MSG_WM_RBUTTONUP(OnMouseUp)
				MSG_WM_MBUTTONUP(OnMouseUp)
				MSG_WM_MOUSEMOVE(OnMouseMove)
				MSG_WM_MOUSEWHEEL(OnMouseWheel)
				MSG_WM_DROPFILES(OnDropFiles)
				CHAIN_MSG_MAP(CFrameWindowImpl<DerivedGUI>)
			END_MSG_MAP()

			enum { IDR_MAINFRAME = 100, IDC_STATUSBAR = 100 };
			DECLARE_FRAME_WND_CLASS(_T("PR_APP_MAIN_GUI"), IDR_MAINFRAME);
		};
	}
}
#endif
