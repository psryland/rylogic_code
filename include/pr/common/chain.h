//*********************************************************************
// Chain
//  Copyright (c) Rylogic Ltd 2004
//*********************************************************************
// A chain is a way of grouping objects where the containment of the objects
// is the responsibility of the client. Chained objects are always part of a
// chain even if they are in a chain by themselves. Copying an object in a chain
// adds the copied object to the chain as well. The advantages of using a
// chain over a normal container are:
//	- The number of elements and their storage is controlled by the client,
//	- There is no memory copying when adding elements to the chain,
//	- Iterators are always valid even after insertion/deletion,
//	- Constant time insertion and removal of an element in a chain,
//	- Removal via the object directly rather than an iterator to the object
//	- Chain objects can belong to several chains simultaneously
//
// Things to be aware of when using a chain:
//	- Copying an element in a chain adds the copy to the chain. This is because
//	  being in a chain is part of the state of the object. Copying the object, copies
//	  its state. Also, this behaviour is required to correctly contain the objects.
//	  *Do not* think of a chain as a list for this reason.
//	- If the storage of the objects is not in contiguous memory, this can cause
//	  data cache misses when iterating over the chain.
//	- The size() function for a chain is an O(n) operation
//
#ifndef PR_COMMON_CHAIN_H
#define PR_COMMON_CHAIN_H

#include <iterator>
#include "pr/common/assert.h"
#include "pr/common/cast.h"

namespace pr
{
	namespace chain
	{
		// Member chain *****************************************
		// This is the most basic form of a chain.
		// The functions assume 'Type' contains the members 'm_next' and 'm_prev'
		// which are of type 'Type*'. This chain only allows 'Type' to be in one chain at a time.
		// Use the following to iterate over elements in this style of chain
		// for (chain::Iter<Type const> iter(elem); iter; ++iter) { iter->DoStuff(); }
		template <typename Type> struct Iter
		{
			Type *i, *iend;
			Iter(Type& elem) :i(&elem) ,iend(0) {}
			operator Type const*() const { return i == iend ? 0 : i; }
			Iter& operator ++() { if (!iend) iend = i; i = i->m_next; return *this; }
			Iter& operator --() { if (!iend) iend = i; i = i->m_prev; return *this; }
			Type* operator ->() { return i; }
			Type& operator *()  { return *i; }
		};
		template <typename Type> inline void Init(Type& elem)
		{
			elem.m_next = &elem;
			elem.m_prev = &elem;
		}
		template <typename Type> inline bool Empty(Type const& elem)
		{
			return elem.m_next == &elem;
		}
		template <typename Type> inline size_t Size(Type const& elem)
		{
			size_t count = 0;
			for (Iter<Type const> iter(elem); iter; ++iter) { ++count; }
			return count;
		}
		template <typename Type> inline void Join(Type& lhs, Type& rhs)
		{
			// if 'lhs = a1->a2->a3->a1' and 'rhs = b1->b2->b3->b1'
			// then Join(lhs, rhs) = 'a1->a2->a3->b1->b2->b3->a1'
			lhs.m_prev->m_next = &rhs;
			rhs.m_prev->m_next = &lhs;
			lhs.m_prev = rhs.m_prev;
			rhs.m_prev = lhs.m_prev;
		}
		template <typename Type> inline void Remove(Type& elem)
		{
			elem.m_prev->m_next = elem.m_next;
			elem.m_next->m_prev = elem.m_prev;
			Init(elem);
		}
		template <typename Type1, typename Type2> inline Type2* Insert(Type1& before_me, Type2& elem)
		{
			Remove(elem);
			elem.m_next         = &before_me;
			elem.m_prev         = before_me.m_prev;
			elem.m_next->m_prev = &elem;
			elem.m_prev->m_next = &elem;
			return &elem;
		}

		// Field chain ******************************************
		// This type expects the owning object to contain one or more instances of 'Link' as members.
		// Multiple links in an object allow it to belong to multiple chains simultaneously.
		// When forming chains for iterating over, a Link should be created called the head.
		// Example:
		//  struct Field
		//  {
		//      int m_i;
		//      pr::chain::Link<Field> m_link;     // Note: copyable!
		//      Field(int i) :m_i(i) ,m_link(this) {}
		//  };
		//  pr::chain::Link<Field> head;
		//  Field f0(0), f1(1), f2(2);
		//  pr::chain::Insert(head, f0.m_link);
		//  pr::chain::Insert(head, f1.m_link);
		//  pr::chain::Insert(head, f2.m_link);
		//  for (pr::chain::Link<Field>* i = head.begin(); i != head.end(); i = i->m_next) i->m_owner->DoStuff();
		template <typename Owner> struct Link
		{
			Owner* m_owner;
			Link *m_next, *m_prev;

			Link(Owner* owner = 0) { init(owner); }
			~Link()                { Remove(*this); }
			Link(Link const& rhs)  { init(0); *this = rhs; }
			Link& operator = (Link const& rhs)
			{
				if (&rhs == this) return *this;
				Remove(*this);
				m_owner = (rhs.m_owner == 0) ? 0 : reinterpret_cast<Owner*>(byte_ptr(this) - (pr::byte_ptr(&rhs) - pr::byte_ptr(rhs.m_owner)));
				if (!rhs.empty()) Insert(const_cast<Link&>(rhs), *this);
				return *this;
			}

			void init(Owner* owner) { m_next = m_prev = this; m_owner = owner; }

			// These methods should only be called on the head link
			bool        empty() const     { return m_next == this && m_prev == this; }
			size_t      size() const      { size_t count = 0; for (Link const* i = begin(); i != end(); i = i->m_next, ++count){} return count; }
			Link const* begin() const     { return m_next; }
			Link*       begin()           { return m_next; }
			Link const* end() const       { return this; }
			Link*       end()             { return this; }

			friend void swap(Link<Owner>& lhs, Link<Owner>& rhs)
			{
				std::swap(lhs.m_owner, rhs.m_owner);
				std::swap(lhs.m_next, rhs.m_next);
				std::swap(lhs.m_prev, rhs.m_prev);
			}
		};

		// Mixin chain ******************************************
		// Simple usage:
		//    struct MyClass : public chain::link<MyClass>
		//    {
		//       void DoSomething();
		//    };
		//
		//    MyClass my_class[10]; // Storage for MyClass objects
		//    chain::head<MyClass> group1;
		//    chain::head<MyClass> group2;
		//
		//    group1.push_back(my_class[0]);
		//    group1.push_back(my_class[1]);
		//    group1.push_back(my_class[2]);
		//    group2.push_back(my_class[1]); // NOTE: 'move' my_class[1] out of 'group1' and into 'group2', not copy.
		//
		//    for( chain::head<MyClass>::iterator i = group1.begin(), i_end = group1.end(); i != i_end; ++i )
		//    {
		//        i->DoSomething();
		//    }
		//
		// Multiple chains usage:
		//    struct Group1;
		//    struct Group2;
		//    struct MyClass :public chain::link<MyClass, Group1> ,public chain::link<MyClass, Group2>
		//    {
		//        void DoSomething();
		//    };
		//
		//    MyClass my_class[10]; // Storage for MyClass objects
		//    chain::head<MyClass, Group1> group1;
		//    chain::head<MyClass, Group2> group2;
		//
		//    group1.push_back(my_class[0]);
		//    group1.push_back(my_class[1]);
		//    group1.push_back(my_class[2]);
		//
		//    grou2.push_back(my_class[1]);    // NOTE: 'my_class[1]' is now in both 'group1' and 'group2'.
		//
		//    for( chain::head<MyClass>::iterator i = group1.begin(), i_end = group1.end(); i != i_end; ++i )
		//    {
		//        i->DoSomething();
		//    }
		//    for( chain::head<MyClass>::iterator i = group2.begin(), i_end = group2.end(); i != i_end; ++i )
		//    {
		//        i->DoSomething();
		//    }

		// Forward declarations
		template <typename Type, typename GroupId> struct head;
		template <typename Type, typename GroupId> struct link;
		template <typename Type, typename GroupId> void insert(link<Type, GroupId> const& where ,link<Type, GroupId>& what);
		template <typename Type, typename GroupId> void unlink(link<Type, GroupId>& what);
		namespace impl
		{
			struct DefaultGroupId;
			template <typename Type, typename QualifiedType, typename IterType> struct iter_common;
			template <typename Type, typename GroupId> struct citer;
			template <typename Type, typename GroupId> struct iter;
		}

		// A node in the chain
		template <typename Type, typename GroupId = impl::DefaultGroupId> struct link
		{
			mutable link<Type, GroupId>* m_next;
			mutable link<Type, GroupId>* m_prev;
			Type* m_obj; // only used for debugging but not conditional due to the one definition rule

			friend struct head<Type, GroupId>;
			friend struct impl::iter_common<Type ,Type const ,link<Type, GroupId> const>;
			friend struct impl::iter_common<Type ,Type       ,link<Type, GroupId>      >;
			friend void   insert<Type, GroupId>(link<Type, GroupId> const& where ,link<Type, GroupId>& what);
			friend void   unlink<Type, GroupId>(link<Type, GroupId>& what);

		protected:
			link()
			{
				m_next = m_prev = this;
				m_obj = static_cast<Type*>(this);
			}
			link(link<Type, GroupId> const& rhs)
			{
				m_next = m_prev = this;
				m_obj = static_cast<Type*>(this);
				insert(rhs, *this);
			}
			~link()
			{
				unlink(*this);
			}
			link& operator = (link<Type, GroupId> const& rhs)
			{
				if (this != &rhs)
					insert(rhs, *this);
				return *this;
			}
		};

		// Iterator implementation
		namespace impl
		{
			template <typename Type, typename QualifiedType, typename IterType> struct iter_common
			{
				typedef std::ptrdiff_t                  difference_type;
				typedef Type                            value_type;
				typedef QualifiedType&                  reference;
				typedef QualifiedType*                  pointer;
				typedef std::bidirectional_iterator_tag iterator_category;

				IterType* m_elem;

				iter_common() {}
				iter_common(IterType* elem) : m_elem(elem) {}
				pointer      operator ->() const { return  static_cast<pointer>(m_elem); }
				reference    operator * () const { return *static_cast<pointer>(m_elem); }
				iter_common& operator ++()       { m_elem = m_elem->m_next; return *this; }
				iter_common& operator --()       { m_elem = m_elem->m_prev; return *this; }
				iter_common  operator ++(int)    { iter_common i(m_elem); m_elem = m_elem->m_next; return i; }
				iter_common  operator --(int)    { iter_common i(m_elem); m_elem = m_elem->m_prev; return i; }
			};
			template <typename Type, typename QualifiedType, typename IterType> inline bool operator <  (iter_common<Type, QualifiedType, IterType> const& lhs, iter_common<Type, QualifiedType, IterType> const& rhs) { return lhs.m_elem <  rhs.m_elem; }
			template <typename Type, typename QualifiedType, typename IterType> inline bool operator == (iter_common<Type, QualifiedType, IterType> const& lhs, iter_common<Type, QualifiedType, IterType> const& rhs) { return lhs.m_elem == rhs.m_elem; }
			template <typename Type, typename QualifiedType, typename IterType> inline bool operator != (iter_common<Type, QualifiedType, IterType> const& lhs, iter_common<Type, QualifiedType, IterType> const& rhs) { return lhs.m_elem != rhs.m_elem; }

			// Iterators for iterating over chain elements
			template <typename Type, typename GroupId> struct citer :iter_common<Type, Type const, link<Type, GroupId> const>
			{
				citer() {}
				citer(link<Type, GroupId> const* elem) :iter_common<Type, Type const, link<Type, GroupId> const>(elem) {}
			};
			template <typename Type, typename GroupId> struct iter :iter_common< Type, Type, link<Type, GroupId> >
			{
				iter() {}
				iter(link<Type, GroupId>* elem) :iter_common< Type, Type, link<Type, GroupId> >(elem) {}
				operator citer<Type, GroupId>() const { return citer<Type, GroupId>(m_elem); }
			};
		}

		// Chain head
		template <typename Type, typename GroupId = impl::DefaultGroupId> struct head :link<Type, GroupId>
		{
			// Chain list heads should not be copied unless empty,
			// otherwise you'll end up with chains containing more than one head.
			head() {}
			head(head const&) :link<Type, GroupId>() { PR_ASSERT(PR_DBG, false, ""); }
			head& operator = (head const& rhs)       { PR_ASSERT(PR_DBG, false, ""); return *this; }

			typedef impl::citer<Type, GroupId> const_iterator;
			typedef impl::iter<Type, GroupId>  iterator;
			typedef const Type*                const_pointer;
			typedef const Type&                const_reference;
			typedef Type*                      pointer;
			typedef Type&                      reference;
			typedef Type                       value_type;
			typedef std::ptrdiff_t             difference_type;
			typedef std::size_t                size_type;
			typedef link<Type, GroupId>        link_type;

			const_iterator  begin() const { return const_iterator(m_next); }
			iterator        begin()       { return iterator(m_next); }
			const_iterator  end() const   { return const_iterator(this); }
			iterator        end()         { return iterator(this); }
			const_reference front() const { return *static_cast<Type*>(m_next); }
			reference       front()       { return *static_cast<Type*>(m_next); }
			const_reference back () const { return *static_cast<Type*>(m_prev); }
			reference       back ()       { return *static_cast<Type*>(m_prev); }

			bool            empty()    const                                   { return m_next == static_cast<const link_type*>(this) && m_prev == static_cast<const link_type*>(this); }
			void            clear()                                            { m_next = m_prev = this; }
			iterator        insert(reference where, reference what)            { chain::insert(where, what); return iterator(&what); }
			iterator        insert(iterator  where, reference what)            { return insert(what, *where); }
			iterator        erase(reference    what)                           { iterator i(what.m_next); chain::unlink(what); return i; }
			iterator        erase(iterator where)                              { return erase(*where); }
			std::size_t     size() const                                       { std::size_t count = 0; for(const link_type* i=m_next; i!=this; i=i->m_next) {++count;} return count; }
			void            push_front(link<Type, GroupId>& what)              { chain::insert(*m_next, what); }
			void            push_back (link<Type, GroupId>& what)              { chain::insert(*this, what); }
			void            splice(iterator  where, head<Type, GroupId>& rhs)  { splice(*where, rhs); }
			void            splice(reference where, head<Type, GroupId>& rhs)
			{
				if (rhs.empty()) return;
				rhs.m_next->m_prev   = where.m_prev;
				rhs.m_prev->m_next   = &where;
				where.m_prev->m_next = rhs.m_next;
				where.m_prev         = rhs.m_prev;
				rhs.m_next           = &rhs;
				rhs.m_prev           = &rhs;
			}
		};

		// Insert 'what' at 'where' in a chain.
		template <typename Type, typename GroupId> void insert(link<Type, GroupId> const& where, link<Type, GroupId>& what)
		{
			// Prevent self insertion
			if (&where == &what) return;

			// Remove 'what' from any existing chain
			what.m_prev->m_next = what.m_next;
			what.m_next->m_prev = what.m_prev;

			// Insert 'what' into the same chain as 'where' at the position of 'where'
			what.m_next = const_cast<link<Type, GroupId>*>(&where);
			what.m_prev = where.m_prev;
			what.m_prev->m_next = &what;
			what.m_next->m_prev = &what;
		}

		// Removes 'what' from the chain it's in and put it in it's own chain
		template <typename Type, typename GroupId> void unlink(link<Type, GroupId>& what)
		{
			// Remove 'what' from any existing chain
			what.m_prev->m_next = what.m_next;
			what.m_next->m_prev = what.m_prev;

			// Link it to itself
			what.m_next = &what;
			what.m_prev = &what;
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		struct Member
		{
			int m_i;
			Member *m_next, *m_prev;
			Member(int i) :m_i(i) { pr::chain::Init(*this); }
		};
		struct Field
		{
			int m_i;
			pr::chain::Link<Field> m_link;
			Field(int i) :m_i(i) { m_link.init(this); }
		};
		PRUnitTest(pr_common_chain)
		{
			{// Member chain
				Member m0(0), m1(1), m2(2);
				pr::chain::Insert(m2, m1);
				pr::chain::Insert(m1, m0);
				PR_CHECK(pr::chain::Size(m0) ,3U);
				PR_CHECK(pr::chain::Size(m1) ,3U);
				PR_CHECK(pr::chain::Size(m2) ,3U);
				{
					pr::chain::Iter<Member> iter(m0);
					PR_CHECK(iter->m_i, 0); ++iter;
					PR_CHECK(iter->m_i, 1); ++iter;
					PR_CHECK(iter->m_i, 2); ++iter;
					PR_CHECK(iter == 0, true);
				}

				Member m3(3), m4(4), m5(5);
				pr::chain::Insert(m5, m4);
				pr::chain::Insert(m4, m3);
				PR_CHECK(pr::chain::Size(m4), 3U);
				{
					pr::chain::Iter<Member> iter(m4);
					PR_CHECK(iter->m_i, 4); --iter;
					PR_CHECK(iter->m_i, 3); --iter;
					PR_CHECK(iter->m_i, 5); --iter;
					PR_CHECK(iter == 0, true);
				}

				pr::chain::Remove(m5);
				PR_CHECK(pr::chain::Size(m3), 2U);
				PR_CHECK(pr::chain::Size(m4), 2U);

				pr::chain::Join(m0, m3);
				{
					pr::chain::Iter<Member> iter(m0);
					PR_CHECK(iter->m_i, 0); ++iter;
					PR_CHECK(iter->m_i, 1); ++iter;
					PR_CHECK(iter->m_i, 2); ++iter;
					PR_CHECK(iter->m_i, 3); ++iter;
					PR_CHECK(iter->m_i, 4); ++iter;
					PR_CHECK(iter == 0, true);
				}
			}
			{
				pr::chain::Link<Field> head;
				Field f0(0), f1(1), f2(2);
				pr::chain::Insert(head, f0.m_link);
				pr::chain::Insert(head, f1.m_link);
				pr::chain::Insert(head, f2.m_link);
				{
					pr::chain::Link<Field>* i = head.begin();
					PR_CHECK(i->m_owner->m_i, 0); i = i->m_next;
					PR_CHECK(i->m_owner->m_i, 1); i = i->m_next;
					PR_CHECK(i->m_owner->m_i, 2); i = i->m_next;
					PR_CHECK(i == &head, true);
				}

				Field f3(f2), f4(4); f4 = f3; // copy construct/assignment
				{
					pr::chain::Link<Field>* i = head.begin();
					PR_CHECK(i->m_owner->m_i, 0); i = i->m_next;
					PR_CHECK(i->m_owner->m_i, 1); i = i->m_next;
					PR_CHECK(i->m_owner->m_i, 2); i = i->m_next;
					PR_CHECK(i->m_owner->m_i, 2); i = i->m_next;
					PR_CHECK(i->m_owner->m_i, 2); i = i->m_next;
					PR_CHECK(i == &head, true);
				}
			}
		}
	}
}
#endif

#endif
