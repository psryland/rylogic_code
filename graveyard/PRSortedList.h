//***********************************************************************//
//                     A Template sorted list class                      //
//	Original version:                                                    //
//		P. Ryland, 2003                                                  //
//                                                                       //
//***********************************************************************//
//
// Special notes about Sorted Lists:
//	1) Calling code must define bool SortedListIsLess(const T& a, const T& b)
//  2) Back referencing assumes 'T' is a pointer to an object that defines
//     SortedList<T, is_pod>::ListRef m_tolist
//
//	inline bool SortedListIsLess(const char& a,		const char& b)		{ return a < b; }
//	inline bool SortedListIsLess(const short& a,	const short& b)		{ return a < b; }
//	inline bool SortedListIsLess(const int& a,		const int& b)		{ return a < b; }
//	inline bool SortedListIsLess(const long& a,		const long& b)		{ return a < b; }
//	inline bool SortedListIsLess(const float& a,	const float& b)		{ return a < b; }
//	inline bool SortedListIsLess(const double& a,	const double& b)	{ return a < b; }
//	inline bool SortedListIsLess(const unsigned char& a,	const unsigned char& b)		{ return a < b; }
//	inline bool SortedListIsLess(const unsigned short& a,	const unsigned short& b)	{ return a < b; }
//	inline bool SortedListIsLess(const unsigned int& a,		const unsigned int& b)		{ return a < b; }
//	inline bool SortedListIsLess(const unsigned long& a,	const unsigned long& b)		{ return a < b; }

//
#ifndef SORTEDLIST_H
#define SORTEDLIST_H

#include "Common\PRNew.h"
#include "Common\PRAssert.h"
#include "Common\MemPool.h"
#include "Common\PRList.h"

namespace PR
{

typedef int SortedListSide;
const SortedListSide	SORTED_LIST_LEFT = 0;
const SortedListSide	SORTED_LIST_RITE = 1;

//*****
// Global functions
template <typename T, bool is_pod>
void SortedListDetach(const T& object);

//*****
// The sorted list.
template <typename T, bool is_pod>
class SortedList : protected List<T, is_pod>
{
public:
	SortedList(DWORD estimated_size = 1);
	SortedList(const SortedList<T, is_pod>& copy);
	~SortedList();
	SortedList<T, is_pod>& operator = (const SortedList<T, is_pod>& copy) { this->~SortedList(); return *new (this) SortedList<T, is_pod>(copy); }

	// Accessor
	DWORD GetCount() const		{ return List<T, is_pod>::GetCount(); }
	DWORD CurrentIndex() const	{ return List<T, is_pod>::CurrentIndex(); }
	T Head() const				{ return List<T, is_pod>::Head(); }
	T Current() const			{ return List<T, is_pod>::Current(); }
	T Tail() const				{ return List<T, is_pod>::Tail(); }
	T* HeadP() const			{ return List<T, is_pod>::HeadP(); }
	T* CurrentP() const			{ return List<T, is_pod>::CurrentP(); }
	T* TailP() const			{ return List<T, is_pod>::TailP(); }
	T& RefHead() const			{ return List<T, is_pod>::RefHead(); }
	T& RefCurrent() const		{ return List<T, is_pod>::RefCurrent(); }
	T& RefTail() const			{ return List<T, is_pod>::RefTail(); }

	// Iterator
	const T First() const		{ return List<T, is_pod>::First(); }
	const T Next() const		{ return List<T, is_pod>::Next(); }
	const T Last() const		{ return List<T, is_pod>::Last(); }
	const T Prev() const		{ return List<T, is_pod>::Prev(); }
	T First()					{ return List<T, is_pod>::First(); }
	T Next()					{ return List<T, is_pod>::Next(); }
	T Last()					{ return List<T, is_pod>::Last(); }
	T Prev()					{ return List<T, is_pod>::Prev(); }
	const T* FirstP() const		{ return List<T, is_pod>::FirstP(); }
	const T* NextP() const		{ return List<T, is_pod>::NextP(); }
	const T* LastP() const		{ return List<T, is_pod>::LastP(); }
	const T* PrevP() const		{ return List<T, is_pod>::PrevP(); }
	T* FirstP()					{ return List<T, is_pod>::FirstP(); }
	T* NextP()					{ return List<T, is_pod>::NextP(); }
	T* LastP()					{ return List<T, is_pod>::LastP(); }
	T* PrevP()					{ return List<T, is_pod>::PrevP(); }

	// Find
	bool Find(const T& object) const;
	bool Find(const T& object, T*& duplicate) const;

	// Add/Insert
	bool Add(const T& object);
	bool AddUnique(const T& object, T*& duplicate);
	bool AddWithBackReference(const T& object);
	bool AddUniqueWithBackReference(const T& object, T*& duplicate);

	// Utility
	bool IsEmpty() const					{ return m_head == NULL; }
	void SetEstimatedSize(DWORD size)		{ m_treenode_pool.SetNumberOfObjectsPerBlock(size); List<T, is_pod>::SetEstimatedSize(size);}

	// Clean up
	void Destroy()			{ m_tree = NULL; m_treenode_pool.ReclaimAll(); List<T, is_pod>::Destroy(); }
	void DeleteAndDestroy()	{ List<T, is_pod>::DeleteAndDestroy(); Destroy(); }
	void ReleaseMemory()	{ m_treenode_pool.ReleaseMemory(); List<T, is_pod>::ReleaseMemory(); }

	// Diagnostic
	bool Verify() const						{ return List<T, is_pod>::Verify(); }

private:
	struct TreeNode
	{
		Node*		m_node;
		TreeNode*	m_left;
		TreeNode*	m_rite;
		union { TreeNode *m_parent, *m_next; };
	};
	MemPool<TreeNode, true> m_treenode_pool;

public:
	struct ListRef
	{
		ListRef() : m_mylist(NULL), m_mynode(NULL)	{}
		bool IsInList() const								{ return m_mylist != NULL && m_mynode != NULL; }
		void RemoveBackReference()							{ m_mylist = NULL; m_mynode = NULL; }
		SortedList<T, is_pod>*	m_mylist;
		TreeNode*		m_mynode;
	};
	friend void SortedListDetach(const T& object);

private:
	void Insert(TreeNode* tree_node, TreeNode* existing, SortedListSide side);
	void Remove(TreeNode* tree_node);

private:
	TreeNode*	m_tree;
	TreeNode*	m_last_thing_added;
};

//***********************************************************************//
// Implementation
//*****
// Constructor
template <typename T, bool is_pod>
SortedList<T, is_pod>::SortedList(DWORD estimated_size) :
List<T, is_pod>(estimated_size),
m_treenode_pool(estimated_size),
m_tree(NULL),
m_last_thing_added(NULL)
{}

//*****
// Copy constructor
template <typename T, bool is_pod>
SortedList<T, is_pod>::SortedList(const SortedList<T, is_pod>& copy) :
List<T, is_pod>(copy),
m_treenode_pool(copy.m_treenode_pool),
m_tree(NULL),
m_last_thing_added(NULL)
{
	PR_ASSERT_STR(copy.m_count == 0, "Don't copy sorted lists with stuff in em");
}
	
//*****
// Destructor
template <typename T, bool is_pod>
SortedList<T, is_pod>::~SortedList()
{
	PR_WARN_EXP(m_tree == NULL, "Sorted list is not empty");
	m_treenode_pool.ReleaseMemory();
}

//*****
// Look for 'object' in the list. Return true if the object is in the list
template <typename T, bool is_pod>
inline bool SortedList<T, is_pod>::Find(const T& object) const
{
	T* dont_care = NULL;
	return Find(object, dont_care);
}

//*****
// Look for 'object' in the list. If found set 'duplicate' to point to it
template <typename T, bool is_pod>
bool SortedList<T, is_pod>::Find(const T& object, T*& duplicate) const
{
	TreeNode* tree = m_tree;
	while( tree )
	{	
		PR_ASSERT(tree->m_node);

			 if( SortedListIsLess(object, tree->m_node->m_object) ) tree = tree->m_left;
		else if( SortedListIsLess(tree->m_node->m_object, object) ) tree = tree->m_rite;
		else { duplicate = &(tree->m_node->m_object); return true; }	// Found the object
	}
	duplicate = NULL;
	return false;
}

//*****
// Insert an object into the list. This method returns true if the object was inserted into the
// tree. NOTE: If the SortedListIsLess function returns true for the equal case then all
// objects should be added to the list.
template <typename T, bool is_pod>
inline bool SortedList<T, is_pod>::Add(const T& object)
{
	T* dont_care = NULL;
	return AddUnique(object, dont_care);
}

//*****
// Insert an object into the list if it is unique. This method returns true if the
// object was inserted into the tree. If the object is already in the tree then
// 'duplicate' is set to point to the duplicate object and 'object' is not added
// to the tree (i.e. false is returned).
// NOTE: Two objects are assumed equal if SortedListIsLess(a,b) and SortedListIsLess(b,a)
template <typename T, bool is_pod>
bool SortedList<T, is_pod>::AddUnique(const T& object, T*& duplicate)
{
	// Search for the insertion point in the list
	SortedListSide side = SORTED_LIST_LEFT;
	TreeNode** pptree = &m_tree;
	TreeNode*  parent = NULL;
	while( *pptree )
	{	
		TreeNode* tree = *pptree;
		PR_ASSERT(tree->m_node );
		
			 if( SortedListIsLess(object, tree->m_node->m_object) ) { pptree = &(tree->m_left); side = SORTED_LIST_LEFT; }
		else if( SortedListIsLess(tree->m_node->m_object, object) ) { pptree = &(tree->m_rite); side = SORTED_LIST_RITE; }
		else { duplicate = &(tree->m_node->m_object); return false; }	// The object is not unique
		parent = tree;
	}

	// If we get this far then an insertion point was found at *pptree;
	TreeNode*& tree	= *pptree;
	tree			= m_treenode_pool.Get();
	tree->m_left	= NULL;
	tree->m_rite	= NULL;
	tree->m_parent	= parent;
	tree->m_node	= m_node_pool.Get();
	new (&(tree->m_node->m_object)) T(object);

	Insert(tree, parent, side);
	return true;
}

//*****
// Insert an object into the list. This method returns true if the object was inserted into the
// tree. NOTE: If the SortedListIsLess function returns true for the equal case then all
// objects should be added to the list.
template <typename T, bool is_pod>
inline bool SortedList<T, is_pod>::AddWithBackReference(const T& object)
{
	T* dont_care = NULL;
	return AddUniqueWithBackReference(object, dont_care);
}

//*****
// Add an object into the list and set a back reference in the object to point back at the list
template <typename T, bool is_pod>
bool SortedList<T, is_pod>::AddUniqueWithBackReference(const T& object, T*& duplicate)
{
	if( AddUnique(object, duplicate) )
	{
		object->m_tolist.m_mylist = this;
		object->m_tolist.m_mynode = m_last_thing_added;
		return true;
	}
	else
	{
		object->m_tolist.m_mylist = NULL;
		object->m_tolist.m_mynode = NULL;
		return false;
	}
}

//*****
// Remove a back referenced object from the list
template <typename T, bool is_pod>
void SortedListDetach(const T& object)
{
	// Make sure that the origin list has been set and that 'object' is actually in the list
	PR_ASSERT(object->m_tolist.m_mylist != NULL);
	PR_ASSERT(object->m_tolist.m_mynode != NULL);
	SortedList<T, is_pod>& list = *(object->m_tolist.m_mylist);
	SortedList<T, is_pod>::TreeNode* detached = object->m_tolist.m_mynode;
	list.Remove(detached);
	object->m_tolist.m_mylist = NULL;
	object->m_tolist.m_mynode = NULL;
}	

//***********************************************************************//
// Private methods
//*****
// Insert 'tree_node' before or after 'existing'
template <typename T, bool is_pod>
void SortedList<T, is_pod>::Insert(TreeNode* tree_node, TreeNode* existing, SortedListSide side)
{
	PR_ASSERT(tree_node);

	// If the existing node is null then the list should be empty.
	if( !existing )
	{
		PR_ASSERT(m_head == NULL && m_tail == NULL);
		m_head = m_tail = tree_node->m_node;
		m_head->m_next = m_head->m_prev = NULL;
		m_count = 1;
	}
	else
	{
		Node* node = tree_node->m_node;
		Node* exis = existing->m_node;

		// Insert before exis
		if( side == SORTED_LIST_LEFT )
		{
			node->m_next = exis;
			node->m_prev = exis->m_prev;
			if( node->m_next ) node->m_next->m_prev = node;
			if( node->m_prev ) node->m_prev->m_next = node;
			if( m_head == exis ) m_head = node;
		}
		// Otherwise insert after parent->m_node
		else // side == SORTED_LIST_RITE
		{
			node->m_next	= exis->m_next;
			node->m_prev	= exis;
			if( node->m_next ) node->m_next->m_prev = node;
			if( node->m_prev ) node->m_prev->m_next = node;
			if( m_tail == exis ) m_tail = node;
		}

		// There's no way of knowing where 'm_current' is in relation to 'exis'
		// so set it to the start. For this reason we can't allow bookmarks either.
		m_current = m_head;
		m_current_index = 0;
		#ifdef LIST_USE_BOOKMARKS
		PR_ASSERT(m_bookmarks == NULL);
		#endif//LIST_USE_BOOKMARKS
		++m_count;
	}
	m_last_thing_added = tree_node;
}

//*****
// Remove 'node' from the tree
template <typename T, bool is_pod>
void SortedList<T, is_pod>::Remove(TreeNode* tree_node)
{
	if( tree_node == m_last_thing_added ) m_last_thing_added = NULL;

	// Get a pointer to the branch of the parent that points to us
	SortedList<T, is_pod>::TreeNode** branch = &m_tree;
	if( tree_node->m_parent != NULL )
	{
		if( tree_node->m_parent->m_left == tree_node )				branch = &(tree_node->m_parent->m_left);
		else { PR_ASSERT(tree_node->m_parent->m_rite == tree_node);	branch = &(tree_node->m_parent->m_rite); }
	}

	// If 'tree_node' only has one branch we can just unlink it from the tree
	if( tree_node->m_left == NULL || tree_node->m_rite == NULL )
	{
			 if( tree_node->m_left != NULL ) *branch = tree_node->m_left;
		else if( tree_node->m_rite != NULL ) *branch = tree_node->m_rite;
		else								 *branch = NULL;
		if( *branch )			 (*branch)->m_parent = tree_node->m_parent;
	}

	// Otherwise we need to replace it with the rightmost on the lefthand side or
	// the leftmost on the righthand side, whichever we find first
	else
	{
		SortedList<T, is_pod>::TreeNode* leftside   = tree_node->m_left;
		SortedList<T, is_pod>::TreeNode* riteside   = tree_node->m_rite;
		SortedList<T, is_pod>::TreeNode* leftparent = tree_node->m_parent;
		SortedList<T, is_pod>::TreeNode* riteparent = tree_node->m_parent;
		while( leftside && riteside )
		{
			if( !leftside->m_rite )
			{
				// Unlink the found object from the tree
				if( leftparent->m_left == leftside )				leftparent->m_left = leftside->m_left;
				else { PR_ASSERT(leftparent->m_rite == leftside);	leftparent->m_rite = leftside->m_left; }
			
				// Link it inplace of 'tree_node'
				leftside->m_parent	= tree_node->m_parent;
				leftside->m_left	= tree_node->m_left;
				leftside->m_rite	= tree_node->m_rite;
				if( leftside->m_left ) leftside->m_left->m_parent = leftside;
				if( leftside->m_rite ) leftside->m_rite->m_parent = leftside;
				break;
			}
			if( !riteside->m_left )
			{
				// Unlink the found object from the tree
				if( riteparent->m_left == riteside )				riteparent->m_left = riteside->m_rite;
				else { PR_ASSERT(riteparent->m_rite == riteside);	riteparent->m_rite = riteside->m_rite; }

				// Link it inplace of 'tree_node'
				riteside->m_parent	= tree_node->m_parent;
				riteside->m_left	= tree_node->m_left;
				riteside->m_rite	= tree_node->m_rite;
				if( riteside->m_left ) riteside->m_left->m_parent = riteside;
				if( riteside->m_rite ) riteside->m_rite->m_parent = riteside;
				break;
			}
			leftparent = leftside;
			riteparent = riteside;
			leftside = leftside->m_rite;
			riteside = riteside->m_left;
		}
	}

	--m_count;
	tree_node->m_next = NULL;
	if( m_destruct_objects ) m_treenode_pool.Return(tree_node);
	else					 m_treenode_pool.Return(tree_node, false);
}

}//namespace PR

#endif//SORTEDLIST_H