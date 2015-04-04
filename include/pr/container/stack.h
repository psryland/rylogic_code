//******************************************
// A very simple stack class
//  Copyright (c) Rylogic Ltd 2007
//******************************************

#pragma once
#include <cassert>

namespace pr
{
	template <typename Type, std::size_t Capacity>
	struct Stack
	{
		Type        m_stack[Capacity];
		std::size_t m_size; // Using an index makes this type copyable
		
		Stack() :m_size(0)                 {}
		bool        empty() const          { return m_size == 0; }
		bool        full() const           { return m_size == capacity(); }
		std::size_t size() const           { return m_size; }
		std::size_t capacity() const       { return Capacity; }
		Type const* begin() const          { return m_stack; }
		Type*       begin()                { return m_stack; }
		Type const* end() const            { return m_stack + m_size; }
		Type*       end()                  { return m_stack + m_size; }
		void        clear()                { m_size = 0; }
		void        push(Type const& node) { assert(!full() ); m_stack[m_size++] = node; }
		Type        pop()                  { assert(!empty()); return m_stack[--m_size]; }
		Type        top() const            { assert(!empty()); return m_stack[m_size-1]; }
		Type&       top()                  { assert(!empty()); return m_stack[m_size-1]; }
	};

	template <typename Word = unsigned int>
	struct BitStack
	{
		Word        m_bits;
		std::size_t m_size;
		
		struct BitProxy
		{
			BitStack<Word>* m_bitstack;
			BitProxy(BitStack<Word>* bitstack) :m_bitstack(bitstack) {}
			void operator = (bool flag)  { m_bitstack->pop(); m_bitstack->push(flag); }
			operator bool() const        { return const_cast<BitStack const*>(m_bitstack)->top(); }
		};
		
		BitStack() :m_size(0) ,m_bits(0) {}
		bool        empty() const        { return m_size == 0; }
		bool        full() const         { return m_size == capacity(); }
		std::size_t size() const         { return m_size; }
		std::size_t capacity() const     { return sizeof(Word)*8; }
		void        clear()              { m_size = 0; m_bits = 0; }
		void        push(bool flag)      { assert(!full() ); m_bits <<= 1; m_bits |= Word(flag); ++m_size; }
		bool        pop()                { assert(!empty()); bool flag = top(); m_bits >>= 1; --m_size; return flag; }
		bool        top() const          { assert(!empty()); return (m_bits & 1) != 0; }
		BitProxy    top()                { assert(!empty()); return BitProxy(this); }
	};
}
