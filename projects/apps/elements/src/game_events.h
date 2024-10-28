#pragma once

#include "elements/forward.h"
#include "elements/element.h"
#include "elements/material.h"

namespace ele
{
	// Raised when a new element or material is discovered
	struct Evt_Discovery
	{
		Element const* m_elem;
		Material const* m_mat;
		std::string m_blurb;
		explicit Evt_Discovery(Element const* elem) :m_elem(elem) ,m_mat()
		{
			m_blurb
				.append(
					"Professor {name}, using {discovery_technique}, has discovered a new element! "
					"{pronoun} has named this element '")
				.append(m_elem->m_name->m_fullname)
				.append("'");
		}
		explicit Evt_Discovery(Material const* mat) :m_elem() ,m_mat(mat)
		{
			m_blurb
				.append("In a freak accident, a new material has been discovered! ")
				.append("The new material has been named '")
				.append(m_mat->m_name)
				.append("' in honour of {name} who paid the ultimate price in making this discovery.");
		}
		PR_NO_COPY(Evt_Discovery);
	};
}

