/*
 *	Platform Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/detail/native_window_interface.hpp
 */

#ifndef NANA_GUI_DETAIL_NATIVE_WINDOW_INTERFACE_HPP
#define NANA_GUI_DETAIL_NATIVE_WINDOW_INTERFACE_HPP

#include "../basis.hpp"
#include <nana/paint/image.hpp>

namespace nana
{
namespace gui
{
namespace detail
{

	struct native_interface
	{
		struct window_result
		{
			nana::gui::native_window_type handle;

			unsigned width;		//client size
			unsigned height;	//client size

			unsigned extra_width;	//extra border size, it is useful in Windows, ignore in X11 always 0
			unsigned extra_height;	//extra border size, it is useful in Windows, ignore in X11 always 0
		};

		static nana::size	screen_size();
		static rectangle screen_area_from_point(const point&);
		static window_result create_window(native_window_type, bool nested, const rectangle&, const appearance&);
		static native_window_type create_child_window(native_window_type, const rectangle&);

#if defined(NANA_X11)
		static void set_modal(native_window_type);
#endif
		static void enable_window(native_window_type, bool is_enabled);
		static bool window_icon(native_window_type, const paint::image&);
		static void activate_owner(native_window_type);
		static void activate_window(native_window_type);
		static void close_window(native_window_type);
		static void show_window(native_window_type, bool show, bool active);
		static void restore_window(native_window_type);
		static void zoom_window(native_window_type, bool ask_for_max);
		static void	refresh_window(native_window_type);
		static bool is_window(native_window_type);
		static bool	is_window_visible(native_window_type);
		static bool is_window_zoomed(native_window_type, bool ask_for_max);

		static nana::point	window_position(native_window_type);
		static void	move_window(native_window_type, int x, int y);
		static void	move_window(native_window_type, int x, int y, unsigned width, unsigned height);
		static void bring_to_top(native_window_type);
		static void	set_window_z_order(native_window_type, native_window_type wd_after, z_order_action action_if_no_wd_after);

		static void	window_size(native_window_type, unsigned width, unsigned height);
		static void	get_window_rect(native_window_type, rectangle&);
		static void	window_caption(native_window_type, const nana::string&);
		static nana::string	window_caption(native_window_type);
		static void	capture_window(native_window_type, bool);
		static nana::point	cursor_position();
		static native_window_type get_owner_window(native_window_type);
		//For Caret
		static void	caret_create(native_window_type, unsigned width, unsigned height);
		static void caret_destroy(native_window_type);
		static void	caret_pos(native_window_type, int x, int y);
		static void caret_visible(native_window_type, bool);

		static bool notify_icon_add(native_window_type, const char_t* tip, const char_t* ico);
		static bool notify_icon_delete(native_window_type);
		static void notify_tip(native_window_type, const char_t* text);
		static void notify_icon(native_window_type, const char_t* icon);

		static void	set_focus(native_window_type);
		static native_window_type get_focus_window();
		static bool calc_screen_point(native_window_type, nana::point&);
		static bool calc_window_point(native_window_type, nana::point&);

		static native_window_type find_window(int x, int y);
		static nana::size check_track_size(nana::size sz, unsigned extra_width, unsigned extra_height, bool true_for_max);
	};


}//end namespace detail
}//end namespace gui
}//end namespace nana
#endif
