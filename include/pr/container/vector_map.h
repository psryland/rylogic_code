//***********************************************************
// Vector Map
//  Copyright (c) Rylogic Ltd 2011
//***********************************************************
// An stl-like map implemented using std::vector-esk container
// Note: it is not a replacement for std::map because it doesn't
// have the same invalidation rules. It's really just an ordered vector

#pragma once

#include <utility>
#include <algorithm>
#include <vector>

namespace pr
{
	// Implements a std::map like interface using a contiguous container
	// *Careful* invalidation rules are not the same as for std::map
	template <typename Key, typename Type, typename Vec = std::vector<std::pair<Key,Type>>>
	struct vector_map
	{
		typedef typename vector_map<Key,Type,Vec> this_type;
		typedef typename std::pair<Key, Type> value_type;
		typedef Vec cont_type;
		typedef Key key_type;
		typedef Type mapped_type;

		Vec m_cont;

		struct pred
		{
			bool operator()(value_type const& lhs, value_type const& rhs) const { return lhs.first < rhs.first; }
			bool operator()(value_type const& lhs, key_type const& rhs) const   { return lhs.first < rhs; }
			bool operator()(key_type const& lhs, value_type const& rhs) const   { return lhs       < rhs.first; }
		};

		bool        empty() const                                 { return size() == 0; }
		std::size_t size() const                                  { return end() - begin(); }
		auto        begin() const -> decltype(std::begin(m_cont)) { return std::begin(m_cont); }
		auto        begin()       -> decltype(std::begin(m_cont)) { return std::begin(m_cont); }
		auto        end() const   -> decltype(std::end(m_cont))   { return std::end(m_cont); }
		auto        end()         -> decltype(std::end(m_cont))   { return std::end(m_cont); }
		
		void clear()
		{
			m_cont.clear();
		}

		// const reference to value at key
		Type const& at(Key const& key) const
		{
			auto iter = Iter(key);
			if (iter == end() || !(iter->first == key)) throw std::exception("key not found");
			return iter->second;
		}

		// const iterator to match for 'key' or end()
		auto find(Key const& key) const -> decltype(begin())
		{
			auto iter = Iter(key);
			if (iter == end() || !(iter->first == key)) return end();
			return iter;
		}

		// iterator to match for 'key' or end()
		auto find(Key const& key) -> decltype(begin())
		{
			auto iter = Iter(key);
			if (iter == end() || !(iter->first == key)) return end();
			return iter;
		}

		// const reference to value at 'key'. Careful with this reference, it may be invalidated by other inserts
		Type const& operator[](Key const& key) const
		{
			return at(key);
		}

		// mutable reference to value at 'key'. Careful with this reference, it may be invalidated by other inserts
		Type& operator[](Key const& key)
		{
			auto iter = Iter(key);
			if (iter == end() || !(iter->first == key)) iter = m_cont.insert(iter, value_type(key, Type()));
			return iter->second;
		}

	private:
		// return an iterator to where 'key' would be inserted
		auto Iter(Key const& key) const -> decltype(begin())
		{
			return std::lower_bound(begin(), end(), key, pred());
		}
		auto Iter(Key const& key) -> decltype(begin())
		{
			return std::lower_bound(begin(), end(), key, pred());
		}
	};
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/container/vector.h"
namespace pr
{
	namespace unittests
	{
		namespace vector_map
		{
			struct Thing
			{
				int m_id;
				Thing() :m_id() {}
				Thing(int id) :m_id(id) {}
			};
		}
		PRUnitTest(pr_vector_map)
		{
			using namespace pr::unittests::vector_map;
			typedef pr::vector<std::pair<int, Thing>, 10, true> fixed_buffer;
			typedef pr::vector_map<int, Thing, fixed_buffer> Map;

			Map map;

			map[3] = Thing(3);
			map[1] = Thing(1);
			map[9] = Thing(9);

			PR_CHECK(!map.empty(), true);
			PR_CHECK(map.size(), 3U);
			PR_CHECK(map[9].m_id, 9);
			PR_CHECK(map[1].m_id, 1);
			PR_CHECK(map[3].m_id, 3);

			map.clear();
			PR_CHECK(map.empty(), true);
		}
	}
}
#endif
