/*
 *	A Label Control Implementation
 *	Nana C++ Library(http://www.nanapro.org)
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/label.hpp
 */

#ifndef NANA_GUI_WIDGET_LABEL_HPP
#define NANA_GUI_WIDGET_LABEL_HPP
#include "widget.hpp"


namespace nana{ namespace gui{
	namespace drawerbase
	{
		namespace label
		{
			struct command
			{
				enum t{enter, leave, click};
			};

			//class trigger
			//@brief:	draw the label
			class trigger: public nana::gui::drawer_trigger
			{
			public:
				struct impl_t;

				trigger();
				~trigger();
				void bind_window(widget_reference);
				impl_t * impl() const;
			private:
				void attached(graph_reference);
				void detached();
				void refresh(graph_reference);
				void mouse_move(graph_reference, const eventinfo&);
				void mouse_leave(graph_reference, const eventinfo&);
				void click(graph_reference, const eventinfo&);
			private:
				impl_t * impl_;
			};

		}//end namespace label
	}//end namespace drawerbase

	//class label
	//@brief: defaine a label widget and it provides the interfaces to be operationa
	class label
		: public widget_object<category::widget_tag, drawerbase::label::trigger>
	{
	public:
		typedef drawerbase::label::command command;
		label();
		label(window, bool visible);
		label(window, const nana::string& text, bool visible = true);
		label(window, const nana::char_t* text, bool visible = true);
		label(window, const rectangle& = rectangle(), bool visible = true);
		label& transparent(bool);
		bool transparent() const;
		label& format(bool);
		label& add_format_listener(const nana::functor<void(command::t, const nana::string&)> &);
		nana::size measure(unsigned limited) const;
		label& text_align(align::t, align_v::t = align_v::top);
	private:
		void _m_caption(const nana::string&);
	};
}//end namespace gui
}//end namespace nana
#endif
