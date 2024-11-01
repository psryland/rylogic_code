#pragma once
#include "src/forward.h"
#include "src/view_base.h"

namespace ele
{
	struct ViewShip
		:ViewBase
		//,pr::events::IRecv<pr::console::Evt_Line<char>>
		,pr::events::IRecv<pr::console::Evt_KeyDown>
	{
		ViewShip(pr::Console& cons, GameInstance& inst);

		//virtual void OnEvent(pr::console::Evt_Line<char> const& e) { Input(e.m_input); }
		void OnEvent(pr::console::Evt_KeyDown const& e) { ViewBase::HandleKeyEvent(EView::ShipDesign, e); }
	};
}