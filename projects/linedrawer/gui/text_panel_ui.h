//***************************************************************************************************
// Lighting Dialog
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"

namespace ldr
{
	struct TextEntryUI :Form
	{
		Panel m_panel_btns;
		Button m_btn_cancel;
		Button m_btn_ok;
		TextBox m_tb;
		pr::gui::Font m_font;
		std::wstring m_body;

		TextEntryUI(HWND parent, wchar_t const* title, wchar_t const* body, bool multiline)
			:Form(MakeDlgParams<>()
				.parent(parent).name("text-entry-ui")
				.title(title).start_pos(EStartPosition::CentreParent).wh(280, multiline ? 400 : 140)
				.style('-',WS_MINIMIZEBOX|WS_MAXIMIZEBOX)
				.style(multiline?'-':'+',DS_MODALFRAME)
				.style(multiline?'+':'-',WS_THICKFRAME)
				.tool_window())
			,m_panel_btns(Panel  ::Params<>().parent(this_).dock(EDock::Bottom).wh(Fill,32))
			,m_btn_cancel(Button ::Params<>().parent(&m_panel_btns).dock(EDock::Right).text(L"Cancel").dlg_result(EDialogResult::Cancel))
			,m_btn_ok    (Button ::Params<>().parent(&m_panel_btns).dock(EDock::Right).text(L"OK").dlg_result(EDialogResult::Ok).def_btn())
			,m_tb        (TextBox::Params<>().parent(this_).dock(EDock::Fill).multiline(multiline).want_return(multiline).style(multiline?'+':'-',WS_HSCROLL|WS_VSCROLL))
			,m_font(L"Courier New", 80, nullptr)
			,m_body(body)
		{
			CreateHandle();

			if (multiline)
			{
				int tab_stop_size = 12;
				Throw(m_tb.SendMsg<bool>(EM_SETTABSTOPS, 1, &tab_stop_size), "Failed to set tab stop size");
			}
			//m_tb.Font(m_font);
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

