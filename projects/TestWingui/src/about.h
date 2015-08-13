#pragma once

#include "forward.h"

// About dialog
struct About :Form
{
	Button m_btn_ok;

	About()
		:Form(DlgParams().id(IDD_ABOUTBOX).name("about"))
		,m_btn_ok(Button::Params().id(IDOK).name("btn_ok").parent(this).anchor(EAnchor::BottomRight))
	{
		m_btn_ok.Click += [&](Button&, EmptyArgs const&){ Close(); };
	}
};

