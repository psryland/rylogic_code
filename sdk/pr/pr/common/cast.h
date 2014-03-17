//******************************************
// Byte Ptr Cast
//  Copyright © March 2008 Paul Ryland
//******************************************
// Use to cast any pointer to a byte pointer
#pragma once
#ifndef PR_COMMON_CAST_H
#define PR_COMMON_CAST_H

#include <cassert>
#include <type_traits>

namespace pr
{
	typedef unsigned char byte;

	// Casting from any type of pointer to a byte pointer
	// Use:
	//   int* int_ptr = ...
	//   byte* u8_ptr = byte_ptr(int_ptr);
	template <typename T> byte const* byte_ptr(T const* t) { return reinterpret_cast<byte const*>(t); }
	template <typename T> byte*       byte_ptr(T*       t) { return reinterpret_cast<byte*      >(t); }
	template <typename T> char const* char_ptr(T const* t) { return reinterpret_cast<char const*>(t); }
	template <typename T> char*       char_ptr(T*       t) { return reinterpret_cast<char*      >(t); }

	// Cast from a void pointer to a pointer of type 'T' (checking alignment)
	template <typename T> T const* type_ptr(void const* t)
	{
		assert(((t - static_cast<T*>(nullptr)) & std::alignment_of<T>::value) == 0 && "Point is not correctly aligned for type");
		return static_cast<T const*>(t);
	}
	template <typename T> T* type_ptr(void* t)
	{
		assert(((t - static_cast<T*>(nullptr)) & std::alignment_of<T>::value) == 0 && "Point is not correctly aligned for type");
		return static_cast<T*>(t);
	}

	// Casting between integral types
	// Use:
	//  int16_t s = -1302;
	//  uint8_t b = checked_cast<uint8_t>(s); <- gives an assert because -1302 cannot be stored in a uint8_t
	template <typename T, typename U> inline T checked_cast(U x)
	{
		assert(static_cast<U>(static_cast<T>(x)) == x && "Cast loses data");
		return static_cast<T>(x);
	}
}

#endif
