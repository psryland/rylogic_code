//***************************************************************************
//
//	A LineDrawer plugin for testing the physics library
//
//***************************************************************************

#ifndef PHYSICS_LAB_H
#define PHYSICS_LAB_H

#include "PR/Common/StdString.h"
#include "PR/Common/Singleton.h"
#include "LineDrawer/PlugInInterface.h"
#include "PhysicsLab_LDPI/Resource.h"
#include "PhysicsLab_LDPI/PhysicsLabToolBar.h"
#include "PhysicsLab_LDPI/SceneParser.h"
#include "PhysicsLab_LDPI/Simulation.h"

class PhysicsLab : public Singleton<PhysicsLab>
{
public:
	PhysicsLab();
	~PhysicsLab();

	PlugInSettings		Initialise();
	EPlugInResult		Step();
	void				UnInitialise();
	
	void				Refresh();
	void				RefreshWindowText();
	void				LoadFile(const char* filename);
	void				ResetSim();
	void				StartSim();
	void				StepSim();
	void				PauseSim();

	SceneParser			m_scene_parser;
	Simulation			m_simulation;
	bool				m_show_velocity;
	bool				m_show_ang_velocity;
	bool				m_show_ang_momentum;

private:
	enum State { eIdle, eSceneLoaded, eRunning, eExit };
	State				m_run_state;
	std::string			m_source_filename;
	CPhysicsLabToolBar	m_tool_bar;
	float				m_physics_step_size;
};

// Plug in functions
extern "C"
{
	inline PlugInSettings	Initialise()							{ AFX_MANAGE_STATE(AfxGetStaticModuleState()); return PhysicsLab::Get().Initialise(); }
	inline EPlugInResult	StepPlugIn()							{ AFX_MANAGE_STATE(AfxGetStaticModuleState()); return PhysicsLab::Get().Step(); }
	inline void				UnInitialise()							{ AFX_MANAGE_STATE(AfxGetStaticModuleState()); return PhysicsLab::Get().UnInitialise(); }
};	// extern "C"

#endif//PHYSICS_LAB_H
