//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include <unordered_map>
#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/allocator.h"

namespace pr
{
	namespace rdr
	{
		// A generator for an unordered map with a custom allocator
		// Use:
		//   typedef Lookup<int, char>::type CharLookup;
		template <typename Key, typename Value>
		struct LookupGenerator
		{
			typedef std::pair<const Key, Value> pair;
			typedef Allocator<pair>             alloc;
			typedef ::std::hash<Key>            hasher;
			typedef ::std::equal_to<Key>        keyeq;

			typedef std::unordered_map<Key, Value, hasher, keyeq, alloc> type;
		};

		template <typename Key, typename Value>
		struct Lookup :LookupGenerator<Key,Value>::type
		{
			typedef LookupGenerator<Key, Value> generator;
			typedef typename generator::type    base;
			typedef typename generator::pair    pair;

			Lookup(MemFuncs& mem)
				:base(8, generator::hasher(), generator::keyeq(), generator::alloc(mem))
			{}
		};
	}
}

