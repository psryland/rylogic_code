//*****************************************************************************************
// Sol
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#ifndef PR_SOL_MAIN_GUI_H
#define PR_SOL_MAIN_GUI_H

#include "sol/main/forward.h"

namespace sol
{
	// Main app window
	struct MainGUI
		:WTL::CFrameWindowImpl<MainGUI>
		,WTL::CMessageFilter
		,WTL::CIdleHandler
	{
		sol::Main*            m_main;
		DWORD                 m_my_thread_id;  // The thread this gui object was created on
		bool                  m_nav_enabled;
		bool                  m_resizing;
		
		MainGUI();
	
		LRESULT OnCreate(LPCREATESTRUCT);
		void    OnDestroy();
		BOOL    OnIdle();
		BOOL    PreTranslateMessage(MSG*);
		void    OnSysCommand(UINT code, WTL::CPoint const& point);
		void    OnCommand(UINT wNotifyCode, INT wID, HWND hWndCtl);
		LRESULT OnEraseBkGnd(HDC hdc);
		void    OnPaint(HDC hdc);
		void    OnSizing(UINT side, LPRECT rect);
		void    OnExitSizeMove();
		void    OnSize(UINT type, CSize size);
		void    OnMouseDown(UINT flags, CPoint point);
		void    OnMouseUp(UINT flags, CPoint point);
		void    OnMouseMove(UINT flags, CPoint point);
		BOOL    OnMouseWheel(UINT flags, short delta, CPoint pt);
	
		// Recent files callbacks
		void MenuList_OnClick(pr::gui::MenuList* sender, pr::gui::MenuList::Item const& item);
		void MenuList_ListChanged(pr::gui::MenuList* sender);
	
		// Update the status text
		void Status(char const* msg, bool bold);
	
		// Shutdown the app
		void CloseApp(int exit_code);
	
		enum { IDR_MAINFRAME = 100, IDC_STATUSBAR = 100 };
		DECLARE_FRAME_WND_CLASS(_T("Sol GUI"), IDR_MAINFRAME);
		BEGIN_MSG_MAP(MainGUI)
			MSG_WM_CREATE(OnCreate)
			MSG_WM_DESTROY(OnDestroy)
			MSG_WM_SYSCOMMAND(OnSysCommand)
			MSG_WM_COMMAND(OnCommand)
			MSG_WM_ERASEBKGND(OnEraseBkGnd)
			MSG_WM_PAINT(OnPaint)
			MSG_WM_SIZING(OnSizing)
			MSG_WM_EXITSIZEMOVE(OnExitSizeMove)
			MSG_WM_SIZE(OnSize)
			MSG_WM_LBUTTONDOWN(OnMouseDown)
			MSG_WM_RBUTTONDOWN(OnMouseDown)
			MSG_WM_LBUTTONUP(OnMouseUp)
			MSG_WM_RBUTTONUP(OnMouseUp)
			MSG_WM_MOUSEMOVE(OnMouseMove)
			MSG_WM_MOUSEWHEEL(OnMouseWheel)
			CHAIN_MSG_MAP(CFrameWindowImpl<MainGUI>)
		END_MSG_MAP()
	};
}
#endif
