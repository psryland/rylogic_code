#pragma once

#include "elements/forward.h"
#include "elements/iview.h"

namespace ele
{
	struct ViewIntro :IView
	{
		// The page number of the intro
		int m_page;

		// The length of time that the current page has been displayed for
		double m_display_time;

		ViewIntro(pr::Console& cons, GameInstance& inst);

		// Step the view, returns the next view to display
		EView Step(double elapsed);
	};
}