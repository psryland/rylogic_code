#pragma once
#include "KeySpy/src/forward.h"

namespace keyspy
{
	namespace g = pr::gui;
	struct ControlsUI :g::Form
	{
		enum EID
		{
			BtnOk = IDOK,
			BtnShowData,
		};

		g::Button m_btn_ok;
		g::Button m_btn_showdata;

		ControlsUI(g::WndRef parent)
			:g::Form(g::MakeDlgParams<>().name("KeySpy").title(L"KeySpy").parent(parent).start_pos(g::EStartPosition::CentreParent))
			,m_btn_ok(g::Button::Params<>().parent(this_).text(L"OK").xy(-10, -1).id(EID::BtnOk).anchor(g::EAnchor::BottomRight))
			,m_btn_showdata(g::Button::Params<>().parent(this_).text(L"Show Data").id(EID::BtnShowData).anchor(g::EAnchor::BottomRight))
		{
			m_btn_ok.Click += [&](g::Button&, g::EmptyArgs const&) { Close(); };
		}

	};
}