//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************
#pragma once
#ifndef LDR_LINEDRAWER_H
#define LDR_LINEDRAWER_H

#include "linedrawer/types/forward.h"
#include "linedrawer/types/ldrevent.h"
#include "linedrawer/main/user_settings.h"
#include "linedrawer/main/navmanager.h"
#include "linedrawer/main/file_sources.h"
#include "linedrawer/main/lua_source.h"
#include "linedrawer/plugin/plugin_manager.h"
#include "pr/linedrawer/ldr_object.h"
#include "pr/linedrawer/ldr_objects_dlg.h"
#include "pr/linedrawer/ldr_tools.h"

class LineDrawer
	:pr::cmdline::IOptionReceiver
	,pr::events::IRecv<pr::ldr::Evt_SettingsChanged>
	,pr::events::IRecv<pr::ldr::Evt_LdrObjectSelectionChanged>
	,pr::events::IRecv<pr::rdr::Evt_SceneRender>
{
	UserSettings                     m_user_settings;
	pr::Renderer                     m_renderer;
	pr::rdr::SceneForward            m_scene;
	pr::Camera                       m_camera;
	pr::ldr::LdrObjectStepData::Link m_step_objects;
	pr::ldr::StockInstance           m_focus_point;
	pr::ldr::StockInstance           m_origin_point;
	pr::ldr::StockInstance           m_selection_box;
	pr::ldr::StockInstance           m_bbox_model;
	pr::ldr::StockInstance           m_test_point;
	bool                             m_test_point_enable;

	// Callback function for reading a point in world space
	static pr::v4 __stdcall ReadPoint(void* ctx);

	// Create stock models such as the focus point, origin, etc
	void CreateStockModels();

	// Render a viewport
	void RenderViewport(pr::rdr::Viewport& viewport);

	// Command line
	bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end);

	// Event handlers
	void OnEvent(pr::ldr::Evt_SettingsChanged const&);
	void OnEvent(pr::ldr::Evt_LdrObjectAdd const&);
	void OnEvent(pr::ldr::Evt_LdrObjectDelete const&);
	void OnEvent(pr::ldr::Evt_LdrObjectSelectionChanged const&);
	void OnEvent(pr::rdr::Evt_SceneRender const&);

public:
	// These types form part of the interface 
	LineDrawerGUI&              m_ui;               // The main window ui
	pr::Renderer&               m_rdr;              // The renderer
	NavManager                  m_nav;              // Controls user input and camera navigation
	pr::ldr::ObjectCont         m_store;            // A container of all ldr objects created
	pr::ldr::ObjectManagerDlg   m_store_ui;         // GUI window for manipulating ldr object properties
	pr::ldr::MeasureDlg         m_measure_tool_ui;  // The UI for the measuring tool
	pr::ldr::AngleDlg           m_angle_tool_ui;    // The UI for the angle measuring tool
	PluginManager               m_plugin_mgr;       // The container of loaded plugins
	LuaSource                   m_lua_src;          // An object for processing lua files
	FileSources                 m_files;            // Manages files that are sources of ldr objects

	LineDrawer(LineDrawerGUI& ui, char const* cmdline);
	~LineDrawer();

	// Return access to the user settings
	UserSettings const& Settings() const;
	UserSettings&       Settings();

	// Reload all data
	void ReloadSourceData();

	// Reset the camera to view all, selected, or visible objects
	void ResetView(pr::ldr::EObjectBounds view_type);

	// Generate a scene containing the supported line drawer objects
	void CreateDemoScene();

	// Update the display
	void Render();

	// Set the view area of the output
	void SetViewArea(pr::IRect client_area);

	// Test point methods
	void TestPoint_Enable(bool yes);
	void TestPoint_SetPosition(pr::v4 const& pos);
};

#endif
