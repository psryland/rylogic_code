//************************************************************************
// Range
//  Copyright (c) Rylogic Ltd 2011
//************************************************************************
#pragma once
#include <limits>
#include <cassert>

namespace pr
{
	// A range representation  (intended for numeric types only, might partially work for iterators)
	// Assume integral by default (so that iterators etc work)
	template <typename T = int, bool Integral = !std::is_floating_point<T>::value> struct Range
	{
		struct traits
		{
			static bool const is_integral = Integral;
		};

		T m_beg; // The first in the range
		T m_end; // One past the last in the range

		// The default empty range
		static Range Zero()
		{
			return Range(0,0);
		}

		// An invalid range. Used as an initialiser when finding a bounding range
		static Range Reset()
		{
			return Range{std::numeric_limits<T>::max(), std::numeric_limits<T>::lowest()};
		}

		// A range containing the maximum interval
		static Range Max()
		{
			return Range{std::numeric_limits<T>::lowest(), std::numeric_limits<T>::max()};
		}

		// Construct a range
		Range() = default;
		Range(T beg, T end)
			:m_beg(beg)
			,m_end(end)
		{}

		// True if this is an empty range
		bool empty() const
		{
			return m_beg == m_end;
		}

		// begin/end range support
		T begin() const
		{
			return m_beg;
		}
		T& begin()
		{
			return m_beg;
		}
		T end() const
		{
			return m_end;
		}
		T& end()
		{
			return m_end;
		}

		// The number of elements in or length of the range
		auto size() const -> decltype(m_end - m_beg)
		{
			return m_end - m_beg;
		}

		// Set the range
		void set(T const& begin, T const& end)
		{
			m_beg = begin;
			m_end = end;
		}

		// Set the number of elements in or length of the range
		template <typename U> void resize(U size)
		{
			m_end = static_cast<T>(m_beg + size);
		}

		// Move the range
		template <typename U> void shift(U offset)
		{
			m_beg = static_cast<T>(m_beg + offset);
			m_end = static_cast<T>(m_end + offset);
		}

		// Return the midpoint of the range
		T mid() const
		{
			return m_beg + (m_end - m_beg)/2;
		}

		// Returns the last value to be considered within the range
		T last() const
		{
			assert(size() >= 0 && "range is invalid");
			assert(!traits::is_integral || !empty() && "range is empty");
			return static_cast<T>(m_end - traits::is_integral);
		}

		// True if 'value' is within this range
		template <typename U> bool contains(U rhs) const
		{
			assert(size() >= 0 && "range is invalid");
			return traits::is_integral
				? rhs >= m_beg && rhs < m_end
				: rhs >= m_beg && rhs <= m_end;
		}

		// True if 'rhs' is entirely within this range
		template <typename U> bool contains(Range<U> rhs) const
		{
			assert(size() >= 0 && "range is invalid");
			assert(rhs.size() >= 0 && "range is invalid");
			return contains(rhs.m_beg) && rhs.m_end <= m_end;
		}

		// Returns true if this range and 'rhs' overlap
		template <typename U> bool intersects(Range<U> rhs) const
		{
			assert(size() >= 0 && "range is invalid");
			assert(rhs.size() >= 0 && "rhs range is invalid");
			return m_beg < rhs.m_end && rhs.m_beg < m_end;
		}

		// Grows the range to include 'rhs'
		template <typename U> U grow(U rhs)
		{
			if (rhs <  m_beg) { m_beg = static_cast<T>(rhs); }
			if (rhs >= m_end) { m_end = static_cast<T>(rhs + traits::is_integral); }
			return rhs;
		}

		// Grows the range to include 'rhs'
		template <typename U> Range<U> grow(Range<U> rhs)
		{
			// Don't treat !rhs.valid() as an error, it's the only way to grow an empty range
			if (rhs.size() < 0) return rhs;
			if (rhs.m_beg <  m_beg) { m_beg = rhs.m_beg; }
			if (rhs.m_end >= m_end) { m_end = rhs.m_end; }
			return rhs;
		}

		// Implicit conversion to a Range<U> if T is convertible to U
		template <typename U, typename = typename std::enable_if<std::is_convertible<T,U>::value>::type>
		operator Range<U>()
		{
			return Range<U>(m_beg, m_end);
		}
	};

	// Operators
	template <typename T, typename U> inline bool operator == (Range<T> const& lhs, Range<U> const& rhs)
	{
		return lhs.m_beg == rhs.m_beg && lhs.m_end == rhs.m_end;
	}
	template <typename T, typename U> inline bool operator != (Range<T> const& lhs, Range<U> const& rhs)
	{
		return !(lhs == rhs);
	}

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
	template <typename T, typename U> [[nodiscard]] inline Range<T> Union(Range<T> const& range, U rhs)
	{
		auto r = range;
		r.grow(rhs);
		return r;
	}
	template <typename T, typename U> inline U Grow(Range<T>& range, U rhs)
	{
		return range.grow(rhs);
	}

	// Expand 'range' to include 'rhs' if necessary
	template <typename T, typename U> [[nodiscard]] inline Range<T> Union(Range<T> const& range, Range<U> const& rhs)
	{
		auto r = range;
		r.grow(rhs);
		return r;
	}
	template <typename T, typename U> inline Range<U> const& Grow(Range<T>& range, Range<U> const& rhs)
	{
		return range.grow(rhs);
	}

	// Returns the intersection of 'lhs' with 'rhs'
	// If there is no intersection, returns [b,b) or [e,e) (from the lhs range).
	// Note: this means Intersect(a,b) != Intersect(b,a)
	template <typename T, typename U> [[nodiscard]] Range<T> Intersect(Range<T> const& lhs, Range<U> const& rhs)
	{
		assert(lhs.size() >= 0 && "lhs range is invalid");
		assert(rhs.size() >= 0 && "rhs range is invalid");
		if (rhs.m_end <= lhs.m_beg) return Range<T>::make(lhs.m_beg, lhs.m_beg);
		if (rhs.m_beg >= lhs.m_end) return Range<T>::make(lhs.m_end, lhs.m_end);
		return Range<T>::make(std::max(lhs.m_beg, rhs.m_beg), std::min(lhs.m_end, rhs.m_end));
	}

	// Clamp 'value' to within 'range'
	template <typename T, typename U> inline T Clamp(T value, Range<U> const& range)
	{
		return pr::Clamp<T>(value, range.m_beg, range.m_end - Range<U>::traits::is_integral);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	PRUnitTest(RangeTests)
	{
		{
			typedef pr::Range<int> IRange;
			IRange r0(0,5);
			IRange r1(5,10);
			IRange r2(3,7);
			IRange r3(0,10);

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

			IRange r4 = IRange::Reset();
			Grow(r4, 4);
			PR_CHECK(4, r4.m_beg);
			PR_CHECK(5, r4.m_end);
			PR_CHECK(1, r4.size());
			PR_CHECK(IsWithin(r4, 4), true);
		}
		{//IterRange
			typedef std::vector<int> Vec;
			typedef pr::Range<Vec::const_iterator> IRange;
			Vec vec; for (int i = 0; i != 10; ++i) vec.push_back(i);

			IRange r0(vec.begin(),vec.begin()+5);
			IRange r1(vec.begin()+5,vec.end());
			IRange r2(vec.begin()+3,vec.begin()+7);
			IRange r3(vec.begin(),vec.end());

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

			IRange r4(vec.end(),vec.begin());
			Grow(r4, vec.begin() + 4);
			PR_CHECK(vec.begin() + 4 == r4.m_beg, true);
			PR_CHECK(vec.begin() + 5 == r4.m_end, true);
			PR_CHECK(1, r4.size());
			PR_CHECK(IsWithin(r4, vec.begin() + 4), true);
		}
		{// Floating point range
			using FRange = pr::Range<float>;
			FRange r0(0.0f, 5.0f);
			FRange r1(5.0f, 10.0f);
			FRange r2(3.0f, 7.0f);
			FRange r3(0.0f, 10.0f);

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

			FRange r4 = FRange::Reset();
			Grow(r4, 4.0f);
			PR_CHECK(4.0f, r4.m_beg);
			PR_CHECK(4.0f, r4.m_end);
			PR_CHECK(0.0f, r4.size());
			PR_CHECK(IsWithin(r4, 4.0f), true);
		}
		{ // Implicit conversion
			Range<uint16> r0(0, 65535);
			Range<uint> r1;
			r1 = r0;
			PR_CHECK(r1 == Range<uint>(0, 65535), true);
		}
	}
}
#endif
