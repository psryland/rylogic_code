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
	template <class T> class span
	{
	public:
		using element_type = T;
		using value_type = std::remove_cv_t<T>;
		using index_type = std::ptrdiff_t;
		using difference_type = std::ptrdiff_t;
		using pointer = T*;
		using reference = T&;

		union {
		T const* m_carr;
		T* m_marr;
		};
		size_t m_count;

		constexpr span(T const* arr, int count)
			:m_carr(arr)
			,m_count(count)
		{}
		constexpr span(T* arr, int count)
			:m_marr(arr)
			,m_count(count)
		{}
		template <int N> constexpr span(std::array<T,N> const& arr)
			:m_carr(arr.data())
			,m_count(arr.size())
		{}
		template <int N> constexpr span(std::array<T,N>& arr)
			:m_marr(arr.data())
			,m_count(arr.size())
		{}
		template <int N> constexpr span(T const (&arr)[N])
			:m_carr(&arr[0])
			,m_count(N)
		{}
		template <int N> constexpr span(T (&arr)[N])
			:m_marr(&arr[0])
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

		T const* begin() const
		{
			return m_carr;
		}
		T* begin()
		{
			return m_marr;
		}

		T const* end() const
		{
			return m_carr + m_count;
		}
		T* end()
		{
			return m_marr + m_count;
		}

		T const& operator[](int i) const
		{
			return m_carr[i];
		}
		T& operator[](int i)
		{
			return m_marr[i];
		}
	};

	// Type deduction helper
	template <typename T> constexpr span<T> make_span(T const* arr, int count)
	{
		return span<T>(arr, count);
	}
	template <typename T> constexpr span<T> make_span(T* arr, int count)
	{
		return span<T>(arr, count);
	}
}
