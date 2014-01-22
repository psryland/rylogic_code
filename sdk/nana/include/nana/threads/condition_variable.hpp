/*
 *	A C++11-like Condition Variable Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/threads/condition_variable.hpp
 */
#ifndef NANA_THREADS_CONDITION_VARIABLE_HPP
#define NANA_THREADS_CONDITION_VARIABLE_HPP

#include <nana/config.hpp>

#if NANA_USE_BOOST_MUTEX_CONDITION_VARIABLE
#include <boost/thread/condition_variable.hpp>
namespace nana{
	namespace threads
	{
		using boost::condition_variable;
	}//end namespace threads
}//end namespace nana

#else

#include <nana/threads/mutex.hpp>

namespace nana{
	namespace threads
	{
		class condition_variable
			: nana::noncopyable
		{
			struct impl;
		public:
			condition_variable();
			~condition_variable();

			void notify_one();
			void notify_all();

			void wait(unique_lock<mutex> & u);

			template<typename Predicate>
			void wait(unique_lock<mutex> & u, Predicate pred)
			{
				while(!pred())
					wait(u);
			}

			bool wait_for(unique_lock<mutex> & u, std::size_t milliseconds);

			template<typename Predicate>
			bool wait_for(unique_lock<mutex> & u, std::size_t milliseconds, Predicate pred)
			{
				while(!pred())
				{
					if(wait_for(u, milliseconds))
						return pred();
				}
				return true;
			}

			typedef void * native_handle_type;
			native_handle_type native_handle();
		private:
			impl * impl_;
		};
	
	}//end namespace threads
}//end namespace nana

#endif
#endif //#if NANA_USE_BOOST_MUTEX_CONDITION_VARIABLE
