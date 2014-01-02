//************************************************************************
// Range
//  Copyright © Rylogic Ltd 2011
//************************************************************************

#pragma once
#ifndef PR_COMMON_RANGE_H
#define PR_COMMON_RANGE_H

#ifndef PR_ASSERT
#   define PR_ASSERT_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif

namespace pr
{
	// A range representation
	template <typename T = int> struct Range
	{
		T m_begin;
		T m_end;

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

		// The number of elements in the range
		size_t size() const
		{
			return m_end - m_begin;
		}

		// True if 'rhs' is contained within the range [,)
		template <typename U> bool contains(U rhs) const
		{
			PR_ASSERT(PR_DBG, size() >= 0, "");
			return m_begin <= rhs && rhs < m_end;
		}

		// True if 'rhs' is contained within the range [,)
		template <typename U> bool contains(Range<U> const& rhs) const
		{
			PR_ASSERT(PR_DBG, size() >= 0 && rhs.size() >= 0, "");
			return contains(rhs.m_begin) && rhs.m_end <= m_end;
		}

		// True if 'rhs' intersects this range
		template <typename U> bool intersects(Range<U> const& rhs) const
		{
			PR_ASSERT(PR_DBG, size() >= 0 && rhs.size() >= 0, "");
			return m_begin < rhs.m_end && rhs.m_begin < m_end;
		}

		// Directly set the range
		void set(T const& begin, T const& end)
		{
			m_begin = begin;
			m_end = end;
		}

		// Set the number of elements in the range
		void resize(size_t count)
		{
			m_end = static_cast<T>(m_begin + count);
		}

		// Move the range
		void shift(int offset)
		{
			m_begin += offset;
			m_end   += offset;
		}

		// Return the midpoint of the range
		T mid() const
		{
			return m_begin + (m_end - m_begin)/2;
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

	// Returns true if 'sub_range' is wholely within 'range'
	template <typename T, typename U> inline bool IsWithin(Range<T> const& range, Range<U> const& sub_range)
	{
		return range.contains(sub_range);
	}

	// Returns true if the ranges 'lhs' and 'rhs' overlay
	template <typename T, typename U> inline bool Intersect(Range<T> const& lhs, Range<U> const& rhs)
	{
		return lhs.intersects(rhs);
	}

	// Expand 'range' to include 'rhs' if necessary
	template <typename T, typename U> inline void Encompass(Range<T>& range, U rhs)
	{
		if      (range.empty())        { range.m_begin = range.m_end = rhs; ++range.m_end; }
		else if (rhs <  range.m_begin) { range.m_begin = rhs; }
		else if (rhs >= range.m_end  ) { range.m_end   = rhs; ++range.m_end; }
	}

	// Expand 'range' to include 'rhs' if necessary
	template <typename T, typename U> inline void Encompass(Range<T>& range, Range<U> const& rhs)
	{
		if      (range.empty())               { range = rhs; }
		else if (rhs.m_begin < range.m_begin) { range.m_begin = rhs.m_begin; }
		else if (rhs.m_end   > range.m_end  ) { range.m_end   = rhs.m_end; }
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
				PR_CHECK(r0.size() , 5U);

				PR_CHECK(r0.contains(-1), false);
				PR_CHECK(r0.contains(0) , true );
				PR_CHECK(r0.contains(4) , true );
				PR_CHECK(r0.contains(5) , false);
				PR_CHECK(r0.contains(6) , false);

				PR_CHECK(r3.contains(r0), true  );
				PR_CHECK(r3.contains(r1), true  );
				PR_CHECK(r3.contains(r2), true  );
				PR_CHECK(r2.contains(r0), false );
				PR_CHECK(r2.contains(r1), false );
				PR_CHECK(r2.contains(r3), false );
				PR_CHECK(r1.contains(r0), false );
				PR_CHECK(r0.contains(r1), false );

				PR_CHECK(r3.intersects(r0), true );
				PR_CHECK(r3.intersects(r1), true );
				PR_CHECK(r3.intersects(r2), true );
				PR_CHECK(r2.intersects(r0), true );
				PR_CHECK(r2.intersects(r1), true );
				PR_CHECK(r2.intersects(r3), true );
				PR_CHECK(r1.intersects(r0), false);
				PR_CHECK(r0.intersects(r1), false);

				r0.shift(3);
				r1.shift(-2);
				PR_CHECK(r0 == r1, true);

				PR_CHECK(r3.mid() == r2.mid(), true);

				r0.shift(-3);
				r0.resize(3);
				PR_CHECK(r0.size(), 3U);
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
				PR_CHECK(r0.size()  ,5U);

				PR_CHECK(r0.contains(vec.begin())     ,true  );
				PR_CHECK(r0.contains(vec.begin() + 4) ,true  );
				PR_CHECK(r0.contains(vec.begin() + 5) ,false );
				PR_CHECK(r0.contains(vec.end())       ,false );

				PR_CHECK(r3.contains(r0), true  );
				PR_CHECK(r3.contains(r1), true  );
				PR_CHECK(r3.contains(r2), true  );
				PR_CHECK(r2.contains(r0), false );
				PR_CHECK(r2.contains(r1), false );
				PR_CHECK(r2.contains(r3), false );
				PR_CHECK(r1.contains(r0), false );
				PR_CHECK(r0.contains(r1), false );

				PR_CHECK(r3.intersects(r0), true  );
				PR_CHECK(r3.intersects(r1), true  );
				PR_CHECK(r3.intersects(r2), true  );
				PR_CHECK(r2.intersects(r0), true  );
				PR_CHECK(r2.intersects(r1), true  );
				PR_CHECK(r2.intersects(r3), true  );
				PR_CHECK(r1.intersects(r0), false );
				PR_CHECK(r0.intersects(r1), false );

				r0.shift(3);
				r1.shift(-2);
				PR_CHECK(r0 == r1, true);

				PR_CHECK(r3.mid() == r2.mid(), true);

				r0.shift(-3);
				r0.resize(3);
				PR_CHECK(r0.size(), 3U);
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
