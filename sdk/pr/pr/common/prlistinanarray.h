//****************************************************************************
//
//	A linked list in an array
//
//****************************************************************************

#ifndef PR_LIST_IN_AN_ARRAY_H
#define PR_LIST_IN_AN_ARRAY_H

#include <new>
#include "pr/meta/if.h"
#include "pr/meta/is_pod.h"
#include "pr/common/prtypes.h"
#include "pr/common/assert.h"

namespace pr
{
	// Fixed size linked list in an array
	template <typename T>
	class ListInAnArray
	{
	public:	// Methods
		enum { INVALID_INDEX = 0x7FFFFFFF };

		ListInAnArray(uint max_size);
		ListInAnArray(const ListInAnArray<T>& copy);
		~ListInAnArray();

		// Accessor
		uint	GetCount()		const	{ return m_count; }
		uint	GetIndex()		const	{ return m_current - m_array; }
		T		Head()			const	{ if(m_head)	{return m_head->m_object;		} else {return T();} }
		T		Current()		const	{ if(m_current) {return m_current->m_object;	} else {return T();} }
		T		Tail()			const	{ if(m_tail)	{return m_tail->m_object;		} else {return T();} }
		T*		HeadP()			const	{ if(m_head)	{return &m_head->m_object;		} else {return 0;} }
		T*		CurrentP()		const	{ if(m_current) {return &m_current->m_object;	} else {return 0;} }
		T*		TailP()			const	{ if(m_tail)	{return &m_tail->m_object;		} else {return 0;} }
		T&		RefHead()		const	{ PR_ASSERT(PR_DBG, m_head, "");	return m_head->m_object;	}
		T&		RefCurrent()	const	{ PR_ASSERT(PR_DBG, m_current, "");	return m_current->m_object;	}
		T&		RefTail()		const	{ PR_ASSERT(PR_DBG, m_tail, "");	return m_tail->m_object;	}

		// Iterator
		const T First()	const		{ if(_First()) {return m_head->m_object;   } else {return T();} }
		const T Next()	const		{ if(_Next())  {return m_current->m_object;} else {return T();} }
		const T Last()	const		{ if(_Last())  {return m_tail->m_object;   } else {return T();} }
		const T Prev()	const		{ if(_Prev())  {return m_current->m_object;} else {return T();} }
		T First()					{ if(_First()) {return m_head->m_object;   } else {return T();} }
		T Next()					{ if(_Next())  {return m_current->m_object;} else {return T();} }
		T Last()					{ if(_Last())  {return m_tail->m_object;   } else {return T();} }
		T Prev()					{ if(_Prev())  {return m_current->m_object;} else {return T();} }

		// Iterator - Returns a pointer to the object
		const T* FirstP()	const	{ if(_First()) {return &m_head->m_object;   } else {return 0;} }
		const T* NextP()	const	{ if(_Next())  {return &m_current->m_object;} else {return 0;} }
		const T* LastP()	const	{ if(_Last())  {return &m_tail->m_object;   } else {return 0;} }
		const T* PrevP()	const	{ if(_Prev())  {return &m_current->m_object;} else {return 0;} }
		T* FirstP()					{ if(_First()) {return &m_head->m_object;   } else {return 0;} }
		T* NextP()					{ if(_Next())  {return &m_current->m_object;} else {return 0;} }
		T* LastP()					{ if(_Last())  {return &m_tail->m_object;   } else {return 0;} }
		T* PrevP()					{ if(_Prev())  {return &m_current->m_object;} else {return 0;} }

		// Relative Iterator
		uint Next(uint which)	const	{ PR_ASSERT(PR_DBG, which < m_max_size, ""); if(m_array[which].m_next)  {return static_cast<uint>(m_array[which].m_next - m_array);} else {return INVALID_INDEX;} }
		uint Prev(uint which)	const	{ PR_ASSERT(PR_DBG, which < m_max_size, ""); if(m_array[which].m_prev)  {return static_cast<uint>(m_array[which].m_prev - m_array);} else {return INVALID_INDEX;} }
		uint Next(uint which)			{ PR_ASSERT(PR_DBG, which < m_max_size, ""); if(m_array[which].m_next)  {return static_cast<uint>(m_array[which].m_next - m_array);} else {return INVALID_INDEX;} }
		uint Prev(uint which)			{ PR_ASSERT(PR_DBG, which < m_max_size, ""); if(m_array[which].m_prev)  {return static_cast<uint>(m_array[which].m_prev - m_array);} else {return INVALID_INDEX;} }

		// Add/Insert
		uint	AddToHead(const T& object);
		uint	AddToTail(const T& object);
		uint	AddAfterCurrent(const T& object);
		uint	AddBeforeCurrent(const T& object);
		T&		ExtendHead();
		T&		ExtendTail();

		// Move
		void	MoveToAfter(uint which, uint where);
		void	MoveToBefore(uint which, uint where);

		// Detach methods do not return the detached item because that would require two copy
		// operations, one from the node to a temporary and the next from the temporary to the return value
		void	DetachHead();
		void	DetachTail();
		void	DetachCurrent();
		void	Detach(const T& object);
		void	Detach(uint which);

		// Utility
		void	SetCurrent(uint which)				{ PR_ASSERT(PR_DBG, which < m_max_size, ""); m_current = &m_array[which]; }
		bool	IsEmpty()  const					{ return m_head == 0; }
		T*		Ptr(uint which)						{ PR_ASSERT(PR_DBG, which == INVALID_INDEX || which < m_max_size, ""); return (which != INVALID_INDEX) ? (&m_array[which].m_object) : (0); }
		T&		 operator [](uint which)			{ return m_array[which].m_object; }
		const T& operator [](uint which) const		{ return m_array[which].m_object; }
		bool	Find(const T& other, bool search_forwards = true) const;
		void	Destroy();

		// Stack Interface
		void Push(const T& object)		{ AddToHead(object);	}
		T	 Pop()						{ return DetachHead();	}

		// Queue Interface
		void Enqueue(const T& object)	{ AddToTail(object);	}
		T	 Dequeue()					{ return DetachHead();	}

		// Diagnostic
		void Print() const				{ for( const T* t = FirstP(); t; t = NextP() ) t->Print(); }
		bool Verify() const;

	protected:	// Structures
		struct Node
		{
			Node() {}
			typename T m_object;
			Node*	m_next;
			Node*	m_prev;
		};
		struct NonPOD
		{
			static Node* Allocate(uint size)						{ return new Node[size]; }
			static void DestructRange(Node* first, Node* last)		{ while( first != last ) {Node* temp = first; first = first->m_next; temp->Node::~Node();} }
			static void Deallocate(Node* target)					{ delete [] target; }
		};
		struct POD
		{
			static Node* Allocate(uint size)		{ return reinterpret_cast<Node*>(operator new (size * sizeof(Node))); }
			static void DestructRange(Node*, Node*)	{}
			static void Deallocate(Node* target)	{ operator delete (target); }
		};
		typedef typename meta::if_< meta::is_pod<T>::value, POD, NonPOD >::type Constructor;

	protected:	// Methods
		Node*	_First() const		{ if( m_head ) {m_current = m_head;} return m_head; }
		Node*	_Last() const		{ if( m_tail ) {m_current = m_tail;} return m_tail; }
		Node*	_Next() const;
		Node*	_Prev() const;
		void	Remove(Node* node);

	protected:	// Members
		Node*	m_array;
		uint	m_max_size;
		Node*	m_free;
		Node*	m_head;
		Node*	m_tail;
		uint	m_count;
		mutable Node*	m_current;
	};

	//****************************************************************************
	// Implementation
	//*****
	// Constructor
	template <typename T>
	ListInAnArray<T>::ListInAnArray(uint max_size)
	:m_array(0)
	,m_max_size(max_size)
	,m_free(0)
	,m_head(0)
	,m_tail(0)
	,m_count(0)
	,m_current(0)
	{
		m_array = Constructor::Allocate(m_max_size);
		PR_ASSERT(PR_DBG, m_array != 0, "");
		Destroy();
	}

	//*****
	// Copy constructor
	template <typename T>
	ListInAnArray<T>::ListInAnArray(const ListInAnArray<T>& copy)
	:m_array(0)
	,m_max_size(copy.m_max_size)
	,m_free(0)
	,m_head(0)
	,m_tail(0)
	,m_count(0)
	,m_current(0)
	{
		PR_ASSERT(PR_DBG, copy.m_count == 0, "Don't copy non-zero arrays");
		m_array = Constructor::Allocate(m_max_size);
		PR_ASSERT(PR_DBG, m_array != 0, "");
		Destroy();
	}

	//*****
	// Destructor
	template <typename T>
	inline ListInAnArray<T>::~ListInAnArray()
	{
		Constructor::DestructRange(m_head, 0);
		Constructor::Deallocate(m_array);
	}

	//*****
	// Add an object to the head of the list. Returns the index position of where the object was added
	template <typename T>
	uint ListInAnArray<T>::AddToHead(const T& object)
	{
		if( !m_free ) { PR_ASSERT(PR_DBG, false, "You've over filled this list"); return INVALID_INDEX; }

		Node *node	= m_free;
		m_free = m_free->m_next;

		new (&node->m_object) T(object);
		node->m_next	= m_head;
		node->m_prev	= (m_head) ? (m_head->m_prev) : (0);
		if( node->m_next ) node->m_next->m_prev = node;
		if( node->m_prev ) node->m_prev->m_next = node;

		m_head			= node;
		if( !m_tail )	m_tail = node;

		++m_count;
		return static_cast<uint>(node - m_array);
	}

	//*****
	// Add an object to the tail of the list
	template <typename T>
	uint ListInAnArray<T>::AddToTail(const T& object)
	{
		if( !m_free ) { PR_ASSERT(PR_DBG, false, "You've over filled this list"); return INVALID_INDEX; }

		Node *node	= m_free;
		m_free = m_free->m_next;

		new (&node->m_object) T(object);
		node->m_next	= (m_tail) ? (m_tail->m_next) : (0);
		node->m_prev	= m_tail;
		if( node->m_next ) node->m_next->m_prev = node;
		if( node->m_prev ) node->m_prev->m_next = node;

		m_tail			= node;
		if( !m_head )	m_head = node;
		++m_count;

		return static_cast<uint>(node - m_array);
	}

	//*****
	// Add an object after the current position. The current position is NOT moved by this method
	template <typename T>
	uint ListInAnArray<T>::AddAfterCurrent(const T& object)
	{
		PR_ASSERT(PR_DBG, m_current, "");
		if( !m_free ) { PR_ASSERT(PR_DBG, false, "You've over filled this list"); return INVALID_INDEX; }

		Node *node = m_free;
		m_free = m_free->m_next;

		new (&node->m_object) T(object);
		node->m_next	= m_current->m_next;
		node->m_prev	= m_current;
		if( node->m_next ) node->m_next->m_prev = node;
		if( node->m_prev ) node->m_prev->m_next = node;
		if( m_tail == m_current ) m_tail = node;
		++m_count;

		return static_cast<uint>(node - m_array);
	}

	//*****
	// Add an object immediately before the current position. The current pointer will still
	// point to the same object after this call
	template <typename T>
	uint ListInAnArray<T>::AddBeforeCurrent(const T& object)
	{
		PR_ASSERT(PR_DBG, m_current, "");
		if( !m_free ) { PR_ASSERT(PR_DBG, false, "You've over filled this list"); return INVALID_INDEX; }

		Node *node = m_free;
		m_free = m_free->m_next;

		new (&node->m_object) T(object);
		node->m_next	= m_current;
		node->m_prev	= m_current->m_prev;
		if( node->m_next ) node->m_next->m_prev = node;
		if( node->m_prev ) node->m_prev->m_next = node;
		if( m_head == m_current ) m_head = node;
		++m_count;

		return static_cast<uint>(node - m_array);
	}

	//*****
	// Extend the head of the list by another node and
	// return a reference to 'T' in that node
	template <typename T>
	T& ListInAnArray<T>::ExtendHead()
	{
		PR_ASSERT(PR_DBG, m_free, "You've over filled this list");

		Node *node = m_free;
		m_free = m_free->m_next;

		if( !is_pod ) new (&node->m_object) T();
		node->m_next	= m_head;
		node->m_prev	= (m_head) ? (m_head->m_prev) : (0);
		if( node->m_next ) node->m_next->m_prev = node;
		if( node->m_prev ) node->m_prev->m_next = node;

		m_head			= node;
		if( !m_tail )	m_tail = node;
		++m_count;

		return node->m_object;
	}

	//*****
	// Extend the tail of the list by another node and
	// return a reference to 'T' in that node
	template <typename T>
	T& ListInAnArray<T>::ExtendTail()
	{
		PR_ASSERT(PR_DBG, m_free, "You've over filled this list");

		Node* node = m_free;
		m_free = m_free->m_next;

		if( !is_pod ) new (&node->m_object) T();
		node->m_next	= (m_tail) ? (m_tail->m_next) : (0);
		node->m_prev	= m_tail;
		if( node->m_next ) node->m_next->m_prev = node;
		if( node->m_prev ) node->m_prev->m_next = node;

		m_tail			= node;
		if( !m_head )	m_head = node;
		++m_count;

		return node->m_object;
	}

	//*****
	// Unlink 'which' and insert it after 'where'. If 'where' is INVALID_INDEX
	// then 'which' is inserted at the head of the list
	template <typename T>
	void ListInAnArray<T>::MoveToAfter(uint which, uint where)
	{
		// Detach 'which' from its current position
		if( m_array[which].m_prev )		m_array[which].m_prev->m_next = m_array[which].m_next;
		if( m_array[which].m_next )		m_array[which].m_next->m_prev = m_array[which].m_prev;
		if( m_head == &m_array[which] )	m_head = m_array[which].m_next;
		if( m_tail == &m_array[which] )	m_tail = m_array[which].m_prev;

		// Insert 'which' immediately after 'where'
		if( where == INVALID_INDEX ) // Add 'which' to the head of the list
		{
			m_array[which].m_prev = 0;
			m_array[which].m_next = m_head;
			if( m_head ) m_head->m_prev = &m_array[which];
			m_head = &m_array[which];
			if( !m_tail ) m_tail = m_head;
		}
		else
		{
			m_array[which].m_prev = &m_array[where];
			m_array[which].m_next =  m_array[where].m_next;
			if( m_array[where].m_next ) m_array[where].m_next->m_prev = &m_array[which];
			m_array[where].m_next = &m_array[which];
			if( m_tail == &m_array[where] ) m_tail = &m_array[which];
		}
	}

	//*****
	// Unlink 'which' and insert it before 'where'. If 'where' is INVALID_INDEX
	// then 'which' is inserted at the tail of the list
	template <typename T>
	void ListInAnArray<T>::MoveToBefore(uint which, uint where)
	{
		// Detach 'which' from its current position
		if( m_array[which].m_prev )		m_array[which].m_prev->m_next = m_array[which].m_next;
		if( m_array[which].m_next )		m_array[which].m_next->m_prev = m_array[which].m_prev;
		if( m_head == &m_array[which] )	m_head = m_array[which].m_next;
		if( m_tail == &m_array[which] )	m_tail = m_array[which].m_prev;

		// Insert 'which' immediately before 'where'
		if( where == INVALID_INDEX ) // Add 'which' to the tail of the list
		{
			m_array[which].m_prev = m_tail;
			m_array[which].m_next = 0;
			if( m_tail ) m_tail->m_next = &m_array[which];
			m_tail = &m_array[which];
			if( !m_head ) m_head = m_tail;
		}
		else
		{
			m_array[which].m_prev =  m_array[where].m_prev;
			m_array[which].m_next = &m_array[where];
			if( m_array[where].m_prev ) m_array[where].m_prev->m_next = &m_array[which];
			m_array[where].m_prev = &m_array[which];
			if( m_head == &m_array[where] ) m_head = &m_array[which];
		}
	}

	//*****
	// Detach the head of the list
	template <typename T>
	inline void ListInAnArray<T>::DetachHead()
	{
		if( !m_head ) return;
		Remove(m_head);
	}

	//*****
	// Detach the tail of the list
	template <typename T>
	inline void ListInAnArray<T>::DetachTail()
	{
		if( !m_tail ) return;
		Remove(m_tail);
	}

	//*****
	// Detach the current position and move the current to the next.
	template <typename T>
	inline void ListInAnArray<T>::DetachCurrent()
	{
		PR_ASSERT(PR_DBG, m_current, "");
		Remove(m_current);
	}

	//*****
	// Detach a particular object from the list.
	template <typename T>
	inline void ListInAnArray<T>::Detach(const T& object)
	{
		if( Find(object) )
			DetachCurrent();
	}

	//*****
	// Detach the 'which'th element in the list
	template <typename T>
	inline void ListInAnArray<T>::Detach(uint which)
	{
		Remove(&m_array[which]);
	}

	//*****
	// Look for 'other' in the list. If found, set the current position and return true
	// If not found, don't change anything
	template <typename T>
	bool ListInAnArray<T>::Find(const T& other, bool search_forwards) const
	{
		if( m_current && m_current->m_object == other ) return true;

		Node*	ptr		= 0;
		if( search_forwards )	{ ptr = m_head; }
		else					{ ptr = m_tail; }

		// Put the if condition outside of the loop for speed reasons
		if( search_forwards )
			while( ptr )
			{
				if( ptr->m_object == other ) break;
				ptr = ptr->m_next;
			}
		else
			while( ptr )
			{
				if( ptr->m_object == other ) break;
				ptr = ptr->m_prev;
			}
		if( !ptr ) return false;

		m_current		= ptr;
		return true;
	}

	//*****
	// Empty the list
	template <typename T>
	void ListInAnArray<T>::Destroy()
	{
		Constructor::DestructRange(m_head, 0);

		// Set up the list pointers
		m_free				= m_array;
		m_head				= 0;
		m_tail				= 0;
		m_count				= 0;
		m_current			= 0;

		// Form a singularly linked list of the free positions
		for( uint i = 0; i < m_max_size - 1; ++i )
		{
			m_array[i].m_next = &m_array[i+1];
		}

		// Terminate the end
		m_array[m_max_size-1].m_next = 0;
	}

	//****************************************************************************
	// Private methods
	//*****
	template <typename T>
	inline typename ListInAnArray<T>::Node* ListInAnArray<T>::_Next() const
	{
		PR_ASSERT(PR_DBG, m_current, "");
		if( !m_current->m_next ) return 0;
		m_current = m_current->m_next;
		return m_current;
	}

	//*****
	template <typename T>
	inline typename ListInAnArray<T>::Node* ListInAnArray<T>::_Prev() const
	{
		PR_ASSERT(PR_DBG, m_current, "");
		if( !m_current->m_prev ) return 0;
		m_current = m_current->m_prev;
		return m_current;
	}

	//*****
	// Remove a node from the list.
	template <typename T>
	void ListInAnArray<T>::Remove(Node* node)
	{
		if( m_head == m_tail )
		{
			PR_ASSERT(PR_DBG, node == m_head, "");
			m_head = m_tail = m_current = 0;
		}
		else
		{
			if( node->m_prev )		node->m_prev->m_next = node->m_next;
			if( node->m_next )		node->m_next->m_prev = node->m_prev;
			if( m_head == node )
			{
				if( m_current == m_head)
				{
					m_current = m_current->m_next;
				}
				m_head = m_head->m_next;
			}
			else if( m_tail == node )
			{
				if( m_current == m_tail )
				{
					m_current = m_current->m_prev;
				}
				m_tail = m_tail->m_prev;
			}
			else if( m_current == node )
			{
				m_current = m_current->m_next;
			}
		}
		PR_ASSERT(PR_DBG, m_count >= 0, "");
		--m_count;

		node->m_next = m_free;
		m_free = node;
	}

	//*****
	// Check the integrity of the list
	template <typename T>
	bool ListInAnArray<T>::Verify() const
	{
		if( !m_head || !m_tail )
		{
			PR_ASSERT(PR_DBG, !m_head, "");
			PR_ASSERT(PR_DBG, !m_tail, "");
			PR_ASSERT(PR_DBG, !m_current, "");
			PR_ASSERT(PR_DBG, m_count == 0, "");
			return true;
		}

		PR_ASSERT(PR_DBG, m_head->m_prev == 0, "");
		PR_ASSERT(PR_DBG, m_tail->m_next == 0, "");

		bool current_is_valid = (m_current) ? (false) : (true);
		uint count = 0;
		const Node* node = m_head;
		do
		{
			++count;
			if( node == m_current )
			{
				current_is_valid = true;
			}
			node = node->m_next;
		}
		while( node );
		PR_ASSERT(PR_DBG, count == m_count, "");
		PR_ASSERT(PR_DBG, current_is_valid, "");

		uint num_free = 0;
		Node* free = m_free;
		while( free ) { ++num_free; free = free->m_next; }
		PR_ASSERT(PR_DBG, num_free + m_count == m_max_size, "");

		return true;
	}
}//namespace PR

#endif//PR_LIST_IN_AN_ARRAY_H
