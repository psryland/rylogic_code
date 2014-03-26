/*
 *	A Timer Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/timer.hpp
 *	@description:
 *		A timer can repeatedly call a piece of code. The duration between 
 *	calls is specified in milliseconds. Timer is defferent from other graphics
 *	controls, it has no graphics interface.
 */

#ifndef NANA_GUI_TIMER_HPP
#define NANA_GUI_TIMER_HPP
#include "programming_interface.hpp"
#include "detail/timer_trigger.hpp"

namespace nana
{
namespace gui
{
	class timer
	{
	public:
		timer();

		~timer();

		bool empty() const;
		void enable(bool);

		template<typename Function>
		void make_tick(Function f)
		{
			nana::gui::API::make_event<detail::basic_event<event_code::elapse> >(reinterpret_cast<nana::gui::window>(this), f);
			this->_m_set_timer();
		}

		void interval(unsigned value);
		unsigned interval() const;
	private:
		//_m_set_timer
		//@brief: timer is a special control. this function will set a timer if empty == true
		//this function is platform-dependent
		void _m_set_timer();
		void _m_kill_timer();
		void _m_umake_event();
	private:
		bool empty_;
		unsigned interval_;
	};
}//end namespace gui
}//end namespace nana

#endif

