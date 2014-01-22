/*
 *	A textbase extra eventbase implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/skeletons/textbase_extra_evtbase.hpp
 *	@description: It is defined outside, some headers like textbox should not include a whole textbase for its ext_evtbase 
 *
 */

#ifndef NANA_GUI_WIDGET_TEXTBASE_EXTRA_EVTBASE_HPP
#define NANA_GUI_WIDGET_TEXTBASE_EXTRA_EVTBASE_HPP

#include <nana/functor.hpp>

namespace nana{	namespace gui{	namespace widgets
{
	namespace skeletons
	{
		template<typename CharT>
		struct textbase_extra_evtbase
		{
			fn_group<void()> first_change;	///< An event for the text first change after text is been opened or stored.
		};
	}//end namespace skeletons
}//end namespace widgets
}//end namespace gui
}//end namespace nana

#endif
