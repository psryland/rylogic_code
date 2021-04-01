//******************************************
// uint8_t Ptr Cast
//  Copyright (c) March 2008 Paul Ryland
//******************************************
// Use to cast any pointer to a uint8_t pointer
#pragma once

#include <cstdint>
#include <cassert>
#include <stdexcept>
#include <type_traits>

namespace pr
{
	// Casting from any type of pointer to a uint8_t pointer
	// Use:
	//   int* int_ptr = ...
	//   uint8_t* u8_ptr = byte_ptr(int_ptr);
	template <typename T> constexpr uint8_t const* byte_ptr(T const* t) { return reinterpret_cast<uint8_t const*>(t); }
	template <typename T> constexpr uint8_t*       byte_ptr(T*       t) { return reinterpret_cast<uint8_t*      >(t); }
	template <typename T> constexpr char const*    char_ptr(T const* t) { return reinterpret_cast<char const*>(t); }
	template <typename T> constexpr char*          char_ptr(T*       t) { return reinterpret_cast<char*      >(t); }
	
	// Handle casting nullptr to bytes/chars
	constexpr uint8_t const* byte_ptr(nullptr_t)
	{
		return static_cast<uint8_t const*>(nullptr);
	}
	constexpr char const* char_ptr(nullptr_t)
	{
		return static_cast<char const*>(nullptr);
	}

	// Cast from a void pointer to a pointer of type 'T' (checking alignment)
	template <typename T> constexpr T const* type_ptr(void const* t)
	{
		assert(((byte_ptr(t) - byte_ptr(nullptr)) % std::alignment_of<T>::value) == 0 && "Point is not correctly aligned for type");
		return static_cast<T const*>(t);
	}
	template <typename T> constexpr T* type_ptr(void* t)
	{
		assert(((byte_ptr(t) - byte_ptr(nullptr)) % std::alignment_of<T>::value) == 0 && "Point is not correctly aligned for type");
		return static_cast<T*>(t);
	}

	// Static cast for integral types with optional runtime checking.
	// Use:
	//  int16_t s = -1302;
	//  uint8_t b = s_cast<uint8_t>(s); <- gives an assert because -1302 cannot be stored in a uint8_t
	//  uint8_t b = s_cast<uint8_t,true>(s); <- throws an exception because -1302 cannot be stored in a uint8_t
	template <typename T, bool RuntimeCheck = false, typename U> constexpr T s_cast(U x)
	{
		if constexpr (RuntimeCheck)
		{
			if (static_cast<U>(static_cast<T>(x)) != x)
				throw std::runtime_error("Cast loses data");
		}
		else
		{
			assert("Cast loses data" && static_cast<U>(static_cast<T>(x)) == x);
		}
		return static_cast<T>(x);
	}
}
