//************************************************************************
// Ref counting base class
//  Copyright (c) Rylogic Ltd 2011
//************************************************************************
// Usage:
//   struct Thing :pr::RefCount<Thing> {};
//   Thing* thing = new Thing();
//   thing->AddRef();
//   thing->Release(); // thing deleted here
//
//   Typically this would be used with pr::RefPtr<Thing>.
//   e.g.
//   {
//       RefPtr<Thing> ptr(new Thing()); // thing deleted when last reference goes out of scope
//   }
//
#pragma once
#include <atomic>
#include <cassert>

namespace pr
{
	struct IRefCounted
	{
		virtual ~IRefCounted() = default;
		virtual long AddRef() const = 0;
		virtual long Release() const = 0;
	};

	// RefCounted concept
	template <typename T> concept RefCountedType = requires(T t)
	{
		t.AddRef();
		t.Release();
	};

	// Reference counting mix-in base class
	// 'Deleter' is a type containing a static function with signature: 'void RefCountZero(RefCount* obj)'
	// Its purpose is to release resources owned by the ref counted object because there are no more references to it.
	// 'RefCount' itself contains a 'RefCountZero' function so that clients can use 'RefCount<Derived>'
	// which will pick up the default behaviour of deleting the ref counted object when the count hits zero
	// 'Shared' should be true if AddRef()/Release() can be called from multiple threads
	template <typename Deleter, bool Shared = true>
	struct RefCount :IRefCounted
	{
		using count_t = std::conditional_t<Shared, std::atomic<long>, long>;
		mutable count_t m_ref_count;

		RefCount()
			:m_ref_count(0)
		{}
		RefCount(RefCount&& rhs) noexcept
			:m_ref_count(std::exchange(rhs.m_ref_count, 0))
		{}
		RefCount(RefCount const& rhs) = delete;
		RefCount& operator = (RefCount&& rhs) noexcept
		{
			if (this == &rhs) return *this;
			if constexpr (Shared)
				m_ref_count.exchange(rhs.m_ref_count.exchange(0));
			else
				m_ref_count = std::exchange(rhs.m_ref_count, 0);

			return *this;
		}
		RefCount& operator = (RefCount const& rhs) = delete;

		long AddRef() const override
		{
			long ref_count;
			if constexpr (Shared)
				ref_count = m_ref_count.fetch_add(1) + 1;
			else
				ref_count = ++m_ref_count;

			return ref_count;
		}
		long Release() const override
		{
			assert(m_ref_count > 0);

			long ref_count;
			if constexpr (Shared)
				ref_count = m_ref_count.fetch_sub(1) - 1;
			else
				ref_count = --m_ref_count;

			if (!ref_count)
				Deleter::RefCountZero(const_cast<RefCount<Deleter,Shared>*>(this));

			return ref_count;
		}

		static void RefCountZero(RefCount* doomed)
		{
			delete doomed;
		}
	};
}
namespace std
{
	template <typename T> void swap(pr::RefCount<T>& lhs, pr::RefCount<T>& rhs)
	{
		swap(lhs.m_ref_count, rhs.m_ref_count);
	}
}