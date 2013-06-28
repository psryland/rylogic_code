//******************************************
// Enumerable
//  Copyright © Oct 2003 Paul Ryland
//******************************************
// A base class for creating C#-style Enumerables
// use:
//   typedef Enumerable<std::vector<Thing>::iterator> Things;
//
//  struct Foo
//  {
//     typedef std::vector<Thing> ThingCont;
//     ThingCont m_things;
//     Enumerable<ThingCont> Things() const { return pr::MakeEnumerable(m_things); }
//  };
//
//  for (auto& i : foo.Things())
//    i.blah();
//

#pragma once
#ifndef PR_COMMON_ENUMERABLE_H
#define PR_COMMON_ENUMERABLE_H

#include <iterator>

namespace pr
{
	// An iterator range
	template <typename TIter> struct Enumerable
	{
		TIter m_begin, m_end;
		TIter const begin() const { return m_begin; }
		TIter       begin()       { return m_begin; }
		TIter const end() const   { return m_end; }
		TIter       end()         { return m_end; }
		Enumerable(TIter begin, TIter end) :m_begin(begin) ,m_end(end) {}
	};

	// Helper for creating a filtering iterator
	template <typename TIter, typename TPred> struct FilterIter
	{
		typedef std::forward_iterator_tag iterator_category;
		TIter m_iter, m_end;
		TPred m_pred;

		FilterIter(TIter iter, TIter end, TPred pred) :m_iter(iter) ,m_end(end) ,m_pred(pred) { find_next_valid(); }
		FilterIter& operator ++() { ++m_iter; find_next_valid(); return *this; }
		auto operator *() -> decltype(*m_iter) const { return *m_iter; }
		bool operator == (FilterIter const& rhs) const { return m_iter == rhs.m_iter; }
		bool operator != (FilterIter const& rhs) const { return m_iter != rhs.m_iter; }
	private:
		void find_next_valid()
		{
			for (; m_iter != m_end && !m_pred(*m_iter); ++m_iter) {}
		}
	};

	// Construct an enumerable from a predicate
	template <typename TCont, typename TPred> Enumerable<FilterIter<typename TCont::iterator, TPred>> MakeEnumerable(TCont& cont, TPred const& pred)
	{
		auto b = std::begin(cont), e = std::end(cont);
		return Enumerable<FilterIter<typename TCont::iterator, TPred>>
		(
			FilterIter<typename TCont::iterator,TPred>(b,e,pred),
			FilterIter<typename TCont::iterator,TPred>(e,e,pred)
		);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <vector>
namespace pr
{
	namespace unittests
	{
		namespace enumerable
		{
			struct Foo
			{
				typedef std::vector<int> Cont;
				Cont m_int;
				Foo()
				{
					m_int.push_back(1);
					m_int.push_back(2);
					m_int.push_back(3);
				}
				pr::Enumerable<pr::FilterIter<Cont::iterator, bool(*)(int)>> OddInts()
				{
					bool (*pred)(int) = [](int item) { return (item % 2) == 1; };
					return pr::MakeEnumerable(m_int, pred);
				}
			};
		}

		PRUnitTest(pr_common_enumerable)
		{
			using namespace pr::unittests::enumerable;

			{
				Foo foo;
				for (auto& i : foo.OddInts())
					i *= 10;
				PR_CHECK(foo.m_int[0], 10);
				PR_CHECK(foo.m_int[1], 2);
				PR_CHECK(foo.m_int[2], 30);
			}
			{
				Foo foo;
				for (auto& i : pr::MakeEnumerable(foo.m_int, [](int item){ return (item % 2) == 0; }))
					i *= -10;
				
				PR_CHECK(foo.m_int[0], 1);
				PR_CHECK(foo.m_int[1], -20);
				PR_CHECK(foo.m_int[2], 3);
			}
		}
	}
}
#endif

#endif


