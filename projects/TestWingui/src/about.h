#pragma once

#include "forward.h"

// About dialog
struct About :Form<About>
{
	Button m_btn_ok;

	About()
		:Form<About>(IDD_ABOUTBOX, "about")
		,m_btn_ok(IDOK, "btn_ok", this, EAnchor::BottomRight)
	{
		m_btn_ok.Click += [&](Button&, EmptyArgs const&){ Close(); };
	}
};

