//*********************************************************************************
// Segmented List
//  Copyright (C) Rylogic 2002
//*********************************************************************************
#pragma once

namespace pr
{
	template <std::size_t NumSegments, typename ListClass>
	class segmented_list
	{
	public:
		typedef typename ListClass::iterator							  iterator;
		typedef typename ListClass::const_iterator					const_iterator;
		typedef typename ListClass::reverse_iterator			  reverse_iterator;
		typedef typename ListClass::const_reverse_iterator	const_reverse_iterator;

		typedef typename ListClass::value_type							value_type;
		typedef typename ListClass::size_type							 size_type;
		typedef typename ListClass::reference							 reference;
		typedef typename ListClass::const_reference				   const_reference;

		static const std::size_t num_segments = NumSegments;

	public:
		segmented_list()
		{
			iterator begin = m_list.begin();
			for( std::size_t s = 0; s <= NumSegments; ++s ) m_iter[s] = begin;
		}

		// Container size
		size_type	size    () const				{ return m_list.size(); }
		size_type	size    (std::size_t seg) const { return std::distance(m_iter[seg], m_iter[seg + 1]); }
		bool		empty   () const				{ return m_list.empty(); }
		bool		empty	(std::size_t seg) const { return m_iter[seg] == m_iter[seg + 1]; }
		size_type	max_size() const				{ return m_list.max_size(); }

		// Forward iterator retrieval
		      iterator begin()						{ return m_list.begin(); }
		const_iterator begin() const				{ return m_list.begin(); }
		      iterator end  ()						{ return m_list.end(); }
		const_iterator end  () const				{ return m_list.end(); }
		      iterator begin(std::size_t seg)       { return m_iter[seg]; }
		const_iterator begin(std::size_t seg) const { return m_iter[seg]; }
		      iterator end  (std::size_t seg)       { return m_iter[seg + 1]; }
		const_iterator end  (std::size_t seg) const { return m_iter[seg + 1]; }

		// Reverse iterator retrieval
		      reverse_iterator rbegin()						 { return       reverse_iterator(end  ()); }
		const_reverse_iterator rbegin() const				 { return const_reverse_iterator(end  ()); }
		      reverse_iterator rend  ()						 { return       reverse_iterator(begin()); }
		const_reverse_iterator rend  () const				 { return const_reverse_iterator(begin()); }
		      reverse_iterator rbegin(std::size_t seg)		 { return       reverse_iterator(end  (seg)); }
		const_reverse_iterator rbegin(std::size_t seg) const { return const_reverse_iterator(end  (seg)); }
		      reverse_iterator rend  (std::size_t seg)		 { return       reverse_iterator(begin(seg)); }
		const_reverse_iterator rend  (std::size_t seg) const { return const_reverse_iterator(begin(seg)); }

		// Front and back element access
		      reference front()    				     { return m_list.front(); }
		const_reference front() const				 { return m_list.front(); }
			  reference back ()      				 { return m_list.back(); }
		const_reference back () const				 { return m_list.back(); }
		      reference front(std::size_t seg)       { return *m_iter[seg]; }
		const_reference front(std::size_t seg) const { return *m_iter[seg]; }
			  reference back (std::size_t seg)       { iterator tmp(m_iter[seg + 1]); --tmp; return *tmp; }
		const_reference back (std::size_t seg) const { iterator tmp(m_iter[seg + 1]); --tmp; return *tmp; }

		// Front insertion/deletion
		void push_front(std::size_t seg, const value_type& val)	{ insert(seg, m_iter[seg], val); }
		void pop_front(std::size_t seg)							{ erase(seg, m_iter[seg]); }

		// Back insertion/deletion
		void push_back(std::size_t seg, const value_type& val)	{ insert(seg, m_iter[seg + 1], val); }
		void pop_back(std::size_t seg)							{ iterator tmp = m_iter[seg + 1]; --tmp; erase(seg, tmp); }

		// Assignment
		segmented_list& operator=(const segmented_list& rhs)
		{
			m_list = rhs.m_list;

			m_iter[0] = begin();
			for( std::size_t s = 1; s <= NumSegments; ++s )
			{
				m_iter[s] = m_iter[s - 1];
				std::advance(m_iter[s], rhs.size(s - 1));
			}
			return *this;
		}

		// Element insertion
		iterator insert(std::size_t seg, iterator at, const value_type& val)
		{
			iterator new_iter = m_list.insert(at, val);
			adjust_iterators(seg, at, new_iter);
			return new_iter;
		}

		// Element removal
		void clear()
		{
			m_list.clear();
			
			iterator begin = m_list.begin();
			for( std::size_t s = 0; s <= NumSegments; ++s ) m_iter[s] = begin;
		}
		void clear(std::size_t seg)
		{
			if( !empty(seg) ) adjust_iterators(seg, m_iter[seg], m_list.erase(m_iter[seg], m_iter[seg + 1]));
		}
		iterator erase(std::size_t seg, iterator at)
		{
			iterator new_iter = m_list.erase(at);
			adjust_iterators(seg, at, new_iter);
			return new_iter;
		}
		void remove(const value_type& val)
		{
			for( std::size_t s = 0; s < NumSegments; ++s ) remove(s, val);
		}
		void remove(std::size_t seg, const value_type& val)
		{
			iterator i    = begin(seg);
			iterator endi = end  (seg);
			while (i != endi)
			{
				if (*i == val)
				{
					erase(seg, i++);
				}
				else
				{
					++i;
				}
			}
		}
		template <typename Predicate>
		void remove_if(Predicate pred)
		{
			for (std::size_t s = 0; s < NumSegments; ++s)
				remove_if(s, pred);
		}
		template <typename Predicate>
		void remove_if(std::size_t seg, Predicate pred)
		{
			iterator i    = begin(seg);
			iterator endi = end  (seg);
			while (i != endi)
			{
				if (pred(*i))
				{
					erase(seg, i++);
				}
				else
				{
					++i;
				}
			}
		}

	private:
		void adjust_iterators(std::size_t seg, const iterator& old_iter, const iterator& new_iter)
		{
			for( std::ptrdiff_t s = seg; s >= 0; --s )
			{
				if( m_iter[s] == old_iter ) m_iter[s] = new_iter;
				else break;
			}
		}

	private:
		ListClass m_list;
		iterator m_iter[NumSegments + 1];
	};
}

