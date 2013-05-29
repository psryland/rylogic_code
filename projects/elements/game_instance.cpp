#include "elements/stdafx.h"
#include "elements/game_instance.h"
#include "elements/material.h"

namespace ele
{
	GameInstance::GameInstance(int seed)
		:m_consts(seed, true)
		,m_world_state(m_consts)
		,m_stockpile()
		,m_lab(m_consts)
		,m_ship()
	{
		// Generate the starting materials
		GenerateStartingMaterials();

		//// Add some materials
		//for (int i = 0; i != 10; ++i)
		//{
		//	Element e1(pr::rand::int1(1,m_consts.m_element_count), m_consts);
		//	Element e2(pr::rand::int1(1,m_consts.m_element_count), m_consts);
		//	m_stockpile.Add(Material(e1,e2,m_consts));
		//}
	}

	void GameInstance::Step(pr::seconds_t elapsed)
	{
		m_world_state.Step(elapsed);
		m_stockpile.Step(elapsed);
	}

	// Generates the starting materials
	void GameInstance::GenerateStartingMaterials()
	{
		// Start with the ideal materials and generate materials backwards to get the starting materials

		// Determine the ideal material for building the space craft by sorting the list of possible
		// materials by their total bond energy
		std::vector<Material const*> mats;
		for (auto& m : m_lab.m_mats) mats.push_back(&m);
		std::sort(std::begin(mats), std::end(mats), [=](Material const* lhs, Material const* rhs){ return lhs->m_enthalpy > rhs->m_enthalpy; });
		
		//
		// by picking randomly from the 5 lightest metals and then choosing the
		// material that forms
		// 
	}

		// Called at the end of the game when the star goes nova
	void GameInstance::Supernova()
	{
	}


	//	int Run()
	//	{
	//		m_cons.Write(EAnchor::TopCentre, "Elements");
	//		for (;;)
	//		{
	//			Render();
	//			if (!HandleInput()) break;
	//		}
	//		return 0;
	//	}

	//	void Render()
	//	{
	//		m_cons.Clear();
	//		RenderInventry();
	//		ShowMenu();
	//	}

	//	void RenderInventry()
	//	{
	//	}

	//	void ShowMenu()
	//	{
	//		std::string str =
	//			"Menu:\n"
	//			"m - Mine for new elements\n"
	//			"q - quit\n";
	//		m_cons.Write(EAnchor::BottomLeft, str);
	//	}

	//	bool HandleInput()
	//	{
	//		std::string input = m_cons.GetLine();
	//		if (input == "q") return false;
	//		if (input == "m") return true;
	//		return true;
	//	}
	//};
}