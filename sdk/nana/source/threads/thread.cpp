/*
 *	A Thread Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: source/threads/thread.cpp
 */

#include <nana/threads/thread.hpp>
#include <nana/system/platform.hpp>
#if defined(NANA_WINDOWS)
	#include <windows.h>
	#include <process.h>
#elif defined(NANA_LINUX)
	#include <unistd.h>
	#include <pthread.h>
	#include <sys/syscall.h>
#endif
namespace nana
{
	namespace threads
	{
		class thread;

		namespace detail
		{
			struct thread_object_impl
			{
#if defined(NANA_WINDOWS)
				typedef HANDLE thread_t;
#elif defined(NANA_LINUX)
				typedef pthread_t thread_t;
#endif
				thread_t handle;
				unsigned tid;
				volatile bool	exitflag;
				nana::functor<void()> functor;

				nana::threads::thread * const owner;
				unsigned (nana::threads::thread::*routine)();
				void (nana::threads::thread::* add_tholder)();

				thread_object_impl(nana::threads::thread * owner)
					:handle(0), tid(0), exitflag(false), owner(owner), routine(0), add_tholder(0)
				{}
			};


			//class thread_holder
				void thread_holder::insert(unsigned tid, thread* obj)
				{
					lock_guard<recursive_mutex> lock(mutex_);
					map_[tid] = obj;
				}

				thread* thread_holder::get(unsigned tid)
				{
					lock_guard<recursive_mutex> lock(mutex_);
					std::map<unsigned, thread*>::iterator it = map_.find(tid);
					if(it != map_.end())
					{
						return it->second;
					}

					return 0;
				}

				void thread_holder::remove(unsigned tid)
				{
					lock_guard<recursive_mutex> lock(mutex_);
					map_.erase(tid);
				}
			//end class thread_holder


#if defined(NANA_WINDOWS)
			unsigned __stdcall thread_starter(void *arg)
			{
				thread_object_impl * to = reinterpret_cast<thread_object_impl*>(arg);

				unsigned retval = (to->owner->*to->routine)();
				::_endthreadex(retval);
				return retval;
			}
#elif defined(NANA_LINUX)
			void* thread_starter(void *arg)
			{
				thread_object_impl * to = reinterpret_cast<thread_object_impl*>(arg);
				to->tid = ::syscall(__NR_gettid);
				(to->owner->*to->add_tholder)();
				return reinterpret_cast<void*>((to->owner->*to->routine)());
			}
#endif
		}//end namespace detail

		//class thread
			thread::thread()
			{
				impl_ = new detail::thread_object_impl(this);
				impl_->routine = &thread::_m_thread_routine;
				impl_->add_tholder = &thread::_m_add_tholder;
			}

			thread::~thread()
			{
				close();
				delete impl_;
			}

			bool thread::empty() const
			{
				return (impl_ == 0 || impl_->handle == 0);
			}

			unsigned thread::tid() const
			{
				return impl_->tid;
			}

			void thread::close()
			{
				if(empty() == false)
				{
					impl_->exitflag = true;
#if defined(NANA_WINDOWS)
					::WaitForSingleObject(impl_->handle, INFINITE);
					::CloseHandle(impl_->handle);
#elif defined(NANA_LINUX)
					::pthread_join(impl_->handle, 0);
					::pthread_detach(impl_->handle);
#endif
					tholder.remove(impl_->tid);
					impl_->handle = 0;
					impl_->tid = 0;
				}
			}

			void thread::check_break(int retval)
			{
				self_type* self = tholder.get(nana::system::this_thread_id());

				if(self && (self->impl_->exitflag))
					throw nana::thrd_exit(retval);
			}

			void thread::_m_start_thread(const nana::functor<void()>& f)
			{
				impl_->functor = f;
				impl_->exitflag = false;
#if defined(NANA_WINDOWS)
				impl_->handle = (HANDLE)::_beginthreadex(0, 0, &detail::thread_starter, impl_, 0, &impl_->tid);
#elif defined(NANA_LINUX)
				::pthread_create(&(impl_->handle), 0, &detail::thread_starter, impl_);
#endif
			}

			void thread::_m_add_tholder()
			{
				tholder.insert(impl_->tid, this);
			}

			unsigned thread::_m_thread_routine()
			{

				unsigned ret = 0;

				try
				{
					this->impl_->functor();
				}
				catch(nana::thrd_exit& e)
				{
					ret = e.retval();
				}
				catch(...)
				{
					ret = static_cast<unsigned>(-1);
				}

				this->impl_->exitflag = true;
				return ret;
			}
		//private:
		//	detail::thread_object_impl impl_;
		//	static		detail::thread_holder tholder;
		//};

		detail::thread_holder thread::tholder;
	}//end namespace threads
}//end namespace nana

