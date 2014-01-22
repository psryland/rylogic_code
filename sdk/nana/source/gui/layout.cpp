/*
 *	An Edge Keeper Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/layout.cpp
 *	@brief: The edge_keeper automatically controls the position and size of a widget.
 */

#include <nana/gui/layout.hpp>

#include <vector>

namespace nana
{
namespace gui
{
	//class gird
		//struct element_tag
		struct gird::element_tag
		{
			unsigned blank;
			unsigned scale;
			kind_t kind;
			union
			{
				window ref_wnd;
				gird * ref_gird;
			}u;

			element_tag(window wd, unsigned blank, unsigned scale)
				:blank(blank), scale(scale), kind(kind_window)
			{
				u.ref_wnd = wd;
			}

			element_tag(unsigned blank, unsigned scale)
				:blank(blank), scale(scale), kind(kind_gird)
			{
				u.ref_gird = new gird(0, 0);
			}

			~element_tag()
			{
				if(kind == kind_gird)
					delete u.ref_gird;
			}

			void area(const nana::rectangle& r)
			{
				switch(kind)
				{
				case kind_window:
					API::move_window(u.ref_wnd, r.x, r.y, r.width, r.height);
					break;
				case kind_gird:
					u.ref_gird->area_ = r;
					u.ref_gird->_m_adjust_children();
					break;
				}
			}
		};
		//end struct element_tag

		gird::gird(gird * owner, unsigned scale)
			: event_handle_(0)
		{
			owner_.kind = kind_gird;
			owner_.u.ref_gird = owner;
		}

		gird::gird()
			: event_handle_(0)
		{
			owner_.kind = kind_window;
			owner_.u.ref_widget = 0;
		}

		gird::gird(window wd)
		{
			owner_.kind = kind_window;
			event_handle_ = API::make_event<events::size>(wd, nana::make_fun(*this, &gird::_m_resize));
			if(event_handle_)
			{
				owner_.u.ref_widget = wd;
				nana::size sz = API::window_size(wd);
				area_.width = sz.width;
				area_.height = sz.height;
			}
			else
				owner_.u.ref_widget = 0;
		}

		gird::~gird()
		{
			API::umake_event(event_handle_);

			for(std::vector<element_tag*>::iterator i = elements_.begin(); i != elements_.end(); ++i)
				delete (*i);

			for(std::vector<element_tag*>::iterator i = child_.begin(); i != child_.end(); ++i)
				delete (*i);
		}

		void gird::bind(window wd)
		{
			if(owner_.u.ref_widget == 0)
			{
				owner_.kind = kind_window;
				event_handle_ = API::make_event<events::size>(wd, nana::make_fun(*this, &gird::_m_resize));
				if(event_handle_)
				{
					owner_.u.ref_widget = wd;
					nana::size sz = API::window_size(wd);
					area_.width = sz.width;
					area_.height = sz.height;
				}
				else
					owner_.u.ref_widget = 0;
			}
		}

		gird * gird::push(unsigned blank, unsigned scale)
		{
			element_tag * p = new element_tag(blank, scale);
			child_.push_back(p);
			_m_adjust_children();
			return p->u.ref_gird;
		}

		void gird::push(nana::gui::window wd, unsigned blank, unsigned scale)
		{
			element_tag* p = new element_tag(wd, blank, scale);
			child_.push_back(p);
			_m_adjust_children();
		}

		gird * gird::add(unsigned blank, unsigned scale)
		{
			element_tag * p = new element_tag(blank, scale);
			elements_.push_back(p);
			_m_adjust_elements();
			return p->u.ref_gird;
		}

		void gird::add(nana::gui::window wd, unsigned blank, unsigned scale)
		{
			elements_.push_back(new element_tag(wd, blank, scale));
			_m_adjust_elements();
		}

		void gird::fasten(window wd)
		{
			fasten_elements_.push_back(wd);
			API::move_window(wd, area_.x, area_.y, area_.width, area_.height);
		}

		void gird::_m_resize()
		{
			nana::size sz = API::window_size(owner_.u.ref_widget);
			area_.width = sz.width;
			area_.height = sz.height;
			_m_adjust_children();
		}

		void gird::_m_adjust_children()
		{
			const unsigned pixels = area_.height;

			unsigned fixed = 0;
			unsigned number_of_adjustable = 0;
			for(std::vector<element_tag*>::iterator i = child_.begin(); i != child_.end(); ++i)
			{
				if(0 == (*i)->scale) //adjustable
					++number_of_adjustable;
				else
					fixed += (*i)->scale;
				fixed += (*i)->blank;
			}

			unsigned pixels_of_adjustable = 0;
			if(number_of_adjustable && (fixed < pixels))
				pixels_of_adjustable = (pixels - fixed) / number_of_adjustable;

			int top = area_.y;
			nana::rectangle area;
			for(std::vector<element_tag*>::iterator i = child_.begin(); i != child_.end(); ++i)
			{
				element_tag * p = *i;
				area.x = area_.x;
				area.y = top + p->blank;
				area.width = area_.width;

				if(0 == p->scale)
				{
					top += pixels_of_adjustable;
					area.height = pixels_of_adjustable;
				}
				else
				{
					top += p->scale;
					area.height = p->scale;
				}
				top += p->blank;
				p->area(area);
			}

			_m_adjust_elements();
		}

		void gird::_m_adjust_elements()
		{
			const unsigned pixels = area_.width;
			unsigned fixed = 0;
			unsigned number_of_adjustable = 0;
			for(std::vector<element_tag*>::iterator i = elements_.begin(); i != elements_.end(); ++i)
			{
				element_tag * p = *i;
				if(0 == p->scale) //adjustable
					++number_of_adjustable;
				else
					fixed += p->scale;
				fixed += p->blank;
			}

			unsigned pixels_of_adjustable = 0;
			if(number_of_adjustable && (fixed < pixels))
				pixels_of_adjustable = (pixels - fixed) / number_of_adjustable;


			int left = area_.x;
			for(std::vector<element_tag*>::iterator i = elements_.begin(); i != elements_.end(); ++i)
			{
				element_tag * p = *i;
				nana::rectangle area(left + p->blank, area_.y, 0, area_.height);

				if(0 == p->scale) //adjustable
				{
					left += pixels_of_adjustable;
					area.width = pixels_of_adjustable;
				}
				else
				{
					left += p->scale;
					area.width = p->scale;
				}
				left += p->blank;
				p->area(area);
			}

			for(std::vector<window>::iterator i = fasten_elements_.begin(); i != fasten_elements_.end(); ++i)
				API::move_window(*i, area_.x, area_.y, area_.width, area_.height);
		}
	//end class gird
}//end namespace gui
}//end namespace nana

