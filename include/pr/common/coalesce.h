//******************************************
// Pointer coalesing operator
//  Copyright (c) 2023 Paul Ryland
//******************************************
#pragma once
#include <memory>
#include <type_traits>
#include "pr/common/refptr.h"

namespace pr
{
	template <typename T> requires (std::is_pointer_v<T>)
	inline constexpr T operator << (T lhs, T rhs)
	{
		return lhs ? lhs : rhs;
	}

	template <typename T, typename Dx>
	inline std::unique_ptr<T,Dx> operator << (std::unique_ptr<T,Dx> lhs, std::unique_ptr<T,Dx> rhs)
	{
		return lhs ? lhs : rhs;
	}

	template <typename T>
	inline std::shared_ptr<T> operator << (std::shared_ptr<T> lhs, std::shared_ptr<T> rhs)
	{
		return lhs ? lhs : rhs;
	}

	template <typename T>
	inline RefPtr<T> operator << (RefPtr<T> lhs, RefPtr<T> rhs)
	{
		return lhs ? lhs : rhs;
	}
}