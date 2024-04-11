//***********************************************************************
// BlitzSearch
//  Copyright (c) Rylogic Ltd 2024
//***********************************************************************
#include "src/forward.h"
#include "src/ui/main_ui.h"

namespace blitzsearch
{
	MainUI::MainUI()
		:ui::Form(Params<>()
			.name("main")
			.title(L"Blitz Search")
			.icon(IDR_MAINFRAME)
			.xy(1000,500)
			.wh(800, 300)
			.menu({{L"&File", ui::Menu(ui::Menu::EKind::Popup, {ui::MenuItem(L"E&xit", IDCLOSE)})}})
			.main_wnd(true)
			.dpi_aware(true)
			.dbl_buffer(true)
			.wndclass(RegisterWndClass<MainUI>()))
		,m_search_box(ui::TextBox::Params<>()
			.name("m_search_box")
			.parent(this_)
			.dock(EDock::Top)
			.margin(1))
		,m_results(ui::ListView::Params<>()
			.name("m_results")
			.parent(this_)
			.mode(ui::ListView::EViewType::Report)
			.add_column(ui::ListView::ColumnInfo(L"Hello").width(30).subitem(0))
			.add_column(ui::ListView::ColumnInfo(L"Boob").width(30).subitem(1))
			.add_column(ui::ListView::ColumnInfo(L"World").width(30).subitem(2))
			.dock(EDock::Fill)
			.bk_col(RGB(255, 255, 255))
			.dbl_buffer(true)
			.margin(1))
	{
		CreateHandle();
		assert(m_results.ColumnCount() == 3);
		m_results.InsertItem(ui::ListView::ItemInfo().text(L"Hello"));
	}
}