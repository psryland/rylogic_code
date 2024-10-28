#include "elements/stdafx.h"
#include "elements/lab.h"
#include "elements/game_constants.h"
#include "elements/bond.h"
#include "elements/material.h"
#include "elements/element.h"
#include "elements/game_events.h"

namespace ele
{
	// Explanation of the physics of electron attractivity:
	// It all has to do with how much positive charge from the nucleus the electrons in the outer shell experience (which is usually called Z-effective or Zeff),
	// which depends on the principle quantum numbers of the electrons involved. The octet rule occurs mainly because an electron around a nucleus will not perfectly
	// "shield" another electron from the nucleus's positive charge, especially if the shielding electron and the incoming electron have the same principle quantum number.
	// 2p electrons can't shield other 2p electrons very well (and 3p can't shield other 3p very well, etc.). It's kind of complicated, but I will explain it as best I can.
	// Consider a helium atom and a hyrogen atom (a proton). If I only give my He atom one electron and give my proton no electrons, both my He and my proton will have a charge
	// of +1. Since they have the same charge, you might think that an electron would be equally attracted to either one - but that's not the case. An electron is much more
	// strongly attracted to an He+ atom than to a H+ atom. This occurs because even though the He+ and the H+ have the same charge, the He has two protons and the electron
	// that's already present won't perfectly shield the incoming electron from one whole unit of positive charge. The result is that an electron coming into an He+ atom will
	// experience a positive charge that's something like +1.3 insted of just +1. How well an electron shields an outer electron from the nucleus depends on the principle
	// quantum number and angular momentum quantum number of the electrons involved. If an electron has the same n and l value as the electron that it's trying to shield,
	// it won't be able to shield very well.
	// Consider a neutral carbon atom: it has 6 protons and 6 electrons (2 1s electrons, 2 2s electrons, and 2 2p electrons). If I add a new electron to make a C- anion,
	// the new electron that I'm adding will think that the atom has a charge of around +0.6 because each of the 2p electrons that are already there can only shield another
	// 2p electron from about .7 units of charge. But if I want to add an electron to a F atom to make F-, now my additional electron will see a charge of something like +1.5,
	// since there are already 5 2p electrons present that each allow +0.3 charge to "bleed through" their coverage of the nucleus. If I want to add another electron to my F-,
	// now I will have to add a 3s electron, and the 2p electrons that are already there will shield the 3s electron much better than they can shield other 2p electrons. So the
	// first extra electron that you add to F will see a charge of around +1.5, while the second will see a charge close to -1.
	// That is the main reason why atoms are more stable if they can get to 8 electrons to make an octet; so long as you are filling up a partly-filled p orbital, positive charge
	// from the nucleus will be able to get through to attract the extra electrons. Once you have filled the p orbital completely, you now have to add to the next level s orbital,
	// to which very little extra charge from the nucleus can get through. There are also a few issues with electrons being lower in energy if there are a lot of other electrons
	// around with the same n, l, and Ms values that contribute to the octet rule, but it mainly has to do with charge and charge shielding.
	// For simplicity, assume full valence shells shield 100% of the charge, valence electrons shield 60%

	Lab::Lab(GameConstants const& consts)
		:m_consts(consts)
		,m_elements()
		,m_materials()
		,m_element_order()
		,m_materials_order()
		,m_known_properties()
	{
		// Populate the container of elements
		for (atomic_number_t i = 1; i <= consts.m_element_count; ++i)
			m_elements.push_back(Element(i, m_consts));

		// Populate the container of materials. Generate every possible combination
		m_materials.resize(pr::tri_table::Size<pr::tri_table::Inclusive>(consts.m_element_count));
		for (size_t i = 0; i != consts.m_element_count; ++i)
		for (size_t j = i; j != consts.m_element_count; ++j)
		{
			Material mat(m_elements[i], m_elements[j], m_consts);
			m_materials[mat.m_index] = mat;
		}
		PR_ASSERT(PR_DBG, m_materials.size() == pr::tri_table::Size<pr::tri_table::Inclusive>(consts.m_element_count), "");

		// The properties of the elements that the player knows about
		m_known_properties |= EElemProp::Existence;
		m_known_properties |= EElemProp::Name;
		m_known_properties |= EElemProp::MeltingPoint;
		m_known_properties |= EElemProp::BoilingPoint;

		// Set the display order collection
		UpdateDisplayOrder();
	}

	// Update the display order of the elements based on what the player
	// currently knowns about them. Order is atomic number, alphabetical
	void Lab::UpdateDisplayOrder()
	{
		// Only the elements that are known to exist are visible
		m_element_order = Where(m_elements, [](Element const& elem){ return pr::AllSet(elem.m_known_properties, EElemProp::Existence); });
		std::sort(std::begin(m_element_order), std::end(m_element_order), [](Element const* lhs, Element const* rhs)
		{
			bool al = pr::AllSet(lhs->m_known_properties, EElemProp::AtomicNumber);
			bool ar = pr::AllSet(rhs->m_known_properties, EElemProp::AtomicNumber);
			if (al != ar) return al;
			if (al && ar) return lhs->m_atomic_number < rhs->m_atomic_number;
			return strcmp(lhs->m_name->m_fullname, rhs->m_name->m_fullname) < 0;
		});

		// Only materials that have been discovered are visible
		m_materials_order = Where(m_materials,[](Material const& mat){ return mat.m_discovered; });
		std::sort(std::begin(m_materials_order), std::end(m_materials_order), [](Material const* lhs, Material const* rhs)
		{
			return lhs->m_name < rhs->m_name;
		});
	}

	// Called to 'discover' a new element
	void Lab::DiscoverElement(atomic_number_t atomic_number)
	{
		auto& element = m_elements[atomic_number - 1];
		PR_ASSERT(PR_DBG, !pr::AllSet(element.m_known_properties, EElemProp::Existence), "Element already discovered");

		// We now know of it's existence, and it's been named
		element.m_known_properties |= EElemProp::Existence;
		element.m_known_properties |= EElemProp::Name;
		UpdateDisplayOrder();

		pr::events::Send(Evt_Discovery(&element));
	}

	// Called to 'discover' a material
	void Lab::DiscoverMaterial(int index)
	{
		auto& material = m_materials[index];
		PR_ASSERT(PR_DBG, !material.m_discovered, "Material already discovered");

		material.UpdateName(material.m_name_common);
		material.m_discovered = true;
		UpdateDisplayOrder();

		pr::events::Send(Evt_Discovery(&material));
	}

	// Returns a collection of the materials related to 'elem'
	MatCPtrCont Lab::RelatedMaterials(Element const& elem) const
	{
		atomic_number_t num = elem.m_atomic_number;
		return SelectWhere(m_materials_order,
			[ ](Material const* m){ return m; },
			[=](Material const* m){ return m->m_elem1.m_atomic_number == num || m->m_elem2.m_atomic_number == num; });
	}


	// Generate the name of a material formed from the given elements
	std::string MaterialName(Element elem1, size_t count1, Element elem2, size_t count2)
	{
		char const* num[] = { // 0 - 23
			"","mono","di","tri","tetra","penta","hexa","hepta","octa","nona","deca",
			"undeca","dodeca","trideca","tetradeca","pentadeca","hexadeca","heptadeca","octadeca","nonadeca",
			"icosa","heicosa","docosa","tricosa"};

		auto IsVowel = [](char x){ return x == 'a' || x == 'e' || x == 'i' || x == 'o' || x == 'u' || x == 'y'; };

		bool flip = elem1.m_atomic_number == 1 || elem2.m_valence_electrons < elem1.m_valence_electrons;
		auto& e1 = flip ? elem2 : elem1;
		auto& e2 = flip ? elem1 : elem2;
		auto& c1 = flip ? count2 : count1;
		auto& c2 = flip ? count1 : count2;

		std::string name;
		if (elem1.m_atomic_number == elem2.m_atomic_number)
		{
			name.append(e1.m_name->m_fullname);
		}
		else
		{
			if (c1 > 1)
			{
				name.append(num[c1]);
				if (IsVowel(e1.m_name->m_fullname[0]) && c1 > 3)
					name.resize(name.size() - 1);
			}
			name.append(e1.m_name->m_fullname);
			name.append(" ");
			if (c2 > 1 && !e1.IsMetal())
			{
				name.append(num[c2]);
				if (IsVowel(e2.m_name->m_sufix_form[0]) && (c2 != 2 || c2 != 3))
					name.resize(name.size() - 1);
			}
			name.append(e2.m_name->m_sufix_form);
			name.append("ide");
		}
		return name;
	}

	// Generate the symbollic name of a material formed from the given elements
	std::string MaterialSymName(Element elem1, size_t count1, Element elem2, size_t count2)
	{
		bool flip = elem1.m_atomic_number == 1 || elem2.m_valence_electrons < elem1.m_valence_electrons;
		auto& e1 = flip ? elem2 : elem1;
		auto& e2 = flip ? elem1 : elem2;
		auto& c1 = flip ? count2 : count1;
		auto& c2 = flip ? count1 : count2;

		std::string name;
		name.append(e1.m_name->m_symbol).append(pr::To<std::string>(c1)).append(e2.m_name->m_symbol).append(pr::To<std::string>(c2));
		return name;
	}

	// Calculates a bond strength between the given elements
	// Negative values mean no bond will form
	double BondStrength(Element elem1, Element elem2, GameConstants const& consts)
	{
		// The electro-static force between two charged objects is F = k*Q*q/r²
		// Assume elem1 and elem2 are separated such that their outermost electron shells just touch
		// The total bond strength is the sum of the electro static forces:
		//  P1 - P2 (repulsive), E1 - E2 (repulsive), P1 - E2 (attractive), P2 - E1 (attractive)
		
		// Assuming ionic/covalent bonding only, P1 and P2 can share electrons in their outer orbital.
		// The proton charges are the effective (Zeff) positive charge, the electron charge is
		// the charge of the maximum number of electrons that can be borrowed when trying to fill the
		// the outer orbital.
		double P1 = +1.0 * elem1.m_valence_electrons;
		double P2 = +1.0 * elem2.m_valence_electrons;
		double E1 = -1.0 * elem1.m_valence_electrons + std::min(elem1.m_valence_holes, elem2.m_valence_electrons);
		double E2 = -1.0 * elem2.m_valence_electrons + std::min(elem2.m_valence_holes, elem1.m_valence_electrons);
		double r  = consts.m_orbital_radius[elem1.m_period+1] + consts.m_orbital_radius[elem2.m_period+1];
		
		double strength = consts.m_coulomb_constant * (P1*E2 + P2*E1 + P1*P2 + E1*E2) / sqr(r);
		return strength;
	}

	// Calculates the bond strengths for all permutations of elem1,elem2
	void BondStrengths(Element elem1, Element elem2, GameConstants const& consts, Bond (&bonds)[EPerm2::NumberOf])
	{
		using namespace EPerm2;
		bonds[AA] = Bond(AA, BondStrength(elem1, elem1, consts));
		bonds[AB] = Bond(AB, BondStrength(elem1, elem2, consts));
		bonds[BB] = Bond(BB, BondStrength(elem2, elem2, consts));
	}

	// Calculates the bond strengths for all permutations of the elements in mat1,mat2
	void BondStrengths(Material mat1, Material mat2, GameConstants const& consts, Bond (&bonds)[EPerm4::NumberOf])
	{
		using namespace EPerm4;
		auto& a = mat1.m_elem1;
		auto& b = mat1.m_elem2;
		auto& c = mat2.m_elem1;
		auto& d = mat2.m_elem2;

		bonds[AA] = Bond(AA, BondStrength(a, a, consts));
		bonds[AB] = Bond(AB, BondStrength(a, b, consts));
		bonds[AC] = Bond(AC, BondStrength(a, c, consts));
		bonds[AD] = Bond(AD, BondStrength(a, d, consts));
		bonds[BB] = Bond(BB, BondStrength(b, b, consts));
		bonds[BC] = Bond(BC, BondStrength(b, c, consts));
		bonds[BD] = Bond(BD, BondStrength(b, d, consts));
		bonds[CC] = Bond(CC, BondStrength(c, c, consts));
		bonds[CD] = Bond(CD, BondStrength(c, d, consts));
		bonds[DD] = Bond(DD, BondStrength(d, d, consts));
	}

	// Returns a factor describing how 'ionic' the bond is.
	// Higher bond ionicity suggests higher macro material strength (melting point, etc)
	// Lower ionicity suggests weak inter-molecular bonding (lower melting points, etc)
	double BondIonicity(Element elem1, Element elem2)
	{
		// Ionic bonds form between elements that are at opposite edges of the periodic table
		auto& e1 = elem1.m_valence_electrons < elem2.m_valence_electrons ? elem1 : elem2;
		auto& e2 = elem1.m_valence_electrons < elem2.m_valence_electrons ? elem2 : elem1;

		// Elements of the same type are always covalently bonded (one can't pull an electron from the other)
		if (e1.m_atomic_number == e2.m_atomic_number) return 0.0;

		// Noble gases don't bond to anything
		if (e1.m_valence_electrons == 0 || e2.m_valence_electrons == 0) return 0.0;

		// +1,-1 bonds are purely ionic, ramping down based on period
		double i1 = pr::Clamp(1.0 - (std::min(e1.m_valence_electrons,e1.m_valence_holes) - 1.0) / (e1.m_period+1), 0.0, 1.0);
		double i2 = pr::Clamp(1.0 - (std::min(e2.m_valence_electrons,e2.m_valence_holes) - 1.0) / (e2.m_period+1), 0.0, 1.0);
		return i1 * i2;
	}
}
