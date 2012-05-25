#ifndef PR_META_IS_CLASS_H
#define PR_META_IS_CLASS_H

#include "pr/meta/Constants.h"

namespace pr
{
	namespace mpl
	{
		namespace impl
		{
			namespace is_class_impl
			{
				template <typename T> true_  Resolve(void (T::*)());
				template <typename T> false_ Resolve(...);

			}//namespace is_class_impl
		}//namespace impl

		template <typename T>
		struct is_class
		{
			enum { value = sizeof(impl::is_class_impl::Resolve<T>(0)) == sizeof(true_) };
		};

	}//namespace mpl
}//namespace pr

#endif//PR_META_IS_CLASS_H
