/*
 *	A Tooltip Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/tooltip.cpp
 */

#include <nana/gui/tooltip.hpp>
#include <nana/gui/timer.hpp>

namespace nana{ namespace gui{
	namespace drawerbase
	{
		namespace tooltip
		{
			class drawer
				: public nana::gui::drawer_trigger
			{
			public:
				void set_tooltip_text(const nana::string& text)
				{
					text_ = text;

					nana::size txt_s;

					nana::string::size_type beg = 0;
					nana::string::size_type off = text.find('\n', beg);

					for(;off != nana::string::npos; off = text.find('\n', beg))
					{
						nana::size s = graph_->text_extent_size(text_.c_str() + beg, off - beg);
						if(s.width > txt_s.width)
							txt_s.width = s.width;
						txt_s.height += s.height + 2;
						beg = off + 1;
					}

					nana::size s = graph_->text_extent_size(text_.c_str() + beg);
					if(s.width > txt_s.width)
						txt_s.width = s.width;
					txt_s.height += s.height;

					widget_->size(txt_s.width + 10, txt_s.height + 10);
				}
			private:
				void bind_window(widget_reference widget)
				{
					widget_ = &widget;
				}
				void attached(graph_reference graph)
				{
					graph_ = &graph;
				}

				void refresh(graph_reference graph)
				{
					nana::size text_s = graph.text_extent_size(text_);

					graph.rectangle(0, 0, graph.width(), graph.height(), 0x0, false);
					graph.rectangle(1, 1, graph.width() - 2, graph.height() - 2, 0xF0F0F0, true);

					const int x = 5;
					int y = 5;

					nana::string::size_type beg = 0;
					nana::string::size_type off = text_.find('\n', beg);

					for(;off != nana::string::npos; off = text_.find('\n', beg))
					{
						graph.string(x, y, 0x0, text_.substr(beg, off - beg));
						y += text_s.height + 2;
						beg = off + 1;
					}

					graph.string(x, y, 0x0, text_.c_str() + beg, text_.size() - beg);
				}
			private:
				widget	*widget_;
				nana::paint::graphics * graph_;
				nana::string text_;
			};

			class uiform
				: public widget_object<category::root_tag, drawer>
			{
				typedef widget_object<category::root_tag, drawer> base_type;
			public:
				uiform()
					:base_type(rectangle(), appear::bald<appear::floating>())
				{
					API::take_active(this->handle(), false, 0);
					timer_.interval(500);
					timer_.make_tick(nana::functor<void()>(*this, &uiform::_m_tick));
				}

				void set_tooltip_text(const nana::string& text)
				{
					this->get_drawer_trigger().set_tooltip_text(text);
				}
			private:
				void _m_tick()
				{
					timer_.enable(false);
					this->show();
				}

			private:
				timer timer_;
			};//class uiform


			class controller
			{
				typedef std::pair<window, nana::string> pair_t;
				typedef controller self_type;

				controller()
					:window_(0), count_ref_(0)
				{}
			public:

				static threads::recursive_mutex& mutex()
				{
					static threads::recursive_mutex rcs_mutex;
					return rcs_mutex;
				}

				static controller* object(bool destroy = false)
				{
					static controller* ptr;
					if(destroy)
					{
						delete ptr;
						ptr = 0;
					}
					else if(ptr == 0)
					{
						ptr = new controller;
					}
					return ptr;
				}

				void inc()
				{
					++count_ref_;
				}

				unsigned long dec()
				{
					return --count_ref_;
				}

				void set(window wd, const nana::string& str)
				{
					pair_t * p = _m_get(wd);
					p ->second = str;
				}

				void show(int x, int y, const nana::string& text)
				{
					if(0 == window_)
						window_ = new uiform;

					window_->set_tooltip_text(text);
					nana::size sz = window_->size();
					nana::size screen_size = API::screen_size();
					if(x + sz.width >= screen_size.width)
						x = static_cast<int>(screen_size.width) - static_cast<int>(sz.width);

					if(y + sz.height >= screen_size.height)
						y = static_cast<int>(screen_size.height) - static_cast<int>(sz.height);

					if(x < 0) x = 0;
					if(y < 0) y = 0;
					window_->move(x, y);
				}

				void close()
				{
					delete window_;
					window_ = 0;
				}
			private:
				void _m_enter(const eventinfo& ei)
				{
					pair_t * p = _m_get(ei.window);
					if(p && p->second.size())
					{
						nana::point pos = API::cursor_position();
						this->show(pos.x, pos.y + 20, p->second);
					}
				}

				void _m_leave(const eventinfo& ei)
				{
					delete window_;
					window_ = 0;
				}

				void _m_destroy(const eventinfo& ei)
				{
					for(std::vector<pair_t*>::iterator i = cont_.begin(); i != cont_.end(); ++i)
					{
						if((*i)->first == ei.window)
						{
							cont_.erase(i);
							return;
						}
					}
				}
			private:
				pair_t* _m_get(nana::gui::window wd)
				{
					for(std::vector<pair_t*>::iterator i = cont_.begin(); i != cont_.end(); ++i)
					{
						if((*i)->first == wd)
							return *i;
					}

					API::make_event<events::mouse_enter>(wd, nana::functor<void(const eventinfo&)>(*this, &self_type::_m_enter));
					API::make_event<events::mouse_leave>(wd, nana::functor<void(const eventinfo&)>(*this, &self_type::_m_leave));
					API::make_event<events::mouse_down>(wd, nana::functor<void(const eventinfo&)>(*this, &self_type::_m_leave));
					API::make_event<events::destroy>(wd, nana::functor<void(const eventinfo&)>(*this, &self_type::_m_destroy));

					pair_t * newp = new pair_t(wd, nana::string());
					cont_.push_back(newp);

					return newp;
				}
			private:
				uiform * window_;
				std::vector<pair_t*> cont_;
				unsigned long count_ref_;
			};
		}//namespace tooltip
	}//namespace drawerbase

	//class tooltip
		tooltip::tooltip()
		{
			typedef drawerbase::tooltip::controller ctrl;
			threads::lock_guard<threads::recursive_mutex> lock(ctrl::mutex());
			ctrl::object()->inc();
		}

		tooltip::~tooltip()
		{
			typedef drawerbase::tooltip::controller ctrl;
			threads::lock_guard<threads::recursive_mutex> lock(ctrl::mutex());

			if(0 == ctrl::object()->dec())
				ctrl::object(true);
		}

		void tooltip::set(nana::gui::window wnd, const nana::string& text)
		{
			if(false == API::empty_window(wnd))
			{
				typedef drawerbase::tooltip::controller ctrl;
				threads::lock_guard<threads::recursive_mutex> lock(ctrl::mutex());
				ctrl::object()->set(wnd, text);
			}
		}

		void tooltip::show(window wd, int x, int y, const nana::string& text)
		{
			typedef drawerbase::tooltip::controller ctrl;
			threads::lock_guard<threads::recursive_mutex> lock(ctrl::mutex());

			nana::point pos(x, y);
			API::calc_screen_point(wd, pos);
			ctrl::object()->show(pos.x, pos.y, text);
		}

		void tooltip::close()
		{
			typedef drawerbase::tooltip::controller ctrl;
			threads::lock_guard<threads::recursive_mutex> lock(ctrl::mutex());
			ctrl::object()->close();
		}
	//};//class tooltip

}//namespace gui
}//namespace nana

