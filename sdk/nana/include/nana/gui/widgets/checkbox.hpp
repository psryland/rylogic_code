/*
 *	A CheckBox Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/checkbox.hpp
 */

#ifndef NANA_GUI_WIDGET_CHECKBOX_HPP
#define NANA_GUI_WIDGET_CHECKBOX_HPP
#include "widget.hpp"
#include <vector>

namespace nana{	namespace gui{
namespace drawerbase
{
	namespace checkbox
	{

		class drawer
			: public drawer_trigger
		{
			struct implement;
		public:
			drawer();
			~drawer();
			void bind_window(widget_reference);
			void attached(graph_reference);
			void detached();
			void refresh(graph_reference);
			void mouse_enter(graph_reference, const eventinfo&);
			void mouse_leave(graph_reference, const eventinfo&);
			void mouse_down(graph_reference, const eventinfo&);
			void mouse_up(graph_reference, const eventinfo&);
		public:
			implement * impl() const;
		private:
			void _m_draw(graph_reference);
			void _m_draw_background(graph_reference);
			void _m_draw_checkbox(graph_reference, unsigned first_line_height);
			void _m_draw_title(graph_reference);
		private:
			static const int interval = 4;
			widget* widget_;
			unsigned state_;
			implement * impl_;
		};
	}//end namespace checkbox
}//end namespace drawerbase

	class checkbox
		: public widget_object<category::widget_tag, drawerbase::checkbox::drawer>
	{
	public:
		checkbox();
		checkbox(window, bool visible);
		checkbox(window, const nana::string& text, bool visible = true);
		checkbox(window, const nana::char_t* text, bool visible = true);
		checkbox(window, const rectangle& = rectangle(), bool visible = true);

		void element_set(const char* name);
		void react(bool want);
		bool checked() const;
		void check(bool chk);
		void radio(bool);
		void transparent(bool value);
		bool transparent() const;
	};//end class checkbox

	class radio_group
	{
		struct element_tag
		{
			checkbox * uiobj;
			event_handle eh_checked;
			event_handle eh_destroy;
		};
	public:
		~radio_group();
		void add(checkbox&);
		std::size_t checked() const;
	private:
		void _m_checked(const eventinfo&);
		void _m_destroy(const eventinfo&);
	private:
		std::vector<element_tag> ui_container_;
	};
}//end namespace gui
}//end namespace nana

#endif
