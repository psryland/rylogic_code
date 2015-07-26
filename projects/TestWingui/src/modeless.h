#pragma once

#include "forward.h"

// Modeless dialog
struct Modeless :Form<Modeless>
{
	Label m_lbl;
	Button m_btn_ok;

	enum { IDC_LBL };
	Modeless(ParentRef parent)
		:Form<Modeless>(L"Modeless", parent, 0, 0, CW_USEDEFAULT, CW_USEDEFAULT, DefaultStyle, DefaultStyleEx, IDC_MENU, "modeless")
		,m_lbl(L"I am a modeless dialog", 10, 10, Auto, Auto, IDC_LBL, this, EAnchor::TopLeft, Label::DefaultStyle, Label::DefaultStyleEx, "modeless-label")
		,m_btn_ok(IDOK, this, EAnchor::BottomRight, "ok_btn")
	{
		HideOnClose(true);
		m_btn_ok.Click += [&](Button&, EmptyArgs const&){ Close(); };
	}
};
