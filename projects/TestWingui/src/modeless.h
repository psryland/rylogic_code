#pragma once

#include "forward.h"

// Modeless dialog
struct Modeless :Form<Modeless>
{
	Button m_btn_ok;

	Modeless()
		:Form<Modeless>(IDD_DLG, "modeless")
		,m_btn_ok(IDOK, this, "ok_btn", EAnchor::Bottom|EAnchor::Right)
	{
		m_btn_ok.Click += [&](Button&, EmptyArgs const&){ Close(); };
	}
};
