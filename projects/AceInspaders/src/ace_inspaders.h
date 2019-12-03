#pragma once

#include "pr/app/forward.h"
#include "pr/app/main.h"
#include "pr/app/main_gui.h"
#include "pr/app/default_setup.h"
#include "pr/gui/sim_message_loop.h"

namespace ace
{
	struct Main;
	struct MainGUI;

	// Create a UserSettings object for loading/saving app settings
	struct UserSettings { explicit UserSettings(int) {} };

	// Derive a application logic type from pr::app::Main
	struct Main :pr::app::Main<UserSettings, MainGUI>
	{
		static wchar_t const* AppName() { return L"AceInspaders"; };
		Main(MainGUI& gui)
			:pr::app::Main<UserSettings, MainGUI>(pr::app::DefaultSetup(), gui)
		{}
	};

	// Derive a GUI class from pr::app::MainGUI
	struct MainGUI :pr::app::MainGUI<MainGUI, Main, pr::gui::SimMsgLoop>
	{
		static wchar_t const* AppTitle() { return L"Ace Inspaders"; };
		MainGUI(wchar_t const*, int)
			:pr::app::MainGUI<MainGUI, Main, pr::gui::SimMsgLoop>(Params().title(AppTitle()))
		{}
	};
}

namespace pr::app
{
	// Create the GUI window
	inline std::unique_ptr<IAppMainGui> CreateGUI(wchar_t const* lpstrCmdLine, int nCmdShow)
	{
		return std::unique_ptr<IAppMainGui>(new ace::MainGUI(lpstrCmdLine, nCmdShow));
	}
}
