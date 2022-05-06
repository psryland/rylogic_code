//*********************************************
// Value smart pointer
//  Copyright (c) Rylogic Ltd 2020
//*********************************************
// An iterator that steps in 'stride' increments
#pragma once
#include <type_traits>

namespace pr
{
	template <typename Type, int Stride>
	struct stride_ptr
	{
		using voidptr_t = std::conditional_t<std::is_const_v<Type>, void const*, void*>;
		using byteptr_t = std::conditional_t<std::is_const_v<Type>, unsigned char const*, unsigned char*>;
		using value_type = std::conditional_t<std::is_same_v<Type, void>, unsigned char, Type>;
		
		using pointer = value_type*;
		using reference = value_type&;
		using difference_type = typename std::pointer_traits<pointer>::difference_type;
		using iterator_category = std::random_access_iterator_tag;
		using stride_t = union
		{
			voidptr_t vptr;
			byteptr_t bptr;
			pointer type;
		};

		stride_t m_ptr;
		stride_ptr()
		{
		}
		stride_ptr(voidptr_t ptr)
			:m_ptr(ptr)
		{}
		pointer operator ->() const
		{
			return m_ptr.type;
		}
		reference operator *() const
		{
			return *m_ptr.type;
		}
		stride_ptr& operator ++()
		{
			m_ptr.bptr += Stride;
			return *this;
		}
		stride_ptr& operator --()
		{
			m_ptr.bptr -= Stride;
			return *this;
		}
		stride_ptr operator ++(int)
		{
			stride_ptr i(*this);
			m_ptr.bptr += Stride;
			return i;
		}
		stride_ptr operator --(int)
		{
			stride_ptr i(*this);
			m_ptr.bptr -= Stride;
			return i;
		}
		reference operator[](difference_type i) const
		{
			return *stride_t(m_ptr.bptr + i * Stride).type;
		}

		operator stride_ptr<Type const, Stride>() const
		{
			return stride_ptr<Type const, Stride>(m_ptr.vptr, 0);
		}

		// Comparison operators
		//friend std::strong_ordering operator <=>(stride_ptr lhs, stride_ptr rhs)
		//{
		//	return lhs.m_ptr.vptr <=> rhs.m_ptr.vptr;
		//}
		friend bool operator == (stride_ptr lhs, stride_ptr rhs)
		{
			return lhs.m_ptr.vptr == rhs.m_ptr.vptr;
		}
		friend bool operator != (stride_ptr lhs, stride_ptr rhs)
		{
			return lhs.m_ptr.vptr != rhs.m_ptr.vptr;
		}
		friend bool operator <  (stride_ptr lhs, stride_ptr rhs)
		{
			return lhs.m_ptr.vptr < rhs.m_ptr.vptr;
		}
		friend bool operator <= (stride_ptr lhs, stride_ptr rhs)
		{
			return lhs.m_ptr.vptr <= rhs.m_ptr.vptr;
		}
		friend bool operator >  (stride_ptr lhs, stride_ptr rhs)
		{
			return lhs.m_ptr.vptr > rhs.m_ptr.vptr;
		}
		friend bool operator >= (stride_ptr lhs, stride_ptr rhs)
		{
			return lhs.m_ptr.vptr >= rhs.m_ptr.vptr;
		}

		friend stride_ptr& operator += (stride_ptr& lhs, difference_type rhs)
		{
			lhs.m_ptr.bptr += rhs * Stride;
			return lhs;
		}
		friend stride_ptr& operator -= (stride_ptr& lhs, difference_type rhs)
		{
			lhs.m_ptr.bptr -= rhs * Stride;
			return lhs;
		}
		friend stride_ptr operator + (stride_ptr lhs, difference_type rhs)
		{
			return stride_ptr(lhs.m_ptr.bptr + rhs * Stride);
		}
		friend stride_ptr operator - (stride_ptr lhs, difference_type rhs)
		{
			return stride_ptr(lhs.m_ptr.bptr - rhs * Stride);
		}
		friend difference_type operator - (stride_ptr lhs, stride_ptr rhs)
		{
			return (lhs.m_ptr.bptr - rhs.m_ptr.bptr) / Stride;
		}
	};
	static_assert(sizeof(stride_ptr<void, 16>) == sizeof(void*));
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	namespace stride_ptr_tests
	{
		using byte_t = unsigned char;
		struct Thing
		{
			int m_int;
			float m_float;
			byte_t m_byte;

			inline static int count = 0;
			
			Thing()
				:m_int(count++)
				,m_float(static_cast<float>(m_int))
				,m_byte(static_cast<byte_t>(m_int & 0xff))
			{}
		};
	}
	PRUnitTest(StridePtrTests)
	{
		using namespace stride_ptr_tests;
		Thing arr[300];

		auto ptr = stride_ptr<byte_t const, sizeof(Thing)>(&arr[0].m_byte);
		auto end = ptr + _countof(arr);

		byte_t i = 0;
		for (auto x = ptr; x != end; ++x)
		{
			PR_CHECK(*x == i, true);
			++i;
		}
	}
}
#endif
