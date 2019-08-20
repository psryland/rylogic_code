//******************************************
// Byte Ptr Cast
//  Copyright (c) March 2008 Paul Ryland
//******************************************
// Use to cast any pointer to a byte pointer
#pragma once

#include <cassert>
#include <type_traits>

namespace pr
{
	using byte = unsigned char;

	// Casting from any type of pointer to a byte pointer
	// Use:
	//   int* int_ptr = ...
	//   byte* u8_ptr = byte_ptr(int_ptr);
	// These are templates so that byte_ptr(0) does not require an extra cast
	template <typename T> constexpr byte const* byte_ptr(T const* t) { return reinterpret_cast<byte const*>(t); }
	template <typename T> constexpr byte*       byte_ptr(T*       t) { return reinterpret_cast<byte*      >(t); }
	template <typename T> constexpr char const* char_ptr(T const* t) { return reinterpret_cast<char const*>(t); }
	template <typename T> constexpr char*       char_ptr(T*       t) { return reinterpret_cast<char*      >(t); }
	constexpr byte const* byte_ptr(nullptr_t) { return reinterpret_cast<byte const*>(0); }
	constexpr char const* char_ptr(nullptr_t) { return reinterpret_cast<char const*>(0); }

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

	// Casting between integral types
	// Use:
	//  int16_t s = -1302;
	//  uint8_t b = checked_cast<uint8_t>(s); <- gives an assert because -1302 cannot be stored in a uint8_t
	template <typename T, typename U> inline T checked_cast(U x)
	{
		assert("Cast loses data" && static_cast<U>(static_cast<T>(x)) == x);
		return static_cast<T>(x);
	}

	// Checked static cast
	template <typename T, typename U> inline T s_cast(U x)
	{
		return checked_cast<T,U>(x);
	}
}
