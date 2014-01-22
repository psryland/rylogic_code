/*
 *	A date chooser Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/date_chooser.hpp
 */

#ifndef NANA_GUI_WIDGETS_DATE_CHOOSER_HPP
#define NANA_GUI_WIDGETS_DATE_CHOOSER_HPP

#include "widget.hpp"
#include <nana/datetime.hpp>

namespace nana{ namespace gui{

	namespace drawerbase
	{
		namespace date_chooser
		{
			class trigger : public drawer_trigger
			{
			public:
				static const int topbar_height = 34;
				static const int border_size = 3;

				struct transform_action
				{
					enum t{none, to_left, to_right, to_enter, to_leave};
				};

				struct where
				{
					enum t{none, left_button, right_button, topbar, textarea};
				};

				struct page
				{
					enum t{
						date, month
					};
				};

				struct drawing_basis
				{
					nana::point refpos;
					double line_s;
					double row_s;
				};

				trigger();
				bool chose() const;
				nana::date read() const;
				void week_name(unsigned index, const nana::string& str);
				void month_name(unsigned index, const nana::string& str);
			private:
				void _m_init_color();
				where::t _m_pos_where(graph_reference graph, int x, int y);
				void _m_draw(graph_reference graph);
				void _m_draw_topbar(graph_reference graph);
				void _m_make_drawing_basis(drawing_basis& dbasis, graph_reference graph, const nana::point& refpos);
				void _m_draw_pos(drawing_basis & dbasis, graph_reference graph, int x, int y, const nana::string& str, bool primary, bool sel);
				void _m_draw_pos(drawing_basis & dbasis, graph_reference graph, int x, int y, int number, bool primary, bool sel);
				void _m_draw_ex_days(drawing_basis & dbasis, graph_reference graph, int begx, int begy, bool before);
				void _m_draw_days(const nana::point& refpos, graph_reference graph);
				void _m_draw_months(const nana::point& refpos, graph_reference graph);
				bool _m_get_trace(int x, int y, int & res);
				void _m_perf_transform(transform_action::t tfid, graph_reference, graph_reference dirtybuf, graph_reference newbuf, const nana::point& refpos);
			private:
				void refresh(graph_reference);
				void bind_window(widget_reference);
				void attached(graph_reference);
				void detached();
				void mouse_move(graph_reference, const eventinfo&);
				void mouse_leave(graph_reference, const eventinfo&);
				void mouse_up(graph_reference, const eventinfo&);
			private:
				nana::string weekstr_[7];
				nana::string monthstr_[12];

				nana::gui::widget * widget_;
				
				bool	chose_;
				page::t	page_;
				where::t	pos_;
				nana::point trace_pos_;

				drawing_basis	dbasis_;

				struct
				{
					int year;
					int month;
					int day;
				}chdate_;

				struct
				{
					int year;
					int month;				
				}chmonth_;

				struct color_tag
				{
					nana::color_t highlight;
					nana::color_t selected;
					nana::color_t normal;
					nana::color_t bkcolor;
				}color_;
			};
		
		}//end namespace date_chooser
	
	}//end namespace drawerbase

	class date_chooser
		: public widget_object<category::widget_tag, drawerbase::date_chooser::trigger>
	{
	public:
		date_chooser();
		date_chooser(window, bool visible);
		date_chooser(window, const nana::string& text, bool visible = true);
		date_chooser(window, const nana::char_t* text, bool visible = true);
		date_chooser(window, const rectangle& = rectangle(), bool visible = true);

		bool chose() const;
		nana::date read() const;
		void weekstr(unsigned index, const nana::string&);
		void monthstr(unsigned index, const nana::string&);
	};
}//end namespace gui
}//end namespace nana

#endif
