//***************************************************************************************************
// Lighting Dialog
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"

namespace ldr
{
	struct TextEntryUI :pr::gui::Form
	{
		pr::gui::TextBox m_tb;
		pr::gui::Button m_btn_cancel;
		pr::gui::Button m_btn_ok;
		pr::gui::Font m_font;
		std::wstring m_body;

		TextEntryUI(HWND parent, wchar_t const* title, wchar_t const* body, bool multiline)
			:Form(DlgParams<>().parent(parent).title(title).wh(132,46).start_pos(pr::gui::EStartPosition::CentreParent).tool_window().style(multiline?'-':'+',DS_MODALFRAME).style(multiline?'+':'-',WS_THICKFRAME))
			,m_tb        (pr::gui::TextBox::Params<>().parent(this_).xy(6, 7).wh(119, 15).multiline(multiline).want_return(multiline).style(multiline?'+':'-',WS_HSCROLL|WS_VSCROLL))
			,m_btn_cancel(pr::gui::Button ::Params<>().parent(this_).text(L"Cancel").id(IDCANCEL).xy(75, 27).wh(50, 14))
			,m_btn_ok    (pr::gui::Button ::Params<>().parent(this_).text(L"OK").id(IDOK).xy(20, 27).wh(50, 14).def_btn())
			,m_font(L"Courier New", 80)
			,m_body(body)
		{
			int tab_stop_size = 12;
			Throw(m_tb.SendMsg<bool>(LB_SETTABSTOPS, 1, &tab_stop_size), "Failed to set tab stop size");
			m_tb.Font(m_font);
			m_tb.Text(m_body);
			m_tb.Selection(pr::gui::RangeI(0, -1));
			m_tb.Focus();
		}
		bool Close(EDialogResult dialog_result) override
		{
			// Save the text that was entered
			m_body = m_tb.Text();
			return Form::Close(dialog_result);
		}
	};
}

