// PhysicsTestbed.cpp : Defines the initialization routines for the DLL.
//

#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/PhysicsTestbed.h"
#include "PhysicsTestbed/Parser.h"
#include "PhysicsTestbed/ParseOutput.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// CPhysicsTestbedApp
BEGIN_MESSAGE_MAP(CPhysicsTestbedApp, CWinApp)
END_MESSAGE_MAP()
CPhysicsTestbedApp theApp;

PhysicsTestbed& Testbed()	{ static PhysicsTestbed s_testbed; return s_testbed; }

// PhysicsTestbed construction
PhysicsTestbed::PhysicsTestbed()
:m_state()
,m_controls()
,m_physics_engine()
,m_scene_manager(&m_physics_engine)
,m_step_return(pr::ldr::EPlugInResult_Continue)
{}

// Return the settings to initialise the plugin with
pr::ldr::PlugInSettings	PhysicsTestbed::InitialisePlugin(pr::ldr::TArgs const& args)
{
	m_controls.Create(IDD_DIALOG_CONTROLS);
	m_controls.ShowWindow(SW_SHOW);

	const char ldr_src[] = "*Camera { *AlignY }";
	ldrSource(ldr_src, sizeof(ldr_src), false, false); 
	ldrSetLDWindowText("Physics Testbed");

	std::string src_file;
	switch( args.size() )
	{
	case 0:
		#if PHYSICS_ENGINE==RYLOGIC_PHYSICS
		src_file = "Q:/Paul/PhysicsTestbed/Scenes/Pauls.pr_script";
		#elif PHYSICS_ENGINE==REFLECTIONS_PHYSICS
		src_file = "Q:/Paul/PhysicsTestbed/Scenes/Reflections.pr_script";
		#endif

		//src_file = "Demo.pr_script";
		//src_file = "Q:/Paul/PhysicsTestbed/Scenes/Physics.pr_script";
		//src_file = "Q:/Paul/PhysicsTestbed/Scenes/Demo.pr_script";
		//src_file = "Q:/Paul/PhysicsTestbed/Scenes/PaulsPhysTest.pr_script";
		//src_file = "Q:/Paul/PhysicsTestbed/Scenes/DeformTest.pr_script";
		//src_file = "Q:/Paul/PhysicsTestbed/Scenes/BrickWall.pr_script";
		//src_file = "Q:/Paul/PhysicsTestbed/Scenes/MultibodyTest2.pr_script";
		break;
	case 2:
		if( pr::str::EqualNoCase(args[0], "-load") )
		{
			src_file = args[1];
			break;
		}// fall through
	default:
		ldrErrorReport("Invalid command line arguments");
	}
	LoadSourceFile(src_file.c_str());
	ldrViewAll();

	pr::ldr::PlugInSettings settings = pr::ldr::DefaultPlugInSettings;
	settings.m_step_rate_hz = 50;
	return settings;
}

// Step the testbed
pr::ldr::EPlugInResult PhysicsTestbed::Step()
{
	if( m_step_return != pr::ldr::EPlugInResult_Terminate )
	{
		m_controls.SetObjectCount(m_physics_engine.GetNumObjects());
		m_controls.SetFrameNumber(m_physics_engine.GetFrameNumber());
		m_physics_engine.SetTimeStep(m_controls.GetStepSize());
		if( m_controls.StartFrame() ) do
		{
			m_scene_manager.PrePhysicsStep();
	
			pr::uint64 s = pr::ReadRTC();
			m_physics_engine.Step();
			pr::uint64 e = pr::ReadRTC();
			if( pr::int64(e - s) > 0 ) m_controls.SetFrameRate(pr::uint64(e - s) * 1000.0f / pr::ReadCPUFreq());

			m_scene_manager.Step(m_controls.GetStepSize());
			m_controls.SetObjectCount(m_physics_engine.GetNumObjects());
			m_controls.SetFrameNumber(m_physics_engine.GetFrameNumber());
		}
		while( m_controls.AdvanceFrame() );
		m_controls.EndFrame();
		m_scene_manager.UpdateTransients();
		ldrRender();
	}
	return m_step_return;
}

// Close the plugin
void PhysicsTestbed::Shutdown()
{
	Clear();
	m_controls.DestroyWindow();
	m_step_return = pr::ldr::EPlugInResult_Terminate;
}

// Clear all data
void PhysicsTestbed::Clear()
{
	m_scene_manager.Clear();
	m_physics_engine.Clear();
	m_controls.Clear();
	PR_ASSERT(PR_DBG_COMMON, ldrGetNumPluginObjects() == 0);
}

// Load the previous source file again
void PhysicsTestbed::Reload()
{
	Clear();
	LoadSourceFile(m_source_file.c_str());
}

// Load a source physics scene from file
void PhysicsTestbed::LoadSourceFile(const char* filename)
{
	m_source_file = pr::filesys::GetFullPath<std::string>(filename);
	if( m_source_file.empty() ) return;

	Parser parser;
	if( parser.Load(filename) )
	{
		// Pass the parser output to the scene manager for creating the
		// line drawer objects and physics engine objects
		m_scene_manager.AddToScene(parser.m_output);
	}
}

bool PhysicsTestbed::HookEnabled(EHookType type) const
{
	return m_hook_state[type].State();
}
void PhysicsTestbed::PushHookState(EHookType type, bool enabled)
{
	return m_hook_state[type].Push(enabled);
}
void PhysicsTestbed::PopHookState(EHookType type)
{
	return m_hook_state[type].Pop();
}
