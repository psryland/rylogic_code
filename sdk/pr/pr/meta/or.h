#ifndef PR_META_OR_H
#define PR_META_OR_H

#include "pr/meta/Constants.h"

namespace pr
{
	namespace mpl
	{
		namespace impl
		{
			template <bool lhs_value>
			struct or_
			{
				template <typename rhs>
				struct impl2 { enum { value = true }; };
			};

			template <>
			struct or_<false>
			{
				template <typename rhs>
				struct impl2 { enum { value = rhs::value }; };
			};
		}//namespace impl

		template <typename lhs, typename rhs>
		struct or_
		{
			enum { value = impl::or_<lhs::value>::template impl2<rhs>::value };
		};

		template <bool lhs, bool rhs>
		struct or_c
		{
			enum { value = impl::or_<lhs>::template impl2< bool_<rhs> >::value };
		};

	}//namespace mpl
}//namespace pr

#endif PR_META_OR_H
