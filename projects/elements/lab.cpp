#include "elements/stdafx.h"
#include "elements/lab.h"
#include "elements/game_constants.h"
#include "elements/material.h"
#include "elements/element.h"

namespace ele
{
	Lab::Lab(GameConstants const& consts)
		:m_consts(consts)
	{}

	//// Given two elements, look for a ratio of the two elements that produces the most stable material (if there are any)
	//void GetStableElementRatio(Element elem1, Element elem2, size_t& ratio1, size_t& ratio2, GameConstants const& consts)
	//{
	//	ratio1 = ratio2 = 0;
	//	if (elem1.IsMetal() == elem2.IsMetal())
	//		return;

	//	auto X = elem1.m_free_electrons;
	//	auto Y = elem2.m_free_holes;





	//	// Find the electron shell to be filled, and the number of electrons in that shell
	//	size_t period = std::max(elem1.Period(consts), elem2.Period(consts)) + 1;
	//	PR_ASSERT(PR_DBG, period > 0 && period < length(consts.m_valency_levels), "Valency levels should be choosen to prevent this");

	//	auto shell_count = consts.m_valency_levels[period] - consts.m_valency_levels[period - 1];
	//	PR_ASSERT(PR_DBG, shell_count != 0, "Valency levels should be choosen to prevent this");


	//	// Limit the possible values of a,b
	//	const int max_ratio = 9;
	//	double best_bond_strength = 0.0;
	//	for (int i = 1; i != max_ratio; ++i)
	//	for (int j = 1; j != max_ratio; ++j)
	//	{
	//		// Doesn't fill the shell...
	//		if (((i*X + j*Y) % shell_count) != 0) continue;

	//		auto gcf = pr::GreatestCommonFactor(i, j);
	//		auto a = i/gcf, b = j/gcf;

	//		// Determine the bond strength for this ratio of elements

	//	}
	//}

	// React two materials
	void Lab::React(Material m1, Material m2, Material& out1, Material& out2) const
	{
		auto& a1 = m1.m_elem1;
		auto& b1 = m1.m_elem2;
		auto& a2 = m2.m_elem1;
		auto& b2 = m2.m_elem2;

		//todo, handle metal-metal and non-metal-non-metal

		// Create a1b2 + a2b1 
		out1 = Material(a1, b2.m_free_holes, b2, a1.m_free_electrons);
		out2 = Material(a2, b1.m_free_holes, b1, a2.m_free_electrons);
	}
}
