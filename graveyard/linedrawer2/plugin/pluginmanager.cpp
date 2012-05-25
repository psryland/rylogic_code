//*********************************************
// Plugin Manager
//	(C)opyright Rylogic Limited 2007
//*********************************************
#include "Stdafx.h"
#include "pr/linedrawer/customobjectdata.h"
#include "LineDrawer/PlugIn/PlugInManager.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/GUI/LineDrawerGUI.h"

//using namespace ldr;

PlugInManager* g_PlugInManager = 0;

PollingToEventSettings PlugInManager::PluginPollerSettings(void* user_data)
{
	PollingToEventSettings settings;
	settings.m_polling_function		= PlugInManager::PollPlugIn;
	settings.m_polling_frequency	= 1000 / 60;
	settings.m_event_function		= 0;
	settings.m_user_data			= user_data;
	return settings;
}

PlugInManager::PlugInManager(LineDrawer& linedrawer)
:m_linedrawer(&linedrawer)
,m_plugin_poller(PluginPollerSettings(this))
,m_step_plugin_pending(false)
,m_plugin(0)
,m_PlugInInitialise(0)
,m_PlugInStepPlugIn(0)
,m_PlugInUnInitialise(0)
,m_NotifyKeyDown(0)
,m_NotifyKeyUp(0)
,m_NotifyOnMouseDown(0)
,m_NotifyOnMouseMove(0)
,m_NotifyOnMouseWheel(0)
,m_NotifyOnMouseUp(0)
,m_NotifyOnMouseClk(0)
,m_NotifyOnMouseDblClk(0)
,m_NotifyDeleteObject(0)
,m_NotifyRefresh(0)
{
	g_PlugInManager = this;
}

PlugInManager::~PlugInManager()
{
	StopPlugIn();
}

//*****
// Remove all of the plugin data objects
void PlugInManager::Clear()
{
	uint delete_start_time = (uint)GetTickCount();
	uint i = 0, num_objects = (uint)m_plugin_objects.size();
	for( TObjectSet::iterator obj = m_plugin_objects.begin(); obj != m_plugin_objects.end(); obj = m_plugin_objects.begin(), ++i )
	{
		LineDrawer::Get().SetProgress(i + 1, num_objects, Fmt("Clearing plugin data: %s", (*obj)->m_name.c_str()).c_str(), (uint)GetTickCount() - delete_start_time);
		LineDrawer::Get().m_data_manager.DeleteObject(*obj);
	}
	LineDrawer::Get().SetProgress(0, 0, "");
	m_plugin_objects.clear();
}

//*****
// Start the plugin
bool PlugInManager::StartPlugIn(const char* plugin_name, pr::cmdline::TArgs const& args)
{
	m_plugin_name = plugin_name;
	m_plugin_args = args;
	return RestartPlugIn();
}
	
//*****
// Load the plugin dll and start it running
bool PlugInManager::RestartPlugIn()
{
	PR_ASSERT(PR_DBG_LDR, m_plugin == 0);
	PR_ASSERT(PR_DBG_LDR, !m_plugin_name.empty());

	// Load the dll
	m_plugin = LoadLibrary(m_plugin_name.c_str());
	if( !m_plugin )
	{
		LineDrawer::Get().m_error_output.Error(Fmt("Failed to load plugin: %s\nLoadLibrary failed.", m_plugin_name.c_str()).c_str());
		StopPlugIn();
		return false;
	}

	// Setup the function pointers
	m_PlugInInitialise		= (PlugInInitialise)	GetProcAddress(m_plugin, "ldrInitialise");
	m_PlugInStepPlugIn		= (PlugInStepPlugIn)	GetProcAddress(m_plugin, "ldrStepPlugIn");
	m_PlugInUnInitialise	= (PlugInUnInitialise)	GetProcAddress(m_plugin, "ldrUnInitialise");
	if( m_PlugInInitialise == 0 || m_PlugInStepPlugIn == 0 || m_PlugInUnInitialise == 0 )
	{
		LineDrawer::Get().m_error_output.Error(Fmt("'%s' is not a valid LineDrawer plugin\n", m_plugin_name.c_str()).c_str());
		StopPlugIn();
		return false;
	}

	// Load optional function pointers
	m_NotifyKeyDown			= (NotifyKeyDown)		GetProcAddress(m_plugin, "ldrNotifyKeyDown");
	m_NotifyKeyUp			= (NotifyKeyUp)			GetProcAddress(m_plugin, "ldrNotifyKeyUp");
	m_NotifyOnMouseDown		= (NotifyOnMouseDown)	GetProcAddress(m_plugin, "ldrNotifyMouseDown");
	m_NotifyOnMouseMove		= (NotifyOnMouseMove)	GetProcAddress(m_plugin, "ldrNotifyMouseMove");
	m_NotifyOnMouseWheel	= (NotifyOnMouseWheel)	GetProcAddress(m_plugin, "ldrNotifyMouseWheel");
	m_NotifyOnMouseUp		= (NotifyOnMouseUp)		GetProcAddress(m_plugin, "ldrNotifyMouseUp");
	m_NotifyOnMouseClk		= (NotifyOnMouseClk)	GetProcAddress(m_plugin, "ldrNotifyMouseClk");
	m_NotifyOnMouseDblClk	= (NotifyOnMouseDblClk)	GetProcAddress(m_plugin, "ldrNotifyMouseDblClk");
	m_NotifyDeleteObject	= (NotifyDeleteObject)	GetProcAddress(m_plugin, "ldrNotifyDeleteObject");
	m_NotifyRefresh			= (NotifyRefresh)		GetProcAddress(m_plugin, "ldrNotifyRefresh");

	// Initialise the plugin
	pr::ldr::PlugInSettings settings = m_PlugInInitialise(m_plugin_args);
	m_plugin_poller.SetFrequency(1000.0f / settings.m_step_rate_hz);

	// Start the plugin poller
	if( !m_plugin_poller.Start() )
	{
		LineDrawer::Get().m_error_output.Error("Plugin poller failed. Bad luck, sorry\n");
		StopPlugIn();
		return false;
	}

	if( LineDrawer::Get().m_line_drawer_GUI )
		LineDrawer::Get().m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_PlugInRunning, true);
	return true;
}

//*****
// Step the plug in
void PlugInManager::StepPlugIn()
{
	// Don't step unless the plugin has been initialised
	if( !IsPlugInLoaded() ) return;

	pr::ldr::EPlugInResult result = m_PlugInStepPlugIn();
	switch( result )
	{
	case pr::ldr::EPlugInResult_Continue:
		m_step_plugin_pending = false;
		break;
	case pr::ldr::EPlugInResult_Terminate:
		StopPlugIn();
		break;
	default:
		PR_ASSERT_STR(PR_DBG_LDR, false, "Unknown step plugin result");
	}
}

//*****
// Stop a plugin and unload the dll
void PlugInManager::StopPlugIn()
{
	// Stop the plugin poller
	m_plugin_poller.Stop();
	m_plugin_poller.BlockTillDead();

	// Destroy the plugin objects
	Clear();

	// Call UnInitialise
	if( m_PlugInUnInitialise ) m_PlugInUnInitialise();

	// Null the function pointers
	m_PlugInInitialise		= 0;
	m_PlugInStepPlugIn		= 0;
	m_PlugInUnInitialise	= 0;

	// Null optional function pointers
	m_NotifyKeyDown			= 0;
	m_NotifyKeyUp			= 0;
	m_NotifyOnMouseDown		= 0;
	m_NotifyOnMouseMove		= 0;
	m_NotifyOnMouseWheel	= 0;
	m_NotifyOnMouseUp		= 0;
	m_NotifyOnMouseClk		= 0;
	m_NotifyOnMouseDblClk	= 0;
	m_NotifyDeleteObject	= 0;
	m_NotifyRefresh			= 0;

	// Unload the dll
	if( m_plugin != 0 ) { FreeLibrary(m_plugin); m_plugin = 0; }

	if( LineDrawer::Get().m_line_drawer_GUI )
		LineDrawer::Get().m_line_drawer_GUI->UpdateMenuItemState(LineDrawerGUI::EMenuItemsWithState_PlugInRunning, false);
	LineDrawer::Get().Refresh();
	LineDrawer::Get().RefreshWindowText();
}

//*****
// This method is called when an object is deleted. If it's one of ours remove it from the set
void PlugInManager::DeleteObject(LdrObject* object)
{
	TObjectSet::iterator obj = m_plugin_objects.find(object);
	if( obj != m_plugin_objects.end() )
	{
		m_plugin_objects.erase(obj);
		Hook_OnDeleteObject(object);
	}
}
	
//*****
// Step the plugin
bool PlugInManager::PollPlugIn(void* user)
{
	PlugInManager* _this = static_cast<PlugInManager*>(user);
	if( !_this->m_step_plugin_pending )
	{
		_this->m_step_plugin_pending = true;
		PostMessage(LineDrawer::Get().m_window_handle, WM_COMMAND, ID_STEP_PLUGIN, 0);
	}
	//SendMessage(LineDrawer::Get().m_window_handle, WM_COMMAND, ID_STEP_PLUGIN, 0);
	return false;
}
