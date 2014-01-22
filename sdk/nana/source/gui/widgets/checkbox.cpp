/*
 *	A CheckBox Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/checkbox.cpp
 */

#include <nana/gui/widgets/checkbox.hpp>
#include <nana/paint/gadget.hpp>
#include <nana/paint/text_renderer.hpp>
#include <nana/gui/element.hpp>

namespace nana{	namespace gui{ namespace drawerbase
{
namespace checkbox
{
	typedef element::crook_interface::state crook_state;

	struct drawer::implement
	{
		bool react;
		bool radio;
		facade<element::crook> crook;
	};
		//class drawer
			drawer::drawer()
				:	widget_(0),
					impl_(new implement)
			{
				impl_->react = true;
				impl_->radio = false;
			}

			drawer::~drawer()
			{
				delete impl_;
			}

			void drawer::bind_window(widget_reference w)
			{
				widget_ = &w;
			}

			void drawer::attached(graph_reference)
			{
				window wd = *widget_;
				using namespace API::dev;
				make_drawer_event<events::mouse_down>(wd);
				make_drawer_event<events::mouse_up>(wd);

				make_drawer_event<events::mouse_enter>(wd);
				make_drawer_event<events::mouse_leave>(wd);
			}

			void drawer::detached()
			{
				API::dev::umake_drawer_event(*widget_);
			}

			void drawer::refresh(graph_reference graph)
			{
				_m_draw(graph);
			}

			void drawer::mouse_down(graph_reference graph, const eventinfo&)
			{
				_m_draw(graph);
			}

			void drawer::mouse_up(graph_reference graph, const eventinfo&)
			{
				if(impl_->react)
					impl_->crook.reverse();

				_m_draw(graph);
			}

			void drawer::mouse_enter(graph_reference graph, const eventinfo&)
			{
				_m_draw(graph);
			}

			void drawer::mouse_leave(graph_reference graph, const eventinfo&)
			{
				_m_draw(graph);
			}

			drawer::implement * drawer::impl() const
			{
				return impl_;
			}

			void drawer::_m_draw(drawer::graph_reference graph)
			{
				_m_draw_background(graph);
				_m_draw_title(graph);
				_m_draw_checkbox(graph, graph.text_extent_size(STR("jN"), 2).height + 2);
				API::lazy_refresh();
			}

			void drawer::_m_draw_background(graph_reference graph)
			{
				if(bground_mode::basic != API::effects_bground_mode(*widget_))
					graph.rectangle(API::background(*widget_), true);
			}

			void drawer::_m_draw_checkbox(graph_reference graph, unsigned first_line_height)
			{
				impl_->crook.draw(graph, widget_->background(), widget_->foreground(), rectangle(0, first_line_height > 16 ? (first_line_height - 16) / 2 : 0, 16, 16), API::element_state(*widget_));
			}

			void drawer::_m_draw_title(graph_reference graph)
			{
				if(graph.width() > 16 + interval)
				{
					nana::string title = widget_->caption();

					unsigned fgcolor = widget_->foreground();
					unsigned pixels = graph.width() - (16 + interval);

					nana::paint::text_renderer tr(graph);
					if(API::window_enabled(widget_->handle()) == false)
					{
						tr.render(17 + interval, 2, 0xFFFFFF, title.c_str(), title.length(), pixels);
						fgcolor = 0x808080;
					}

					tr.render(16 + interval, 1, fgcolor, title.c_str(), title.length(), pixels);
				}
			}
		//end class drawer
}//end namespace checkbox
}//end namespace drawerbase

	//class checkbox
		checkbox::checkbox(){}

		checkbox::checkbox(window wd, bool visible)
		{
			create(wd, rectangle(), visible);
		}

		checkbox::checkbox(window wd, const nana::string& text, bool visible)
		{
			create(wd, rectangle(), visible);
			caption(text);
		}

		checkbox::checkbox(window wd, const nana::char_t* text, bool visible)
		{
			create(wd, rectangle(), visible);
			caption(text);
		}

		checkbox::checkbox(window wd, const rectangle& r, bool visible)
		{
			create(wd, r, visible);
		}

		void checkbox::element_set(const char* name)
		{
			get_drawer_trigger().impl()->crook.switch_to(name);
		}

		void checkbox::react(bool want)
		{
			get_drawer_trigger().impl()->react = want;
		}

		bool checkbox::checked() const
		{
			return (get_drawer_trigger().impl()->crook.checked() != drawerbase::checkbox::crook_state::unchecked);
		}

		void checkbox::check(bool chk)
		{
			typedef drawerbase::checkbox::crook_state crook_state;
			get_drawer_trigger().impl()->crook.check(chk ? crook_state::checked : crook_state::unchecked);
			API::refresh_window(handle());
		}

		void checkbox::radio(bool is_radio)
		{
			get_drawer_trigger().impl()->crook.radio(is_radio);
		}

		void checkbox::transparent(bool enabled)
		{
			if(enabled)
				API::effects_bground(*this, effects::bground_transparent(0), 0.0);
			else
				API::effects_bground_remove(*this);
		}

		bool checkbox::transparent() const
		{
			return (bground_mode::basic == API::effects_bground_mode(*this));
		}
	//end class checkbox

	//class radio_group
		radio_group::~radio_group()
		{
			for(std::vector<element_tag>::iterator i = ui_container_.begin(); i != ui_container_.end(); ++i)
			{
				nana::gui::API::umake_event(i->eh_checked);
				nana::gui::API::umake_event(i->eh_destroy);
			}
		}

		void radio_group::add(checkbox& uiobj)
		{
			uiobj.radio(true);
			uiobj.check(false);
			uiobj.react(false);

			element_tag el;

			el.uiobj = &uiobj;
			el.eh_checked = uiobj.make_event<events::click>(*this, &radio_group::_m_checked);
			el.eh_destroy = uiobj.make_event<events::destroy>(*this, &radio_group::_m_destroy);

			ui_container_.push_back(el);
		}

		std::size_t radio_group::checked() const
		{
			for(std::vector<element_tag>::const_iterator i = ui_container_.begin(); i != ui_container_.end(); ++i)
			{
				if(i->uiobj->checked())
					return static_cast<size_t>(i - ui_container_.begin());
			}
			return 0;
		}

		void radio_group::_m_checked(const nana::gui::eventinfo& ei)
		{
			for(std::vector<element_tag>::iterator i = ui_container_.begin(); i != ui_container_.end(); ++i)
				i->uiobj->check(ei.window == i->uiobj->handle());
		}

		void radio_group::_m_destroy(const nana::gui::eventinfo& ei)
		{
			for(std::vector<element_tag>::iterator i = ui_container_.begin(); i != ui_container_.end(); ++i)
			{
				if(ei.window == i->uiobj->handle())
				{
					ui_container_.erase(i);
					return;
				}
			}
		}
	//end class radio_group
}//end namespace gui
}//end namespace nana
