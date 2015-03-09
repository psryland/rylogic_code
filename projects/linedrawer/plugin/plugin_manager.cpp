//***************************************************************************************************
// Plugin Manager
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#include "linedrawer/main/stdafx.h"
#include "linedrawer/utility/misc.h"
#include "linedrawer/plugin/plugin_manager.h"
#include "linedrawer/plugin/plugin.h"
#include "linedrawer/main/ldrexception.h"
#include "linedrawer/main/ldrevent.h"
#include "linedrawer/main/linedrawer.h"
#include "linedrawer/gui/linedrawergui.h"
#include "pr/linedrawer/ldr_plugin_interface.h"
#include "pr/linedrawer/ldr_object.h"

// Plugin functions implementation *****************************************************************

// Add objects to the store associated with a particular context id
LDR_EXPORT ldrapi::ObjectHandle ldrRegisterObject(ldrapi::PluginHandle handle, char const* object_description, char const* include_paths, pr::ldr::ContextId ctx_id, bool async)
{
	if (!handle) return 0;
	try { return handle->RegisterObject(object_description, include_paths, ctx_id, async); }
	catch (std::exception const& e)
	{
		pr::events::Send(ldr::Event_Error(pr::Fmt("Failed to create plugin object.\nReason: %s", e.what())));
		return 0;
	}
}

// Return a particular object from the store
LDR_EXPORT void ldrUnregisterObject(ldrapi::PluginHandle handle, ldrapi::ObjectHandle object)
{
	if (!handle) return;
	handle->UnregisterObject(object);
}

// Remove all objects belonging to a particular context id
LDR_EXPORT void ldrUnregisterAllObjects(ldrapi::PluginHandle handle)
{
	if (!handle) return;
	handle->UnregisterAllObjects();
}

// Cause a refresh of the view
LDR_EXPORT void ldrRender(ldrapi::PluginHandle)
{
	pr::events::Send(ldr::Event_Refresh());
}

// Return the window handle for the ldr main window
LDR_EXPORT HWND ldrMainWindowHandle(ldrapi::PluginHandle handle)
{
	if (!handle) return 0;
	return handle->m_ldr->m_gui;
}

// Report an error via the ldr error reporting system
LDR_EXPORT void ldrError(ldrapi::PluginHandle, char const* err_msg)
{
	pr::events::Send(ldr::Event_Error(err_msg));
}

// Update text on the status bar
LDR_EXPORT void ldrStatus(ldrapi::PluginHandle, char const* msg, bool bold, int priority, DWORD min_display_time_ms)
{
	pr::events::Send(ldr::Event_Status(msg, bold, priority, min_display_time_ms));
}

// Turn on/off mouse status updates
LDR_EXPORT void ldrMouseStatusUpdates(ldrapi::PluginHandle handle, bool enable)
{
	handle->m_ldr->m_gui.m_mouse_status_updates = enable;
}

// Get/Set the object to world transform for an object
LDR_EXPORT pr::m4x4 ldrObjectO2W(ldrapi::ObjectHandle object)
{
	return object->m_o2p;
}
LDR_EXPORT void ldrObjectSetO2W(ldrapi::ObjectHandle object, pr::m4x4 const& o2w)
{
	object->m_o2p = o2w;
}

// Get/Set whether an object is visible
LDR_EXPORT bool ldrObjectVisible(ldrapi::ObjectHandle object)
{
	return object->m_visible;
}
LDR_EXPORT void ldrObjectSetVisible(ldrapi::ObjectHandle object, bool visible, char const* name)
{
	object->Visible(visible, name);
}

// Get/Set object wireframe mode
LDR_EXPORT bool ldrObjectWireframe(ldrapi::ObjectHandle object)
{
	return object->m_wireframe;
}
LDR_EXPORT void ldrObjectSetWireframe(ldrapi::ObjectHandle object, bool wireframe, char const* name)
{
	object->Wireframe(wireframe, name);
}

namespace ldr
{
	// Plugin manager *****************************************************************

	PluginManager::PluginManager(ldr::Main* ldr)
		:m_plugins()
		,m_ldr(ldr)
		,m_last_poll(0)
	{}
	PluginManager::~PluginManager()
	{
		for (auto pi : m_plugins)
			delete pi;
	}

	// Poll stepable plugins
	void PluginManager::Poll(double elapsed_s)
	{
		if (elapsed_s > 0.0 && elapsed_s < 1.0)
		{
			for (auto pi : m_plugins)
				pi->Poll(elapsed_s);
		}
	}

	// Load a plugin and add it to the collection
	// Returns a pointer to the plugin instance if started up correctly
	Plugin* PluginManager::Add(char const* filepath, char const* args)
	{
		Plugin* plugin = new Plugin(m_ldr, filepath, args);
		m_plugins.push_back(plugin);
		plugin->Start();
		return plugin;
	}

	// Shutdown and unload a plugin
	void PluginManager::Remove(Plugin* plugin)
	{
		PluginCont::iterator iter = std::find(m_plugins.begin(), m_plugins.end(), plugin);
		if (iter != m_plugins.end()) m_plugins.erase(iter);
		delete plugin;
	}

	// Access to the plugins
	Plugin const* PluginManager::First(Iter& iter) const
	{
		iter = m_plugins.begin();
		return Next(iter);
	}
	Plugin const* PluginManager::Next(Iter& iter) const
	{
		return iter != m_plugins.end() ? *iter++ : 0;
	}
}