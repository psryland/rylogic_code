//***************************************************************************************************
// Plugin manager
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"

namespace ldr
{
	class PluginManager
	{
		using PluginCont = std::vector<Plugin*>;

		PluginCont m_plugins;
		Main*      m_ldr;
		pr::uint64 m_last_poll;

	public:

		~PluginManager();
		explicit PluginManager(Main* ldr);
		PluginManager(PluginManager const&) = delete;
		void operator =(PluginManager const&) = delete;

		// Poll step-able plugins
		void Poll(double elapsed_s);

		// Load a plugin and add it to the collection
		// Returns a pointer to the plugin instance if started up correctly
		Plugin* Add(wchar_t const* filepath, wchar_t const* args);

		// Shutdown and unload a plugin
		void Remove(Plugin* plugin);

		// Access to the plugins
		typedef PluginCont::const_iterator Iter;
		Plugin const* First(Iter& iter) const;
		Plugin const* Next(Iter& iter) const;
	};
}
