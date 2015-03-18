//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once

#include "pr/maths/irect.h"

namespace pr
{
	// Set the x dimension of the rect
	// 'anchor' : -1 = anchor the left, 0 = anchor centre, 1 = anchor right
	inline void IRect::SizeX(int sz, int anchor)
	{
		sz = m_max.x - m_min.x - sz;
		switch (anchor){
		case -1: m_max.x -= sz; break;
		case 0:  m_min.x += (sz>>1); m_max.x -= (sz+1)>>1; break;
		case +1: m_min.x += sz; break;
		}
	}

	// Set the y dimension of the rect
	// 'anchor' : -1 = anchor the left, 0 = anchor centre, 1 = anchor right
	inline void IRect::SizeY(int sz, int anchor)
	{
		sz = m_max.y - m_min.y - sz;
		switch (anchor){
		case -1: m_max.y -= sz; break;
		case 0:  m_min.y += (sz>>1); m_max.y -= (sz+1)>>1; break;
		case +1: m_min.y += sz; break;
		}
	}

	inline IRect& IRect::operator = (FRect const& rhs)
	{
		m_min = rhs.m_min;
		m_max = rhs.m_max;
		return *this;
	}

	inline IRect Zero(IRect& rect)
	{
		return rect = IRectZero;
	}
	inline bool IsZero(IRect const& rect)
	{
		return IsZero2(rect.m_min) && IsZero2(rect.m_max);
	}

	inline IRect Inflate(IRect const& rect, int xmin, int ymin, int xmax, int ymax)
	{
		return IRect::make(rect.m_min.x - xmin, rect.m_min.y - ymin, rect.m_max.x + xmax, rect.m_max.y + ymax);
	}
	inline IRect Inflate(IRect const& rect, int byX, int byY)
	{
		return Inflate(rect, byX, byY, byX, byY);
	}
	inline IRect Inflate(IRect const& rect, int by)
	{
		return Inflate(rect, by, by);
	}

	inline IRect Scale(IRect const& rect, int xmin, int ymin, int xmax, int ymax)
	{
		int sx = rect.SizeX();
		int sy = rect.SizeY();
		return Inflate(rect, sx*xmin/2, sy*ymin/2, sx*xmax/2, sy*ymax/2);
	}
	inline IRect Scale(IRect const& rect, int byX, int byY)
	{
		return Scale(rect, byX, byY, byX, byY);
	}
	inline IRect Scale(IRect const& rect, int by)
	{
		return Scale(rect, by, by);
	}

	// Encompass 'point' in 'rect'
	inline IRect& Encompass(IRect& rect, iv2 const& point)
	{
		if (point.x < rect.m_min.x) rect.m_min.x = point.x;
		if (point.y < rect.m_min.y) rect.m_min.y = point.y;
		if (point.x > rect.m_max.x) rect.m_max.x = point.x;
		if (point.y > rect.m_max.y) rect.m_max.y = point.y;
		return rect;
	}
	inline IRect Encompass(IRect const& rect, iv2 const& point)
	{
		IRect r = rect;
		return Encompass(r, point);
	}

	// Encompass 'rhs' in 'lhs'
	inline IRect& Encompass(IRect& lhs, IRect const& rhs)
	{
		if (rhs.m_min.x < lhs.m_min.x) lhs.m_min.x = rhs.m_min.x;
		if (rhs.m_min.y < lhs.m_min.y) lhs.m_min.y = rhs.m_min.y;
		if (rhs.m_max.x > lhs.m_max.x) lhs.m_max.x = rhs.m_max.x;
		if (rhs.m_max.y > lhs.m_max.y) lhs.m_max.y = rhs.m_max.y;
		return lhs;
	}
	inline IRect Encompass(IRect const& lhs, IRect const& rhs)
	{
		IRect r = lhs;
		return Encompass(r, rhs);
	}

	// Returns true if 'point' is within the bounding volume
	inline bool IsWithin(IRect const& rect, iv2 const& point)
	{
		return  point.x >= rect.m_min.x && point.x < rect.m_max.x &&
				point.y >= rect.m_min.y && point.y < rect.m_max.y;
	}

	// Returns true if 'lhs' and 'rhs' intersect
	inline bool IsIntersection(IRect const& lhs, IRect const& rhs)
	{
		return !(lhs.m_max.x < rhs.m_min.x || lhs.m_min.x > rhs.m_max.x ||
				 lhs.m_max.y < rhs.m_min.y || lhs.m_min.y > rhs.m_max.y);
	}

	// Return 'point' scaled by the transform that maps 'rect' to the square (bottomleft:-1,-1)->(topright:1,1) 
	// 'xsign' should be -1 if the rect origin is on the right, false if on the left
	// 'ysign' should be -1 if the rect origin is at the top, false if at the bottom
	// Inverse of 'ScalePoint'
	inline pr::v2 NormalisePoint(IRect const& rect, pr::v2 const& point, float xsign, float ysign)
	{
		return pr::v2::make(
			xsign * (2.0f * (point.x - rect.m_min.x) / rect.SizeX() - 1.0f),
			ysign * (2.0f * (point.y - rect.m_min.y) / rect.SizeY() - 1.0f));
	}

	// Scales a normalised 'point' by the transform that maps the square (bottomleft:-1,-1)->(topright:1,1) to 'rect'
	// 'xsign' should be -1 if the rect origin is on the right, false if on the left
	// 'ysign' should be -1 if the rect origin is at the top, false if at the bottom
	// Inverse of 'NormalisedPoint'
	inline pr::v2 ScalePoint(IRect const& rect, pr::v2 const& point, float xsign, float ysign)
	{
		return pr::v2::make(
			rect.m_min.x + rect.SizeX() * (1.0f + xsign*point.x) / 2.0f,
			rect.m_min.y + rect.SizeY() * (1.0f + ysign*point.y) / 2.0f);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_irect)
		{
			{//NormalisePoint/ScalePoint
				auto pt = pr::v2::make(200, 300);
				auto rt = pr::IRect::make(50,50,200,300);
				auto nss = pr::NormalisePoint(rt, pt, 1.0f, 1.0f);
				auto ss  = pr::ScalePoint(rt, nss, 1.0f, 1.0f);
				PR_CHECK(FEql2(nss, pr::v2::make(1.0f, 1.0f)), true);
				PR_CHECK(FEql2(pt, ss), true);

				pt = pr::v2::make(200, 300);
				rt = pr::IRect::make(50,50,200,300);
				nss = pr::NormalisePoint(rt, pt, 1.0f, -1.0f);
				ss  = pr::ScalePoint(rt, nss, 1.0f, -1.0f);
				PR_CHECK(FEql2(nss, pr::v2::make(1.0f, -1.0f)), true);
				PR_CHECK(FEql2(pt, ss), true);

				pt = pr::v2::make(75, 130);
				rt = pr::IRect::make(50,50,200,300);
				nss = pr::NormalisePoint(rt, pt, 1.0f, -1.0f);
				ss  = pr::ScalePoint(rt, nss, 1.0f, -1.0f);
				PR_CHECK(FEql2(nss, pr::v2::make(-0.666667f, 0.36f)), true);
				PR_CHECK(FEql2(pt, ss), true);
			}
		}
	}
}
#endif