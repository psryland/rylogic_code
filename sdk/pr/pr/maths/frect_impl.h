//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_FRECT_IMPL_H
#define PR_MATHS_FRECT_IMPL_H

#include "pr/maths/frect.h"

namespace pr
{
	// Set the x dimension of the rect
	// 'anchor' : -1 = anchor the left, 0 = anchor centre, 1 = anchor right
	inline void FRect::SizeX(float sz, int anchor)
	{
		sz = m_max.x - m_min.x - sz;
		switch (anchor){
		case -1: m_max.x -= sz; break;
		case 0:  m_min.x += sz*0.5f; m_max.x -= sz*0.5f; break;
		case +1: m_min.x += sz; break;
		}
	}

	// Set the y dimension of the rect
	// 'anchor' : -1 = anchor the left, 0 = anchor centre, 1 = anchor right
	inline void FRect::SizeY(float sz, int anchor)
	{
		sz = m_max.y - m_min.y - sz;
		switch (anchor){
		case -1: m_max.y -= sz; break;
		case 0:  m_min.y += sz*0.5f; m_max.y -= sz*0.5f; break;
		case +1: m_min.y += sz; break;
		}
	}

	inline FRect& FRect::operator = (IRect const& rhs)
	{
		m_min = rhs.m_min;
		m_max = rhs.m_max;
		return *this;
	}
	inline FRect& Zero(FRect& rect)
	{
		return rect = FRectZero;
	}
	inline bool IsZero(FRect const& rect)
	{
		return IsZero2(rect.m_min) && IsZero2(rect.m_max);
	}
	inline FRect Inflate(FRect const& rect, float xmin, float ymin, float xmax, float ymax)
	{
		return FRect::make(rect.m_min.x - xmin, rect.m_min.y - ymin, rect.m_max.x + xmax, rect.m_max.y + ymax);
	}
	inline FRect Inflate(FRect const& rect, float byX, float byY)
	{
		return Inflate(rect, byX, byY, byX, byY);
	}
	inline FRect Inflate(FRect const& rect, float by)
	{
		return Inflate(rect, by, by);
	}
	inline FRect Scale(FRect const& rect, float xmin, float ymin, float xmax, float ymax)
	{
		float sx = rect.SizeX() * 0.5f;
		float sy = rect.SizeY() * 0.5f;
		return Inflate(rect, sx*xmin, sy*ymin, sx*xmax, sy*ymax);
	}
	inline FRect Scale(FRect const& rect, float byX, float byY)
	{
		return Scale(rect, byX, byY, byX, byY);
	}
	inline FRect Scale(FRect const& rect, float by)
	{
		return Scale(rect, by, by);
	}

	// Encompass 'point' in 'frect'
	inline FRect& Encompass(FRect& rect, v2 const& point)
	{
		if (point.x < rect.m_min.x) rect.m_min.x = point.x;
		if (point.y < rect.m_min.y) rect.m_min.y = point.y;
		if (point.x > rect.m_max.x) rect.m_max.x = point.x;
		if (point.y > rect.m_max.y) rect.m_max.y = point.y;
		return rect;
	}
	inline FRect Encompass(FRect const& rect, v2 const& point)
	{
		FRect r = rect;
		return Encompass(r, point);
	}

	// Encompass 'rhs' in 'lhs'
	inline FRect& Encompass(FRect& lhs, FRect const& rhs)
	{
		if (rhs.m_min.x < lhs.m_min.x) lhs.m_min.x = rhs.m_min.x;
		if (rhs.m_min.y < lhs.m_min.y) lhs.m_min.y = rhs.m_min.y;
		if (rhs.m_max.x > lhs.m_max.x) lhs.m_max.x = rhs.m_max.x;
		if (rhs.m_max.y > lhs.m_max.y) lhs.m_max.y = rhs.m_max.y;
		return lhs;
	}
	inline FRect Encompass(FRect const& lhs, FRect const& rhs)
	{
		FRect r = lhs;
		return Encompass(r, rhs);
	}

	// Returns true if 'point' is within the bounding volume
	inline bool IsWithin(FRect const& rect, v2 const& point)
	{
		return  point.x >= rect.m_min.x && point.x < rect.m_max.x &&
				point.y >= rect.m_min.y && point.y < rect.m_max.y;
	}

	// Returns true if 'lhs' and 'rhs' intersect
	inline bool IsIntersection(FRect const& lhs, FRect const& rhs)
	{
		return !(lhs.m_max.x < rhs.m_min.x || lhs.m_min.x > rhs.m_max.x ||
				 lhs.m_max.y < rhs.m_min.y || lhs.m_min.y > rhs.m_max.y);
	}
}

#endif
