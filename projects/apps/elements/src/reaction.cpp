#include "elements/stdafx.h"
#include "elements/reaction.h"
#include "elements/material.h"
#include "elements/Bond.h"
#include "elements/lab.h"

namespace ele
{
	Reaction::Reaction()
		:m_mat1()
		,m_mat2()
		,m_input_energy()
		,m_out()
		,m_energy_change()
	{}

	// Represents the result of a reaction between two materials
	Reaction::Reaction(Material const& mat1, Material const& mat2)
		:m_mat1(&mat1)
		,m_mat2(&mat2)
		,m_input_energy(0)
		,m_out()
		,m_energy_change(0)
	{}

	void Reaction::Do(GameConstants const& consts)
	{
		using namespace EPerm4;
		PR_ASSERT(PR_DBG, m_mat1 != nullptr && m_mat2 != nullptr, "Don't call 'Do' untill materials have been assigned");

		auto& a = m_mat1->m_elem1;
		auto& b = m_mat1->m_elem2;
		auto& c = m_mat2->m_elem1;
		auto& d = m_mat2->m_elem2;
		
		// The four elements have the possibility of forming these 10 pairs:
		// AA,AB,AC,AD,BB,BC,BD,CC,CD,DD
		// Determine the bond strength of each pair and pick the strongest bonds
		// as the new materials produced
		Bond bonds[NumberOf];
		BondStrengths(*m_mat1, *m_mat2, consts, bonds);
		OrderByStrength(bonds);

		int used = 0; // bit mask of elements available
		for (int i = 0; i != NumberOf && used != EElemMask::ABCD; ++i)
		{
			using namespace EElemMask;
			switch (bonds[i].m_perm)
			{
			case EPerm4::AA: m_out.push_back(Material(a,a,consts)); used |= A;   break;
			case EPerm4::AB: m_out.push_back(Material(a,b,consts)); used |= A|B; break;
			case EPerm4::AC: m_out.push_back(Material(a,c,consts)); used |= A|C; break;
			case EPerm4::AD: m_out.push_back(Material(a,d,consts)); used |= A|D; break;
			case EPerm4::BB: m_out.push_back(Material(b,b,consts)); used |= B;   break;
			case EPerm4::BC: m_out.push_back(Material(b,c,consts)); used |= B|C; break;
			case EPerm4::BD: m_out.push_back(Material(b,d,consts)); used |= B|D; break;
			case EPerm4::CC: m_out.push_back(Material(c,c,consts)); used |= C;   break;
			case EPerm4::CD: m_out.push_back(Material(c,d,consts)); used |= C|D; break;
			case EPerm4::DD: m_out.push_back(Material(d,d,consts)); used |= D;   break;
			}
		}


		//double bond_energy
		//// Create a1b2 + a2b1 
		//out1 = Material(a1, b2.m_valence_holes, b2, a1.m_valence_electrons);
		//out2 = Material(a2, b1.m_valence_holes, b1, a2.m_valence_electrons);
	}

}
