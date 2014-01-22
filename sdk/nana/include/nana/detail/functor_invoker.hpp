/*
 *	A Functor implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/detail/functor_invoker.hpp
 *	@description:
 *		This is a functor invoker definition.
 */
 
#ifndef NANA_DETAIL_FUNCTOR_INVOKER_HPP
#define NANA_DETAIL_FUNCTOR_INVOKER_HPP

namespace nana
{
	namespace detail
	{
		//class abs_invoker
		//@brief: a class defined an abstract invoker for describing a functor
		template<typename Function>
		class abs_invoker;
		
		//class invoker
		//@brief: invoker is used for saving a function address and invoking it.
		template<typename Function>
		class invoker: public abs_invoker<Function>{};

		template<typename FO, typename Function>
		class fo_invoker: public abs_invoker<Function>{};

		template<typename Function>
		class interface_holder
		{
		public:
			struct this_is_a_empty_interface_holder;
			typedef abs_invoker<Function> invoker_type;
			void operator()(this_is_a_empty_interface_holder&) const;
		};
	}//end namespace detail
}//end namespace nana
#endif
