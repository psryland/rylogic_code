#pragma once

#include "elements/forward.h"
#include "elements/view_base.h"

namespace ele
{
	struct ViewHome
		:ViewBase
		,pr::events::IRecv<pr::console::Evt_Line<char>>
	{
		pr::seconds_t m_countdown_redraw_timer;
		ViewHome(pr::Console& cons, GameInstance& inst);

		EView Step(pr::seconds_t elapsed);

		// Step the view, returns the next view to display
		void Render() const;
		void RenderCountdown() const;
		void RenderResearchStatus() const;
		void RenderShipSpec() const;

		void OnEvent(pr::console::Evt_Line<char> const& e);
	};
}