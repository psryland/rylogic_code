//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************
#pragma once
#ifndef LDR_LINEDRAWER_H
#define LDR_LINEDRAWER_H

#include "linedrawer/main/forward.h"
#include "linedrawer/main/ldrevent.h"
#include "linedrawer/main/user_settings.h"
#include "linedrawer/main/navmanager.h"
#include "linedrawer/main/file_sources.h"
#include "linedrawer/main/lua_source.h"
#include "linedrawer/plugin/plugin_manager.h"

namespace ldr
{
	struct Main
		:pr::app::Main<UserSettings, MainGUI>
		,pr::events::IRecv<pr::ldr::Evt_SettingsChanged>
		,pr::events::IRecv<pr::ldr::Evt_LdrObjectSelectionChanged>
		,pr::events::IRecv<pr::rdr::Evt_SceneRender>
	{
		// These types form part of the interface
		NavManager                  m_nav;              // Controls user input and camera navigation
		pr::ldr::ObjectCont         m_store;            // A container of all ldr objects created
		PluginManager               m_plugin_mgr;       // The container of loaded plugins
		LuaSource                   m_lua_src;          // An object for processing lua files
		FileSources                 m_files;            // Manages files that are sources of ldr objects

		pr::ldr::LdrObjectStepData::Link m_step_objects;
		pr::ldr::StockInstance           m_focus_point;
		pr::ldr::StockInstance           m_origin_point;
		pr::ldr::StockInstance           m_selection_box;
		pr::ldr::StockInstance           m_bbox_model;
		pr::ldr::StockInstance           m_test_model;
		bool                             m_test_model_enable;

		wchar_t const* AppTitle() const { return ldr::AppTitleW(); }

		explicit Main(MainGUI& gui);
		~Main();

		// Reset the camera to view all, selected, or visible objects
		void ResetView(pr::ldr::EObjectBounds view_type);

		// Update the display
		void DoRender(bool force = false);

		// Reload all data
		void ReloadSourceData();

		// Generate a scene containing the supported line drawer objects
		void CreateDemoScene();

		// The size of the window has changed
		virtual void Resize(pr::iv2 const& size);

	private:
		// Create stock models such as the focus point, origin, etc
		void CreateStockModels();

		// Event handlers
		void OnEvent(pr::ldr::Evt_SettingsChanged const&);
		void OnEvent(pr::ldr::Evt_LdrObjectAdd const&);
		void OnEvent(pr::ldr::Evt_LdrObjectDelete const&);
		void OnEvent(pr::ldr::Evt_LdrObjectSelectionChanged const&);
		void OnEvent(pr::rdr::Evt_SceneRender const&);
	};
}

#endif
