//******************************************
// Aligned Storage
//  Copyright (c) Rylogic Ltd 2017
//******************************************
#pragma once

#include <new>
#include <type_traits>
#include <cassert>

namespace pr
{
	#pragma warning (push)
	#pragma warning (disable:4324)
	template <typename T>
	class optional
	{
		using storage_t = typename std::aligned_storage<sizeof(T), alignof(T)>::type;
		static_assert(sizeof(storage_t) == sizeof(T), "Storage type has the wrong size");

		T const* m_value;
		alignas(alignof(T)) storage_t m_storage;

		T const* ptr() const
		{
			return reinterpret_cast<T const*>(&m_storage);
		}
		T* ptr()
		{
			return reinterpret_cast<T*>(&m_storage);
		}

		void construct(T const& rhs)
		{
			assert(!bool(*this));
			new (ptr()) T(rhs);
			m_value = ptr();
		}
		void construct(T&& rhs)
		{
			assert(!bool(*this));
			new (ptr()) T(std::forward<T>(rhs));
			m_value = ptr();
		}
		void destruct()
		{
			assert(bool(*this));
			ptr()->~T();
			m_value = nullptr;
		}

	public:

		constexpr optional()
			:m_value()
			,m_storage()
		{}
		constexpr optional(nullptr_t)
			:m_value()
			,m_storage()
		{}
		constexpr optional(T&& rhs)
			:m_value()
			,m_storage()
		{
			construct(std::forward<T>(rhs));
		}
		constexpr optional(T const& rhs)
			:m_value()
			,m_storage()
		{
			construct(rhs);
		}
		optional(optional&& rhs)
			:optional()
		{
			if (!rhs) return;
			construct(std::move(*rhs.ptr()));
			rhs.destruct();
		}
		optional(optional const& rhs)
			:optional()
		{
			if (!rhs) return;
			construct(*rhs.ptr());
		}
		~optional()
		{
			if (*this)
				destruct();
		}

		optional& operator =(nullptr_t) noexcept
		{
			if (*this) destruct();
			return *this;
		}
		optional& operator =(T const& rhs) noexcept
		{
			if (!*this) construct(rhs);
			else        value() = rhs;
			return *this;
		}
		optional& operator =(T&& rhs) noexcept
		{
			if (!*this) construct(std::forward<T>(rhs));
			else        value() = std::move(rhs);
			return *this;
		}
		optional& operator =(optional const& rhs) noexcept
		{
			if (this == &rhs) return *this;
			if (*this) destruct();
			construct(*rhs.ptr());
			return *this;
		}
		optional& operator =(optional&& rhs) noexcept
		{
			if (this == &rhs) return *this;
			if (*this && rhs)
			{
				std::swap(*ptr(), *rhs.ptr());
			}
			else if (*this)
			{
				destruct();
			}
			else if (rhs)
			{
				construct(std::move(*rhs.ptr()));
				rhs.destruct();
			}
			return *this;
		}

		constexpr T const& value() const
		{
			if (!*this) throw std::runtime_error("empty optional accessed");
			return *ptr();
		}
		T& value()
		{
			if (!*this) throw std::runtime_error("empty optional accessed");
			return *ptr();
		}

		constexpr explicit operator bool() const
		{
			return m_value != nullptr;
		}

		constexpr T const* operator->() const
		{
			return *this ? ptr() : nullptr;
		}
		T* operator->()
		{
			return *this ? ptr() : nullptr;
		}
		constexpr T const& operator *() const
		{
			return value();
		}
		T& operator *()
		{
			return value();
		}
	};
	#pragma warning (pop)

	// Compare optional with an optional. Empty optionals are considered Less that all values
	template <typename T> constexpr bool operator == (optional<T> const& lhs, optional<T> const& rhs)
	{
		if (bool(lhs) != bool(rhs)) return false;
		if (bool(lhs) == false) return true;
		return *lhs == *rhs;
	}
	template <typename T> constexpr bool operator != (optional<T> const& lhs, optional<T> const& rhs)
	{
		return !(lhs == rhs);
	}
	template <typename T> constexpr bool operator <  (optional<T> const& lhs, optional<T> const& rhs)
	{
		if (bool(lhs) == false) return false;
		if (bool(rhs) == false) return false;
		return *lhs < *rhs;
	}
	template <typename T> constexpr bool operator <= (optional<T> const& lhs, optional<T> const& rhs)
	{
		return !(rhs < lhs);
	}
	template <typename T> constexpr bool operator >  (optional<T> const& lhs, optional<T> const& rhs)
	{
		return rhs < lhs;
	}
	template <typename T> constexpr bool operator >= (optional<T> const& lhs, optional<T> const& rhs)
	{
		return !(lhs < rhs);
	}

	// Compare optional with null. Null is the same as an empty optional
	template <typename T> constexpr bool operator == (optional<T> const& lhs, nullptr_t) noexcept
	{
		return !lhs;
	}
	template <typename T> constexpr bool operator == (nullptr_t, optional<T> const& rhs) noexcept
	{
		return !rhs;
	}
	template <typename T> constexpr bool operator != (optional<T> const& lhs, nullptr_t) noexcept
	{
		return bool(lhs);
	}
	template <typename T> constexpr bool operator != (nullptr_t, optional<T> const& rhs) noexcept
	{
		return bool(rhs);
	}
	template <typename T> constexpr bool operator <  (optional<T> const& lhs, nullptr_t) noexcept
	{
		(void)lhs; return false;
	}
	template <typename T> constexpr bool operator <  (nullptr_t, optional<T> const& rhs) noexcept
	{
		return bool(rhs);
	}
	template <typename T> constexpr bool operator <= (optional<T> const& lhs, nullptr_t) noexcept
	{
		return !lhs;
	}
	template <typename T> constexpr bool operator <= (nullptr_t, optional<T> const& rhs) noexcept
	{
		(void)rhs; return true;
	}
	template <typename T> constexpr bool operator >  (optional<T> const& lhs, nullptr_t) noexcept
	{
		return bool(lhs);
	}
	template <typename T> constexpr bool operator >  (nullptr_t, optional<T> const& rhs) noexcept
	{
		(void)rhs; return false;
	}
	template <typename T> constexpr bool operator >= (optional<T> const& lhs, nullptr_t) noexcept
	{
		(void)lhs; return true;
	}
	template <typename T> constexpr bool operator >= (nullptr_t, optional<T> const& rhs) noexcept
	{
		return !rhs;
	}

	// Compare optional with a value. Empty optionals are considered Less that all values
	template <typename T> constexpr bool operator == (optional<T> const& lhs, T const& rhs)
	{
		return bool(lhs) ? *lhs == rhs : false;
	}
	template <typename T> constexpr bool operator == (T const& lhs, optional<T> const& rhs)
	{
		return bool(rhs) ? rhs == *lhs : false;
	}
	template <typename T> constexpr bool operator != (optional<T> const& lhs, T const& rhs)
	{
		return bool(lhs) ? !(*lhs == rhs) : true;
	}
	template <typename T> constexpr bool operator != (T const& lhs, optional<T> const& rhs)
	{
		return bool(rhs) ? !(lhs == *rhs) : true;
	}
	template <typename T> constexpr bool operator <  (optional<T> const& lhs, T const& rhs)
	{
		return bool(lhs) ? *lhs < rhs : true;
	}
	template <typename T> constexpr bool operator <  (T const& lhs, optional<T> const& rhs)
	{
		return bool(rhs) ? lhs < *rhs : false;
	}
	template <typename T> constexpr bool operator <= (optional<T> const& lhs, T const& rhs)
	{
		return !(lhs > rhs);
	}
	template <typename T> constexpr bool operator <= (T const& lhs, optional<T> const& rhs)
	{
		return !(lhs > rhs);
	}
	template <typename T> constexpr bool operator >  (optional<T> const& lhs, T const& rhs)
	{
		return bool(lhs) ? rhs < *lhs : false;
	}
	template <typename T> constexpr bool operator >  (T const& lhs, optional<T> const& rhs)
	{
		return bool(rhs) ? lhs < *rhs : true;
	}
	template <typename T> constexpr bool operator >= (optional<T> const& lhs, T const& rhs)
	{
		return !(lhs < rhs);
	}
	template <typename T> constexpr bool operator >= (T const& lhs, optional<T> const& rhs)
	{
		return !(lhs < rhs);
	}

}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/maths/maths.h"
#include <vector>
namespace pr
{
	namespace unittests
	{
		namespace optional
		{
			struct Thing
			{
				int ref;
				bool d;

				Thing()
					:ref()
					,d()
				{
					ref = ++ref_count();
					d = true;
				}
				~Thing()
				{
					if (d)
						--ref_count();
				}
				Thing(Thing const&)
				{
					ref = ++ref_count();
					d = true;
				}
				Thing(Thing&& rhs)
					:ref()
					,d()
				{
					std::swap(ref, rhs.ref);
					std::swap(d, rhs.d);
				}
				Thing& operator =(Thing const&)
				{
					return *this;
				}
				Thing& operator =(Thing&& rhs)
				{
					std::swap(ref, rhs.ref);
					std::swap(d, rhs.d);
					return *this;
				}
				static int& ref_count()
				{
					static int s_ref_count;
					return s_ref_count;
				}
			};
		}
		PRUnitTest(pr_meta_optional)
		{
			using Thing = pr::unittests::optional::Thing;

			{
				pr::optional<double> a;
				PR_CHECK(a != 1.0, true);
				PR_CHECK(a == false, true);
				PR_CHECK(a == nullptr, true);
				PR_CHECK(a < -limits<double>::max(), true);
			}
			{
				pr::optional<double> a = 1.0;
				PR_CHECK(a == 1.0, true);
				PR_CHECK(bool(a), true);
				PR_CHECK(a >= 0.0 || a < 0.0, true);
			}
			{
				pr::optional<v4> a = v4Zero;
				PR_CHECK(a != v4One, true);
				PR_CHECK(bool(a), true);
			}
			{
				pr::optional<m4x4> a = m4x4Identity;
				PR_CHECK(maths::is_aligned(&a.value()), true);
			}
			{
				pr::optional<Thing> a;
				static_assert(sizeof(a) == sizeof(Thing) + sizeof(Thing*), "");
				PR_CHECK(Thing::ref_count() == 0, true);

				a = Thing();
				PR_CHECK(bool(a), true);
				PR_CHECK(Thing::ref_count() == 1, true);

				auto b = a;
				PR_CHECK(bool(b), true);
				PR_CHECK(Thing::ref_count() == 2, true);

				a = nullptr;
				PR_CHECK(Thing::ref_count() == 1, true);

				b = nullptr;
				PR_CHECK(Thing::ref_count() == 0, true);

				std::vector<pr::optional<Thing>> vec;
				for (int i = 0; i != 10; ++i) vec.push_back(Thing());
				PR_CHECK(Thing::ref_count() == 10, true);

				vec.resize(0);
				PR_CHECK(Thing::ref_count() == 0, true);
			}
		}
	}
}
#endif

