/*
 *	A Drawer Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/detail/drawer.hpp
 */

#ifndef NANA_GUI_DETAIL_DRAWER_HPP
#define NANA_GUI_DETAIL_DRAWER_HPP

#include <vector>
#include "eventinfo.hpp"
#include <nana/paint/graphics.hpp>
#include <nana/paint/image.hpp>
#include <nana/functor.hpp>

namespace nana
{
namespace gui
{
	class widget;

	class drawer_trigger
		: private nana::noncopyable
	{
	public:
		typedef gui::widget&		widget_reference;
		typedef paint::graphics&	graph_reference;

		virtual ~drawer_trigger();
		virtual void bind_window(widget_reference);
		virtual void attached(graph_reference);	//none-const
		virtual void detached();	//none-const

		virtual void typeface_changed(graph_reference);
		virtual void refresh(graph_reference);

		virtual void resizing(graph_reference, const eventinfo&);
		virtual void resize(graph_reference, const eventinfo&);
		virtual void move(graph_reference, const eventinfo&);
		virtual void click(graph_reference, const eventinfo&);
		virtual void dbl_click(graph_reference, const eventinfo&);
		virtual void mouse_enter(graph_reference, const eventinfo&);
		virtual void mouse_move(graph_reference, const eventinfo&);
		virtual void mouse_leave(graph_reference, const eventinfo&);
		virtual void mouse_down(graph_reference, const eventinfo&);
		virtual void mouse_up(graph_reference, const eventinfo&);
		virtual void mouse_wheel(graph_reference, const eventinfo&);
		virtual void mouse_drop(graph_reference, const eventinfo&);

		virtual void focus(graph_reference, const eventinfo&);
		virtual void key_down(graph_reference, const eventinfo&);
		virtual void key_char(graph_reference, const eventinfo&);
		virtual void key_up(graph_reference, const eventinfo&);
		virtual void shortkey(graph_reference, const eventinfo&);
	};

	namespace detail
	{
		struct basic_window;

		namespace dynamic_drawing
		{
			//declaration
			class object;
		}

		//class drawer
		//@brief:	Every window has a drawer, the drawer holds a drawer_trigger for
		//			a widget.
		class drawer
			: nana::noncopyable
		{
		public:

			drawer();
			~drawer();

			void attached(basic_window*);

			void typeface_changed();
			void click(const eventinfo&);
			void dbl_click(const eventinfo&);
			void mouse_enter(const eventinfo&);
			void mouse_move(const eventinfo&);
			void mouse_leave(const eventinfo&);
			void mouse_down(const eventinfo&);
			void mouse_up(const eventinfo&);
			void mouse_wheel(const eventinfo&);
			void mouse_drop(const eventinfo&);
			void resizing(const eventinfo&);
			void resize(const eventinfo&);
			void move(const eventinfo&);
			void focus(const eventinfo&);
			void key_down(const eventinfo&);
			void key_char(const eventinfo&);
			void key_up(const eventinfo&);
			void shortkey(const eventinfo&);
			void map(window);	//Copy the root buffer to screen
			void refresh();
			drawer_trigger* realizer() const;
			void attached(drawer_trigger&);
			drawer_trigger* detached();
		public:
			void clear();
			void* draw(const nana::functor<void(paint::graphics&)> & , bool diehard);
			void erase(void* diehard);
			void string(int x, int y, unsigned color, const nana::char_t*);
			void line(int x, int y, int x2, int y2, unsigned color);
			void rectangle(int x, int y, unsigned width, unsigned height, unsigned color, bool issolid);
			void shadow_rectangle(int x, int y, unsigned width, unsigned height, nana::color_t beg, nana::color_t end, bool vertical);
			void bitblt(int x, int y, unsigned width, unsigned height, const nana::paint::graphics& graph, int srcx, int srcy);
			void bitblt(int x, int y, unsigned width, unsigned height, const nana::paint::image& img, int srcx, int srcy);
			void stretch(const nana::rectangle& r_dst, const nana::paint::graphics& graph, const nana::rectangle& r_src);
			void stretch(const nana::rectangle& r_dst, const nana::paint::image& img, const nana::rectangle& r_src);
			event_handle make_event(event_code::t, window wd);
		private:
			void _m_bground_pre();
			void _m_bground_end();
			void _m_draw_dynamic_drawing_object();
		public:
			nana::paint::graphics graphics;
		private:
			basic_window*	core_window_;
			drawer_trigger*	realizer_;
			std::vector<dynamic_drawing::object*>	dynamic_drawing_objects_;
			bool refreshing_;
		};
	}//end namespace detail
}//end namespace gui
}//end namespace nana

#endif
