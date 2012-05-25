//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_LINE3_IMPL_H
#define PR_MATHS_LINE3_IMPL_H

#include "pr/maths/line3.h"

namespace pr
{
	inline float Length3Sq(Line3 const& l)
	{
		return Length3Sq(l.m_line);
	}
	inline float Length3(Line3 const& l)
	{
		return Length3(l.m_line);
	}
}

#endif
