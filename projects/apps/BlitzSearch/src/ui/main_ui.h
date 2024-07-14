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
		ui::Panel m_panel_search;
		ui::TextBox m_tb_search;
		ui::Button m_btn_search;
		ui::ListView m_results;
		ui::Button m_btn;

		MainIndex& m_main_index;

		explicit MainUI(MainIndex& main_index);
		MainUI(MainUI&&) = delete;
		MainUI(MainUI const&) = delete;
		MainUI& operator =(MainUI&&) = delete;
		MainUI& operator =(MainUI const&) = delete;
	};
}