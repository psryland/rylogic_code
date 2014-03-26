/*
 *	Tray Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/tray.cpp
 *
 *	Tray is a class that is a right bottom area of taskbar abstraction.
 */

#include <nana/gui/widgets/tray.hpp>
#include GUI_BEDROCK_HPP

namespace nana{ namespace gui{
	//class tray
		struct tray::tray_impl
		{
			native_window_type wd;

			bool enabled;
			event_handle closed;

			void closed_helper(const eventinfo& ei)
			{
				API::tray_delete(wd);
				wd = nullptr;
			}
		};

		tray::tray()
			: impl_(new tray_impl)
		{
			impl_->wd = nullptr;
			impl_->enabled = false;
		}

		tray::~tray()
		{
			unbind();

			delete impl_;
		}

		void tray::bind(window wd)
		{
			if(nullptr == impl_->wd)
			{
				//The wd may not be the root category widget.
				//The destroy event needs the root category widget
				impl_->wd = API::root(wd);
				impl_->closed = API::make_event<events::destroy>(API::root(impl_->wd), std::bind(&tray_impl::closed_helper, impl_, std::placeholders::_1));
			}
		}

		void tray::unbind()
		{
			if(impl_->wd)
			{
				API::umake_event(impl_->closed);
				impl_->closed = nullptr;

				API::tray_delete(impl_->wd);
				impl_->wd = nullptr;
			}
		}

		bool tray::add(const nana::char_t* tip, const nana::char_t* image) const
		{
			if(impl_->wd)
				return (impl_->enabled = API::tray_insert(impl_->wd, tip, image));

			return false;
		}

		tray& tray::tip(const nana::char_t* text)
		{
			if(impl_->wd)
				API::tray_tip(impl_->wd, text);
			return *this;
		}

		tray & tray::icon(const char_t * ico)
		{
			if(impl_->wd)
				API::tray_icon(impl_->wd, ico);
			return *this;
		}

		void tray::umake_event()
		{
			if (impl_->wd)
				detail::bedrock::instance().wd_manager.tray_umake_event(impl_->wd);
		}

		bool tray::_m_make_event(event_code code, const event_fn_t& fn) const
		{
			return (impl_->wd
				? detail::bedrock::instance().wd_manager.tray_make_event(impl_->wd, code, fn) : false);
		}
	//end class tray
}//end namespace gui
}//end namespace nana
