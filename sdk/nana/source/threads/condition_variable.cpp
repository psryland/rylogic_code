/*
 *	A C++11-like Condition Variable Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: source/threads/condition_variable.cpp
 */

#include <nana/config.hpp>
#include <nana/threads/condition_variable.hpp>

#if 0 == NANA_USE_BOOST_MUTEX_CONDITION_VARIABLE

#if defined(NANA_WINDOWS)
	#include <windows.h>
#elif defined(NANA_LINUX)
	#include <pthread.h>
	#include <sys/time.h>
	#include <errno.h>
#endif

namespace nana{
	namespace threads
	{
		//class condition_variable
			//struct impl
			struct condition_variable::impl
			{
#if defined(NANA_WINDOWS)
				HANDLE cond;

				nana::threads::mutex	cnt_mutex;
				std::size_t wait_count;
#elif defined(NANA_LINUX)
				pthread_cond_t cond;
#endif
			};
			//end struct impl
		
			condition_variable::condition_variable()
				: impl_(new impl)
			{
#if defined(NANA_WINDOWS)
				impl_->cond = ::CreateEventW(0, FALSE, FALSE, 0);
				impl_->wait_count = 0;
#elif defined(NANA_LINUX)
				pthread_cond_t dup = PTHREAD_COND_INITIALIZER;
				impl_->cond = dup;
#endif
			}

			condition_variable::~condition_variable()
			{
#if defined(NANA_WINDOWS)
				if(impl_->cond)
					::CloseHandle(impl_->cond);
#elif defined(NANA_LINUX)
				pthread_cond_destroy(&(impl_->cond));
#endif
				delete impl_;
			}


			void condition_variable::notify_one()
			{
#if defined(NANA_WINDOWS)
				if(impl_->cond)
				{
					lock_guard<nana::threads::mutex> l(impl_->cnt_mutex);
					if(impl_->wait_count)
					{
						--(impl_->wait_count);
						::SetEvent(impl_->cond);
					}
				}
#elif defined(NANA_LINUX)
				::pthread_cond_signal(&impl_->cond);
#endif
			}

			void condition_variable::notify_all()
			{
#if defined(NANA_WINDOWS)
				if(impl_->cond)
				{
					lock_guard<nana::threads::mutex> l(impl_->cnt_mutex);
					while(impl_->wait_count)
					{
						::SetEvent(impl_->cond);
						--(impl_->wait_count);
					}
				}
#elif defined(NANA_LINUX)
				::pthread_cond_broadcast(&impl_->cond);
#endif			
			}

			void condition_variable::wait(unique_lock<mutex> & u)
			{
#if defined(NANA_WINDOWS)
				if(impl_->cond)
				{
					u.unlock();

					{
						lock_guard<nana::threads::mutex> l(impl_->cnt_mutex);
						++(impl_->wait_count);
					}
					::WaitForSingleObject(impl_->cond, INFINITE);
					u.lock();
				}
#elif defined(NANA_LINUX)
				u.owns_ = false;
				::pthread_cond_wait(&(impl_->cond), reinterpret_cast<pthread_mutex_t*>(u.mutex()->native_handle()));
				u.owns_ = true;
#endif
			}

			bool condition_variable::wait_for(unique_lock<mutex> & u, std::size_t milliseconds)
			{
				bool res = false;
#if defined(NANA_WINDOWS)
				if(impl_->cond)
				{
					u.unlock();
					{
						lock_guard<nana::threads::mutex> l(impl_->cnt_mutex);
						++(impl_->wait_count);
					}
					res = (WAIT_TIMEOUT == ::WaitForSingleObject(impl_->cond, static_cast<DWORD>(milliseconds)));
					u.lock();
				}
#elif defined(NANA_LINUX)
				struct timeval now;
				struct timespec tm;
				::gettimeofday(&now, 0);
				tm.tv_nsec = now.tv_usec * 1000 + (milliseconds % 1000) * 1000000;
				tm.tv_sec = now.tv_sec + milliseconds / 1000 + tm.tv_nsec / 1000000000;
				tm.tv_nsec %= 1000000000;
				u.owns_ = false;
				res = (ETIMEDOUT == ::pthread_cond_timedwait(&impl_->cond, reinterpret_cast<pthread_mutex_t*>(u.mutex()->native_handle()), &tm));
				u.owns_ = true;
#endif
				return res;
			}

			condition_variable::native_handle_type condition_variable::native_handle()
			{
				return (impl_ ? &impl_->cond : 0);
			}
		//end class condition_variable
	}//end namespace thread
}//end namespace nana

#endif //#if 0 == NANA_USE_BOOST_MUTEX_CONDITION_VARIABLE
