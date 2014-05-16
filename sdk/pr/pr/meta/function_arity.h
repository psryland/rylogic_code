//******************************************
// Function Arity
//  Copyright (c) Oct 2009 Paul Ryland
//******************************************
#ifndef PR_META_FUNCTION_ARITY_H
#define PR_META_FUNCTION_ARITY_H

namespace pr
{
	namespace meta
	{
		template <typename Function> struct function_arity;
		template <typename R                                                                                                        > struct function_arity<R(                              )> { enum { value = 0 }; };
		template <typename R, typename P0                                                                                           > struct function_arity<R(P0                            )> { enum { value = 1 }; };
		template <typename R, typename P0, typename P1                                                                              > struct function_arity<R(P0, P1                        )> { enum { value = 2 }; };
		template <typename R, typename P0, typename P1, typename P2                                                                 > struct function_arity<R(P0, P1, P2                    )> { enum { value = 3 }; };
		template <typename R, typename P0, typename P1, typename P2, typename P3                                                    > struct function_arity<R(P0, P1, P2, P3                )> { enum { value = 4 }; };
		template <typename R, typename P0, typename P1, typename P2, typename P3, typename P4                                       > struct function_arity<R(P0, P1, P2, P3, P4            )> { enum { value = 5 }; };
		template <typename R, typename P0, typename P1, typename P2, typename P3, typename P4, typename P5                          > struct function_arity<R(P0, P1, P2, P3, P4, P5        )> { enum { value = 6 }; };
		template <typename R, typename P0, typename P1, typename P2, typename P3, typename P4, typename P5, typename P6             > struct function_arity<R(P0, P1, P2, P3, P4, P5, P6    )> { enum { value = 7 }; };
		template <typename R, typename P0, typename P1, typename P2, typename P3, typename P4, typename P5, typename P6, typename P7> struct function_arity<R(P0, P1, P2, P3, P4, P5, P6, P7)> { enum { value = 8 }; };
	}
}

#endif
