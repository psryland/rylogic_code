//****************************************************************
// Example plugin for LineDrawer
//  Copyright © Rylogic Ltd 2010
//****************************************************************
	
#include "pr/common/min_max_fix.h"
#include "pr/common/fmt.h"
#include "pr/linedrawer/ldr_plugin_interface.h"
//#include "pr/linedrawer/ldr_object.h" // including this allows LdrObject members to be used directly, but that's dodgy
	
class Main
{
	// The handle for with this plugin instance.
	// Needed for calls to LineDrawer
	ldrapi::PluginHandle m_handle;
	
	// A handle to a registered ldr object
	ldrapi::Object m_ldr;
	
	// Running timer
	double m_clock;
	
public:
	Main(ldrapi::PluginHandle handle, char const*)
	:m_handle(handle)
	,m_ldr()
	,m_clock(0.0)
	{
		// Use the main window handle
		::SetWindowTextA(ldrapi::MainWindowHandle(m_handle), "Example Plugin Running");
		m_ldr = ldrapi::RegisterObject(m_handle, "*box ldrpi {1 *o2w{*randpos {0 0 0 1}}}", 0, false);
	}
	~Main()
	{
		// Could Unregister 'm_ldr' but it should be cleaned up automatically when the plugin is unloaded
	}
	void Step(double elapsed_s)
	{
		m_clock += elapsed_s;

		// Can use the LdrObject type directly
		m_ldr.O2W(pr::Rotation4x4((float)sin(m_clock), (float)cos(m_clock), (float)sin(m_clock), pr::v4Origin));
		
		// Cause a refresh
		ldrapi::Render(m_handle);

		// Update status
		ldrapi::Status(m_handle, pr::FmtS("Plugin Clock: %f", m_clock), true, 1, 100);
	}
};
	
namespace { Main* g_main = 0; }
LDR_IMPORT void ldrInitialise(ldrapi::PluginHandle handle, char const* args) { ldrapi::InitAPI(); g_main = new Main(handle, args); }
LDR_IMPORT void ldrUninitialise()                                            { delete g_main; g_main = 0; }
LDR_IMPORT void ldrStep(double elapsed_s)                                    { g_main->Step(elapsed_s); }

