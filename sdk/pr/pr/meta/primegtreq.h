// Shamelessly lifted from Steve Robb's fuz library

#ifndef PR_META_PRIME_GTREQ_H
#define PR_META_PRIME_GTREQ_H

#include "pr/meta/IsPrime.h"

namespace pr
{
	namespace mpl
	{
		namespace impl
		{
			namespace prime_gtreq
			{
				template <std::size_t OddValue, bool IsPrime>
				struct impl
				{
					static const std::size_t next  = OddValue + static_cast<std::size_t>(2);
					static const std::size_t value = impl<next, pr::mpl::is_prime<next>::value>::value;
				};

				template <std::size_t OddValue>
				struct impl<OddValue, true>
				{
					static const std::size_t value = OddValue;
				};
			}//namespace prime_gtreq
		}//namespace impl

		template <std::size_t Value>
		struct prime_gtreq
		{
			static const std::size_t value = (Value <= 2) ? 2 : impl::prime_gtreq::impl<Value|1, pr::mpl::is_prime<Value|1>::value>::value;
		};
	}//namespace mpl
}//namespace pr

#endif//PR_META_PRIME_GTREQ_H
