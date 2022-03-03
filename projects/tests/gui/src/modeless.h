#pragma once
#include "src/forward.h"

namespace pr::gui
{
	// Modeless dialog
	struct Modeless :Form
	{
		Label m_lbl;
		Button m_btn_ok;

		enum { IDC_LBL };
		Modeless(WndRef parent)
			:Form(Params<>().dlg().name("modeless").title(L"Modeless").parent(parent).menu(IDC_MENU).wh(400,400).start_pos(EStartPosition::CentreParent).wndclass(RegisterWndClass<Modeless>()))
			,m_lbl   (Label ::Params<>().parent(this_).name("modeless-label").text(L"I am a modeless dialog").wh(Auto,Auto).xy(10,10).id(IDC_LBL).anchor(EAnchor::TopLeft))
			,m_btn_ok(Button::Params<>().parent(this_).name("btn_ok").text(L"OK").xy(-10,-10).id(IDOK).anchor(EAnchor::BottomRight))
		{
			HideOnClose(true);
			m_btn_ok.Click += [&](Button&, EmptyArgs const&){ Close(); };
		}

		// Default main menu handler
		// 'menu_item_id' - the menu item id or accelerator id
		// 'event_source' - 0 = menu, 1 = accelerator, 2 = control-defined notification code
		// 'ctrl_hwnd' - the control that sent the notification. Only valid when src == 2
		// Typically you'll only need 'menu_item_id' unless your accelerator ids
		// overlap your menu ids, in which case you'll need to check 'event_source'
		bool HandleMenu(UINT item_id, UINT, HWND) override
		{
			// Example implementation
			switch (item_id)
			{
			default: return false;
			case IDM_EXIT: Close(); return true;
			}
		}
	};
}