//***********************************************************************//
//                     A Template binary tree class                      //
//                                                                       //
//                  (c)opyright Nov 2002 Paul Ryland                     //
//                                                                       //
//***********************************************************************//
//
//	Optional defines
//		CHECK_TREE_AFTER_INSERTION_OR_DELETION	-	Checks the integrity of the tree structure
//		BALANCED_TREE	-	Turns this tree into an AVL balanced tree
//
//	Methods and members that can be required for this template class:
//		bool T::BinaryTreeIsEqual(const T& other) const;
//		bool T::BinaryTreeIsLeftOf(const T& other) const;
//		bool T::BinaryTreeIsRightOf(const T& other) const;
//		void T::Print();
//		BinaryTree<T>::TreeRef m_totree;
//
#ifndef BINARYTREE_H
#define BINARYTREE_H

#include "MemPool.h"

#ifdef BALANCED_TREE
#undef BALANCED_TREE
#define BALANCED_TREE(exp) exp
#else
#define BALANCED_TREE(exp)
#endif//_BALANCED_TREE

//***************************************************************************************************************
// Global BinaryTree functions
template <typename T>
inline T* BinaryTreeDetach(T *obj);

//***************************************************************************************************************
// BinaryTree class
template <typename T>
class BinaryTree
{
public:
	BinaryTree(DWORD estimated_size = 100);
	~BinaryTree();

	DWORD GetCount() const { return m_count; }
	void FindClosest(const T *obj, const T*& dup) const;
	bool Find(const T *obj, const T*& dup) const;
	bool FindUsing(const T *obj, const T*& dup, bool(*Equal)(const T& a, const T& b), bool(*Greater)(const T& a, const T& b), bool(*Less)(const T& a, const T& b)) const; 
	bool Insert(T *obj);
	bool InsertUnique(T *obj, T*& dup);
	void TraverseLeftToRight(void (*ActionFunc)(T &object, void* data), void* data = NULL);
	T*   LeftMost();
	T*   RightOne();
	void TraverseRightToLeft(void (*ActionFunc)(T &object, void* data), void* data = NULL);
	T*   RightMost();
	T*   LeftOne();
	void EndIterate();
	void ShouldNotBeDestroyed( bool should_not ) { m_should_not_be_destroyed = should_not; }
	void Destroy();
	void Desolve();
	void ReleaseMemory();

private:
	struct Node;
	typedef Node* NODEPTR;
	BALANCED_TREE(void EnsureBalanceAfterInsertion(Node* start));
	BALANCED_TREE(void EnsureBalanceAfterDeletion(Node* start));
	BALANCED_TREE(void RollLeft(Node* top));
	BALANCED_TREE(void RollRight(Node* top));
	void DesolveOrDestroy(bool destroy);
	friend T* BinaryTreeDetach(T *obj);

	struct Node
	{
		T		*m_object;					// The object in this node
		union {	NODEPTR m_left, m_next; };	// Left branch of the tree, or a pointer to the next Node when used in a stack or queue
		union { NODEPTR m_right, m_node;};	// Right branch of the tree, or a pointer into the tree when used in a stack or queue
		NODEPTR	 m_parent;					// The parent to this node
		int		 m_balance;					// The AVL balance factor for this node (-1, 0, 1)
		bool CheckBalance()
		{
			if( m_balance == 0 ) return ((m_left == NULL) == (m_right == NULL));
			else return true;
		}
	};
	MemPool<Node> m_node_pool;				// A pool of nodes

	// A small stack implementation for use when navigating trees
	struct TreeStack
	{
		TreeStack(MemPool<Node>& node_pool) : m_top(NULL), m_node_pool(node_pool) {}
		TreeStack& operator=(const TreeStack& other) { PR_ASSERT(false); other; return this; } // Shouldn't be assigning these
		bool IsEmpty()	{ return m_top == NULL; }
		Node* Top()		{ return m_top->m_node; }
		void Empty()	{ while( !IsEmpty() ) Pop(); }
		void Push(Node* node) { Node* temp = m_node_pool.Get(); temp->m_node = node; temp->m_next = m_top; m_top = temp; }
		Node* Pop()
		{
			PR_ASSERT(!IsEmpty());	// Can't get from an empty stack
			Node* detached = m_top; Node* detached_node = m_top->m_node;
			m_top = m_top->m_next; detached->m_next = NULL; m_node_pool.Return(detached);
			return detached_node;
		}
	private:
		Node* m_top;
		MemPool<Node>& m_node_pool;
	};

	// A small queue implementation for use when navigating trees
	struct TreeQueue
	{
		TreeQueue(MemPool<Node>& node_pool) : m_node_pool(node_pool), m_front(&m_dummy), m_back(&m_dummy) {}
		TreeQueue& operator=(const TreeQueue& other) { PR_ASSERT(false); other; return this; } // Shouldn't be assigning these
		bool IsEmpty()	{ return m_front == m_back; }
		Node* Head()	{ return m_front->m_next->m_right; }
		void Empty()	{ while( !IsEmpty() ) Get(); }
		void Add(Node* node)
		{
			Node* temp = m_node_pool.Get();
			temp->m_node = node;
			m_back->m_next = temp;
			m_back = temp;
		}
		Node* Get()
		{
			PR_ASSERT(!IsEmpty());	// Can't get from an empty queue
			Node* detached		= m_front->m_next;
			Node* detached_node = detached->m_right;
			if( detached == m_back ) m_back = m_front;
			else					 m_front->m_next = detached->m_next;
			detached->m_next = NULL; m_node_pool.Return(detached);
			return detached_node;
		}
	private:
		Node *m_front, *m_back, m_dummy;
		MemPool<Node>& m_node_pool;
	};
	TreeStack m_stack;
	TreeQueue m_queue;

public:
	class TreeRef
	{
	public:
		TreeRef() { Reset(); }
		bool IsInTree() const		{ return m_mytree != NULL && m_mynode != NULL; }
		BinaryTree<T>* GetTree()	{ return m_mytree; }
	private:
		friend class BinaryTree<T>;
		friend T* BinaryTreeDetach(T *obj);
		void Reset() { m_mytree = NULL; m_mynode = NULL; }
		BinaryTree<T>* m_mytree;
		Node*		   m_mynode;
	};

	// Debugging methods
	#ifdef _DEBUG
	void Print();
	bool CheckReferences();
	BALANCED_TREE(bool CheckTree(Node* tree = NULL, int* count = NULL));
	#endif//_DEBUG

private:
	Node *m_tree;
	DWORD m_count;
	bool  m_should_not_be_destroyed;
};

//***************************************************************************************************************
// Public BinaryTree methods
//*****
// Constructor
template <typename T>
inline BinaryTree<T>::BinaryTree(DWORD estimated_size) :
	m_node_pool(estimated_size),
	m_stack(m_node_pool),
	m_queue(m_node_pool),
	m_tree(NULL),
	m_count(0),
	m_should_not_be_destroyed(false)
{}

//*****
// Destructor
template <typename T>
inline BinaryTree<T>::~BinaryTree()
{
	PR_ASSERT( m_tree == NULL );
	// The binary tree does not necessarily own the objects it points to.
	// Therefore it cannot delete them during destruction
	// If this PR_ASSERT fires then check for the following:
	// 1) You've forgotten to call Destroy or Desolve
	//    before the tree is being deleted
	// 2) You've made a local instance of a tree that is going
	//    out of scope
	m_stack.Empty();
	m_queue.Empty();
	m_node_pool.ReleaseMemory();
}

//*****
// Search the tree for an object that is the closest match for 'obj'. This method uses
// BinaryTreeIsLeftOf and BinaryTreeIsRightOf to navigate down the tree. When 'obj' is
// neither left or right of an object in the tree, dup is set to point to the object.
template <typename T>
inline void BinaryTree<T>::FindClosest(const T *obj, const T*& dup) const
{
	if( !m_tree ) { dup = NULL; return; }

	Node*const* pptree = &m_tree;
	while( *pptree )
	{
		const Node* tree = *pptree;
			 if( tree->m_left && obj->BinaryTreeIsLeftOf(*tree->m_object) )   pptree = &(tree->m_left);
		else if( tree->m_right && obj->BinaryTreeIsRightOf(*tree->m_object) ) pptree = &(tree->m_right);
		else break;
	}
	PR_ASSERT( *pptree != NULL );
	dup = (*pptree)->m_object;
}

//*****
// Search the tree for 'obj'. This method returns true if 'obj' is found and
// 'dup' is set to point at the found object. This method uses BinaryTreeIsLeftOf
// and BinaryTreeIsRightOf to navigate down the tree. Only when 'obj' is neither
// left or right of an object in the tree is it tested using BinaryTreeIsEqual.
// If BinaryTreeIsEqual returns true then dup is set to point to the duplicate
// and true is returned otherwise false is returned
template <typename T>
inline bool BinaryTree<T>::Find(const T *obj, const T*& dup) const
{
	FindClosest(obj, dup);
	if( dup == NULL ) return false;
	if( obj->BinaryTreeIsEqual(*dup) ) return true;
	return false;
}

//*****
// Insert into the tree. This method returns true if the object was
// inserted into the tree. An object may not be inserted if the overrideable
// methods BinaryTreeIsLeftOf and BinaryTreeIsRightOf do not include the
// case were the object is equal to another object in the tree
template <typename T>
inline bool BinaryTree<T>::Insert(T *obj)
{
	T* dup = NULL;
	return InsertUnique(obj, dup);
}

//*****
// Insert an object uniquely into the tree. This method returns true if the
// object was inserted into the tree. If the object is already in the
// tree then 'dup' is set to point to the duplicate object and 'obj' is not
// added to the tree (i.e. returns false). NOTE: Two objects are assumed
// equal if both BinaryTreeIsLeftOf and BinaryTreeIsRightOf return false
template <typename T>
inline bool BinaryTree<T>::InsertUnique(T *obj, T*& dup)
{
	// Prepare a new node for insertion
	Node* node = m_node_pool.Get();
	node->m_object	= obj;
	node->m_parent	= NULL;
	node->m_left	= NULL;
	node->m_right	= NULL;
	node->m_balance	= 0;

	// Search for the insertion point in the tree
	Node** pptree = &m_tree;
	while( *pptree )
	{	
		Node* tree = *pptree;
		PR_ASSERT(tree->m_object );
		PR_ASSERT(tree->m_object != obj);	// You can't insert the same object in two places within the tree
			 if( obj->BinaryTreeIsLeftOf(*tree->m_object) )  pptree = &(tree->m_left);
		else if( obj->BinaryTreeIsRightOf(*tree->m_object) ) pptree = &(tree->m_right);
		else { m_node_pool.Return(node); dup = tree->m_object; return false; }	// The object is not unique
		node->m_parent = tree;
	}
	
	// An insertion point was found, add the node to the tree and update the objects back reference into the tree
	*pptree = node;
	obj->m_totree.m_mytree = this;
	obj->m_totree.m_mynode = node;
	++m_count;
	dup = NULL;
	
	// Balance the tree
	BALANCED_TREE(EnsureBalanceAfterInsertion(node));
	#ifdef CHECK_TREE_AFTER_INSERTION_OR_DELETION
	PR_ASSERT(CheckReferences());
	#endif//CHECK_TREE_AFTER_INSERTION_OR_DELETION
	return true;
}

//*****
// This method tranverses the tree from left
// to right calling 'ActionFunc' for each object.
template <typename T>
inline void BinaryTree<T>::TraverseLeftToRight(void (*ActionFunc)(T &object, void* data), void* data)
{
	if( !m_tree ) return;

	PR_ASSERT(m_stack.IsEmpty());	// The last user didn't clean up after using this
	Node* tree = m_tree;
	m_stack.Push(tree);
	bool from_stack = false;
	while( !m_stack.IsEmpty() )
	{
		while( !from_stack && tree->m_left ) { tree = tree->m_left; m_stack.Push(tree); }

		m_stack.Pop();
		ActionFunc(*tree->m_object, data);
		
		if( tree->m_right )				{ tree = tree->m_right; m_stack.Push(tree); from_stack = false; }
		else if( !m_stack.IsEmpty() )	{ tree = m_stack.Top(); from_stack = true; }
	}
}

//*****
// Go to the leftmost node in the tree in preparation for iteration from left to right
template <typename T>
inline T* BinaryTree<T>::LeftMost()
{
	if( !m_tree ) return NULL;

	PR_ASSERT(m_stack.IsEmpty());	// Remember to call "EndIterate" after each iteration loop
	Node* tree = m_tree;
	do
	{
		m_stack.Push(tree);
		tree = tree->m_left;
	}
	while( tree );
	return m_stack.Top()->m_object;
}

//*****
// Go to the next node to the right. Come into the method assuming the top of the stack has already been returned
template <typename T>
inline T* BinaryTree<T>::RightOne()
{
	if( m_stack.IsEmpty() ) return NULL;

	if( m_stack.Top()->m_right )
	{
		Node* tree = m_stack.Top()->m_right;
		do
		{
			m_stack.Push(tree);
			tree = tree->m_left;
		}
		while( tree );
	}
	else
	{
		Node* prev;
		do
		{
			prev = m_stack.Pop();
		}
		while( !m_stack.IsEmpty() && m_stack.Top()->m_right == prev );
	}
	if( m_stack.IsEmpty() ) return NULL;
	return m_stack.Top()->m_object;
}

//*****
// This method tranverses the tree from right
// to left calling 'ActionFunc' for each object.
template <typename T>
inline void BinaryTree<T>::TraverseRightToLeft(void (*ActionFunc)(T &object, void* data), void* data)
{
	if( !m_tree ) return;

	PR_ASSERT(m_stack.IsEmpty());	// The last user didn't clean up after using this
	Node* tree = m_tree;
	m_stack.Push(tree);
	bool from_stack = false;
	while( !m_stack.IsEmpty() )
	{
		while( !from_stack && tree->m_right ) { tree = tree->m_right; m_stack.Push(tree); }

		m_stack.Pop();
		ActionFunc(*tree->m_object, data);
		
		if( tree->m_left )				{ tree = tree->m_left; m_stack.Push(tree); from_stack = false; }
		else if( !m_stack.IsEmpty() )	{ tree = m_stack.Top(); from_stack = true; }
	}
}

//*****
// Go to the rightmost node in the tree in preparation for iteration from right to left
template <typename T>
inline T* BinaryTree<T>::RightMost()
{
	if( !m_tree ) return NULL;

	PR_ASSERT(m_stack.IsEmpty());	// Remember to call "EndIterate" after each iteration loop
	Node* tree = m_tree;
	do
	{
		m_stack.Push(tree);
		tree = tree->m_right;
	}
	while( tree );
	return m_stack.Top()->m_object;
}

//*****
// Go to the next node to the left. Come into the method assuming the top of the stack has already been returned
template <typename T>
inline T* BinaryTree<T>::LeftOne()
{
	if( m_stack.IsEmpty() ) return NULL;

	if( m_stack.Top()->m_left )
	{
		Node* tree = m_stack.Top()->m_left;
		do
		{
			m_stack.Push(tree);
			tree = tree->m_right;
		}
		while( tree );
	}
	else
	{
		Node* prev;
		do
		{
			prev = m_stack.Pop();
		}
		while( !m_stack.IsEmpty() && m_stack.Top()->m_left == prev );
	}
	if( m_stack.IsEmpty() ) return NULL;
	return m_stack.Top()->m_object;
}

//*****
// Clean up after iterating
template <typename T>
inline void BinaryTree<T>::EndIterate()
{
	m_stack.Empty();
}

//*****
// Deletes the objects in the tree and returns the nodes of the tree to the
// memory pool. To free cached memory used by this tree call ReleaseMemory()
template <typename T>
inline void BinaryTree<T>::Destroy()
{
	PR_ASSERT( m_should_not_be_destroyed == false || m_tree == NULL );
	if( !m_should_not_be_destroyed ) DesolveOrDestroy(true);
}

//*****
// Returns the nodes of the tree to the memory pool but does not delete
// the objects in the tree. To free cached memory used by this tree
// call ReleaseMemory()
template <typename T>
inline void BinaryTree<T>::Desolve()
{
	PR_ASSERT( m_should_not_be_destroyed == true || m_tree == NULL );
	if( m_should_not_be_destroyed ) DesolveOrDestroy(false);
}

//*****
// Release memory associated with the node pool for this tree.
// As the tree grows, nodes are created, these are not destroyed
// until the tree is destructed. This method allows this memory to
// be released without destructing the tree. NOTE: Destroy or Desolve
// must be called before a call to this method
template <typename T>
void BinaryTree<T>::ReleaseMemory()
{
	PR_ASSERT(m_tree == NULL);
	m_node_pool.SetPoolSize(0);
}

//***************************************************************************************************************
// Diagnostic Methods
#ifdef _DEBUG
//*****
// Print the tree to the console
template <typename T>
inline void BinaryTree<T>::Print()
{
	PR_ASSERT(m_queue.IsEmpty());
	printf( "Tree: %d objects\n", GetCount());
	if( !m_tree ) return;
	int objs_in_this_row = 1;
	int objs_in_next_row = 0;
	m_queue.Add(m_tree);
	do
	{
		Node* tree = m_queue.Get();
		if( tree->m_left )	{ m_queue.Add(tree->m_left);	++objs_in_next_row; }
		if( tree->m_right )	{ m_queue.Add(tree->m_right);	++objs_in_next_row; }
		tree->m_object->Print();
		if( --objs_in_this_row == 0 )
		{
			printf("\n");
			objs_in_this_row = objs_in_next_row;
			objs_in_next_row = 0;
		}
		else
			printf(" ");
	}
	while( !m_queue.IsEmpty() );
}

//*****
// Check the 'm_totree' member of each object in the tree
template <typename T>
inline bool BinaryTree<T>::CheckReferences()
{
	if( !m_tree ) return true;
	bool all_ok = true;
	PR_ASSERT(m_stack.IsEmpty());
	m_stack.Push(m_tree);
	do
	{
		Node* tree = m_stack.Pop();
		if( tree->m_left )	m_stack.Push(tree->m_left);
		if( tree->m_right )	m_stack.Push(tree->m_right);

		if( tree->m_left && tree->m_left->m_parent != tree )
		{
			PR_ASSERT(false);
			all_ok = false;
		}

		if( tree->m_right && tree->m_right->m_parent != tree )
		{
			PR_ASSERT(false);
			all_ok = false;
		}

		if( tree->m_object->m_totree.m_mynode != tree )
		{
			PR_ASSERT(false);
			all_ok = false;
		}

		#ifdef _BALANCED_TREE
		if( !tree->CheckBalance() )
		{
			PR_ASSERT(false);
			all_ok = false;
		}
		#endif//_BALANCED_TREE
	}
	while( !m_stack.IsEmpty() );
	return all_ok;
}

//*****
// Debugging method
#ifdef _BALANCED_TREE
template <typename T>
bool BinaryTree<T>::CheckTree(Node* tree, int* count)
{
	if( tree == NULL )
		tree = m_tree;
	
	int left_count = 0;
	int right_count = 0;
	if( tree->m_left ) CheckTree(tree->m_left, &left_count);
	if( tree->m_right) CheckTree(tree->m_right, &right_count);

	if( tree->m_balance == -1 )
	{
		if( left_count != right_count + 1 )
			PR_ASSERT(false);
		if( count ) *count = left_count + 1;
	}
	else if( tree->m_balance == 0 )
	{
		if( left_count != right_count )
			PR_ASSERT(false);
		if( count ) *count = left_count + 1;
	}
	else if( tree->m_balance == 1 )
	{
		if( right_count != left_count + 1 )
			PR_ASSERT(false);
		if( count ) *count = right_count + 1;
	}
	else
		PR_ASSERT(false);
	// This method returns bool so that it can be used in PR_ASSERTs
	return true;
}
#endif//_BALANCED_TREE
#endif//_DEBUG

//***************************************************************************************************************
// Private BinaryTree Methods
//*****
// This method assumes that 'start' has been added to the tree.
// It propagates up the tree ensuring that the tree is balanced.
#ifdef _BALANCED_TREE
template <typename T>
inline void BinaryTree<T>::EnsureBalanceAfterInsertion(Node* start)
{
	Node* node = start;
	PR_ASSERT(node->m_balance == 0);
	Node* parent = node->m_parent;
	while( parent )
	{
		// Adjust the balance depending on which side of the parent 'node' is on
		if( parent->m_left == node )			--parent->m_balance;
		else { PR_ASSERT(parent->m_right == node); ++parent->m_balance; }

		int bal;
		if( parent->m_balance == -2)
		{
			if( node->m_balance == 1 )
			{
				bal = node->m_right->m_balance;
				RollLeft(node);
					 if( bal == -1 ) {   node->m_parent->m_balance = -2; node->m_balance =  0; }
				else if( bal ==  0 ) {   node->m_parent->m_balance = -1; node->m_balance =  0; }
				else { PR_ASSERT(bal == 1); node->m_parent->m_balance = -1; node->m_balance = -1; }
				node = node->m_parent;
			}
			RollRight(parent);
				 if( node->m_balance == -2 ) { parent->m_parent->m_balance = 0; parent->m_balance =  1; }
			else if( node->m_balance == -1 ) { parent->m_parent->m_balance = 0; parent->m_balance =  0; }
			else if( node->m_balance ==  0 ) { parent->m_parent->m_balance = 1; parent->m_balance = -1; }
			else { PR_ASSERT(false); } // Shouldn't get here

			#ifdef CHECK_TREE_AFTER_INSERTION_OR_DELETION
			PR_ASSERT(CheckTree());
			#endif//CHECK_TREE_AFTER_INSERTION_OR_DELETION
			PR_ASSERT(parent->CheckBalance());
			PR_ASSERT(!parent->m_right || parent->m_right->CheckBalance());
			PR_ASSERT(!parent->m_left  || parent->m_left->CheckBalance());
			break;
		}
		else if( parent->m_balance == 2 )
		{
			if( node->m_balance == -1 )
			{
				bal = node->m_left->m_balance;
				RollRight(node);
					 if( bal == 1 ) {	  node->m_parent->m_balance = 2; node->m_balance = 0; }
				else if( bal == 0 ) {     node->m_parent->m_balance = 1; node->m_balance = 0; }
				else { PR_ASSERT(bal == -1); node->m_parent->m_balance = 1; node->m_balance = 1; }
				node = node->m_parent;
			}
			RollLeft(parent);
				 if( node->m_balance == 2 ) { parent->m_parent->m_balance =  0; parent->m_balance = -1; }
			else if( node->m_balance == 1 ) { parent->m_parent->m_balance =  0; parent->m_balance =  0; }
			else if( node->m_balance == 0 ) { parent->m_parent->m_balance = -1; parent->m_balance =  1; }
			else { PR_ASSERT(false); } // Shouldn't get here
			
			#ifdef CHECK_TREE_AFTER_INSERTION_OR_DELETION
			PR_ASSERT(CheckTree());
			#endif//CHECK_TREE_AFTER_INSERTION_OR_DELETION
			PR_ASSERT(parent->CheckBalance());
			PR_ASSERT(!parent->m_left  || parent->m_left->CheckBalance());
			PR_ASSERT(!parent->m_right || parent->m_right->CheckBalance());
			break;
		}
		else if( parent->m_balance == 0 )
			break;
		
		// Move up the tree
		node = parent;
		parent = node->m_parent;
	}
}

//*****
// This method assumes that 'start' is to be deleted from the tree.
// It propagates up the tree ensuring that the tree will be balanced.
template <typename T>
inline void BinaryTree<T>::EnsureBalanceAfterDeletion(Node* start)
{
	Node* node	 = start;
	Node* parent = node->m_parent;
	Node* other  = NULL;
	while( parent )
	{
		// Adjust the balance depending on which side of the parent 'node' is on
		if( parent->m_left == node ) {			other = parent->m_right; ++parent->m_balance; }
		else { PR_ASSERT(parent->m_right == node); other = parent->m_left;  --parent->m_balance; }
		
		int bal;
		if( parent->m_balance == -2)
		{
			if( other->m_balance == 1 )
			{
				bal = other->m_right->m_balance;
				RollLeft(other);
					 if( bal == -1 ) {   other->m_parent->m_balance = -2; other->m_balance =  0; }
				else if( bal ==  0 ) {   other->m_parent->m_balance = -1; other->m_balance =  0; }
				else { PR_ASSERT(bal == 1); other->m_parent->m_balance = -1; other->m_balance = -1; }
				other = other->m_parent;
			}
			RollRight(parent);
				 if( other->m_balance == -2 ) { parent->m_parent->m_balance = 0; parent->m_balance =  1; }
			else if( other->m_balance == -1 ) {	parent->m_parent->m_balance = 0; parent->m_balance =  0; }
			else if( other->m_balance ==  0 ) { parent->m_parent->m_balance = 1; parent->m_balance = -1; }
			else { PR_ASSERT(false); } // Shouldn't get here
			parent = parent->m_parent;
		}
		else if( parent->m_balance == 2 )
		{
			if( other->m_balance == -1 )
			{
				bal = other->m_left->m_balance;
				RollRight(other);
					 if( bal == 1 ) {	  other->m_parent->m_balance = 2; other->m_balance = 0; }
				else if( bal == 0 ) {     other->m_parent->m_balance = 1; other->m_balance = 0; }
				else { PR_ASSERT(bal == -1); other->m_parent->m_balance = 1; other->m_balance = 1; }
				other = other->m_parent;
			}
			RollLeft(parent);
				 if( other->m_balance == 2 ) { parent->m_parent->m_balance =  0; parent->m_balance = -1; }
			else if( other->m_balance == 1 ) { parent->m_parent->m_balance =  0; parent->m_balance =  0; }
			else if( other->m_balance == 0 ) { parent->m_parent->m_balance = -1; parent->m_balance =  1; }
			else { PR_ASSERT(false); } // Shouldn't get here
			parent = parent->m_parent;
		}
		
		if( parent->m_balance != 0 )
			break;

		// Move up the tree
		node = parent;
		parent = node->m_parent;
	}
}

//*****
// This method is called to balance a section of the tree.
template <typename T>
inline void BinaryTree<T>::RollRight(Node* top)
{
	PR_ASSERT( top && top->m_left != NULL );
	Node* left = top->m_left;

	bool have_parent = top->m_parent != NULL;
	bool is_left = true;
	if( have_parent )
	{
		is_left = top->m_parent->m_left == top;
		PR_ASSERT(is_left || top->m_parent->m_right == top);
		PR_ASSERT(left->m_parent == top);
	}

	// Adjust the parent pointers
	left->m_parent = top->m_parent;
	top->m_parent = left;
	if( have_parent )
	{
		if( is_left ) left->m_parent->m_left  = left;
		else		  left->m_parent->m_right = left;
	}
	else
		m_tree = left;
	
	// Adjust the left and right subtrees
	top->m_left = left->m_right;
	if( top->m_left ) top->m_left->m_parent = top;
	left->m_right = top;

	// Fix the back references for the moved nodes
	top->m_object->m_totree.m_mynode = top;
	left->m_object->m_totree.m_mynode = left;
	if( top->m_left ) top->m_left->m_object->m_totree.m_mynode = top->m_left;
}

//*****
// This method is called to balance a section of the tree.
template <typename T>
inline void BinaryTree<T>::RollLeft(Node* top)
{
	PR_ASSERT( top && top->m_right != NULL );
	Node* right = top->m_right;

	bool have_parent = top->m_parent != NULL;
	bool is_right = true;
	if( have_parent )
	{
		is_right = top->m_parent->m_right == top;
		PR_ASSERT(is_right || top->m_parent->m_left == top);
		PR_ASSERT(right->m_parent == top);
	}
	
	// Adjust the parent pointers
	right->m_parent = top->m_parent;
	top->m_parent = right;
	if( have_parent )
	{
		if( is_right ) right->m_parent->m_right = right;
		else		   right->m_parent->m_left  = right;
	}
	else
		m_tree = right;

	// Adjust the left and right subtrees
	top->m_right = right->m_left;
	if( top->m_right ) top->m_right->m_parent = top;
	right->m_left = top;

	// Fix the back references for the moved nodes
	top->m_object->m_totree.m_mynode = top;
	right->m_object->m_totree.m_mynode = right;
	if( top->m_right ) top->m_right->m_object->m_totree.m_mynode = top->m_right;
}
#endif//_BALANCED_TREE

//*****
// Returns the nodes of the tree to the memory pool.
// Deletes the objects in the tree if 'destroy' is tree.
template <typename T>
inline void BinaryTree<T>::DesolveOrDestroy(bool destroy)
{
	if( m_tree )
	{
		m_stack.Push(m_tree);
		m_tree = NULL;
		do
		{
			Node* tree = m_stack.Pop();

			// If this PR_ASSERT fires then the tree is malformed. Either both branches point
			// to the same thing or each branch contains a pointer to the same object
			PR_ASSERT(  tree->m_left == NULL || tree->m_right == NULL ||
					(tree->m_left != tree->m_right && tree->m_left->m_object != tree->m_right->m_object) );

			if( tree->m_left )	m_stack.Push(tree->m_left);
			if( tree->m_right )	m_stack.Push(tree->m_right);

			tree->m_object->m_totree.Reset();
			if( destroy ) delete tree->m_object;
			tree->m_next = NULL;
			m_node_pool.Return(tree);
		}
		while( !m_stack.IsEmpty() );
	}
	m_count = 0;
}

//***************************************************************************************************************
// Global BinaryTree functions
//*****
// This method uses the m_totree tree reference to remove 'obj' from the tree.
// Callers should use the Find method to locate the object they wish to remove.
template <typename T>
inline T* BinaryTreeDetach(T *obj)
{
	// Check that 'obj' is actually in a tree
	PR_ASSERT(obj->m_totree.m_mytree != NULL && obj->m_totree.m_mynode != NULL);
	if( obj->m_totree.m_mytree == NULL || obj->m_totree.m_mynode == NULL ) return NULL;

	BinaryTree<T>*		 tree = obj->m_totree.m_mytree;
	BinaryTree<T>::Node* node = obj->m_totree.m_mynode;
	BinaryTree<T>::Node** ptr = &(tree->m_tree);
	if( node->m_parent != NULL )
	{
		if( node->m_parent->m_left == node )			ptr = &(node->m_parent->m_left);
		else { PR_ASSERT(node->m_parent->m_right == node);	ptr = &(node->m_parent->m_right); }
	}

	// If 'node' only has one other branch we can just unlink it from the tree
	if( node->m_left == NULL || node->m_right == NULL )
	{
		BALANCED_TREE(tree->EnsureBalanceAfterDeletion(node));
			 if( node->m_left  != NULL ) *ptr = node->m_left;
		else if( node->m_right != NULL ) *ptr = node->m_right;
		else							 *ptr = NULL;
		if( *ptr ) (*ptr)->m_parent = node->m_parent;
		node->m_next = NULL;
		tree->m_node_pool.Return(node);
	}
	// Otherwise we need to replace it with the rightmost on the lefthand side or the leftmost
	// on the righthand side, whichever may result in a more balanced tree
	else
	{
		bool use_lefthand_side = node->m_balance < 1;
		BinaryTree<T>::Node* swap = (use_lefthand_side)?(node->m_left):(node->m_right);
		BinaryTree<T>::Node** ptr = (use_lefthand_side)?(&(node->m_left)):(&(node->m_right));
		if( use_lefthand_side )	// Find the rightmost on the lefthand side
			while( swap->m_right ) { ptr = &(swap->m_right); swap = swap->m_right; }
		else					// Find the leftmost on the righthand side
			while( swap->m_left  ) { ptr = &(swap->m_left);  swap = swap->m_left;  }

		// Move the found object into 'node's position
		node->m_object = swap->m_object;
		node->m_object->m_totree.m_mynode = node;

		// Delete the found node
		BALANCED_TREE(tree->EnsureBalanceAfterDeletion(swap));
			 if( swap->m_left  != NULL ) *ptr = swap->m_left;
		else if( swap->m_right != NULL ) *ptr = swap->m_right;
		else							 *ptr = NULL;
		if( *ptr ) (*ptr)->m_parent = swap->m_parent;
		swap->m_next = NULL;
		tree->m_node_pool.Return(swap);
	}

	--tree->m_count;
	#ifdef CHECK_TREE_AFTER_INSERTION_OR_DELETION
	PR_ASSERT(tree->CheckReferences());
	BALANCED_TREE(PR_ASSERT(tree->CheckTree()));
	#endif//CHECK_TREE_AFTER_INSERTION_OR_DELETION

	// Remove the objects reference to the tree
	obj->m_totree.Reset();
	return obj;
}	

#endif//BINARYTREE_H