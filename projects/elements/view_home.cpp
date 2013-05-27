#include "elements/stdafx.h"
#include "elements/view_home.h"
#include "elements/iview.h"
#include "elements/game_instance.h"

using namespace pr::console;

namespace ele
{
	const pr::seconds_t redraw_time = 1.0;
	const int PadRightSideWidth = 60;
	const int PadCountDownDy = 3;
	int PadCountDownHeight;
	int PadResourceHeight;

	ViewHome::ViewHome(pr::Console& cons, GameInstance& inst)
		:IView(cons, inst)
		,m_redraw_timer(redraw_time)
	{
		m_cons.Clear();
		
		// Set the input location. Rendering should not change this
		m_cons.Cursor(EAnchor::BottomLeft, 3, 0);
	}

	EView ViewHome::Step(double elapsed)
	{
		m_redraw_timer += elapsed;
		if (m_redraw_timer > redraw_time)
		{
			m_redraw_timer = 0;
			
			CursorScope s0(m_cons);
			RenderCountdown();
			RenderResearchStatus();
			RenderShipSpec();
			RenderMenu();
		}
		return EView::SameView;
	}

	void ViewHome::Input(std::string const& line)
	{
		//m_inputCurren
		OutputDebugStringA(pr::FmtS("Input: %s\n", line.c_str()));
	}

	void ViewHome::RenderCountdown() const
	{
		Pad pad;
		pad.Title(" World State ");
		pad.Border(EColour::BrightRed);
		pad << EColour::Red       << "       Time till nova: "
			<< EColour::BrightRed << pr::datetime::ToCountdownString(m_inst.m_world_state.m_time_till_nova, pr::datetime::EMaxUnit::Days);

		pad.m_width = PadRightSideWidth;
		m_cons.Write(EAnchor::TopRight, pad, 0, PadCountDownDy);
		PadCountDownHeight = pad.Size().cy;
	}

	void ViewHome::RenderResearchStatus() const
	{
		auto& ws = m_inst.m_world_state;
		auto& consts = m_inst.m_consts;
		
		Colours blue(EColour::Blue);
		Colours bblue(EColour::BrightBlue);

		Pad pad;
		pad.Title(" Research ");
		pad.Border(EColour::Black);
		
		{// star mass
			auto& r = ws.m_star_mass_research;
			pad << EColour::Blue << "Star Mass:\n";
			if (r.Complete())
				pad << "   " << EColour::Green << pr::str::PrettyNumber(consts.m_star_mass, 15, 1) << " million trillion kg\n";
			else if (r.m_time_till_discovery < ws.m_time_till_nova)
				pad << "   " << EColour::Red << pr::FmtS("(estimated discovery in %1.0f days)\n", pr::datetime::SecondsToDays(r.m_time_till_discovery));
			else
				pad << "   " << EColour::BrightRed << "(research needed!)\n";
		}
		{// star distance
			auto& r = ws.m_star_distance_research;
			pad << EColour::Blue << "Star Distance:\n";
			if (r.Complete())
				pad << "   " << EColour::Green << pr::str::PrettyNumber(consts.m_star_distance, 6, 1) << " million km\n";
			else if (r.m_time_till_discovery < ws.m_time_till_nova)
				pad << "   " << EColour::Red << pr::FmtS("(estimated discovery in %1.0f days)\n", pr::datetime::SecondsToDays(r.m_time_till_discovery));
			else
				pad << "   " << EColour::BrightRed << "(research needed!)\n";
		}
		{// Escape velocity and Required acceleration
			bool known = ws.m_star_distance_research.Complete() && ws.m_star_mass_research.Complete();
			
			pad << EColour::Blue << "Escape Velocity:\n";
			if (known)
				pad << "   " << EColour::Green << pr::str::PrettyNumber(consts.m_escape_velocity, 3, 1) << "km/s\n";
			else
				pad << "   " << EColour::Red << "(awaiting discoveries)\n";

			pad << EColour::Blue << "Required Acceleration:\n";
			if (known)
				pad << "   " << EColour::Green << pr::str::PrettyNumber(ws.m_required_acceleration, 3, 1) << "km/s/s";
			else
				pad << "   " << EColour::Red << "(awaiting discoveries)\n";
		}

		pad.m_width = PadRightSideWidth;
		m_cons.Write(EAnchor::TopRight, pad, 0, PadCountDownDy + PadCountDownHeight);
		PadResourceHeight = pad.Size().cy;
	}

	void ViewHome::RenderShipSpec() const
	{
		auto& ship = m_inst.m_ship;
		auto build_time = pr::datetime::ToCountdownString(ship.m_construction_time, pr::datetime::EMaxUnit::Days);

		Pad pad;
		pad.Title(" Space Craft Specifications ");
		pad.Border(EColour::Black);
		pad << EColour::Blue << "      Passenger Count: " << EColour::BrightBlue << ship.m_passenger_count  << "\n";
		pad << EColour::Blue << "        Fuel Material: " << EColour::BrightBlue << ship.m_fuel.m_name      << "\n";
		pad << EColour::Blue << "            Fuel Mass: " << EColour::BrightBlue << ship.m_fuel_mass        << "kg\n";
		pad << EColour::Blue << "  Structural Material: " << EColour::BrightBlue << ship.m_structure.m_name << "\n";
		pad << EColour::Blue << "     Systems Material: " << EColour::BrightBlue << ship.m_systems.m_name   << "\n";
		pad << EColour::Blue << "      Shield Material: " << EColour::BrightBlue << ship.m_shield.m_name    << "\n";
		pad << EColour::Blue << "          Shield Mass: " << EColour::BrightBlue << ship.m_shield_mass      << "kg\n";
		pad << EColour::Blue << "\n";
		pad << EColour::Blue << "    Construction Time: " << EColour::BrightBlue << build_time              << "\n";
		pad << EColour::Blue << "           Total Mass: " << EColour::BrightBlue << ship.m_total_mass       << "\n";
		pad << EColour::Blue << "         Total Volume: " << EColour::BrightBlue << ship.m_total_volume     << "\n";

		pad.m_width = PadRightSideWidth;
		m_cons.Write(EAnchor::TopRight, pad, 0, PadCountDownDy + PadCountDownHeight + PadResourceHeight);
	}

	void ViewHome::RenderMenu() const
	{
		Pad pad(EColour::Green);
		pad.Title(" Menu ", Colours(EColour::Black), EAnchor::Left);
		if (m_inst.m_view != EView::Home       ) pad << "   H - Home\n";
		if (m_inst.m_view != EView::ShipDesign ) pad << "   S - Ship Design\n";
		if (m_inst.m_view != EView::MaterialLab) pad << "   M - Materials Lab\n";
		if (m_inst.m_view != EView::Launch     ) pad << "   L - Launch Ship (end game)\n";
		pad << "=> " << EColour::Black << Pad::CurrentInput;
		m_cons.Write(EAnchor::BottomLeft, pad);
	}
}