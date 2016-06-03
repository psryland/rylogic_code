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
		pr::gui::Button  m_btn_ok;
		pr::gui::TextBox m_tb_info;
		pr::gui::ImageBox m_img_icon;
		enum { ID_INFO = 100, ID_ICON };

		AboutUI()
			:Form(MakeDlgParams<>().title(L"About LineDrawer").wh(187, 64).dlu().start_pos(EStartPosition::CentreParent).icon(IDI_ICON_MAIN).wndclass(RegisterWndClass<AboutUI>()))
			,m_btn_ok  (pr::gui::Button  ::Params<>().parent(this_).id(IDOK).xy(130, 45).wh(50, 14).dlu().text(L"OK").def_btn().anchor(EAnchor::BottomRight))
			,m_tb_info (pr::gui::TextBox ::Params<>().parent(this_).id(ID_INFO).xy(33, 7).wh(147, 33).dlu().multiline().read_only().anchor(EAnchor::All))
			,m_img_icon(pr::gui::ImageBox::Params<>().parent(this_).id(ID_ICON).xy(7,7).wh(21,20).dlu().icon(IDI_ICON_MAIN).anchor(EAnchor::TopLeft))
		{
			CreateHandle();

			m_tb_info.Text(pr::Widen(AppString()));
			m_btn_ok.Click += [&](pr::gui::Button&, EmptyArgs const&){ Close(); };
		}
		void OnVisibilityChanged(VisibleEventArgs const& args) override
		{
		//	if (args.m_visible) m_tb_info.Text(pr::Widen(ldr::AppString()));
			Form::OnVisibilityChanged(args);
		}
	};
}