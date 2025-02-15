﻿//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector2.h"

namespace pr
{
	template <typename TVec = v2> struct Rectangle
	{
		using Vec2 = TVec;
		using elem_type = typename maths::is_vec<TVec>::elem_type;

		Vec2 m_min;
		Vec2 m_max;

		Rectangle() = default;
		Rectangle(Vec2 const& min, Vec2 const& max)
			:m_min(min)
			,m_max(max)
		{}
		Rectangle(elem_type xmin, elem_type ymin, elem_type xmax, elem_type ymax)
			:m_min(xmin, ymin)
			,m_max(xmax, ymax)
		{}
		template <typename V> explicit Rectangle(Rectangle<V> const& rhs)
			:m_min(Vec2(rhs.m_min))
			,m_max(Vec2(rhs.m_max))
		{}

		// Reset this rectangle to an invalid interval
		Rectangle& reset()
		{
			m_min = +limits<Vec2>::max();
			m_max = -limits<Vec2>::max();
			return *this;
		}

		// Returns true if this bbox does not bound anything
		bool empty() const
		{
			return x_cp(m_min) > x_cp(m_max) || y_cp(m_min) > y_cp(m_max);
		}

		// The minimum X coord of the rectangle
		elem_type X() const
		{
			return x_cp(m_min);
		}

		// The minimum Y coord of the rectangle
		elem_type Y() const
		{
			return y_cp(m_min);
		}

		// The width and height of the rectangle
		Vec2 Size() const
		{
			return m_max - m_min;
		}

		// Get/Set the width of the rectangle. 'anchor' : -1 = anchor the left, 0 = anchor centre, 1 = anchor right
		elem_type SizeX() const
		{
			return maths::comp<0>(m_max) - maths::comp<0>(m_min);
		}
		void SizeX(elem_type sz, int anchor)
		{
			switch (anchor)
			{
				case -1:
				{
					m_max = Vec2(x_cp(m_min) + sz, y_cp(m_max));
					break;
				}
				case  0:
				{
					auto w0 = sz / 2;
					auto w1 = sz - w0;
					auto c = (x_cp(m_min) + x_cp(m_max)) / 2;
					m_min = Vec2(c - w0, y_cp(m_min));
					m_max = Vec2(c + w1, y_cp(m_max));
					break;
				}
				case +1:
				{
					m_min = Vec2(x_cp(m_max) - sz, y_cp(m_min));
					break;
				}
			}
		}

		// Get/Set the height of the rectangle. 'anchor' : -1 = anchor the top, 0 = anchor centre, 1 = anchor bottom
		elem_type SizeY() const
		{
			return maths::comp<1>(m_max) - maths::comp<1>(m_min);
		}
		void SizeY(elem_type sz, int anchor)
		{
			switch (anchor)
			{
				case -1:
				{
					m_max = Vec2(x_cp(m_max), y_cp(m_min) + sz);
					break;
				}
				case  0:
				{
					auto h0 = sz / 2;
					auto h1 = sz - h0;
					auto c = (y_cp(m_min) + y_cp(m_max)) / 2;
					m_min = Vec2(x_cp(m_min), c - h0);
					m_max = Vec2(x_cp(m_max), c + h1);
					break;
				}
				case +1:
				{
					m_min = Vec2(x_cp(m_min), y_cp(m_max) - sz);
					break;
				}
			}
		}

		// The left edge position (x value)
		elem_type Left() const
		{
			return maths::comp<0>(m_min);
		}

		// The top edge position (y value)
		elem_type Top() const
		{
			return maths::comp<1>(m_min);
		}

		// The right edge position (x value)
		elem_type Right() const
		{
			return maths::comp<0>(m_max);
		}

		// The bottom edge position (y value)
		elem_type Bottom() const
		{
			return maths::comp<1>(m_max);
		}

		// The centre position of the rectangle
		Vec2 Centre() const
		{
			return (m_min + m_max) / 2;
		}

		// The diagonal length of the rectangle
		elem_type DiametreSq() const
		{
			return Length2Sq(m_max - m_min);
		}
		float Diametre() const
		{
			return Sqrt(DiametreSq());
		}

		// The area of the rectangle
		elem_type Area() const
		{
			return SizeX() * SizeY();
		}

		// The aspect ratio (width/height)
		float Aspect() const
		{
			return float(SizeX()) / SizeY();
		}

		// Assignment from integer rectangle
		template <typename V> Rectangle& operator = (Rectangle<V> const& rhs)
		{
			m_min = rhs.m_min;
			m_max = rhs.m_max;
			return *this;
		}

		#pragma region Operators
		friend bool operator == (Rectangle const& lhs, Rectangle const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
		friend bool operator != (Rectangle const& lhs, Rectangle const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
		friend bool operator <  (Rectangle const& lhs, Rectangle const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
		friend bool operator >  (Rectangle const& lhs, Rectangle const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
		friend bool operator <= (Rectangle const& lhs, Rectangle const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
		friend bool operator >= (Rectangle const& lhs, Rectangle const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }
		friend Rectangle& operator += (Rectangle& lhs, Vec2 const& offset)
		{
			lhs.m_min += offset;
			lhs.m_max += offset;
			return lhs;
		}
		friend Rectangle& operator -= (Rectangle& lhs, Vec2 const& offset)
		{
			lhs.m_min -= offset;
			lhs.m_max -= offset;
			return lhs;
		}
		friend Rectangle operator + (Rectangle const& lhs, Vec2 const& offset)
		{
			auto f = lhs;
			return f += offset;
		}
		friend Rectangle operator - (Rectangle const& lhs, Vec2 const& offset)
		{
			auto f = lhs;
			return f -= offset;
		}
		friend bool FEql(Rectangle const& lhs, Rectangle const& rhs)
		{
			return
				FEql2(lhs.m_min, rhs.m_min) &&
				FEql2(lhs.m_max, rhs.m_max);
		}
		#pragma endregion
	};

	using FRect = Rectangle<Vec2<float, void>>;
	using IRect = Rectangle<Vec2<int, void>>;

	#pragma region Constants
	static FRect const FRectZero  = {v2Zero, v2Zero};
	static FRect const FRectReset = {v2Max, -v2Max};
	static FRect const FRectUnit  = {v2Zero, v2One};
	static IRect const IRectZero  = {iv2Zero, iv2Zero};
	static IRect const IRectReset = {iv2Max, -iv2Max};
	static IRect const IRectUnit  = {iv2Zero, iv2One};
	#pragma endregion

	#pragma region Functions

	// Returns 'rect' offset by 'dx,dy'
	template <typename V, typename E = Rectangle<V>::elem_type> Rectangle<V> Shifted(Rectangle<V> const& rect, E dx, E dy)
	{
		return Rectangle<V>(
			maths::comp<0>(rect.m_min) + dx, maths::comp<1>(rect.m_min) + dy,
			maths::comp<0>(rect.m_max) + dx, maths::comp<1>(rect.m_max) + dy);
	}

	// Returns 'rect' inflated by the given values. Positive values increase the rect size, negative values decrease it
	template <typename V, typename E = Rectangle<V>::elem_type> inline Rectangle<V> Inflated(Rectangle<V> const& rect, E dxmin, E dymin, E dxmax, E dymax)
	{
		return Rectangle<V,E>(
			maths::comp<0>(rect.m_min) - dxmin, maths::comp<1>(rect.m_min) - dymin,
			maths::comp<0>(rect.m_max) + dxmax, maths::comp<1>(rect.m_max) + dymax);
	}
	template <typename V, typename E = Rectangle<V>::elem_type> inline Rectangle<V> Inflated(Rectangle<V> const& rect, E dx, E dy)
	{
		return Inflated(rect, dx, dy, dx, dy);
	}
	template <typename V, typename E = Rectangle<V>::elem_type> inline Rectangle<V> Inflated(Rectangle<V> const& rect, E by)
	{
		return Inflated(rect, by, by);
	}

	// Returns 'rect' inflated by the given values, scaled by the current width/height of 'rect'
	template <typename V, typename E = Rectangle<V>::elem_type> inline Rectangle<V> Scale(Rectangle<V> const& rect, E xmin, E ymin, E xmax, E ymax)
	{
		auto sx = rect.SizeX() / 2;
		auto sy = rect.SizeY() / 2;
		return Inflated(rect, sx*xmin, sy*ymin, sx*xmax, sy*ymax);
	}
	template <typename V, typename E = Rectangle<V>::elem_type> inline Rectangle<V> Scale(Rectangle<V> const& rect, E dx, E dy)
	{
		return Scale(rect, dx, dy, dx, dy);
	}
	template <typename V, typename E = Rectangle<V>::elem_type> inline Rectangle<V> Scale(Rectangle<V> const& rect, E by)
	{
		return Scale(rect, by, by);
	}

	// Include 'point' in 'frect'
	template <typename V, typename E = Rectangle<V>::elem_type> inline Rectangle<V>& Grow(Rectangle<V>& rect, V const& point)
	{
		rect.m_min = Min(point, rect.m_min);
		rect.m_max = Max(point, rect.m_max);
		return rect;
	}
	template <typename V, typename E = Rectangle<V>::elem_type> [[nodiscard]] inline Rectangle<V> Union(Rectangle<V> const& rect, V const& point)
	{
		auto r = rect;
		return Grow(r, point);
	}

	// Include 'rhs' in 'lhs'
	template <typename V, typename E = Rectangle<V>::elem_type> inline Rectangle<V>& Grow(Rectangle<V>& lhs, Rectangle<V> const& rhs)
	{
		lhs.m_min = Min(lhs.m_min, rhs.m_min);
		lhs.m_max = Max(lhs.m_max, rhs.m_max);
		return lhs;
	}
	template <typename V, typename E = Rectangle<V>::elem_type> [[nodiscard]] inline Rectangle<V> Union(Rectangle<V> const& lhs, Rectangle<V> const& rhs)
	{
		auto r = lhs;
		return Grow(r, rhs);
	}

	// Returns true if 'point' is within the bounding volume
	template <typename V> inline bool IsWithin(Rectangle<V> const& rect, V const& point)
	{
		return
			maths::comp<0>(point) >= maths::comp<0>(rect.m_min) && maths::comp<0>(point) < maths::comp<0>(rect.m_max) &&
			maths::comp<1>(point) >= maths::comp<1>(rect.m_min) && maths::comp<1>(point) < maths::comp<1>(rect.m_max);
	}

	// Returns true if 'lhs' and 'rhs' intersect
	template <typename V> inline bool IsIntersection(Rectangle<V> const& lhs, Rectangle<V> const& rhs)
	{
		return
			!(maths::comp<0>(lhs.m_max) < maths::comp<0>(rhs.m_min) || maths::comp<0>(lhs.m_min) > maths::comp<0>(rhs.m_max) ||
			  maths::comp<1>(lhs.m_max) < maths::comp<1>(rhs.m_min) || maths::comp<1>(lhs.m_min) > maths::comp<1>(rhs.m_max));
	}

	// Return 'point' scaled by the transform that maps 'rect' to the square (bottom left:-1,-1)->(top right:1,1) 
	// 'xsign' should be -1 if the rect origin is on the right, +1 if on the left
	// 'ysign' should be -1 if the rect origin is at the top, +1 if at the bottom
	// Inverse of 'ScalePoint'
	template <typename V> inline v2 NormalisePoint(Rectangle<V> const& rect, v2 const& point, float xsign, float ysign)
	{
		return v2(
			xsign * (2.0f * (point.x - maths::comp<0>(rect.m_min)) / rect.SizeX() - 1.0f),
			ysign * (2.0f * (point.y - maths::comp<1>(rect.m_min)) / rect.SizeY() - 1.0f));
	}

	// Scales a normalised 'point' by the transform that maps the square (bottom left:-1,-1)->(top right:1,1) to 'rect'
	// 'xsign' should be -1 if the rect origin is on the right, +1 if on the left
	// 'ysign' should be -1 if the rect origin is at the top, +1 if at the bottom
	// Inverse of 'NormalisedPoint'
	template <typename V> inline v2 ScalePoint(Rectangle<V> const& rect, v2 const& point, float xsign, float ysign)
	{
		return v2(
			maths::comp<0>(rect.m_min) + rect.SizeX() * (1.0f + xsign*point.x) / 2.0f,
			maths::comp<1>(rect.m_min) + rect.SizeY() * (1.0f + ysign*point.y) / 2.0f);
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(RectangleTests)
	{
		{//NormalisePoint/ScalePoint
			auto pt = v2(200, 300);
			auto rt = IRect(50,50,200,300);
			auto nss = NormalisePoint(rt, pt, 1.0f, 1.0f);
			auto ss  = ScalePoint(rt, nss, 1.0f, 1.0f);
			PR_CHECK(FEql(nss, v2(1.0f, 1.0f)), true);
			PR_CHECK(FEql(pt, ss), true);

			pt = v2(200, 300);
			rt = IRect(50,50,200,300);
			nss = NormalisePoint(rt, pt, 1.0f, -1.0f);
			ss  = ScalePoint(rt, nss, 1.0f, -1.0f);
			PR_CHECK(FEql(nss, v2(1.0f, -1.0f)), true);
			PR_CHECK(FEql(pt, ss), true);

			pt = v2(75, 130);
			rt = IRect(50,50,200,300);
			nss = NormalisePoint(rt, pt, 1.0f, -1.0f);
			ss  = ScalePoint(rt, nss, 1.0f, -1.0f);
			PR_CHECK(FEql(nss, v2(-0.666667f, 0.36f)), true);
			PR_CHECK(FEql(pt, ss), true);
		}
	}
}
#endif