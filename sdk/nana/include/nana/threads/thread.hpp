/*
 *	Thread Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/threads/thread.hpp
 */

#ifndef NANA_THREADS_THREAD_HPP
#define NANA_THREADS_THREAD_HPP

#include <map>
#include "mutex.hpp"
#include "../exceptions.hpp"
#include "../traits.hpp"
#include <nana/functor.hpp>

namespace nana
{
	namespace threads
	{
		class thread;

		namespace detail
		{
			struct thread_object_impl;

			class thread_holder
			{
			public:
				void insert(unsigned tid, thread* obj);
				thread* get(unsigned tid);
				void remove(unsigned tid);
			private:
				threads::recursive_mutex	mutex_;
				std::map<unsigned, thread*>	map_;
			};
		}//end namespace detail

		class thread
			:nana::noncopyable
		{
			typedef thread self_type;
		public:
			thread();
			~thread();

			template<typename Functor>
			void start(const Functor& f)
			{
				close();
				_m_start_thread(f);
			}

			template<typename Object, typename Concept>
			void start(Object& obj, void (Concept::*memf)())
			{
				close();
				_m_start_thread(nana::functor<void()>(obj, memf));
			}

			bool empty() const;
			unsigned tid() const;
			void close();
			static void check_break(int retval);

		private:
			void _m_start_thread(const nana::functor<void()>& f);
		private:
			void _m_add_tholder();
			unsigned _m_thread_routine();
		private:
			detail::thread_object_impl* impl_;
			static		detail::thread_holder tholder;
		};
	}//end namespace threads
}//end namespace nana

#endif

