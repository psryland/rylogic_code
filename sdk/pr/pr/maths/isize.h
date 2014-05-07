//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_ISIZE_H
#define PR_MATHS_ISIZE_H

#include "pr/maths/constants.h"
#include "pr/maths/forward.h"

namespace pr
{
	struct ISize
	{
		size_t x;
		size_t y;

		ISize& set(size_t x_, size_t y_)             { x = x_; y = y_; return *this; }
		static ISize make(size_t x_, size_t y_)       { ISize sz; return sz.set(x_, y_); }
	};

	static ISize const ISizeZero  = {0, 0};

	//// Assignment operators
	//inline IRect& operator += (IRect& lhs, iv2 const& offset) { lhs.m_min += offset; lhs.m_max += offset; return lhs; }
	//inline IRect& operator -= (IRect& lhs, iv2 const& offset) { lhs.m_min -= offset; lhs.m_max -= offset; return lhs; }

	//// Binary operators
	//inline IRect operator + (IRect const& lhs, iv2 const& offset)   { IRect r = lhs; return r += offset; }
	//inline IRect operator - (IRect const& lhs, iv2 const& offset)   { IRect r = lhs; return r -= offset; }

	//// Equality operators
	//inline bool operator == (IRect const& lhs, IRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	//inline bool operator != (IRect const& lhs, IRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	//inline bool operator <  (IRect const& lhs, IRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	//inline bool operator >  (IRect const& lhs, IRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	//inline bool operator <= (IRect const& lhs, IRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	//inline bool operator >= (IRect const& lhs, IRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	//// Functions
	//IRect   Zero(IRect& rect);
	//bool    IsZero(IRect const& rect);
	//IRect   Inflate(IRect const& rect, int xmin, int ymin, int xmax, int ymax);
	//IRect   Inflate(IRect const& rect, int byX, int byY);
	//IRect   Inflate(IRect const& rect, int by);
	//IRect   Scale(IRect const& rect, int xmin, int ymin, int xmax, int ymax);
	//IRect   Scale(IRect const& rect, int byX, int byY);
	//IRect   Scale(IRect const& rect, int by);
	//IRect&  Encompass(IRect& rect, iv2 const& point);
	//IRect   Encompass(IRect const& rect, iv2 const& point);
	//IRect&  Encompass(IRect& lhs, IRect const& rhs);
	//FRect   Encompass(FRect const& lhs, FRect const& rhs);
	//bool    IsWithin(IRect const& rect, iv2 const& point);
	//bool    IsIntersection(IRect const& lhs, IRect const& rhs);
	//pr::v2  NormalisePoint(IRect const& rect, pr::v2 const& point, float ysign);
}

#endif
