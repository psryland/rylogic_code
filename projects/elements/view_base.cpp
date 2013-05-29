#include "elements/stdafx.h"
#include "elements/view_base.h"
#include "elements/game_instance.h"

using namespace pr::console;

namespace ele
{
	ViewBase::ViewBase(pr::Console& cons, GameInstance& inst)
		:m_cons(cons)
		,m_inst(inst)
		,m_view(EView::SameView)
		,m_panel_width(width (m_cons.Info().srWindow) / 2 - 1)
		,m_panel_height(height(m_cons.Info().srWindow) - TitleHeight - MenuHeight - 2)
	{
		m_cons.Clear();
		m_cons.Cursor(EAnchor::BottomLeft, 3, 0); // Set the input location. Rendering should not change this
	}
	ViewBase::~ViewBase()
	{}

	void ViewBase::RenderMenu(EView this_view, strvec_t const& options) const
	{
		Pad pad(EColour::Green);
		pad.Title("== Menu ==", Colours(EColour::Black), EAnchor::Left);
		
		const int column_width = 30;
		
		// Add navigation options
		std::string s;
		if (this_view != EView::Home       ) s.append(" H - Home                   \n");
		if (this_view != EView::ShipDesign ) s.append(" S - Ship Design            \n");
		if (this_view != EView::MaterialLab) s.append(" M - Materials Lab          \n");
		if (this_view != EView::Launch     ) s.append(" L - Launch Ship (end game) \n");
		pad << s << Coord(column_width,0);
		
		// Add custom options
		s.resize(0);
		for (auto& i : options) s.append(" ").append(i).append("\n");
		pad << s;

		// Add the prompt and existing input
		pad << Coord(0,3) << "=> " << EColour::Black << Pad::CurrentInput;
		
		pad.AutoSize();
		pad.Draw(m_cons, EAnchor::BottomLeft);
	}

	void ViewBase::HandleOption(EView this_view, std::string option)
	{
		if (option == "h" && this_view != EView::Home       ) m_view = EView::Home;
		if (option == "s" && this_view != EView::ShipDesign ) m_view = EView::ShipDesign;
		if (option == "m" && this_view != EView::MaterialLab) m_view = EView::MaterialLab;
		if (option == "l" && this_view != EView::Launch     ) m_view = EView::Launch;
		if (option == "") Render();
	}

	void ViewBase::HandleKeyEvent(EView this_view, pr::console::Evt_KeyDown const& e)
	{
		if (e.m_key.wVirtualKeyCode == VK_ESCAPE && this_view != EView::Home)
		{
			m_view = EView::Home;
			return;
		}
	}
}