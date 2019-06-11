//*********************************************
// Array view
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include <array>

// Until C++20 is supported
namespace std
{
	// std::span
	// Remember 'T' can be const
	template <class T> class span
	{
	public:
		using element_type = T;
		using value_type = std::remove_cv_t<T>;
		using index_type = std::ptrdiff_t;
		using difference_type = std::ptrdiff_t;
		using pointer = T*;
		using reference = T&;

		T* m_arr;
		size_t m_count;

		constexpr span(T* arr, size_t count)
			:m_arr(arr)
			,m_count(count)
		{}
		constexpr span(std::initializer_list<T> list)
			:m_arr(list.begin())
			,m_count(list.size())
		{}
		template <int N> constexpr span(std::array<T,N> const& arr)
			:m_arr(arr.data())
			,m_count(arr.size())
		{}
		template <int N> constexpr span(T (&arr)[N])
			:m_arr(&arr[0])
			,m_count(N)
		{}

		bool empty() const
		{
			return m_count == 0;
		}
		size_t size() const
		{
			return m_count;
		}

		T* begin() const
		{
			return m_arr;
		}
		T* end() const
		{
			return m_arr + m_count;
		}

		T& operator[](int i) const
		{
			return m_arr[i];
		}

		operator span<T const>() const
		{
			return std::span<T const>(m_arr, m_count);
		}
	};

	// Type deduction helper
	template <typename T> constexpr span<T> make_span(T* arr, int count)
	{
		return span<T>(arr, count);
	}
}
