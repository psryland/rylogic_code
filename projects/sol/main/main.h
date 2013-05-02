//*****************************************************************************************
// Sol
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#ifndef PR_SOL_MAIN_MAIN_H
#define PR_SOL_MAIN_MAIN_H

#include "sol/main/forward.h"
#include "sol/objects/astronomical_body.h"
#include "sol/objects/test_model.h"

namespace sol
{
	// Dummy settings type
	struct UserSettings
	{
		UserSettings(void*) {}
	};

	// Main app logic
	struct Main
		:pr::app::Main<UserSettings, MainGUI>
		,pr::events::IRecv<pr::rdr::Evt_SceneRender>
	{
		pr::app::Skybox m_skybox;
		pr::app::Gimble m_gimble;
		//AstronomicalBody m_earth;
		//AstronomicalBody m_earth2;
		//AstronomicalBody m_earth3;
		//TestModel m_test1;
		//TestModel m_test2;
		bool m_wireframe;

		Main(MainGUI& gui);

		wchar_t const* AppTitle() const { return L"Sol"; };

		// Add to a viewport
		void OnEvent(pr::rdr::Evt_SceneRender const& e)
		{
			e.m_scene->m_global_light.m_direction = e.m_scene->m_view.m_c2w * pr::v4::normal3(-1.0f, -2.0f, -3.0f, 0.0f);
		}

		void ToggleWireframe()
		{
			m_wireframe = !m_wireframe;
			m_scene.m_rsb = m_wireframe ? pr::rdr::RSBlock::WireCullNone() : pr::rdr::RSBlock::SolidCullBack();
		}
		void ToggleStereo()
		{
			m_scene.Stereoscopic(!m_scene.Stereoscopic(), 0.1f, false);
		}
	};
}

#endif
