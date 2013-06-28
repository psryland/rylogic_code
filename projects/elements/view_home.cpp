#include "elements/stdafx.h"
#include "elements/view_home.h"
#include "elements/view_base.h"
#include "elements/game_instance.h"

using namespace pr::console;

namespace ele
{
	const pr::seconds_t CountDownRedrawTimer = 1.0;
	const int PadRightSideWidth = 60;
	int PadCountDownHeight;
	int PadResourceHeight;

	ViewHome::ViewHome(pr::Console& cons, GameInstance& inst)
		:ViewBase(cons, inst)
		,m_countdown_redraw_timer(CountDownRedrawTimer)
	{
		Render();
	}

	EView ViewHome::Step(pr::seconds_t elapsed)
	{
		m_countdown_redraw_timer += elapsed;
		if (m_countdown_redraw_timer< CountDownRedrawTimer || (m_countdown_redraw_timer = 0) != 0)
			RenderCountdown();
		
		return m_view;
	}

	void ViewHome::Render() const
	{
		Scope s(m_cons);

		m_cons.Write(EAnchor::TopLeft, "Home");
		RenderCountdown();
		RenderResearchStatus();
		RenderShipSpec();
		RenderMenu(EView::Home, strvec_t());
	}

	void ViewHome::RenderCountdown() const
	{
		Pad pad;
		pad.Title(" World State ");
		pad.Border(EColour::BrightRed);
		pad << EColour::Red       << "       Time till nova: "
			<< EColour::BrightRed << pr::datetime::ToCountdownString(m_inst.m_world_state.m_time_till_nova, pr::datetime::EMaxUnit::Days) << "\n";
		pad << EColour::Red       << "  Average Temperature: "
			<< EColour::BrightRed << pr::FmtS("%1.2f°C", m_inst.m_world_state.m_average_local_temperature);

		pad.Width(PadRightSideWidth);
		pad.AutoSize();
		pad.Draw(m_cons, EAnchor::TopRight, 0, TitleHeight);
		PadCountDownHeight = pad.WindowHeight();
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

		pad.Width(PadRightSideWidth);
		pad.AutoSize();
		pad.Draw(m_cons, EAnchor::TopRight, 0, TitleHeight + PadCountDownHeight);
		PadResourceHeight = pad.WindowHeight();
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

		pad.Width(PadRightSideWidth);
		pad.AutoSize();
		pad.Draw(m_cons, EAnchor::TopRight, 0, TitleHeight + PadCountDownHeight + PadResourceHeight);
	}

	void ViewHome::OnEvent(pr::console::Evt_Line<char> const& e)
	{
		std::string option = e.m_input;
		std::transform(begin(option), end(option), begin(option), ::tolower);
		ViewBase::HandleOption(EView::Home, option);
	}
}
