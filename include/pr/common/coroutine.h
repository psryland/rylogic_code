//************************************************************************
// Coroutines
//************************************************************************

#pragma once
#include <cstdint>
#include <exception>
#include <span>
#include <vector>
#include <deque>
#include <memory>
#include <atomic>
#include <semaphore>
#include <thread>
#include <condition_variable>
#include <coroutine>
#include <numbers>
#include <format>
#include <cassert>

#include "pr/common/cancel_token.h"
#include "pr/threads/name_thread.h"

#ifndef PR_COROUTINE_NAMES
#define PR_COROUTINE_NAMES 1
#endif
#if PR_COROUTINE_NAMES
#include <regex>
#include <source_location>
#endif

namespace pr::coroutine
{
	template<typename T>
	concept Duration = requires(T t)
	{
		typename T::rep;
		typename T::period;
		std::chrono::duration_cast<T>(t);
	};

	struct yield_none {};

	// Default Scheduler implementation
	struct Scheduler
	{
		// Notes:
		// - The schduler is a singleton, but it must be instantiated manually early in the life of
		//   the program. Scheduler instantiations replace the previous scheduler and restored it when
		//   destructed.
		// - The reason for doing it this way is so that the scheduler is constructed/destructed within
		//   the normal scope of a program rather than at static initialization/destruction time.

	private:

		// A worker thread
		struct Worker
		{
			std::thread m_thread;
			std::mutex m_mutex;
			std::condition_variable m_cv_queued;
			std::deque<std::coroutine_handle<>> m_queue;
			std::atomic<bool> m_shutdown;

			Worker()
				: m_thread()
				, m_mutex()
				, m_cv_queued()
				, m_queue()
				, m_shutdown()
			{
				m_thread = std::thread([this]
				{
					threads::SetCurrentThreadName(std::format("Worker({})", std::this_thread::get_id()));
					try
					{
						for (;;)
						{
							// Grab a job from the queue
							std::coroutine_handle<> job;
							{
								std::unique_lock<std::mutex> lock(m_mutex);
								m_cv_queued.wait(lock, [this] { return !m_queue.empty() || m_shutdown; });
								if (m_shutdown)
									break;

								// Grab the next job
								job = m_queue.front();
								m_queue.pop_front();
							}

							// Run the job up to the next await point, or to completion
							job();
						}
					}
					catch (...)
					{
						assert(false && "Unhandled exception in worker thread");
					}
				});
			}
			~Worker()
			{
				m_shutdown = true;
				m_cv_queued.notify_all();
				if (m_thread.joinable())
					m_thread.join();
			}

			// Check if the worker has jobs queued
			[[nodiscard]] bool is_busy() const
			{
				return !m_queue.empty();
			}

			// Queue a coroutine to be continued on this worker thread
			void enqueue(std::coroutine_handle<> coroutine)
			{
				std::unique_lock<std::mutex> lock(m_mutex);
				m_queue.push_back(coroutine);
				m_cv_queued.notify_all();
			}
		};

		// Singleton instance
		inline static Scheduler* s_instance = nullptr;
		Scheduler* m_prev_scheduler;

		// The worker threads
		std::vector<Worker> m_workers;

		// PRNG state, used to perform a low-discrepancy selection of a work queue to enqueue a coroutine to
		std::atomic<int> m_rng_worker;

		// Notification that a job has completed or has reached the next await
		std::condition_variable m_cv_notify_coroutine_complete;
		std::mutex m_mutex_coroutine_complete;

	public:

		Scheduler(int threads = std::thread::hardware_concurrency())
			: m_prev_scheduler(s_instance)
			, m_workers(threads)
			, m_rng_worker(std::rand())
			, m_cv_notify_coroutine_complete()
			, m_mutex_coroutine_complete()
		{
			s_instance = this;
		}
		Scheduler(Scheduler&&) = delete;
		Scheduler(Scheduler const&) = delete;
		Scheduler& operator =(Scheduler&&) = delete;
		Scheduler& operator =(Scheduler const) = delete;
		~Scheduler()
		{
			s_instance = m_prev_scheduler;
		}

		// Queue a coroutine to be continued on a worker thread.
		// Use 'thread_id = {}' to run on any available worker thread.
		void Schedule(std::coroutine_handle<> coroutine, std::thread::id thread_id = {})
		{
			// Use this to serialise everything
			constexpr bool SerialiseAll = false;
			if constexpr (SerialiseAll)
			{
				coroutine();
			}
			else
			{
				// Add to a specific worker thread
				if (thread_id != std::thread::id{})
				{
					// Find the worker thread with the matching ID
					for (auto& worker : m_workers)
					{
						if (worker.m_thread.get_id() != thread_id)
							continue;

						worker.enqueue(coroutine);
						return;
					}

					throw std::runtime_error("Thread ID not found");
				}

				const int worker_count = static_cast<int>(m_workers.size());

				// Otherwise, queue on the first available worker
				auto start = static_cast<int>(m_rng_worker++ * std::numbers::phi_v<float>) % worker_count; // (Kronecker recurrence sequence)
				for (int i = 0; i != worker_count; ++i)
				{
					auto j = (start + i) % worker_count;
					if (m_workers[j].is_busy())
						continue;

					m_workers[j].enqueue(coroutine);
					return;
				}

				// All are busy, so queue on the first worker
				m_workers[start].enqueue(coroutine);
			}
		}

		// Get the singleton instance of the scheduler
		static Scheduler& instance()
		{
			return *s_instance;
		}
	};

	// Async task type
	template <typename T = void>
	struct Task final
	{
		// Notes:
		//  - A Task is just an awaiter that always queues the continuation on the scheduler
		//  - Task is basically a shared_ptr to a promise type.
		//  - When the last Task is destructed, so too is the promise/coroutine

		template <typename TData> struct Promise;
		using promise_type = Promise<T>;
		using handle_type = std::coroutine_handle<promise_type>;

		// Promise
		struct PromiseBase
		{
			// Notes:
			// - A promise is roughly equivalent to a coroutine handle. Basically a coroutine
			//   handle is a pointer to a blob of heap memory that contains an instance of this
			//   promise.
			// - The promise/coroutine is created at the call-site of the coroutine.
			// - The lifetime of the promise is controlled by 'final_suspend'. If not suspended,
			//   then the promise is deleted at the return from the coroutine function. If suspended,
			//   then handle().destroy() must be called manually to prevent a leak.
			// - Awaiting kinda is orthogonal to coroutines/promises. Anything with the "await_..."
			//   functions can be awaited.
			inline static std::vector<PromiseBase*> LiveObjects{};
			inline static std::mutex LiveObjectsMutex{};

			std::coroutine_handle<> m_continuation{};
			std::exception_ptr m_exception{};
			std::atomic_bool m_done{false};
			std::atomic<int> m_ref_count{};
			#if PR_COROUTINE_NAMES
			std::string m_name{};
			#endif

			#if PR_COROUTINE_NAMES
			PromiseBase(std::source_location loc)
				: m_name(loc.function_name())
			{
				// Extract the short name from a function name
				std::smatch sm;
				auto const templ_shite = std::regex(R"(<[^<>]*>)");
				for (; std::regex_search(m_name, sm, templ_shite); sm = {})
					m_name = std::regex_replace(m_name, templ_shite, "");

				auto const extract_name = std::regex(R"(.*\b(\w+?)\()");
				if (std::regex_search(m_name, sm, extract_name))
					m_name = sm[1];
			
				LiveObjects.push_back(this);
				OutputDebugStringA(std::format("Promise({})={}, Handle={}\n", m_name, (void*)this, handle().address()).c_str());
			}
			#else
			PromiseBase() = default;
			#endif
			PromiseBase(PromiseBase&&) = delete;
			PromiseBase(PromiseBase const&) = delete;
			PromiseBase& operator= (PromiseBase&&) = delete;
			PromiseBase& operator= (PromiseBase const&) = delete;
			~PromiseBase()
			{
				{
					std::lock_guard lock(LiveObjectsMutex);
					auto it = std::find(LiveObjects.begin(), LiveObjects.end(), this);
					if (it != LiveObjects.end())
					{
						LiveObjects.erase(it);
					}
					else
					{
						assert(false && "double delete");
					}
				}
				assert(m_ref_count == 0 && "dangling references");
			}
			handle_type handle()
			{
				return handle_type::from_promise(static_cast<promise_type&>(*this));
			}
			Task get_return_object()
			{
				return Task(static_cast<promise_type*>(this));
			}
			auto initial_suspend() noexcept
			{
				return std::suspend_never{};
			}
			auto suspend(std::coroutine_handle<> coroutine)
			{
				// 'coroutine' is a thing that will run the rest of the function after this await point.
				// Queue it to run on a worker thread. That means 'coroutine' may complete at any point
				// from now on.
				m_continuation = coroutine;
				Scheduler::instance().Schedule(coroutine);
			}
			auto final_suspend() noexcept
			{
				// Mark the coroutine as done
				// Note: the coroutine handle is not "done()" yet, so we can't destroy it here,
				// or notify anyone waiting for it to complete because it would be a race condition.
				// We're forced to use a separate synchronization primitive to signal completion.
				m_done = true;
				m_done.notify_all();

				return std::suspend_always{};
			}
			auto result()
			{
				// Resume will be called in the context of the background thread that is running 'coroutine'
				if (m_exception)
					std::rethrow_exception(m_exception);

				if constexpr (!std::is_same_v<T, void>)
				{
					return static_cast<promise_type&>(*this).m_data;
				}
				else
				{
					return;
				}
			}
			void unhandled_exception()
			{
				m_exception = std::current_exception();
			}
			void add_ref() noexcept
			{
				++m_ref_count;
			}
			void release() noexcept
			{
				assert(m_ref_count > 0);
				if (--m_ref_count == 0)
					handle().destroy();
			}
			void wait() noexcept
			{
				m_done.wait(false);
			}
			void* operator new(size_t size)
			{
				return ::operator new(size);
			}
			void operator delete(void* ptr)
			{
				::operator delete(ptr);
			}
		};
		template <typename TData> struct Promise : PromiseBase
		{
			TData m_data = {};

			#if PR_COROUTINE_NAMES
			Promise(std::source_location loc = std::source_location::current())
				: PromiseBase(loc)
			{}
			#else
			Promise() = default;
			#endif

			//void return_value(TData const& value) noexcept(std::is_nothrow_copy_assignable_v<TData>)
			//{
			//	m_data = value;
			//}
			//void return_value(TData&& value) noexcept(std::is_nothrow_move_assignable_v<TData>)
			//{
			//	m_data = std::move(value);
			//}
			template <std::convertible_to<T> From> auto return_value(From&& from)
			{
				m_data = std::forward<From>(from);
			}
			template <std::convertible_to<T> From> auto yield_value(From&& from)
			{
				m_data = std::forward<From>(from);
				return std::suspend_always{};
			}
		};
		template <> struct Promise<void> : PromiseBase
		{
			#if PR_COROUTINE_NAMES
			Promise(std::source_location loc = std::source_location::current())
				: PromiseBase(loc)
			{}
			#else
			Promise() = default;
			#endif

			void return_void()
			{
			}
			auto yield_value(yield_none)
			{
				return std::suspend_always{};
			}
		};

		// Tasks are awaitable.
		// Awaiting a task means waiting for the contained promise to complete.
		bool await_ready() const
		{
			// This task doesn't need to block if there is no coroutine we're waiting on, or it's finished.
			return !m_promise->m_continuation || m_promise->m_continuation.done();
			//return m_promise->handle().done();
		}
		auto await_suspend(std::coroutine_handle<> coroutine)
		{
			return m_promise->suspend(coroutine);
		}
		auto await_resume() noexcept
		{
			return m_promise->result();
		}

	private:

		promise_type* m_promise;
		Task(promise_type* promise)
			: m_promise(promise)
		{
			m_promise->add_ref();
		}

	public:

		Task(Task&& rhs) noexcept
			: m_promise(std::move(rhs.m_promise))
		{
			rhs.m_promise = nullptr;
		}
		Task(Task const& rhs)
			: m_promise(rhs.m_promise)
		{
			m_promise->add_ref();
		}
		Task& operator =(Task&& rhs) noexcept
		{
			if (this == &rhs) return *this;
			std::swap(m_promise, rhs.m_promise);
			return *this;
		}
		Task& operator =(Task const& rhs)
		{
			if (this == &rhs) return *this;
			m_promise = rhs.m_promise;
			m_promise->add_ref();
			return *this;
		}
		~Task()
		{
			if (m_promise)
				m_promise->release();
		}

		// Access the result of the coroutine
		auto operator*() const requires (!std::is_same_v<T, void>)
		{
			return Result();
		}

		// Wait for the coroutine to complete and return the result
		T Result() const requires (!std::is_same_v<T, void>)
		{
			m_promise->wait();
			return m_promise->result();
		}

		// Wait for the coroutine to complete
		void Wait() const requires (std::is_same_v<T, void>)
		{
			m_promise->wait();
			return;
		}
	};

	// Return type for enumerable coroutines
	template<typename T>
	struct Generator
	{
		struct promise_type;
		using handle_type = std::coroutine_handle<promise_type>;

		struct promise_type
		{
			T m_data = {};
			std::exception_ptr m_exception = {};
 
			Generator get_return_object()
			{
				return Generator(handle_type::from_promise(*this));
			}
			auto initial_suspend()
			{
				return std::suspend_always{};
			}
			auto final_suspend() const noexcept
			{
				return std::suspend_always{};
			}
			void unhandled_exception()
			{
				m_exception = std::current_exception();
			}
			void return_void()
			{
			}
			template <std::convertible_to<T> From> auto yield_value(From&& from)
			{
				m_data = std::forward<From>(from);
				return std::suspend_always{};
			}
		};
 
		class iter
		{
			friend struct Generator;
			handle_type m_handle;

			iter(handle_type handle)
				: m_handle(handle)
			{
				next();
			}

			// Continue the coroutine until done or the next co_yield
			void next()
			{
				if (m_handle == nullptr)
					return;

				// Run the coroutine
				m_handle();

				// Propagate the coroutine exception in called context
				if (m_handle.promise().m_exception)
					std::rethrow_exception(m_handle.promise().m_exception);

				// If the coroutine is finished, make the iter == end()
				if (m_handle.done())
					m_handle = nullptr;
			}

		public:

			T const& operator *() const
			{
				return m_handle.promise().m_data;
			}
			iter& operator ++()
			{
				next();
				return *this;
			}

			friend bool operator == (iter const& lhs, iter const& rhs)
			{
				// Only end iterators are equal, so both must be end iterators
				return (lhs.m_handle == nullptr) && (rhs.m_handle == nullptr);
			}
			friend bool operator != (iter const& lhs, iter const& rhs)
			{
				return !(lhs == rhs);
			}
		};
 
	private:

		handle_type m_handle;

	public:

		Generator(handle_type handle)
			: m_handle(handle)
		{}
		~Generator()
		{
			m_handle.destroy();
		}

		auto begin()
		{
			return iter{m_handle};
		}
		auto end()
		{
			return iter{nullptr};
		}
	};

	// Wait for all tasks to complete
	template<typename... Tasks>
	Task<> WhenAll(Tasks... tasks)
	{
		(co_await tasks, ...);
		co_return;
	}

	// Delay a task for a duration
	template <Duration D>
	Task<> Delay(D wait_time, CancelToken cancel)
	{
		co_yield {};
		cancel.Wait(wait_time);
		co_return;
	}

	//Task<> SwitchToThread(std::thread::id thread_id)
	//{
	//	co_return;
	//}
	//Task<> SwitchToWorkerThread()
	//{
	//	return SwitchToThread({});
	//}
	
	// An awaiter for switching to a specific thread. Use 'thread_id == {}' for any worker thread
	inline auto SwitchToThread(std::thread::id thread_id)
	{
		struct awaiter
		{
			std::thread::id m_thread_id;
			bool await_ready() const noexcept
			{
				return std::this_thread::get_id() == m_thread_id;
			}
			void await_suspend(std::coroutine_handle<> coroutine) const noexcept
			{
				// Queue the rest of the function to run on a background thread
				// and return control back to the caller.
				Scheduler::instance().Schedule(coroutine, m_thread_id);
			}
			void await_resume() const noexcept
			{
			}
		};
		return awaiter{ thread_id };
	}
	inline auto SwitchToWorkerThread()
	{
		return SwitchToThread({});
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::coroutine
{
	namespace tests
	{
		using millis = std::chrono::milliseconds;

		Generator<int> Fibonacci(int n)
		{
			int a = 0, b = 1;
			for (int i = 0; i < n; ++i)
			{
				co_yield a;

				auto next = a + b;
				a = b;
				b = next;
			}
		}
		Task<float> GetFloatAsync(CancelToken cancel)
		{
			auto delay = Delay(millis(1000), cancel);
			co_await delay;
			//// main thread
			//auto swtch = SwitchToWorkerThread();

			//// 'swtch.await_suspend()' called on main thread. Provides a continuation that
			//// starts on the next line, then, control returns to the caller.
			//co_await swtch;

			// Continuation starts on a background worker thread.
			// Calls 'swtch.await_resume()'
			// Then 'promise.return_value()'
			// Then 'promise.final_suspend()'
			co_return 6.28f;
		}
		Task<> ReadFloat(CancelToken cancel)
		{
			// main thread
			// 'get_float.continuation' is switch.promise
			auto get_float = GetFloatAsync(cancel);
			
			// 'get_float.await_suspend()' called on the main thread, but called on 'get_float'
			// Provided continuation must only be queued after 'swtch.final_suspend'
			auto value = co_await get_float;
			PR_EXPECT(value == 6.28f);
			co_return;
		}
		Task<int> GetBusy0(CancelToken cancel)
		{
			co_await SwitchToWorkerThread();
			cancel.Wait(millis(100));
			co_return 1 << 0;
		}
		Task<int> GetBusy1(CancelToken cancel)
		{
			co_await SwitchToWorkerThread();
			cancel.Wait(millis(200));
			co_return 1 << 1;
		}
		Task<int> GetBusy2(CancelToken cancel)
		{
			co_await SwitchToWorkerThread();
			cancel.Wait(millis(200));
			co_return 1 << 2;
		}
		Task<int> JobAsync(CancelToken cancel)
		{
			co_await SwitchToWorkerThread();
			auto t0 = GetBusy0(cancel);
			auto t1 = GetBusy1(cancel);
			auto t2 = GetBusy2(cancel);
			co_await WhenAll(t0, t1, t2);
			co_return *t0 + *t1 + *t2;
		}
	}

	PRUnitTest(CoroutineTests)
	{
		using namespace tests;

		Scheduler scheduler(1);

		CancelTokenSource cts;
		auto cancel = cts.Token();

		auto main_thread_id = std::this_thread::get_id();
/*
*/
		// Test getting data from an awaited task
		{
			ReadFloat(cancel).Wait();
		}

		// Generator test
		{
			int fib[] = { 0, 1, 1, 2, 3, 5, 8, 13, 21, 34 }, i = 0;
			for (auto f : Fibonacci(10))
				PR_EXPECT(f == fib[i++]);
		}

		// Simple background thread task
		{
			auto r = GetBusy1(cancel).Result();
			PR_EXPECT(r == 0b10);
			PR_EXPECT(std::this_thread::get_id() == main_thread_id);
		}

		// Awaitable test
		{
			auto f1 = GetFloatAsync(cancel).Result();
			PR_EXPECT(f1 == 6.28f);

			auto f2 = GetFloatAsync(cancel).Result();
			PR_EXPECT(f2 == 6.28f);
			PR_EXPECT(std::this_thread::get_id() == main_thread_id);
		}
		{
			ReadFloat(cancel).Wait();
			ReadFloat(cancel).Wait();
			PR_EXPECT(std::this_thread::get_id() == main_thread_id);
		}
		{
			auto r = JobAsync(cancel).Result();
			PR_EXPECT(r == 0b111);
			PR_EXPECT(std::this_thread::get_id() == main_thread_id);
		}
	}
}
#endif
