/*
 *	A Thread Pool Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *
 *	@file: nana/threads/pool.cpp
 */

#include <nana/threads/pool.hpp>
#include <nana/system/platform.hpp>
#include <nana/threads/mutex.hpp>
#include <nana/threads/condition_variable.hpp>

#include <time.h>
#include <deque>
#include <vector>
#include <stdexcept>

#if defined(NANA_WINDOWS)
	#include <windows.h>
	#include <process.h>
#elif defined(NANA_LINUX)
	#include <pthread.h>
#endif

namespace nana
{
namespace threads
{
	//class pool
		//struct task
			pool::task::task(t k) : kind(k){}
			pool::task::~task(){}
		//end struct task

		//struct task_signal
		struct pool::task_signal
			: task
		{
			task_signal()
				: task(task::signal)
			{}

			void run()
			{}
		};//end struct task_signal

		class pool::impl
		{
			struct state
			{
				enum t{init, idle, run, finished};
			};

			struct pool_throbj
			{
#if defined(NANA_WINDOWS)
				typedef HANDLE thread_t;
#elif defined(NANA_LINUX)
				typedef pthread_t thread_t;
#endif
				impl * pool_ptr;
				task * task_ptr;
				thread_t	handle;
				volatile state::t	thr_state;
				time_t	timestamp;
#if defined(NANA_LINUX)
				mutex wait_mutex;
				condition_variable wait_cond;
				volatile bool suspended;
#endif
			};
		public:
			impl(std::size_t thr_number)
				: runflag_(true)
			{
				if(0 == thr_number) thr_number = 4;

				for(; thr_number; --thr_number)
				{
					pool_throbj * pto = new pool_throbj;
					pto->pool_ptr = this;
					pto->thr_state = state::init;
					pto->task_ptr = 0;
#if defined(NANA_WINDOWS)
					pto->handle = (HANDLE)::_beginthreadex(0, 0, reinterpret_cast<unsigned(__stdcall*)(void*)>(&impl::_m_thr_starter), pto, 0, 0);
#elif defined(NANA_LINUX)
					pto->suspended = false;
					::pthread_create(&(pto->handle), 0, reinterpret_cast<void*(*)(void*)>(&impl::_m_thr_starter), pto);
#endif
					container_.threads.push_back(pto);
				}
			}

			~impl()
			{
				runflag_ = false;

				while(true)
				{
					bool all_finished = true;
					{
						for(std::vector<pool_throbj*>::iterator i = container_.threads.begin(); i != container_.threads.end(); ++i)
						{
							if(state::finished != (*i)->thr_state)
							{
								all_finished = false;
								break;
							}
						}
					}

					if(all_finished)
						break;
					
					while(true)
					{
						pool_throbj* idle_thr = _m_pick_up_an_idle();
						if(idle_thr)
							_m_resume(idle_thr);
						else
							break;
					}
					nana::system::sleep(100);
				}

				std::vector<pool_throbj*> dup;
				dup.swap(container_.threads);

				for(std::vector<pool_throbj*>::iterator i = dup.begin(); i != dup.end(); ++i)
				{
					pool_throbj * thr = *i;
#if defined(NANA_WINDOWS)
					::WaitForSingleObject(thr->handle, INFINITE);
					::CloseHandle(thr->handle);
#elif defined(NANA_LINUX)
					::pthread_join(thr->handle, 0);
					::pthread_detach(thr->handle);
#endif
				}

				lock_guard<recursive_mutex> lock(mutex_);
				for(std::deque<task*>::iterator i = container_.tasks.begin(); i != container_.tasks.end(); ++i)
				{
					delete (*i);
				}
			}

			void push(task * taskptr)
			{
				if(false == runflag_)
				{
					delete taskptr;
					throw std::runtime_error("Nana.Pool: Do not accept task now");
				}

				pool_throbj * pto = _m_pick_up_an_idle();
				
				if(pto)
				{
					pto->task_ptr = taskptr;
					_m_resume(pto);
				}
				else
				{
					lock_guard<recursive_mutex> lock(mutex_);
					container_.tasks.push_back(taskptr);
				}
			}

			void wait_for_signal()
			{
				unique_lock<mutex> lock(signal_.mutex);
				signal_.cond.wait(lock);
			}

			void wait_for_finished()
			{
				while(true)
				{
					{
						lock_guard<recursive_mutex> lock(mutex_);
						if(container_.tasks.empty())
						{
							bool finished = true;
							for(std::vector<pool_throbj*>::iterator i = container_.threads.begin(); i != container_.threads.end(); ++i)
							{
								if(state::run == (*i)->thr_state)
								{
									finished = false;
									break;
								}
							}
							if(finished)
								return;
						}
					}
					nana::system::sleep(100);
				}			
			}
		private:
			pool_throbj* _m_pick_up_an_idle()
			{
				for(std::vector<pool_throbj*>::iterator i = container_.threads.begin(); i != container_.threads.end(); ++i)
				{
					pool_throbj* thr = *i;
					if(state::idle == thr->thr_state)
					{
						lock_guard<recursive_mutex> lock(mutex_);
						if(state::idle == thr->thr_state)
						{
							thr->thr_state = state::run;
							return thr;
						}
					}
				}
				return 0;
			}

			void _m_suspend(pool_throbj* pto)
			{
				pto->thr_state = state::idle;
#if defined(NANA_WINDOWS)
				::SuspendThread(pto->handle);
#elif defined(NANA_LINUX)
				unique_lock<mutex> lock(pto->wait_mutex);
				pto->suspended = true;
				pto->wait_cond.wait(lock);
				pto->suspended = false;
#endif
			}

			void _m_resume(pool_throbj* pto)
			{
#if defined(NANA_WINDOWS)
				while(true)
				{
					DWORD n = ::ResumeThread(pto->handle);
					if(n == 1 || n == static_cast<DWORD>(-1))
						break;
				}
#elif defined(NANA_LINUX)
				while(false == pto->suspended)
					;
				unique_lock<mutex> lock(pto->wait_mutex);
				pto->wait_cond.notify_one();
#endif
			}

			bool _m_read(pool_throbj* pto)
			{
				pto->task_ptr = 0;
				if(runflag_)
				{
					lock_guard<recursive_mutex> lock(mutex_);
					if(container_.tasks.size())
					{
						pto->task_ptr = container_.tasks.front();
						container_.tasks.erase(container_.tasks.begin());
					}
				}
				else
					return false;

				if(0 == pto->task_ptr)
				{
					//The task queue is empty, so that
					//suspend and wait for a task.
					_m_suspend(pto);
				}

				return (0 != pto->task_ptr);
			}

			void _m_thr_runner(pool_throbj* pto)
			{
				while(_m_read(pto))
				{
					pto->timestamp = time(0);
					switch(pto->task_ptr->kind)
					{
					case task::general:
						try
						{
							pto->task_ptr->run();
						}catch(...){}
						break;
					case task::signal:
						while(true)
						{
							bool finished = true;
							{
								lock_guard<recursive_mutex> lock(mutex_);
								for(std::vector<pool_throbj*>::iterator i = container_.threads.begin(); i != container_.threads.end(); ++i)
								{
									pool_throbj * thr = *i;
									if((thr != pto) && (state::run == thr->thr_state) && (thr->timestamp <= pto->timestamp))
									{
										finished = false;
										break;
									}
								}
							}

							if(finished)
								break;
							nana::system::sleep(100);
						}

						//wait till the cond is waiting.
						signal_.cond.notify_one();
						break;
					}
					delete pto->task_ptr;
					pto->task_ptr = 0;
				}

				pto->thr_state = state::finished;
			}

			//Here defines the a function used for creating a thread.
			//This is platform-specified.
#if defined(NANA_WINDOWS)
			static unsigned __stdcall _m_thr_starter(pool_throbj * pto)
			{
				pto->pool_ptr->_m_thr_runner(pto);
				::_endthreadex(0);
				return 0;
			}
#elif defined(NANA_LINUX)
			static void * _m_thr_starter(pool_throbj * pto)
			{
				pto->pool_ptr->_m_thr_runner(pto);
				return 0;
			}
#endif
		private:
			volatile bool runflag_;
			recursive_mutex mutex_;

			struct signal
			{
				nana::threads::mutex mutex;
				nana::threads::condition_variable cond;
			}signal_;

			struct container
			{
				std::deque<task*> tasks;
				std::vector<pool_throbj*> threads;
			}container_;
		};//end class impl

		pool::pool()
			: impl_(new impl(4))
		{
		}

		pool::pool(std::size_t thread_number)
			: impl_(new impl(thread_number))
		{
		}

		pool::~pool()
		{
			delete impl_;
		}

		void pool::signal()
		{
			task * task_ptr = 0;
			try
			{
				task_ptr = new task_signal;
				_m_push(task_ptr);
			}
			catch(std::bad_alloc&)
			{
				delete task_ptr;
			}
		}

		void pool::wait_for_signal()
		{
			impl_->wait_for_signal();
		}

		void pool::wait_for_finished()
		{
			impl_->wait_for_finished();
		}

		void pool::_m_push(task* task_ptr)
		{
			impl_->push(task_ptr);
		}
	//end class pool

}//end namespace threads
}//end namespace nana
