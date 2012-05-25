//***************************************
// Physics Testbed 
//***************************************
#pragma once
#include "PhysicsTestbed/Res/Resource.h"
#include "PhysicsTestbed/Forwards.h"
#include "pr/linedrawer/plugininterface.h"
#include "PhysicsTestbed/TestbedState.h"
#include "PhysicsTestbed/Controls.h"
#include "PhysicsTestbed/PhysicsEngine.h"
#include "PhysicsTestbed/SceneManager.h"
#include "PhysicsTestbed/Hooks.h"

// CPhysicsTestbedApp
struct CPhysicsTestbedApp : public CWinApp
{
	DECLARE_MESSAGE_MAP()
};

class PhysicsTestbed
{
public:
	PhysicsTestbed();
	pr::ldr::PlugInSettings	InitialisePlugin(pr::ldr::TArgs const& args);
	pr::ldr::EPlugInResult	Step();
	void					Shutdown();
	void					Clear();
	void					Reload();
	void					LoadSourceFile(const char* filename);
	void					ProfileOutput();
	bool					HookEnabled  (EHookType type) const;
	void					PushHookState(EHookType type, bool enabled);
	void					PopHookState (EHookType type);

public:
	TestbedState			m_state;
	CControls				m_controls;
	PhysicsEngine			m_physics_engine;
	SceneManager			m_scene_manager;

private:
	pr::ldr::EPlugInResult	m_step_return;
	std::string				m_source_file;
	HookState				m_hook_state[EHookType_NumberOf];
};

PhysicsTestbed& Testbed();
