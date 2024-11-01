#pragma once
#include "src/forward.h"
#include "src/view_base.h"

namespace ele
{
	struct ViewIntro
		:ViewBase
		,pr::events::IRecv<Console::Evt_KeyDown>
	{
		int           m_page;            // The page number of the intro
		pr::seconds_t m_display_time;    // The length of time that the current page has been displayed for

		ViewIntro(pr::Console& cons, GameInstance& inst);

		// Step the view, returns the next view to display
		EView Step(double elapsed);

		void Render() const;

		void OnEvent(pr::console::Evt_KeyDown const& e);
	};
}