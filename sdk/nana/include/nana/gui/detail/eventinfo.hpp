/*
 *	An Event Info Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/detail/eventinfo.hpp
 *
 *	@description:
 *	eventinfo is a struct with kinds of event parameter 
 */

#ifndef NANA_GUI_DETAIL_EVENTINFO_HPP
#define NANA_GUI_DETAIL_EVENTINFO_HPP
#include "../basis.hpp"
#include <vector>

namespace nana
{
namespace gui
{
	enum class event_code
	{
		click,
		dbl_click,
		mouse_enter,
		mouse_move,
		mouse_leave,
		mouse_down,
		mouse_up,
		mouse_wheel,
		mouse_drop,
		expose,
		sizing, size,
		move,
		unload,
		destroy,
		focus,
		key_down,
		key_char,
		key_up,
		shortkey,

		//Unoperational events
		elapse,

		//End indicator, it's not an event.
		end
	};

	namespace detail
	{
		struct tag_mouse
		{
			short x;
			short y;

			bool left_button;
			bool mid_button;
			bool right_button;
			bool shift;
			bool ctrl;
		};

		struct tag_keyboard
		{
			mutable nana::char_t key;
			mutable bool ignore;
			unsigned char ctrl;
		};

		struct tag_wheel
		{
			short x;
			short y;
			bool upwards;
			bool shift;
			bool ctrl;
		};

		struct tag_dropinfo
		{
			std::vector<nana::string> filenames;
			nana::point pos;
		};
	}//end namespace detail

	//eventinfo
	//@brief:
	struct eventinfo
	{
		event_code identifier;	//for identifying what event is
		gui::window window;		//which window the event triggered on

		union
		{
			bool exposed;
			detail::tag_mouse	mouse;
			detail::tag_wheel	wheel;
			detail::tag_keyboard	keyboard;
			detail::tag_dropinfo	*dropinfo;

			struct
			{
				int x;
				int y;
			}move;

			struct
			{
				window_border border;
				mutable unsigned width;
				mutable unsigned height;
			}sizing;

			struct
			{
				unsigned width;
				unsigned height;
			}size;

			struct
			{
				mutable bool cancel;
			}unload;

			struct
			{
				bool getting;
				native_window_type receiver;
			}focus;

			struct
			{
				void* timer;
			}elapse;
		};
	};

	namespace detail
	{
		struct check
		{
			inline static bool accept(event_code evtid, category::flags categ)
			{
				return true;
				//return (evtid < event_name::end) && ((static_cast<int>(event_category[evtid]) & static_cast<int>(categ)) == static_cast<int>(event_category[evtid]));
			}

			static category::flags event_category[static_cast<int>(event_code::end)];
		};

		struct event_type_tag{};

		template<event_code Event>
		struct basic_event
			:public event_type_tag
		{
			const static event_code identifier = Event;
		};
	}//end namespace detail

	namespace events
	{
		typedef detail::basic_event<event_code::click>		click;
		typedef detail::basic_event<event_code::dbl_click>	dbl_click;

		typedef detail::basic_event<event_code::mouse_enter>	mouse_enter;
		typedef detail::basic_event<event_code::mouse_move>		mouse_move;
		typedef detail::basic_event<event_code::mouse_leave>	mouse_leave;
		typedef detail::basic_event<event_code::mouse_down>		mouse_down;
		typedef detail::basic_event<event_code::mouse_up>		mouse_up;
		typedef detail::basic_event<event_code::mouse_wheel>	mouse_wheel;
		typedef detail::basic_event<event_code::mouse_drop>		mouse_drop;

		typedef detail::basic_event<event_code::expose>		expose;
		typedef detail::basic_event<event_code::sizing>		sizing;
		typedef detail::basic_event<event_code::size>		size;
		typedef detail::basic_event<event_code::move>		move;
		typedef detail::basic_event<event_code::unload>		unload;
		typedef detail::basic_event<event_code::destroy>	destroy;
		typedef detail::basic_event<event_code::focus>		focus;
		typedef detail::basic_event<event_code::key_down>	key_down;
		typedef detail::basic_event<event_code::key_char>	key_char;
		typedef detail::basic_event<event_code::key_up>		key_up;
		typedef detail::basic_event<event_code::shortkey>	shortkey;
	};//end struct events
}//end namespace gui
}//end namespace nana
#endif
