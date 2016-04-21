//***************************************************************************************************
// Lighting Dialog
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#include "linedrawer/main/stdafx.h"
#include "linedrawer/plugin/plugin_manager_dlg.h"
#include "linedrawer/plugin/plugin_manager.h"
#include "linedrawer/plugin/plugin.h"
#include "linedrawer/main/ldrevent.h"

using namespace pr::gui;

namespace ldr
{
	enum class EColumn { Name, Filepath, };
	static COMDLG_FILTERSPEC const PluginFilterSpec[] = {{L"Ldr Plug-in (*.dll)", L"*.dll"}};

	PluginManagerUI::PluginManagerUI(PluginManager& plugin_mgr, HWND parent)
		:Form(MakeFormParams<>().parent(parent).title(L"Plug-in Manager").wh(317,213))

		,m_list_plugins      (ListView::Params<>().parent(this_).id(ID_LIST_PLUGINS        ).xy( 5,   7).wh(305, 148).report().no_hdr_sort())
		,m_tb_plugin_filepath(TextBox ::Params<>().parent(this_).id(ID_EDIT_PLUGIN_FILEPATH).xy(48, 160).wh(211, 14))
		,m_tb_plugin_args    (TextBox ::Params<>().parent(this_).id(ID_EDIT_PLUGIN_ARGS    ).xy(48, 175).wh(211, 14))
		
		,m_btn_browse        (Button  ::Params<>().parent(this_).text(L"Browse..."  ).id(ID_BUTTON_BROWSE_PLUGIN).xy(260, 161).wh(50, 14))
		,m_btn_add           (Button  ::Params<>().parent(this_).text(L"Add"        ).id(ID__BUTTON_ADDPLUGIN   ).xy(  7, 192).wh(50, 14))
		,m_btn_remove        (Button  ::Params<>().parent(this_).text(L"Remove"     ).id(ID__BUTTON_REMOVEPLUGIN).xy( 62, 192).wh(50, 14))
		,m_btn_ok            (Button  ::Params<>().parent(this_).text(L"OK"         ).id(IDOK                   ).xy(260, 192).wh(50, 14))

		,m_lbl_plugin_dll    (Label   ::Params<>().parent(this_).text(L"Plugin Dll:").id(ID__LBL_PLUGIN_DLL ).xy(16, 162).wh(32, 8).style('+', SS_LEFT))
		,m_lbl_arguments     (Label   ::Params<>().parent(this_).text(L"Arguments:" ).id(ID__LBL_PLUGIN_ARGS).xy( 9, 176).wh(38, 8).style('+', SS_LEFT))

		,m_plugin_mgr(&plugin_mgr)
	{
		CenterWindow(Parent());

		// Add columns to the plugin list
		m_list_plugins.InsertColumn(0, ListView::ColumnInfo(L"Name"    ).width(200));
		m_list_plugins.InsertColumn(1, ListView::ColumnInfo(L"Filepath").width(200));

		// Populate the plugin list
		PluginManager::Iter iter;
		for (auto p = m_plugin_mgr->First(iter); p; p = m_plugin_mgr->Next(iter))
			AddPluginToList(p);

		// List
		m_list_plugins.SelectionChanged += [&](ListView&, ListView::ItemChangedEventArgs const&)
		{
			UpdateUI();
		};

		// Browse
		m_btn_browse.Click += [&](pr::gui::Button&, pr::gui::EmptyArgs const&)
		{
			auto files = OpenFileUI(m_hwnd, FileUIOptions(L"dll", PluginFilterSpec, _countof(PluginFilterSpec)));
			if (files.empty()) return;
			m_tb_plugin_filepath.Text(files[0]);
		};

		// Add
		m_btn_add.Click += [&](pr::gui::Button&, pr::gui::EmptyArgs const&)
		{
			AddPluginToList(nullptr);
			UpdateUI();
		};

		// Remove
		m_btn_remove.Click += [&](pr::gui::Button&, pr::gui::EmptyArgs const&)
		{
			RemovePluginsFromList();
			UpdateUI();
		};

		UpdateUI();
	}

	// Add a plugin to the list in the UI
	void PluginManagerUI::AddPluginToList(Plugin const* plugin)
	{
		// If no plugin is given, get it from the text box
		if (plugin == nullptr)
		{
			// Check if a path to the plug in has been given, if not browse for one instead of adding
			auto filepath = m_tb_plugin_filepath.Text();
			if (filepath.empty())
				return m_btn_browse.PerformClick();

			// Try to add the plugin
			try
			{
				auto text = m_tb_plugin_args.Text();
				plugin = m_plugin_mgr->Add(filepath.c_str(), text.c_str());
			}
			catch (LdrException const& ex)
			{
				pr::events::Send(Event_Error(pr::Fmt("Plugin %s failed to load.\nReason: %s", filepath.c_str(), ex.what())));
			}

			// Don't add on failure
			if (plugin == nullptr)
				return;
		}

		auto name  = pr::Widen(plugin->Name());
		auto fpath = pr::Widen(plugin->Filepath());

		auto info = ListView::ItemInfo(name.c_str(), (int)m_list_plugins.ItemCount()).user((void*)plugin);
		int item = m_list_plugins.InsertItem(info);

		info = ListView::ItemInfo(item);
		m_list_plugins.Item(info.subitem((int)EColumn::Name    ).text(name.c_str()));
		m_list_plugins.Item(info.subitem((int)EColumn::Filepath).text(fpath.c_str()));

		pr::events::Send(Event_Refresh());
	}

	// Remove the selected plugins from the list in the UI
	void PluginManagerUI::RemovePluginsFromList()
	{
		// While there is a selected item, remove it
		for (auto item = m_list_plugins.NextItem(LVNI_SELECTED); item != -1; item = m_list_plugins.NextItem(LVNI_SELECTED, -1))
		{
			auto plugin = m_list_plugins.UserData<Plugin>(item);
			m_plugin_mgr->Remove(plugin);
			m_list_plugins.DeleteItem(item);
		}
		pr::events::Send(Event_Refresh());
	}

	// Enable/Disable UI elements
	void PluginManagerUI::UpdateUI()
	{
		bool plugin_selected = m_list_plugins.SelectedCount() != 0;
		m_btn_remove.Enabled(plugin_selected);
	}
}
