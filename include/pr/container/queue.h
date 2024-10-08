//******************************************
// A very simple queue class
//  Copyright (c) Rylogic Ltd 2007
//******************************************
#pragma once

#include <algorithm>
#include <cassert>

namespace pr
{
	template <typename Type, std::size_t Count>
	struct Queue
	{
		enum { Capacity = Count, Wrap = Count + 1 }; // Plus 1 for the end iterator (this is basically a ring buffer)
		Type m_queue[Wrap];
		int  m_begin, m_end;

		Queue() :m_begin(0) ,m_end(0) {}
		static int  incr(int i)                            { return (i + 1       ) % Wrap; }
		static int  decr(int i)                            { return (i - 1 + Wrap) % Wrap; }
		static int  incr(int i, int by)                    { return (i + (by%Wrap)       ) % Wrap; }
		static int  decr(int i, int by)                    { return (i - (by%Wrap) + Wrap) % Wrap; }
		static int  size(int begin, int end)               { return ((end - begin) + Wrap) % Wrap; }
		bool        empty() const                          { return m_end == m_begin; }
		bool        full() const                           { return incr(m_end) == m_begin; }
		std::size_t size() const                           { return size(m_begin, m_end); }
		std::size_t capacity() const                       { return Capacity; }
		Type const& back() const                           { assert(!empty()); return m_queue[decr(m_end)]; }
		Type&       back()                                 { assert(!empty()); return m_queue[decr(m_end)]; }
		Type const& front() const                          { assert(!empty()); return m_queue[m_begin]; }
		Type&       front()                                { assert(!empty()); return m_queue[m_begin]; }
		void        push_back(Type const& elem)            { assert(!full());  m_queue[m_end] = elem; m_end = incr(m_end); }
		void        push_front(Type const& elem)           { assert(!full());  m_begin = decr(m_begin); m_queue[m_begin] = elem; }
		Type        pop_back()                             { assert(!empty()); m_end = decr(m_end); return m_queue[m_end]; }
		Type        pop_front()                            { assert(!empty()); int begin = m_begin; m_begin = incr(m_begin); return m_queue[begin]; }
		Type const& operator[](int i) const                { assert(i >= 0 && i < (int)size()); return m_queue[incr(m_begin,i)]; }
		Type&       operator[](int i)                      { assert(i >= 0 && i < (int)size()); return m_queue[incr(m_begin,i)]; }
		void        push_back_overwrite(Type const& elem)  { if (full()) {m_begin = incr(m_begin);} push_back(elem); }
		void        push_front_overwrite(Type const& elem) { if (full()) {m_end   = decr(m_end);}   push_front(elem); }
		void        queue(Type const& elem)                { push_back(elem); }
		Type        dequeue()                              { return pop_front(); }
		void        canonicalise()
		{
			if (m_end < m_begin)
			{
				::memmove_s(&m_queue[m_end], (Wrap - m_end) * sizeof(Type), &m_queue[m_begin], (Wrap - m_begin) * sizeof(Type));
				std::rotate(&m_queue[0], &m_queue[m_end], &m_queue[m_end - m_begin + Wrap]);
			}
			else
			{
				::memmove_s(&m_queue[0], (Wrap) * sizeof(Type), &m_queue[m_begin], (m_end - m_begin) * sizeof(Type));
			}
			m_end = size(m_begin, m_end);
			m_begin = 0;
		}
	};
}
