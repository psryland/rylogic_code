#include "elements/stdafx.h"
#include "elements/reaction.h"
#include "elements/material.h"
#include "elements/Bond.h"
#include "elements/lab.h"

namespace ele
{
	// Represents the result of a reaction between two materials
	Reaction::Reaction(Material mat1, Material mat2, GameConstants const& consts)
		:m_mat1(mat1)
		,m_mat2(mat2)
	{
		using namespace EPerm4;
		auto& a = m_mat1.m_elem1;
		auto& b = m_mat1.m_elem2;
		auto& c = m_mat2.m_elem1;
		auto& d = m_mat2.m_elem2;

		// The four elements have the possibility of forming these 10 pairs:
		// AA,AB,AC,AD,BB,BC,BD,CC,CD,DD
		// Determine the bond strength of each pair and pick the strongest bonds
		// as the new materials produced
		Bond bonds[NumberOf];
		BondStrengths(mat1, mat2, consts, bonds);
		//double bond_energy
		//// Create a1b2 + a2b1 
		//out1 = Material(a1, b2.m_free_holes, b2, a1.m_free_electrons);
		//out2 = Material(a2, b1.m_free_holes, b1, a2.m_free_electrons);
	}

}
