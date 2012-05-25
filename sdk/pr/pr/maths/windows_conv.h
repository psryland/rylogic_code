//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#ifndef PR_MATHS_WINDOWS_CONV_H
#define PR_MATHS_WINDOWS_CONV_H
#pragma once

#include "pr/common/min_max_fix.h"
#include <windows.h>
#include <gdiplus.h>
#include "pr/maths/maths.h"

namespace pr
{
	inline long GetX(POINT const& pt) { return pt.x; }
	inline long GetY(POINT const& pt) { return pt.y; }
	inline long GetX(SIZE const& sz) { return sz.cx; }
	inline long GetY(SIZE const& sz) { return sz.cy; }
	
	// Convert from pr::v2
	template <typename ToType> inline ToType    To         (pr::v2 const& pt);
	template <>                inline POINT     To<POINT>  (pr::v2 const& pt) { POINT p = {int(pt.x), int(pt.y)}; return p; }
	
	// Convert from pr::iv2
	template <typename ToType> inline ToType    To         (pr::iv2 const& pt);
	template <>                inline POINT     To<POINT>  (pr::iv2 const& pt) { POINT p = {pt.x, pt.y}; return p; }
	template <>                inline SIZE      To<SIZE>   (pr::iv2 const& sz) { SIZE  s = {sz.x, sz.y}; return s; }
	
	// Convert from pr::IRect
	template <typename ToType> inline ToType    To         (pr::IRect const& rect);
	template <>                inline RECT      To<RECT>   (pr::IRect const& rect) { RECT r = {rect.m_min.x, rect.m_min.y, rect.m_max.x, rect.m_max.y}; return r; }
	
	// Convert from POINT
	template <typename ToType> inline ToType    To         (POINT const& pt);
	template <>                inline pr::v2    To<pr::v2> (POINT const& pt) { return pr::v2::make(float(pt.x), float(pt.y)); }
	template <>                inline pr::iv2   To<pr::iv2>(POINT const& pt) { return pr::iv2::make(pt.x, pt.y); }
	template <>                inline SIZE      To<SIZE>   (POINT const& pt) { SIZE s = {pt.x, pt.y}; return s; }
	
	// Convert from SIZE
	template <typename ToType> inline ToType    To         (SIZE const& pt);
	template <>                inline pr::iv2   To<pr::iv2>(SIZE const& sz) { return pr::iv2::make(sz.cx, sz.cy); }
	template <>                inline pr::v2    To<pr::v2> (SIZE const& sz) { return pr::v2::make(float(sz.cx), float(sz.cy)); }
	template <>                inline RECT      To<RECT>   (SIZE const& sz) { RECT r = {0, 0, sz.cx, sz.cy}; return r; }
	
	// Convert from RECT
	template <typename ToType> inline ToType         To                 (RECT const& rect);
	template <>                inline pr::IRect      To<pr::IRect>      (RECT const& rect) { return pr::IRect::make(rect.left, rect.top, rect.right, rect.bottom); }
	template <>                inline SIZE           To<SIZE>           (RECT const& rect) { SIZE s = {rect.right - rect.left, rect.bottom - rect.top}; return s; }
	template <>                inline Gdiplus::Rect  To<Gdiplus::Rect>  (RECT const& rect) { return Gdiplus::Rect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top); }
	template <>                inline Gdiplus::RectF To<Gdiplus::RectF> (RECT const& rect) { return Gdiplus::RectF(float(rect.left), float(rect.top), float(rect.right - rect.left), float(rect.bottom - rect.top)); }

	// Convert from Gdiplus::Rect
	template <typename ToType> inline ToType    To       (Gdiplus::Rect const& rect);
	template <>                inline RECT      To<RECT> (Gdiplus::Rect const& rect) { RECT r = {rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height}; return r; }
}

#endif
