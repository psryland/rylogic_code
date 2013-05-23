#include "elements/stdafx.h"
#include "elements/material.h"
#include "elements/element.h"
#include "elements/game_constants.h"
#include "elements/lab.h"

namespace ele
{
	Material::Material()
		:m_elem1()
		,m_elem2()
		,m_ionic(false)
		,m_count1(0)
		,m_count2(0)
		,m_name()
		,m_hash(0)
		,m_bonds()
	{}
	Material::Material(Element e1, Element e2, GameConstants const& consts)
		:m_elem1(e1.m_free_electrons < e2.m_free_electrons ? e1 : e2)
		,m_elem2(e1.m_free_electrons < e2.m_free_electrons ? e2 : e1)
		,m_ionic(IsIonicBond(m_elem1, m_elem2))
		,m_count1(m_elem2.m_free_holes     / pr::GreatestCommonFactor(m_elem1.m_free_electrons, m_elem2.m_free_holes))
		,m_count2(m_elem1.m_free_electrons / pr::GreatestCommonFactor(m_elem1.m_free_electrons, m_elem2.m_free_holes))
		,m_name(MaterialName(m_elem1, m_count1, m_elem2, m_count2))
		,m_hash(pr::hash::HashC(m_name.c_str()))
		,m_bonds()
	{
		using namespace EPerm2;

		// Find the bond configuration by making the strongest bonds first
		Bond bonds[NumberOf];
		BondStrengths(m_elem1, m_elem2, consts, bonds);
		OrderByStrength(bonds);
		
		size_t count1 = m_count1;
		size_t count2 = m_count2;
		
		// Create bonds until at least one of the elements is used up
		for (auto bond : bonds)
		{
			size_t c;
			switch (bond.m_perm)
			{
			case AA:
				c = count1/2;
				count1 -= c*2;
				m_bonds[AA] += c;
				break;
			case BB:
				c = count2/2;
				count2 -= c*2;
				m_bonds[BB] += c;
				break;
			case AB:
				c = std::min(count1,count2);
				count1 -= c;
				count2 -= c;
				m_bonds[AB] += c;
			}
		}
		// The bonds calculated so far are the minimum needed. If these produce
		// two distinct sets (i.e. no AB bonds) then the material is unstable
	}
}
