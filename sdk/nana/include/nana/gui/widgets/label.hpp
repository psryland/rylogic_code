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
			enum class command
			{
				enter, leave, click
			};

			//class trigger
			//@brief:	draw the label
			class trigger: public drawer_trigger
			{
			public:
				struct impl_t;

				trigger();
				~trigger();
				impl_t * impl() const;
			private:
				void attached(widget_reference, graph_reference)	override;
				void refresh(graph_reference)	override;
				void mouse_move(graph_reference, const eventinfo&)	override;
				void mouse_leave(graph_reference, const eventinfo&)	override;
				void click(graph_reference, const eventinfo&)	override;
			private:
				impl_t * impl_;
			};

		}//end namespace label
	}//end namespace drawerbase

	///class label
	///@brief: defaine a label widget and it provides the interfaces to be operationa
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
		label& add_format_listener(const std::function<void(command, const nana::string&)> &);
		label& add_format_listener(std::function<void(command, const nana::string&)> &&);
		nana::size measure(unsigned limited) const;
		label& text_align(align, align_v = align_v::top);
	private:
		void _m_caption(const nana::string&);
	};
}//end namespace gui
}//end namespace nana
#endif
