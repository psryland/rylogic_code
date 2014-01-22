/*
 *	A Timer Trigger Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/detail/timer_trigger.cpp
 *	@description:
 *		A timer trigger provides a construction and destruction of the timer and
 *	implemented the event callback trigger. The capacity of event managment is implemented with
 *	event_manager
 */
#include <nana/config.hpp>
#include GUI_BEDROCK_HPP
#include <nana/gui/detail/timer_trigger.hpp>

#if defined(NANA_WINDOWS)
	#include <windows.h>
#elif defined(NANA_LINUX)
	#include PLATFORM_SPEC_HPP
	#include <nana/system/platform.hpp>
	#include <iostream>
#endif

namespace nana
{
namespace gui
{
namespace detail
{

#if defined(NANA_WINDOWS)
	void __stdcall timer_trigger_proc(HWND hwnd, UINT uMsg, UINT_PTR idEvent, DWORD dwTime);
#elif defined(NANA_LINUX)
	void timer_trigger_proc(std::size_t id);
#endif
	//class timer_trigger
		nana::threads::recursive_mutex  timer_trigger::mutex_;
		timer_trigger::holder_timer_type	timer_trigger::holder_timer_;
		timer_trigger::holder_handle_type	timer_trigger::holder_handle_;

		void timer_trigger::create_timer(timer_object timer, unsigned interval)
		{
			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(mutex_);

			if(_m_find_by_timer_object(timer) == 0)
			{
#if defined(NANA_WINDOWS)
				timer_handle handle = reinterpret_cast<timer_handle>(::SetTimer(0, 0, interval, timer_trigger_proc));
#elif defined(NANA_LINUX)
				timer_handle handle = reinterpret_cast<timer_handle>(timer);
				nana::detail::platform_spec::instance().set_timer(reinterpret_cast<std::size_t>(timer), interval, timer_trigger_proc);
#endif
				holder_timer_[timer] = handle;
				holder_handle_[handle] = timer;
			}
		}

		void timer_trigger::kill_timer(timer_object timer)
		{
			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(mutex_);

			timer_handle* ptr = _m_find_by_timer_object(timer);
			if(ptr)
			{
#if defined(NANA_WINDOWS)
				::KillTimer(0, UINT_PTR(*ptr));
#elif defined(NANA_LINUX)
				nana::detail::platform_spec::instance().kill_timer(reinterpret_cast<std::size_t>(*ptr));
#endif
				holder_timer_.erase(timer);
				holder_handle_.erase(*ptr);
			}
		}

		void timer_trigger::set_interval(timer_object timer, unsigned interval)
		{
			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(mutex_);

			timer_handle* old = _m_find_by_timer_object(timer);
			if(old)
			{
#if defined(NANA_WINDOWS)
				::KillTimer(0, UINT_PTR(*old));
				holder_handle_.erase(*old);
				timer_handle handle = reinterpret_cast<timer_handle>(::SetTimer(0, 0, interval, timer_trigger_proc));
#elif defined(NANA_LINUX)
				timer_handle handle = reinterpret_cast<timer_handle>(timer);
				nana::detail::platform_spec::instance().set_timer(reinterpret_cast<std::size_t>(timer), interval, timer_trigger_proc);
#endif
				holder_timer_[timer] = handle;
				holder_handle_[handle] = timer;
			}
		}

		void timer_trigger::fire(timer_object object)
		{
			eventinfo ei;
			ei.elapse.timer = object;
			nana::gui::detail::bedrock& bedrock = nana::gui::detail::bedrock::instance();
			bedrock.evt_manager.answer(detail::event_tag::elapse, reinterpret_cast<nana::gui::window>(object), ei, event_manager::event_kind::user);
		}

		timer_trigger::timer_handle* timer_trigger::_m_find_by_timer_object(timer_object t)
		{
			std::map<timer_object, timer_handle>::iterator it = holder_timer_.find(t);
			if(it != holder_timer_.end())
				return &(it->second);

			return 0;
		}

		timer_trigger::timer_object* timer_trigger::find_by_timer_handle(timer_handle h)
		{
			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(mutex_);

			std::map<timer_handle, timer_object>::iterator it = holder_handle_.find(h);
			if(it != holder_handle_.end())
				return &(it->second);

			return 0;
		}

	//end class timer_trigger

#if defined(NANA_WINDOWS)
	void __stdcall timer_trigger_proc(HWND hwnd, UINT uMsg, UINT_PTR id, DWORD dwTime)
#elif defined(NANA_LINUX)
	void timer_trigger_proc(std::size_t id)
#endif
	{
		timer_trigger::timer_object* ptr = timer_trigger::find_by_timer_handle(reinterpret_cast<timer_trigger::timer_handle>(id));
		if(ptr)
		{
			timer_trigger::fire(*ptr);
		}
	}
}//end namespace detail
}//end namespace gui
}//end namespace nana
