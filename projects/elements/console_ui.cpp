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
		m_cons.Colour(EColour::Black, EColour::Grey);
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
		m_cons.Clear();
		switch (m_inst->m_view)
		{
		case EView::Home:
			RenderHomeView();
			break;
		}
		//RenderWorldState();
		//RenderShipSpec();
		//RenderMenu();
	}

	void ConsoleUI::RenderHomeView()
	{
		RenderWorldState();
		RenderMaterialInventory();
		
	}

	void ConsoleUI::RenderWorldState()
	{
		auto& ws = m_inst->m_world_state;
		auto& cons = m_inst->m_constants;
		Colours red(EColour::Red);
		Colours blue(EColour::Blue);
		auto ttl = pr::datetime::ToCountdownString(ws.m_time_till_nova, pr::datetime::EMaxUnit::Days);

		Pad pad(" World State ", EColour::Black, EColour::Default, EColour::Blue);
		pad
			<< red  << "       Time till nova: " << ttl << "\n"
			<< blue << "            Star Mass: " << cons.m_star_mass << "kg\n"
			        << "   Distance from Star: " << cons.m_star_distance << "m\n"
			        << "      Escape Velocity: " << cons.m_escape_velocity << "m/s\n"
			        << "Required Acceleration: " << ws.m_required_acceleration << "m/s/s\n";
		m_cons.Write(EAnchor::TopRight, pad);
	}

	// Render the list of materials
	void ConsoleUI::RenderMaterialInventory()
	{
		Colours black(EColour::Black);
		Colours green(EColour::Green);
		char const* fmt = "%-20s | %10s | %10s";
		
		Pad pad(" Material Stockpile ", EColour::Black);
		pad
			<< pr::FmtS(fmt, "Material Name", "Stock (kg)", "Rate (kg/s)");

		for (auto& i : m_inst->m_stockpile.m_mats)
		{
			pad
				<< pr::FmtS(fmt, i.Name(m_inst->m_constants).c_str(), 1, 1);
		}
		m_cons.Write(EAnchor::TopLeft, pad);
	}

	void ConsoleUI::RenderShipSpec()
	{
		auto& ship = m_inst->m_ship;
		Colours c0(EColour::Blue);
		Colours c1(EColour::Red);
		auto build_time = pr::datetime::ToCountdownString(ship.m_construction_time, pr::datetime::EMaxUnit::Days);

		Pad pad(" Ship Specifications ", EColour::Black);
		pad
			<< "      Passenger Count: " << c1 << ship.m_passenger_count << c0 << "\n"
			<< "        Fuel Material: " << ship.m_fuel.Name(m_inst->m_constants) << "\n"
			<< "            Fuel Mass: " << ship.m_fuel_mass << "kg\n"
			<< "  Structural Material: " << ship.m_structure.Name(m_inst->m_constants) << "\n"
			<< "     Systems Material: " << ship.m_systems.Name(m_inst->m_constants) << "\n"
			<< "      Shield Material: " << ship.m_shield.Name(m_inst->m_constants) << "\n"
			<< "          Shield Mass: " << ship.m_shield_mass << "kg\n"
			<< "\n"
			<< "    Construction Time: " << c1 << build_time << c0 << "\n"
			<< "           Total Mass: " << ship.m_total_mass << "\n"
			<< "         Total Volume: " << ship.m_total_volume << "\n"
			;
		m_cons.Write(EAnchor::TopRight, pad);
	}

	void ConsoleUI::RenderMenu()
	{
		Colours c0(EColour::Blue);
		Pad pad("Menu", EColour::Black);
		pad
			<< " Menu:\n" << c0
			<< "   S - Change ship spec\n"
			<< "   M - Material Research\n"
			<< "   L - Launch\n"
			<< "=>"
			;
		m_cons.Cursor(EAnchor::BottomLeft, 2, 0);
		m_cons.Write(EAnchor::BottomLeft, pad);
	}
}