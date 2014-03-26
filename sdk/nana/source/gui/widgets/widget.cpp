/*
 *	The fundamental widget class implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/widget.cpp
 */

#include <nana/gui/widgets/widget.hpp>
#include <nana/gui/tooltip.hpp>

namespace nana
{
namespace gui
{
	//class widget
	//@brief: The definition of class widget
		widget::~widget(){}

		nana::string widget::caption() const
		{
			return _m_caption();
		}
		
		void widget::caption(const nana::string& str)
		{
			_m_caption(str);
		}

		nana::gui::cursor::t widget::cursor() const
		{
			return _m_cursor();
		}

		void widget::cursor(nana::gui::cursor::t cur)
		{
			_m_cursor(cur);
		}

		void widget::typeface(const nana::paint::font& font)
		{
			_m_typeface(font);
		}

		nana::paint::font widget::typeface() const
		{
			return _m_typeface();
		}

		void widget::close()
		{
			_m_close();
		}

		window widget::parent() const
		{
			return API::get_parent_window(handle());
		}

		bool widget::enabled() const
		{
			return API::window_enabled(handle());
		}

		void widget::enabled(bool value)
		{
			_m_enabled(value);
		}

		bool widget::empty() const
		{
			return (0 == handle());
		}

		void widget::focus()
		{
			API::focus_window(handle());
		}

		bool widget::focused() const
		{
			return API::is_focus_window(handle());
		}

		void widget::show()
		{
			_m_show(true);
		}

		void widget::hide()
		{
			_m_show(false);
		}

		bool widget::visible() const
		{
			return _m_visible();
		}

		nana::size widget::size() const
		{
			return API::window_size(handle());
		}

		void widget::size(unsigned width, unsigned height)
		{
			_m_size(width, height);
		}

		nana::point widget::pos() const
		{
			return API::window_position(handle());
		}

		void widget::move(int x, int y)
		{
			_m_move(x, y);
		}

		void widget::move(int x, int y, unsigned width, unsigned height)
		{
			_m_move(x, y, width, height);
		}

		void widget::foreground(nana::color_t value)
		{
			_m_foreground(value);
		}

		nana::color_t widget::foreground() const
		{
			return _m_foreground();
		}

		void widget::background(nana::color_t value)
		{
			_m_background(value);
		}

		nana::color_t widget::background() const
		{
			return _m_background();
		}

		void widget::umake_event(event_handle eh) const
		{
			API::umake_event(eh);
		}

		widget& widget::tooltip(const nana::string& text)
		{
			nana::gui::tooltip::set(*this, text);
			return *this;
		}

		widget::operator widget::dummy_bool_type() const
		{
			return (handle()? dummy_bool_type(1):0);
		}

		widget::operator window() const
		{
			return handle();
		}

		void widget::_m_complete_creation()
		{}

		nana::string widget::_m_caption() const
		{
			return API::dev::window_caption(handle());
		}
		
		void widget::_m_caption(const nana::string& str)
		{
			API::dev::window_caption(handle(), str);
		}

		nana::gui::cursor::t widget::_m_cursor() const
		{
			return API::window_cursor(handle());
		}

		void widget::_m_cursor(nana::gui::cursor::t cur)
		{
			API::window_cursor(handle(), cur);
		}

		void widget::_m_close()
		{
			API::close_window(handle());
		}

		bool widget::_m_enabled() const
		{
			return API::window_enabled(handle());
		}

		void widget::_m_enabled(bool value)
		{
			API::window_enabled(handle(), value);
		}

		bool widget::_m_show(bool visible)
		{
			API::show_window(handle(), visible);
			return visible;
		}

		bool widget::_m_visible() const
		{
			return API::visible(handle());
		}

		void widget::_m_size(unsigned width, unsigned height)
		{
			API::window_size(handle(), width, height);
		}

		void widget::_m_move(int x, int y)
		{
			API::move_window(handle(), x, y);
		}

		void widget::_m_move(int x, int y, unsigned width, unsigned height)
		{
			API::move_window(handle(), x, y, width, height);
		}

		void widget::_m_typeface(const nana::paint::font& font)
		{
			API::typeface(handle(), font);
		}

		nana::paint::font widget::_m_typeface() const
		{
			return API::typeface(handle());
		}

		void widget::_m_foreground(nana::color_t value)
		{
			API::foreground(handle(), value);
		}

		nana::color_t widget::_m_foreground() const
		{
			return API::foreground(handle());
		}

		void widget::_m_background(nana::color_t value)
		{
			API::background(handle(), value);
		}

		nana::color_t widget::_m_background() const
		{
			return API::background(handle());
		}

	//end class widget

}//end namespace gui
}//end namespace nana

