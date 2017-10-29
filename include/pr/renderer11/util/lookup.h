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

			Lookup(MemFuncs& mem)
				:base(8, generator::hasher(), generator::keyeq(), generator::alloc(mem))
			{}
		};
	}
}

