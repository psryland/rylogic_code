//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_IRECT_H
#define PR_MATHS_IRECT_H

#include "pr/maths/constants.h"
#include "pr/maths/forward.h"
#include "pr/maths/scalar.h"
#include "pr/maths/ivector2.h"
#include "pr/maths/frect.h"

namespace pr
{
	struct IRect
	{
		iv2 m_min;
		iv2 m_max;
		
		IRect&  set(int xmin, int ymin, int xmax, int ymax)       { m_min.set(xmin, ymin); m_max.set(xmax, ymax); return *this; }
		IRect&  set(iv2 const& min, iv2 const& max)               { m_min = min; m_max = max; return *this; }
		IRect&  shift(int xofs, int yofs)                         { m_min.x += xofs; m_max.x += xofs; m_min.y += yofs; m_max.y += yofs; return *this; }
		IRect&  inflate(int dx, int dy, int anchorX, int anchorY) { SizeX(SizeX() + dx, anchorX); SizeY(SizeY() + dy, anchorY); return *this; }
		int     X() const                                         { return m_min.x; }
		int     Y() const                                         { return m_min.y; }
		iv2     Size() const                                      { return m_max - m_min; }
		int     SizeX() const                                     { return m_max.x - m_min.x; }
		int     SizeY() const                                     { return m_max.y - m_min.y; }
		int     Left() const                                      { return m_min.x; }
		int     Top() const                                       { return m_min.y; }
		int     Right() const                                     { return m_max.x; }
		int     Bottom() const                                    { return m_max.y; }
		iv2     CentreI() const                                   { return (m_min + m_max) / 2; }
		v2      CentreF() const                                   { return (m_min + m_max) * 0.5f; }
		int     DiametreSq() const                                { return AsInt(Length2Sq(m_max - m_min)); }
		float   Diametre() const                                  { return pr::Sqrt(AsReal(DiametreSq())); }
		int     Area() const                                      { return SizeX() * SizeY(); }
		float   Aspect() const                                    { return SizeX() / float(SizeY()); }
		void    SizeX(int sz, int anchor);
		void    SizeY(int sz, int anchor);
		IRect&  operator = (FRect const& rhs);
		
		static IRect make(int xmin, int ymin, int xmax, int ymax) { IRect rect; return rect.set(xmin, ymin, xmax, ymax); }
		static IRect make(iv2 const& min, iv2 const& max)         { IRect rect; return rect.set(min, max); }
		static IRect make(FRect const& rect)                      { IRect irect; return irect = rect; }
	};
	
	IRect const IRectZero  = {0, 0, 0, 0};
	IRect const IRectReset = {maths::int_max, maths::int_max, -maths::int_max, -maths::int_max};
	IRect const IRectUnit  = {0, 0, 1, 1};
	
	// Assignment operators
	inline IRect& operator += (IRect& lhs, iv2 const& offset) { lhs.m_min += offset; lhs.m_max += offset; return lhs; }
	inline IRect& operator -= (IRect& lhs, iv2 const& offset) { lhs.m_min -= offset; lhs.m_max -= offset; return lhs; }
	
	// Binary operators
	inline IRect operator + (IRect const& lhs, iv2 const& offset)   { IRect r = lhs; return r += offset; }
	inline IRect operator - (IRect const& lhs, iv2 const& offset)   { IRect r = lhs; return r -= offset; }
	
	// Equality operators
	inline bool operator == (IRect const& lhs, IRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (IRect const& lhs, IRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (IRect const& lhs, IRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (IRect const& lhs, IRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (IRect const& lhs, IRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (IRect const& lhs, IRect const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }
	
	// Functions
	IRect   Zero(IRect& rect);
	bool    IsZero(IRect const& rect);
	IRect   Inflate(IRect const& rect, int xmin, int ymin, int xmax, int ymax);
	IRect   Inflate(IRect const& rect, int byX, int byY);
	IRect   Inflate(IRect const& rect, int by);
	IRect   Scale(IRect const& rect, int xmin, int ymin, int xmax, int ymax);
	IRect   Scale(IRect const& rect, int byX, int byY);
	IRect   Scale(IRect const& rect, int by);
	IRect&  Encompase(IRect& rect, iv2 const& point);
	IRect   Encompase(IRect const& rect, iv2 const& point);
	IRect&  Encompase(IRect& lhs, IRect const& rhs);
	FRect   Encompase(FRect const& lhs, FRect const& rhs);
	bool    IsWithin(IRect const& rect, iv2 const& point);
	bool    IsIntersection(IRect const& lhs, IRect const& rhs);
	pr::v2  NormalisePoint(IRect const& rect, pr::v2 const& point, float ysign);
}

#endif
