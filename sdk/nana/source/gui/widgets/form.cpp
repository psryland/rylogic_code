/*
 *	A Form Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/form.cpp
 */

#include <nana/gui/widgets/form.hpp>

namespace nana{ namespace gui{
	namespace drawerbase
	{
		namespace form
		{
		//class trigger
			trigger::trigger():wd_(nullptr){}

			void trigger::attached(widget_reference widget, graph_reference graph)
			{
				wd_ = &widget;
				API::dev::make_drawer_event<events::size>(*wd_);
			}

			void trigger::refresh(graph_reference graph)
			{
				graph.rectangle(API::background(*wd_), true);
			}

			void trigger::resize(graph_reference graph, const eventinfo&)
			{
				graph.rectangle(API::background(*wd_), true);
				API::lazy_refresh();
			}
		}//end namespace form
	}//end namespace drawerbase

	//class form
	typedef widget_object<category::root_tag, drawerbase::form::trigger> form_base_t;


		form::form(const rectangle& r, const appearance& apr)
			: form_base_t(0, false, r, apr)
		{}

		form::form(window owner, const appearance& apr)
			: form_base_t(owner, false, API::make_center(owner, 300, 150), apr)
		{}

		form::form(window owner, const rectangle& r, const appearance& apr)
			: form_base_t(owner, false, r, apr)
		{}
	//end class form

	//class nested_form
		nested_form::nested_form(window owner, const appearance& apr)
			: form_base_t(owner, true, rectangle(), apr)
		{}

		nested_form::nested_form(window owner, const rectangle& r, const appearance& apr)
			: form_base_t(owner, true, r, apr)
		{}
	//end nested_form
}//end namespace gui
}//end namespace nana
