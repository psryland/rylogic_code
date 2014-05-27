//***************************************************************************************************
// Plugin manager
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#pragma once
#ifndef PR_LDR_PLUGIN_MANAGER_H
#define PR_LDR_PLUGIN_MANAGER_H

#include "linedrawer/main/forward.h"
#include "linedrawer/resources/linedrawer.res.h"
#include "pr/linedrawer/ldr_plugin_interface.h"

namespace ldr
{
	class PluginManager
	{
		typedef std::vector<Plugin*> PluginCont;
		PluginCont  m_plugins;
		Main* m_ldr;
		pr::uint64  m_last_poll;

		PluginManager(PluginManager const&);
		void operator =(PluginManager const&);
	public:
		explicit PluginManager(Main* ldr);
		~PluginManager();

		// Poll stepable plugins
		void Poll(double elapsed_s);

		// Load a plugin and add it to the collection
		// Returns a pointer to the plugin instance if started up correctly
		Plugin* Add(char const* filepath, char const* args);

		// Shutdown and unload a plugin
		void Remove(Plugin* plugin);

		// Access to the plugins
		typedef PluginCont::const_iterator Iter;
		Plugin const* First(Iter& iter) const;
		Plugin const* Next(Iter& iter) const;
	};
}

#endif
