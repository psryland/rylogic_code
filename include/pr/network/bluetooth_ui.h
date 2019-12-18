//*****************************************************************************
// Bluetooth UI
// Copyright (c) Rylogic Ltd 2016
//*****************************************************************************
#pragma once

#ifndef _WIN32_WINNT
#define _WIN32_WINNT _WIN32_WINNT_WIN7
#elif _WIN32_WINNT < _WIN32_WINNT_WIN7
#error "_WIN32_WINNT >= _WIN32_WINNT_WIN7 required"
#endif

#include "pr/hardware/find_bt_device.h"
#include "pr/network/bluetooth.h"
#include "pr/gui/wingui.h"

namespace pr::network
{
	// A custom control for picking/pairing bluetooth devices
	struct BTDevicePickerCtrl :pr::gui::Control
	{
		enum { DefW = 120, DefH = 120 };
		enum :DWORD { DefaultStyle   = (pr::gui::DefaultControlStyle | WS_GROUP | SS_LEFT | SS_NOPREFIX) & ~(WS_TABSTOP | WS_CLIPSIBLINGS) };
		enum :DWORD { DefaultStyleEx = pr::gui::DefaultControlStyleEx };
		static wchar_t const* WndClassName() { return L"pr::gui::BluetoothDevicePicker"; }

		template <typename Derived = void>
		struct Params :Control::Params<not_void_t<Derived, Params<Derived>>>
		{
			Params()
			{
				this->wndclass(RegisterWndClass<BTDevicePickerCtrl>())
					.name("bt_device_picker")
					.wh(Auto, Auto)
					.style('=', DefaultStyle)
					.style_ex('=', DefaultStyleEx);
			}
		};

		pr::gui::Label m_lbl_enable_bt;
		pr::gui::Button m_chk_enable_bt;
		pr::gui::ListView m_lv_devices;

		// Construct
		BTDevicePickerCtrl()
			:BTDevicePickerCtrl(Params<>()) {}
		template <typename TParams>
		explicit BTDevicePickerCtrl(TParams const& p)
			:Control(p)
			,m_lbl_enable_bt(pr::gui::Label::Params<>().parent(this_).text(L"Enable Bluetooth"))
			,m_chk_enable_bt(pr::gui::Button::Params<>().parent(this_).chk_box())
			//,m_lv_devices(pr::gui::ListView::Params<>().parent(this_).wh(Fill,Fill).mode(pr::gui::ListView::EViewType::List))
		{}
	};

	// A dialog for choosing blue tooth devices
	struct BTDevicePickerUI :pr::gui::Form
	{
		pr::gui::Panel  m_panel_buttons;
		pr::gui::Button m_btn_cancel;
		pr::gui::Button m_btn_ok;
		BTDevicePickerCtrl m_bt_picker;

		BTDevicePickerUI()
			:pr::gui::Form(Params<>().dlg().name("bt-device-ui").start_pos(EStartPosition::CentreParent).title(L"Choose a Bluetooth Device").main_wnd(true))
			,m_panel_buttons(pr::gui::Panel::Params<>().name("panel-btns").parent(this_).wh(Fill, 36).dock(EDock::Bottom).margin(3))
			,m_btn_cancel(pr::gui::Button::Params<>().name("btn-cancel").parent(&m_panel_buttons).dock(EDock::Right).text(L"Cancel"))
			,m_btn_ok(pr::gui::Button::Params<>().name("btn-ok").parent(&m_panel_buttons).dock(EDock::Right).text(L"OK"))
			,m_bt_picker(BTDevicePickerCtrl::Params<>().name("bt-device-ctrl").parent(this_).dock(EDock::Fill).margin(3))
		{
			m_btn_ok.Click += [&](pr::gui::Button&, pr::gui::EmptyArgs const&){};
		}
	};
}
