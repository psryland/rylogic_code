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
	const bool dependant = Value;

	// Use static_assert(dep_false<MyType>, "MyType was instantiated");
	template <typename T>
	const bool dependant_false = dependant<T, false>;

	template <typename T>
	const bool dependant_true = dependant<T, true>;
}
