//******************************************
// Byte Ptr Cast
//  Copyright (c) March 2008 Paul Ryland
//******************************************
// Use to cast any pointer to a byte pointer
#pragma once

#include <cassert>
#include <exception>
#include <type_traits>

namespace pr
{
	using byte = unsigned char;

	// Casting from any type of pointer to a byte pointer
	// Use:
	//   int* int_ptr = ...
	//   byte* u8_ptr = byte_ptr(int_ptr);
	template <typename T> constexpr byte const* byte_ptr(T const* t) { return reinterpret_cast<byte const*>(t); }
	template <typename T> constexpr byte*       byte_ptr(T*       t) { return reinterpret_cast<byte*      >(t); }
	template <typename T> constexpr char const* char_ptr(T const* t) { return reinterpret_cast<char const*>(t); }
	template <typename T> constexpr char*       char_ptr(T*       t) { return reinterpret_cast<char*      >(t); }
	constexpr byte const* byte_ptr(nullptr_t) { return static_cast<byte const*>(nullptr); }
	constexpr char const* char_ptr(nullptr_t) { return static_cast<char const*>(nullptr); }

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
	template <typename T, bool RuntimeCheck = false, typename U> inline T s_cast(U x)
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
	
	template <typename T, typename U> [[deprecated("use s_cast")]] inline T checked_cast(U x)
	{
		return s_cast<T,U>(x);
	}
}
