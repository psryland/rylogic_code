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

namespace nana{ namespace gui{
	//class tray
		struct tray::tray_impl
		{
			native_window_type wd;

			bool enabled;
			event_handle closed;

			void closed_helper(const nana::gui::eventinfo& ei)
			{
				API::tray_delete(wd);
				wd = 0;
			}
		};

		tray::tray()
			: impl_(new tray_impl)
		{
			impl_->wd = 0;
			impl_->enabled = false;
		}

		tray::~tray()
		{
			unbind();

			delete impl_;
		}

		void tray::bind(window wd)
		{
			if(0 == impl_->wd)
			{
				impl_->wd = API::root(wd);
				
				//The parameter wd may not be the real root window.
				window root = API::root(impl_->wd); //the handle is guaranteed a root window.

				if(root)
				{
					impl_->closed = API::make_event<events::destroy>(root, nana::functor<void(const nana::gui::eventinfo&)>(*impl_, &tray_impl::closed_helper));
				}
			}
		}

		void tray::unbind()
		{
			if(impl_->wd)
			{
				if(impl_->closed)
				{
					API::umake_event(impl_->closed);
					impl_->closed = 0;
				}

				API::tray_delete(impl_->wd);
				impl_->wd = 0;
			}
		}

		bool tray::add(const nana::char_t* tip, const nana::char_t* image) const
		{
			if(impl_->wd)
			{
				return (impl_->enabled = API::tray_insert(impl_->wd, tip, image));
			}
			return false;
		}

		tray& tray::tip(const nana::char_t* text)
		{
			if(impl_->wd)
				API::tray_tip(impl_->wd, text);
			return *this;
		}

		tray & tray::icon(const nana::char_t * ico)
		{
			if(impl_->wd)
				API::tray_icon(impl_->wd, ico);
			return *this;
		}

		void tray::umake_event()
		{
			if(impl_->wd)
				API::tray_umake_event(impl_->wd);
		}

		bool tray::_m_make_event(unsigned identifier, const nana::functor<void(const eventinfo&)>& f) const
		{
			if(impl_->wd)
				return API::tray_make_event(impl_->wd, identifier, f);
			return false;
		}
	//end class tray
}//end namespace gui
}//end namespace nana
