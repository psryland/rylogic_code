#pragma once

#include "elements/forward.h"
#include "elements/iview.h"

namespace ele
{
	struct ViewHome :IView
	{
		// A clock for triggering redraws of display elements
		double m_redraw_timer;

		ViewHome(pr::Console& cons, GameInstance& inst);

		// Step the view, returns the next view to display
		EView Step(double elapsed);

		void RenderCountdown() const;
		void RenderResearchStatus() const;
		void RenderShipSpec() const;
		void RenderMenu() const;
	};
}