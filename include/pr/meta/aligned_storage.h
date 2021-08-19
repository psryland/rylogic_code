//******************************************
// Aligned Storage
//  Copyright (C) Rylogic Ltd 2010
//******************************************

#pragma once

// <type_traits> was introduced in sp1
#if defined(_MSC_FULL_VER) && _MSC_FULL_VER < 150030729
#error VS2008 SP1 or greater is required to build this file
#endif

#include <type_traits>
#include "pr/meta/aligned_type.h"

namespace pr
{
	namespace meta
	{
		#if _MSC_VER >= 1600
		
		// Use using buffer_t = aligned_storage<sizeof(Thing), alignof(Thing)>::type;
		template <std::size_t Size, std::size_t Alignment>
		struct aligned_storage :std::aligned_storage<Size,Alignment>
		{};
		
		#else
		
		template <std::size_t Size, std::size_t Alignment>
		struct aligned_storage
		{
			#ifdef _MSC_VER
			#pragma warning(push)
			#pragma warning(disable : 4324)
			#endif
			
			union type
			{
				unsigned char m_buffer[Size];
				typename aligned_type<Alignment>::type aligner;
				struct pr_is_pod { enum { value = true }; };
			};
			
			#ifdef _MSC_VER
			#pragma warning(pop)
			#endif
		};
		
		#endif
	}
}
