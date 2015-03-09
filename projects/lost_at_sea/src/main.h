//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2015
//************************************

#pragma once

#include "lost_at_sea/src/forward.h"
#include "lost_at_sea/src/settings.h"
//#include "cam/cam.h"
//#include "world/terrain.h"
//#include "ship/ship.h"

namespace las
{
	// Main application logic container
	struct Main :pr::app::Main<Settings, MainGUI>
	{
	//	pr::Camera     m_cam;
	//	ICamPtr        m_cam_ctrl;
		pr::app::Skybox m_skybox;
	//	Ship           m_ship;
	//	Terrain        m_terrain;

		Main(MainGUI& gui);

		// Advance the game by one frame
		void Step(double elapsed_seconds);

	//	// Draw the scene
	//	void Render();
	//	
	//	// The size of the window has changed
	//	void Resize(pr::IRect const& client_area);
	};

	// Main app window
	struct MainGUI :pr::app::MainGUI<MainGUI, Main, pr::SimMsgLoop>
	{
		MainGUI(LPTSTR lpstrCmdLine, int nCmdShow);
		static char const* AppName() { return "Lost At Sea"; }

		//BOOL    OnIdle(int);
		//LRESULT OnCreate(LPCREATESTRUCT);
		//void    OnDestroy();
		//BOOL    PreTranslateMessage(MSG*);
		//void    OnSysCommand(UINT code, WTL::CPoint const& point);
		//void    OnCommand(UINT wNotifyCode, INT wID, HWND hWndCtl);
		//LRESULT OnEraseBkGnd(HDC hdc);
	//	void    OnPaint(CDCHandle dc);
		//void    OnSizing(UINT side, LPRECT rect);
		//void    OnExitSizeMove();
		//void    OnSize(UINT type, CSize size);
		//void    OnMouseDown(UINT flags, CPoint point);
		//void    OnMouseUp(UINT flags, CPoint point);
		//void    OnMouseMove(UINT flags, CPoint point);
		//BOOL    OnMouseWheel(UINT flags, short delta, CPoint pt);
	
		//// Recent files callbacks
		//void MenuList_OnClick(pr::gui::MenuList* sender, pr::gui::MenuList::Item const& item);
		//void MenuList_ListChanged(pr::gui::MenuList* sender);
	
		//// Update the status text
		//void Status(char const* msg, bool bold);
	
		// Shutdown the app
		//void CloseApp(int exit_code);
	
		//// Event handlers
		//void OnEvent(Event_MediaSet const& e);
		//void OnEvent(Event_Message const& e);
	
		//enum { IDR_MAINFRAME = 100, IDC_STATUSBAR = 100 };
		//DECLARE_FRAME_WND_CLASS(_T("LostAtSeaWinClass"), IDR_MAINFRAME);
		//BEGIN_MSG_MAP(MainGUI)
		//	MSG_WM_CREATE(OnCreate)
		//	MSG_WM_DESTROY(OnDestroy)
		//	MSG_WM_SYSCOMMAND(OnSysCommand)
		//	MSG_WM_COMMAND(OnCommand)
		//	MSG_WM_ERASEBKGND(OnEraseBkGnd)
		//	MSG_WM_PAINT(OnPaint)
		//	MSG_WM_SIZING(OnSizing)
		//	MSG_WM_EXITSIZEMOVE(OnExitSizeMove)
		//	MSG_WM_SIZE(OnSize)
		//	MSG_WM_LBUTTONDOWN(OnMouseDown)
		//	MSG_WM_RBUTTONDOWN(OnMouseDown)
		//	MSG_WM_LBUTTONUP(OnMouseUp)
		//	MSG_WM_RBUTTONUP(OnMouseUp)
		//	MSG_WM_MOUSEMOVE(OnMouseMove)
		//	MSG_WM_MOUSEWHEEL(OnMouseWheel)
		//	CHAIN_MSG_MAP(CFrameWindowImpl<MainGUI>)
		//	CHAIN_MSG_MAP_MEMBER(m_recent)
		//END_MSG_MAP()
	};
}
