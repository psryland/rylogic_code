//*******************************************************************
// Lighting Dialog
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************
#pragma once

#include "linedrawer/main/forward.h"

namespace ldr
{
	// A UI for setting options
	struct OptionsUI :Form
	{
		enum
		{
			ID_TABCTRL = 100,
			ID_TAB_GENERAL, ID_LBL_TEXTEDITOR, ID_TB_TEXTEDITOR, ID_GRP_FOCUS_POINT,
			ID_TAB_NAVIGATION,
		};

		#pragma region Tabs
		struct General :Panel
		{
			Label    m_lbl_text_editor;
			TextBox  m_tb_text_editor;
			GroupBox m_grp_focus;

			General(Control* parent)
				:Panel(Panel::Params<>().name("tab-general").id(ID_TAB_GENERAL).wh(Fill,Fill).margin(10).parent(parent).anchor(EAnchor::All))
				,m_lbl_text_editor(Label::Params<>().name("lbl-text-editor").id(ID_LBL_TEXTEDITOR).text(L"Text Editor: ").xy(10,10).parent(this))
				,m_tb_text_editor(TextBox::Params<>().name("tb-text-editor").id(ID_TB_TEXTEDITOR).xy(0,Top|BottomOf|ID_LBL_TEXTEDITOR).wh(Fill, TextBox::DefH).margin(20).parent(this).anchor(EAnchor::LeftTopRight))
				,m_grp_focus(GroupBox::Params<>().name("grp-focus-point").id(ID_GRP_FOCUS_POINT).xy(10, Top|BottomOf|ID_TB_TEXTEDITOR).wh(Fill, Panel::DefH).margin(20).parent(this).text(L"Focus Point"))
			{}
		};
		struct Navigation :Panel
		{
			Navigation(Control* parent)
				:Panel(Panel::Params<>().name("tab-navigation").id(ID_TAB_NAVIGATION).wh(Fill,Fill).margin(10).parent(parent).anchor(EAnchor::All))
			{}
		};
		#pragma endregion

		TabControl m_tc;
		General    m_tab_general;
		Navigation m_tab_navigation;
		UserSettings* m_settings;

		OptionsUI(Control* main_ui, UserSettings& settings)
			:Form(MakeFormParams<>().name("options").title(L"Options").parent(main_ui).xy(CentreP,CentreP).wh(480,360).icon(IDI_ICON_MAIN).wndclass(RegisterWndClass<Form>()).hide_on_close().pin_window())
			,m_tc(TabControl::Params<>().name("m_tc").text(L"tabctrl").wh(Fill,Fill).id(ID_TABCTRL).margin(10).parent(this).anchor(EAnchor::All))//, DefaultControlStyle, 0UL)
			,m_tab_general(&m_tc)
			,m_tab_navigation(&m_tc)
			,m_settings(&settings)
		{
			CreateHandle();
			m_tc.Insert(L"General", m_tab_general);
			m_tc.Insert(L"Navigation", m_tab_navigation);
			m_tc.SelectedIndex(0);
		}
	};
}

#if 0
#include "linedrawer/main/forward.h"
#include "linedrawer/main/user_settings.h"
#include "linedrawer/utility/misc.h"
#include "linedrawer/resources/linedrawer.res.h"
#include "pr/gui/wtl_tab_view_ctrl.h"

namespace ldr
{
	// General options tab
	class COptionsGeneralTab
		:public CDialogImpl<COptionsGeneralTab>
		,public CDialogResize<COptionsGeneralTab>
		,public CWinDataExchange<COptionsGeneralTab>
	{
		CEdit         m_edit_text_editor_cmd;
		CTrackBarCtrl m_slider_focus_point_scale;
		CButton       m_check_reset_camera_on_load;
		CButton       m_check_msgbox_error_msgs;
		CButton       m_check_ignore_missing_includes;

	public:
		std::string  m_text_editor_cmd;
		float        m_focus_point_scale;
		bool         m_reset_camera_on_load;
		bool         m_msgbox_error_msgs;
		bool         m_ignore_missing_includes;

		enum { IDD = IDD_TAB_GENERAL };
		BEGIN_MSG_MAP(COptionsGeneralTab)
			MESSAGE_HANDLER(WM_INITDIALOG ,OnInitDialog)
			MESSAGE_HANDLER(WM_DESTROY    ,OnDestroy)
			CHAIN_MSG_MAP(CDialogResize<COptionsGeneralTab>)
		END_MSG_MAP()
		BEGIN_DLGRESIZE_MAP(COptionsGeneralTab)
			DLGRESIZE_CONTROL(IDC_STATIC_TEXT_EDITOR_CMD        ,DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_EDIT_TEXT_EDITOR_CMD          ,DLSZ_SIZE_X|DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_GROUP_CAMERA                  ,DLSZ_SIZE_X|DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_STATIC_FOCUS_POINT_SCALE      ,DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_SLIDER_FOCUS_POINT_SCALE      ,DLSZ_SIZE_X|DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_CHECK_RESET_CAM_ON_LOAD       ,DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_GROUP_ERROR_OUTPUT            ,DLSZ_SIZE_X|DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_CHECK_MSGBOX_ERROR_MSGS       ,DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_CHECK_IGNORE_MISSING_INCLUDES ,DLSZ_REPAINT)
		END_DLGRESIZE_MAP()
		BEGIN_DDX_MAP(COptionsGeneralTab)
			if (nCtlID == (UINT)-1 || nCtlID == IDC_EDIT_TEXT_EDITOR_CMD)
			{
				if (bSaveAndValidate) m_text_editor_cmd = pr::GetCtrlText<char>(m_edit_text_editor_cmd);
				else                  ::SetWindowTextA(m_edit_text_editor_cmd, m_text_editor_cmd.c_str());
			}
			if (nCtlID == (UINT)-1 || nCtlID == IDC_SLIDER_FOCUS_POINT_SCALE)
			{
				if (bSaveAndValidate) m_focus_point_scale = m_slider_focus_point_scale.GetPos() * 0.002f;
				else                  m_slider_focus_point_scale.SetPos(pr::Clamp(int(m_focus_point_scale * 500.0f), 0, 100));
			}
			if (nCtlID == (UINT)-1 || nCtlID == IDC_CHECK_RESET_CAM_ON_LOAD)
			{
				if (bSaveAndValidate) m_reset_camera_on_load = m_check_reset_camera_on_load.GetCheck() == BST_CHECKED;
				else                  m_check_reset_camera_on_load.SetCheck(m_reset_camera_on_load ? BST_CHECKED : BST_UNCHECKED);
			}
			if (nCtlID == (UINT)-1 || nCtlID == IDC_CHECK_MSGBOX_ERROR_MSGS)
			{
				if (bSaveAndValidate) m_msgbox_error_msgs = m_check_msgbox_error_msgs.GetCheck() == BST_CHECKED;
				else                  m_check_msgbox_error_msgs.SetCheck(m_msgbox_error_msgs ? BST_CHECKED : BST_UNCHECKED);
			}
			if (nCtlID == (UINT)-1 || nCtlID == IDC_CHECK_IGNORE_MISSING_INCLUDES)
			{
				if (bSaveAndValidate) m_ignore_missing_includes = m_check_ignore_missing_includes.GetCheck() == BST_CHECKED;
				else                  m_check_ignore_missing_includes.SetCheck(m_ignore_missing_includes ? BST_CHECKED : BST_UNCHECKED);
			}
		END_DDX_MAP();

		COptionsGeneralTab(UserSettings const& settings)
		{
			m_text_editor_cmd         = pr::Narrow(settings.m_TextEditorCmd);
			m_focus_point_scale       = settings.m_FocusPointScale;
			m_reset_camera_on_load    = settings.m_ResetCameraOnLoad;
			m_msgbox_error_msgs       = settings.m_ErrorOutputMsgBox;
			m_ignore_missing_includes = settings.m_IgnoreMissingIncludes;
		}

		// Handler methods
		LRESULT OnInitDialog(UINT, WPARAM, LPARAM, BOOL&)
		{
			m_edit_text_editor_cmd          .Attach(GetDlgItem(IDC_EDIT_TEXT_EDITOR_CMD));
			m_slider_focus_point_scale      .Attach(GetDlgItem(IDC_SLIDER_FOCUS_POINT_SCALE));
			m_check_reset_camera_on_load    .Attach(GetDlgItem(IDC_CHECK_RESET_CAM_ON_LOAD));
			m_check_msgbox_error_msgs       .Attach(GetDlgItem(IDC_CHECK_MSGBOX_ERROR_MSGS));
			m_check_ignore_missing_includes .Attach(GetDlgItem(IDC_CHECK_IGNORE_MISSING_INCLUDES));

			m_slider_focus_point_scale.SetRange(0, 100);

			DoDataExchange(FALSE);
			DlgResize_Init(false, false);
			return S_OK;
		}
		LRESULT OnDestroy(UINT, WPARAM, LPARAM, BOOL&)
		{
			DoDataExchange(TRUE);
			m_edit_text_editor_cmd          .Detach();
			m_slider_focus_point_scale      .Detach();
			m_check_reset_camera_on_load    .Detach();
			m_check_msgbox_error_msgs       .Detach();
			m_check_ignore_missing_includes .Detach();
			return S_OK;
		}
		LRESULT OnEraseBkGnd(UINT)
		{
			return S_OK;
		}
	};

	// Rendering options tab
	//class COptionsRenderingTab
	//	:public CDialogImpl<COptionsRenderingTab>
	//	,public CWinDataExchange<COptionsRenderingTab>
	//{
	//	CComboBox m_combo_shader_version;
	//	CComboBox m_combo_geometry_quality;
	//	CComboBox m_combo_texture_quality;
	//
	//public:
	//	pr::rdr::EShaderVersion::Type m_shader_version;
	//	pr::rdr::EQuality::Type       m_geometry_quality;
	//	pr::rdr::EQuality::Type       m_texture_quality;
	//
	//	enum { IDD = IDD_TAB_RENDERING };
	//	BEGIN_MSG_MAP(COptionsRenderingTab)
	//		MESSAGE_HANDLER(WM_INITDIALOG ,OnInitDialog)
	//		MESSAGE_HANDLER(WM_DESTROY    ,OnDestroy)
	//	END_MSG_MAP()
	//	BEGIN_DDX_MAP(COptionsRenderingTab)
	//		if (nCtlID == (UINT)-1 || nCtlID == IDC_COMBO_SHADER_VERSION)
	//			if (bSaveAndValidate) m_shader_version = static_cast<pr::rdr::EShaderVersion::Type>(m_combo_shader_version.GetCurSel());
	//			else                  m_combo_shader_version.SetCurSel(static_cast<int>(m_shader_version));
	//		if (nCtlID == (UINT)-1 || nCtlID == IDC_COMBO_GEOMETRY_QUALITY)
	//			if (bSaveAndValidate) m_geometry_quality = static_cast<pr::rdr::EQuality::Type>(m_combo_geometry_quality.GetCurSel());
	//			else                  m_combo_geometry_quality.SetCurSel(static_cast<int>(m_geometry_quality));
	//		if (nCtlID == (UINT)-1 || nCtlID == IDC_COMBO_TEXTURE_QUALITY)
	//			if (bSaveAndValidate) m_texture_quality = static_cast<pr::rdr::EQuality::Type>(m_combo_texture_quality.GetCurSel());
	//			else                  m_combo_texture_quality.SetCurSel(static_cast<int>(m_texture_quality));
	//	END_DDX_MAP();
	//
	//	COptionsRenderingTab(UserSettings const& settings)
	//	{
	//		m_shader_version = settings.m_shader_version;
	//		m_geometry_quality = settings.m_geometry_quality;
	//		m_texture_quality = settings.m_texture_quality;
	//	}
	//
	//	// Handler methods
	//	LRESULT OnInitDialog(UINT, WPARAM, LPARAM, BOOL&)
	//	{
	//		m_combo_shader_version.Attach(GetDlgItem(IDC_COMBO_SHADER_VERSION));
	//		m_combo_geometry_quality.Attach(GetDlgItem(IDC_COMBO_GEOMETRY_QUALITY));
	//		m_combo_texture_quality.Attach(GetDlgItem(IDC_COMBO_TEXTURE_QUALITY));
	//		for (int i = 0; i != pr::rdr::EShaderVersion::NumberOf; ++i)
	//			m_combo_shader_version.AddString(pr::rdr::EShaderVersion::ToString(static_cast<pr::rdr::EShaderVersion::Type>(i)));
	//		for (int i = 0; i != pr::rdr::EQuality::NumberOf; ++i)
	//			m_combo_geometry_quality.AddString(pr::rdr::EQuality::ToString(static_cast<pr::rdr::EQuality::Type>(i)));
	//		for (int i = 0; i != pr::rdr::EQuality::NumberOf; ++i)
	//			m_combo_texture_quality.AddString(pr::rdr::EQuality::ToString(static_cast<pr::rdr::EQuality::Type>(i)));
	//
	//		DoDataExchange(FALSE);
	//		return S_OK;
	//	}
	//	LRESULT OnDestroy(UINT, WPARAM, LPARAM, BOOL&)
	//	{
	//		DoDataExchange(TRUE);
	//		m_combo_shader_version.Detach();
	//		return S_OK;
	//	}
	//};

	// Rendering options tab
	class COptionsNavigationTab
		:public CDialogImpl<COptionsNavigationTab>
		,public CDialogResize<COptionsNavigationTab>
		,public CWinDataExchange<COptionsNavigationTab>
	{
		enum { CamOrbitSpeedLimit = 314 };
		CTrackBarCtrl m_slider_camera_orbit_speed;

	public:
		float m_camera_orbit_speed;

		enum { IDD = IDD_TAB_NAVIGATION };
		BEGIN_MSG_MAP(COptionsNavigationTab)
			MESSAGE_HANDLER(WM_INITDIALOG   ,OnInitDialog)
			MESSAGE_HANDLER(WM_DESTROY      ,OnDestroy)
		END_MSG_MAP()
		BEGIN_DLGRESIZE_MAP(COptionsNavigationTab)
			DLGRESIZE_CONTROL(IDC_STATIC_CAM_ORBIT_SPEED    ,DLSZ_REPAINT)
			DLGRESIZE_CONTROL(IDC_SLIDER_CAM_ORBIT_SPEED    ,DLSZ_SIZE_X|DLSZ_REPAINT)
		END_DLGRESIZE_MAP()
		BEGIN_DDX_MAP(COptionsNavigationTab)
			if (nCtlID == (UINT)-1 || nCtlID == IDC_SLIDER_CAM_ORBIT_SPEED)
			if (bSaveAndValidate) m_camera_orbit_speed = m_slider_camera_orbit_speed.GetPos() * 0.01f;
			else                  m_slider_camera_orbit_speed.SetPos(pr::Clamp<int>(int(m_camera_orbit_speed * 100.0f), -CamOrbitSpeedLimit, CamOrbitSpeedLimit));
		END_DDX_MAP();

		COptionsNavigationTab(UserSettings const& settings)
		{
			m_camera_orbit_speed = settings.m_CameraOrbitSpeed;
		}

		// Handler methods
		LRESULT OnInitDialog(UINT, WPARAM, LPARAM, BOOL&)
		{
			m_slider_camera_orbit_speed.Attach(GetDlgItem(IDC_SLIDER_CAM_ORBIT_SPEED));
			m_slider_camera_orbit_speed.SetRange(-CamOrbitSpeedLimit, CamOrbitSpeedLimit);
			DoDataExchange(FALSE);

			DlgResize_Init(false, false);
			return S_OK;
		}
		LRESULT OnDestroy(UINT, WPARAM, LPARAM, BOOL&)
		{
			DoDataExchange(TRUE);
			m_slider_camera_orbit_speed.Detach();
			return S_OK;
		}
		LRESULT OnEraseBkGnd(UINT)
		{
			return S_OK;
		}
	};

	// Main Options
	class COptionsDlg
		:public CDialogImpl<COptionsDlg>
		,public CDialogResize<COptionsDlg>
	{
		HWND               m_parent;
		WTL::CTabViewCtrl  m_tab_main;

	public:
		COptionsGeneralTab m_tab_general;
		//COptionsRenderingTab m_tab_rendering;
		COptionsNavigationTab m_tab_navigation;

		enum { IDD = IDD_DIALOG_OPTIONS };
		BEGIN_MSG_MAP(COptionsDlg)
			MESSAGE_HANDLER(WM_INITDIALOG ,OnInitDialog)
			MESSAGE_HANDLER(WM_SIZE       ,OnSize)
			COMMAND_ID_HANDLER(IDOK       ,OnCloseDialog)
			COMMAND_ID_HANDLER(IDCANCEL   ,OnCloseDialog)
			NOTIFY_HANDLER(IDC_TAB_MAIN   ,TCN_SELCHANGE ,m_tab_main.OnSelectionChanged)
			CHAIN_MSG_MAP(CDialogResize<COptionsDlg>)
		END_MSG_MAP()
		BEGIN_DLGRESIZE_MAP(COptionsDlg)
			DLGRESIZE_CONTROL(IDC_TAB_MAIN ,DLSZ_SIZE_X|DLSZ_SIZE_Y)
			DLGRESIZE_CONTROL(IDOK         ,DLSZ_MOVE_X|DLSZ_MOVE_Y)
			DLGRESIZE_CONTROL(IDCANCEL     ,DLSZ_MOVE_X|DLSZ_MOVE_Y)
		END_DLGRESIZE_MAP()

		COptionsDlg(UserSettings const& settings, HWND parent = 0)
			:m_parent(parent)
			,m_tab_main()
			,m_tab_general(settings)
			,m_tab_navigation(settings)
			//,m_tab_rendering(settings)
		{}

		// Handler methods
		LRESULT OnInitDialog(UINT, WPARAM, LPARAM, BOOL&)
		{
			CenterWindow(m_parent);

			// Attach and initialise controls
			m_tab_main.Attach(GetDlgItem(IDC_TAB_MAIN));
			m_tab_general    .Create(m_tab_main.m_hWnd);
			m_tab_navigation .Create(m_tab_main.m_hWnd);
			//m_tab_rendering.Create(m_hWnd);
			m_tab_main.AddTab("General"    ,m_tab_general    ,TRUE  ,-1 ,(LPARAM)&m_tab_general);
			m_tab_main.AddTab("Navigation" ,m_tab_navigation ,FALSE ,-1 ,(LPARAM)&m_tab_navigation);
			//m_tab_main.AddTab("Rendering"  ,m_tab_rendering  ,FALSE ,-1 ,(LPARAM)&m_tab_rendering);
			m_tab_main.UpdateViews();

			DlgResize_Init(false, false);
			return S_OK;
		}
		LRESULT OnCloseDialog(WORD, WORD wID, HWND, BOOL&)
		{
			// Detach controls
			m_tab_main.Detach();
			EndDialog(wID);
			return S_OK;
		}
		LRESULT OnSize(UINT, WPARAM, LPARAM, BOOL& bHandled)
		{
			m_tab_main.UpdateViews();
			bHandled = FALSE;
			return S_OK;
		}

		// Return the settings
		void GetSettings(UserSettings& settings) const
		{
			settings.m_TextEditorCmd         = pr::Widen(m_tab_general.m_text_editor_cmd);
			settings.m_ResetCameraOnLoad     = m_tab_general.m_reset_camera_on_load;
			settings.m_FocusPointScale       = m_tab_general.m_focus_point_scale;
			settings.m_ErrorOutputMsgBox     = m_tab_general.m_msgbox_error_msgs;
			settings.m_IgnoreMissingIncludes = m_tab_general.m_ignore_missing_includes;
			settings.m_CameraOrbitSpeed      = m_tab_navigation.m_camera_orbit_speed;
		}
	};
}
#endif