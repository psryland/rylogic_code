//***********************************************************************//
//                     A Template list class                             //
//	Original version:                                                    //
//		P. Ryland, 2001                                                  //
//                                                                       //
//***********************************************************************//
//
// Special notes about Lists:
//  1) Back referencing assumes 'T' is a pointer to an object that defines
//     List<T, is_pod>::ListRef m_tolist


#ifndef LIST_H
#define LIST_H

#include "PRAssert.h"
#include "MemPool.h"

#ifdef LIST_USE_BOOKMARKS
#define BK(exp) exp
#else
#define BK(exp)
#endif//LIST_USE_BOOKMARKS

namespace PR
{

	//*****
	// Global functions
	template <typename T, bool is_pod>
	inline void ListDetach(const T& object);

	//*****
	// The list.
	template <typename T, bool is_pod>
	class List
	{
	public:	// Methods
		enum { LIST_INDEX_INVALID = 0x7FFFFFFF };

		List(DWORD estimated_size = 1);
		List(const List<T, is_pod>& copy);
		~List();
		List<T, is_pod>& operator = (const List<T, is_pod>& copy) { this->~List(); return *new (this) List<T, is_pod>(copy); }
		
		// Accessor
		DWORD	GetCount()		const	{ return m_count; }
		DWORD	CurrentIndex()	const	{ return m_current_index; }
		T		Head()			const	{ if(m_head)	{return m_head->m_object;		} else {return T();} }
		T		Current()		const	{ if(m_current) {return m_current->m_object;	} else {return T();} }
		T		Tail()			const	{ if(m_tail)	{return m_tail->m_object;		} else {return T();} }
		T*		HeadP()			const	{ if(m_head)	{return &m_head->m_object;		} else {return NULL;} }
		T*		CurrentP()		const	{ if(m_current) {return &m_current->m_object;	} else {return NULL;} }
		T*		TailP()			const	{ if(m_tail)	{return &m_tail->m_object;		} else {return NULL;} }
		T&		RefHead()		const	{ PR_ASSERT(m_head);	return m_head->m_object;	}
		T&		RefCurrent()	const	{ PR_ASSERT(m_current);	return m_current->m_object;	}
		T&		RefTail()		const	{ PR_ASSERT(m_tail);	return m_tail->m_object;	}

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
		const T* FirstP()	const	{ if(_First()) {return &m_head->m_object;   } else {return NULL;} }
		const T* NextP()	const	{ if(_Next())  {return &m_current->m_object;} else {return NULL;} }
		const T* LastP()	const	{ if(_Last())  {return &m_tail->m_object;   } else {return NULL;} }
		const T* PrevP()	const	{ if(_Prev())  {return &m_current->m_object;} else {return NULL;} }
		T* FirstP()					{ if(_First()) {return &m_head->m_object;   } else {return NULL;} }
		T* NextP()					{ if(_Next())  {return &m_current->m_object;} else {return NULL;} }
		T* LastP()					{ if(_Last())  {return &m_tail->m_object;   } else {return NULL;} }
		T* PrevP()					{ if(_Prev())  {return &m_current->m_object;} else {return NULL;} }

		// Add/Insert
		void AddToHead(const T& object);
		void AddToTail(const T& object);
		void AddAfterCurrent(const T& object);
		void AddBeforeCurrent(const T& object);
		T&	 ExtendHead();
		T&	 ExtendTail();

		// Back Referencing
		void AddToHeadWithBackReference(const T& object);
		void AddToTailWithBackReference(const T& object);
		void BackReference(const T& object);

		// Detach methods do not return the detached item because that would require two copy
		// operations, one from the node to a temporary and the next from the temporary to the return value
		void DetachHead();
		void DetachTail();
		void DetachCurrent();
		void Detach(const T& object);
		void Detach(DWORD which);

		// Utility
		bool IsEmpty()  const					{ return m_head == NULL; }
		void Reserve(DWORD size)				{ m_node_pool.SetNumberOfObjectsPerBlock(size); }
		void ReverseOrder();
		void RemoveCommonObjects(const List<T, is_pod>& other);
		bool Find(const T& other, bool search_forwards = true) const;
		void Find(DWORD which) const;
		T&		 operator [](DWORD which)		{ return Index(which); }
		const T& operator [](DWORD which) const	{ return Index(which); }

		// Clean up
		void Destroy();
		void DeleteAndDestroy();
		void ReleaseMemory()	{ BK(PR_ASSERT(!m_bookmarks);  m_bookmark_pool.ReleaseMemory();) PR_ASSERT(!m_head); m_node_pool.ReleaseMemory();}

		// Circular list
		void MakeCircular()		{ if( m_head && !m_circular ) { m_head->m_prev = m_tail; m_tail->m_next = m_head; } m_circular = true;  }
		void MakeLinear()		{ if( m_head &&  m_circular ) { m_head->m_prev = NULL;   m_tail->m_next = NULL;   } m_circular = false; }
		bool IsCircular() const	{ return m_circular; }
		void Rotate(int by);
		void CurrentToHead();
		void CurrentToTail();
		
		#ifdef LIST_USE_BOOKMARKS
		// Bookmarks
		DWORD	Bookmark() const;
		void	ClearBookmark(DWORD which) const;
		bool	RestoreBookmark(DWORD which) const;
		void	ResetBookmarks() const;
		#endif//LIST_USE_BOOKMARKS
		
		// Stack Interface
		void Push(const T& object)		{ AddToHead(object);	}
		T	 Pop()						{ T head = Head(); DetachHead(); return head; }

		// Queue Interface
		void Enqueue(const T& object)	{ AddToTail(object);	}
		T	 Dequeue()					{ return DetachHead();	}

		// Diagnostic
		void Print() const				{ for( const T* t = FirstP(); t; t = NextP() ) t->Print(); }
		bool Verify() const;

	protected:	// Structures
		struct Node
		{
			T		m_object;
			Node*	m_next;
			Node*	m_prev;
		};
		MemPool<Node, is_pod> m_node_pool;

		#ifdef LIST_USE_BOOKMARKS
		struct BookmarkPosition
		{
			BookmarkPosition() : m_ptr(NULL), m_idx(LIST_INDEX_INVALID), m_next(NULL) {}
			BookmarkPosition* m_next;
			Node*	m_ptr;
			DWORD	m_idx;
		};
		mutable MemPool<BookmarkPosition, true> m_bookmark_pool;
		mutable BookmarkPosition* m_bookmarks;
		#endif//LIST_USE_BOOKMARKS

	public:	// Structures
		struct ListRef
		{
			ListRef() : m_mylist(NULL), m_mynode(NULL)	{}
			bool IsInList() const						{ return m_mylist != NULL && m_mynode != NULL; }
			void RemoveBackReference()					{ m_mylist = NULL; m_mynode = NULL; }
			List<T, is_pod>*	m_mylist;
			Node*				m_mynode;
		};
		friend void ListDetach<T, is_pod>(const T& object);

	protected:	// Methods
		Node*	_First() const		{ if( m_head ) {m_current = m_head; m_current_index = 0;}			return m_head; }
		Node*	_Last() const		{ if( m_tail ) {m_current = m_tail; m_current_index = m_count - 1;} return m_tail; }
		Node*	_Next() const;
		Node*	_Prev() const;
		DWORD	GetIndexFor(Node* ptr) const;
		T&		Index(DWORD which) const;
		void	Remove(Node* node, DWORD index);
		BK(void RemoveBookmarksThatReference(Node* node) const;)

	protected:	// Members
		Node*	m_head;
		Node*	m_tail;
		DWORD	m_count;
		mutable Node*	m_current;
		mutable DWORD	m_current_index;
		bool	m_circular;
		Node*	m_last_thing_added;
	};

	//***********************************************************************//
	// Implementation
	//*****
	// Constructor
	template <typename T, bool is_pod>
	List<T, is_pod>::List(DWORD estimated_size)
	:m_node_pool(estimated_size)
	#ifdef LIST_USE_BOOKMARKS
	,m_bookmark_pool(10)
	,m_bookmarks(NULL)
	#endif//LIST_USE_BOOKMARKS
	,m_head(NULL)
	,m_tail(NULL)
	,m_count(0)
	,m_current(NULL)
	,m_current_index(LIST_INDEX_INVALID)
	,m_circular(false)
	,m_last_thing_added(NULL)
	{}

	//*****
	// Copy constructor
	template <typename T, bool is_pod>
	List<T, is_pod>::List(const List<T, is_pod>& copy)
	:m_node_pool(copy.m_node_pool)
	#ifdef LIST_USE_BOOKMARKS
	,m_bookmark_pool(10)
	,m_bookmarks(NULL)
	#endif//LIST_USE_BOOKMARKS
	,m_head(NULL)
	,m_tail(NULL)
	,m_count(0)
	,m_current(NULL)
	,m_current_index(LIST_INDEX_INVALID)
	,m_circular(false)
	,m_last_thing_added(NULL)
	{
		PR_ASSERT_STR(m_count == 0, "Don't copy lists with stuff in em");
	}

	//*****
	// Destructor
	template <typename T, bool is_pod>
	List<T, is_pod>::~List()
	{
		// If this fails you may have:
		// 1) forgotten to delete the elements in the list. e.g.
		//		my_list.Destroy();	
		// 2) Passed the list to a function instead of a reference to it
		//    when the function ends it will try and delete the list
		//PR_WARN_EXP(m_head == NULL, "List is not empty\n");
		
		// If this fails you've leaked bookmarks somewhere
		//BK(PR_WARN_EXP(m_bookmarks == NULL, "List still has bookmarks, you may be leaking them somewhere\n");)

		Destroy();
	}

	//*****
	// Add an object to the head of the list
	template <typename T, bool is_pod>
	void List<T, is_pod>::AddToHead(const T& object)
	{
		Node *node		= m_node_pool.Get();
		new (&node->m_object) T(object);
		
		node->m_next	= m_head;
		node->m_prev	= (m_head) ? (m_head->m_prev) : (NULL);
		if( node->m_next ) node->m_next->m_prev = node;
		if( node->m_prev ) node->m_prev->m_next = node;

		m_head			= node;
		if( !m_tail )	m_tail = node;
		
		if( m_current )
		{
			PR_ASSERT(m_current_index != LIST_INDEX_INVALID);
			++m_current_index;
			BK(BookmarkPosition* bk = m_bookmarks;)
			BK(while( bk ) { ++bk->m_idx; bk = bk->m_next; })
		}
		++m_count;
		m_last_thing_added = node;
	}

	//*****
	// Add an object to the tail of the list
	template <typename T, bool is_pod>
	void List<T, is_pod>::AddToTail(const T& object)
	{
		Node *node		= m_node_pool.Get();
		new (&node->m_object) T(object);

		node->m_next	= (m_tail) ? (m_tail->m_next) : (NULL);
		node->m_prev	= m_tail;
		if( node->m_next ) node->m_next->m_prev = node;
		if( node->m_prev ) node->m_prev->m_next = node;

		m_tail			= node;
		if( !m_head )	m_head = node;
		++m_count;

		m_last_thing_added = node;
	}

	//*****
	// Add an object after the current position. The current position is NOT moved by this method
	template <typename T, bool is_pod>
	void List<T, is_pod>::AddAfterCurrent(const T& object)
	{
		PR_ASSERT(m_current);

		Node *node		= m_node_pool.Get();
		new (&node->m_object) T(object);

		node->m_next	= m_current->m_next;
		node->m_prev	= m_current;
		if( node->m_next ) node->m_next->m_prev = node;
		if( node->m_prev ) node->m_prev->m_next = node;
		if( m_tail == m_current ) m_tail = node;
		++m_count;
		
		BK(BookmarkPosition* bk = m_bookmarks;)
		BK(while( bk ) { if( bk->m_idx > m_current_index ) ++bk->m_idx; bk = bk->m_next; })

		m_last_thing_added = node;
	}

	//*****
	// Add an object immediately before the current position. The current pointer will still
	// point to the same object after this call
	template <typename T, bool is_pod>
	void List<T, is_pod>::AddBeforeCurrent(const T& object)
	{
		PR_ASSERT(m_current);
		
		Node *node		= m_node_pool.Get();
		new (&node->m_object) T(object);

		node->m_next	= m_current;
		node->m_prev	= m_current->m_prev;
		if( node->m_next ) node->m_next->m_prev = node;
		if( node->m_prev ) node->m_prev->m_next = node;
		if( m_head == m_current ) m_head = node;
		++m_count;
		
		BK(BookmarkPosition* bk = m_bookmarks;)
		BK(while( bk ) { if( bk->m_idx >= m_current_index ) ++bk->m_idx; bk = bk->m_next; })
		++m_current_index;

		m_last_thing_added = node;
	}

	//*****
	// Extend the head of the list by another node and
	// return a reference to 'T' in that node
	template <typename T, bool is_pod>
	T& List<T, is_pod>::ExtendHead()
	{
		Node *node		= m_node_pool.Get();
		node->m_next	= m_head;
		node->m_prev	= (m_head) ? (m_head->m_prev) : (NULL);
		if( node->m_next ) node->m_next->m_prev = node;
		if( node->m_prev ) node->m_prev->m_next = node;

		m_head			= node;
		if( !m_tail )	m_tail = node;
		if( m_current )
		{
			PR_ASSERT(m_current_index != LIST_INDEX_INVALID);
			++m_current_index;

			BK(BookmarkPosition* bk = m_bookmarks;)
			BK(while( bk ) { ++bk->m_idx; bk = bk->m_next; })
		}
		++m_count;

		m_last_thing_added = node;
		return node->m_object;
	}

	//*****
	// Extend the tail of the list by another node and
	// return a reference to 'T' in that node
	template <typename T, bool is_pod>
	T& List<T, is_pod>::ExtendTail()
	{
		Node* node		= m_node_pool.Get();
		node->m_next	= (m_tail) ? (m_tail->m_next) : (NULL);
		node->m_prev	= m_tail;
		if( node->m_next ) node->m_next->m_prev = node;
		if( node->m_prev ) node->m_prev->m_next = node;

		m_tail			= node;
		if( !m_head )	m_head = node;
		++m_count;

		m_last_thing_added = node;
		return node->m_object;
	}

	//*****
	// Add 'object' to the head of the list and update a pointer
	// in 'object' to point back to it's position in the list
	template <typename T, bool is_pod>
	inline void List<T, is_pod>::AddToHeadWithBackReference(const T& object)
	{
		AddToHead(object);
		BackReference(object);
	}

	//*****
	// Add 'object' to the tail of the list and update a pointer
	// in 'object' to point back to it's position in the list
	template <typename T, bool is_pod>
	inline void List<T, is_pod>::AddToTailWithBackReference(const T& object)
	{
		AddToTail(object);
		BackReference(object);
	}

	//*****
	// Add a back reference to 'object'. The object must be the last thing added
	template <typename T, bool is_pod>
	inline void List<T, is_pod>::BackReference(const T& object)
	{
		PR_ASSERT(m_last_thing_added->m_object == object);
		object->m_tolist.m_mylist = this;
		object->m_tolist.m_mynode = m_last_thing_added;
	}

	//*****
	// Detach an object with a reference into a list.
	// The current position is moved to the head of the list.
	// This function is much more efficient if there are no bookmarks in the list
	template <typename T, bool is_pod>
	void ListDetach(const T& object)
	{
		// Make sure that the origin list has been set and that 'object' is actually in the list
		PR_ASSERT(object->m_tolist.m_mylist != NULL);
		PR_ASSERT(object->m_tolist.m_mynode	!= NULL);
		List<T, is_pod>& list  = *object->m_tolist.m_mylist;
		List<T, is_pod>::Node* detached = object->m_tolist.m_mynode;
		list.Remove(detached, LIST_INDEX_INVALID);
		object->m_tolist.m_mylist = NULL;
		object->m_tolist.m_mynode = NULL;
	}

	//*****
	// Detach the head of the list
	template <typename T, bool is_pod>
	inline void List<T, is_pod>::DetachHead()
	{
		if( !m_head ) return;
		Remove(m_head, 0);
	}

	//*****
	// Detach the tail of the list
	template <typename T, bool is_pod>
	inline void List<T, is_pod>::DetachTail()
	{
		if( !m_tail ) return;
		Remove(m_tail, m_count - 1);
	}

	//*****
	// Detach the current position and move the current to the next.
	template <typename T, bool is_pod>
	inline void List<T, is_pod>::DetachCurrent()
	{
		PR_ASSERT(m_current);
		Remove(m_current, m_current_index);
	}

	//*****
	// Detach a particular object from the list.
	template <typename T, bool is_pod>
	inline void List<T, is_pod>::Detach(const T& object)
	{
		if( Find(object) )
			DetachCurrent();
	}

	//*****
	// Detach the 'which'th element in the list
	template <typename T, bool is_pod>
	inline void List<T, is_pod>::Detach(DWORD which)
	{
		Find(which);
		DetachCurrent();
	}

	//*****
	// Reverse the order of the elements in the list
	template <typename T, bool is_pod>
	void List<T, is_pod>::ReverseOrder()
	{
		if( !m_head ) return;

		Node* node = m_head;
		Node* swap;
		while( node )
		{
			swap		 = node->m_next;
			node->m_next = node->m_prev;
			node->m_prev = swap;
			node		 = node->m_prev;
		}
		swap   = m_head;
		m_head = m_tail;
		m_tail = swap;
		if( m_current ) m_current_index = m_count - m_current_index - 1;
		BK(BookmarkPosition* bk = m_bookmarks;)
		BK(while( bk ) { bk->m_idx = m_count - bk->m_idx - 1; bk = bk->m_next; })
	}

	//*****
	// This method removes nodes from our list that appear in 'other'
	template <typename T, bool is_pod>
	void List<T, is_pod>::RemoveCommonObjects(const List<T, is_pod>& other)
	{
		if( !m_head ) return;

		const Node* other_node;
		Node* node = _First();
		while( node )
		{
			for( other_node = other.m_head; other_node; other_node = other_node->m_next )
			{
				if( node->m_object == other_node->m_object )
				{
					DetachCurrent();
					node = NULL;
					break;
				}
			}
			if( node == NULL )	node = m_current;
			else				node = _Next();
		}
	}

	//*****
	// Look for 'other' in the list. If found, set the current position and return true
	// If not found, don't change anything
	template <typename T, bool is_pod>
	bool List<T, is_pod>::Find(const T& other, bool search_forwards) const
	{
		if( m_current && m_current->m_object == other ) return true;

		Node*	ptr		= NULL;
		DWORD	index	= 0;
		if( search_forwards )	{ ptr = m_head; index = 0; }
		else					{ ptr = m_tail; index = m_count - 1; }

		// Put the if condition outside of the loop for speed reasons
		if( search_forwards )
			while( ptr )
			{
				if( ptr->m_object == other ) break;
				ptr = ptr->m_next;
				++index;
			}
		else
			while( ptr )
			{
				if( ptr->m_object == other ) break;
				ptr = ptr->m_prev;
				--index;
			}
		if( !ptr ) return false;

		m_current		= ptr;
		m_current_index = index;
		return true;
	}

	//*****
	// Find the 'which' th element in the list and set the current position.
	template <typename T, bool is_pod>
	void List<T, is_pod>::Find(DWORD which) const
	{
		PR_ASSERT(which < m_count);
		if( m_current && m_current_index == which ) return;

		bool forwards			= true;
		Node* closest			= m_head;
		DWORD closest_index		= 0;
		DWORD closest_distance	= which;
		if( m_count - which < closest_distance ) { closest = m_tail; closest_index = m_count - 1; closest_distance = m_count - which; forwards = false; }
		if( m_current )
		{
			PR_ASSERT(m_current_index != LIST_INDEX_INVALID);
			
			// Taking advantage of wrapping here...
			if( m_current_index - which < closest_distance ) { closest = m_current; closest_index = m_current_index; closest_distance = m_current_index - which; }
			if( which - m_current_index < closest_distance ) { closest = m_current; closest_index = m_current_index; closest_distance = which - m_current_index; forwards = false; }
		}
		m_current		= closest;
		m_current_index = closest_index;
		if( forwards )	for( ; closest_distance > 0; --closest_distance ) { m_current = m_current->m_next; ++m_current_index; }
		else			for( ; closest_distance > 0; --closest_distance ) { m_current = m_current->m_prev; --m_current_index; }
	}

	//*****
	// Return all of the nodes to the memory pool and destruct 'T'
	template <typename T, bool is_pod>
	void List<T, is_pod>::Destroy()
	{
		m_head = m_tail = m_current = NULL;
		m_current_index = LIST_INDEX_INVALID;
		m_count = 0;
		m_node_pool.ReclaimAll();
		BK(m_bookmarks = NULL;)
		BK(m_bookmark_pool.ReclaimAll();)
	}

	//*****
	// This method assumes 'T' is a pointer to an object (not an array!)
	// and calls delete on every object before destroying the list
	template <typename T, bool is_pod>
	void List<T, is_pod>::DeleteAndDestroy()
	{
		Node* node = m_head;
		while( node ) { delete node->m_object; node = node->m_next; }
		Destroy();
	}

	//*****
	// Rotate the list by 'by' positions
	template <typename T, bool is_pod>
	void List<T, is_pod>::Rotate(int by)
	{
		bool make_linear = false;
		if( !m_circular ) { MakeCircular(); make_linear = true; }
		_First();
		if( by >= 0 )	for( int i = 0; i < by; ++i ) _Next();
		else			for( int i = 0; i > by; --i ) _Prev();
		CurrentToHead();
		if( make_linear ) MakeLinear();
	}

	//*****
	// Make the current position the head of the list
	template <typename T, bool is_pod>
	void List<T, is_pod>::CurrentToHead()
	{
		PR_ASSERT(m_current);
		if( m_current == m_head ) return;
		
		bool make_linear = false;
		if( !m_circular ) { MakeCircular(); make_linear = true; }
		m_head = m_current;
		m_tail = m_current->m_prev;
		if( make_linear ) MakeLinear();

		BK(BookmarkPosition* bk = m_bookmarks;)
		BK(while( bk ) { bk->m_idx = (bk->m_idx - m_current_index + m_count) % m_count; bk = bk->m_next; })

		m_current_index = 0;
	}

	//*****
	// Make the current position the tail of the list
	template <typename T, bool is_pod>
	void List<T, is_pod>::CurrentToTail()
	{
		PR_ASSERT(m_current);
		if( m_current == m_tail ) return;

		bool make_linear = false;
		if( !m_circular ) { MakeCircular(); make_linear = true; }
		m_tail = m_current;
		m_head = m_current->m_next;
		if( make_linear ) MakeLinear();

		BK(BookmarkPosition* bk = m_bookmarks;)
		BK(while( bk ) { bk->m_idx = (bk->m_idx + (m_count - 1 - m_current_index)) % m_count; bk = bk->m_next; })

		m_current_index = m_count - 1;
	}

	#ifdef LIST_USE_BOOKMARKS
	//***********************************************************************//
	// Bookmark methods
	//*****
	// Register a bookmark
	template <typename T, bool is_pod>
	inline DWORD List<T, is_pod>::Bookmark() const
	{
		if( !m_current ) return 0;
		BookmarkPosition* bk	= m_bookmark_pool.Get();
		bk->m_ptr				= m_current;
		bk->m_idx				= m_current_index;
		bk->m_next				= m_bookmarks;
		m_bookmarks				= bk;
		return reinterpret_cast<DWORD>(bk);
	}
	//*****
	// Cancel a bookmark location
	template <typename T, bool is_pod>
	void List<T, is_pod>::ClearBookmark(DWORD which) const
	{
		if( !m_bookmarks ) return;
		if(  which == 0  ) return;	// This is the case if 'Bookmark' fails
		BookmarkPosition* mark = reinterpret_cast<BookmarkPosition*>(which);
		
		// Only clear valid bookmarks
		if( m_bookmarks == mark )
		{
			m_bookmarks  = mark->m_next;
			mark->m_next = NULL;
			m_bookmark_pool.Return(mark, false);
			return;
		}

		BookmarkPosition* bk = m_bookmarks;
		while( bk->m_next )
		{
			if( bk->m_next == mark )
			{
				bk->m_next		= mark->m_next;
				mark->m_next	= NULL;
				m_bookmark_pool.Return(mark, false);
				return;
			}
			bk = bk->m_next;
		}
	}

	//*****
	// Restore a previously saved bookmark position.
	// Returns true if the bookmark was restored
	template <typename T, bool is_pod>
	bool List<T, is_pod>::RestoreBookmark(DWORD which) const
	{
		if( !m_bookmarks ) return false;
		if(  which == 0  ) return false;	// This is the case if 'Bookmark' fails
		BookmarkPosition* mark = reinterpret_cast<BookmarkPosition*>(which);
		
		// Only restore valid bookmarks
		if( m_bookmarks == mark )
		{
			m_current		= mark->m_ptr;
			m_current_index = mark->m_idx;
			m_bookmarks		= mark->m_next;
			mark->m_next	= NULL;
			m_bookmark_pool.Return(mark, false);
			return true;
		}
		BookmarkPosition* bk = m_bookmarks;
		while( bk->m_next )
		{
			if( bk->m_next == mark )
			{
				m_current		= mark->m_ptr;
				m_current_index = mark->m_idx;
				bk->m_next		= mark->m_next;
				mark->m_next	= NULL;
				m_bookmark_pool.Return(mark, false);
				return true;
			}
			bk = bk->m_next;
		}
		return false;
	}

	//*****
	// Forget all bookmarks
	template <typename T, bool is_pod>
	inline void List<T, is_pod>::ResetBookmarks() const
	{
		BookmarkPosition *tmp, *bk = m_bookmarks;
		while( bk )
		{
			tmp = bk;
			bk  = bk->m_next;
			tmp->m_next = NULL;
			m_bookmark_pool.Return(tmp, false);
		}
	}
	#endif//LIST_USE_BOOKMARKS

	//***********************************************************************//
	// Private methods
	//*****
	template <typename T, bool is_pod>
	inline typename List<T, is_pod>::Node* List<T, is_pod>::_Next() const
	{
		PR_ASSERT(m_current);
		if( !m_current->m_next ) return NULL;
		m_current = m_current->m_next; ++m_current_index;
		if( m_current_index == m_count ) { PR_ASSERT(m_circular); m_current_index = 0; }
		return m_current;
	}

	//*****
	template <typename T, bool is_pod>
	inline typename List<T, is_pod>::Node* List<T, is_pod>::_Prev() const
	{
		PR_ASSERT(m_current);
		if( !m_current->m_prev ) return NULL;
		m_current = m_current->m_prev; --m_current_index;
		if( m_current_index == 0xFFFFFFFF )	{ PR_ASSERT(m_circular); m_current_index = m_count - 1; }
		return m_current;
	}

	//*****
	// Scan the list for the index of 'ptr'
	template <typename T, bool is_pod>
	inline DWORD List<T, is_pod>::GetIndexFor(Node* ptr) const
	{
		DWORD index = 0;
		const Node* node = m_head;
		while( node != ptr && index < m_count )
		{
			++index;
			node = node->m_next;
		}
		if( index == m_count ) index = LIST_INDEX_INVALID;
		return index;
	}

	//*****
	// Return a reference to the 'which' th element in the list
	template <typename T, bool is_pod>
	T& List<T, is_pod>::Index(DWORD which) const
	{
		PR_ASSERT(which < m_count);

		// Try some likely candidates first...
		if( m_current_index != LIST_INDEX_INVALID )
		{
			if( which == m_current_index		) { return m_current->m_object; }
			if( which == m_current_index + 1	) { _Next(); return m_current->m_object; }
			if( which == m_current_index - 1	) { _Prev(); return m_current->m_object; }
			if( which == m_current_index + 2	) { _Next(); _Next(); return m_current->m_object; }
			if( which == m_current_index - 2	) { _Prev(); _Prev(); return m_current->m_object; }
		}
		if( which == 0						) { _First(); return m_current->m_object; }
		if( which == m_count - 1			) { _Last();  return m_current->m_object; }
		
		Find(which);
		return m_current->m_object;
	}

	//*****
	// Remove a node from the list. If 'index' is invalid and there are bookmarks then
	// this method will find the index for 'node' in order to correct the bookmarks.
	// Therefore, 
	template <typename T, bool is_pod>
	void List<T, is_pod>::Remove(Node* node, DWORD index)
	{
		if( node == m_last_thing_added ) m_last_thing_added = NULL;
		
		// If there are bookmarks then we'll need to adjust their indices
		// therefore we need the index position of 'node'
		#ifdef LIST_USE_BOOKMARKS
		RemoveBookmarksThatReference(node);
		if( m_bookmarks && index == LIST_INDEX_INVALID )
		{
			index = GetIndexFor(node);
			PR_ASSERT(index != LIST_INDEX_INVALID);
		}
		#endif//LIST_USE_BOOKMARKS

		if( m_head == m_tail )
		{
			m_head = m_tail = m_current = NULL;
			m_current_index = LIST_INDEX_INVALID;
			BK(PR_ASSERT(!m_bookmarks);)
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
					// m_current_index unchanged
				}
				m_head = m_head->m_next;
			}
			else if( m_tail == node )
			{
				if( m_circular )
				{
					if( m_current == m_tail )
					{
						m_current = m_current->m_next;
						m_current_index = 0;
					}
					m_tail = m_tail->m_next;
				}
				else
				{
					if( m_current == m_tail )
					{
						m_current = m_current->m_prev;
						--m_current_index;
					}
					m_tail = m_tail->m_prev;
				}
			}
			else if( m_current == node )
			{
				m_current = m_current->m_next;
				// m_current_index unchanged
			}
			else if( index == LIST_INDEX_INVALID )
			{
				m_current = m_head;
				m_current_index = 0;
			}
			else
			{
				if( m_current_index > index ) --m_current_index;
			}
		}
		--m_count;
		
		BK(BookmarkPosition *bk = m_bookmarks;)
		BK(while( bk ) { if( bk->m_idx > index ) { --bk->m_idx; } bk = bk->m_next; })

		node->m_next = NULL;
		m_node_pool.Return(node);
	}	

	#ifdef LIST_USE_BOOKMARKS
	//*****
	// Remove all bookmarks that reference a particular node
	template <typename T, bool is_pod>
	void List<T, is_pod>::RemoveBookmarksThatReference(Node* node) const
	{
		BookmarkPosition *delete_me, *prev = NULL, *bk = m_bookmarks;
		while( bk )
		{
			if( bk->m_ptr == node )
			{
				if( !prev )	{ delete_me = m_bookmarks;	m_bookmarks = m_bookmarks->m_next; }
				else		{ delete_me = bk; bk = bk->m_next; prev->m_next = bk; }
				
				delete_me->m_next = NULL;
				m_bookmark_pool.Return(delete_me, false);
			}
			else
			{
				prev = bk;
				bk = bk->m_next;
			}
		}
	}
	#endif//LIST_USE_BOOKMARKS

	//*****
	// Check the integrity of the list
	template <typename T, bool is_pod>
	bool List<T, is_pod>::Verify() const
	{
		if( !m_head || !m_tail )
		{
			PR_ASSERT(!m_head);
			PR_ASSERT(!m_tail);
			PR_ASSERT(!m_current);
			PR_ASSERT(m_current_index == LIST_INDEX_INVALID);
			PR_ASSERT(m_count == 0);
			return true;
		}
		if( m_circular )
		{
			PR_ASSERT(m_head->m_prev == m_tail);
			PR_ASSERT(m_tail->m_next == m_head);
		}
		else
		{
			PR_ASSERT(m_head->m_prev == NULL);
			PR_ASSERT(m_tail->m_next == NULL);
		}
		bool current_is_valid = (m_current) ? (false) : (true);
		DWORD count = 0;
		const Node* node = m_head;
		do
		{
			++count;
			if( node == m_current )
			{
				current_is_valid = true;
				PR_ASSERT(m_current_index == count - 1);
			}
			node = node->m_next;
		}
		while( node && node != m_head );
		PR_ASSERT(count == m_count);
		PR_ASSERT(current_is_valid);
		return true;
	}

}// namespace PR

#undef BK

#endif//LIST_H