//************************************************************************
// Range
//  Copyright (c) Rylogic Ltd 2011
//************************************************************************

#pragma once
#ifndef PR_COMMON_RANGE_H
#define PR_COMMON_RANGE_H

#include <limits>

#ifndef PR_ASSERT
#   define PR_ASSERT_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif

namespace pr
{
	// A range representation  (intended for numeric types only, might partially work for iterators)
	template <typename T = int> struct Range
	{
		// By default assume integral type traits
		template <typename U> struct traits_impl         { enum { is_integral = true }; };
		template <>           struct traits_impl<double> { enum { is_integral = false }; };
		template <>           struct traits_impl<float>  { enum { is_integral = false }; };
		struct traits :traits_impl<T> {};

		T m_begin; // The first in the range
		T m_end;   // One past the last in the range

		/// <summary>The default empty range</summary>
		static Range Zero() { return Range::make(0,0); }

		/// <summary>An invalid range. Used as an initialiser when finding a bounding range</summary>
		static Range Invalid() { return Range::make(std::numeric_limits<T>::max(), std::numeric_limits<T>::lowest()); }

		// Construct a range
		static Range make(T begin, T end)
		{
			Range r = {begin, end};
			return r;
		}

		// True if this is an empty range
		bool empty() const
		{
			return m_begin == m_end;
		}

		// The number of elements in or length of the range
		auto size() const -> decltype(T() - T())
		{
			return m_end - m_begin;
		}

		// Set the range
		void set(T const& begin, T const& end)
		{
			m_begin = begin;
			m_end = end;
		}

		// Set the number of elements in or length of the range
		template <typename U> void resize(U size)
		{
			m_end = static_cast<T>(m_begin + size);
		}

		// Move the range
		template <typename U> void shift(U offset)
		{
			m_begin = static_cast<T>(m_begin + offset);
			m_end   = static_cast<T>(m_end   + offset);
		}

		// Return the midpoint of the range
		T mid() const
		{
			return m_begin + (m_end - m_begin)/2;
		}

		// Returns the last value to be considered within the range
		T last() const
		{
			PR_ASSERT(PR_DBG, size() >= 0, "range is invalid");
			PR_ASSERT(PR_DBG, !traits::is_integral || !empty(), "range is empty");
			return static_cast<T>(m_end - traits::is_integral);
		}

		// True if 'value' is within this range
		template <typename U> bool contains(U rhs) const
		{
			PR_ASSERT(PR_DBG, size() >= 0, "range is invalid");
			return traits::is_integral
				? rhs >= m_begin && rhs < m_end
				: rhs >= m_begin && rhs <= m_end;
		}

		// True if 'rhs' is entirely within this range
		template <typename U> bool contains(Range<U> rhs) const
		{
			PR_ASSERT(PR_DBG, size() >= 0, "range is invalid");
			PR_ASSERT(PR_DBG, rhs.size() >= 0, "range is invalid");
			return contains(rhs.m_begin) && rhs.m_end <= m_end;
		}

		// Returns true if this range and 'rhs' overlap
		template <typename U> bool intersects(Range<U> rhs) const
		{
			PR_ASSERT(PR_DBG, size() >= 0, "range is invalid");
			PR_ASSERT(PR_DBG, rhs.size() >= 0, "rhs range is invalid");
			return m_begin < rhs.m_end && rhs.m_begin < m_end;
		}

		// Grows the range to include 'rhs'
		template <typename U> Range<T>& encompass(U rhs)
		{
			if (rhs < m_begin) { m_begin = static_cast<T>(rhs); }
			if (rhs >= m_end ) { m_end   = static_cast<T>(rhs + traits::is_integral); }
			return *this;
		}

		// Grows the range to include 'rhs'
		template <typename U> Range<T>& encompass(Range<U> rhs)
		{
			PR_ASSERT(PR_DBG, rhs.size() >= 0, "rhs range is invalid");
			if (rhs.m_begin < m_begin) { m_begin = rhs.m_begin; }
			if (rhs.m_end  >= m_end  ) { m_end   = rhs.m_end; }
			return *this;
		}
	};

	// Operators
	template <typename T, typename U> inline bool operator == (Range<T> const& lhs, Range<U> const& rhs) { return lhs.m_begin == rhs.m_begin && lhs.m_end == rhs.m_end; }
	template <typename T, typename U> inline bool operator != (Range<T> const& lhs, Range<U> const& rhs) { return !(lhs == rhs); }

	// Returns true if 'rhs' is with 'range'
	template <typename T, typename U> inline bool IsWithin(Range<T> const& range, U rhs)
	{
		return range.contains(rhs);
	}

	// Returns true if 'sub_range' is entirely within 'range'
	template <typename T, typename U> inline bool IsWithin(Range<T> const& range, Range<U> const& sub_range)
	{
		return range.contains(sub_range);
	}

	// Returns true if the ranges 'lhs' and 'rhs' overlap
	template <typename T, typename U> inline bool Intersects(Range<T> const& lhs, Range<U> const& rhs)
	{
		return lhs.intersects(rhs);
	}

	// Expand 'range' if necessary to include 'rhs'
	template <typename T, typename U> inline Range<T>& Encompass(Range<T>& range, U rhs)
	{
		return range.encompass(rhs);
	}
	template <typename T, typename U> inline Range<T>  Encompass(Range<T> const& range, U rhs)
	{
		Range<T> r = range;
		return Encompass(r, rhs);
	}

	// Expand 'range' to include 'rhs' if necessary
	template <typename T, typename U> inline Range<T>& Encompass(Range<T>& range, Range<U> const& rhs)
	{
		return range.encompass(rhs);
	}
	template <typename T, typename U> inline Range<T>  Encompass(Range<T> const& range, Range<U> const& rhs)
	{
		Range<T> r = range;
		return Encompass(r, rhs);
	}

	// Returns the intersection of 'lhs' with 'rhs'
	// If there is no intersection, returns [b,b) or [e,e) (from the lhs range).
	// Note: this means Intersect(a,b) != Intersect(b,a)
	template <typename T, typename U> Range<T> Intersect(Range<T> const& lhs, Range<U> const& rhs)
	{
		PR_ASSERT(PR_DBG, lhs.size() >= 0, "lhs range is invalid");
		PR_ASSERT(PR_DBG, rhs.size() >= 0, "rhs range is invalid");
		if (rhs.m_end   <= lhs.m_begin) return Range<T>::make(lhs.m_begin, lhs.m_begin);
		if (rhs.m_begin >= lhs.m_end  ) return Range<T>::make(lhs.m_end  , lhs.m_end  );
		return Range<T>::make(std::max(lhs.m_begin, rhs.m_begin), std::min(lhs.m_end, rhs.m_end));
	}

	// Returns a range that is the union of this range with 'rhs'
	template <typename T, typename U> Range<T> Union(Range<T> const& lhs, Range<U> const& rhs)
	{
		PR_ASSERT(PR_DBG, lhs.size() >= 0, "lhs range is invalid");
		PR_ASSERT(PR_DBG, rhs.size() >= 0, "rhs range is invalid");
		return Range<T>::make(std::min(lhs.m_begin, rhs.m_begin), std::max(lhs.m_end, rhs.m_end));
	}

	// Clamp 'value' to within 'range'
	template <typename T, typename U> inline T Clamp(T value, Range<U> const& range)
	{
		return pr::Clamp<T>(value, range.m_begin, range.m_end - Range<U>::traits::is_integral);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(TestName)
		{
			using namespace pr;
			{
				typedef pr::Range<int> IRange;
				IRange r0 = IRange::make(0,5);
				IRange r1 = IRange::make(5,10);
				IRange r2 = IRange::make(3,7);
				IRange r3 = IRange::make(0,10);

				PR_CHECK(r0.empty(), false);
				PR_CHECK(r0.size() , 5);

				PR_CHECK(IsWithin(r0, -1) , false);
				PR_CHECK(IsWithin(r0,  0) , true );
				PR_CHECK(IsWithin(r0,  4) , true );
				PR_CHECK(IsWithin(r0,  5) , false);
				PR_CHECK(IsWithin(r0,  6) , false);

				PR_CHECK(IsWithin(r3, r0), true  );
				PR_CHECK(IsWithin(r3, r1), true  );
				PR_CHECK(IsWithin(r3, r2), true  );
				PR_CHECK(IsWithin(r2, r0), false );
				PR_CHECK(IsWithin(r2, r1), false );
				PR_CHECK(IsWithin(r2, r3), false );
				PR_CHECK(IsWithin(r1, r0), false );
				PR_CHECK(IsWithin(r0, r1), false );

				PR_CHECK(Intersects(r3, r0), true );
				PR_CHECK(Intersects(r3, r1), true );
				PR_CHECK(Intersects(r3, r2), true );
				PR_CHECK(Intersects(r2, r0), true );
				PR_CHECK(Intersects(r2, r1), true );
				PR_CHECK(Intersects(r2, r3), true );
				PR_CHECK(Intersects(r1, r0), false);
				PR_CHECK(Intersects(r0, r1), false);

				r0.shift(3);
				r1.shift(-2);
				PR_CHECK(r0 == r1, true);

				PR_CHECK(r3.mid() == r2.mid(), true);

				r0.shift(-3);
				r0.resize(3);
				PR_CHECK(r0.size(), 3);

				IRange r4 = IRange::Invalid();
				Encompass(r4, 4);
				PR_CHECK(4, r4.m_begin);
				PR_CHECK(5, r4.m_end);
				PR_CHECK(1, r4.size());
				PR_CHECK(IsWithin(r4, 4), true);
			}
			{//IterRange
				typedef std::vector<int> Vec;
				typedef pr::Range<Vec::const_iterator> IRange;
				Vec vec; for (int i = 0; i != 10; ++i) vec.push_back(i);

				IRange r0 = IRange::make(vec.begin(),vec.begin()+5);
				IRange r1 = IRange::make(vec.begin()+5,vec.end());
				IRange r2 = IRange::make(vec.begin()+3,vec.begin()+7);
				IRange r3 = IRange::make(vec.begin(),vec.end());

				PR_CHECK(r0.empty() ,false);
				PR_CHECK(r0.size()  ,5);

				PR_CHECK(IsWithin(r0, vec.begin()     ) ,true  );
				PR_CHECK(IsWithin(r0, vec.begin() + 4 ) ,true  );
				PR_CHECK(IsWithin(r0, vec.begin() + 5 ) ,false );
				PR_CHECK(IsWithin(r0, vec.end()       ) ,false );

				PR_CHECK(IsWithin(r3, r0), true  );
				PR_CHECK(IsWithin(r3, r1), true  );
				PR_CHECK(IsWithin(r3, r2), true  );
				PR_CHECK(IsWithin(r2, r0), false );
				PR_CHECK(IsWithin(r2, r1), false );
				PR_CHECK(IsWithin(r2, r3), false );
				PR_CHECK(IsWithin(r1, r0), false );
				PR_CHECK(IsWithin(r0, r1), false );

				PR_CHECK(Intersects(r3, r0), true  );
				PR_CHECK(Intersects(r3, r1), true  );
				PR_CHECK(Intersects(r3, r2), true  );
				PR_CHECK(Intersects(r2, r0), true  );
				PR_CHECK(Intersects(r2, r1), true  );
				PR_CHECK(Intersects(r2, r3), true  );
				PR_CHECK(Intersects(r1, r0), false );
				PR_CHECK(Intersects(r0, r1), false );

				r0.shift(3);
				r1.shift(-2);
				PR_CHECK(r0 == r1, true);

				PR_CHECK(r3.mid() == r2.mid(), true);

				r0.shift(-3);
				r0.resize(3);
				PR_CHECK(r0.size(), 3);

				IRange r4 = IRange::make(vec.end(),vec.begin());
				Encompass(r4, vec.begin() + 4);
				PR_CHECK(vec.begin() + 4 == r4.m_begin, true);
				PR_CHECK(vec.begin() + 5 == r4.m_end  , true);
				PR_CHECK(1, r4.size());
				PR_CHECK(IsWithin(r4, vec.begin() + 4), true);
			}
			{// Floating point range
				typedef pr::Range<float> FRange;

				auto r0 = FRange::make(0.0f, 5.0f);
				auto r1 = FRange::make(5.0f, 10.0f);
				auto r2 = FRange::make(3.0f, 7.0f);
				auto r3 = FRange::make(0.0f, 10.0f);

				PR_CHECK(r0.empty(), false);
				PR_CHECK(r0.size() , 5.0f);

				PR_CHECK(IsWithin(r0, -1.0f), false);
				PR_CHECK(IsWithin(r0, 0.0f) , true );
				PR_CHECK(IsWithin(r0, 4.0f) , true );
				PR_CHECK(IsWithin(r0, 5.0f) , true );
				PR_CHECK(IsWithin(r0, 6.0f) , false);

				PR_CHECK(IsWithin(r3, r0), true  );
				PR_CHECK(IsWithin(r3, r1), true  );
				PR_CHECK(IsWithin(r3, r2), true  );
				PR_CHECK(IsWithin(r2, r0), false );
				PR_CHECK(IsWithin(r2, r1), false );
				PR_CHECK(IsWithin(r2, r3), false );
				PR_CHECK(IsWithin(r1, r0), false );
				PR_CHECK(IsWithin(r0, r1), false );

				PR_CHECK(Intersects(r3, r0), true );
				PR_CHECK(Intersects(r3, r1), true );
				PR_CHECK(Intersects(r3, r2), true );
				PR_CHECK(Intersects(r2, r0), true );
				PR_CHECK(Intersects(r2, r1), true );
				PR_CHECK(Intersects(r2, r3), true );
				PR_CHECK(Intersects(r1, r0), false);
				PR_CHECK(Intersects(r0, r1), false);

				r0.shift(3.0f);
				r1.shift(-2.0f);
				PR_CHECK(r0 == r1, true);

				PR_CHECK(r3.mid() == r2.mid(), true);

				r0.shift(-3.0f);
				r0.resize(3.0f);
				PR_CHECK(r0.size(), 3.0f);

				FRange r4 = FRange::Invalid();
				Encompass(r4, 4.0f);
				PR_CHECK(4.0f, r4.m_begin);
				PR_CHECK(4.0f, r4.m_end);
				PR_CHECK(0.0f, r4.size());
				PR_CHECK(IsWithin(r4, 4.0f), true);
			}
		}
	}
}
#endif

#ifdef PR_ASSERT_DEFINED
#   undef PR_ASSERT_DEFINED
#   undef PR_ASSERT
#endif

#endif
