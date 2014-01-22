/*
 *	Window Manager Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/detail/window_manager.cpp
 *
 */

#include <nana/gui/detail/window_manager.hpp>
#include <stdexcept>

namespace nana
{
namespace gui
{
namespace detail
{

	//class signal_manager

		//begin struct inner_invoker
			signal_manager::inner_invoker::~inner_invoker(){}
		//end struct inner_invoker

		signal_manager::~signal_manager()
		{
			manager_.clear();
		}

		void signal_manager::umake(identifier id)
		{
			if(id == 0) return;
			manager_.erase(id);
			end_ = manager_.end();
		}

		void signal_manager::fireaway(signal_manager::identifier wd, int message, const signals& info)
		{
			std::map<identifier, inner_invoker*>::iterator i = manager_.find(wd);
			if(i != manager_.end())
			{
				inner_invoker* & invk = i->second;
				if(invk)	invk->fireaway(message, info);
			}
		}
	//end class signal_manager


	//class shortkey_container::
		bool shortkey_container::make(window wd, unsigned long key)
		{
			if(wd == 0) return false;
			if(key < 0x61) key += (0x61 - 0x41);

			for(std::vector<item_type>::iterator i = keybase_.begin(); i != keybase_.end(); ++i)
				if(i->handle == wd)
				{
					i->keys.push_back(key);
					return true;
				}

			item_type m;
			m.handle = wd;
			m.keys.push_back(key);
			keybase_.push_back(m);
			return true;
		}

		void shortkey_container::clear()
		{
			keybase_.clear();
		}

		void shortkey_container::umake(window wd)
		{
			if(wd == 0) return;
			for(std::vector<item_type>::iterator i = keybase_.begin(); i != keybase_.end(); ++i)
				if(i->handle == wd)
				{
					keybase_.erase(i);
					return;
				}
		}

		window shortkey_container::find(unsigned long key) const
		{
			if(key < 0x61) key += (0x61 - 0x41);

			for(std::vector<item_type>::const_iterator i = keybase_.begin(); i != keybase_.end(); ++i)
			{
				const item_type & m = *i;
				for(std::vector<unsigned long>::const_iterator u = m.keys.begin(); u != m.keys.end(); ++u)
					if(key == *u)
						return m.handle;
			}
			return 0;
		}
	//end class shortkey_container


	//class tray_event_manager
		void tray_event_manager::fire(native_window_type wd, unsigned identifier, const eventinfo& ei)
		{
			maptable_type::const_iterator i = maptable_.find(wd);
			if(i == maptable_.end()) return;
			
			event_maptable_type::const_iterator u = i->second.find(identifier);
			if(u == i->second.end()) return;
			
			const fvec_t & fvec = u->second;
			for(fvec_t::const_iterator j = fvec.begin(); j != fvec.end(); ++j)
				(*j)(ei);
		}

		bool tray_event_manager::make(native_window_type wd, unsigned identifier, const nana::functor<void(const eventinfo&)> & f)
		{
			if(wd)
			{
				maptable_[wd][identifier].push_back(f);
				return true;
			}
			return false;
		}

		void tray_event_manager::umake(native_window_type wd)
		{
			maptable_.erase(wd);
		}
	//end class tray_event_manager

		//class revertible_mutex
			reversible_mutex::reversible_mutex()
			{
				thr_.tid = 0;
				thr_.refcnt = 0;
			}

			void reversible_mutex::lock()
			{
				recursive_mutex::lock();

				if(0 == thr_.tid)
					thr_.tid = nana::system::this_thread_id();
				++thr_.refcnt;
			}

			bool reversible_mutex::try_lock()
			{
				if(recursive_mutex::try_lock())
				{
					if(0 == thr_.tid)
						thr_.tid = nana::system::this_thread_id();
					++thr_.refcnt;
					return true;
				}
				return false;
			}

			void reversible_mutex::unlock()
			{
				if(thr_.tid == nana::system::this_thread_id())
					if(0 == --thr_.refcnt)
						thr_.tid = 0;
				recursive_mutex::unlock();
			}

			void reversible_mutex::revert()
			{
				if(thr_.refcnt && (thr_.tid == nana::system::this_thread_id()))
				{
					std::size_t cnt = thr_.refcnt;
					
					stack_.push_back(thr_);
					thr_.tid = 0;
					thr_.refcnt = 0;

					for(std::size_t i = 0; i < cnt; ++i)
						recursive_mutex::unlock();
				}
			}

			void reversible_mutex::forward()
			{
				recursive_mutex::lock();
				if(stack_.size())
				{
					thr_refcnt thr = stack_.back();
					if(thr.tid == nana::system::this_thread_id())
					{
						stack_.pop_back();
						for(std::size_t i = 0; i < thr.refcnt; ++i)
							recursive_mutex::lock();
						thr_ = thr;
					}
					else
						throw std::runtime_error("Nana.GUI: The forward is not matched.");
				}
				recursive_mutex::unlock();
			}
		//end class revertible_mutex
}//end namespace detail
}//end namespace gui
}//end namespace nana
