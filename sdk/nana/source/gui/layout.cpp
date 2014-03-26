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
			: event_handle_(nullptr)
		{
			owner_.kind = kind_gird;
			owner_.u.ref_gird = owner;
		}

		gird::gird()
			: event_handle_(nullptr)
		{
			owner_.kind = kind_window;
			owner_.u.ref_widget = nullptr;
		}

		gird::gird(window wd)
		{
			owner_.kind = kind_window;
			event_handle_ = API::make_event<events::size>(wd, nana::make_fun(*this, &gird::_m_resize));
			if(event_handle_)
			{
				owner_.u.ref_widget = wd;
				area_ = API::window_size(wd);
			}
			else
				owner_.u.ref_widget = nullptr;
		}

		gird::~gird()
		{
			API::umake_event(event_handle_);

			for(auto i : elements_)
				delete i;

			for(auto i : child_)
				delete i;
		}

		void gird::bind(window wd)
		{
			if(nullptr == owner_.u.ref_widget)
			{
				owner_.kind = kind_window;
				event_handle_ = API::make_event<events::size>(wd, nana::make_fun(*this, &gird::_m_resize));
				if(event_handle_)
				{
					owner_.u.ref_widget = wd;
					area_ = API::window_size(wd);
				}
			}
		}

		gird * gird::push(unsigned blank, unsigned scale)
		{
			element_tag * p = new element_tag(blank, scale);
			child_.push_back(p);
			_m_adjust_children();
			return p->u.ref_gird;
		}

		void gird::push(window wd, unsigned blank, unsigned scale)
		{
			child_.push_back(new element_tag(wd, blank, scale));
			_m_adjust_children();
		}

		gird * gird::add(unsigned blank, unsigned scale)
		{
			element_tag * p = new element_tag(blank, scale);
			elements_.push_back(p);
			_m_adjust_elements();
			return p->u.ref_gird;
		}

		void gird::add(window wd, unsigned blank, unsigned scale)
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
			area_ = API::window_size(owner_.u.ref_widget);
			_m_adjust_children();
		}

		template<typename Container>
		unsigned prepare_adjustable_pixels(const unsigned range_pixels, const Container& cont)
		{
			unsigned fixed = 0;
			unsigned number = 0;

			for(auto i : cont)
			{
				if(0 == i->scale) //adjustable
					++number;
				else
					fixed += i->scale;
				fixed += i->blank;
			}

			return ((number && (fixed < range_pixels)) ? (range_pixels - fixed) / number : 0);
		}

		void gird::_m_adjust_children()
		{
			unsigned pixels_of_adjustable = prepare_adjustable_pixels(area_.height, child_);

			int top = area_.y;
			nana::rectangle area;
			for(auto i : child_)
			{
				area.x = area_.x;
				area.y = top + i->blank;
				area.width = area_.width;

				if(0 == i->scale)
				{
					top += pixels_of_adjustable;
					area.height = pixels_of_adjustable;
				}
				else
				{
					top += i->scale;
					area.height = i->scale;
				}
				top += i->blank;
				i->area(area);
			}

			_m_adjust_elements();
		}

		void gird::_m_adjust_elements()
		{
			unsigned pixels_of_adjustable = prepare_adjustable_pixels(area_.width, elements_);

			int left = area_.x;
			for(auto i : elements_)
			{
				nana::rectangle area(left + i->blank, area_.y, 0, area_.height);

				if(0 == i->scale) //adjustable
				{
					left += pixels_of_adjustable;
					area.width = pixels_of_adjustable;
				}
				else
				{
					left += i->scale;
					area.width = i->scale;
				}
				left += i->blank;
				i->area(area);
			}

			for(auto i : fasten_elements_)
				API::move_window(i, area_.x, area_.y, area_.width, area_.height);
		}
	//end class gird
}//end namespace gui
}//end namespace nana

