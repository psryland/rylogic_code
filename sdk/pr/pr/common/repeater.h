//*******************************************************************************************
// Repeater
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************************
#pragma once

#include "pr/common/interpolate.h"

namespace pr
{
	// An iterator wrapper that returns N items from an iterator
	// that points to M items, where N/M is an integer > 0
	template <typename TCIter, typename TItem, typename Interp = typename Interpolate<TItem>::Point>
	struct Repeater
	{
		Interp      m_interp;   // The interpolation functor
		TCIter      m_iter;     // The source iterator
		std::size_t m_count;    // The number of items available through 'm_iter'
		std::size_t m_output;   // The total number of items to return
		std::size_t m_i;        // The current item number
		std::size_t m_r;        // The current repeat number
		TItem       m_default;  // A default item to use when 'm_iter' is exhausted
		TItem       m_curr;     // The value of the current item
		TItem       m_next;     // The value of the next item
		TItem       m_item;     // The interpolated value
		
		// 'iter' is the src iterator
		// 'count' is the number of available items pointed to by 'iter'
		// 'output_count' is the number items to be output from the iterator
		// 'def' is the value to return when 'iter' is exhausted
		Repeater(TCIter iter, std::size_t count, std::size_t output_count, TItem const& def, Interp interp = Interp())
			:m_interp(interp)
			,m_iter(iter)
			,m_count(count)
			,m_output(output_count)
			,m_i(0)
			,m_r(0)
			,m_default(def)
			,m_curr(Next())
			,m_next(Next())
			,m_item(m_interp(m_curr, m_next, 0, 1))
		{}
		virtual ~Repeater()
		{}
		TItem operator*() const
		{
			return m_item;
		}
		Repeater& operator ++()
		{
			// step size is (m_count-1)/(m_output-1)
			// e.g.  count = 3 |                 |                 |
			//      output = 7 |     |     |     |     |     |     |
			//      output = 6 |      |      |      |      |       |
			// step = 2/6 = 1/3
			m_r += m_count - 1;
			if (m_r >= m_output - 1)
			{
				m_r -= m_output - 1;
				m_curr = m_next;
				m_next = Next();
			}
			m_item = m_interp(m_curr, m_next, m_r, m_output - 1);
			return *this;
		}
		Repeater operator ++(int)
		{
			Repeater r = *this;
			++(*this);
			return r;
		}

	protected:

		virtual TItem Next()
		{
			// Exhausted the iterator? return the default
			if (m_i == m_count) return m_default;
			++m_i;
			return *m_iter++;
		}
	};

	// Helper method for returning a point sampling repeater
	// 'iter' is the src iterator
	// 'count' is the number of available items pointed to by 'iter'
	// 'output_count' is the number of times the iterator will be incremented
	// 'def' is the value to return when 'iter' is exhausted
	template <typename TCIter, typename TItem>
	Repeater<TCIter,TItem,typename Interpolate<TItem>::Point>
	CreateRepeater(TCIter iter, std::size_t count, std::size_t output_count, TItem const& def)
	{
		return Repeater<TCIter,TItem,typename Interpolate<TItem>::Point>(iter, count, output_count, def);
	}

	// Helper method for returning a linear interpolating repeater
	// 'iter' is the src iterator
	// 'count' is the number of available items pointed to by 'iter'
	// 'output_count' is the number of times the iterator will be incremented
	// 'def' is the value to return when 'iter' is exhausted
	template <typename TCIter, typename TItem>
	Repeater<TCIter,TItem,typename Interpolate<TItem>::Linear>
	CreateLerpRepeater(TCIter iter, std::size_t count, std::size_t output_count, TItem const& def)
	{
		return Repeater<TCIter,TItem,typename Interpolate<TItem>::Linear>(iter, count, output_count, def);
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
			{
				std::vector<int> vec;
				vec.push_back(0);
				vec.push_back(1);
				vec.push_back(2);

				auto rep = pr::CreateRepeater(begin(vec), vec.size(), 6, -1);
				PR_CHECK(*(rep  ),  0);
				PR_CHECK(*(++rep),  0);
				PR_CHECK(*(++rep),  0);
				PR_CHECK(*(++rep),  1);
				PR_CHECK(*(++rep),  1);
				PR_CHECK(*(++rep),  2);
				PR_CHECK(*(++rep),  2);
				PR_CHECK(*(++rep),  2);
				PR_CHECK(*(++rep), -1);
				PR_CHECK(*(++rep), -1);
			}
			{
				std::vector<float> vec;
				vec.push_back(0.0f);
				vec.push_back(0.5f);
				vec.push_back(1.0f);

				auto rep = pr::CreateLerpRepeater(begin(vec), vec.size(), 6, 1.0f);
				PR_CHECK(*rep++, 0.0f);
				PR_CHECK(*rep++, 0.2f);
				PR_CHECK(*rep++, 0.4f);
				PR_CHECK(*rep++, 0.6f);
				PR_CHECK(*rep++, 0.8f);
				PR_CHECK(*rep++, 1.0f);
				PR_CHECK(*rep++, 1.0f);
				PR_CHECK(*rep++, 1.0f);
			}
			{
				std::vector<float> vec;
				vec.push_back(0.0f);
				vec.push_back(1.0f);
				
				auto rep = pr::CreateLerpRepeater(begin(vec), vec.size(), 6, -1.0f);
				PR_CHECK(*rep++, 0.0f);
				PR_CHECK(*rep++, 0.2f);
				PR_CHECK(*rep++, 0.4f);
				PR_CHECK(*rep++, 0.6f);
				PR_CHECK(*rep++, 0.8f);
				PR_CHECK(*rep++, 1.0f);
			}
		}
	}
}
#endif
