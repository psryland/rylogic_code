//***************************************************************************************************
// Lighting Dialog
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"

namespace ldr
{
	class PluginManagerUI :public pr::gui::Form
	{
		enum
		{
			ID_LIST_PLUGINS = 100, ID_EDIT_PLUGIN_FILEPATH, ID_EDIT_PLUGIN_ARGS, ID_BUTTON_BROWSE_PLUGIN,
			ID__BUTTON_ADDPLUGIN, ID__BUTTON_REMOVEPLUGIN, ID__LBL_PLUGIN_DLL, ID__LBL_PLUGIN_ARGS,
		};

		pr::gui::ListView m_list_plugins;
		pr::gui::TextBox  m_tb_plugin_filepath;
		pr::gui::TextBox  m_tb_plugin_args;
		pr::gui::Button   m_btn_browse;
		pr::gui::Button   m_btn_add;
		pr::gui::Button   m_btn_remove;
		pr::gui::Button   m_btn_ok;
		pr::gui::Label    m_lbl_plugin_dll;
		pr::gui::Label    m_lbl_arguments;
		PluginManager*    m_plugin_mgr;

	public:

		PluginManagerUI(PluginManager& plugin_mgr, HWND parent);

		// Add a plugin to the list in the UI
		void AddPluginToList(Plugin const* plugin);

		// Remove the selected plugins from the list in the UI
		void RemovePluginsFromList();

		// Enable/Disable UI elements
		void UpdateUI();
	};
}
