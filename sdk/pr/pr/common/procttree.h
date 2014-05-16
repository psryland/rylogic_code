//***********************************************************************
// Oct Tree
//  Copyright (c) Rylogic Ltd 2004
//***********************************************************************

#ifndef PR_OCTTREE_H
#define PR_OCTTREE_H

#include "pr/common/assert.h"
#include "pr/common/PRTypes.h"
#include "pr/common/MemPool.h"

namespace pr
{
	template <typename T>
	class OctTree
	{
	private:	// Structures
		struct Node
		{
			struct pr_is_pod { enum { value = true }; };

			T*			m_object;				// Pointer to the object at this node in the oct tree
			uint		m_level;				// The depth down the tree
			uint		m_indexX;				// X index of the cell in the oct tree at m_level
			uint		m_indexY;				// Y index of the cell in the oct tree at m_level
			uint		m_indexZ;				// Z index of the cell in the oct tree at m_level
			Node*		m_oct[8];				// The octants below this Node
			union { Node *m_parent, *m_next; };	// 'm_next' is required by 'MemPool'
		};
		MemPool<Node> m_node_pool;				// A pool of nodes

	public:	// Methods
		enum { MAX_LEVEL = 32 };
		OctTree(float dimension, uint estimated_number_of_nodes = 100);
		~OctTree();

		uint	GetCount() const						{ return m_count; }
		uint	MaxIndexAtLevel(uint level) const		{ return (1 << level); }
		float	CellSizeAtLevel(uint level) const		{ return m_dim / static_cast<float>(MaxIndexAtLevel(level)); }

		void	Add(const T& object, const v4& position, float radius);
		void	DeleteAndDestroy();
		void	Destroy();
		void	ReleaseMemory();

	private: // Methods
		void	GetCell(const v4& position, float radius, uint& level, uint& X, uint& Y, uint& Z) const;
		void	GetIndex(uint level, float posX, float posZ, uint& X, uint& Z) const;
		Node*	GetOrCreateNode(uint level, uint X, uint Y, uint Z);
		void	DeleteAndDestroyRecersive(Node* tree);

	private: // Members
		Node*	m_tree;							// The oct tree
		uint	m_count;						// The number of nodes in the tree
		float	m_dim;							// The dimension of the space covered by this oct tree (must be square)
	};

	//***************************************************************************************************************
	// Public OctTree methods
	//*****
	// Constructor
	template <typename T>
	inline OctTree<T>::OctTree(float dimension, uint estimated_number_of_nodes)
	:m_node_pool(estimated_number_of_nodes)
	,m_tree(NULL)
	,m_count(0)
	,m_dim(dimension)
	{}

	//*****
	// Destructor
	template <typename T>
	inline OctTree<T>::~OctTree()
	{
		Destroy();
		ReleaseMemory();
	}

	//*****
	// Insert 'object' into the tree at the correct level so that 'diametre'
	// is less than the cell size at that level
	template <typename T>
	inline void OctTree<T>::Add(const T& object, const v4& position, float radius)
	{
		uint level,X,Y,Z;
		GetCell(position, radius, level, X, Y, Z);

		Node* node = GetOrCreateNode(level, X, Y, Z);
		node->m_object = &object;
	}

	//*****
	// Delete the objects pointed to in each node and destroy the oct tree.
	template <typename T>
	inline void OctTree<T>::DeleteAndDestroy()
	{
		DeleteAndDestroyRecersive(m_tree);
		Destroy();
	}

	//*****
	// Delete the objects pointed to in each node and destroy the oct tree.
	template <typename T>
	void OctTree<T>::DeleteAndDestroyRecersive(Node* tree)
	{
		if( !tree ) return;
		for( uint i = 0; i < 8; ++i )
			DeleteAndDestroyRecersive(tree->m_oct[i]);

		delete tree->m_object;
	}

	//*****
	// Delete objects and nodes in the oct tree.
	template <typename T>
	inline void OctTree<T>::Destroy()
	{
		m_node_pool.ReclaimAll();
		m_tree	= NULL;
		m_count	= 0;
	}

	//*****
	// Release memory in the node_pool
	template <typename T>
	inline void OctTree<T>::ReleaseMemory()
	{
		PR_ASSERT(PR_DBG, m_tree == NULL && m_count == 0, "Call Destroy first");
		m_node_pool.ReleaseMemory();
	}

	//***************************************************************************************************************
	// Private OctTree methods
	//*****
	// Returns the level, X, Y, and Z address of the cell in the oct tree
	// that an object with a bounding radius of 'radius' should be inserted.
	template <typename T>
	void OctTree<T>::GetCell(const v4& position, float radius, uint& level, uint& X, uint& Y, uint& Z) const
	{
		float diameter = 2.0f * radius;
		PR_ASSERT(PR_DBG, diameter < m_dim, "");
		for( level = 1; level < MAX_LEVEL; ++level )
		{
			if( diameter > CellSizeAtLevel(level) )
			{
				--level;
				break;
			}
		}

		float cell_size = CellSizeAtLevel(level);
		X = static_cast<uint>(position[0] / cell_size);
		Y = static_cast<uint>(position[1] / cell_size);
		Z = static_cast<uint>(position[2] / cell_size);

		// If these fire then 'position' is outside the boundary of this oct tree
		PR_ASSERT(PR_DBG, X < MaxIndexAtLevel(level), "");
		PR_ASSERT(PR_DBG, Y < MaxIndexAtLevel(level), "");
		PR_ASSERT(PR_DBG, Z < MaxIndexAtLevel(level), "");
	}

	//*****
	// Navigate the oct tree adding nodes if necessary.
	// Return the node at 'level', index position (x,y,z)
	template <typename T>
	typename OctTree<T>::Node* OctTree<T>::GetOrCreateNode(uint level, uint X, uint Y, uint Z)
	{
		// Make sure X, Y, Z are valid at 'level'
		PR_ASSERT(PR_DBG, X < MaxXIndexAtLevel(level), "");
		PR_ASSERT(PR_DBG, Y < MaxXIndexAtLevel(level), "");
		PR_ASSERT(PR_DBG, Z < MaxZIndexAtLevel(level), "");

		// If the oct tree does not yet exist then create the first node
		if( m_tree == NULL )
		{
			m_tree = m_node_pool.Get();
			m_tree->m_object = NULL;
			++m_count;
			PR_ASSERT(PR_DBG, m_count == 1, "");
		}

		// Navigate down the oct tree adding nodes if necessary until we reach 'level'
		uint twoX	= X * 2;
		uint twoY	= Y * 2;
		uint twoZ	= Z * 2;
		uint maxL	= MaxIndexAtLevel(level);
		uint L		= maxL;
		Node* tree	= m_tree;
		for( uint lvl = 0; lvl < level; ++lvl )
		{
			int oct;
			if( twoZ < L )
			{
				if( twoY < L )
				{
					if( twoX < L )		oct = 0;
					else { twoX -= L;	oct = 1; }
				}
				else
				{
					twoY -= L;
					if( twoX < L )		oct = 2;
					else { twoX -= L;	oct = 3; }
				}
			}
			else // twoZ >= L
			{
				twoZ -= L;
				if( twoY < L )
				{
					if( twoX < L )		oct = 4;
					else { twoX -= L;	oct = 5; }
				}
				else
				{
					twoY -= L;
					if( twoX < L )		oct = 6;
					else { twoX -= L;	oct = 7; }
				}
			}
			L /= 2;

			// If there is no node in this oct then add one
			if( tree->m_oct[oct] == NULL )
			{
				tree->m_oct[oct]	= m_node_pool.Get();
				Node* child			= tree->m_oct[oct];
				child->m_object		= NULL;
				child->m_level		= lvl + 1;
				child->m_indexX		= X >> (level - child->m_level);
				child->m_indexX		= X >> (level - child->m_level);
				child->m_indexZ		= Z >> (level - child->m_level);
				child->m_parent		= tree;
				++m_count;
			}
			tree = tree->m_oct[oct];
		}

		return tree;
	}
}//namespace pr

#endif//PR_OCTTREE_H
