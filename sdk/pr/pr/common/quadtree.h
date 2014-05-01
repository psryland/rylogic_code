//*****************************************
// Quad Tree
//  Copyright © Rylogic Ltd 2014
//*****************************************
// Sparce loose quad tree.
// Items in the nodes of the tree can over hang up to half
// the size of the smallest dimension of the node.

#pragma once

#include <deque>
#include <limits>
#include <algorithm>
#include <cassert>

namespace pr
{
	namespace quad_tree
	{
		// The coordinates of a node within the tree
		struct Coord
		{
			size_t m_level;      // The level in the quad tree that this node is in
			size_t m_x, m_y;     // The coordinates of this node within the level
			Coord(size_t level = 0, size_t x = 0, size_t y = 0) :m_level(level) ,m_x(x) ,m_y(y) {}
		};
		inline bool operator == (Coord const& lhs, Coord const& rhs)
		{
			return lhs.m_level == rhs.m_level && lhs.m_x == rhs.m_x && lhs.m_y == rhs.m_y;
		}
		inline bool operator != (Coord const& lhs, Coord const& rhs) { return !(lhs == rhs); }

		// Quad tree node
		template <typename TItem> struct Node :Coord
		{
			typedef std::deque<TItem> ItemCont;

			ItemCont     m_items;    // The items contained in this node
			Node<TItem>* m_parent;   // Pointer to the parent node
			Node<TItem>* m_child[4]; // Pointers to the child nodes
			Node(Coord coord, Node<TItem>* parent) :Coord(coord) ,m_items() ,m_parent(parent) ,m_child() {}
		};
		//template <typename TItem> inline bool operator == (Coord const& lhs, Coord const& rhs)
		//{
		//	return lhs.m_level == rhs.m_level && lhs.x == rhs.x && lhs.y == rhs.y;
		//}
		//template <typename TItem> inline bool operator != (Coord const& lhs, Coord const& rhs) { return !(lhs == rhs); }
	}

	// Loose quad tree
	template <typename TItem> struct QuadTree
	{
		typedef quad_tree::Node<TItem> Node;
		typedef quad_tree::Coord Coord;

		std::deque<Node>  m_nodes;         // Storage for the nodes. Note, references are not invalidated by push/pop at the front/back of a std::deque
		Node*             m_root;          // The top node of the tree
		float             m_minx,m_miny;   // The min x,y corner of the region covered by the quad tree
		float             m_sizex,m_sizey; // The x,y size of the region covered by the quad tree
		size_t            m_max_levels;    // The maximum depth the tree will grow to
		size_t            m_count;         // The number of items added to the tree

		QuadTree(float minx, float miny, float sizex, float sizey, size_t max_levels = 16)
			:m_nodes()
			,m_root(NewNode(Coord(),nullptr))
			,m_minx(minx)
			,m_miny(miny)
			,m_sizex(sizex)
			,m_sizey(sizey)
			,m_max_levels(max_levels < 32 ? max_levels : 32)
			,m_count()
		{}

		// Returns the maximum index value for a given level.
		// e.g level 0 = [0,1), level 1 = [0,2), level 4 = [0, 8), etc
		static size_t MaxIndex(size_t level)
		{
			return size_t(1) << level;
		}

		// Returns the level in the quad tree for an item bounded by 'radius'.
		size_t GetLevel(float radius) const
		{
			auto twor = 2 * radius;
			for (size_t i = 1; i != m_max_levels; ++i)
			{
				// We need to handle the worst case position of the item which is
				// the item positioned on the edge of the cell, overhanging by 'radius'.
				// That means find the deepest level for which half the smallest dimension
				// is greater than 'radius'.
				if (twor > CellSizeX(i) || twor > CellSizeY(i))
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

		// Converts 'coord' from its current level to 'to_level'
		Coord CoordAtLevel(Coord coord, size_t to_level) const
		{
			assert(SanityCheck(coord) && "coord is not valid");
			if (coord.m_level >= to_level)
			{
				coord.m_x >>= (coord.m_level - to_level);
				coord.m_y >>= (coord.m_level - to_level);
			}
			else
			{
				coord.m_x <<= (to_level - coord.m_level);
				coord.m_y <<= (to_level - coord.m_level);
			}
			coord.m_level = to_level;
			return coord;
		}

		// Returns the quad that a node at 'coord' would be in at 'level'
		int QuadAtLevel(Coord coord, size_t level) const
		{
			assert(SanityCheck(coord) && "invalid coordinate");
			auto c = CoordAtLevel(coord, level);
			return (c.m_x & 1) + 2*(c.m_y & 1);
		}

		// Returns the coordinate of the cell that an object bounded by 'point' and 'radius' would be added to
		Coord GetLevelAndIndices(float const (&point)[2], float radius) const
		{
			// Find 'point' relative to the min x,y of the region
			float pt[2] = {point[0] - m_minx, point[1] - m_miny};
			assert(radius >= 0.0f && "negative radius");

			// Get the node location for 'point'+'radius'
			size_t level = GetLevel(radius);
			int x = int(pt[0] / CellSizeX(level)); // signed because pt[0] might be less than m_minx
			int y = int(pt[1] / CellSizeY(level)); // signed because pt[1] might be less than m_miny
			assert(pt[0] / CellSizeX(level) < float(std::numeric_limits<int>::max()) && "Cell index for 'point' causes overflow because it's too far from the quad tree region");
			assert(pt[0] / CellSizeX(level) > float(std::numeric_limits<int>::min()) && "Cell index for 'point' causes overflow because it's too far from the quad tree region");
			assert(pt[1] / CellSizeY(level) < float(std::numeric_limits<int>::max()) && "Cell index for 'point' causes overflow because it's too far from the quad tree region");
			assert(pt[1] / CellSizeY(level) > float(std::numeric_limits<int>::min()) && "Cell index for 'point' causes overflow because it's too far from the quad tree region");

			// If 'point' is outside of the region then we need to keep going up levels
			// until 'point' + 'radius' is within half the cell width of the closest cell.
			// This is a special case for when 'point' is outside the region but not by more
			// than half of the smallest region dimension.
			if (pt[0] < 0.0f || pt[0] >= m_sizex || pt[1] < 0.0f || pt[1] >= m_sizey)
			{
				float xdist = 0.0f, ydist = 0.0f;
				if (pt[0] < 0.0f)     { xdist = -pt[0] + radius;           x = 0; }
				if (pt[0] >= m_sizex) { xdist =  pt[0] - m_sizex + radius; x = static_cast<int>(MaxIndex(level) - 1); }
				if (pt[1] < 0.0f)     { ydist = -pt[1] + radius;           y = 0; }
				if (pt[1] >= m_sizey) { ydist =  pt[1] - m_sizey + radius; y = static_cast<int>(MaxIndex(level) - 1); }

				while (level > 0 && 2.0f*xdist > CellSizeX(level))
				{
					--level;
					x /= 2;
					y /= 2;
				}
				while (level > 0 && 2.0f*ydist > CellSizeY(level))
				{
					--level;
					x /= 2;
					y /= 2;
				}
			}

			Coord c(level, size_t(x), size_t(y));
			assert(SanityCheck(c) && "invalid coordinate");
			return c;
		}

		// Insert an item into the quad tree
		// Inserting an item that is too big for the quad will result in it being added to the root node
		// Returns the node that contains 'item'
		Node const* Insert(TItem const& item, float const (&point)[2], float radius)
		{
			// Find where 'item' should go
			Coord c = GetLevelAndIndices(point, radius);

			// Get a node at that position
			Node* node = GetOrCreateNode(c);

			// Add 'item' to the collection in this node
			node->m_items.push_back(item);
			++m_count;
			return node;
		}

		// Traverse the quad tree returning the node at (level,x,y) (adding the node if necessary)
		Node* GetOrCreateNode(Coord coord)
		{
			assert(SanityCheck(coord) && "invalidate coordinate");

			// Special case the root node
			if (coord.m_level == 0)
				return m_root;

			// Navigate down the quad tree to look for an existing node
			// at 'coord' or to find where a new node should be added
			int quad = 0;
			Node* node = m_root;
			Node* existing = nullptr;
			for (;node->m_level < coord.m_level;)
			{
				// Get the child quad to decend to
				quad = QuadAtLevel(coord, node->m_level + 1);
				existing = node->m_child[quad];

				// There is no child, search done. 'node' is the parent of the node
				// we need to add. (which will go in m_child[quad])
				if (existing == nullptr)
					break;

				// There is a child which is at or below the level where we need
				// to add a node. If below, then a node will be inserted between
				// 'node' and 'node->m_child[quad]'
				auto lvl = std::min(existing->m_level, coord.m_level);
				if (existing->m_level >= coord.m_level ||
					CoordAtLevel(*existing, lvl) != CoordAtLevel(coord, lvl))
					break;

				// The child is higher than where we need to add a node, keep decending
				node = node->m_child[quad];
			}

			// If there is no node at 'coord' then add one.
			if (existing == nullptr)
			{
				node->m_child[quad] = NewNode(coord, node);
				assert(SanityCheck(*node->m_child[quad]));
				return node->m_child[quad];
			}

			// If the child node is the one we want, use it
			if (*existing == coord)
			{
				assert(SanityCheck(*existing));
				return existing;
			}

			// If the existing child node is not the one we want we need to
			// insert a node above it such that 'existing' and 'newchild' are
			// no longer in the same quad. It may be that the new node is also
			// the new node that we wanted to add.
			assert(node->m_level < existing->m_level - 1 && "should only happen when a level has been skipped");

			// Find the level for the intermediate node.
			// We're looking for the lowest level where the coordinates of
			// 'existing' and 'coord' are the same. As children of that
			// level, they should then be in different quads.
			Coord icoord = CoordAtLevel(coord, std::min(existing->m_level, coord.m_level));
			for (;;)
			{
				auto ecoord = CoordAtLevel(*existing, icoord.m_level);
				auto ncoord = CoordAtLevel(coord    , icoord.m_level);
				if (ecoord == ncoord) break;
				icoord = CoordAtLevel(icoord, icoord.m_level - 1);
				assert(icoord != *node && "It must be possible to insert a node below 'node'");
			}

			// Insert the intermediate node
			Node* interm = NewNode(icoord, node);
			int iquad = QuadAtLevel(icoord, node->m_level + 1);
			node->m_child[iquad] = interm;

			// There are now two possibilities, either 'existing' and 'coord' are both
			// parented by 'interm', or 'coord' == 'interm' and 'existing' is parented
			// by 'coord'.
			int equad = QuadAtLevel(*existing, icoord.m_level + 1);
			int nquad = QuadAtLevel(coord,     icoord.m_level + 1);

			Node* newchild;
			if (coord == *interm)
			{
				// 'coord' is 'interm'
				newchild = interm;

				// Insert 'existing' into interm
				existing->m_parent = interm;
				interm->m_child[equad] = existing;
			}
			// 'existing' and 'coord' are in different quads within 'interm'
			else
			{
				assert(equad != nquad);

				// Insert 'newchild' into interm
				newchild = NewNode(coord, interm);
				interm->m_child[nquad] = newchild;

				// Insert 'existing' into interm
				existing->m_parent = interm;
				interm->m_child[equad] = existing;
			}

			// sanity check
			assert(SanityCheck(*node));
			assert(SanityCheck(*interm));
			assert(SanityCheck(*existing));
			assert(SanityCheck(*newchild));
			return newchild;
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
				if (!pred(item, root)) // Note: callers could use void* for the second param if they don't care about the node
					return false;
			}

			// Jump to each child node that might contain items that overlap with 'point','radius'
			for (auto child : root->m_child)
			{
				if (child == nullptr) continue;
				if (!Overlaps(*child, point, radius)) continue;
				if (!Traverse(point, radius, pred, child))
					return false;
			}

			return true;
		}

		// Returns true if 'node' can contain an item could overlay 'point'+'radius' (in region space).
		bool Overlaps(Node const& node, float const (&point)[2], float radius) const
		{
			float min[2], max[2]; NodeBounds(node, true, min, max);
			return !(point[0] + radius < min[0] || point[0] - radius > max[0] ||
					 point[1] + radius < min[1] || point[1] - radius > max[1]);
		}

		// Return the bounds of 'node', optionally including the region that items in the node might overlap (in region space).
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

		// Sanity check a node
		bool SanityCheck(Node const& node) const
		{
			for (int i = 0; i != 4; ++i)
			{
				auto child = node.m_child[i];
				if (child == nullptr) continue;

				// Child must be a lower level
				if (child->m_level <= node.m_level)
					return false;

				// The child coords, converted to the parent level should equal the parent's coords
				if (node != CoordAtLevel(*child, node.m_level))
					return false;

				// Check each child is in the correct quad
				int quad = QuadAtLevel(*child, node.m_level + 1);
				if (i != quad)
					return false;
			}

			if (node.m_parent != nullptr)
			{
				// Node must be a lower level than parent
				if (node.m_level <= node.m_parent->m_level)
					return false;

				// The child coords, converted to the parent level should equal the parent's coords
				if (*node.m_parent != CoordAtLevel(node, node.m_parent->m_level))
					return false;

				// Check node is in the correct quad in the parent
				int quad = QuadAtLevel(node, node.m_parent->m_level + 1);
				if (node.m_parent->m_child[quad] != &node)
					return false;
			}

			return true;
		}

		// Sanity check a coord
		bool SanityCheck(Coord const& coord) const
		{
			if (coord.m_level > m_max_levels)
				return false;
			if (coord.m_x >= MaxIndex(coord.m_level))
				return false;
			if (coord.m_y >= MaxIndex(coord.m_level))
				return false;

			return true;
		}

	private:

		QuadTree(QuadTree const &); // no copying, the nodes contain pointers
		QuadTree& operator=(QuadTree const&);

		// Allocate a new node
		Node* NewNode(Coord coord, Node* parent)
		{
			// Push back does not invalidate existing node for std::deque
			m_nodes.push_back(Node(coord, parent));
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
		namespace quad_tree
		{
			inline int Id() { static int s_id = 0; return ++s_id; }
			struct Watzit
			{
				float pos[2];
				float radius;
				int id;
				bool flag;
				Watzit(float x, float y, float r) { pos[0] = x; pos[1] = y; radius = r; id = Id(); flag = false; }
			};
			inline bool Collide(Watzit const& lhs, Watzit const& rhs)
			{
				float diff[2] = {rhs.pos[0] - lhs.pos[0], rhs.pos[1] - lhs.pos[1]};
				return pr::Len2(diff[0], diff[1]) < lhs.radius + rhs.radius;
			}
		}

		PRUnitTest(pr_common_quadtree)
		{
			using namespace pr::unittests::quad_tree;

			pr::QuadTree<Watzit> qtree(-10,-5,20,10);

			// just inside quad0 at the root level
			Watzit w0(-0.5f*qtree.CellSizeX(15),-0.5f*qtree.CellSizeY(15), 0);
			auto n0 = qtree.Insert(w0, w0.pos, w0.radius);
			PR_CHECK(qtree.m_nodes.size(), 2U);
			PR_CHECK(n0->m_level, 15U);
			PR_CHECK(n0->m_x, 0x4000U - 1);
			PR_CHECK(n0->m_y, 0x4000U - 1);

			// somewhere in quad3 at the root level
			Watzit w1(2.5f, 2.5f, 0.2f);
			auto n1 = qtree.Insert(w1, w1.pos, w1.radius);
			PR_CHECK(qtree.m_nodes.size(), 3U);
			PR_CHECK(n1->m_level, 4U);
			PR_CHECK(n1->m_x, 10U);
			PR_CHECK(n1->m_y, 12U);

			// Outside the reqion but within the overhang at level 1
			Watzit w2(-14.99f, -7.2499f, 0);
			auto n2 = qtree.Insert(w2, w2.pos, w2.radius);
			PR_CHECK(qtree.m_nodes.size(), 4U);
			PR_CHECK(n2->m_level, 1U);
			PR_CHECK(n2->m_x, 0U);
			PR_CHECK(n2->m_y, 0U);

			// Outside the reqion on y but within on x
			Watzit w3(6.5f, 7.24449f, 0);
			auto n3 = qtree.Insert(w3, w3.pos, w3.radius);
			PR_CHECK(qtree.m_nodes.size(), 5U);
			PR_CHECK(n3->m_level, 1U);
			PR_CHECK(n3->m_x, 1U);
			PR_CHECK(n3->m_y, 1U);

			for (int i = 0; i != 10000; ++i)
			{
				Watzit w(pr::rand::fltc(0.0f, qtree.m_sizex), pr::rand::fltc(0.0f, qtree.m_sizey), 0.2f*pr::rand::fltr(0.0f, 0.5f * pr::Len2(qtree.m_sizex, qtree.m_sizey)));
				auto n = qtree.Insert(w, w.pos, w.radius);

				// the root node can have arbitrarily large objects in it
				if (n->m_level != 0)
				{
					float min[2],max[2];
					qtree.NodeBounds(*n, true, min, max);
					PR_CHECK(w.pos[0] - w.radius >= min[0], true);
					PR_CHECK(w.pos[1] - w.radius >= min[1], true);
					PR_CHECK(w.pos[0] + w.radius <  max[0], true);
					PR_CHECK(w.pos[1] + w.radius <  max[1], true);
				}
			}

			// Sanity check
			size_t count = 0;
			for (auto& node : qtree.m_nodes)
			{
				PR_CHECK(qtree.SanityCheck(node), true);
				count += node.m_items.size();
			}
			PR_CHECK(count, qtree.m_count);

			for (int i = 0; i != 100; ++i)
			{
				// reset flags
				for (auto& node : qtree.m_nodes)
					for (auto& item : node.m_items)
						item.flag = false;

				Watzit W(pr::rand::fltc(0.0f, qtree.m_sizex), pr::rand::fltc(0.0f, qtree.m_sizey), 0.2f*pr::rand::fltr(0.0f, 0.5f * pr::Len2(qtree.m_sizex, qtree.m_sizey)));
				qtree.Traverse(W.pos, W.radius, [&](Watzit& w, void*)
				{
					w.flag = Collide(W, w);
					return true;
				});

				// All flagged should collide, not flagged should not
				for (auto& node : qtree.m_nodes)
					for (auto& item : node.m_items)
						PR_CHECK(Collide(W, item), item.flag);
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