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
		using const_pointer = T const*;
		using const_reference = T const&;

		T* m_arr;
		size_t m_count;

		constexpr span(T* arr, size_t count) noexcept
			:m_arr(arr)
			,m_count(count)
		{}
		constexpr span(std::initializer_list<T> list) noexcept
			:m_arr(list.begin())
			,m_count(list.size())
		{}
		template <int N> constexpr span(std::array<std::remove_const_t<T>,N>& arr) noexcept
			:m_arr(arr.data())
			,m_count(arr.size())
		{}
		template <int N> constexpr span(std::array<std::remove_const_t<T>,N> const& arr) noexcept
			:m_arr(arr.data())
			,m_count(arr.size())
		{}
		template <int N> constexpr span(T (&arr)[N]) noexcept
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
		
		T* data() const
		{
			return m_arr;
		}

		operator span<T const>() const
		{
			return std::span<T const>(m_arr, m_count);
		}
	};
	template <> class span<void const> :span<uint8_t const>
	{
		constexpr span(void const* arr, size_t count) noexcept
			:span<uint8_t const>(static_cast<uint8_t const*>(arr), count)
		{}
		template <typename T> constexpr span(span<T> const& rhs) noexcept
			:span(static_cast<void const*>(rhs.data()), rhs.size() * sizeof(T))
		{}
	};
	template <> class span<void> :span<uint8_t>
	{
		constexpr span(void* arr, size_t count) noexcept
			:span<uint8_t>(static_cast<uint8_t*>(arr), count)
		{}
		template <typename T> constexpr span(span<T>& rhs) noexcept
			:span(static_cast<void*>(rhs.data()), rhs.size() * sizeof(T))
		{}
	};

	// Type deduction helper
	template <typename T> constexpr span<T> make_span(T* arr, int count)
	{
		return span<T>(arr, count);
	}
}
