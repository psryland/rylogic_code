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
#pragma once
#include <iterator>
#include <cassert>

namespace pr::chain
{
	// Member chain **********************************************************************************
	// This is the most basic form of a chain.
	// The functions assume 'Type' contains the members 'm_next' and 'm_prev'
	// which are of type 'Type*'. This chain only allows 'Type' to be in one chain at a time.
	// Use the following to iterate over elements in this style of chain
	// for (chain::Iter<Type const> iter(elem); iter; ++iter) { iter->DoStuff(); }
	template <typename Type>
	struct Iter
	{
		using value_type = Type;

		value_type* i;
		value_type* iend;

		Iter(value_type& elem)
			: i(&elem)
			, iend(nullptr)
		{}
		operator value_type const* () const
		{
			return i == iend ? 0 : i;
		}
		Iter& operator ++()
		{
			if (!iend) iend = i;
			i = i->m_next;
			return *this;
		}
		Iter& operator --()
		{
			if (!iend) iend = i;
			i = i->m_prev;
			return *this;
		}
		value_type* operator ->()
		{
			return i;
		}
		value_type& operator *()
		{
			return *i;
		}
	};

	// Chain functions
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
		for (Iter<Type const> iter(elem); iter; ++iter) ++count;
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

	// Field chain ***********************************************************************************
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
	template <typename Owner>
	struct Link
	{
		Owner* m_owner;
		Link* m_next;
		Link* m_prev;

		Link(Owner* owner = nullptr)
		{
			init(owner);
		}
		~Link()
		{
			Remove(*this);
		}
		Link(Link const& rhs)
		{
			init(nullptr);
			*this = rhs;
		}
		Link& operator = (Link const& rhs)
		{
			if (&rhs == this) return *this;
			Remove(*this);
			
			m_owner = nullptr;
			if (rhs.m_owner != nullptr)
			{
				auto byte_offset = reinterpret_cast<char const*>(&rhs) - reinterpret_cast<char const*>(rhs.m_owner);
				m_owner = reinterpret_cast<Owner*>(reinterpret_cast<char*>(this) - byte_offset);
			}

			if (!rhs.empty())
				Insert(const_cast<Link&>(rhs), *this);

			return *this;
		}

		void init(Owner* owner)
		{
			m_next = m_prev = this;
			m_owner = owner;
		}

		// These methods should only be called on the head link
		bool empty() const
		{
			return m_next == this && m_prev == this;
		}
		size_t size() const
		{
			size_t count = 0;
			for (Link const* i = begin(); i != end(); i = i->m_next) ++count;
			return count;
		}
		Link const* begin() const
		{
			return m_next;
		}
		Link* begin()
		{
			return m_next;
		}
		Link const* end() const
		{
			return this;
		}
		Link* end()
		{
			return this;
		}

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
	struct DefaultGroupId;
	template <typename Type, typename GroupId> struct head;
	template <typename Type, typename GroupId> struct link;
	namespace impl
	{
		template <typename Type, typename GroupId> struct citer;
		template <typename Type, typename GroupId> struct miter;
	}

	// A node in the chain
	template <typename Type, typename GroupId = DefaultGroupId>
	struct link
	{
		using value_type = Type;
		using group_id_t = GroupId;

		mutable link* m_next;
		mutable link* m_prev;
		value_type* m_obj; // only used for debugging but not conditional due to the one definition rule

		friend struct head<value_type, group_id_t>;
		friend struct impl::citer<value_type, group_id_t>;
		friend struct impl::miter<value_type, group_id_t>;

		// Insert 'what' at 'where' in a chain.
		friend void insert(link const& where, link& what)
		{
			// Prevent self insertion
			if (&where == &what) return;

			// Remove 'what' from any existing chain
			what.m_prev->m_next = what.m_next;
			what.m_next->m_prev = what.m_prev;

			// Insert 'what' into the same chain as 'where' at the position of 'where'
			what.m_next = const_cast<link*>(&where);
			what.m_prev = where.m_prev;
			what.m_prev->m_next = &what;
			what.m_next->m_prev = &what;
		}

		// Removes 'what' from the chain it's in and put it in it's own chain
		friend void unlink(link& what)
		{
			// Remove 'what' from any existing chain
			what.m_prev->m_next = what.m_next;
			what.m_next->m_prev = what.m_prev;

			// Link it to itself
			what.m_next = &what;
			what.m_prev = &what;
		}

	protected:

		link()
		{
			m_next = m_prev = this;
			m_obj = static_cast<value_type*>(this);
		}
		link(link const& rhs)
		{
			m_next = m_prev = this;
			m_obj = static_cast<value_type*>(this);
			insert(rhs, *this);
		}
		~link()
		{
			unlink(*this);
		}
		link& operator = (link const& rhs)
		{
			if (this == &rhs) return *this;
			insert(rhs, *this);
			return *this;
		}
	};

	// Insert 'what' at 'where' in a chain.
	template <typename Type, typename GroupId>
	void insert(link<Type, GroupId> const& where, link<Type, GroupId>& what)
	{
		// Prevent self insertion
		if (&where == &what)
			return;

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
	template <typename Type, typename GroupId>
	void unlink(link<Type, GroupId>& what)
	{
		// Remove 'what' from any existing chain
		what.m_prev->m_next = what.m_next;
		what.m_next->m_prev = what.m_prev;

		// Link it to itself
		what.m_next = &what;
		what.m_prev = &what;
	}

	// Iterator implementation
	namespace impl
	{
		// Iterators for iterating over chain elements
		template <typename Type, typename GroupId> struct citer
		{
			using value_type = Type;
			using group_id_t = GroupId;
			using pointer = value_type const*;
			using reference = value_type const&;
			using difference_type = std::ptrdiff_t;
			using iterator_category = std::bidirectional_iterator_tag;
			using link_type = link<value_type, group_id_t> const;

			link_type* m_elem;

			citer() = default;
			citer(link_type* elem)
				:m_elem(elem)
			{}

			value_type const* operator ->() const
			{
				return  static_cast<value_type const*>(m_elem);
			}
			value_type const& operator *() const
			{
				return *static_cast<value_type const*>(m_elem);
			}
			citer& operator ++()
			{
				m_elem = m_elem->m_next;
				return *this;
			}
			citer& operator --()
			{
				m_elem = m_elem->m_prev;
				return *this;
			}
			citer operator ++(int)
			{
				auto i = *this;
				m_elem = m_elem->m_next;
				return i;
			}
			citer operator --(int)
			{
				autp i = *this;
				m_elem = m_elem->m_prev;
				return i;
			}

			friend bool operator == (citer const& lhs, citer const& rhs) { return lhs.m_elem == rhs.m_elem; }
			friend bool operator != (citer const& lhs, citer const& rhs) { return lhs.m_elem != rhs.m_elem; }
			friend bool operator <  (citer const& lhs, citer const& rhs) { return lhs.m_elem < rhs.m_elem; }
		};
		template <typename Type, typename GroupId> struct miter
		{
			using value_type = Type;
			using group_id_t = GroupId;
			using pointer = value_type*;
			using reference = value_type&;
			using difference_type = std::ptrdiff_t;
			using iterator_category = std::bidirectional_iterator_tag;
			using link_type = link<value_type, group_id_t>;

			link_type* m_elem;

			miter() = default;
			miter(link_type* elem)
				: m_elem(elem)
			{}

			value_type* operator ->() const
			{
				return static_cast<value_type*>(m_elem);
			}
			value_type& operator * () const
			{
				return *static_cast<value_type*>(m_elem);
			}
			miter& operator ++()
			{
				m_elem = m_elem->m_next;
				return *this;
			}
			miter& operator --()
			{
				m_elem = m_elem->m_prev;
				return *this;
			}
			miter operator ++(int)
			{
				auto i = *this;
				m_elem = m_elem->m_next;
				return i;
			}
			miter operator --(int)
			{ 
				auto i = *this;
				m_elem = m_elem->m_prev;
				return i;
			}

			operator citer<value_type, group_id_t>() const
			{
				return citer<value_type, group_id_t>(m_elem);
			}

			friend bool operator == (miter const& lhs, miter const& rhs) { return lhs.m_elem == rhs.m_elem; }
			friend bool operator != (miter const& lhs, miter const& rhs) { return lhs.m_elem != rhs.m_elem; }
			friend bool operator <  (miter const& lhs, miter const& rhs) { return lhs.m_elem < rhs.m_elem; }
		};
	}

	// Chain head
	template <typename Type, typename GroupId = DefaultGroupId>
	struct head :link<Type, GroupId>
	{
		using value_type      = Type;
		using group_id_t      = GroupId;
		using const_iterator  = impl::citer<value_type, group_id_t>;
		using iterator        = impl::miter<value_type, group_id_t>;
		using const_pointer   = value_type const*;
		using const_reference = value_type const&;
		using pointer         = value_type*;
		using reference       = value_type&;
		using difference_type = std::ptrdiff_t;
		using size_type       = std::size_t;
		using link_type       = link<value_type, group_id_t>;
		using head_type       = head<value_type, group_id_t>;
		
		using link_type::m_next;
		using link_type::m_prev;

		// Chain list heads should not be copied unless empty,
		// otherwise you'll end up with chains containing more than one head.
		head() {}
		head(head const&) = delete;
		head& operator = (head const& rhs) = delete;

		const_iterator begin() const
		{
			return const_iterator(m_next);
		}
		iterator begin()
		{
			return iterator(m_next);
		}
		const_iterator end() const
		{
			return const_iterator(this);
		}
		iterator end()
		{
			return iterator(this);
		}
		const_reference front() const
		{
			return *static_cast<value_type*>(m_next);
		}
		reference front()
		{
			return *static_cast<value_type*>(m_next);
		}
		const_reference back() const
		{
			return *static_cast<value_type*>(m_prev);
		}
		reference back()
		{
			return *static_cast<value_type*>(m_prev);
		}

		bool empty() const
		{
			return
				m_next == static_cast<link_type const*>(this) &&
				m_prev == static_cast<link_type const*>(this);
		}
		void clear()
		{
			m_next = m_prev = this;
		}
		iterator insert(reference where, reference what)
		{
			chain::insert(where, what);
			return iterator(&what);
		}
		iterator insert(iterator where, reference what)
		{
			return insert(what, *where);
		}
		iterator erase(reference what)
		{
			iterator i(what.m_next);
			chain::unlink(what);
			return i;
		}
		iterator erase(iterator where)
		{
			return erase(*where);
		}
		size_type size() const
		{
			std::size_t count = 0;
			for (auto i = m_next; i != this; i = i->m_next) ++count;
			return count;
		}
		void push_front(link_type& what)
		{
			chain::insert(*m_next, what);
		}
		void push_back(link_type& what)
		{
			chain::insert(*this, what);
		}
		void splice(iterator  where, head_type& rhs)
		{
			splice(*where, rhs);
		}
		void splice(reference where, head_type& rhs)
		{
			if (rhs.empty()) return;
			rhs.m_next->m_prev = where.m_prev;
			rhs.m_prev->m_next = &where;
			where.m_prev->m_next = rhs.m_next;
			where.m_prev = rhs.m_prev;
			rhs.m_next = &rhs;
			rhs.m_prev = &rhs;
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
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
	PRUnitTest(ChainTests)
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
#endif
