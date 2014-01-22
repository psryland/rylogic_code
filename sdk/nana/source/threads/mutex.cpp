/*
 *	A C++11-like Mutex Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: source/threads/mutex.cpp
 */

#include <nana/threads/mutex.hpp>

#if 0 == NANA_USE_BOOST_MUTEX_CONDITION_VARIABLE

#include <nana/system/platform.hpp>
#include <stdexcept>

#if defined(NANA_WINDOWS)
	//TryEnterCriticalSection requires _WIN32_WINNT >= 0x400
	#ifdef _WIN32_WINNT
		#undef _WIN32_WINNT
	#endif
	#define _WIN32_WINNT 0x0400
	#include <windows.h>
#elif defined(NANA_LINUX)
	#include <pthread.h>
	#include <time.h>
	#include <sys/time.h>
	#include <errno.h>
#endif

namespace nana{
	namespace threads
	{
		//class mutex
			struct mutex::impl
			{
				unsigned tid;
#if defined(NANA_WINDOWS)
				HANDLE native_mutex;
#elif defined(NANA_LINUX)
				pthread_mutex_t native_mutex;
#endif
			};

			mutex::mutex()
				: impl_(new impl)
			{
				impl_->tid = static_cast<unsigned>(-1);
#if defined(NANA_WINDOWS)
				impl_->native_mutex = ::CreateEventW(0, FALSE, TRUE, 0);
#elif defined(NANA_LINUX)
				pthread_mutex_t dup = PTHREAD_MUTEX_INITIALIZER;
				impl_->native_mutex = dup;
				pthread_mutex_init(&(impl_->native_mutex), 0);
#endif
			};

			mutex::~mutex()
			{
#if defined(NANA_WINDOWS)
				if(impl_->native_mutex)
					::CloseHandle(impl_->native_mutex);
#elif defined(NANA_LINUX)
				pthread_mutex_destroy(&(impl_->native_mutex));
#endif
				delete impl_;
			}

			void mutex::lock()
			{
				unsigned id = nana::system::this_thread_id();
				if(impl_->tid == id)
					throw std::runtime_error("device or resource busy.");
#if defined(NANA_WINDOWS)
				if(impl_->native_mutex)
					::WaitForSingleObject(impl_->native_mutex, INFINITE);
#elif defined(NANA_LINUX)
				::pthread_mutex_lock(&(impl_->native_mutex));
#endif
				impl_->tid = id;
			}

			bool mutex::try_lock()
			{
				unsigned id = nana::system::this_thread_id();
#if defined(NANA_WINDOWS)
				if(impl_->native_mutex)
				{
					if(WAIT_TIMEOUT != ::WaitForSingleObject(impl_->native_mutex, 1))
					{
						impl_->tid = id;
						return true;
					}
				}
#elif defined(NANA_LINUX)
				if(0 == pthread_mutex_trylock(&(impl_->native_mutex)))
				{
					impl_->tid = id;
					return true;
				}
#endif
				return false;
			}

			void mutex::unlock()
			{
				impl_->tid = static_cast<unsigned>(-1);
#if defined(NANA_WINDOWS)
				if(impl_->native_mutex)
					::SetEvent(impl_->native_mutex);
#elif defined(NANA_LINUX)
				pthread_mutex_unlock(&(impl_->native_mutex));
#endif			
			}

			mutex::native_handle_type mutex::native_handle()
			{
				return (impl_ ? & impl_->native_mutex : 0);
			}
		//end class mutex


		//class recursive_mutex
			struct recursive_mutex::impl
			{
#if defined(NANA_WINDOWS)
				::CRITICAL_SECTION native_mutex;
#elif defined(NANA_LINUX)
				pthread_mutex_t native_mutex;
#endif
			};

			recursive_mutex::recursive_mutex()
				: impl_(new impl)
			{
#if defined(NANA_WINDOWS)
					::InitializeCriticalSection(&(impl_->native_mutex));
#elif defined(NANA_LINUX)
					pthread_mutex_t dup = PTHREAD_MUTEX_INITIALIZER;
					impl_->native_mutex = dup;
					pthread_mutexattr_t attr;
					pthread_mutexattr_init(&attr);
					pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE);
					pthread_mutex_init(&impl_->native_mutex, &attr);
					pthread_mutexattr_destroy(&attr);
#endif			
			}

			recursive_mutex::~recursive_mutex()
			{
#if defined(NANA_WINDOWS)
				::DeleteCriticalSection(&impl_->native_mutex);
#elif defined(NANA_LINUX)
				pthread_mutex_destroy(&impl_->native_mutex);
#endif
				delete impl_;
			}

			void recursive_mutex::lock()
			{
#if defined(NANA_WINDOWS)
				::EnterCriticalSection(&(impl_->native_mutex));
#elif defined(NANA_LINUX)
				pthread_mutex_lock(&(impl_->native_mutex));
#endif			
			}

			bool recursive_mutex::try_lock()
			{
#if defined(NANA_WINDOWS)
				return (0 != ::TryEnterCriticalSection(&(impl_->native_mutex)));
#elif defined(NANA_LINUX)
				return (0 == pthread_mutex_trylock(&(impl_->native_mutex)));
#endif			
			}

			void recursive_mutex::unlock()
			{
#if defined(NANA_WINDOWS)
				::LeaveCriticalSection(&(impl_->native_mutex));
#elif defined(NANA_LINUX)
				pthread_mutex_unlock(&(impl_->native_mutex));
#endif			
			}

			recursive_mutex::native_handle_type recursive_mutex::native_handle()
			{
				return (impl_ ? &impl_->native_mutex : 0);
			}
		//end class recursive_mutex


		//class timed_mutex
			struct timed_mutex::impl
			{
#if defined(NANA_WINDOWS)
				HANDLE native_mutex;
#elif defined(NANA_LINUX)
				pthread_mutex_t native_mutex;
#endif
			};

			timed_mutex::timed_mutex()
				: impl_(new impl)
			{
#if defined(NANA_WINDOWS)
				impl_->native_mutex = ::CreateEventW(0, FALSE, FALSE, 0);
#elif defined(NANA_LINUX)
				pthread_mutex_t dup = PTHREAD_MUTEX_INITIALIZER;
				impl_->native_mutex = dup;
				pthread_mutex_init(&(impl_->native_mutex), 0);
#endif			
			}

			timed_mutex::~timed_mutex()
			{
#if defined(NANA_WINDOWS)
				::CloseHandle(&impl_->native_mutex);
#elif defined(NANA_LINUX)
				pthread_mutex_destroy(&impl_->native_mutex);
#endif
				delete impl_;
			}

			void timed_mutex::lock()
			{
#if defined(NANA_WINDOWS)
				if(impl_->native_mutex)
					::WaitForSingleObject(impl_->native_mutex, INFINITE);
#elif defined(NANA_LINUX)
				::pthread_mutex_lock(&(impl_->native_mutex));
#endif			
			}

			bool timed_mutex::try_lock()
			{
#if defined(NANA_WINDOWS)
				if(impl_->native_mutex)
					return (WAIT_TIMEOUT != ::WaitForSingleObject(impl_->native_mutex, 1));
#elif defined(NANA_LINUX)
				return (0 == pthread_mutex_trylock(&(impl_->native_mutex)));
#endif
				return false;
			}

			bool timed_mutex::try_lock_for(std::size_t milliseconds)
			{
#if defined(NANA_WINDOWS)
				if(impl_->native_mutex)
					return (WAIT_TIMEOUT != ::WaitForSingleObject(impl_->native_mutex, 1));
#elif defined(NANA_LINUX)
				timespec abs;
				clock_gettime(CLOCK_REALTIME, &abs);
				abs.tv_nsec += milliseconds;
				return (0 == pthread_mutex_timedlock(&(impl_->native_mutex), &abs));
#endif
				return false;
			}

			void timed_mutex::unlock()
			{
#if defined(NANA_WINDOWS)
				if(impl_->native_mutex)
					::SetEvent(impl_->native_mutex);
#elif defined(NANA_LINUX)
				pthread_mutex_unlock(&(impl_->native_mutex));
#endif
			}

			timed_mutex::native_handle_type timed_mutex::native_handle()
			{
				return (impl_ ? & impl_->native_mutex : 0);
			}
		//end class timed_mutex
	}//end namespace threads
}//end namespace nana
#endif //#if 0 == NANA_USE_BOOST_MUTEX_CONDITION_VARIABLE
