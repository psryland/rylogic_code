//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"
#include "linedrawer/main/linedrawer.h"
#include "linedrawer/main/ldrevent.h"
#include "linedrawer/resources/linedrawer.res.h"
#include "linedrawer/utility/misc.h"

namespace ldr
{
	struct MainGUI
		:pr::app::MainGUI<ldr::MainGUI, ldr::Main, pr::SimMsgLoop>
		,pr::cmdline::IOptionReceiver
		,pr::gui::RecentFiles::IHandler
		,pr::events::IRecv<ldr::Event_Info>
		,pr::events::IRecv<ldr::Event_Warn>
		,pr::events::IRecv<ldr::Event_Error>
		,pr::events::IRecv<ldr::Event_Status>
		,pr::events::IRecv<ldr::Event_Refresh>
		,pr::events::IRecv<ldr::Event_StoreChanging>
		,pr::events::IRecv<ldr::Event_StoreChanged>
		,pr::events::IRecv<pr::rdr::Evt_UpdateScene>
		,pr::events::IRecv<pr::ldr::Evt_Refresh>
		,pr::events::IRecv<pr::ldr::Evt_LdrMeasureCloseWindow>
		,pr::events::IRecv<pr::ldr::Evt_LdrMeasureUpdate>
		,pr::events::IRecv<pr::ldr::Evt_LdrAngleDlgCloseWindow>
		,pr::events::IRecv<pr::ldr::Evt_LdrAngleDlgUpdate>
		,pr::events::IRecv<pr::settings::Evt<UserSettings>>
		,pr::AlignTo<16>
	{
		typedef pr::app::MainGUI<ldr::MainGUI, ldr::Main, pr::SimMsgLoop> base;

		enum
		{
			IDC_STATUSBAR_MAIN = 200,
		};

		pr::gui::StatusBar        m_status;               // The status bar
		pr::gui::RecentFiles      m_recent_files;         // The recent files
		pr::gui::MenuList         m_saved_views;          // A list of camera snapshots
		pr::ldr::ObjectManagerDlg m_store_ui;             // GUI window for manipulating ldr object properties
		pr::ldr::ScriptEditorDlg  m_editor_ui;            // An editor for ldr script
		pr::ldr::MeasureDlg       m_measure_tool_ui;      // The UI for the measuring tool
		pr::ldr::AngleDlg         m_angle_tool_ui;        // The UI for the angle measuring tool
		pr::gui::Menu             m_menu;                 // The main menu handle (needed for restoring after fullscreen mode switch)
		bool                      m_mouse_status_updates; // Whether to show mouse position in the status bar (todo: more general system for this)
		bool                      m_suspend_render;       // True to prevent rendering
		StatusPri                 m_status_pri;           // Status priority buffer

		static char const* AppName() { return ldr::AppTitleA(); }

		MainGUI(LPTSTR cmdline, int nCmdShow);
		~MainGUI();

	private:
		// 30Hz step function
		void Step30Hz(double elapsed_seconds);

		// Message map function
		bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override;

		bool OnPaint(PaintEventArgs const& args) override;
		void OnDropFiles(DropFilesEventArgs const& drop) override;
		bool OnKey(KeyEventArgs const& args) override;
		bool OnMouseButton(MouseEventArgs const& args) override;
		bool OnMouseClick(MouseEventArgs const& args) override;
		void OnMouseMove(MouseEventArgs const& args) override;
		bool OnMouseWheel(MouseWheelArgs const& args) override;
		void OnFullScreenToggle(bool is_fullscreen) override;

		bool HandleMenu(UINT item_id, UINT event_source, HWND ctrl_hwnd) override;
		void OnFileNew();
		void OnFileNewScript();
		void OnFileOpen(bool additive);
		void OnResetView(EObjectBounds bounds);
		void OnViewAxis(pr::v4 const& axis);
		void OnSetFocusPosition();
		void OnSetCameraPosition();
		void OnNavAlign(pr::v4 const& axis);
		void OnSaveView(bool clear_saves);
		void OnOrbit();
		void OnShowObjectManagerUI();
		void OnEditSourceFiles();
		void OnDataClearScene();
		void OnDataAutoRefresh();
		void OnCreateDemoScene();
		void OnShowFocus();
		void OnShowOrigin();
		void OnShowSelection();
		void OnShowObjBBoxes();
		void OnToggleFillMode();
		void OnRender2D();
		void OnRenderTechnique();
		void OnShowLightingDlg();
		void OnShowToolDlg(int tool);
		void OnManipulateMode();
		void OnShowOptions();
		void OnShowPluginMgr();
		void OnWindowAlwaysOnTop();
		void OnWindowBackgroundColour();
		void OnWindowExampleScript();
		void OnCheckForUpdates();
		void OnWindowShowAboutBox();

		void CloseApp(int exit_code);
		void FileNew(wchar_t const* filepath);
		void FileOpen(wchar_t const* filepath, bool additive);
		void OpenTextEditor(StrList const& files);
		pr::v2 ToNormSS(pr::v2 const& pt_ss);
		void MouseStatusUpdate(pr::v2 const& mouse_location);
		void ShowAbout() const;
		void UpdateUI();

		// Recent files callbacks
		void MenuList_OnClick(pr::gui::MenuList* sender, pr::gui::MenuList::Item const& item);
		void MenuList_ListChanged(pr::gui::MenuList* sender);

		// Ldr event handlers
		void OnEvent(ldr::Event_Info const& e) override;
		void OnEvent(ldr::Event_Warn const& e) override;
		void OnEvent(ldr::Event_Error const& e) override;
		void OnEvent(ldr::Event_Status const& e) override;
		void OnEvent(ldr::Event_Refresh const& e) override;
		void OnEvent(ldr::Event_StoreChanging const&) override;
		void OnEvent(ldr::Event_StoreChanged const&) override;
		void OnEvent(pr::rdr::Evt_UpdateScene const&) override;
		void OnEvent(pr::ldr::Evt_Refresh const& e) override;
		void OnEvent(pr::ldr::Evt_LdrMeasureCloseWindow const&) override;
		void OnEvent(pr::ldr::Evt_LdrMeasureUpdate const&) override;
		void OnEvent(pr::ldr::Evt_LdrAngleDlgCloseWindow const&) override;
		void OnEvent(pr::ldr::Evt_LdrAngleDlgUpdate const&) override;
		void OnEvent(pr::settings::Evt<UserSettings> const&) override;

		// Command line
		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end);
	};
}
