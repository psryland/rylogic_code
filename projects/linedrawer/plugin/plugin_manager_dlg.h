//***************************************************************************************************
// Lighting Dialog
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************
#pragma once
#ifndef PR_LDR_PLUGIN_MANAGER_DLG_H
#define PR_LDR_PLUGIN_MANAGER_DLG_H

#include "linedrawer/main/forward.h"
#include "linedrawer/resources/linedrawer.res.h"

namespace ldr
{
	class PluginManagerDlg
		:public CDialogImpl<PluginManagerDlg>
		,public CDialogResize<PluginManagerDlg>
		//,public CWinDataExchange<PluginManagerDlg>
	{
		HWND           m_parent;
		PluginManager& m_plugin_mgr;
		CListViewCtrl  m_list_plugins;
		CEdit          m_edit_plugin_filepath;
		CEdit          m_edit_plugin_args;
		CButton        m_btn_browse;
		CButton        m_btn_add;
		CButton        m_btn_remove;

	public:
		enum { IDD = IDD_DIALOG_PLUGINS };
		BEGIN_MSG_MAP(PluginManagerDlg)
			MESSAGE_HANDLER(WM_INITDIALOG              ,OnInitDialog)
			COMMAND_ID_HANDLER(IDOK                    ,OnCloseDialog)
			COMMAND_ID_HANDLER(IDCLOSE                 ,OnCloseDialog)
			COMMAND_ID_HANDLER(IDCANCEL                ,OnCloseDialog)
			COMMAND_HANDLER(IDC_BUTTON_BROWSE_PLUGIN   ,BN_CLICKED      ,OnBrowsePlugin)
			COMMAND_HANDLER(IDC_BUTTON_ADDPLUGIN       ,BN_CLICKED      ,OnAddPlugin)
			COMMAND_HANDLER(IDC_BUTTON_REMOVEPLUGIN    ,BN_CLICKED      ,OnRemovePlugin)
			NOTIFY_HANDLER(IDC_LIST_PLUGINS            ,LVN_ITEMCHANGED	,OnListItemSelected)
			CHAIN_MSG_MAP(CDialogResize<PluginManagerDlg>)
		END_MSG_MAP()
		BEGIN_DLGRESIZE_MAP(PluginManagerDlg)
			DLGRESIZE_CONTROL(IDC_LIST_PLUGINS         ,DLSZ_SIZE_X|DLSZ_SIZE_Y|DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_LBL_PLUGIN_DLL       ,DLSZ_MOVE_Y|DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_LBL_PLUGIN_ARGS      ,DLSZ_MOVE_Y|DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_EDIT_PLUGIN_FILEPATH ,DLSZ_SIZE_X|DLSZ_MOVE_Y|DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_EDIT_PLUGIN_ARGS     ,DLSZ_SIZE_X|DLSZ_MOVE_Y|DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_BUTTON_BROWSE_PLUGIN ,DLSZ_MOVE_X|DLSZ_MOVE_Y|DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_BUTTON_ADDPLUGIN     ,DLSZ_MOVE_Y|DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_BUTTON_REMOVEPLUGIN  ,DLSZ_MOVE_Y|DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDOK                     ,DLSZ_MOVE_X|DLSZ_MOVE_Y|DLSZ_REPAINT)
		END_DLGRESIZE_MAP()
		//BEGIN_DDX_MAP(PluginManagerDlg)
		//	if (nCtlID == (UINT)-1 || nCtlID == IDC_EDIT_PLUGIN_FILEPATH)
		//		if (bSaveAndValidate) m_text_editor_cmd = pr::GetCtrlText(m_edit_text_editor_cmd);
		//		else                  m_edit_text_editor_cmd.SetWindowText(m_text_editor_cmd.c_str());
		//	if (nCtlID == (UINT)-1 || nCtlID == IDC_SLIDER_FOCUS_POINT_SCALE)
		//		if (bSaveAndValidate) m_focus_point_scale = m_slider_focus_point_scale.GetPos() * 0.002f;
		//		else                  m_slider_focus_point_scale.SetPos(pr::Clamp(int(m_focus_point_scale * 500.0f), 0, 100));
		//	if (nCtlID == (UINT)-1 || nCtlID == IDC_CHECK_RESET_CAM_ON_LOAD)
		//		if (bSaveAndValidate) m_reset_camera_on_load = m_check_reset_camera_on_load.GetCheck() == BST_CHECKED;
		//		else                  m_check_reset_camera_on_load.SetCheck(m_reset_camera_on_load ? BST_CHECKED : BST_UNCHECKED);
		//	if (nCtlID == (UINT)-1 || nCtlID == IDC_CHECK_MSGBOX_ERROR_MSGS)
		//		if (bSaveAndValidate) m_msgbox_error_msgs = m_check_msgbox_error_msgs.GetCheck() == BST_CHECKED;
		//		else                  m_check_msgbox_error_msgs.SetCheck(m_msgbox_error_msgs ? BST_CHECKED : BST_UNCHECKED);
		//	if (nCtlID == (UINT)-1 || nCtlID == IDC_CHECK_IGNORE_MISSING_INCLUDES)
		//		if (bSaveAndValidate) m_ignore_missing_includes = m_check_ignore_missing_includes.GetCheck() == BST_CHECKED;
		//		else                  m_check_ignore_missing_includes.SetCheck(m_ignore_missing_includes ? BST_CHECKED : BST_UNCHECKED);
		//END_DDX_MAP();

		PluginManagerDlg(PluginManager& plugin_mgr, HWND parent);

		// Handler methods
		LRESULT OnInitDialog      (UINT, WPARAM, LPARAM, BOOL&);
		LRESULT OnCloseDialog     (WORD, WORD wID, HWND, BOOL&);
		LRESULT OnBrowsePlugin    (WORD, WORD, HWND, BOOL&);
		LRESULT OnAddPlugin       (WORD, WORD, HWND, BOOL&);
		LRESULT OnRemovePlugin    (WORD, WORD, HWND, BOOL&);
		LRESULT OnListItemSelected(WPARAM, LPNMHDR, BOOL&);

		// Add a plugin to the list in the UI
		void AddPluginToList(Plugin const* plugin);

		// Enable/Disable UI elements
		void UpdateUI();
	};
}
#endif
