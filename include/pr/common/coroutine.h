//************************************************************************
// Coroutines
//************************************************************************
// Inspired by:
//   Credit: 'Jeremyong'
//   Github: https://github.com/jeremyong/coop.git

#pragma once
#include <cstdint>
#include <exception>
#include <span>
#include <deque>
#include <vector>
#include <atomic>
#include <semaphore>
#include <thread>
#include <condition_variable>
#include <coroutine>
#include <source_location>
#include <numbers>
#include <format>
#include <Windows.h>

#include "pr/common/cancel_token.h"
#include "pr/container/concurrent_queue.h"
#include "pr/threads/name_thread.h"

namespace pr::coroutine
{
	using source_location_t = std::source_location;

	// Forward declare the scheduler
	struct Scheduler;

	// Available priority levels
	inline static constexpr int PriorityCount = 2;

	namespace impl
	{
		template <typename T>
		concept initial_suspend_return_type = std::is_same_v<T, std::suspend_never> || std::is_same_v<T, std::suspend_always>;

		template <typename T>
		concept await_suspend_return_type = std::is_same_v<T, void> || std::is_same_v<T, bool> || std::is_same_v<T, std::coroutine_handle<>>;
	}

	// The concept of a coroutine promise type
	template <typename T>
	concept PromiseType = requires(T t)
	{
		// Invoked when we first enter a coroutine. The returned object is returned to the caller when the coroutine first suspends.
		{ t.get_return_object() };
		
		 // When the caller enters the coroutine, we have the option to suspend immediately.
		{ t.initial_suspend() } -> impl::initial_suspend_return_type;
		
		// The coroutine is about to complete (via co_return or reaching the end of the coroutine body).
		// The awaiter returned here defines what happens next
		{ t.final_suspend() } -> std::same_as<std::suspend_always>;

		// If an exception was thrown in the coroutine body, we would handle it here
		{ t.unhandled_exception() } -> std::same_as<void>;

		// optional:
		//   operator new(size_t, Args...) -> void*;
		//   operator delete(void*, Args...) -> void;
		//  Note: size_t is *NOT* sizeof(PromiseType), but the size of the coroutine state
		//
		// Must have one of the following:
		//  { t.return_value(<expr>) };
		//  { t.return_void() } -> std::same_as<void>;		
	};

	// The concept of an coroutine awaiter type
	template <typename T>
	concept AwaiterType = requires(T t)
	{
		// Do we need to suspend? False => no suspension is needed, coroutine continues executing
		{ t.await_ready() } -> std::same_as<bool>;

		// Can return void, bool, or std::coroutine_handle<>
		// If 'await_suspend' returns void:
		//     Control is immediately returned to the caller/resumer of the current coroutine (this coroutine remains suspended)
		// If 'await_suspend' returns bool:
		//     true => returns control to the caller/resumer of the current coroutine (this coroutine remains suspended)
		//     false => resumes the current coroutine.
		// If 'await_suspend' returns a coroutine handle for some other coroutine, that handle is resumed (by a call to handle.resume())
		//  (note this may chain to eventually cause the current coroutine to resume).
		// If 'await_suspend' throws an exception, the exception is caught, the coroutine is resumed, and the exception is immediately re-thrown.
		{ t.await_suspend(std::coroutine_handle<>{}) } -> impl::await_suspend_return_type;

		// Returns the result of awaiting the awaiter
		{ t.await_resume() };
	};

	// Base class for Promise types
	template <bool Joinable>
	struct PromiseBase
	{
		// Notes:
		// 	- All promises need the `continuation` member, which is set when a coroutine is suspended within another coroutine.
		//    The `continuation` handle is used to hop back from that suspension point when the inner coroutine finishes.
		//  - Coroutine state is allocated dynamically via non-array operator new.
		//    If the Promise type defines a class-level replacement new operator, it will be used, otherwise global operator new will be used.
		//    If the Promise type defines a placement form of operator new that takes additional parameters, and they match an argument list
		//    where the first argument is the size requested (of type std::size_t) and the rest are the coroutine function arguments, those
		//    arguments will be passed to operator new (this makes it possible to use leading-allocator-convention for coroutines).
		inline static constexpr bool joinable_v = Joinable;

		// When a coroutine suspends, the continuation stores the handle to the
		// resume point, which immediately following the suspend point.
		std::coroutine_handle<> continuation = nullptr;

		// Signals that... ??
		// Ensures only one thread returns 'continuation'
		std::atomic<bool> flag = false;

		// Do not suspend immediately on entry of a coroutine
		std::suspend_never initial_suspend() const noexcept
		{
			return {};
		}

		// Called for tasks that don't trap all exceptions
		void unhandled_exception() const noexcept
		{
			assert(false && "not implemented");
		}
	};
	template <>
	struct PromiseBase<true> : PromiseBase<false>
	{
		// Joinable tasks need an additional semaphore the joiner can wait on
		std::binary_semaphore join_semaphore{ 0 };
	};

	namespace impl
	{
		template <typename P, bool Joinable> requires std::is_base_of_v<PromiseBase<Joinable>, P>
		inline std::coroutine_handle<> do_suspend(std::coroutine_handle<P> coroutine)
		{
			if constexpr (Joinable)
			{
				coroutine.promise().join_semaphore.release();
				coroutine.destroy();
			}
			else
			{
				// Check if this coroutine is being finalized from the middle of a "continuation" coroutine
				// and hop back there to continue execution while *this* coroutine is suspended.
				//LOG("Final await for coroutine %p on thread %zu\n", coroutine.address(), detail::thread_id());

				// After acquiring the flag, the other thread's write to the coroutine's continuation must be visible (one-way communication)
				if (coroutine.promise().flag.exchange(true, std::memory_order_acquire))
				{
					// We're not the first to reach here, meaning the continuation is installed properly (if any)
					auto continuation = coroutine.promise().continuation;
					if (continuation)
					{
						//LOG("Resuming continuation %p on %p on thread %zu\n", continuation.address(), coroutine.address(), detail::thread_id());
						return continuation;
					}

					//LOG("Coroutine %p on thread %zu missing continuation\n", coroutine.address(), detail::thread_id());
				}
			}
			return std::noop_coroutine();
		}
	}

	// Default Promise implementation
	template <typename Task, typename T, bool Joinable>
	struct Promise : PromiseBase<Joinable>
	{
		// The result of the promise
		T data = {};

		Task get_return_object() noexcept
		{
			// On coroutine entry, we store as the continuation a handle corresponding to the next sequence point from the caller.
			return { std::coroutine_handle<Promise>::from_promise(*this) };
		}
		void return_value(T const& value) noexcept(std::is_nothrow_copy_assignable_v<T>)
		{
			data = value;
		}
		void return_value(T&& value) noexcept(std::is_nothrow_move_assignable_v<T>)
		{
			data = std::move(value);
		}
		auto final_suspend() noexcept
		{
			struct awaiter_t
			{
				bool await_ready() const noexcept
				{
					return false;
				}
				auto await_suspend(std::coroutine_handle<Promise> coroutine) const noexcept
				{
					return impl::do_suspend<Promise, Joinable>(coroutine);
				}
				void await_resume() const noexcept
				{
				}
			};
			return awaiter_t{};
		}
	};
	template <typename Task, bool Joinable>
	struct Promise<Task, void, Joinable> : PromiseBase<Joinable>
	{
		Task get_return_object() noexcept
		{
			// On coroutine entry, we store as the continuation a handle
			// corresponding to the next sequence point from the caller.
			return { std::coroutine_handle<Promise>::from_promise(*this) };
		}
		void return_void() noexcept
		{
		}
		auto final_suspend() noexcept
		{
			struct awaiter_t
			{
				bool await_ready() const noexcept
				{
					return false;
				}
				auto await_suspend(std::coroutine_handle<Promise> coroutine) const noexcept
				{
					return impl::do_suspend<Promise, Joinable>(coroutine);
				}
				void await_resume() const noexcept
				{
				}
			};
			return awaiter_t{};
		}
	};

	// Default async task type returned from a coroutine
	template <typename T = void, bool Joinable = false>
	struct Task
	{
		// The return type of a coroutine must contain a nested struct or type alias called `promise_type`
		using promise_type = Promise<Task, T, Joinable>;

		// The coroutine handle (basically a pointer to the resume point + captured variable states)
		std::coroutine_handle<promise_type> m_coroutine;

		Task() noexcept
			: m_coroutine()
		{}
		Task(std::coroutine_handle<promise_type> coroutine) noexcept
			: m_coroutine(coroutine)
		{}
		Task(Task&& rhs) noexcept
			: m_coroutine(rhs.m_coroutine)
		{
			rhs.m_coroutine = nullptr;
		}
		Task(Task const&) = delete;
		Task& operator=(Task&& rhs) noexcept
		{
			if (this == &rhs) return *this;
			if constexpr (!Joinable)
			{
				// For joinable tasks, the coroutine is destroyed in the final awaiter to support fire-and-forget semantics
				if (m_coroutine)
					m_coroutine.destroy();
			}
			m_coroutine = rhs.m_coroutine;
			rhs.m_coroutine = nullptr;
			return *this;
		}
		Task& operator=(Task const&) = delete;
		~Task() noexcept
		{
			if constexpr (!Joinable)
			{
				if (m_coroutine)
					m_coroutine.destroy();
			}
		}

		// The dereferencing operators below return the data contained in the associated promise
		[[nodiscard]] auto operator*() noexcept
		{
			static_assert(!std::is_same_v<T, void>, "This task doesn't contain any data");
			return std::ref(promise().data);
		}
		[[nodiscard]] auto operator*() const noexcept
		{
			static_assert(!std::is_same_v<T, void>, "This task doesn't contain any data");
			return std::cref(promise().data);
		}

		// A task_t is 'truthy' if it is not associated with an outstanding coroutine or the coroutine it is associated with is complete
		[[nodiscard]] operator bool() const noexcept
		{
			return await_ready();
		}
		
		// Awaiter traits
		bool await_ready() const noexcept
		{
			return !m_coroutine || m_coroutine.done();
		}
		std::coroutine_handle<> await_suspend(std::coroutine_handle<> coroutine) noexcept
		{
			// When suspending from a coroutine *within* this task's coroutine,
			// save the resume point (to be resumed when the inner coroutine finalizes)
			if constexpr (Joinable)
			{
				// Joinable tasks are never awaited and so cannot have a continuation by definition
				return std::noop_coroutine();
			}
			else
			{
				//LOG("Installing continuation %p for %p on thread %zu\n", next.address(), base.address(), detail::thread_id());
				promise().continuation = coroutine;

				// The write to the continuation must be visible to a person that acquires the flag
				if (promise().flag.exchange(true, std::memory_order_release))
				{
					// We're not the first to reach here, meaning the continuation won't get read
					return coroutine;
				}

				return std::noop_coroutine();
			}
		}
		auto await_resume() const noexcept
		{
			// The return value of 'await_resume' is the final result of `co_await this_task` once the coroutine associated with this task completes
			if constexpr (std::is_same_v<T, void>)
				return;
			else
				return std::move(promise().data);
		}

		// Join the back to the original thread
		void join() requires(Joinable)
		{
			promise().join_semaphore.acquire();
		}

	protected:

		// Return the promise for the coroutine
		[[nodiscard]] promise_type& promise() const noexcept
		{
			return m_coroutine.promise();
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
			T m_data;
			std::exception_ptr m_exception;
 
			Generator get_return_object()
			{
				return Generator(handle_type::from_promise(*this));
			}
			std::suspend_always initial_suspend()
			{
				return {};
			}
			std::suspend_always final_suspend() noexcept
			{
				return {};
			}
			void unhandled_exception()
			{
				m_exception = std::current_exception();
			}
			template <std::convertible_to<T> From> std::suspend_always yield_value(From&& from)
			{
				m_data = std::forward<From>(from);
				return {};
			}
			void return_void()
			{
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

		Generator(handle_type h)
			: m_handle(h)
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

	// Implementation details
	namespace impl
	{
		// Result of waiting for multiple events
		struct WaitResult
		{
			// Wait result status
			enum EStatus
			{
				Normal,
				Abandoned,
				Timeout,
				Failed
			};

			EStatus status = EStatus::Normal;
			uint32_t index = 0;
		};

		// Non-owning reference to an event
		struct EventRef
		{
			void* m_handle;

			EventRef() noexcept
				: m_handle()
			{}
			EventRef(void* handle) noexcept
				: m_handle(handle)
			{}
			EventRef(EventRef&& rhs) noexcept
				: m_handle(rhs.m_handle)
			{
				rhs.m_handle = nullptr;
			}
			EventRef(EventRef const&) = default;
			EventRef& operator=(EventRef&& rhs) noexcept
			{
				if (this == &rhs) return *this;
				std::swap(m_handle, rhs.m_handle);
				return *this;
			}
			EventRef& operator=(EventRef const&) = default;

			// Check if this event is signaled (returns immediately)
			bool is_signaled() const noexcept
			{
				return WaitForSingleObject(m_handle, 0) == WAIT_OBJECT_0;
			}

			// Wait (potentially indefinitely) for this event to be signaled
			bool wait() const
			{
				return WaitForSingleObject(m_handle, INFINITE) == WAIT_OBJECT_0;
			}

			// Mark this event as signaled
			void signal()
			{
				SetEvent(m_handle);
			}

			// Mark this event as not signaled (needed for events that are manually reset, as opposed to reset after wait)
			void reset()
			{
				ResetEvent(m_handle);
			}

			// 'truthy' operator
			operator bool() const noexcept
			{
				return is_signaled();
			}

			// Return the index of the first event signaled in a given array of events
			static WaitResult wait_many(std::span<EventRef const> events)
			{
				static_assert(sizeof(EventRef) == sizeof(HANDLE));
				HANDLE const* handles = reinterpret_cast<HANDLE const*>(events.data());

				uint32_t result = WaitForMultipleObjects(static_cast<DWORD>(events.size()), handles, false, INFINITE);
				if (result == WAIT_FAILED)
					return { WaitResult::Failed };
				if (result == WAIT_TIMEOUT)
					return { WaitResult::Timeout };
				if (result >= WAIT_ABANDONED_0)
					return { WaitResult::Abandoned, result - WAIT_OBJECT_0 };

				return { WaitResult::Normal, result - WAIT_OBJECT_0 };
			}
		};

		// RAII object for an event
		struct Event final : EventRef
		{
			uint64_t m_cpu_affinity;
			uint32_t m_priority;

			Event() noexcept
				: EventRef()
				, m_cpu_affinity()
				, m_priority()
			{}
			Event(bool manual_reset = false, char const* label = nullptr) noexcept
				: EventRef(::CreateEventA(nullptr, manual_reset, false, label))
				, m_cpu_affinity()
				, m_priority()
			{}
			Event(Event&&) noexcept = default;
			Event(Event const&) = delete;
			Event& operator=(Event&&) noexcept = default;
			Event& operator=(Event const&) = delete;
			~Event() noexcept
			{
				if (m_handle)
					CloseHandle(m_handle);
			}

			// Return a new reference to this event
			EventRef ref() const noexcept
			{
				return { m_handle };
			}

			// The CPU affinity is used to consider the *continuation* after this event is signaled
			void set_cpu_affinity(uint32_t affinity) noexcept
			{
				m_cpu_affinity = affinity;
			}

			// The CPU priority is used to consider the *continuation* after this event is signaled
			void set_priority(uint32_t priority) noexcept
			{
				m_priority = priority;
			}

			// Awaiter traits
			bool await_ready() const noexcept
			{
				return is_signaled();
			}
			void await_resume() const noexcept
			{
			}
			void await_suspend(std::coroutine_handle<> coroutine) noexcept;
		};

		// Continuation for joinable promises
		struct EventContinuation
		{
			std::coroutine_handle<> m_coroutine;
			EventRef m_event;
			uint64_t m_cpu_affinity;
			uint32_t m_priority;
		};

		// CPU Work Queue
		struct WorkQueue
		{
			using coroutine_t = struct { std::coroutine_handle<> handle; source_location_t src; };
			using coroutines_queue_t = pr::container::ConcurrentQueue<coroutine_t>;

			int m_cpu_index;
			std::thread m_thread;
			std::atomic<bool> m_shutdown;
			std::counting_semaphore<> m_semaphore;
			coroutines_queue_t m_queues[PriorityCount];

			WorkQueue(int cpu_index)
				: m_cpu_index(cpu_index)
				, m_thread()
				, m_shutdown()
				, m_semaphore(0)
				, m_queues()
			{
				m_thread = std::thread([this]
				{
					threads::SetCurrentThreadName(std::format("WorkQueue:{}", m_cpu_index));
					SetThreadAffinityMask(GetCurrentThread(), 1ULL << m_cpu_index);

					try
					{
						for (;;)
						{
							// Block until there is work to do
							m_semaphore.acquire();
							if (m_shutdown)
								break;

							// Dequeue from highest priority to lowest
							for (auto i = _countof(m_queues); i-- != 0; )
							{
								coroutine_t coroutine;
								if (!m_queues[i].try_dequeue(coroutine))
									continue;

								//LOG("De-queueing coroutine %p on thread %zu (%i)\n", coroutine.address(), detail::thread_id(), m_cpu_index);
								m_semaphore.release();
								coroutine.handle.resume();
								break;
							}
						}
					}
					catch (std::exception const& ex)
					{
						(void)ex;
						//LOG("WorkQueue %i caught exception: %s\n", m_cpu_index, ex.what());
					}
				});
			}
			WorkQueue(WorkQueue&&) = delete;
			WorkQueue(WorkQueue const&) = delete;
			WorkQueue& operator=(WorkQueue&&) = delete;
			WorkQueue& operator=(WorkQueue const&) = delete;
			~WorkQueue() noexcept
			{
				m_shutdown = true;
				m_semaphore.release();
				if (m_thread.joinable())
					m_thread.join();
			}

			// Returns the approximate number of queue coroutines across all queues of any priority
			size_t size_approx() const noexcept
			{
				size_t out = 0;
				for (auto i = _countof(m_queues); i-- != 0; )
					out += m_queues[i].size_approx();

				return out;
			}

			// Enqueue a coroutine to be continued on this work queue
			void enqueue(std::coroutine_handle<> coroutine, int priority = 0, source_location_t source_location = {})
			{
				priority = std::clamp(priority, 0, static_cast<int>(_countof(m_queues)) - 1);

				//LOG("En-queueing coroutine %p on thread %zu (%s:%zu)\n", coroutine.address(), detail::thread_id(), source_location.file, source_location.line);

				m_queues[priority].enqueue({ coroutine, source_location });
				m_semaphore.release();
			}
		};
	}

	// Default thread safe scheduler implementation
	struct Scheduler
	{
		using event_t = impl::Event;
		using event_ref_t = impl::EventRef;
		using event_continuation_t = impl::EventContinuation;
		using event_queue_t = pr::container::ConcurrentQueue<event_continuation_t>;
		using event_ref_container_t = std::vector<event_ref_t>;
		using continuations_container_t = std::vector<event_continuation_t>;
		using cpu_work_queues_container_t = std::deque<impl::WorkQueue>;
		using EEventStatus = impl::WaitResult::EStatus;

		// Event processing thread used to wait for signals from other threads
		std::thread m_event_thread;

		//
		event_queue_t m_pending_events;

		// Continuation points in coroutines that are waiting on events.
		// 'm_continuations' and 'm_events' are parallel containers, the event in [i] corresponds to the continuation in [i]
		// 'm_continuations[0]' is a dummy value, as the event at index 0 is reserved for the main thread signaler
		continuations_container_t m_continuations;

		// Events for continuations.
		// [0] is reserved for the main thread signaler.
		event_ref_container_t m_events;

		// Flag to signal the event processing thread to stop
		std::atomic<bool> m_shutdown;

		// A queue of jobs attributed to each CPU
		cpu_work_queues_container_t m_cpu_queues;

		// PRNG state, used to perform a low-discrepancy selection of a work queue to enqueue a coroutine to
		std::atomic<int> m_rng_queue_selector;

		// An event used to signal to the event thread that there are pending events to process (or that 'm_shutdown' is signaled)
		event_t m_event_thread_signal;

		// This is the "logical" CPU count, equal to the number of concurrent threads possible
		int m_cpu_count;

		// Default global thread safe scheduler
		static Scheduler& instance() noexcept
		{
			static Scheduler s_scheduler;
			return s_scheduler;
		}

		Scheduler()
			: m_event_thread()
			, m_pending_events()
			, m_continuations()
			, m_events()
			, m_shutdown()
			, m_cpu_queues()
			, m_rng_queue_selector(std::rand())
			, m_event_thread_signal(false, "main_event")
			, m_cpu_count(std::thread::hardware_concurrency())
		{
			//LOG("Spawning coop scheduler with %i threads\n", m_cpu_count);
			assert(m_cpu_count > 0 && m_cpu_count <= 64 && "More than 64 cores not yet supported");

			// Create work queues for each CPU
			for (auto i = 0; i != m_cpu_count; ++i)
				m_cpu_queues.emplace_back(i);

			// Initialize room for 32 events
			m_events.reserve(32);
			m_continuations.reserve(32);

			// Add the special event is for the main thread signaler
			m_events.push_back(m_event_thread_signal);
			m_continuations.push_back({});

			// Start the thread that waits on event signals
			m_event_thread = std::thread([this]
			{
				for (; !m_shutdown;)
				{
					auto [status, index] = event_t::wait_many(m_events);
					if (status == EEventStatus::Failed || status == EEventStatus::Timeout)
						continue;

					// The 'm_event[0]' is used to indicate events are pending, or that 'm_shutdown' is signaled
					if (index == 0)
					{
						if (m_shutdown)
							return;

						// Dequeue continuation requests from the concurrent queue in bulk
						auto size = m_pending_events.size_approx();

						// Note that the number of items we actually dequeue may be more than originally advertised
						auto ofs = m_continuations.size();
						m_continuations.resize(ofs + size);
						size = m_pending_events.try_dequeue_bulk(m_continuations.data() + ofs, m_continuations.size() - ofs);
						m_continuations.resize(ofs + size);

						// Add the events from the continuations to the events collection
						for (size_t i = 0; i != size; ++i)
							m_events.push_back(m_continuations[ofs + i].m_event);

						//LOG("Added %zu events to the event processing thread\n", size);
					}
					else
					{
						//LOG("Event %i signaled on the event processing thread\n", index);

						// An event has been signaled. Enqueue its associated continuation.
						auto& continuation = m_continuations[index];
						schedule(continuation.m_coroutine, continuation.m_cpu_affinity, continuation.m_priority);

						// NOTE: if this event was the only event in the queue (aside from the thread signaler),
						// these swaps are in-place swaps and thus no-ops
						std::swap(m_continuations[index], m_continuations.back());
						std::swap(m_events[index], m_events.back());
						m_continuations.pop_back();
						m_events.pop_back();
					}
				}
			});
		}
		~Scheduler() noexcept
		{
			// Shutdown the event thread
			m_shutdown = true;
			m_events[0].signal();
			if (m_event_thread.joinable())
				m_event_thread.join();
		}

		// Schedule a coroutine continuation
		void schedule(std::coroutine_handle<> coroutine, uint64_t cpu_affinity = 0, uint32_t priority = 0, source_location_t source_location = {}) noexcept
		{
			// Use default CPU affinity if none is provided
			const uint64_t cpu_mask = ~0ULL >> (64 - m_cpu_count);
			cpu_affinity = cpu_affinity ? cpu_affinity  : (~cpu_affinity & cpu_mask);

			// Find an idle queue to enqueue the coroutine
			for (auto i = 0; i != m_cpu_count; ++i)
			{
				// Skip this CPU if it's not in the affinity mask
				if ((cpu_affinity & (1ULL << i)) == 0)
					continue;
				
				// Not idle
				if (m_cpu_queues[i].size_approx() != 0)
					continue;

				///LOG("Empty work queue %i identified\n", i);
				m_cpu_queues[i].enqueue(coroutine, priority, source_location);
				return;
			}

			// Otherwise, all queues are busy. Pick a random one with reasonably low discrepancy (Kronecker recurrence sequence)
			auto index = static_cast<int>(m_rng_queue_selector++ * std::numbers::phi_v<float>) % std::popcount(cpu_affinity);

			// Iteratively unset bits to determine the nth set bit
			for (auto i = 0; i != index; ++i)
				cpu_affinity &= cpu_affinity - 1ULL;

			// Add the continuation to the selected queue
			auto queue = std::countr_zero(cpu_affinity);
			m_cpu_queues[queue].enqueue(coroutine, priority, source_location);

			//LOG("Work queue %i identified\n", queue);
		}
		void schedule(std::coroutine_handle<> coroutine, event_ref_t event, uint64_t cpu_affinity, uint32_t priority)
		{
			m_pending_events.enqueue({ coroutine, event, cpu_affinity, priority });
			m_events[0].signal();
		}
	};

	// Suspend the current coroutine to be scheduled for execution on a different thread by the supplied scheduler.
	inline auto suspend(uint64_t cpu_mask = 0, uint32_t priority = 0, source_location_t source_location = source_location_t::current()) noexcept
	{
		// Remember to `co_await` this function's returned value.
		// The least significant bit of the CPU mask, corresponds to CPU 0.
		// A non-zero mask will prevent this coroutine from being scheduled on CPUs corresponding to bits that are set.
		// Thread-Safe only if 'S::schedule()' is thread safe (the default one provided is thread safe).

		struct awaiter_t
		{
			Scheduler& scheduler;
			uint64_t cpu_mask;
			uint32_t priority;
			source_location_t source_location;

			bool await_ready() const noexcept
			{
				return false;
			}
			void await_suspend(std::coroutine_handle<> coroutine) const noexcept
			{
				scheduler.schedule(coroutine, cpu_mask, priority, source_location);
			}
			void await_resume() const noexcept
			{
			}
		};
		return awaiter_t{ Scheduler::instance(), cpu_mask, priority, source_location };
	}

	// Wait for all tasks to complete
	template<typename... Tasks>
	Task<void> WhenAll(Tasks&... tasks)
	{
		(co_await tasks, ...);
		co_return;
	}

	// Inline implementation
	namespace impl
	{
		inline void Event::await_suspend(std::coroutine_handle<> coroutine) noexcept
		{
			// Enqueue coroutine for resumption when this event transitions to the signaled state
			Scheduler::instance().schedule(coroutine, ref(), m_cpu_affinity, m_priority);
		}
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

		Task<int> Job0(CancelToken cancel)
		{
			// Suspend execution of the function. The next line will run on a different thread
			co_await coroutine::suspend();

			millis busy_time(10);

			int i = 0;
			for (; !cancel.Wait(busy_time); ++i)
			{}

			co_return i;
		}
		Task<int> Job1(CancelToken cancel)
		{
			auto task0 = Job0(cancel);
			auto task1 = Job0(cancel);
			auto task2 = Job0(cancel);

			co_await WhenAll(task0, task1, task2);
			co_return *task0 + *task1 + *task2;
		}
		Task<int, true> Main(CancelToken cancel)
		{
			auto sum = co_await Job1(cancel);
			co_return sum;
		}
	}

	PRUnitTest(CoroutineTests)
	{
		using namespace tests;
		auto main_thread_id = std::this_thread::get_id();

		{
			int fib[] = { 0, 1, 1, 2, 3, 5, 8, 13, 21, 34 }, i = 0;
			for (auto f : Fibonacci(10))
				PR_EXPECT(f == fib[i++]);
		}

		{
			CancelTokenSource cts;
			auto token = cts.Token();

			auto job = Main(token);

			// Block for a while
			token.Wait(millis(100));
			cts.Cancel();

			job.join();

			PR_EXPECT(std::this_thread::get_id() == main_thread_id);
			PR_EXPECT(*job > 0);
		}

	}
}
#endif