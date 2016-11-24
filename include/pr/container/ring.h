//******************************************
// A very simple stack class
//  Copyright (c) Rylogic Ltd 2007
//******************************************

#pragma once
#include <cassert>

namespace pr
{
	// Wrapper for a container with array access operators 
	// to allow access as a ring buffer.
	template <typename TIter> struct Ring
	{
		TIter m_first;
		std::ptrdiff_t m_count;
		int m_offset;

		Ring(TIter first, TIter last, int offset = 0)
			:m_first(first)
			,m_count(last - first)
			,m_offset(offset)
		{}

		// Negative indices access from the end and loop backwards through the container
		auto operator[](int i) -> decltype(*m_first)
		{
			auto j = i >= 0 ? (i % m_count) : ((m_count-1) - (~i % m_count));
			return *(m_first + j);
		}
		auto operator[](int i) const -> decltype(*m_first)
		{
			auto j = i >= 0 ? (i % m_count) : ((m_count-1) - (~i % m_count));
			return *(m_first + j);
		}
	};

	// Create ring buffer access to a range
	template <typename Iter> Ring<Iter> MakeRing(Iter first, Iter last, int offset = 0)
	{
		return Ring<Iter>(first, last, offset);
	}
}
