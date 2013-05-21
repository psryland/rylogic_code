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
		m_cons.DoubleBuffer();
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
		case EView::Home:        RenderHomeView(); break;
		case EView::ShipDesign:  RenderShipView(); break;
		case EView::MaterialLab: RenderLabView(); break;
		case EView::Launch:      RenderLaunchView(); break;
		}
		m_cons.FlipBuffer();
	}

	void ConsoleUI::RenderHomeView()
	{
		RenderWorldState();
		RenderMaterialInventory();
		RenderMenu();
		//RenderWorldState();
		//RenderShipSpec();
		//RenderMenu();
	}

	void ConsoleUI::RenderShipView()
	{
		RenderMenu();
	}

	void ConsoleUI::RenderLabView()
	{
		RenderMaterialInventory();
		RenderMenu();
	}

	void ConsoleUI::RenderLaunchView()
	{
		RenderMenu();
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
		
		Pad pad(" Material Stockpile ", EColour::Black);
		pad << pr::FmtS("%-30s | %10s | %10s\n", "Material Name", "Stock (kg)", "Rate (kg/s)");
		for (auto& i : m_inst->m_stockpile.m_mats)
			pad << pr::FmtS("%-30s | %10d | %10d\n", i.second.m_name.c_str(), 1, 1);
		
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
		m_cons.Write(EAnchor::TopRight, pad);
	}

	void ConsoleUI::RenderMenu()
	{
		Colours c0(EColour::Blue);
		Pad pad("Menu", EColour::Black);
		pad << " Menu:\n" << c0;
		if (m_inst->m_view != EView::Home       ) pad << "   H - Home\n";
		if (m_inst->m_view != EView::ShipDesign ) pad << "   S - Ship Design\n";
		if (m_inst->m_view != EView::MaterialLab) pad << "   M - Materials Lab\n";
		if (m_inst->m_view != EView::Launch     ) pad << "   L - Launch Ship (end game)\n";
		pad << "=>";
		m_cons.Write(EAnchor::BottomLeft, pad);
		m_cons.Cursor(EAnchor::BottomLeft, 2, 0);
	}
}