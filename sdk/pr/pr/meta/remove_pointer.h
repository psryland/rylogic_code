#pragma once
#ifndef PR_META_REMOVE_POINTER_H
#define PR_META_REMOVE_POINTER_H

#include <type_traits>

namespace pr
{
	namespace meta
	{
		#if _MSC_VER >= 1600

		template <class T> struct remove_pointer :std::remove_pointer<T>
		{};

		#else

		template< class T > struct remove_pointer                    {typedef T type;};
		template< class T > struct remove_pointer<T*>                {typedef T type;};
		template< class T > struct remove_pointer<T* const>          {typedef T type;};
		template< class T > struct remove_pointer<T* volatile>       {typedef T type;};
		template< class T > struct remove_pointer<T* const volatile> {typedef T type;};
		
		#endif
	}
}

#endif
