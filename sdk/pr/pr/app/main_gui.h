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
			typename Main
		>
		struct MainGUI
			:WTL::CFrameWindowImpl<DerivedGUI>
			,WTL::CMessageFilter
			,WTL::CIdleHandler
		{
			// Define this type as base as a helper for derived type constructors
			// so they can call: MyType(...) :base(..) {}
			typedef MainGUI<DerivedGUI, Main> base;
			
			Main*  m_main;          // The app logic object
			DWORD  m_my_thread_id;  // The thread this gui object was created on
			bool   m_resizing;      // True during a resize of the main window
			bool   m_nav_enabled;   // True to allow default mouse navigation
			
			MainGUI()
			:m_main(0)
			,m_my_thread_id(GetCurrentThreadId())
			,m_resizing(false)
			,m_nav_enabled(false)
			{}
			virtual ~MainGUI()
			{
				PR_ASSERT(PR_DBG, m_main == 0, "Destructing MainGUI before DestroyWindow has been called");
			}
			
			// Create/Destroy the main window
			virtual LRESULT OnCreate(LPCREATESTRUCT)
			{
				//// Set icons
				//SetIcon((HICON)::LoadImage(create->hInstance, "MainIcon", IMAGE_ICON, ::GetSystemMetrics(SM_CXICON),   ::GetSystemMetrics(SM_CYICON),   LR_DEFAULTCOLOR) ,TRUE);
				//SetIcon((HICON)::LoadImage(create->hInstance, "MainIcon", IMAGE_ICON, ::GetSystemMetrics(SM_CXSMICON), ::GetSystemMetrics(SM_CYSMICON), LR_DEFAULTCOLOR) ,FALSE);
	
				// Register this class for message filtering and idle updates
				CMessageLoop* loop = pr::app::Module().GetMessageLoop();
				loop->AddMessageFilter(this);
				loop->AddIdleHandler(this);
	
				// Create the main app logic
				m_main = new Main(static_cast<DerivedGUI&>(*this));
				SetWindowTextW(m_hWnd, m_main->AppTitle());
				OnTimerTick(0); // Initiate the render timer
				return S_OK;
			}
			virtual void OnDestroy()
			{
				delete m_main; m_main = 0;
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
			
			// Handle menu commands
			void OnSysCommand(UINT wparam, WTL::CPoint const&)
			{
				switch (wparam)
				{
				default: SetMsgHandled(FALSE); break;
				case SC_CLOSE: CloseApp(0); break;
				}
			}
			void OnCommand(UINT, INT wID, HWND)
			{
				switch (wID)
				{
				default: SetMsgHandled(FALSE); break;
				case IDCLOSE:
					CloseApp(0);
					break;
				}
			}
			
			// Rendering the window
			virtual LRESULT OnEraseBkGnd(HDC hdc)
			{
				(void)hdc;
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
				if (m_main/* && !m_resizing*/) m_main->Render();
				SetMsgHandled(FALSE);
			}
			virtual void OnTimerTick(UINT_PTR)
			{
				// If a refresh has been flagged, render now
				//if (m_refresh)
				m_main->DoRender();
				SetTimer(ID_MAIN_RENDER_TIMER, 1, 0);
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
				if (m_resizing) return;
				if (type != SIZE_MINIMIZED)
				{
					// Find the new client area
					pr::IRect area = pr::ClientArea(m_hWnd);
					if (m_hWndStatusBar) area.m_max.y -= pr::WindowBounds(m_hWndStatusBar).SizeY();
					
					//SaveWindowBounds();
					//UpdateUI();
					UpdateLayout(true);
					m_main->Resize(area.Size());
					m_main->Render();
				}
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
			
			// Message Map
			enum { IDR_MAINFRAME = 100, IDC_STATUSBAR = 100, ID_MAIN_RENDER_TIMER = 100 };
			DECLARE_FRAME_WND_CLASS(_T("PR_APP_MAIN_GUI"), IDR_MAINFRAME);
			BEGIN_MSG_MAP(MainGUI)
				MSG_WM_CREATE(OnCreate)
				MSG_WM_DESTROY(OnDestroy)
				MSG_WM_SYSCOMMAND(OnSysCommand)
				MSG_WM_COMMAND(OnCommand)
				MSG_WM_ERASEBKGND(OnEraseBkGnd)
				MSG_WM_PAINT(OnPaint)
				MSG_WM_TIMER(OnTimerTick)
				MSG_WM_GETMINMAXINFO(OnGetMinMaxInfo)
				MSG_WM_SIZING(OnSizing)
				MSG_WM_EXITSIZEMOVE(OnExitSizeMove)
				MSG_WM_SIZE(OnSize)
				MSG_WM_LBUTTONDOWN(OnMouseDown)
				MSG_WM_RBUTTONDOWN(OnMouseDown)
				MSG_WM_LBUTTONUP(OnMouseUp)
				MSG_WM_RBUTTONUP(OnMouseUp)
				MSG_WM_MOUSEMOVE(OnMouseMove)
				MSG_WM_MOUSEWHEEL(OnMouseWheel)
				CHAIN_MSG_MAP(CFrameWindowImpl<DerivedGUI>)
			END_MSG_MAP()
		};
	}
}
#endif
