#include "elements/stdafx.h"
#include "elements/console_ui.h"
#include "elements/game_instance.h"
#include "elements/view_base.h"
#include "elements/view_intro.h"
#include "elements/view_home.h"
#include "elements/view_lab.h"
#include "elements/view_ship.h"

using namespace pr::console;

namespace ele
{
	ConsoleUI::ConsoleUI(GameInstance& inst)
		:m_inst(inst)
		,m_cons()
		,m_loop()
		//,m_view(new ViewIntro(m_cons, m_inst))
		//,m_view(new ViewHome(m_cons, m_inst))
		,m_view(new ViewLab(m_cons, m_inst))
	{
		m_cons.Open(140, 60);
		m_cons.AutoScroll(false);
		m_cons.Echo(false);
		m_cons.Colour(EColour::Black, EColour::Grey);
		m_loop.AddStepContext("step", [this](double elapsed){ Run(elapsed); }, 10.0f, true);
		m_loop.Run();
	}

	void ConsoleUI::Run(double elapsed)
	{
		// Step the game
		m_inst.Step(elapsed);

		// Step the view
		m_cons.PumpInput();
		switch (m_view->Step(elapsed))
		{
		default: throw std::exception("Unknown view type");
		case EView::SameView:    break;
		case EView::Home:        m_view.reset(new ViewHome(m_cons, m_inst)); break;
		case EView::MaterialLab: m_view.reset(new ViewLab(m_cons, m_inst)); break;
		case EView::ShipDesign:  m_view.reset(new ViewShip(m_cons, m_inst)); break;
		case EView::Launch:      break;
		}
	}
/*
	void ConsoleUI::Render()
	{
		switch (m_inst.m_view)
		{
		case EView::Home:        RenderHomeView(); break;
		case EView::ShipDesign:  RenderShipView(); break;
		case EView::MaterialLab: RenderLabView(); break;
		case EView::Launch:      RenderLaunchView(); break;
		}
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



	// Render the list of materials
	void ConsoleUI::RenderMaterialInventory()
	{
		Pad pad(EColour::Black);
		pad.Title(" Material Stockpile ");
		pad.Border(EColour::Blue);
		pad << pr::FmtS("%-30s | %10s | %10s\n", "Material Name", "Stock (kg)", "Rate (kg/s)") << Colours(EColour::Blue);
		for (auto& i : m_inst.m_stockpile.m_mats)
			pad << pr::FmtS("%-30s | %10d | %10d\n", i.second.m_name.c_str(), 1, 1);
		
		m_cons.Write(EAnchor::TopLeft, pad, 0, 3);
	}
	*/
}