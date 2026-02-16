//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
// Double-buffered state snapshot for safe Step → Render data handoff.
// Step tasks acquire a write lock via Lock(), which returns an RAII
// guard providing mutable access to the back buffer. On destruction,
// the guard swaps the back buffer to become the new front buffer.
// Render tasks read a const view of the front buffer via Read().
#pragma once
#include <atomic>
#include <cassert>
#include <utility>

namespace las
{
	template <typename T>
	struct StateSnapshot
	{
		// RAII write guard. Provides mutable access to the back buffer
		// and commits (swaps) on destruction. Only one lock at a time.
		struct WriteLock
		{
			WriteLock(StateSnapshot& owner)
				: m_owner(owner)
			{
				assert(!m_owner.m_writing && "StateSnapshot: concurrent writes detected");
				m_owner.m_writing = true;
			}
			WriteLock(WriteLock const&) = delete;
			WriteLock& operator =(WriteLock const&) = delete;
			WriteLock(WriteLock&& rhs) noexcept
				: m_owner(rhs.m_owner)
			{
				rhs.m_moved = true;
			}
			~WriteLock()
			{
				if (m_moved)
					return;

				// Swap the back buffer to become the new front buffer
				auto front = m_owner.m_front.load(std::memory_order_acquire);
				m_owner.m_front.store(1 - front, std::memory_order_release);
				m_owner.m_writing = false;
			}

			T& operator *() { return m_owner.m_buffers[1 - m_owner.m_front.load(std::memory_order_acquire)]; }
			T* operator ->() { return &**this; }

		private:

			StateSnapshot& m_owner;
			bool m_moved = false;
		};

		// Construct with default-constructed state in both buffers
		StateSnapshot()
			: m_buffers{}
			, m_front(0)
			, m_writing(false)
		{}

		// Construct with initial state copied to both buffers
		explicit StateSnapshot(T const& initial)
			: m_buffers{initial, initial}
			, m_front(0)
			, m_writing(false)
		{}

		// Read access: returns a const reference to the front buffer.
		// Safe to call from Render tasks while Step writes to the back buffer.
		T const& Read() const
		{
			return m_buffers[m_front.load(std::memory_order_acquire)];
		}

		// Acquire a write lock on the back buffer. The returned guard
		// provides mutable access and commits the snapshot on destruction.
		[[nodiscard]] WriteLock Lock()
		{
			return WriteLock(*this);
		}

	private:

		friend struct WriteLock;

		T m_buffers[2];
		std::atomic<int> m_front;
		bool m_writing; // Debug-only single-writer guard
	};
}
