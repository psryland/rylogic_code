// Algorithm from this page: http://www.embedded.com/98/9802fe2.htm

#ifndef PR_META_SQRT_H
#define PR_META_SQRT_H

#include "pr/meta/Constants.h"

namespace pr
{
	namespace mpl
	{
		namespace impl
		{
			template <std::size_t Iter, std::size_t N, std::size_t Root = 0, std::size_t Rem = 0>
			struct square_root_iter
			{
				static const std::size_t divisor = (Root << 2) + 1;
				static const std::size_t nextrem = (Rem  << 2) + (N >> 30);
				static const std::size_t mult	 = !!(divisor <= nextrem);
				static const std::size_t value	 = square_root_iter<Iter - 1, N << 2, (Root << 1) + mult, nextrem - mult * divisor>::value;
			};

			template <std::size_t N, std::size_t Root, std::size_t Rem>
			struct square_root_iter<0, N, Root, Rem>
			{
				static const std::size_t value = Root;
			};
		}

		template <std::size_t N>
		struct square_root
		{
			static const std::size_t value = impl::square_root_iter<16, N>::value;
		};

	}//namespace mpl
}//namespace pr

#endif PR_META_SQRT_H
