#pragma once

// std
#include <string>
#include <vector>
#include <map>
#include <algorithm>
#include <iostream>
#include <functional>

// pr
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/common/console.h"
#include "pr/common/datetime.h"
#include "pr/common/hash.h"
#include "pr/common/si_units.h"
#include "pr/container/tri_table.h"
#include "pr/macros/enum.h"
#include "pr/macros/no_copy.h"
#include "pr/macros/count_of.h"
#include "pr/app/sim_message_loop.h"
#include "pr/maths/maths.h"
#include "pr/maths/rand.h"
#include "pr/str/tostring.h"

namespace ele
{
	typedef size_t atomic_number_t;
	typedef double man_days_t;
	typedef double man_power_t;
	typedef std::vector<std::string> strvec_t;
	const double seconds_per_day = 60.0*60.0*24.0;

	struct GameInstance;
	struct GameConstants;
	struct Element;
	struct Material;
	struct Ship;
	struct ResearchEffort;

	typedef std::vector<Element>         ElemCont;
	typedef std::vector<Material>        MatCont;
	typedef std::vector<Element*>        ElemPtrCont;
	typedef std::vector<Element const*>  ElemCPtrCont;
	typedef std::vector<Material*>       MatPtrCont;
	typedef std::vector<Material const*> MatCPtrCont;

	// An id for each view. These are only used by the view layer,
	// the game instance does not have any concept of the 'current view'
	#define PR_ENUM(x)\
		x(Intro)\
		x(Home)\
		x(ShipDesign)\
		x(MaterialLab)\
		x(Launch)\
		x(SameView)
	PR_DEFINE_ENUM1(EView, PR_ENUM);
	#undef PR_ENUM

	// Element properties
	#define PR_ENUM(x)\
		x(Existence         ,= 1 << 0)\
		x(Name              ,= 1 << 1)\
		x(AtomicNumber      ,= 1 << 2)\
		x(MeltingPoint      ,= 1 << 3)\
		x(BoilingPoint      ,= 1 << 4)\
		x(ValenceElectrons  ,= 1 << 5)\
		x(ElectroNegativity ,= 1 << 6)\
		x(AtomicRadius      ,= 1 << 7)
	PR_DEFINE_ENUM2_FLAGS(EElemProp, PR_ENUM);
	#undef PR_ENUM

	// Permutations
	namespace EPerm2 { enum { AA,AB,BB, NumberOf }; }
	namespace EPerm4 { enum { AA,AB,AC,AD,BB,BC,BD,CC,CD,DD, NumberOf }; }
	namespace EElemMask { enum { A = 1<<0, B = 1<<1, C = 1<<2, D = 1<<3, ABCD = A|B|C|D, AB = A|B }; }
		
	// The following steps take you through the process of building a chemical name, using compound XaYb as an example:
	// 1 Is X hydrogen?
	//  If so, the compound is probably an acid and may use a common name. If X isn't hydrogen, proceed to Step 2.
	// 2 Is X a nonmetal or a metal?
	//  If X is a nonmetal, then the compound is molecular. For molecular compounds, use numeric prefixes before each
	//  element's name to specify the number of each element. If there's only one atom of element X, no prefix is
	//  required before the name of X. Use the suffix –ide after the element name for Y. If X is a metal, then the
	//  compound is ionic; proceed to Step 3.
	// 3 Is X a metal that has variable charge?
	//  If X has a variable charge (often, these are group B metals), you must specify its charge within the compound
	//  by using a Roman numeral within parentheses between the element names for X and Y. For example, use (II) for
	//  Fe2+ and (III) for Fe3+. Proceed to Step 4.
	// 4 Is Y a polyatomic ion?
	//  If Y is a polyatomic ion, use the appropriate name for that ion. Usually, polyatomic anions have an ending
	//  of –ate or –ite (corresponding to related ions that contain more or less oxygen, respectively). Another common
	//  ending for polyatomic ions is –ide, as in hydroxide (OH–) and cyanide (CN–). If Y is not a polyatomic ion,
	//  use the suffix –ide after the name of Y.
	struct ElementName
	{
		// Full element name (all lower case)
		// e.g. hydrogen, sodium, iron, carbon, oxygen, sulfur, fluorine, argon
		char m_fullname[16];

		// Symbol
		// e.g  H, Na, Fe, C, O, S, Ar
		char m_symbol[3];

		// The suffix form of the element name (only really needed for nonmetals)
		// will have one of 'ide','ite','ate' appended
		// e.g. hydr, sodim, ferr, carb, ox, sul, fluor, argon
		char m_sufix_form[16];
	};

	template <typename T> inline T sqr(T t)    { return static_cast<T>(t * t); }
	template <typename T> inline T sqrt(T t)   { return static_cast<T>(std::sqrt(t)); }
	template <typename T> inline T cubert(T t) { return static_cast<T>(std::pow(t, 1.0/3.0)); }
	template <typename T> inline T ln(T t)     { return static_cast<T>(std::log(t)); }

	// Returns a vector of the results that pass 'pred' converted by 'select'
	template <typename TCont, typename TSelect, typename TPred>
	inline auto SelectWhere(TCont const& cont, TSelect select, TPred pred) -> std::vector<decltype(select(cont.front()))>
	{
		std::vector<decltype(select(cont.front()))> vec;
		for (auto& i : cont)
		{
			if (!pred(i)) continue;
			vec.push_back(select(i));
		}
		return vec;
	}

	// Returns a vector of the results of 'select'
	template <typename TCont, typename TPred>
	inline auto Where(TCont& cont, TPred pred) -> std::vector<typename TCont::value_type*>
	{
		std::vector<typename TCont::value_type*> vec;
		for (auto& i : cont)
		{
			if (!pred(i)) continue;
			vec.push_back(&i);
		}
		return vec;
	}

	// Returns a vector of the results of 'select'
	template <typename TCont, typename TSelect>
	inline auto Select(TCont& cont, TSelect select) -> std::vector<decltype(select())>
	{
		std::vector<decltype(select())> vec;
		for (auto& i : cont)
		{
			vec.push_back(select(i));
		}
		return vec;
	}

	// Return a pointer to an item in 'cont' if index is within range, otherwise return nullptr
	template <typename TCont> inline typename TCont::pointer Find(TCont& cont, int index)
	{
		if (index < 0 || index >= int(cont.size())) return nullptr;
		return &cont[index];
	}
}

