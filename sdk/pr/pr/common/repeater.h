//*******************************************************************************************
// Repeater
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************************
#pragma once
#ifndef PR_COMMON_REPEATER_H
#define PR_COMMON_REPEATER_H

namespace pr
{
	// An iterator wrapper that returns N items from an iterator
	// that points to M items, where N/M is an integer > 0
	template <typename TCIter, typename TItem>
	struct Repeater
	{
		TCIter      m_iter;     // The source iterator
		std::size_t m_count;    // The number of items available through 'm_iter'
		std::size_t m_repeat;   // The number of times to return the same item before incrementing to the next
		std::size_t m_r;        // The current repeat number
		std::size_t m_n;        // The current item number
		TItem       m_default;  // A default item to use when 'm_iter' is exhausted
		TItem       m_item;     // The value of the colour last read from 'm_colours'

		// 'iter' is the src iterator
		// 'count' is the number of available items pointed to by 'iter'
		// 'output_count' is the number of times the iterator will be incremented
		// 'def' is the value to return when 'iter' is exhausted
		Repeater(TCIter iter, std::size_t count, std::size_t output_count, TItem def)
		:m_iter(iter)
		,m_count(count)
		,m_repeat(output_count / (count + (count == 0)))
		,m_r(0)
		,m_n(0)
		,m_default(def)
		,m_item(Next())
		{}

		TItem operator*() const
		{
			return m_item;
		}
		Repeater& operator ++()
		{
			if (++m_r == m_repeat)
			{
				m_r = 0;
				m_n += m_n != m_count;
				m_item = Next();
			}
			return *this;
		}
		Repeater operator ++(int)
		{
			Repeater r = *this;
			++(*this);
			return r;
		}

		// Reads from 'm_iter' and increments.
		TItem Next()
		{
			TItem item;
			if (m_n != m_count) { item = *m_iter; ++m_iter; }
			else                { item = m_default; }
			return item;
		}
	};

	// Helper method for returning a repeater instance using type inference
	// 'iter' is the src iterator
	// 'count' is the number of available items pointed to by 'iter'
	// 'output_count' is the number of times the iterator will be incremented
	// 'def' is the value to return when 'iter' is exhausted
	template <typename TCIter, typename TItem> Repeater<TCIter,TItem> CreateRepeater(TCIter iter, std::size_t count, std::size_t output_count, TItem def)
	{
		return Repeater<TCIter, TItem>(iter, count, output_count, def);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_repeater)
		{
			std::vector<int> vec;
			vec.push_back(0);
			vec.push_back(1);
			vec.push_back(2);

			auto rep = pr::CreateRepeater(begin(vec), vec.size(), 6, -1);
			PR_CHECK(*rep  ,  0); ++rep;
			PR_CHECK(*rep  ,  0); rep++;
			PR_CHECK(*rep++,  1);
			PR_CHECK(*rep++,  1);
			PR_CHECK(*rep++,  2);
			PR_CHECK(*rep++,  2);
			PR_CHECK(*rep++, -1);
			PR_CHECK(*rep++, -1);
		}
	}
}
#endif

#endif
