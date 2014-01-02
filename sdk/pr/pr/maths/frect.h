//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_FRECT_H
#define PR_MATHS_FRECT_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector2.h"
#include "pr/maths/irect.h"

namespace pr
{
	struct FRect
	{
		v2 m_min;
		v2 m_max;

		FRect&  set(float xmin, float ymin, float xmax, float ymax)   { m_min.set(xmin, ymin); m_max.set(xmax, ymax); return *this; }
		FRect&  set(v2 const& min, v2 const& max)                     { m_min = min; m_max = max; return *this; }
		FRect&  shift(float xofs, float yofs)                         { m_min.x += xofs; m_max.x += xofs; m_min.y += yofs; m_max.y += yofs; return *this; }
		FRect&  inflate(float dx, float dy, int anchorX, int anchorY) { SizeX(SizeX() + dx, anchorX); SizeY(SizeY() + dy, anchorY); return *this; }
		float   X() const                                             { return m_min.x; }
		float   Y() const                                             { return m_min.y; }
		v2      Size() const                                          { return m_max - m_min; }
		float   SizeX() const                                         { return m_max.x - m_min.x; }
		float   SizeY() const                                         { return m_max.y - m_min.y; }
		float   Left() const                                          { return m_min.x; }
		float   Top() const                                           { return m_min.y; }
		float   Right() const                                         { return m_max.x; }
		float   Bottom() const                                        { return m_max.y; }
		v2      Centre() const                                        { return (m_min + m_max) * 0.5f; }
		float   DiametreSq() const                                    { return Length2Sq(m_max - m_min); }
		float   Diametre() const                                      { return pr::Sqrt(DiametreSq()); }
		float   Area() const                                          { return SizeX() * SizeY(); }
		float   Aspect() const                                        { return SizeX() / SizeY(); }
		void    SizeX(float sz, int anchor);
		void    SizeY(float sz, int anchor);
		FRect&  operator = (IRect const& rhs);

		static FRect make(float xmin, float ymin, float xmax, float ymax) { FRect rect; return rect.set(xmin, ymin, xmax, ymax); }
		static FRect make(v2 const& min, v2 const& max)                   { FRect rect; return rect.set(min, max); }
		static FRect make(IRect const& rect)                              { FRect frect; return frect = rect; }
	};

	FRect const FRectZero  = {v2Zero, v2Zero};
	FRect const FRectReset = {v2Max, -v2Max};
	FRect const FRectUnit  = {v2Zero, v2One};

	// Assignment operators
	inline FRect& operator += (FRect& lhs, v2 const& offset) { lhs.m_min += offset; lhs.m_max += offset; return lhs; }
	inline FRect& operator -= (FRect& lhs, v2 const& offset) { lhs.m_min -= offset; lhs.m_max -= offset; return lhs; }

	// Binary operators
	inline FRect operator + (FRect const& lhs, v2 const& offset) { FRect f = lhs; return f += offset; }
	inline FRect operator - (FRect const& lhs, v2 const& offset) { FRect f = lhs; return f -= offset; }

	// Equality operators
	inline bool FEqlZero(FRect const& rect)                      { return FEqlZero2(rect.m_min) && FEqlZero2(rect.m_max); }
	inline bool FEql(FRect const& lhs, FRect const& rhs)         { return FEql2(lhs.m_min, rhs.m_min) && FEql2(lhs.m_max, rhs.m_max); }
	inline bool operator == (FRect const& lhs, FRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (FRect const& lhs, FRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (FRect const& lhs, FRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (FRect const& lhs, FRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (FRect const& lhs, FRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (FRect const& lhs, FRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	FRect& Zero(FRect& rect);
	bool   IsZero(FRect const& rect);
	FRect  Inflate(FRect const& rect, float xmin, float ymin, float xmax, float ymax);
	FRect  Inflate(FRect const& rect, float byX, float byY);
	FRect  Inflate(FRect const& rect, float by);
	FRect  Scale(FRect const& rect, float xmin, float ymin, float xmax, float ymax);
	FRect  Scale(FRect const& rect, float byX, float byY);
	FRect  Scale(FRect const& rect, float by);
	FRect& Encompass(FRect& rect, v2 const& point);
	FRect  Encompass(FRect const& rect, v2 const& point);
	FRect& Encompass(FRect& lhs, FRect const& rhs);
	FRect  Encompass(FRect const& lhs, FRect const& rhs);
	bool   IsWithin(FRect const& rect, v2 const& point);
	bool   IsIntersection(FRect const& lhs, FRect const& rhs);
}

#endif
