//**********************************************************
//
//	The bounding coordinates of object in the map
//
//**********************************************************
#ifndef DOM_BOUNDING_COORD_H
#define DOM_BOUNDING_COORD_H

#include "PR/Common/PRTypes.h"
#include "PR/Common/PRAssert.h"
#include "PR/DynamicObjectMap/DOMAssertEnable.h"

namespace pr
{
	class DOMBoundingCoord
	{
	public:
		enum Type { Lower = 0, Upper = 1, IS_UPPER_BIT = 0x80000000, INDEX_MASK = 0x7FFFFFFF };
		DOMBoundingCoord(Type upper, uint object_index, float bound)
		{
			PR_ASSERT(PR_DBG_DOM, (object_index & INDEX_MASK) == object_index);
			m_is_upper_and_index = object_index;
			if( upper == Upper ) m_is_upper_and_index |= IS_UPPER_BIT;
			m_bound = bound;
		}
		bool	IsUpper() const			{ return (m_is_upper_and_index & IS_UPPER_BIT) > 0; }
		uint	ObjectIndex() const		{ return m_is_upper_and_index & INDEX_MASK; }
		float	Bound() const			{ return m_bound; }
		void	SetBound(float bound)	{ m_bound = bound; }
		
		// Tag this structure as being pod
		struct pr_is_pod { enum { value = true }; };

	private:
		uint	m_is_upper_and_index;
		float	m_bound;
	};
}//namespace pr

#endif//DOM_BOUNDING_COORD_H
