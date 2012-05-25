//****************************************************************
// LineDrawer plugin interface
//****************************************************************
#include "PhysicsTestbed/Stdafx.h"
//#include "pr/linedrawer/plugininterface.h"
#include "PhysicsTestbed/PhysicsTestbed.h"

extern "C"
{
	// Create the physics testbed dll
	pr::ldr::PlugInSettings ldrInitialise(pr::ldr::TArgs const& args)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		return Testbed().InitialisePlugin(args);
	}

	// "Main" function for the plugin. Return eContinue to be stepped again or eTerminate to end
	pr::ldr::EPlugInResult ldrStepPlugIn()
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		return Testbed().Step();
	}

	// Uninitialise the plugin
	void ldrUnInitialise()
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		Testbed().Shutdown();
	}

	// Optional functions ********
	
	// Key press
	pr::ldr::EPlugInResult ldrNotifyKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		return Testbed().m_controls.HandleKeys(nChar, nRepCnt, nFlags);
	}
	// Plugin object deleted
	void ldrNotifyDeleteObject(pr::ldr::ObjectHandle object)
	{
		AFX_MANAGE_STATE(AfxGetStaticModuleState());
		if( Testbed().HookEnabled(EHookType_DeleteObjects) )
			Testbed().m_scene_manager.DeleteObject(object);
	}

};	// extern "C"
