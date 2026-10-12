//*********************************************
// Vector Hash Map
//  Copyright (c) Rylogic Ltd 2026
//*********************************************
// An stl-like map that stores its elements contiguously in insertion order, with a hash index for constant-time lookup.
// Note: it is not a replacement for std::unordered_map because it has vector-like invalidation rules and does not support erasing elements.
#pragma once
#include <algorithm>
#include <bit>
#include <cstdint>
#include <functional>
#include <stdexcept>
#include <tuple>
#include <utility>
#include <vector>

namespace pr
{
	// Implements a std::unordered_map like interface using a contiguous container
	// *Careful* invalidation rules are the same as for 'std::vector', not std::unordered_map.
	// Elements are iterated in insertion order. The keys of elements must not be modified through iterators.
	// 'clear()' keeps the allocated capacity so that a map refilled with a similar set of keys does not allocate.
	template <typename Key, typename Type, typename Hash = std::hash<Key>, typename KeyEqual = std::equal_to<Key>>
	struct vector_hash_map
	{
		// Notes:
		//  - 'm_cont' holds the elements in insertion order.
		//  - 'm_index' is an open-addressing hash table with linear probing. Each slot holds an element index plus one, so zero marks an empty slot.
		//  - The index size is a power of two and is at least twice the element count. Probe sequences therefore stay short and always reach an empty slot.
		//  - Lookups use raw pointers rather than container iterators so that Debug builds do not pay for checked iterators.

		using this_type       = vector_hash_map<Key, Type, Hash, KeyEqual>;
		using key_type        = Key;
		using mapped_type     = Type;
		using value_type      = std::pair<Key, Type>;
		using container_type  = std::vector<value_type>;
		using size_type       = typename container_type::size_type;
		using difference_type = typename container_type::difference_type;
		using iterator        = typename container_type::iterator;
		using const_iterator  = typename container_type::const_iterator;
		using hasher          = Hash;
		using key_equal       = KeyEqual;

	private:

		static constexpr size_t MinIndexSize = 16;

		container_type m_cont;
		std::vector<uint32_t> m_index;
		Hash m_hash;
		KeyEqual m_equal;

	public:

		vector_hash_map()
			: m_cont()
			, m_index()
			, m_hash()
			, m_equal()
		{}
		explicit vector_hash_map(Hash hash, KeyEqual equal = KeyEqual())
			: m_cont()
			, m_index()
			, m_hash(std::move(hash))
			, m_equal(std::move(equal))
		{}

		bool empty() const
		{
			return m_cont.empty();
		}
		size_type size() const
		{
			return m_cont.size();
		}
		const_iterator begin() const
		{
			return m_cont.begin();
		}
		iterator begin()
		{
			return m_cont.begin();
		}
		const_iterator end() const
		{
			return m_cont.end();
		}
		iterator end()
		{
			return m_cont.end();
		}
		const_iterator cbegin() const
		{
			return begin();
		}
		const_iterator cend() const
		{
			return end();
		}
		value_type const* data() const
		{
			return m_cont.data();
		}
		value_type* data()
		{
			return m_cont.data();
		}

		// The number of elements that can be held without allocating
		size_type capacity() const
		{
			return std::min(m_cont.capacity(), m_index.size() / 2);
		}

		// Ensure 'count' elements can be held without allocating
		void reserve(size_type count)
		{
			// Grow both the elements and the index so neither allocates before 'count' elements are added
			m_cont.reserve(count);
			if (count * 2 > m_index.size())
				Rehash(count);
		}

		// Remove all elements, keeping the allocated capacity
		void clear()
		{
			// Empty every index slot. The index is small relative to the elements, so this is cheaper than allocating again.
			m_cont.clear();
			std::fill(m_index.begin(), m_index.end(), 0U);
		}

		// const reference to value at key
		Type const& at(Key const& key) const
		{
			auto i = Find(key);
			if (i == npos)
				throw std::out_of_range("key not found");

			return m_cont.data()[i].second;
		}

		// mutable reference to value at key
		Type& at(Key const& key)
		{
			auto i = Find(key);
			if (i == npos)
				throw std::out_of_range("key not found");

			return m_cont.data()[i].second;
		}

		// True if 'key' is in the map
		bool contains(Key const& key) const
		{
			return Find(key) != npos;
		}

		// const iterator to match for 'key' or end()
		const_iterator find(Key const& key) const
		{
			auto i = Find(key);
			return i != npos ? begin() + i : end();
		}

		// iterator to match for 'key' or end()
		iterator find(Key const& key)
		{
			auto i = Find(key);
			return i != npos ? begin() + i : end();
		}

		// const reference to value at 'key'. Throws if 'key' is not in the map.
		Type const& operator[](Key const& key) const
		{
			return at(key);
		}

		// mutable reference to value at 'key', default constructing the value if 'key' is not in the map. Careful with this reference, it may be invalidated by other inserts.
		Type& operator[](Key const& key)
		{
			return try_emplace(key).first->second;
		}
		Type& operator[](Key&& key)
		{
			return try_emplace(std::move(key)).first->second;
		}

		// Insert 'value' if its key is not in the map. Returns the element with the key, and true if it was inserted.
		std::pair<iterator, bool> insert(value_type const& value)
		{
			return try_emplace(value.first, value.second);
		}
		std::pair<iterator, bool> insert(value_type&& value)
		{
			return try_emplace(std::move(value.first), std::move(value.second));
		}

		// Construct a value from 'args' if 'key' is not in the map. Returns the element with the key, and true if it was inserted.
		template <typename K, typename... Args>
		std::pair<iterator, bool> try_emplace(K&& key, Args&&... args)
		{
			// Grow before probing so that the probe finds the slot that the new element will occupy
			if ((m_cont.size() + 1) * 2 > m_index.size())
				Rehash(m_cont.size() + 1);

			// Return the existing element if 'key' is already in the map
			auto slot = Probe(key);
			if (m_index[slot] != 0)
				return { begin() + (m_index[slot] - 1), false };

			// Append the new element and record its position in the empty slot
			m_cont.emplace_back(std::piecewise_construct, std::forward_as_tuple(std::forward<K>(key)), std::forward_as_tuple(std::forward<Args>(args)...));
			m_index[slot] = static_cast<uint32_t>(m_cont.size());
			return { end() - 1, true };
		}

	private:

		static constexpr size_t npos = ~size_t(0);

		// Return the index of the element with 'key', or 'npos' if 'key' is not in the map
		size_t Find(Key const& key) const
		{
			// An empty index means no elements have ever been added
			if (m_index.empty())
				return npos;

			auto slot = Probe(key);
			return m_index[slot] != 0 ? m_index[slot] - 1 : npos;
		}

		// Return the index slot that holds 'key', or the empty slot where 'key' would be added. The index must not be empty.
		template <typename K>
		size_t Probe(K const& key) const
		{
			// Step through slots from the key's hash position until the key or an empty slot is found
			auto const* index = m_index.data();
			auto const* values = m_cont.data();
			auto const mask = m_index.size() - 1;
			for (auto slot = static_cast<size_t>(m_hash(key)) & mask;; slot = (slot + 1) & mask)
			{
				auto i = index[slot];
				if (i == 0 || m_equal(values[i - 1].first, key))
					return slot;
			}
		}

		// Resize the index to hold at least 'count' elements, and add the existing elements to it
		void Rehash(size_type count)
		{
			// Keep the index at most half full so that probe sequences stay short
			m_index.assign(std::max(MinIndexSize, std::bit_ceil(count * 2)), 0U);
			auto* index = m_index.data();
			auto const* values = m_cont.data();
			auto const mask = m_index.size() - 1;
			for (size_t i = 0, iend = m_cont.size(); i != iend; ++i)
			{
				// Keys are unique, so each element goes in the first empty slot of its probe sequence
				auto slot = static_cast<size_t>(m_hash(values[i].first)) & mask;
				for (; index[slot] != 0; slot = (slot + 1) & mask) {}
				index[slot] = static_cast<uint32_t>(i + 1);
			}
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <string>
namespace pr::container
{
	PRUnitTestClass(VectorHashMapTests)
	{
		using Map = pr::vector_hash_map<int, std::string>;

		// A hash that sends every key to the same slot, so that lookups must step through collisions
		struct CollidingHash
		{
			size_t operator()(int) const
			{
				return 7;
			}
		};

		PRUnitTestMethod(LookupAndInsertionOrder, Quick)
		{
			Map map;
			map[3] = "three";
			map[1] = "one";
			map[9] = "nine";

			PR_EXPECT(!map.empty());
			PR_EXPECT(map.size() == 3U);
			PR_EXPECT(map.at(1) == "one");
			PR_EXPECT(map[9] == "nine");
			PR_EXPECT(map.contains(3));
			PR_EXPECT(!map.contains(2));
			PR_EXPECT(map.find(2) == map.end());
			PR_EXPECT(map.find(1)->second == "one");
			PR_EXPECT(map.begin()->first == 3);
			PR_EXPECT((map.begin() + 1)->first == 1);
			PR_EXPECT((map.begin() + 2)->first == 9);
		}

		PRUnitTestMethod(ConstLookupOnEmptyAndFilledMaps, Quick)
		{
			Map map;
			auto const& cmap = map;
			PR_EXPECT(cmap.find(1) == cmap.end());
			PR_EXPECT(!cmap.contains(1));
			PR_THROWS(cmap.at(1), std::out_of_range);

			map[1] = "one";
			PR_EXPECT(cmap.at(1) == "one");
			PR_EXPECT(cmap[1] == "one");
			PR_THROWS(cmap[2], std::out_of_range);
		}

		PRUnitTestMethod(InsertExistingKeyKeepsValue, Quick)
		{
			Map map;
			auto r0 = map.try_emplace(4, "four");
			PR_EXPECT(r0.second);
			PR_EXPECT(r0.first->second == "four");

			auto r1 = map.try_emplace(4, "FOUR");
			PR_EXPECT(!r1.second);
			PR_EXPECT(r1.first->second == "four");

			auto r2 = map.insert({ 4, "IV" });
			PR_EXPECT(!r2.second);
			PR_EXPECT(map.size() == 1U);
			PR_EXPECT(map.at(4) == "four");
		}

		PRUnitTestMethod(GrowthPreservesElements, Quick)
		{
			Map map;
			for (int i = 0; i != 1000; ++i)
				map.try_emplace(i * 7919, std::to_string(i));

			PR_EXPECT(map.size() == 1000U);
			for (int i = 0; i != 1000; ++i)
			{
				PR_EXPECT(map.at(i * 7919) == std::to_string(i));
				PR_EXPECT((map.begin() + i)->first == i * 7919);
			}
			PR_EXPECT(!map.contains(1));
		}

		PRUnitTestMethod(Collisions, Quick)
		{
			pr::vector_hash_map<int, int, CollidingHash> map;
			for (int i = 0; i != 40; ++i)
				map[i] = i * 10;

			PR_EXPECT(map.size() == 40U);
			for (int i = 0; i != 40; ++i)
				PR_EXPECT(map.at(i) == i * 10);

			PR_EXPECT(!map.contains(40));
		}

		PRUnitTestMethod(ClearKeepsCapacity, Quick)
		{
			Map map;
			map.reserve(100);
			auto capacity = map.capacity();
			auto data = map.data();
			PR_EXPECT(capacity >= 100U);

			for (int round = 0; round != 3; ++round)
			{
				for (int i = 0; i != 100; ++i)
					map[i + round] = "x";

				PR_EXPECT(map.size() == 100U);
				PR_EXPECT(map.contains(round));
				PR_EXPECT(!map.contains(round + 100));

				map.clear();
				PR_EXPECT(map.empty());
				PR_EXPECT(!map.contains(round));
				PR_EXPECT(map.capacity() == capacity);
				PR_EXPECT(map.data() == data);
			}
		}
	};
}
#endif
