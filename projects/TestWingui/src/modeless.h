#pragma once

#include "forward.h"

// Modeless dialog
struct Modeless :Form
{
	Label m_lbl;
	Button m_btn_ok;

	enum { IDC_LBL };
	Modeless(WndRef parent)
		:Form(RegisterWndClass<Modeless>(), L"Modeless", "modeless", parent, 0, 0, DefW, DefH, DefaultFormStyle, DefaultFormStyleEx, IDC_MENU)
		,m_lbl(L"I am a modeless dialog", "modeless-label", 10, 10, Auto, Auto, IDC_LBL, this, EAnchor::TopLeft, Label::DefaultStyle, Label::DefaultStyleEx)
		,m_btn_ok(L"OK", "btn_ok", -10, -10, Auto, Auto, IDOK, this, EAnchor::BottomRight)
	{
		HideOnClose(true);
		m_btn_ok.Click += [&](Button&, EmptyArgs const&){ Close(); };
	}
};
