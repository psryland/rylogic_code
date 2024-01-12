//******************************************
// Pointer coalesing operator
//  Copyright (c) 2023 Paul Ryland
//******************************************
#pragma once
#include <memory>
#include <concepts>
#include <type_traits>
#include "pr/common/refptr.h"

namespace pr
{
	template <typename T> concept RawPointerType = requires
	{
		std::is_pointer_v<T>;
	};
	template <typename T> concept UniquePointerType = requires
	{
		std::is_same_v<T, std::unique_ptr<typename T::element_type, typename T::deleter_type>>;
	};
	template <typename T> concept SharedPointerType = requires
	{
		std::is_same_v<T, std::shared_ptr<typename T::element_type>>;
	};
	template <typename T> concept RefPtrPointerType = requires
	{
		std::is_same_v<T, RefPtr<typename T::element_type>>;
	};
	template <typename T> concept PointerType = requires
	{
		RawPointerType<T> ||
		UniquePointerType<T> ||
		SharedPointerType<T> ||
		RefPtrPointerType<T>;
	};
	template <typename F, typename T> concept PointerFactoryType = requires (F f)
	{
		PointerType<T>;
		{ f() } -> std::convertible_to<T>;
	};

	template <typename T, typename... Args> requires PointerType<T>
	inline constexpr T coalesce(T lhs, Args... rhs)
	{
		if constexpr (sizeof...(rhs) == 0)
			return lhs;
		else
			return lhs ? lhs : coalesce(rhs...);
	}

	template <PointerType T, PointerFactoryType<T> F>
	inline constexpr T coalesce(T lhs, F rhs)
	{
		return lhs ? lhs : rhs();
	}

	//template <PointerType T>
	//inline T& coalesce (T& lhs, T rhs)
	//{
	//	return lhs ? lhs : (lhs = rhs);
	//}

	//template <PointerType T, PointerFactoryType<T> F>
	//inline T& coalesce (T& lhs, F&& rhs)
	//{
	//	return lhs ? lhs : (lhs = rhs());
	//}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	namespace unittests::coalesce
	{
		int const s_outside = 42;

		int create_called_count = 0;
		int const* CreateInt()
		{
			++create_called_count;
			return &s_outside;
		}
	}
	PRUnitTest(CoalesceTests)
	{
		using namespace pr;
		using namespace unittests::coalesce;

		int const s_inside = 24;
		int const* inside = &s_inside;
		int const* outside = &s_outside;
		int const* ptr;
		
		static_assert(PointerType<const int*>);
		static_assert(PointerType<decltype(inside)>);
		static_assert(PointerType<decltype(outside)>);
		static_assert(PointerType<decltype(ptr)>);

		ptr = inside;
		ptr = coalesce(ptr, outside);
		PR_CHECK(ptr == inside, true);

		ptr = nullptr;
		ptr = coalesce(ptr, outside);
		PR_CHECK(ptr == outside, true);

		ptr = inside;
		create_called_count = 0;
		ptr = coalesce(ptr, CreateInt);
		PR_CHECK(ptr == inside, true);
		PR_CHECK(create_called_count == 0, true);

		ptr = nullptr;
		create_called_count = 0;
		ptr = coalesce(ptr, [] { return CreateInt(); });
		PR_CHECK(ptr == outside, true);
		PR_CHECK(create_called_count == 1, true);

		//ptr = inside;
		//ptr <<= outside;
		//PR_CHECK(ptr == inside, true);

		//ptr = nullptr;
		//ptr <<= outside;
		//PR_CHECK(ptr == outside, true);

		//ptr = inside;
		//create_called_count = 0;
		//ptr <<= [] { return CreateInt(); };
		//PR_CHECK(ptr == inside, true);
		//PR_CHECK(create_called_count == 1, true);

		//ptr = nullptr;
		//create_called_count = 0;
		//ptr <<= [] { return CreateInt(); };
		//PR_CHECK(ptr == outside, true);
		//PR_CHECK(create_called_count == 1, true);
	}
}
#endif