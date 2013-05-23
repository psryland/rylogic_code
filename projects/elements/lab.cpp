#include "elements/stdafx.h"
#include "elements/lab.h"
#include "elements/game_constants.h"
#include "elements/bond.h"
#include "elements/material.h"
#include "elements/element.h"

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
	{
		Element e15(15, m_consts);
		Element e9(9, m_consts);
		Element e3(3, m_consts);
		Element e11(11, m_consts);
		Material m1(e15,e9,m_consts);
		Material m2(e3,e11,m_consts);
	}

	// Generate the name of a material formed from the given elements
	std::string MaterialName(Element elem1, size_t count1, Element elem2, size_t count2)
	{
		char const* num[] = { // 0 - 23
			"","mono","di","tri","tetra","penta","hexa","hepta","octa","nona","deca",
			"undeca","dodeca","trideca","tetradeca","pentadeca","hexadeca","heptadeca","octadeca","nonadeca",
			"icosa","heicosa","docosa","tricosa"};

		auto IsVowel = [](char x){ return x == 'a' || x == 'e' || x == 'i' || x == 'o' || x == 'u' || x == 'y'; };

		auto& e1 = elem1.m_free_electrons < elem2.m_free_electrons ? elem1 : elem2;
		auto& e2 = elem1.m_free_electrons < elem2.m_free_electrons ? elem2 : elem1;
		auto& c1 = elem1.m_free_electrons < elem2.m_free_electrons ? count1 : count2;
		auto& c2 = elem1.m_free_electrons < elem2.m_free_electrons ? count2 : count1;

		std::string name;
		if (c1 > 1)
		{
			name.append(num[c1]);
			if (IsVowel(e1.m_name->m_fullname[0]) && c1 > 3)
				name.resize(name.size() - 1);
		}
		name.append(e1.m_name->m_fullname);
		name.append(" ");
		if (c2 > 1 || !e1.IsMetal())
		{
			name.append(num[c2]);
			if (IsVowel(e2.m_name->m_sufix_form[0]) && (c2 != 2 || c2 != 3))
				name.resize(name.size() - 1);
		}
		name.append(e2.m_name->m_sufix_form);
		name.append("ide");
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
		double P1 = +1.0 * elem1.m_free_electrons;
		double P2 = +1.0 * elem2.m_free_electrons;
		double E1 = -1.0 * elem1.m_free_electrons + std::min(elem1.m_free_holes, elem2.m_free_electrons);
		double E2 = -1.0 * elem2.m_free_electrons + std::min(elem2.m_free_holes, elem1.m_free_electrons);
		double r  = consts.m_orbital_radius[elem1.m_period] + consts.m_orbital_radius[elem2.m_period];
		
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

	// Returns true if 'elem1' and 'elem2' would be ionically bonded (as opposed to covalently bonded)
	bool IsIonicBond(Element elem1, Element elem2)
	{
		// Ionic bonds form between elements that are at opposite edges of the periodic table
		auto& e1 = elem1.m_free_electrons < elem2.m_free_electrons ? elem1 : elem2;
		auto& e2 = elem1.m_free_electrons < elem2.m_free_electrons ? elem2 : elem1;
		
		// Hyrdogen is always covalently bonded, otherwise ionic if the elements are near the edges of the periodic table
		return
			e1.m_atomic_number != 1 &&
			e2.m_atomic_number != 1 &&
			e1.m_free_electrons <= e1.m_period + 1 &&
			e2.m_free_holes     <= e2.m_period + 1;
	}
}
