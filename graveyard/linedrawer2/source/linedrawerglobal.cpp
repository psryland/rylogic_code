//*******************************************************************************************
//
// The global app for line Drawer
//
//*******************************************************************************************
#include "Stdafx.h"
#include "LineDrawer/Source/LineDrawerGlobal.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/GUI/LineDrawerGUI.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

//*****
// GLineDrawer message map
BEGIN_MESSAGE_MAP(LineDrawerGlobal, CWinApp)
	ON_COMMAND(ID_HELP, CWinApp::OnHelp)
END_MESSAGE_MAP()

//*****
// The one and only LineDrawerGlobal object
LineDrawerGlobal LineDrawerApp;

//*****
// App Initialisation
BOOL LineDrawerGlobal::InitInstance()
{
	//_CrtSetBreakAlloc(135);

	// InitCommonControls() is required on Windows XP if an application
	// manifest specifies use of ComCtl32.dll version 6 or later to enable
	// visual styles.  Otherwise, any window creation will fail.
	InitCommonControls();

	// Initialise AFX stuff
	//AfxEnableControlContainer();
	//if( !AfxSocketInit() ) { AfxMessageBox(IDP_SOCKETS_INIT_FAILED); return FALSE; }

	CWinApp::InitInstance();

	try
	{
		LineDrawer::Get().DoModal();
	}
	catch(const pr::Exception&)
	{}
	LineDrawer::Delete();

	// Since the dialog has been closed, return FALSE so that we exit the
	// application, rather than start the application's message pump.
	return FALSE;
}
