//******************************************
// Aligned Storage
//  Copyright © Rylogic Ltd 2010
//******************************************

#pragma once
#ifndef PR_META_ALIGNED_STORAGE_H
#define PR_META_ALIGNED_STORAGE_H

#include <type_traits>
#include "pr/meta/alignedtype.h"

namespace pr
{
	namespace mpl
	{
		#if _MSC_VER >= 1600
		
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

#endif
