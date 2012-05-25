//***************************************************************************
//
//	A LineDrawer plugin for testing the physics library
//
//***************************************************************************

#include "Stdafx.h"
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

//*****
// PhysicsLab construction
PhysicsLab::PhysicsLab()
:m_scene_parser()
,m_simulation()
,m_show_velocity(false)
,m_show_ang_velocity(false)
,m_show_ang_momentum(false)
,m_run_state(eIdle)
,m_source_filename("")
,m_physics_step_size(0.05f)
{}

//*****
// PhysicsLab construction
PhysicsLab::~PhysicsLab()
{}

//*****
// Initialise the physics lab plugin.
PlugInSettings PhysicsLab::Initialise()
{
	m_run_state = eIdle;

	m_scene_parser.Initialise();
	m_simulation.Initialise();

	m_tool_bar.Create(IDD_DIALOG_ControlPanel);
	m_tool_bar.ShowWindow(SW_SHOW);

	RefreshWindowText();
	return DefaultPlugInSettings;
}

//*****
// Step the plugin
EPlugInResult PhysicsLab::Step()
{
	if( m_run_state == eRunning )
	{
		m_simulation.Step(m_physics_step_size);
	}
	
	ldrRender();
	return (m_run_state == eExit) ? (eTerminate) : (eContinue);
}

//*****
// Uninitlialise the plugin
void PhysicsLab::UnInitialise()
{
	m_scene_parser.UnInitialise();
	m_simulation.UnInitialise();
	m_run_state = eIdle;
}

//*****
// Redraw the display
void PhysicsLab::Refresh()
{
	m_simulation.UpdateObjectTransforms();
	ldrRender();
}

//*****
// Set the line drawer window text
void PhysicsLab::RefreshWindowText()
{
	std::string window_text = "PhysicsLab";
	switch( m_run_state )
	{
	case eIdle:			window_text += " (Idle)";	break;
	case eSceneLoaded:	window_text += " (Ready)";	break;
	case eRunning:		window_text += " (Run)";	break;
	case eExit:			window_text += " (Exit)";	break;
	default: PR_ERROR(PR_DBG_PHLAB);
	};
	window_text += Fmt(" %3.3f", m_simulation.GetSimulationTime());
	if( m_source_filename.length() > 0 ) { window_text += " - "; window_text += m_source_filename; }
	
	ldrSetLDWindowText(window_text.c_str());
}

//*****
// Load and parse a file containing a physics scene
void PhysicsLab::LoadFile(const char* filename)
{
	m_run_state = eIdle;
	m_source_filename = filename;
	ResetSim();
}

//*****
// Reload the physics scene
void PhysicsLab::ResetSim()
{
	if( m_source_filename.length() > 0 )
	{
		if( !m_scene_parser.Parse(m_source_filename.c_str()) )
		{
			AfxMessageBox(Fmt("Failed to load Physics Scene: %s", m_source_filename.c_str()).c_str(), MB_OK | MB_ICONEXCLAMATION);
			m_simulation.UnInitialise();
			m_run_state = eIdle;
			RefreshWindowText();
			return;
		}

		m_simulation.Reset();
		m_run_state = eSceneLoaded;
		RefreshWindowText();
	}
}

//*****
// Start the simulation
void PhysicsLab::StartSim()
{
	if( m_run_state == eSceneLoaded )
	{
		m_run_state = eRunning;
		RefreshWindowText();
	}
}

//*****
// Step one frame of the simulation
void PhysicsLab::StepSim()
{
	if( m_run_state == eSceneLoaded )
	{
		m_simulation.Step(m_physics_step_size);
	}
}

//*****
// Pause the simulation
void PhysicsLab::PauseSim()
{
	if( m_run_state == eRunning )
	{
		m_run_state = eSceneLoaded;
		RefreshWindowText();
	}
}





	