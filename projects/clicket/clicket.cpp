// Clicket.cpp : main source file for Clicket.exe
//
#include "clicket/stdafx.h"
#include "clicket/resource.h"
#include "clicket/aboutdlg.h"
#include "clicket/maindlg.h"

CAppModule _Module;

int Run(LPSTR lpstrCmdLine = NULL, int nCmdShow = SW_SHOWDEFAULT)
{
	(void)lpstrCmdLine;
	CMessageLoop theLoop;
	_Module.AddMessageLoop(&theLoop);
	
	CMainDlg dlgMain;
	if (dlgMain.Create(NULL) == NULL)
	{
		ATLTRACE(_T("Main dialog creation failed!\n"));
		return 0;
	}
	
	dlgMain.ShowWindow(nCmdShow);
	int nRet = theLoop.Run();
	_Module.RemoveMessageLoop();
	return nRet;
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE /*hPrevInstance*/, LPSTR lpstrCmdLine, int nCmdShow)
{
	pr::InitCom com;
	
	// this resolves ATL window thunking problem when Microsoft Layer for Unicode (MSLU) is used
	::DefWindowProc(NULL, 0, 0, 0L);
	
	AtlInitCommonControls(ICC_BAR_CLASSES); // add flags to support other controls
	
	pr::Throw(_Module.Init(NULL, hInstance));
	int nRet = Run(lpstrCmdLine, nCmdShow);
	_Module.Term();
	return nRet;
}
