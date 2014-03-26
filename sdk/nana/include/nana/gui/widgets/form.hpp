/*
 *	A Form Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/form.hpp
 */

#ifndef NANA_GUI_WIDGET_FORM_HPP
#define NANA_GUI_WIDGET_FORM_HPP

#include "widget.hpp"

namespace nana{namespace gui{
	namespace drawerbase
	{
		namespace form
		{
			class trigger: public drawer_trigger
			{
			public:
				trigger();
				void attached(widget_reference, graph_reference)	override;
				void refresh(graph_reference)	override;
				void resize(graph_reference, const eventinfo&)	override;
			private:
				widget*	wd_;
			};
		}//end namespace form
	}//end namespace drawerbase

	class form: public widget_object<category::root_tag, drawerbase::form::trigger>
	{
	public:
		typedef nana::gui::appear appear;

		form(const rectangle& = API::make_center(300, 200), const appearance& = appearance());
		form(window, const appearance& = appearance());
		form(window, const rectangle&, const appearance& = appearance());
	};

	class nested_form: public widget_object<category::root_tag, drawerbase::form::trigger>
	{
	public:
		typedef nana::gui::appear appear;

		nested_form(window, const appearance&);
		nested_form(window, const rectangle& = rectangle(), const appearance& = appearance());
	};
}//end namespace gui
}//end namespace nana
#endif
