//*****************************************************************************************
// LinQ
//  Copyright (c) Rylogic Ltd 2016
//*****************************************************************************************
#pragma once

#include <iterator>

namespace pr
{
	struct AlwaysTrue
	{
		template <typename... Args> bool operator ()(Args&&...) const { return true; }
	};
	struct AlwaysFalse
	{
		template <typename... Args> bool operator ()(Args&&...) const { return false; }
	};
	struct Unchanged
	{
		template <typename T> T&& operator()(T&& x) const { return std::forward<T>(x); }
	};

	// A helper for providing LinQ-type expressions
	template <typename IterType, typename ValueType, typename Pred = AlwaysTrue, typename Adapt = Unchanged>
	struct Linq :std::iterator<std::input_iterator_tag, ValueType>
	{
		using iter_type = IterType;
		using value_type = ValueType;

		iter_type m_beg;
		iter_type m_end;
		Pred m_pred;
		Adapt m_adapt;

		Linq(iter_type beg, iter_type end, Pred pred = Pred(), Adapt adapt = Adapt())
			:m_beg(beg)
			,m_end(end)
			,m_pred(pred)
			,m_adapt(adapt)
		{
			seek();
		}

		// Seek to the first value iterator position
		Linq& seek()
		{
			for (; !(m_beg == m_end) && !m_pred(*m_beg); ++m_beg) {}
			return *this;
		}
		bool empty() const
		{
			return m_beg == m_end;
		}

		// Iterator interface
		Linq begin() const
		{
			return *this;
		}
		Linq end() const
		{
			return Linq(m_end, m_end, m_pred, m_adapt);
		}
		value_type operator *() const
		{
			return m_adapt(*m_beg);
		}
		Linq& operator ++()
		{
			++m_beg;
			return seek();
		}
		Linq operator ++(int)
		{
			auto copy = *this;
			return ++*this, copy;
		}
		bool operator == (Linq const& rhs) const
		{
			return m_beg == rhs.m_beg && m_end == rhs.m_end;
		}
		bool operator != (Linq const& rhs) const
		{
			return !(*this == rhs);
		}

		// Return the range as a vector
		std::vector<value_type> to_vector() const
		{
			return std::vector<value_type>(begin(), end());
		}

		// Where: [](value_type const& v) { return true; }
		template <typename Pred2> Linq<Linq, ValueType, Pred2, Adapt> where(Pred2 pred) const
		{
			return Linq<Linq, ValueType, Pred2, Adapt>(begin(), end(), pred, m_adapt);
		}

		// Select: [](value_type const& v) { return v.other; }
		template <typename Adapt2> auto select(Adapt2 adapt) const -> Linq<Linq, decltype(adapt(std::declval<ValueType>())), AlwaysTrue, Adapt2>
		{
			return Linq<Linq, decltype(adapt(std::declval<ValueType>())), AlwaysTrue, Adapt2>(begin(), end(), AlwaysTrue(), adapt);
		}

		// True if 'any' in the linQ expression return true for 'pred'
		template <typename Pred2> bool any(Pred2 pred) const
		{
			// Seeks to the first value where 'pred' is true
			return !where(pred).empty();
		}
	};

	// Create a LinQ wrapper instance from an iterator range
	template <typename IterType, typename ValueType = std::iterator_traits<IterType>::value_type>
	inline Linq<IterType, ValueType> linq(IterType beg, IterType end)
	{
		return Linq<IterType, ValueType>(beg, end);
	}

	// Create a LinQ wrapper instance from a container
	template <typename TCont>
	inline auto linq(TCont const& cont) -> decltype(linq(std::begin(cont), std::end(cont)))
	{
		return linq(std::begin(cont), std::end(cont));
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <vector>
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_linq)
		{
			using namespace pr;

			{ // Simple array
				int cont[] = {0,1,2,3,4,5,6,7,8,9};
				auto expr = linq(cont)
					.where([](int x){ return (x%2) == 1; });

				PR_CHECK(*expr++ == 1, true);
				PR_CHECK(*expr++ == 3, true);
				PR_CHECK(*expr++ == 5, true);
				PR_CHECK(*expr++ == 7, true);
				PR_CHECK(*expr++ == 9, true);
			}
			{ // Container type
				std::vector<int> cont = {0,1,2,3,4,5,6,7,8,9};
				auto result = linq(cont)
					.where([](int i){ return (i%3) == 0; })
					.to_vector();

				PR_CHECK(result.size(), 4U);
				PR_CHECK(result[0], 0);
				PR_CHECK(result[1], 3);
				PR_CHECK(result[2], 6);
				PR_CHECK(result[3], 9);
			}
			{ // Select
				int cont[] = {0,1,2,3,4,5};
				auto result = linq(cont)
					.where([](int i){ return (i%2) == 0;})
					.select([](int i){ return i + 0.5f; })
					.to_vector();

				PR_CHECK(result.size(), 3U);
				PR_CHECK(result[0], 0.5f);
				PR_CHECK(result[1], 2.5f);
				PR_CHECK(result[2], 4.5f);
			}
			{ // Any
				bool cont[] = {false, false, false, true, false};
				auto result = linq(cont).any([](bool b) { return b; });

				PR_CHECK(result, true);
			}
		}
	}
}
#endif
