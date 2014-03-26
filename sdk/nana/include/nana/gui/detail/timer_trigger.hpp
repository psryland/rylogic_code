/*
 *	A Timer Trigger Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/detail/timer_trigger.hpp
 *	@description:
 *		A timer trigger provides a construction and destruction of the timer and
 *	implemented the event callback trigger. The capacity of the event managment is implemented with
 *	event_manager
 */

#ifndef NANA_GUI_DETAIL_TIMER_TRIGGER_HPP
#define NANA_GUI_DETAIL_TIMER_TRIGGER_HPP

#include "event_manager.hpp"
#include <map>

namespace nana
{
namespace gui
{
namespace detail
{
	class timer_trigger
	{
	public:
		typedef void* timer_object;
		typedef unsigned* timer_handle;
		typedef std::map<timer_object, timer_handle> holder_timer_type;
		typedef std::map<timer_handle, timer_object> holder_handle_type;

		static void create_timer(timer_object timer, unsigned interval);
		static void kill_timer(timer_object timer);
		static void set_interval(timer_object timer, unsigned interval);
		static void fire(timer_object object);
		static timer_object* find_by_timer_handle(timer_handle h);
	private:
		static timer_handle* _m_find_by_timer_object(timer_object t);
		
	private:
		static std::recursive_mutex mutex_;
		static holder_timer_type holder_timer_;
		static holder_handle_type holder_handle_;
	};//end class timer_trigger

}//end namespace detail
}//end namespace gui
}//end namespace nana

#endif

