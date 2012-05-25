//***************************************************************************
//
//	Attribute - The per-face properties
//
//***************************************************************************
#ifndef ATTRIBUTE_H
#define ATTRIBUTE_H

#include "PR/Common/PRTypes.h"

namespace pr
{
	namespace rdr
	{
		struct Attribute
		{
			Attribute() : m_mat_index(0) {}
			bool operator == (const Attribute& a) const { return m_mat_index == a.m_mat_index; }
			uint m_mat_index;
		};

	}//namespace rdr
}//namespace pr

#endif//ATTRIBUTE_H
