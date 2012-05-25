//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_UTIL_LOOKUP_H
#define PR_RDR_UTIL_LOOKUP_H

//#include <hash_map>
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
			typedef pr::rdr::Allocator<pair>    alloc;
			typedef ::std::tr1::hash<Key>       hasher;
			typedef ::std::equal_to<Key>        keyeq;
			
			typedef std::tr1::unordered_map<Key, Value, hasher, keyeq, alloc> type;
			//typedef stdext::hash_compare< Key, std::less<Key> >   compare;
			//typedef stdext::hash_map<Key, Value, compare, alloc> type;
		};

		template <typename Key, typename Value>
		struct Lookup :LookupGenerator<Key,Value>::type
		{
			typedef LookupGenerator<Key, Value> generator;
			typedef typename generator::type    base;
			
			Lookup(pr::rdr::MemFuncs& mem)
			:base(8, generator::hasher(), generator::keyeq(), generator::alloc(mem))
			{}
		};
	}
}

#endif
