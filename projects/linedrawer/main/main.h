//*****************************************************************************************
// LineDrawer
//  Copyright (C) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"
#include "linedrawer/main/lua_source.h"
#include "linedrawer/main/navigation.h"
#include "linedrawer/main/manipulator.h"
#include "linedrawer/main/script_sources.h"
#include "linedrawer/main/user_settings.h"
#include "linedrawer/utility/misc.h"
#include "linedrawer/gui/options_dlg.h"
#include "linedrawer/resources/linedrawer.res.h"

namespace ldr
{
	struct MainUI
		:pr::gui::Form
		,pr::events::IRecv<Evt_AppMsg>
		,pr::events::IRecv<Evt_Refresh>
		,pr::events::IRecv<Evt_StoreChanging>
		,pr::events::IRecv<Evt_StoreChanged>
		,pr::events::IRecv<Evt_SettingsError>
		,pr::events::IRecv<Evt_SettingsChanged>
		,pr::events::IRecv<Evt_UpdateScene>
		,pr::events::IRecv<Evt_SelectionChanged>
		,pr::events::IRecv<Evt_RenderStepExecute>
		,pr::cmdline::IOptionReceiver<wchar_t>
		,pr::AlignTo<16>
	{
		enum
		{
			IDC_STATUSBAR_MAIN = 200,
		};

		// App settings
		UserSettings m_settings;

		// Main UI
		pr::gui::StatusBar   m_status;       // The status bar
		pr::gui::Panel       m_panel;        // The panel containing the 3D scene
		pr::gui::RecentFiles m_recent_files; // The recent files
		pr::gui::MenuList    m_saved_views;  // A list of camera snapshots

		// 3D Scene
		pr::Renderer    m_rdr;         // The renderer
		pr::rdr::Window m_window;      // The window that will be rendered into
		pr::rdr::Scene  m_scene;       // The main view
		pr::Camera      m_cam;         // A camera

		// Object Container
		pr::ldr::ObjectCont m_store; // A container of all ldr objects created

		// Child windows/dialogs
		pr::ldr::LdrObjectManagerUI m_store_ui;        // UI for managing ldr objects in the scene
		pr::ldr::ScriptEditorUI     m_editor_ui;       // An editor for ldr script
		pr::ldr::LdrMeasureUI       m_measure_tool_ui; // The UI for the measuring tool
		pr::ldr::LdrAngleUI         m_angle_tool_ui;   // The UI for the angle measuring tool
		OptionsUI                   m_options_ui;      // The UI for setting LineDrawer settings

		// Stock Objects
		pr::ldr::LdrObjectStepData::Link m_step_objects;
		pr::ldr::StockInstance           m_focus_point;
		pr::ldr::StockInstance           m_origin_point;
		pr::ldr::StockInstance           m_selection_box;
		pr::ldr::StockInstance           m_bbox_model;
		pr::ldr::StockInstance           m_test_model;
		bool                             m_test_model_enable;

		// Modules
		Navigation    m_nav;     // Implements camera navigation from user input
		Manipulator   m_manip;   // Implements object manipulation from user input
		LuaSource     m_lua_src; // An object for processing lua files
		ScriptSources m_sources; // Manages source scripts

		mutable pr::BBox m_bbox_scene;           // Bounding box for all objects in the scene (Lazy updated)
		EControlMode     m_ctrl_mode;            // The mode of control, either navigating or manipulating
		IInputHandler*   m_input;                //
		int              m_scene_rdr_pass;       // Index of the current scene.Render() call
		bool             m_mouse_status_updates; // Whether to show mouse position in the status bar (todo: more general system for this)
		bool             m_suspend_render;       // True to prevent rendering
		StatusPri        m_status_pri;           // Status priority buffer

		// Construct
		MainUI(wchar_t const* cmdline, int nCmdShow);
		~MainUI();

		// Run the application
		int Run();

		// 30Hz step function
		void Step30Hz(double elapsed_seconds);

		// Reset the camera to view all, selected, or visible objects
		void ResetView(EObjectBounds view_type);

		// Render the 3D scene
		void Render(Control&, PaintEventArgs& args);

		// Request a render
		void RenderNeeded();

		// Enable/Disable full screen mode
		void FullScreenMode(bool enable_fullscreen);

		// The size of the window has changed
		void Resize(Control&, WindowPosEventArgs const& args);

		// Mouse/Key navigation/manipulation
		void MouseButton(Control&, MouseEventArgs& args);
		void MouseClick(Control&, MouseEventArgs& args);
		void MouseMove(Control&, MouseEventArgs& args);
		void MouseWheel(Control&, MouseWheelArgs& args);
		void KeyEvent(Control&, KeyEventArgs& args);

		// Handle menu and accelerator key events
		bool HandleMenu(UINT item_id, UINT event_source, HWND ctrl_hwnd) override;

		// Create a new text file for ldr script
		void CreateNewScript(std::wstring filepath = std::wstring());

		// Add a script file to the file sources collection
		void LoadScripts(std::vector<std::wstring> filepath, bool additive);

		// Reload all data
		void ReloadSourceData();

		// Open the built-in script editor
		void OpenScriptEditor();

		// Open a 3rd party text editor with the given files
		void OpenExternalTextEditor(StrList const& files);

		// Get/Set the control mode
		EControlMode ControlMode() const;
		void ControlMode(EControlMode mode);

		// View the current focus point looking down the selected axis
		void AlignCamToAxis(pr::v4 const& axis);

		// Set the position of the camera focus point in world space
		void ShowFocusPositionUI();

		// Display the lighting options UI
		void ShowLightingUI();

		// Display the object manager UI
		void ShowObjectManagerUI();

		// Display the options UI
		void ShowOptionsUI();

		// Check the web for the latest version
		void CheckForUpdates();

		// Create stock models such as the focus point, origin, etc
		void CreateStockModels();

		// Return the bounding box of objects in the current scene for the given bounds type
		pr::BBox GetSceneBounds(EObjectBounds bound_type) const;

		// Generate a scene containing the supported line drawer objects
		void CreateDemoScene();

		// Drop files onto the application
		void DropFiles(Control&, DropFilesEventArgs const& drop);

		// Ldr event handlers
		void OnEvent(Evt_Refresh const&) override;
		void OnEvent(Evt_AppMsg const&) override;
		void OnEvent(Evt_StoreChanging const&) override;
		void OnEvent(Evt_StoreChanged const&) override;
		void OnEvent(Evt_SettingsError const&) override;
		void OnEvent(Evt_UpdateScene const&) override;
		void OnEvent(Evt_SelectionChanged const&) override;
		void OnEvent(Evt_SettingsChanged const&) override;
		void OnEvent(Evt_RenderStepExecute const&) override;

		// Update the mouse coordinates in the status bar
		void MouseStatusUpdate(pr::v2 const& mouse_location);

		// Message map function
		bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override;

		// Set UI elements to reflect their current state
		void UpdateUI();
	};
}
