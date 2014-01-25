//***************************************************************************************************
// Lighting Dialog
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************
#include "linedrawer/main/stdafx.h"
#include "linedrawer/plugin/plugin_manager_dlg.h"
#include "linedrawer/plugin/plugin_manager.h"
#include "linedrawer/plugin/plugin.h"
#include "linedrawer/main/ldrevent.h"
#include "pr/gui/misc.h"

namespace ldr
{
	TCHAR PluginFileFilter[] = TEXT("Ldr Plugin (*.dll)\0*.dll\0\0");
	namespace EColumn
	{
		enum Type { Name, Filepath, NumberOf };
		inline char const* ToString(EColumn::Type type)
		{
			switch (type){
			default:       return "";
			case Name:     return "Name";
			case Filepath: return "Filepath";
			}
		}
	}
}

ldr::PluginManagerDlg::PluginManagerDlg(PluginManager& plugin_mgr, HWND parent)
:m_plugin_mgr(plugin_mgr)
,m_parent(parent)
{}

//
LRESULT ldr::PluginManagerDlg::OnInitDialog(UINT, WPARAM, LPARAM, BOOL&)
{
	CenterWindow(m_parent);
	m_list_plugins         .Attach(GetDlgItem(IDC_LIST_PLUGINS));
	m_edit_plugin_filepath .Attach(GetDlgItem(IDC_EDIT_PLUGIN_FILEPATH));
	m_edit_plugin_args     .Attach(GetDlgItem(IDC_EDIT_PLUGIN_ARGS));
	m_btn_browse           .Attach(GetDlgItem(IDC_BUTTON_BROWSE_PLUGIN));
	m_btn_add              .Attach(GetDlgItem(IDC_BUTTON_ADDPLUGIN));
	m_btn_remove           .Attach(GetDlgItem(IDC_BUTTON_REMOVEPLUGIN));

	// Add columns to the plugin list
	for (int i = 0; i != EColumn::NumberOf; ++i)
	{
		m_list_plugins.AddColumn(EColumn::ToString(EColumn::Type(i)), i);
		m_list_plugins.SetColumnWidth(i, LVSCW_AUTOSIZE_USEHEADER);
	}

	// Populate the plugin list
	PluginManager::Iter iter;
	for (Plugin const* p = m_plugin_mgr.First(iter); p; p = m_plugin_mgr.Next(iter))
		AddPluginToList(p);

	DlgResize_Init();
	UpdateUI();
	return S_OK;
}

LRESULT ldr::PluginManagerDlg::OnCloseDialog(WORD, WORD wID, HWND, BOOL&)
{
	EndDialog(wID);
	return S_OK;
}

// Browse for a plugin filepath
LRESULT ldr::PluginManagerDlg::OnBrowsePlugin(WORD, WORD, HWND, BOOL&)
{
	WTL::CFileDialog fd(TRUE,0,0,0,ldr::PluginFileFilter,m_hWnd);
	if (fd.DoModal() != IDOK) return S_OK;
	m_edit_plugin_filepath.SetWindowTextA(fd.m_szFileName);
	return S_OK;
}

// Add a plugin
LRESULT ldr::PluginManagerDlg::OnAddPlugin(WORD, WORD, HWND, BOOL& bHandled)
{
	// Check if a path to the plug in has been given, if not browse for one instead of adding
	std::string filepath = pr::GetCtrlText(m_edit_plugin_filepath);
	if (filepath.empty()) return OnBrowsePlugin(0,0,0,bHandled);

	// Try to add the plugin
	Plugin* plugin = 0;
	try { plugin = m_plugin_mgr.Add(filepath.c_str(), pr::GetCtrlText(m_edit_plugin_args).c_str()); }
	catch (LdrException const& ex)
	{
		pr::events::Send(ldr::Event_Error(pr::Fmt("Plugin %s failed to load.\nReason: %s", filepath.c_str(), ex.what())));
		return S_OK;
	}

	// If successfully added, add an entry to the list
	AddPluginToList(plugin);
	UpdateUI();

	pr::events::Send(ldr::Event_Refresh());
	return S_OK;
}

// Remove selected plugins
LRESULT ldr::PluginManagerDlg::OnRemovePlugin(WORD, WORD, HWND, BOOL&)
{
	// While there is a selected item, remove it
	for (int i = m_list_plugins.GetNextItem(-1, LVNI_SELECTED); i != -1; i = m_list_plugins.GetNextItem(-1, LVNI_SELECTED))
	{
		Plugin* plugin = reinterpret_cast<Plugin*>(m_list_plugins.GetItemData(i));
		m_plugin_mgr.Remove(plugin);
		m_list_plugins.DeleteItem(i);
	}
	pr::events::Send(ldr::Event_Refresh());
	return S_OK;
}

// Handle list items being selected
LRESULT ldr::PluginManagerDlg::OnListItemSelected(WPARAM, LPNMHDR hdr, BOOL& bHandled)
{
	if (hdr->code == LVN_ITEMCHANGED)
	{
		NMLISTVIEW* data = reinterpret_cast<NMLISTVIEW*>(hdr);
		if ((data->uNewState ^ data->uOldState) & LVIS_SELECTED) // If the selection has changed
		{
			UpdateUI();
			return S_OK;
		}
	}
	bHandled = false;
	return S_OK;
}

// Add a plugin to the list in the UI
void ldr::PluginManagerDlg::AddPluginToList(Plugin const* plugin)
{
	int item = m_list_plugins.InsertItem(m_list_plugins.GetItemCount(), plugin->Name());
	m_list_plugins.SetItemText(item ,EColumn::Name     ,plugin->Name());
	m_list_plugins.SetItemText(item ,EColumn::Filepath ,plugin->Filepath());
	m_list_plugins.SetItemData(item, reinterpret_cast<DWORD_PTR>(plugin));

	// Resize the columns
	for (int i = 0; i != EColumn::NumberOf; ++i)
		m_list_plugins.SetColumnWidth(i, LVSCW_AUTOSIZE_USEHEADER);
}

// Enable/Disable UI elements
void ldr::PluginManagerDlg::UpdateUI()
{
	bool plugin_selected = m_list_plugins.GetSelectedCount() != 0;
	m_btn_remove.EnableWindow(plugin_selected);
}
