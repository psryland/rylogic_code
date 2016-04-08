//***************************************************************************************************
// Lighting Dialog
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"

namespace ldr
{
	struct AboutUI :pr::gui::Form
	{
		pr::gui::TextBox m_tb_info;
		pr::gui::Button  m_btn_ok;

		AboutUI()
			:Form(DlgParams<>().title(L"About LineDrawer").wh(187, 64).start_pos(EStartPosition::CentreParent))
			,m_tb_info(pr::gui::TextBox::Params<>().parent(this_).xy(33, 7).wh(147, 33).multiline().read_only().anchor(EAnchor::All))
			,m_btn_ok (pr::gui::Button ::Params<>().parent(this_).text(L"OK").id(IDOK).xy(130, 45).wh(50, 14).def_btn().anchor(EAnchor::BottomRight))
			//    ICON            IDI_ICON_MAIN, IDC_STATIC, 7, 7, 21, 20, SS_ICON, WS_EX_LEFT
		{}
		void OnVisibilityChanged(VisibleEventArgs const& args) override
		{
		//	if (args.m_visible) m_tb_info.Text(pr::Widen(ldr::AppString()));
			Form::OnVisibilityChanged(args);
		}
	};
}