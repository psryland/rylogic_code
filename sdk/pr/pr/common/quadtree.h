//*****************************************
// Quad Tree
//  Copyright © Rylogic Ltd 2014
//*****************************************
// Loose quad tree.
// Items in the nodes of the tree can over hang up to half
// the size of the smallest dimension of the node.

#pragma once

#include <deque>
#include <cassert>

namespace pr
{
	namespace quad_tree
	{
		// Quad tree node
		template <typename TItem> struct Node
		{
			typedef std::deque<TItem> ItemCont;

			ItemCont     m_items;    // The items contained in this node
			size_t       m_level;    // The level in the quad tree that this node is in
			size_t       m_x;        // The coordinates of this node within the level
			size_t       m_y;        // The coordinates of this node within the level
			Node<TItem>* m_parent;   // Pointer to the parent node
			Node<TItem>* m_child[4]; // Pointers to the child nodes

			Node(size_t level, size_t x, size_t y, Node<TItem>* parent)
				:m_items()
				,m_level(level)
				,m_x(x)
				,m_y(y)
				,m_parent(parent)
				,m_child()
			{}
		};
	}

	// Loose quad tree
	template <typename TItem> struct QuadTree
	{
		typedef quad_tree::Node<TItem> Node;

		std::deque<Node>  m_nodes;         // Storage for the nodes. Note, references are not invalidated by push/pop at the front/back of a std::deque
		Node*             m_root;          // The top node of the tree
		float             m_minx,m_miny;   // The min x,y corner of the region covered by the quad tree
		float             m_sizex,m_sizey; // The x,y size of the region covered by the quad tree
		size_t            m_max_levels;    // The maximum depth the tree will grow to
		size_t            m_count;         // The number of items added to the tree

		QuadTree(float minx, float miny, float sizex, float sizey, size_t max_levels = 32)
			:m_nodes()
			,m_root(NewNode(0,0,0,nullptr))
			,m_minx(minx)
			,m_miny(miny)
			,m_sizex(sizex)
			,m_sizey(sizey)
			,m_max_levels(max_levels < 32 ? max_levels : 32)
		{}

		// Returns the maximum index value for a given level.
		// e.g level 0 = [0,1), level 1 = [0,2), level 4 = [0, 8), etc
		static size_t MaxIndex(size_t level)
		{
			return 1U << level;
		}

		// Returns the optimal level in the quad tree for an object bounded by 'radius'.
		size_t GetLevel(float radius) const
		{
			for (size_t i = 1; i != m_max_levels; ++i)
			{
				// The optimal level is when the object is right in the centre of the
				// cell and it's radius is just less than the smallest dimension of the cell.
				if (radius > CellSizeX(i) || radius > CellSizeY(i))
					return i - 1;
			}
			return m_max_levels - 1;
		}

		// Returns the dimensions of a cell at the given level
		float CellSizeX(size_t level) const
		{
			return m_sizex / MaxIndex(level);
		}
		float CellSizeY(size_t level) const
		{
			return m_sizey / MaxIndex(level);
		}

		// Returns the level and x,y coordinates of the cell that an object bounded by 'point' and 'radius' would be added to
		void GetLevelAndIndices(float const (&point)[2], float radius, size_t& level, size_t& x, size_t& y) const
		{
			// Find 'point' relative to the min x,y of the region
			float pt[2] = {point[0] - m_minx, point[1] - m_miny};

			// Get the node location for 'point'+'radius'
			level = GetLevel(radius);
			x     = static_cast<size_t>(pt[0] / CellSizeX(level));
			y     = static_cast<size_t>(pt[1] / CellSizeY(level));

			// If 'point' is outside of the region then we need to keep going up levels
			// until 'point' + 'radius' is within half the cell width of the closest cell.
			// This is a special case for when 'point' is outside the region but not by more
			// than the half the smallest region dimension.
			if (pt[0] < 0.0f || pt[0] >= m_sizex || pt[1] < 0.0f || pt[1] >= m_sizey)
			{
				float xdist = 0.0f, ydist = 0.0f;
				if (pt[0] < 0.0f)     { xdist = -pt[0] + radius;           x = 0;                   }
				if (pt[0] >= m_sizex) { xdist =  pt[0] - m_sizex + radius; x = MaxIndex(level) - 1; }
				if (pt[1] < 0.0f)     { ydist = -pt[1] + radius;           y = 0;                   }
				if (pt[1] >= m_sizey) { ydist =  pt[1] - m_sizey + radius; y = MaxIndex(level) - 1; }

				while (level > 0 && CellSizeX(level) / 2.0f < xdist)
				{
					--level;
					x /= 2;
					y /= 2;
				}
				while (level > 0 && CellSizeY(level) / 2.0f < ydist)
				{
					--level;
					x /= 2;
					y /= 2;
				}

				// This means that 'point'+'radius' is too big to fit in the quad tree
				// Insert it at the top level
				if (level <= 0)
				{
					level = 0;
					x = 0;
					y = 0;
				}
			}
		}

		// Insert an item into the quad tree
		// Inserting an item that is too big for the quad will result in it being added to the root node
		// Returns the node that contains 'item'
		Node const* Insert(TItem const& item, float const (&point)[2], float radius)
		{
			// Find where 'item' should go
			size_t level, x, y;
			GetLevelAndIndices(point, radius, level, x, y);

			// Get a node at that position
			Node* node = GetOrCreateNode(level, x, y);

			// Add 'item' to the collection in this node
			node->m_items.push_back(item);
			++m_count;
			return node;
		}

		// Traverse the quad tree, adding nodes if necessary, returning the node at (level,x,y)
		Node* GetOrCreateNode(size_t level, size_t x, size_t y)
		{
			// Make sure x,y is valid at 'level'
			assert(x >= 0 && x < MaxIndex(level) && "invalid node x coordinate");
			assert(y >= 0 && y < MaxIndex(level) && "invalid node y coordinate");

			// Navigate down the quad tree adding nodes if necessary until we reach 'level'
			size_t two_x = x * 2;
			size_t two_y = y * 2;
			size_t L = MaxIndex(level);
			Node* node = m_root;
			for (size_t lvl = 0; lvl != level; ++lvl)
			{
				// Find the quad containing (level,x,y)
				int quad;
				if (two_y < L)
				{
					if (two_x < L) quad = 0;
					else { two_x -= L; quad = 1; }
				}
				else // two_y >= L
				{
					two_y -= L;
					if (two_x < L) quad = 2;
					else { two_x -= L; quad = 3; }
				}
				L /= 2;

				// If there is no node in this quadrant, add one
				if (node->m_child[quad] == nullptr)
				{
					size_t child_level = lvl + 1;
					node->m_child[quad] = NewNode(child_level, x >> (level - child_level), y >> (level - child_level), node);
				}

				// Decend down the tree
				node = node->m_child[quad];
			}
			return node;
		}

		// Traverse the quad tree passing each item that possibly intersects 'point','radius' to 'pred'
		// 'pred' should return false to end the traversal, or true to continue.
		// Returns 'true' if a full search occurred, false if 'pred' returned false ending the search early.
		template <typename Pred> bool Traverse(float const (&point)[2], float radius, Pred pred, Node* root = nullptr)
		{
			if (!root)
				root = m_root;

			// Pass the items at this level to pred
			for (auto& item : root->m_items)
			{
				if (!pred(item))
					return false;
			}

			// Jump to each child node that might contain items that overlap with 'point','radius'
			for (auto child : root->m_child)
			{
				if (!Overlaps(*child, point)) continue;
				if (!Traverse(point, radius, pred, child))
					return false;
			}

			return true;
		}

		// Returns true if 'node' can contain an item could overlay 'point'+'radius'.
		bool Overlaps(Node const& node, float const (&point)[2], float radius) const
		{
			float pt[2] = {point[0] - m_minx, point[1] - m_miny};
			float min[2], max[2]; NodeBounds(node, true, min, max);
			return !(pt[0] + radius < min[0] || pt[0] - radius > max[0] ||
					 pt[1] + radius < min[1] || pt[1] - radius > max[1]);
		}

		// Return the bounds of 'node', optionally including the region that items in the node might overlap
		void NodeBounds(Node const& node, bool overlap_region, float (&min)[2], float (&max)[2]) const
		{
			float ovr = overlap_region ? 0.5f : 0.0f;
			float cell_sx = CellSizeX(node.m_level);
			float cell_sy = CellSizeY(node.m_level);
			min[0] = (node.m_x - (0.0f + ovr)) * cell_sx + m_minx;
			min[1] = (node.m_y - (0.0f + ovr)) * cell_sy + m_miny;
			max[0] = (node.m_x + (1.0f + ovr)) * cell_sx + m_minx;
			max[1] = (node.m_y + (1.0f + ovr)) * cell_sy + m_miny;
		}

	private:

		QuadTree(QuadTree const &); // no copying, the nodes contain pointers
		QuadTree& operator=(QuadTree const&);

		// Allocate a new node
		Node* NewNode(size_t level, size_t x, size_t y, Node* parent)
		{
			// Push back does not invalidate existing node for std::deque
			m_nodes.push_back(Node(level, x, y, parent));
			return &m_nodes.back();
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/fmt.h"
#include "pr/maths/maths.h"
#include <fstream>

namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_quadtree)
		{
			struct Watzit
			{
				float pos[2];
				float radius;
				Watzit(float x, float y, float r) { pos[0] = x; pos[1] = y; radius = r; }
			};

			pr::QuadTree<Watzit> qtree(-10,-5,20,10);

			Watzit w0(0,0,0);
			auto n0 = qtree.Insert(w0, w0.pos, w0.radius);

			PR_CHECK(qtree.m_nodes.size(), 32U);
			PR_CHECK(n0->m_level, 31U);
			PR_CHECK(n0->m_x, 0x40000000U);
			PR_CHECK(n0->m_y, 0x40000000U);

			Watzit w1(-2.5f, -2.5f, 2.0f);
			auto n1 = qtree.Insert(w1, w1.pos, w1.radius);

			PR_CHECK(qtree.m_nodes.size(), 34U);
			PR_CHECK(n1->m_level, 2U);
			PR_CHECK(n1->m_x, 1U);
			PR_CHECK(n1->m_y, 1U);

			for (int i = 0; i != 100; ++i)
			{
				Watzit w(pr::rand::fltc(0.0f, qtree.m_sizex), pr::rand::fltc(0.0f, qtree.m_sizey), 0.2f*pr::rand::fltc(0.0f, pr::Len2(qtree.m_sizex, qtree.m_sizey)));
				qtree.Insert(w, w.pos, w.radius);
			}

			/*
			{// Ldr script for the tree
				std::ofstream outf("D:\\dump\\quadtree.ldr");
				float const spread = 2.0f;
				for (auto& n : qtree.m_nodes)
				{
					float min[2], max[2]; qtree.NodeBounds(n, false, min, max);
					outf << pr::FmtS("*Box b_%d_%d_%d 7F008000 { %f %f 0.05 *o2w { *pos {%f %f %f} } }"
						,n.m_level ,n.m_x ,n.m_y
						,qtree.CellSizeX(n.m_level)
						,qtree.CellSizeY(n.m_level)
						,(max[0] + min[0]) * 0.5f
						,(max[1] + min[1]) * 0.5f
						,n.m_level * spread)
						<< std::endl;

					//for (auto& item : n.m_items)
					//	outf << pr::FmtS("*Circle item 7F800000 { 3 %f *o2w { *pos {%f %f %f} } *Solid }"
					//		,item.radius
					//		,item.pos[0]
					//		,item.pos[1]
					//		,n.m_level * spread)
					//		<< std::endl;
				}
			}//*/
		}
	}
}
#endif