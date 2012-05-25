//******************************************
// Byte Ptr Cast
//  Copyright © March 2008 Paul Ryland
//******************************************
// Use to cast any pointer to a byte pointer
#pragma once
#ifndef PR_COMMON_BYTE_PTR_CAST_H
#define PR_COMMON_BYTE_PTR_CAST_H

namespace pr
{
	typedef unsigned char byte;
	
	template <typename T> byte const* byte_ptr(T const* t) { return reinterpret_cast<byte const*>(t); }
	template <typename T> byte*       byte_ptr(T*       t) { return reinterpret_cast<byte*      >(t); }
	
	template <typename T> T const* type_ptr(void const* t) { return static_cast<T const*>(t); }
	template <typename T> T*       type_ptr(void*       t) { return static_cast<T*      >(t); }
}

#endif
