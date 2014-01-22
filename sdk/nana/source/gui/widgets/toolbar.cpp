/*
 *	A Toolbar Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/toolbar.cpp
 */

#include <nana/gui/widgets/toolbar.hpp>
#include <vector>
#include <stdexcept>
#include <nana/gui/tooltip.hpp>

namespace nana{ namespace gui{
	namespace drawerbase
	{
		namespace toolbar
		{
			struct listitem
			{
				nana::string text;
				nana::paint::image image;
				bool enable;
			};

			struct item_type
			{
				enum{TypeButton, TypeContainer};

				typedef std::size_t size_type;

				nana::string text;
				nana::paint::image image;
				unsigned	pixels;
				nana::size	textsize;
				bool		enable;
				nana::gui::window other;

				int type;
				nana::functor<void(size_type, size_type)> answer;
				std::vector<listitem> children;

				item_type(const nana::string& text, const nana::paint::image& img, int type)
					:text(text), image(img), pixels(0), enable(true), other(0), type(type)
				{}
			};

			class container
			{
				container(const container&);
				container& operator=(const container&);
			public:
				typedef std::vector<item_type*>::size_type size_type;
				typedef std::vector<item_type*>::iterator iterator;
				typedef std::vector<item_type*>::const_iterator const_iterator;

				container()
				{}

				~container()
				{
					for(iterator i = cont_.begin(); i != cont_.end(); ++i)
						delete *i;
				}

				void insert(size_type pos, const nana::string& text, const nana::paint::image& img, int type)
				{
					item_type* m = new item_type(text, img, type);

					if(pos < cont_.size())
						cont_.insert(cont_.begin() + pos, m);
					else
						cont_.push_back(m);
				}

				void push_back(const nana::string& text, const nana::paint::image& img)
				{
					insert(cont_.size(), text, img, item_type::TypeButton);
				}

				void push_back(const nana::string& text)
				{
					insert(cont_.size(), text, nana::paint::image(), item_type::TypeButton);
				}

				void insert(size_type pos)
				{
					if(pos < cont_.size())
						cont_.insert(cont_.begin() + pos, static_cast<item_type*>(0)); //both works in C++0x and C++2003
					else
						cont_.push_back(0);
				}

				void push_back()
				{
					cont_.push_back(0);
				}

				size_type size() const
				{
					return cont_.size();
				}

				item_type* at(size_type n)
				{
					if(n < cont_.size())
						return cont_[n];

					throw std::out_of_range("toolbar: bad index!");
				}

				iterator begin()
				{
					return cont_.begin();
				}

				iterator end()
				{
					return cont_.end();
				}

				const_iterator begin() const
				{
					return cont_.begin();
				}

				const_iterator end() const
				{
					return cont_.end();
				}
			private:
				std::vector<item_type*> cont_;
			};

			class item_renderer
			{
			public:
				enum{StateNormal, StateHighlight, StateSelected};
				const static unsigned extra_size = 6;

				item_renderer(nana::paint::graphics& graph, bool textout, unsigned scale, nana::color_t color)
					:graph(graph), textout(textout), scale(scale), color(color)
				{}

				void operator()(int x, int y, unsigned width, unsigned height, item_type& item, int state)
				{
					//draw background
					if(state != StateNormal)
						graph.rectangle(x, y, width, height, 0x3399FF, false);
					switch(state)
					{
					case StateHighlight:
						graph.shadow_rectangle(x + 1, y + 1, width - 2, height - 2, color, /*graph.mix(color, 0xC0DDFC, 0.5)*/ 0xC0DDFC, true);
						break;
					case StateSelected:
						graph.shadow_rectangle(x + 1, y + 1, width - 2, height - 2, color, /*graph.mix(color, 0x99CCFF, 0.5)*/0x99CCFF, true);
					default: break;
					}

					if(item.image.empty() == false)
					{
						nana::size size = item.image.size();
						if(size.width > scale) size.width = scale;
						if(size.height > scale) size.height = scale;

						nana::point pos(x, y);
						pos.x += static_cast<int>(scale + extra_size - size.width) / 2;
						pos.y += static_cast<int>(height - size.height) / 2;

						item.image.paste(size, graph, pos);
						if(item.enable == false)
						{
							nana::paint::graphics gh(size.width, size.height);
							gh.bitblt(size, graph, pos);
							gh.rgb_to_wb();
							gh.paste(graph, pos.x, pos.y);
						}
						else if(state == StateNormal)
						{
							graph.blend(nana::rectangle(pos, size), graph.mix(color, 0xC0DDFC, 0.5), 0.25);
						}

						x += scale;
						width -= scale;
					}

					if(textout)
					{
						graph.string(x + (width - item.textsize.width) / 2, y + (height - item.textsize.height) / 2, 0x0, item.text);
					}
				}

			protected:
				nana::paint::graphics& graph;
				bool textout;
				unsigned scale;
				nana::color_t color;
			};

			struct drawer::drawer_impl_type
			{
				unsigned scale;
				bool textout;
				size_type which;
				int state;

				container cont;
				nana::gui::tooltip tooltip;


				drawer_impl_type()
					:scale(16), textout(false), which(npos), state(item_renderer::StateNormal)
				{}
			};

			//class drawer
				drawer::drawer()
					: impl_(new drawer_impl_type)
				{
				}

				drawer::~drawer()
				{
					delete impl_;
					impl_ = 0;
				}

				void drawer::append(const nana::string& text, const nana::paint::image& img)
				{
					impl_->cont.push_back(text, img);
				}

				void drawer::append()
				{
					impl_->cont.push_back();
				}

				bool drawer::enable(drawer::size_type n) const
				{
					if(impl_->cont.size() > n)
					{
						item_type * item = *(impl_->cont.begin() + n);
						return (item && item->enable);
					}
					return false;
				}

				bool drawer::enable(drawer::size_type n, bool eb)
				{
					if(impl_->cont.size() > n)
					{
						item_type * item = *(impl_->cont.begin() + n);
						if(item && item->enable != eb)
						{
							item->enable = eb;
							return true;
						}
					}
					return false;
				}

				void drawer::scale(unsigned s)
				{
					impl_->scale = s;

					for(container::iterator i = impl_->cont.begin(); i != impl_->cont.end(); ++i)
					{
						_m_fill_pixels(*i, true);
					}
				}

				void drawer::bind_window(drawer::widget_reference widget)
				{
					widget_ = & widget;
					widget.caption(STR("Nana Toolbar"));
					nana::gui::API::make_event<nana::gui::events::size>(widget.parent(), nana::functor<void(const nana::gui::eventinfo&)>(*this, &drawer::_m_owner_sized));
				}

				void drawer::refresh(drawer::graph_reference)
				{
					_m_draw();
				}

				void drawer::attached(drawer::graph_reference graph)
				{
					graph_ = &graph;
					using namespace API::dev;
					make_drawer_event<nana::gui::events::mouse_move>(widget_->handle());
					make_drawer_event<nana::gui::events::mouse_leave>(widget_->handle());
					make_drawer_event<nana::gui::events::mouse_down>(widget_->handle());
					make_drawer_event<nana::gui::events::mouse_up>(widget_->handle());
				}

				void drawer::detached()
				{
					API::dev::umake_drawer_event(widget_->handle());
				}

				void drawer::mouse_move(drawer::graph_reference graph, const nana::gui::eventinfo& ei)
				{
					if(ei.mouse.left_button == false)
					{
						size_type which = _m_which(ei.mouse.x, ei.mouse.y, true);
						if(impl_->which != which)
						{
							if(impl_->which != npos && (*(impl_->cont.begin() + impl_->which))->enable)
								ext_event.leave(*static_cast<nana::gui::toolbar*>(widget_), impl_->which);

							impl_->which = which;

							if(which == npos || (*(impl_->cont.begin() + which))->enable)
							{
								impl_->state = (ei.mouse.left_button ? item_renderer::StateSelected : item_renderer::StateHighlight);

								_m_draw();
								nana::gui::API::lazy_refresh();

								if(impl_->state == item_renderer::StateHighlight)
									ext_event.enter(*static_cast<nana::gui::toolbar*>(widget_), which);
							}

							if(which != npos)
								impl_->tooltip.show(widget_->handle(), ei.mouse.x, ei.mouse.y + 20, (*(impl_->cont.begin() + which))->text);
							else
								impl_->tooltip.close();
						}
					}
				}

				void drawer::mouse_leave(drawer::graph_reference, const nana::gui::eventinfo&)
				{
					if(impl_->which != npos)
					{
						size_type which = impl_->which;

						impl_->which = npos;
						_m_draw();
						nana::gui::API::lazy_refresh();

						if(which != npos && (*(impl_->cont.begin() + which))->enable)
							ext_event.leave(*static_cast<nana::gui::toolbar*>(widget_), which);
					}
					impl_->tooltip.close();
				}

				void drawer::mouse_down(drawer::graph_reference, const nana::gui::eventinfo&)
				{
					impl_->tooltip.close();
					if(impl_->which != npos && (impl_->cont.at(impl_->which)->enable))
					{
						impl_->state = item_renderer::StateSelected;
						_m_draw();
						nana::gui::API::lazy_refresh();
					}
				}

				void drawer::mouse_up(drawer::graph_reference, const nana::gui::eventinfo& ei)
				{
					if(impl_->which != npos)
					{
						size_type which = _m_which(ei.mouse.x, ei.mouse.y, false);
						if(impl_->which == which)
						{
							ext_event.selected(*static_cast<nana::gui::toolbar*>(widget_), which);

							impl_->state = item_renderer::StateHighlight;
						}
						else
						{
							impl_->which = which;
							impl_->state = (which == npos ? item_renderer::StateNormal : item_renderer::StateHighlight);
						}

						_m_draw();
						nana::gui::API::lazy_refresh();
					}
				}

				drawer::size_type drawer::_m_which(int x, int y, bool want_if_disabled) const
				{
					if(x < 2 || y < 2 || y >= static_cast<int>(impl_->scale + item_renderer::extra_size + 2)) return npos;

					x -= 2;

					size_type pos = 0;
					for(container::const_iterator i = impl_->cont.begin(); i != impl_->cont.end(); ++i, ++pos)
					{
						bool compart = (*i == 0);

						if(x < static_cast<int>(compart ? 3 : (*i)->pixels))
							return ((compart || ((*i)->enable == false && want_if_disabled == false)) ? npos : pos);

						x -= (compart ? 3 : (*i)->pixels);
					}
					return npos;
				}

				void drawer::_m_draw_background(nana::color_t color)
				{
					graph_->shadow_rectangle(0, 0, graph_->width(), graph_->height(), graph_->mix(color, 0xFFFFFF, 0.9), graph_->mix(color, 0x0, 0.95), true);
				}

				void drawer::_m_draw()
				{
					int x = 2, y = 2;

					unsigned color = nana::gui::API::background(widget_->handle());
					_m_draw_background(color);

					item_renderer ir(*graph_, impl_->textout, impl_->scale, color);
					size_type index = 0;
					for(container::iterator i = impl_->cont.begin(); i != impl_->cont.end(); ++i, ++index)
					{
						item_type * item = *i;
						if(item)
						{
							_m_fill_pixels(item, false);

							ir(x, y, item->pixels, impl_->scale + ir.extra_size, *item, (index == impl_->which ? impl_->state : item_renderer::StateNormal));

							x += item->pixels;
						}
						else
						{
							graph_->line(x + 2, y + 2, x + 2, y + impl_->scale + ir.extra_size - 4, 0x808080);
							x += 6;
						}
					}
				}

				void drawer::_m_owner_sized(const nana::gui::eventinfo& ei)
				{
					nana::size s = widget_->size();
					nana::gui::API::window_size(widget_->handle(), ei.size.width, s.height);
					_m_draw();
					nana::gui::API::update_window(widget_->handle());
				}

				void drawer::_m_fill_pixels(item_type* item, bool force)
				{
					if(item && (force || 0 == item->pixels))
					{
						if(item->text.size())
							item->textsize = graph_->text_extent_size(item->text);

						if(item->image.empty() == false)
							item->pixels = impl_->scale + item_renderer::extra_size;

						if(item->textsize.width && impl_->textout)
							item->pixels += item->textsize.width + 8;
					}
				}
			//};//class drawer

		}//end namespace toolbar
	}//end namespace drawerbase

	//class toolbar
	//	: public nana::gui::widget_object<category::widget_tag, drawerbase::toolbar::drawer>
	//{
	//public:
	//	typedef std::size_t size_type;
	//	typedef nana::functor<void(size_type)> functor;

		toolbar::toolbar()
		{}

		toolbar::toolbar(window wd, bool visible)
		{
			create(wd, rectangle(), visible);
		}

		toolbar::toolbar(window wd, const rectangle& r, bool visible)
		{
			create(wd, r, visible);
		}

		toolbar::ext_event_type& toolbar::ext_event() const
		{
			return get_drawer_trigger().ext_event;
		}

		void toolbar::append()
		{
			get_drawer_trigger().append();
			API::refresh_window(this->handle());
		}

		void toolbar::append(const nana::string& text, const nana::paint::image& img)
		{
			get_drawer_trigger().append(text, img);
			API::refresh_window(this->handle());
		}

		void toolbar::append(const nana::string& text)
		{
			get_drawer_trigger().append(text, nana::paint::image());
			API::refresh_window(this->handle());
		}

		bool toolbar::enable(size_type n) const
		{
			return get_drawer_trigger().enable(n);
		}

		void toolbar::enable(size_type n, bool eb)
		{
			if(get_drawer_trigger().enable(n, eb))
				API::refresh_window(this->handle());
		}

		void toolbar::scale(unsigned s)
		{
			get_drawer_trigger().scale(s);
			API::refresh_window(this->handle());
		}
	//}; class toolbar
}//end namespace gui
}//end namespace nana


