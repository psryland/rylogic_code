//*********************************************
// AudioManager
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/audio/forward.h"

namespace pr::audio
{
	// Helper for allocating and constructing a type using 'alloc_traits'
	template <typename T, typename... Args>
	[[nodiscard]] inline T* New(Args&&... args)
	{
		Allocator<T> alex;
		auto ptr = alloc_traits<T>::allocate(alex, sizeof(T));
		alloc_traits<T>::construct(alex, ptr, std::forward<Args>(args)...);
		return ptr;
	}
	template <typename T>
	inline void Delete(T* ptr)
	{
		Allocator<T> alex;
		alloc_traits<T>::destroy(alex, ptr);
		alloc_traits<T>::deallocate(alex, ptr, 1);
	}
}