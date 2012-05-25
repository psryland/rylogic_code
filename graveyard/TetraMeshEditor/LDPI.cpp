//****************************************************************
// LineDrawer plugin interface
//****************************************************************
#include "Stdafx.h"
#include "LineDrawer/Plugin/PluginInterface.h"
#include "TetraMeshEditor.h"

extern "C"
{
	// Create the physics testbed dll
	pr::ldr::PlugInSettings ldrInitialise(pr::ldr::TArgs const&)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		Editor();//.InitInstance();
		return Editor().InitialisePlugin();
	}

	// "Main" function for the plugin. Return eContinue to be stepped again or eTerminate to end
	pr::ldr::EPlugInResult ldrStepPlugIn()
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		return Editor().Step();
	}

	// Uninitialise the plugin
	void ldrUnInitialise()
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		Editor();//.ExitInstance();
		//delete &Editor();
	}

	// Optional functions ********
	
	pr::ldr::EPlugInResult ldrNotifyKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		return Editor().HandleKeys(nChar, nRepCnt, nFlags, true);
	}
	pr::ldr::EPlugInResult ldrNotifyKeyUp(UINT nChar, UINT nRepCnt, UINT nFlags)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		return Editor().HandleKeys(nChar, nRepCnt, nFlags, false);
	}
	pr::ldr::EPlugInResult 	ldrNotifyMouseClk(unsigned int button, pr::v2 position)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		return Editor().MouseClk(button, position);
	}
	pr::ldr::EPlugInResult 	ldrNotifyMouseDblClk(unsigned int button, pr::v2 position)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		return Editor().MouseDblClk(button, position);
	}
	pr::ldr::EPlugInResult 	ldrNotifyMouseDown(unsigned int button, pr::v2 position)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		return Editor().MouseDown(button, position);
	}
	pr::ldr::EPlugInResult 	ldrNotifyMouseMove(pr::v2 position)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		return Editor().MouseMove(position);
	}
	pr::ldr::EPlugInResult 	ldrNotifyMouseUp(unsigned int button, pr::v2 position)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		return Editor().MouseUp(button, position);
	}
};	// extern "C"
