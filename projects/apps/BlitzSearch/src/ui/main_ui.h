//***********************************************************************
// BlitzSearch
//  Copyright (c) Rylogic Ltd 2024
//***********************************************************************
#pragma once
#include "src/forward.h"
#include "res/resource.h"

namespace blitzsearch
{
	struct MainUI : ui::Form
	{
		ui::TextBox m_search_box;
		ui::ListView m_results;

		MainUI();
	};
}