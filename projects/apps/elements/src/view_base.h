#pragma once

#include "elements/forward.h"

namespace ele
{
	struct ViewBase
	{
		enum { TitleHeight = 2, MenuHeight = 10 };
		using Console = pr::Console<char>;

		Console&      m_cons;
		GameInstance& m_inst;
		EView         m_view;   // The view id that will next be returned from Step
		const int     m_panel_width;
		const int     m_panel_height;

		ViewBase(Console& cons, GameInstance& inst);
		virtual ~ViewBase();

		// Step the view, returns the next view to display
		virtual EView Step(pr::seconds_t) { return m_view; }

		// Render the view
		virtual void Render() const {}

		// Common menu options/handling
		void RenderMenu(EView this_view, strvec_t const& options) const;
		void HandleOption(EView this_view, std::string option);

		// Common handling of key events
		void HandleKeyEvent(EView this_view, pr::console::Evt_KeyDown const& e);

		PR_NO_COPY(ViewBase);
	};
}