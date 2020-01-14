//*********************************************
// Value smart pointer
//  Copyright (c) Rylogic Ltd 2020
//*********************************************
#pragma once
#include <memory>

namespace pr
{
	template <typename T, typename Deleter = std::default_delete<T>>
	class value_ptr
	{
		// Notes:
		//  - Simple implementation of a smart pointer with value semantics

	public:

		using pointer = T*;
		using element_type = T;
		using deleter_type = Deleter;

	private:

		T* m_ptr;

	public:

		value_ptr() noexcept
			:m_ptr()
		{
		}
		value_ptr(nullptr_t) noexcept
			:m_ptr()
		{
		}
		explicit value_ptr(T* rhs) noexcept
			:m_ptr(rhs)
		{
		}
		value_ptr(value_ptr&& rhs) noexcept
			:m_ptr(rhs.m_ptr)
		{
			rhs.m_ptr = nullptr;
		}
		value_ptr(value_ptr const& rhs)
			:m_ptr()
		{
			if (rhs.m_ptr)
				m_ptr = new T(*rhs.m_ptr);
		}
		~value_ptr() noexcept
		{
			reset(nullptr);
		}
		value_ptr& operator =(value_ptr&& rhs) noexcept
		{
			if (&rhs == this) return *this;
			std::swap(m_ptr, rhs.m_ptr);
			return *this;
		}
		value_ptr& operator =(value_ptr const& rhs)
		{
			if (&rhs == this) return *this;
			value_ptr p(rhs);
			std::swap(m_ptr, p.m_ptr);
			return *this;
		}
		T* operator*() const noexcept
		{
			return get();
		}
		T* operator->() const noexcept
		{
			return get();
		}
		void reset(T* ptr) noexcept
		{
			get_deleter()(m_ptr);
			m_ptr = ptr;
		}
		T* get() const noexcept
		{
			return m_ptr;
		}
		T* release() noexcept
		{
			auto p = m_ptr;
			m_ptr = nullptr;
			return p;
		}
		deleter_type get_deleter() const noexcept
		{
			return deleter_type();
		}
		explicit operator bool() const
		{
			return m_ptr != nullptr;
		}
		friend bool operator == (value_ptr lhs, value_ptr rhs)
		{
			return lhs.m_ptr == rhs.m_ptr;
		}
		friend bool operator != (value_ptr lhs, value_ptr rhs)
		{
			return lhs.m_ptr != rhs.m_ptr;
		}
		friend bool operator <  (value_ptr lhs, value_ptr rhs)
		{
			return lhs.m_ptr < rhs.m_ptr;
		}
		friend bool operator <= (value_ptr lhs, value_ptr rhs)
		{
			return lhs.m_ptr <= rhs.m_ptr;
		}
		friend bool operator >  (value_ptr lhs, value_ptr rhs)
		{
			return lhs.m_ptr > rhs.m_ptr;
		}
		friend bool operator >= (value_ptr lhs, value_ptr rhs)
		{
			return lhs.m_ptr >= rhs.m_ptr;
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	PRUnitTest(ValuePtrTests)
	{
		struct Thing
		{
			int m_i;
			Thing(int i) :m_i(i) {}
		};

		value_ptr<Thing> v0(new Thing(0));
		value_ptr<Thing> v1(new Thing(1));
		PR_CHECK(v0->m_i, 0);
		PR_CHECK(v1->m_i, 1);

		auto v2 = v1;
		PR_CHECK(v2->m_i, 1);

		v2->m_i++;
		PR_CHECK(v1->m_i, 1);
		PR_CHECK(v2->m_i, 2);

		v1 = std::move(v2);
		PR_CHECK(v1->m_i, 2);
	}
}
#endif