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
//#include "pr/app/gimble.h"

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
			return pr::filesys::CombinePath<wstring>(L"D:/Users/Paul/Rylogic/media", relpath);
		}
	};

	// Main app logic
	struct Main :pr::app::Main<UserSettings, MainGUI>
	{
		pr::app::Skybox m_skybox;
		//pr::app::Gimble m_gimble;
		
		wchar_t const* AppTitle() const { return L"Sol"; };
		
		Main(MainGUI& gui)
		:base(pr::app::DefaultSetup(), gui)
		,m_skybox(m_rdr, ResMgr::DataPath(L"skybox/galaxy1/galaxy??.dds"), pr::app::Skybox::SixSidedCube)
		//,m_gimble(m_rdr)
		{

		}
	};
	
	struct MainGUI :pr::app::MainGUI<MainGUI, Main>
	{
	};
}

// Create the GUI window
extern std::shared_ptr<ATL::CWindow> pr::app::CreateGUI(LPTSTR lpstrCmdLine)
{
	return CreateGUI<sol::MainGUI>(lpstrCmdLine);
}

#endif
