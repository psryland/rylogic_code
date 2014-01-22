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
		unsigned identifier;	//for identifying what event is
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
				window_border::t border;
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
		struct event_tag
		{
			enum
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
				count
			};

			
			inline static bool accept(unsigned event_id, unsigned category)
			{
				return (event_id < event_tag::count) && ((event_category[event_id] & category) == event_category[event_id]);
			}

			static unsigned event_category[event_tag::count];
		};

		struct event_type_tag{};

		template<unsigned Event>
		struct event_template
			:public event_type_tag
		{
			enum{	identifier = Event};
		};
	}//end namespace detail

	namespace events
	{
		typedef detail::event_template<detail::event_tag::click>		click;
		typedef detail::event_template<detail::event_tag::dbl_click>	dbl_click;
		typedef detail::event_template<detail::event_tag::mouse_enter>	mouse_enter;
		typedef detail::event_template<detail::event_tag::mouse_move>	mouse_move;
		typedef detail::event_template<detail::event_tag::mouse_leave>	mouse_leave;
		typedef detail::event_template<detail::event_tag::mouse_down>	mouse_down;
		typedef detail::event_template<detail::event_tag::mouse_up>		mouse_up;
		typedef detail::event_template<detail::event_tag::mouse_wheel>	mouse_wheel;
		typedef detail::event_template<detail::event_tag::mouse_drop>	mouse_drop;

		typedef detail::event_template<detail::event_tag::expose>		expose;
		typedef detail::event_template<detail::event_tag::sizing>		sizing;
		typedef detail::event_template<detail::event_tag::size>			size;
		typedef detail::event_template<detail::event_tag::move>			move;
		typedef detail::event_template<detail::event_tag::unload>		unload;
		typedef detail::event_template<detail::event_tag::destroy>		destroy;
		typedef detail::event_template<detail::event_tag::focus>		focus;
		typedef detail::event_template<detail::event_tag::key_down>		key_down;
		typedef detail::event_template<detail::event_tag::key_char>		key_char;
		typedef detail::event_template<detail::event_tag::key_up>		key_up;
		typedef detail::event_template<detail::event_tag::shortkey>		shortkey;
	};//end struct events
}//end namespace gui
}//end namespace nana
#endif
