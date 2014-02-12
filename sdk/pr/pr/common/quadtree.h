//***********************************************************************
//
//	Quad Tree
//
//***********************************************************************

#ifndef PR_QUAD_TREE_H
#define PR_QUAD_TREE_H

#include <vector>
#include <list>
#include "pr/common/assert.h"
#include "pr/common/mempool.h"
#include "pr/maths/maths.h"

namespace pr
{
	namespace quad_tree
	{
		// A node in the quad tree
		template <typename Type>
		struct Node
		{
			Node() :m_level(0) ,m_indexX(0) ,m_indexZ(0) ,m_parent(0) { memset(m_quad, 0, sizeof(m_quad)); }

			typedef typename std::vector<Type> TInstance;
			TInstance		m_instance;
			unsigned int	m_level;
			unsigned int	m_indexX;
			unsigned int	m_indexZ;
			union {
			Node<Type>*		m_parent;
			Node<Type>*		m_next;		// Use by MemPool
			};
			Node<Type>*		m_quad[4];
		};

		// A quad tree
		template <typename Type>
		class Tree
		{
		public:
			typedef typename Node<Type> Node;

			Tree(v4 const& centre, float region_width, float region_depth, unsigned int max_levels, unsigned int size_estimate = 1);
			~Tree();

			// Return access to the tree structure
			const Node*		GetTree() const									{ return m_tree; }
			v4				GetCentre() const								{ return m_origin + GetDimensions() / 2.0f; }
			v4				GetDimensions() const							{ return v4::make(m_region_width, 0.0f, m_region_depth, 0.0f); }
			unsigned int	GetNumInstance() const							{ return m_number_of_instances; }
			unsigned int	MaxIndexAtLevel(unsigned int level) const		{ PR_ASSERT(PR_DBG, level < m_max_levels, ""); return 1 << level; }
			float			CellWidthAtLevel(unsigned int level) const		{ return m_region_width / static_cast<float>(MaxIndexAtLevel(level)); }
			float			CellDepthAtLevel(unsigned int level) const		{ return m_region_depth / static_cast<float>(MaxIndexAtLevel(level)); }
			bool			GetLevelAndIndices(v4 const& point, float radius, unsigned int& level, unsigned int& indexX, unsigned int& indexZ) const;
			unsigned int	GetLevel(float radius) const;

			// Add an instance to the quad tree
			bool			Add(Type instance, v4 const& point, float radius);

			// Helper methods for searching the quad tree
			v4				GetRelative(const v4& point) const { return point - m_origin; }
			bool			PointInQuad(const Node& node, v4 const& point) const;

		private:
			Tree(const Tree&);//no copying
			Tree& operator=(const Tree&);
			Node*			NewNode() { return m_nodes.Get(); }
			Node*			GetOrCreateNode(unsigned int level, unsigned int indexX, unsigned int indexZ);

		private:
			MemPool<Node>	m_nodes;				// Storage for the nodes
			Node*			m_tree;					// The top node of the tree
			v4				m_origin;				// The min X,Z corner of the quad tree
			float			m_region_width;			// The X dimension of the area covered by the tree
			float			m_region_depth;			// The Z dimension of the area covered by the tree
			unsigned int	m_max_levels;			// The maximum depth the tree will grow to
			unsigned int	m_number_of_instances;	// The number of instances added to the tree
		};

		// Implementation ************************************************

		//*****
		template <typename Type>
		Tree<Type>::Tree(v4 const& centre, float region_width, float region_depth, unsigned int max_levels, unsigned int size_estimate)
		:m_nodes(size_estimate)
		,m_tree(NewNode())
		,m_origin(v4::make(centre.x - region_width/2.0f, 0.0f, centre.z - region_depth/2.0f, 1.0f))
		,m_region_width(region_width)
		,m_region_depth(region_depth)
		,m_max_levels(max_levels)
		,m_number_of_instances(0)
		{}

		//*****
		template <typename Type>
		Tree<Type>::~Tree()
		{
			m_nodes.ReclaimAll();
		}

		//*****
		// Add an 'instance' to the quad tree. Returns true if the instance was added
		template <typename Type>
		bool Tree<Type>::Add(Type instance, v4 const& point, float radius)
		{
            // Find where 'instance' should go
			unsigned int level, indexX, indexZ;
			if( !GetLevelAndIndices(point, radius, level, indexX, indexZ) ) return false;

			// Get a node at that position
			Node* node = GetOrCreateNode(level, indexX, indexZ);

			// Stick 'instance' in the node
			node->m_instance.push_back(instance);
			++m_number_of_instances;
			return true;
		}

		//*****
		// Returns true if 'node' can contain something could reach 'point'. Note 'point'
		// should already be relative to the quad tree centre
		template <typename Type>
		inline bool Tree<Type>::PointInQuad(const typename Tree<Type>::Node& node, v4 const& point) const
		{
			float quad_width = CellWidthAtLevel(node.m_level);
			float quad_depth = CellDepthAtLevel(node.m_level);
			float cell_xmin = float( node.m_indexX    - 0.5f) * quad_width + m_origin[0];
			float cell_zmin = float( node.m_indexZ    - 0.5f) * quad_depth + m_origin[2];
			float cell_xmax = float((node.m_indexX+1) + 0.5f) * quad_width + m_origin[0];
			float cell_zmax = float((node.m_indexZ+1) + 0.5f) * quad_depth + m_origin[2];
			return !(point[0] < cell_xmin || point[2] < cell_zmin || point[0] > cell_xmax || point[2] > cell_zmax);
		}

		//*****
		// Returns 'indexX', 'indexZ', and 'level' for an object with bounding radius 'radius' at 'point'
		template <typename Type>
		bool Tree<Type>::GetLevelAndIndices(v4 const& point, float radius, unsigned int& level, unsigned int& indexX, unsigned int& indexZ) const
		{
			// Convert 'point' to be relative to the origin
			point = GetRelative(point);

			// Check the position within the area covered by this tree. We don't want models to overhang
			// the area by more than half the area width
			if( point[0] - radius < -m_region_width * 0.5f || point[0] + radius > m_region_width * 1.5f ||
				point[2] - radius < -m_region_depth * 0.5f || point[2] + radius > m_region_depth * 1.5f )
			{ return false; }

			level			= GetLevel(radius);
			float			cell_width	= CellWidthAtLevel(level);
			float			cell_depth	= CellDepthAtLevel(level);
			unsigned int	max_index	= MaxIndexAtLevel(level);
			indexX			= static_cast<unsigned int>(point[0] / cell_width);
			indexZ			= static_cast<unsigned int>(point[2] / cell_depth);

			// If 'point' is outside of the region then we need to keep going up
			// levels until 'point' + 'radius' is within half the cell width of the closest cell.
			if( point[0] < 0.0f || point[0] >= m_region_width || point[2] < 0.0f || point[2] >= m_region_depth )
			{
				float xdist = 0.0f, zdist = 0.0f;
				if( point[0] < 0.0f )				{ xdist = -point[0] + radius;					indexX = 0;				}
				if( point[0] >= m_region_width )	{ xdist =  point[0] - m_region_width + radius;	indexX = max_index - 1;	}
				if( point[2] < 0.0f )				{ zdist = -point[2] + radius;					indexZ = 0;				}
				if( point[2] >= m_region_depth )	{ zdist =  point[2] - m_region_depth + radius;	indexZ = max_index - 1;	}

				while( level >= 0 && CellWidthAtLevel(level) / 2.0f < xdist )
				{
					--level;
					indexX /= 2;
					indexZ /= 2;
				}

				while( level >= 0 && CellDepthAtLevel(level) / 2.0f < zdist )
				{
					--level;
					indexX /= 2;
					indexZ /= 2;
				}

				// This means that instance is too big to fit in the quad tree
				if( level < 0 ) { return false; }
			}
			return true;
		}

		//*****
		// Returns the level in the quad tree that an object with a maximum size of 'radius' should be inserted.
		template <typename Type>
		inline unsigned int Tree<Type>::GetLevel(float radius) const
		{
			unsigned int i;
			for( i = 1; i < m_max_levels; ++i )
			{
				if( radius > CellWidthAtLevel(i) / 2.0f || radius > CellDepthAtLevel(i) / 2.0f ) return i - 1;
			}
			return i - 1;
		}

		//*****
		// Navigate the quad tree adding nodes if necessary.
		// Return the node at 'level', 'indexX', 'indexZ'
		template <typename Type>
		typename Tree<Type>::Node* Tree<Type>::GetOrCreateNode(unsigned int level, unsigned int indexX, unsigned int indexZ)
		{
			// Make sure indexX, indexZ are valid at 'level'
			PR_ASSERT(PR_DBG, indexX >= 0 && indexX < MaxIndexAtLevel(level), "");
			PR_ASSERT(PR_DBG, indexZ >= 0 && indexZ < MaxIndexAtLevel(level), "");

			// Navigate down the quad tree adding nodes if necessary until we reach 'level'
			unsigned int twoX	= indexX * 2;
			unsigned int twoZ	= indexZ * 2;
			unsigned int L		= MaxIndexAtLevel(level);
			Node*		 node	= m_tree;
			for( unsigned int lvl = 0; lvl < level; ++lvl )
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
				if( node->m_quad[quad] == 0 )
				{
					node->m_quad[quad]	= NewNode();
					Node* child			= node->m_quad[quad];
					child->m_level		= lvl + 1;
					child->m_indexX		= indexX >> (level - child->m_level);
					child->m_indexZ		= indexZ >> (level - child->m_level);
					child->m_parent		= node;
				}
				node = node->m_quad[quad];
			}
			return node;
		}
	}//namespace quad_tree
}//namespace pr

#endif//PR_QUAD_TREE_H
