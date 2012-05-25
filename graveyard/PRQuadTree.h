//***********************************************************************//
//                     A Template quad tree class                        //
//                                                                       //
//                  (c)opyright June 2003 Paul Ryland                    //
//                                                                       //
//***********************************************************************//
//
//	Optional defines
//
//	Methods and members that can be required for this template class:
//		void T::Print();
//
#ifndef PR_QUAD_TREE_H
#define PR_QUAD_TREE_H

#include "PR/Common/MemPool.h"
#include "PR/Common/StdList.h"

const int QUAD_TREE_MAX_LEVEL = 32;

//***************************************************************************************************************
// QuadTree class
template <typename T>
class QuadTree
{
private:	// Structures
	struct Node
	{
		Node() : m_level(0), m_indexX(0), m_indexZ(0), m_parent(0) { memset(m_quad, 0, 4 * sizeof(Node)); }
		List<T>		m_object;				// The list of objects in this node
		int			m_level;
		int			m_indexX;
		int			m_indexZ;
		Node*		m_quad[4];
		union { Node *m_parent, *m_next; };	// 'm_next' is required by 'MemPool'
	};
	MemPool<Node> m_node_pool;				// A pool of nodes

public:	// Methods
	QuadTree(float dimension, DWORD estimated_size = 100);
	~QuadTree();

	DWORD	GetCount() const						{ return m_count; }
	DWORD	MaxIndexAtLevel(DWORD level) const		{ return (1 << level); }
	float	CellSizeAtLevel(DWORD level) const		{ return m_dim / float(MaxIndexAtLevel(level)); }
	void	SetDimension(float dimension)			{ assert(m_tree == NULL); m_dim = dimension; }		// You can only do this if the tree is empty
	
	void	Insert(T* object, float posX, float posZ, float diametre);
	void	Desolve(Node* tree = NULL);		// If tree is null then the whole tree is desolved
	void	Destroy(Node* tree = NULL);		// If tree is null then the whole tree is destroyed

private: // Methods
	DWORD	GetLevel(float size) const;
	void	GetIndex(DWORD level, float posX, float posZ, DWORD& indexX, DWORD& indexZ) const;
	Node*	GetOrCreateNode(DWORD level, DWORD indexX, DWORD indexZ);

private: // Members
	Node*	m_tree;							// The quad tree
	DWORD	m_node_count;					// The number of nodes in the tree
	DWORD	m_object_count;					// The number of objects in the nodes of the tree
	float	m_dim;							// The dimension of the space covered by this quad tree (must be square)
	bool	m_should_not_be_destroyed;		// False if this tree owns the objects
};

//***************************************************************************************************************
// Public QuadTree methods
//*****
// Constructor
template <typename T>
inline QuadTree<T>::QuadTree(float dimension, DWORD estimated_size) :
m_node_pool(estimated_size),
m_tree(NULL),
m_node_count(0),
m_object_count(0),
m_dim(dimension),
m_should_not_be_destroyed(false)
{}

//*****
// Destructor
template <typename T>
inline QuadTree<T>::~QuadTree()
{
	assert(m_tree == NULL);
	// The quad tree does not necessarily own the objects it points to.
	// Therefore it cannot delete them during destruction
	// If this assert fires then check for the following:
	// 1) You've forgotten to call Destroy or Desolve
	//    before the tree is being deleted
	// 2) You've made a local instance of a tree that is going
	//    out of scope
	m_node_pool.ReleaseMemory();
}

//*****
// Insert 'object' into the tree at the correct level so that 'diametre'
// is less than the cell size at that level
template <typename T>
inline void QuadTree<T>::Insert(T* object, float posX, float posZ, float diametre)
{
	DWORD level = GetLevel(diametre);
	DWORD indexX, indexZ;
	GetIndex(level, posX, posZ, indexX, indexZ);

	Node* node = GetOrCreateNode(level, indexX, indexZ);
	node->m_object.AddToTail(object);
	++m_object_count;
}

//*****
//*****
// Delete objects and nodes in the quad tree. If 'tree' is NULL then the
// whole quad tree is destroyed. Otherwise only from 'tree' down
template <typename T>
inline void	QuadTree<T>::Desolve(Node* tree)
{
	if( !tree ) tree = m_tree;

	if( tree->m_quad[0] ) Desolve(tree->m_quad[0]);
	if( tree->m_quad[1] ) Desolve(tree->m_quad[1]);
	if( tree->m_quad[2] ) Desolve(tree->m_quad[2]);
	if( tree->m_quad[3] ) Desolve(tree->m_quad[3]);
	
	// Desolve the list of objects at this level
	m_object_count -= tree->m_object.GetCount();
	tree->m_object.Desolve();

	// Destroy the node at this level
	--m_node_count;
	m_node_pool.Return(tree);
}

//*****
// Delete objects and nodes in the quad tree. If 'tree' is NULL then the
// whole quad tree is destroyed. Otherwise only from 'tree' down
template <typename T>
inline void QuadTree<T>::Destroy(Node* tree)
{
	if( !tree ) tree = m_tree;

	if( tree->m_quad[0] ) Destroy(tree->m_quad[0]);
	if( tree->m_quad[1] ) Destroy(tree->m_quad[1]);
	if( tree->m_quad[2] ) Destroy(tree->m_quad[2]);
	if( tree->m_quad[3] ) Destroy(tree->m_quad[3]);
	
	// Destroy the list of objects at this level
	m_object_count -= tree->m_object.GetCount();
	tree->m_object.Destroy();

	// Destroy the node at this level
	--m_node_count;
	m_node_pool.Return(tree);
}

//***************************************************************************************************************
// Private QuadTree methods
//*****
// Returns the level in the quad tree that an object with a maximum size of 'diametre' should be inserted.
template <typename T>
inline DWORD QuadTree<T>::GetLevel(float diametre) const
{
	assert(diametre < m_dim);
	for( DWORD i = 1; i < QUAD_TREE_MAX_LEVEL; ++i )
	{
		if( diametre > CellSizeAtLevel(i) )
			return i - 1;
	}
	return 0;
}

//*****
// Returns 'indexX' and 'indexZ' for an object in 'level' at 'posX, posZ'  
template <typename T>
inline void QuadTree<T>::GetIndex(DWORD level, float posX, float posZ, DWORD& indexX, DWORD& indexZ) const
{
	float cell_size = CellSizeAtLevel(level);
	indexX = DWORD(posX / cell_size);
	indexZ = DWORD(posZ / cell_size);

	// If these fire then 'posX' or 'posZ' are outside the boundary of this quad tree
	assert(indexX >= 0 && indexX < MaxIndexAtLevel(level));
	assert(indexZ >= 0 && indexZ < MaxIndexAtLevel(level));
}

//*****
// Navigate the quad tree adding nodes if necessary.
// Return the node at 'level', index position (x,z)
template <typename T>
inline QuadTree<T>::Node* QuadTree<T>::GetOrCreateNode(DWORD level, DWORD indexX, DWORD indexZ)
{
	// Make sure indexX, indexZ are valid at 'level'
	assert(x >= 0 && x < MaxXIndexAtLevel(level));
	assert(z >= 0 && z < MaxZIndexAtLevel(level));

	// If the quad tree does not yet exist then create the first node
	if( m_tree == NULL )
	{
		m_tree = m_node_pool.Get();
		++m_node_count;
		assert(m_node_count == 1);
	}

	// Navigate down the quad tree adding nodes if necessary until we reach 'level'
	DWORD twoX	= indexX * 2;
	DWORD twoZ	= indexZ * 2;
	DWORD maxL	= MaxIndexAtLevel(level);
	DWORD L		= maxL;
	Node* tree	= m_tree;
	for( DWORD lvl = 0; lvl < level; ++lvl )
	{
		int quad;
		if( twoZ < L )
		{
			if( twoX < L )		quad = 0;
			else { twoX -= L;	quad = 1; }
		}
		else // twoZ >= L
		{
			twoZ -= L;
			if( twoX < L )		quad = 2;
			else { twoX -= L;	quad = 3; }
		}
		L /= 2;

		// If there is no node in this quad then add one
		if( tree->m_quad[quad] == NULL )
		{
			tree->m_quad[quad]	= m_node_pool.Get();
			Node* child			= tree->m_quad[quad];
			child->m_level		= lvl + 1;
			child->m_indexX		= indexX >> (level - child->m_level);
			child->m_indexZ		= indexZ >> (level - child->m_level);
			child->m_parent		= tree;
			++m_node_count;
		}
		tree = tree->m_quad[quad];
	}

	return tree;
}

#endif//PR_QUAD_TREE_H
