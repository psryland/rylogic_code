//*********************************************
// Array view
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

namespace pr
{
	// std::array_view
	template <typename T> struct array_view
	{
		union {
		T const* m_carr;
		T* m_marr;
		};
		int m_count;

		array_view(T const* arr, int count)
			:m_carr(arr)
			,m_count(count)
		{}
		array_view(T* arr, int count)
			:m_marr(arr)
			,m_count(count)
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
}
