//***************************************************************************
//
//	A LineDrawer plugin for testing the physics library
//
//***************************************************************************

#include "Stdafx.h"
#include "PhysicsLab_LDPI/PhysicsLab_LDPI.h"
#include "PhysicsLab_LDPI/PhysicsLab.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

//
//	Note!
//
//		If this DLL is dynamically linked against the MFC
//		DLLs, any functions exported from this DLL which
//		call into MFC must have the AFX_MANAGE_STATE macro
//		added at the very beginning of the function.
//
//		For example:
//
//		extern "C" BOOL PASCAL EXPORT ExportedFunction()
//		{
//			AFX_MANAGE_STATE(AfxGetStaticModuleState());
//			// normal function body here
//		}
//
//		It is very important that this macro appear in each
//		function, prior to any calls into MFC.  This means that
//		it must appear as the first statement within the 
//		function, even before any object variable declarations
//		as their constructors may generate calls into the MFC
//		DLL.
//
//		Please see MFC Technical Notes 33 and 58 for additional
//		details.
//

// The one and only CPhysicsLab_LDPIApp object
CPhysicsLab_LDPIApp	g_PhysicsLab;

// CPhysicsLab_LDPIApp
BEGIN_MESSAGE_MAP(CPhysicsLab_LDPIApp, CWinApp)
END_MESSAGE_MAP()

//*****
// CPhysicsLab_LDPIApp initialization
BOOL CPhysicsLab_LDPIApp::InitInstance()
{
	CWinApp::InitInstance();
	PhysicsLab::Get();
	return TRUE;
}

//*****
// CPhysicsLab_LDPIApp uninitialization
int CPhysicsLab_LDPIApp::ExitInstance()
{
	PhysicsLab::Delete();
	return 0;
}
	