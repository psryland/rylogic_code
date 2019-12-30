//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include <unordered_map>
#include "pr/view3d/forward.h"

namespace pr::rdr
{
	// A generator for an unordered map with a custom allocator
	// Use:
	//   typedef Lookup<int, char>::type CharLookup;
	template <typename Key, typename Value>
	struct LookupGenerator
	{
		using pair   = std::pair<const Key, Value>;
		using hasher = std::hash<Key>;
		using keyeq  = std::equal_to<Key>;
		using alloc  = Allocator<pair>;

		using type = std::unordered_map<Key, Value, hasher, keyeq, alloc>;
	};

	template <typename Key, typename Value>
	struct Lookup :LookupGenerator<Key,Value>::type
	{
		using generator = LookupGenerator<Key, Value>;
		using base = typename generator::type;
		using pair = typename generator::pair;

		Lookup()
			:base(8, generator::hasher(), generator::keyeq(), generator::alloc())
		{}
	};

	#if 0 // std::vector based implementation
	template <typename Key, typename Value>
	struct Lookup
	{
		struct Pair
		{
			Key first;
			Value second;

			Pair() = default;
			Pair(Key k, Value v)
				:first(k)
				,second(v)
			{}
		};
		using pair = Pair;//std::pair<const Key, Value>;
		using container_t = std::vector<pair>;
		using const_iterator = typename container_t::const_iterator;
		using iterator = typename container_t::iterator; 
		using citer_t = const_iterator;
		using miter_t = iterator;
			
		container_t m_table;

		Lookup()
		{}

		citer_t begin() const
		{
			return std::begin(m_table);
		}
		miter_t begin()
		{
			return std::begin(m_table);
		}
		citer_t end() const
		{
			return std::end(m_table);
		}
		miter_t end()
		{
			return std::end(m_table);
		}

		bool empty() const
		{
			return m_table.empty();
		}
		size_t size() const
		{
			return m_table.size();
		}
		size_t count(Key const& key) const
		{
			return find(key) != end() ? 1 : 0;
		}
		citer_t find(Key const& key) const
		{
			return std::find_if(begin(), end(), [=](auto const& p){ return p.first == key; });
		}
		miter_t find(Key const& key)
		{
			return std::find_if(begin(), end(), [=](auto const& p){ return p.first == key; });
		}
		miter_t erase(citer_t iter)
		{
			return m_table.erase(iter);
		}
		miter_t erase(Key const& key)
		{
			return erase(find(key));
		}
		miter_t insert(miter_t iter, pair const& p)
		{
			return m_table.insert(iter, p);
		}

		Value const& operator [](Key const& key) const
		{
			auto iter = find(key);
			if (iter == end()) throw std::runtime_error("no found");
			return iter->second;
		}
		Value& operator [](Key const& key)
		{
			auto iter = find(key);
			if (iter != end())
				return iter->second;
				
			m_table.push_back(pair(key, Value()));
			return m_table.back().second;
		}
	};
	#endif
}

