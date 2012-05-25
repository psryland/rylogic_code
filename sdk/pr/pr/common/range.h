//************************************************************************
// Range
//  Copyright © Rylogic Ltd 2011
//************************************************************************
#ifndef PR_RANGE_H
#define PR_RANGE_H
	
//"pr/common/assert.h" should be included prior to this for pr asserts
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
	template <typename T, typename U> inline void Encompase(Range<T>& range, U rhs)
	{
		if      (range.empty())        { range.m_begin = range.m_end = rhs; ++range.m_end; }
		else if (rhs <  range.m_begin) { range.m_begin = rhs; }
		else if (rhs >= range.m_end  ) { range.m_end   = rhs; ++range.m_end; }
	}
	
	// Expand 'range' to include 'rhs' if necessary
	template <typename T, typename U> inline void Encompase(Range<T>& range, Range<U> const& rhs)
	{
		if      (range.empty())               { range = rhs; }
		else if (rhs.m_begin < range.m_begin) { range.m_begin = rhs.m_begin; }
		else if (rhs.m_end   > range.m_end  ) { range.m_end   = rhs.m_end; }
	}
}
	
#ifdef PR_ASSERT_DEFINED
#   undef PR_ASSERT_DEFINED
#   undef PR_ASSERT
#endif
	
#endif
