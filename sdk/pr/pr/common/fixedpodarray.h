//*************************************
// POD Array
//  Copyright © Rylogic Ltd 2009
//*************************************

#ifndef PR_FIXED_POD_ARRAY_H
#define PR_FIXED_POD_ARRAY_H

#include "pr/common/assert.h"

namespace pr
{
	#define PR_CHK_ROOM(s) PR_ASSERT(PR_DBG, (s) <= max_size(), "")
	#define PR_CHK_ITER    PR_ASSERT(PR_DBG, begin() <= where && where < end(), "")
	#define PR_CHK_IDX     PR_ASSERT(PR_DBG, 0 <= i && i < m_count, "")

	template <typename Type, unsigned int Capacity> class pod_array
	{
		Type m_array[Capacity];
		std::size_t m_count;

	public:
		typedef const Type*		const_iterator;
		typedef       Type*		iterator;
		typedef	const Type*		const_pointer;
		typedef	const Type&		const_reference;
		typedef	Type*			pointer;
		typedef	Type&			reference;
		typedef	Type			value_type;
		typedef	std::ptrdiff_t	difference_type;
		typedef	std::size_t		size_type;

		pod_array() : m_count(0) {}

		const_iterator	begin()	const	{ return m_array; }
		      iterator	begin()			{ return m_array; }
		const_iterator	end	 ()	const	{ return m_array + m_count; }
		      iterator	end	 ()			{ return m_array + m_count; }
		const Type&		front()	const	{ return m_array[0]; }
		      Type&		front()			{ return m_array[0]; }
		const Type&		back ()	const	{ return m_array[m_count - 1]; }
		      Type&		back ()			{ return m_array[m_count - 1]; }

		const Type&		at(int i) const								{ PR_CHK_IDX; return m_array[i]; }
			  Type&		at(int i)									{ PR_CHK_IDX; return m_array[i]; }
		bool			empty()	const								{ return m_count == 0; }
		bool			full() const								{ return m_count == Capacity; }
		std::size_t		capacity() const							{ return Capacity; }
		void			clear()										{ m_count = 0; }
		iterator		insert(iterator where, const Type& val)		{ PR_CHK_ITER; PR_CHK_ROOM; std::memmove(where + 1, where, (end() - where    ) * sizeof(Type)); *where = val;	++m_count; return where; }
		iterator		erase(iterator where)						{ PR_CHK_ITER;				std::memmove(where, where + 1, (end() - where - 1) * sizeof(Type));					--m_count; return where; }
		std::size_t		max_size() const							{ return Capacity; }
		void			resize(std::size_t size)					{ PR_CHK_ROOM(size); m_count = size; }
		void			resize(std::size_t size, const Type& val)	{ PR_CHK_ROOM(size); if(size < m_count) {m_count = size;} else {for(pointer i = m_array+m_count, i_end = m_array+size; i !+ i_end; ++i) { *i = val; }} }
		std::size_t		size() const								{ return m_count; }
		void			push_back(const Type& val)					{ PR_CHK_ROOM(m_count + 1); m_array[m_count] = val; ++m_count; }
		void			pop_back()									{ --m_count; }

		const Type&		operator [] (std::size_t i) const			{ PR_CHK_IDX; return m_array[i]; }
		      Type&		operator [] (std::size_t i)					{ PR_CHK_IDX; return m_array[i]; }
	};

	#undef PR_CHK_ROOM
	#undef PR_CHK_ITER
	#undef PR_CHK_IDX

}

#endif
