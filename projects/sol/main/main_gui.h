//*****************************************************************************************
// Sol
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#ifndef PR_SOL_MAIN_MAIN_GUI_H
#define PR_SOL_MAIN_MAIN_GUI_H

#include "sol/main/forward.h"
#include "sol/main/main.h"

namespace sol
{
	struct MainGUI
		:pr::app::MainGUI<MainGUI, Main, pr::SimMsgLoop>
	{
		typedef pr::app::MainGUI<MainGUI, Main, pr::SimMsgLoop> base;
		static char const* AppName() { return "Sol"; }

		explicit MainGUI(LPTSTR)
		{
		}
		virtual LRESULT OnCreate(LPCREATESTRUCT create)
		{
			pr::Throw(base::OnCreate(create));
			m_msg_loop.AddStepContext("sol main loop", [this](double){ m_main->DoRender(true); }, 60.0f, false);
			return S_OK;
		}
		virtual void OnKeyUp(UINT nChar, UINT, UINT)
		{
			if (nChar == 'W' && pr::KeyDown(VK_CONTROL))
			{
				m_main->ToggleWireframe();
				return;
			}
			if (nChar == 'S' && pr::KeyDown(VK_CONTROL))
			{
				m_main->ToggleStereo();
				return;
			}
			SetMsgHandled(FALSE);
		}
		BEGIN_MSG_MAP(x)
			MSG_WM_KEYUP(OnKeyUp)
			CHAIN_MSG_MAP(base)
		END_MSG_MAP()
	};
}

// Create the GUI window
extern std::shared_ptr<ATL::CWindow> pr::app::CreateGUI(LPTSTR lpstrCmdLine)
{
	return CreateGUI<sol::MainGUI>(lpstrCmdLine);
}

#endif
