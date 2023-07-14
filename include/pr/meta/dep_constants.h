//******************************************
// Dependent Constants
//  Copyright (c) Rylogic Ltd 2008
//******************************************
// Constants that are dependent on a type.
// Used to prevent instantiation of static asserts

#pragma once
namespace pr
{
	// Use static_assert(dep<MyType, IS_DEFINED>, "MyType was instantiated when 'IS_DEFINED' is 0");
	template <typename T, bool Value>
	const bool dependent = Value;

	// Use static_assert(dep_false<MyType>, "MyType was instantiated");
	template <typename T>
	const bool dependent_false = dependent<T, false>;

	template <typename T>
	const bool dependent_true = dependent<T, true>;
}
