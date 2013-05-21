#include "elements/stdafx.h"
#include "elements/material.h"
#include "elements/element.h"
#include "elements/game_constants.h"

namespace ele
{
	// The name of the material (derived from the elements)
	inline std::string MaterialName(Element e1, size_t c1, Element e2, size_t c2)
	{
		char const* num[] =
		{
			// 0 - 23
			"","mono","di","tri","tetra","penta","hexa","hepta","octa","nona","deca",
			"undeca","dodeca","trideca","tetradeca","pentadeca","hexadeca","heptadeca","octadeca","nonadeca",
			"icosa","heicosa","docosa","tricosa"
		};
		auto IsVowel = [](char x){ return x == 'a' || x == 'e' || x == 'i' || x == 'o' || x == 'u' || x == 'y'; };

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

	// The stuff that the universe has in it
	Material::Material()
		:m_elem1()
		,m_elem2()
		,m_count1(0)
		,m_count2(0)
		,m_name()
		,m_hash(0)
	{}

	//
	Material::Material(Element e1, size_t c1, Element e2, size_t c2)
		:m_elem1(e1)
		,m_elem2(e2)
		,m_count1(c1)
		,m_count2(c2)
		,m_name(MaterialName(e1,c1,e2,c2))
		,m_hash(pr::hash::HashC(m_name.c_str()))
	{
		// Two non-metals are allowed tho
		//PR_ASSERT(PR_DBG, !(e1.IsMetal() && e2.IsMetal()), "Cannot make a material from two metals");
	}
}
