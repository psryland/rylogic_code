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

#include <nana/gui/timer.hpp>

namespace nana
{
namespace gui
{
	//class timer::
		timer::timer()
			:empty_(true), interval_(1000)
		{
		}

		timer::~timer()
		{
			_m_kill_timer();
			_m_umake_event();
		}

		void timer::interval(unsigned value)
		{
			interval_ = value;
			if(!empty_)
				detail::timer_trigger::set_interval(this, interval_);
		}

		unsigned timer::interval() const
		{
			return interval_;
		}

		bool timer::empty() const
		{
			return empty_;
		}

		void timer::enable(bool value)
		{
			if(value)
				_m_set_timer();
			else
				_m_kill_timer();
		}

		/*
		 * _m_set_timer
		 * @brief: timer is a special control. this function will set a timer if empty == true
		 * this function is platform-dependent
		 */
		void timer::_m_set_timer()
		{
			if(empty_)
			{
				detail::timer_trigger::create_timer(this, interval_);
				empty_ = false;
			}
		}

		void timer::_m_kill_timer()
		{
			if(!empty_)
			{
				detail::timer_trigger::kill_timer(this);
				empty_ = true;
			}
		}

		void timer::_m_umake_event()
		{
			nana::gui::API::umake_event(reinterpret_cast<nana::gui::window>(this));
		}
	//end class timer
}//end namespace gui
}//end namespace nana
