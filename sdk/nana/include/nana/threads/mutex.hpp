/*
 *	A C++11-like Mutex Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/threads/mutex.hpp
 */

#ifndef NANA_THREADS_MUTEX_HPP
#define NANA_THREADS_MUTEX_HPP
#include <nana/config.hpp>

#if NANA_USE_BOOST_MUTEX_CONDITION_VARIABLE
#include <boost/thread/mutex.hpp>
#include <boost/thread/recursive_mutex.hpp>

namespace nana{
	namespace threads
	{
		using boost::lock_guard;
		using boost::unique_lock;

		using boost::mutex;
		using boost::recursive_mutex;
		using boost::timed_mutex;
	}//end namespace threads
}//end namespace nana

#else
#include <nana/traits.hpp>
#include <cstddef>

namespace nana{
	namespace threads
	{
		template<typename Mutex>
		class lock_guard
		{
		public:
			typedef Mutex mutex_type;

			explicit lock_guard(mutex_type & mtx)
				: mutex_(mtx)
			{
				mtx.lock();
			}

			~lock_guard()
			{
				mutex_.unlock();
			}
		private:
			mutex_type & mutex_;
		};

		template<typename Mutex>
		class unique_lock
		{
		public:
			typedef Mutex mutex_type;

			unique_lock()
				: mutex_ptr_(0), owns_(false)
			{}

			explicit unique_lock(mutex_type& m)
				: mutex_ptr_(&m), owns_(false)
			{
				m.lock();
				owns_ = true;
			}

			~unique_lock()
			{
				if(owns_)
					mutex_ptr_->unlock();
			}

			void lock()
			{
				mutex_ptr_->lock();
				owns_ = true;
			}

			bool try_lock() throw ()
			{
				return (owns_ = mutex_ptr_->try_lock());
			}

			bool try_lock_for(std::size_t milliseconds)
			{
				//This method requires the mutex_type shall meet the TimedLockable requirements.
				return (owns_ = mutex_ptr_->try_lock_for(milliseconds));
			}

			void unlock()
			{
				mutex_ptr_->unlock();
				owns_ = false;
			}

			void swap(unique_lock & u) throw()
			{
				mutex_type * m = mutex_ptr_;
				mutex_ptr_ = u.mutex_ptr_;
				u.mutex_ptr_ = m;

				bool o = owns_;
				owns_ = u.owns_;
				u.owns_ = o;
			}

			mutex_type * release() throw()
			{
				mutex_type * pm = mutex_ptr_;
				mutex_ptr_ = 0;
				owns_ = false;
				return pm;
			}

			operator bool() const throw()
			{
				return owns_;
			}

			mutex_type * mutex() const throw()
			{
				return mutex_ptr_;
			}
		private:
			friend class condition_variable;
			mutex_type * mutex_ptr_;
			bool owns_;
		};

		class mutex
			: nana::noncopyable
		{
			struct impl;
		public:
			mutex();
			~mutex();

			void lock();
			bool try_lock();
			void unlock();

			typedef void * native_handle_type;
			native_handle_type native_handle();
		private:
			friend class condition_variable;
			impl * impl_;
		};

		class recursive_mutex
			: nana::noncopyable
		{
			struct impl;
		public:
			recursive_mutex();
			~recursive_mutex();

			void lock();
			bool try_lock();
			void unlock();

			typedef void* native_handle_type;
			native_handle_type native_handle();
		private:
			impl * impl_;
		};

		class timed_mutex
			: nana::noncopyable
		{
			struct impl;
		public:
			timed_mutex();
			~timed_mutex();

			void lock();
			bool try_lock();
			bool try_lock_for(std::size_t milliseconds);

			void unlock();

			typedef void* native_handle_type;
			native_handle_type native_handle();
		private:
			impl * impl_;
		};
	}//end namespace threads
}//end namespace nana
#endif
#endif
