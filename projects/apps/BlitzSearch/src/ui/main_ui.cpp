//***********************************************************************
// BlitzSearch
//  Copyright (c) Rylogic Ltd 2024
//***********************************************************************
#include "src/forward.h"
#include "src/index.h"
#include "src/ui/main_ui.h"

namespace blitzsearch
{
	MainUI::MainUI(MainIndex& main_index)
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
		,m_panel_search(ui::Panel::Params<>()
			.name("m_panel_search")
			.parent(this_)
			.dock(EDock::Top)
			.h(24)
		)
		,m_tb_search(ui::TextBox::Params<>()
			.name("m_tb_search")
			.parent(&m_panel_search)
			.dock(EDock::Left)
			.multiline(false)
			.w(200)
			.margin(1)
		)
		,m_btn_search(ui::Button::Params<>()
			.name("m_btn_search")
			.parent(&m_panel_search)
			.dock(EDock::Right)
			.w(40)
			.text(L"Search")
		)
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
			.margin(1)
			.visible(false)
		)
		,m_btn(ui::Button::Params<>()
			.name("m_btn")
			.parent(this_)
			.text(L"Add File")
			.dock(EDock::Top)
		)
		,m_main_index(main_index)
	{
		CreateHandle();
		assert(m_results.ColumnCount() == 3);
		m_results.InsertItem(ui::ListView::ItemInfo().text(L"Hello"));

		//hack - manually add a file to search
		auto filepath = std::filesystem::path{ "E:\\Dump\\test.txt" };
		if (std::filesystem::exists(filepath))
			m_main_index.AddFile(filepath);

		m_btn_search.Click += [&](ui::Button&, ui::EmptyArgs const&)
		{
			auto text = ui::Narrow(m_tb_search.Text());
			auto pattern = std::span<uint8_t const>(
				reinterpret_cast<uint8_t const*>(text.c_str()), text.size());

			m_main_index.Search(pattern);
		};
	}
}