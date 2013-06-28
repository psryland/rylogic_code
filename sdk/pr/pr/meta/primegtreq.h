// Shamelessly lifted from Steve Robb's fuz library

#ifndef PR_META_PRIME_GTREQ_H
#define PR_META_PRIME_GTREQ_H

#include "pr/meta/IsPrime.h"

namespace pr
{
	namespace meta
	{
		namespace impl
		{
			namespace prime_gtreq
			{
				template <std::size_t OddValue, bool IsPrime>
				struct impl
				{
					static const std::size_t next  = OddValue + static_cast<std::size_t>(2);
					static const std::size_t value = impl<next, pr::meta::is_prime<next>::value>::value;
				};

				template <std::size_t OddValue>
				struct impl<OddValue, true>
				{
					static const std::size_t value = OddValue;
				};
			}
		}

		template <std::size_t Value>
		struct prime_gtreq
		{
			static const std::size_t value = (Value <= 2) ? 2 : impl::prime_gtreq::impl<Value|1, pr::meta::is_prime<Value|1>::value>::value;
		};
	}
}

#endif
