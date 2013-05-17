#include "elements/stdafx.h"
#include "elements/console_ui.h"
#include "elements/game_instance.h"

using namespace pr::console;

namespace ele
{
	ConsoleUI::ConsoleUI(GameInstance& inst)
		:m_inst(&inst)
		,m_cons()
		,m_loop()
	{
		m_cons.Open(140, 40);
		m_loop.AddStepContext("step", [this](double elapsed){ Run(elapsed); }, 1.0f, true);
		m_loop.Run();
	}

	void ConsoleUI::Run(double elapsed)
	{
		m_inst->Step(elapsed);
		Render();
	}

	void ConsoleUI::Render()
	{
		CursorScope s0(m_cons);
		ColourScope s1(m_cons);

		RenderWorldState();
		RenderShipSpec();
		RenderMenu();
	}

	void ConsoleUI::RenderWorldState()
	{
		auto& ws = m_inst->m_world_state;
		auto& cons = m_inst->m_constants;

		Pad pad(EColour::Black, EColour::Grey);
		Colours c0(EColour::Blue,EColour::Grey);
		Colours c1(EColour::Red ,EColour::Grey);
		auto ttl = pr::datetime::ToCountdownString(ws.m_time_till_nova, pr::datetime::EMaxUnit::Days);
		pad
			<< " World State\n" << c0
			<< "       Time till nova: " << c1 << ttl << c0 << "\n"
			<< "            Star Mass: " << cons.m_star_mass << "kg\n"
			<< "   Distance from Star: " << cons.m_star_distance << "m\n"
			<< "      Escape Velocity: " << cons.m_escape_velocity << "m/s\n"
			<< "Required Acceleration: " << c1 << ws.m_required_acceleration << "m/s/s\n";
		
		m_cons.Write(EAnchor::TopRight, pad);
	}

	void ConsoleUI::RenderShipSpec()
	{
		auto& ship = m_inst->m_ship;

		Pad pad(EColour::Black, EColour::Grey);
		Colours c0(EColour::Blue,EColour::Grey);
		Colours c1(EColour::Red ,EColour::Grey);
		auto build_time = pr::datetime::ToCountdownString(ship.m_construction_time, pr::datetime::EMaxUnit::Days);

		pad
			<< " Ship Specifications\n" << c0
			<< "      Passenger Count: " << c1 << ship.m_passenger_count << c0 << "\n"
			<< "        Fuel Material: " << ship.m_fuel.m_name << "\n"
			<< "            Fuel Mass: " << ship.m_fuel_mass << "kg\n"
			<< "  Structural Material: " << ship.m_structure.m_name << "\n"
			<< "     Systems Material: " << ship.m_systems.m_name << "\n"
			<< "      Shield Material: " << ship.m_shield.m_name << "\n"
			<< "          Shield Mass: " << ship.m_shield_mass << "kg\n"
			<< "\n"
			<< "    Construction Time: " << c1 << build_time << c0 << "\n"
			<< "           Total Mass: " << ship.m_total_mass << "\n"
			<< "         Total Volume: " << ship.m_total_volume << "\n"
			;
		
		m_cons.Write(EAnchor::TopLeft, pad);
	}

	void ConsoleUI::RenderMenu()
	{
	}
}