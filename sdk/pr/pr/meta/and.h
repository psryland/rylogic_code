#ifndef PR_META_AND_H
#define PR_META_AND_H

#include "pr/meta/Constants.h"

namespace pr
{
	namespace mpl
	{
		namespace impl
		{
			template <bool lhs_value>
			struct and_
			{
				template <typename rhs>
				struct impl2 { enum { value = rhs::value }; };
			};

			template <>
			struct and_<false>
			{
				template <typename rhs>
				struct impl2 { enum { value = false }; };
			};
		}//namespace impl

		template <typename lhs, typename rhs>
		struct and_
		{
			enum { value = impl::and_<lhs::value>::template impl2<rhs>::value };
		};

		template <bool lhs, bool rhs>
		struct and_c
		{
			enum { value = impl::and_<lhs>::template impl2< bool_<rhs> >::value };
		};

	}//namespace mpl
}//namespace pr

#endif//PR_META_AND_H
