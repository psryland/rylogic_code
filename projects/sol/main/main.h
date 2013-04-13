//*****************************************************************************************
// Sol
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#ifndef PR_SOL_H
#define PR_SOL_H

#include "sol/main/forward.h"
#include "pr/app/main_gui.h"
#include "pr/app/main.h"
#include "pr/app/skybox.h"
#include "pr/app/gimble.h"
#include "pr/app/sim_message_loop.h"

namespace sol
{
	// Dummy settings type
	struct UserSettings
	{
		UserSettings(void*) {}
	};

	// Resource manager
	struct ResMgr
	{
		static wstring DataPath(wstring const& relpath)
		{
			return pr::filesys::CombinePath<wstring>(L"Q:\\local\\media", relpath);
		}
	};

	struct TestModel :pr::events::IRecv<pr::rdr::Evt_SceneRender>
	{
		// A renderer instance type for the skybox
		PR_RDR_DECLARE_INSTANCE_TYPE2
		(
			Instance
			,pr::m4x4            ,m_i2w   ,pr::rdr::EInstComp::I2WTransform
			,pr::rdr::ModelPtr   ,m_model ,pr::rdr::EInstComp::ModelPtr
		);

		Instance m_inst;  // The skybox instance

		TestModel(pr::Renderer& rdr)
		{
			pr::Array<pr::v4> points;
			for (int i = 0; i != 20; ++i)
				points.push_back(pr::Random4(pr::v4Origin, 5.0f));

			// Create the model
			//m_inst.m_model = pr::rdr::model::BoxList(rdr, points.size(), &points[0], pr::v4::make(0.1f, 0.2f, 0.05f,0), 1, &pr::Colour32Red);
			m_inst.m_model = pr::rdr::model::Box(rdr, pr::v4::make(0.2f,0.2f,0.2f,0.0f), pr::m4x4Identity, pr::Colour32Red);
			m_inst.m_i2w = pr::Translation(0,0,0);
		}

		// Add the skybox to a viewport
		void OnEvent(pr::rdr::Evt_SceneRender const& e)
		{
			e.m_scene->AddInstance(m_inst);
		}
	};

	// Main app logic
	struct Main :pr::app::Main<UserSettings, MainGUI>
	{
		//pr::app::Skybox m_skybox;
		pr::app::Gimble m_gimble;
		TestModel m_test;

		wchar_t const* AppTitle() const { return L"Sol"; };

		Main(MainGUI& gui)
		:base(pr::app::DefaultSetup(), gui)
		//,m_skybox(m_rdr, ResMgr::DataPath(L"skybox/galaxy1/galaxy??.dds"), pr::app::Skybox::SixSidedCube)
		//,m_skybox(m_rdr, ResMgr::DataPath(L"skybox/sky1/skybox-clouds-few-noon.dds"), pr::app::Skybox::FiveSidedCube)
		,m_gimble(m_rdr)
		,m_test(m_rdr)
		{
			// Enable wireframe
			//m_scene.m_rs = m_rdr.m_rs_mgr.RasterState(pr::rdr::ERasterState::WireCullNone);
			m_scene.m_stereoscopic = true;
		}
	};

	struct MainGUI :pr::app::MainGUI<MainGUI, Main, pr::SimMsgLoop>
	{
		virtual LRESULT OnCreate(LPCREATESTRUCT create)
		{
			pr::Throw(base::OnCreate(create));
			m_msg_loop.AddStepContext([this](double){ m_main->DoRender(true); }, 60.0f, false);
			return S_OK;
		}
	};
}

// Create the GUI window
extern std::shared_ptr<ATL::CWindow> pr::app::CreateGUI(LPTSTR lpstrCmdLine)
{
	return CreateGUI<sol::MainGUI>(lpstrCmdLine);
}

#endif
