// NuggetView.cpp : Defines the class behaviors for the application.
//

#include "stdafx.h"
#include "NuggetView.h"
#include "NuggetViewDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// The one and only NuggetViewApp object
NuggetViewApp theApp;

// NuggetViewApp
BEGIN_MESSAGE_MAP(NuggetViewApp, CWinApp)
	ON_COMMAND(ID_HELP, CWinApp::OnHelp)
END_MESSAGE_MAP()

// NuggetViewApp initialization
BOOL NuggetViewApp::InitInstance()
{
	// InitCommonControls() is required on Windows XP if an application
	// manifest specifies use of ComCtl32.dll version 6 or later to enable
	// visual styles.  Otherwise, any window creation will fail.
	InitCommonControls();

	// Initialise AFX stuff
	AfxEnableControlContainer();
	CWinApp::InitInstance();

	NuggetViewDlg dlg;
	m_pMainWnd = &dlg;
	dlg.DoModal();
	return FALSE;
}
