/*
 *	Tray Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/tray.hpp
 *
 *	Tray is a class that is a right bottom area of taskbar abstraction.
 */

#ifndef NANA_GUI_WIDGETS_TRAY_HPP
#define NANA_GUI_WIDGETS_TRAY_HPP
#include <nana/gui/widgets/widget.hpp>
#include <nana/traits.hpp>

namespace nana{ namespace gui{
	class tray
		: nana::noncopyable
	{
		struct tray_impl;
	public:
		typedef std::function<void(const eventinfo&)> event_fn_t;
		tray();
		~tray();

		void bind(window);
		void unbind();

		bool add(const nana::char_t* tip, const nana::char_t* image) const;
		tray& tip(const char_t* text);
		tray & icon(const char_t * ico);

		template<typename Event>
		bool make_event(const event_fn_t & f) const
		{
			return _m_make_event(Event::identifier, f);
		}

		template<typename Event, typename Class, typename Concept>
		bool make_event(Class& obj, void (Concept::*memf)(const eventinfo&)) const
		{
			return _m_make_event(Event::identifier, make_fun(obj, memf));
		}
		void umake_event();
	private:
		bool _m_make_event(event_code, const event_fn_t&) const;
	private:
		struct tray_impl *impl_;
	};
}//end namespace gui
}//end namespace nana

#endif
