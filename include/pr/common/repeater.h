//*******************************************************************************************
// Repeater
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************************
#pragma once
#include <span>
#include <functional>

namespace pr
{
	// An iterator wrapper that returns N items from an iterator that points to M items, where N/M is an integer > 0
	template <typename TCIter, typename TItem>
	struct Repeater
	{
		using Interp = TItem(*)(TItem const& lhs, TItem const& rhs, int n, int N);

		Interp m_interp;   // The interpolation function
		TCIter m_iter;     // The source iterator
		int    m_count;    // The number of items available through 'm_iter'
		int    m_output;   // The total number of items to return
		int    m_i;        // The current item number
		int    m_r;        // The current repeat number
		TItem  m_default;  // A default item to use when 'm_iter' is exhausted
		TItem  m_curr;     // The value of the current item
		TItem  m_next;     // The value of the next item
		TItem  m_item;     // The interpolated value
		
		// 'iter' is the src iterator
		// 'count' is the number of available items pointed to by 'iter'
		// 'output_count' is the number items to be output from the iterator
		// 'def' is the value to return when 'iter' is exhausted
		Repeater(TCIter iter, int count, int output_count, TItem const& def, Interp interp)
			: m_interp(interp)
			, m_iter(iter)
			, m_count(count)
			, m_output(output_count)
			, m_i(0)
			, m_r(0)
			, m_default(def)
			, m_curr(Next())
			, m_next(Next())
			, m_item(m_interp(m_curr, m_next, 0, 0))
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
			if (m_output > 1)
			{
				if (m_count != 0)
				{
					m_r += m_count - 1;
					if (m_r >= m_output - 1)
					{
						m_r -= m_output - 1;
						m_curr = m_next;
						m_next = Next();
					}
				}
				m_item = m_interp(m_curr, m_next, m_r, m_output - 1);
			}
			return *this;
		}
		Repeater operator ++(int)
		{
			Repeater r = *this;
			++(*this);
			return r;
		}
		TItem Next()
		{
			// Exhausted the iterator? return the default
			if (m_i != m_count)
			{
				++m_i;
				return *m_iter++;
			}
			return m_default;
		}
	};

	//// Helper method for returning a point sampling repeater
	//// 'iter' is the src iterator
	//// 'count' is the number of available items pointed to by 'iter'
	//// 'output_count' is the number of times the iterator will be incremented
	//// 'def' is the value to return when 'iter' is exhausted
	template <typename TCIter, typename TItem>
	Repeater<TCIter, TItem> CreateRepeater(TCIter iter, int count, int output_count, TItem const& def)
	{
		constexpr auto PointInterp = [](TItem const& lhs, TItem const&, int, int) { return lhs; };
		return Repeater<TCIter, TItem>(iter, count, output_count, def, PointInterp);
	}
	template <typename TContainer, typename TItem = typename TContainer::value_type>
	Repeater<TItem const*, TItem> CreateRepeater(TContainer const& source, int output_count, TItem const& def)
	{
		return CreateRepeater(source.data(), static_cast<int>(source.size()), output_count, def);
	}

	// Helper method for returning a linear interpolating repeater
	// 'iter' is the src iterator
	// 'count' is the number of available items pointed to by 'iter'
	// 'output_count' is the number of times the iterator will be incremented
	// 'def' is the value to return when 'iter' is exhausted
	template <typename TCIter, typename TItem>
	Repeater<TCIter, TItem> CreateLerpRepeater(TCIter iter, int count, int output_count, TItem const& def)
	{
		constexpr auto LinearInterp = [](TItem const& lhs, TItem const& rhs, int n, int N)
		{
			if (N == 0) return lhs;
			return Lerp(lhs, rhs, 1.0f * n / N);
		};
		return Repeater<TCIter, TItem>(iter, count, output_count, def, LinearInterp);
	}
	template <typename TContainer, typename TItem = typename TContainer::value_type>
	Repeater<TItem const*, TItem> CreateLerpRepeater(TContainer const& source, int output_count, TItem const& def)
	{
		return CreateLerpRepeater(source.data(), static_cast<int>(source.size()), output_count, def);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	PRUnitTest(RepeaterTests)
	{
		{
			std::vector<int> vec;
			vec.push_back(0);
			vec.push_back(1);
			vec.push_back(2);

			auto rep = pr::CreateRepeater(vec, 6, -1);
			PR_EXPECT(*(rep  ) ==  0);
			PR_EXPECT(*(++rep) ==  0);
			PR_EXPECT(*(++rep) ==  0);
			PR_EXPECT(*(++rep) ==  1);
			PR_EXPECT(*(++rep) ==  1);
			PR_EXPECT(*(++rep) ==  2);
			PR_EXPECT(*(++rep) ==  2);
			PR_EXPECT(*(++rep) ==  2);
			PR_EXPECT(*(++rep) == -1);
			PR_EXPECT(*(++rep) == -1);
		}
		{
			std::vector<float> vec;
			vec.push_back(0.0f);
			vec.push_back(0.5f);
			vec.push_back(1.0f);

			auto rep = pr::CreateLerpRepeater(vec, 6, 1.0f);
			PR_EXPECT(*rep++ == 0.0f);
			PR_EXPECT(*rep++ == 0.2f);
			PR_EXPECT(*rep++ == 0.4f);
			PR_EXPECT(*rep++ == 0.6f);
			PR_EXPECT(*rep++ == 0.8f);
			PR_EXPECT(*rep++ == 1.0f);
			PR_EXPECT(*rep++ == 1.0f);
			PR_EXPECT(*rep++ == 1.0f);
		}
		{
			std::vector<float> vec;
			vec.push_back(0.0f);
			vec.push_back(1.0f);
				
			auto rep = pr::CreateLerpRepeater(vec, 6, -1.0f);
			PR_EXPECT(*rep++ == 0.0f);
			PR_EXPECT(*rep++ == 0.2f);
			PR_EXPECT(*rep++ == 0.4f);
			PR_EXPECT(*rep++ == 0.6f);
			PR_EXPECT(*rep++ == 0.8f);
			PR_EXPECT(*rep++ == 1.0f);
		}
		{
			auto rep = pr::CreateRepeater(nullptr, 0, 1, 2.0f);
			PR_EXPECT(*rep++ == 2.0f);
			PR_EXPECT(*rep++ == 2.0f);
		}
		{
			auto rep = pr::CreateRepeater(nullptr, 0, 4, 2.0f);
			PR_EXPECT(*rep++ == 2.0f);
			PR_EXPECT(*rep++ == 2.0f);
			PR_EXPECT(*rep++ == 2.0f);
			PR_EXPECT(*rep++ == 2.0f);
			PR_EXPECT(*rep++ == 2.0f);
		}
		{
			float f = 1.0f;
			auto rep = pr::CreateLerpRepeater(&f, 1, 4, 2.0f);
			PR_EXPECT(*rep++ == 1.0f);
			PR_EXPECT(*rep++ == 1.0f);
			PR_EXPECT(*rep++ == 1.0f);
			PR_EXPECT(*rep++ == 1.0f);
			PR_EXPECT(*rep++ == 1.0f); // only returns default if no data is provided
		}
	}
}
#endif
