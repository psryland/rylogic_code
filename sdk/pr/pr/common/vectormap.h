//***********************************************************
// Vector Map
//  Copyright © Rylogic Ltd 2011
//***********************************************************
// An stl-like map implemented using std::vector-esk container
// Note: it is not a replacement for std::map because it doesn't
// have the same invalidation rules. It's really just an ordered vector
	
#ifndef PR_COMMON_VECTOR_MAP_H
#define PR_COMMON_VECTOR_MAP_H
	
#include <utility>
#include <algorithm>
#include <vector>
	
namespace pr
{
	template < typename Key, typename Type, typename Vec = std::vector< std::pair<Key,Type> > >
	struct vec_map
	{
		typedef typename std::pair<Key, Type> Elem;
		typedef typename Vec                  Cont;
		typedef typename Cont::const_iterator const_iterator;
		typedef typename Cont::iterator       iterator;
		Cont m_cont;
		
		struct pred
		{
			bool operator()(Elem const& lhs, Elem const& rhs) const	{ return lhs.first < rhs.first; }
			bool operator()(Elem const& lhs, Key  const& rhs) const	{ return lhs.first < rhs; }
			bool operator()(Key  const& lhs, Elem const& rhs) const	{ return lhs       < rhs.first; }
		};
		
		bool            empty() const   { return m_cont.empty(); }
		void            clear()         { m_cont.clear(); }
		std::size_t     size() const    { return m_cont.size(); }
		const_iterator  begin() const   { return m_cont.begin(); }
		iterator        begin()         { return m_cont.begin(); }
		const_iterator  end() const     { return m_cont.end(); }
		iterator        end()           { return m_cont.end(); }
		
		const_iterator find(Key const& key) const
		{
			Cont::const_iterator iter = std::lower_bound(m_cont.begin(), m_cont.end(), key, pred());
			if (iter == m_cont.end()) return end();
			return &*iter;
		}
		iterator find(Key const& key)
		{
			Cont::iterator iter = std::lower_bound(m_cont.begin(), m_cont.end(), key, pred());
			if (iter == m_cont.end()) return end();
			return &*iter;
		}
		
		Type& operator[](Key const& key)
		{
			Cont::iterator iter = std::lower_bound(m_cont.begin(), m_cont.end(), key, pred());
			if (iter == m_cont.end() || !(iter->first == key)) iter = m_cont.insert(iter, Elem(key, Type()));
			return iter->second;
		}
	};
}
	
#endif
