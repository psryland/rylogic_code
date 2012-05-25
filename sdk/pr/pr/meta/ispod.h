//******************************************************
//
//	Detect POD'ness
//
//******************************************************
// Usage:
//	struct pr_is_pod { enum { value = true }; };
//
#pragma once
#ifndef PR_META_IS_POD_H
#define PR_META_IS_POD_H

#include <type_traits>
#include "pr/meta/or.h"
#include "pr/meta/and.h"
#include "pr/meta/not.h"
#include "pr/meta/isclass.h"
#include "pr/meta/constants.h"

namespace pr
{
	namespace mpl
	{
		#if _MSC_VER >= 1600
		
		template <typename T> struct is_pod :std::is_pod<T>
		{};
		
		#else
		
		namespace impl
		{
			struct Tag
			{
				#if _MSC_VER <= 1300
				template <typename T> static true_  Resolve(T::pr_is_pod*);
				#else _MSC_VER <= 1300
				template <typename T> static true_  Resolve(typename T::pr_is_pod*);
				#endif//_MSC_VER <= 1300
				template <typename T> static false_ Resolve(...);
			};
			
			template <typename T> struct has_tag
			{
				enum { value = sizeof(Tag::Resolve<T>(0)) == sizeof(true_) };
			};
			
			template <typename T> struct get_tag
			{
				enum { value = T::pr_is_pod::value };
			};
		}
		
		template <typename T> struct is_pod
		{
			enum
			{
				value = or_
						<
							not_< is_class<T> >,
							and_
							<
								impl::has_tag<T>,
								impl::get_tag<T>
							>
						>::value
			};
		};
		
		#endif
	}
}
	
#endif
