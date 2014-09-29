//*****************************************
// Quad Tree
//  Copyright (c) Rylogic Ltd 2014
//*****************************************
// Sparce loose quad tree.
// Items in the nodes of the tree can over hang up to half
// the size of the smallest dimension of the node.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace pr.container
{
	/// <summary>Loose sparce quad tree</summary>
	public class QuadTree<TItem>
	{
		/// <summary>The coordinates of a node within the tree</summary>
		public class Coord
		{
			public uint m_level;      // The level in the quad tree that this node is in
			public uint m_x, m_y;     // The coordinates of this node within the level
			public Coord(uint level = 0, uint x = 0, uint y = 0)
			{
				m_level = level;
				m_x = x;
				m_y = y;
			}
			public Coord(Coord rhs)
			{
				m_level = rhs.m_level;
				m_x = rhs.m_x;
				m_y = rhs.m_y;
			}
			public override bool Equals(object obj)
			{
				var rhs = obj as Coord;
				return rhs != null
					&& Equals(m_level ,rhs.m_level)
					&& Equals(m_x     ,rhs.m_x    )
					&& Equals(m_y     ,rhs.m_y    );
			}
			public override int GetHashCode()
			{
				return
					base.GetHashCode()^
					m_level.GetHashCode()^
					m_x.GetHashCode()^
					m_y.GetHashCode();
			}
			public override string ToString() { return "Level:" + m_level + " X:" + m_x + " Y:" + m_y; }
		};

		/// <summary>Quad tree node</summary>
		public class Node :Coord
		{
			public List<TItem> m_items;  // The items contained in this node
			public Node        m_parent; // Pointer to the parent node
			public Node[]      m_child;  // Pointers to the child nodes
			public Node(Coord coord, Node parent) :base(coord)
			{
				m_items = new List<TItem>();
				m_parent = parent;
				m_child = new Node[4];
			}
			public override string ToString() { return "Items:" + m_items.Count + " Coord:" + base.ToString(); }
		};

		private readonly List<Node> m_nodes;         // Storage for the nodes. Note, references are not invalidated by push/pop at the front/back of a std::deque
		private readonly Node       m_root;          // The top node of the tree
		private readonly float      m_minx,m_miny;   // The min x,y corner of the region covered by the quad tree
		private readonly float      m_sizex,m_sizey; // The x,y size of the region covered by the quad tree
		private readonly uint       m_max_levels;    // The maximum depth the tree will grow to
		private int                 m_count;         // The number of items added to the tree

		
		public QuadTree(RectangleF rect, uint max_levels = 16) :this(rect.X, rect.Y, rect.Width, rect.Height, max_levels) {}
		public QuadTree(PointF min, SizeF size, uint max_levels = 16) :this(min.X, min.Y, size.Width, size.Height, max_levels) {}
		public QuadTree(float minx, float miny, float sizex, float sizey, uint max_levels = 16)
		{
			m_nodes      = new List<Node>();
			m_root       = NewNode(new Coord(), null);
			m_minx       = minx;
			m_miny       = miny;
			m_sizex      = sizex;
			m_sizey      = sizey;
			m_max_levels = max_levels < 32 ? max_levels : 32;
			m_count      = 0;
		}

		/// <summary>All the nodes in the quad tree</summary>
		public List<Node> Nodes { get { return m_nodes; } }

		/// <summary>The min x,y corner of the region covered by the quad tree</summary>
		public PointF Location { get { return new PointF(m_minx, m_miny); } }

		/// <summary>The x,y size of the region covered by the quad tree</summary>
		public SizeF Size { get { return new SizeF(m_sizex, m_sizey); } }

		/// <summary>The maximum depth of the quad tree</summary>
		public uint MaxLevels { get { return m_max_levels; } }

		/// <summary>The number of items added to the tree</summary>
		public int Count { get { return m_count; } }

		/// <summary>
		/// Returns the maximum index value for a given level.
		/// e.g level 0 = [0,1), level 1 = [0,2), level 4 = [0, 8), etc</summary>
		public static uint MaxIndex(uint level)
		{
			return 1U << (int)level;
		}

		/// <summary>Returns the level in the quad tree for an item bounded by 'radius'.</summary>
		public uint GetLevel(float radius)
		{
			var twor = 2 * radius;
			for (uint i = 1; i != m_max_levels; ++i)
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

		/// <summary>Returns the dimensions of a cell at the given level</summary>
		public float CellSizeX(uint level)
		{
			return m_sizex / MaxIndex(level);
		}
		public float CellSizeY(uint level)
		{
			return m_sizey / MaxIndex(level);
		}

		/// <summary>Converts 'coord' from its current level to 'to_level'</summary>
		public Coord CoordAtLevel(Coord coord_, uint to_level)
		{
			Debug.Assert(SanityCheck(coord_), "coord is not valid");
			var coord = new Coord(coord_);
			if (coord.m_level >= to_level)
			{
				coord.m_x >>= (int)(coord.m_level - to_level);
				coord.m_y >>= (int)(coord.m_level - to_level);
			}
			else
			{
				coord.m_x <<= (int)(to_level - coord.m_level);
				coord.m_y <<= (int)(to_level - coord.m_level);
			}
			coord.m_level = to_level;
			return coord;
		}

		/// <summary>Returns the quad that a node at 'coord' would be in at 'level'</summary>
		public int QuadAtLevel(Coord coord, uint level)
		{
			Debug.Assert(SanityCheck(coord), "invalid coordinate");
			var c = CoordAtLevel(coord, level);
			return (int)((c.m_x & 1) + 2*(c.m_y & 1));
		}

		/// <summary>Returns the coordinate of the cell that an object bounded by 'point' and 'radius' would be added to</summary>
		public Coord GetLevelAndIndices(float[] point, float radius)
		{
			Debug.Assert(point.Length >= 2);

			// Find 'point' relative to the min x,y of the region
			float[] pt = new []{point[0] - m_minx, point[1] - m_miny};
			Debug.Assert(radius >= 0.0f, "negative radius");

			// Get the node location for 'point'+'radius'
			uint level = GetLevel(radius);
			int x = (int)(pt[0] / CellSizeX(level)); // signed because pt[0] might be less than m_minx
			int y = (int)(pt[1] / CellSizeY(level)); // signed because pt[1] might be less than m_miny
			Debug.Assert(pt[0] / CellSizeX(level) < (float)int.MaxValue, "Cell index for 'point' causes overflow because it's too far from the quad tree region");
			Debug.Assert(pt[0] / CellSizeX(level) > (float)int.MinValue, "Cell index for 'point' causes overflow because it's too far from the quad tree region");
			Debug.Assert(pt[1] / CellSizeY(level) < (float)int.MaxValue, "Cell index for 'point' causes overflow because it's too far from the quad tree region");
			Debug.Assert(pt[1] / CellSizeY(level) > (float)int.MinValue, "Cell index for 'point' causes overflow because it's too far from the quad tree region");

			// If 'point' is outside of the region then we need to keep going up levels
			// until 'point' + 'radius' is within half the cell width of the closest cell.
			// This is a special case for when 'point' is outside the region but not by more
			// than half of the smallest region dimension.
			if (pt[0] < 0.0f || pt[0] >= m_sizex || pt[1] < 0.0f || pt[1] >= m_sizey)
			{
				float xdist = 0.0f, ydist = 0.0f;
				if (pt[0] < 0.0f)     { xdist = -pt[0] + radius;           x = 0; }
				if (pt[0] >= m_sizex) { xdist =  pt[0] - m_sizex + radius; x = (int)(MaxIndex(level) - 1); }
				if (pt[1] < 0.0f)     { ydist = -pt[1] + radius;           y = 0; }
				if (pt[1] >= m_sizey) { ydist =  pt[1] - m_sizey + radius; y = (int)(MaxIndex(level) - 1); }

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

			var c = new Coord(level, (uint)x, (uint)y);
			Debug.Assert(SanityCheck(c), "invalid coordinate");
			return c;
		}

		/// <summary>
		/// Insert an item into the quad tree
		/// Inserting an item that is too big for the quad will result in it being added to the root node
		/// Returns the node that contains 'item'</summary>
		public Node Insert(TItem item, float[] point, float radius)
		{
			Debug.Assert(point.Length >= 2);

			// Find where 'item' should go
			Coord c = GetLevelAndIndices(point, radius);

			// Get a node at that position
			Node node = GetOrCreateNode(c);

			// Add 'item' to the collection in this node
			node.m_items.Add(item);
			++m_count;
			return node;
		}

		/// <summary>Traverse the quad tree returning the node at (level,x,y) (adding the node if necessary)</summary>
		public Node GetOrCreateNode(Coord coord)
		{
			Debug.Assert(SanityCheck(coord), "invalidate coordinate");

			// Special case the root node
			if (coord.m_level == 0)
				return m_root;

			// Navigate down the quad tree to look for an existing node
			// at 'coord' or to find where a new node should be added
			int quad = 0;
			Node node = m_root;
			Node existing = null;
			for (;node.m_level < coord.m_level;)
			{
				// Get the child quad to decend to
				quad = QuadAtLevel(coord, node.m_level + 1);
				existing = node.m_child[quad];

				// There is no child, search done. 'node' is the parent of the node
				// we need to add. (which will go in m_child[quad])
				if (existing == null)
					break;

				// There is a child which is at or below the level where we need
				// to add a node. If below, then a node will be inserted between
				// 'node' and 'node->m_child[quad]'
				var lvl = Math.Min(existing.m_level, coord.m_level);
				if (existing.m_level >= coord.m_level ||
					!Equals(CoordAtLevel(existing, lvl), CoordAtLevel(coord, lvl)))
					break;

				// The child is higher than where we need to add a node, keep decending
				node = node.m_child[quad];
			}

			// If there is no node at 'coord' then add one.
			if (existing == null)
			{
				node.m_child[quad] = NewNode(coord, node);
				Debug.Assert(SanityCheck(node.m_child[quad]));
				return node.m_child[quad];
			}

			// If the child node is the one we want, use it
			if (Equals(existing, coord))
			{
				Debug.Assert(SanityCheck(existing));
				return existing;
			}

			// If the existing child node is not the one we want we need to
			// insert a node above it such that 'existing' and 'newchild' are
			// no longer in the same quad. It may be that the new node is also
			// the new node that we wanted to add.
			Debug.Assert(node.m_level < existing.m_level - 1, "should only happen when a level has been skipped");

			// Find the level for the intermediate node.
			// We're looking for the lowest level where the coordinates of
			// 'existing' and 'coord' are the same. As children of that
			// level, they should then be in different quads.
			Coord icoord = CoordAtLevel(coord, Math.Min(existing.m_level, coord.m_level));
			for (;;)
			{
				var ecoord = CoordAtLevel(existing, icoord.m_level);
				var ncoord = CoordAtLevel(coord   , icoord.m_level);
				if (Equals(ecoord,ncoord)) break;
				icoord = CoordAtLevel(icoord, icoord.m_level - 1);
				Debug.Assert(!Equals(icoord, node), "It must be possible to insert a node below 'node'");
			}

			// Insert the intermediate node
			Node interm = NewNode(icoord, node);
			int iquad = QuadAtLevel(icoord, node.m_level + 1);
			node.m_child[iquad] = interm;

			// There are now two possibilities, either 'existing' and 'coord' are both
			// parented by 'interm', or 'coord' == 'interm' and 'existing' is parented
			// by 'coord'.
			int equad = QuadAtLevel(existing, icoord.m_level + 1);
			int nquad = QuadAtLevel(coord,    icoord.m_level + 1);

			Node newchild;
			if (Equals(coord, interm))
			{
				// 'coord' is 'interm'
				newchild = interm;

				// Insert 'existing' into interm
				existing.m_parent = interm;
				interm.m_child[equad] = existing;
			}
			// 'existing' and 'coord' are in different quads within 'interm'
			else
			{
				Debug.Assert(equad != nquad);

				// Insert 'newchild' into interm
				newchild = NewNode(coord, interm);
				interm.m_child[nquad] = newchild;

				// Insert 'existing' into interm
				existing.m_parent = interm;
				interm.m_child[equad] = existing;
			}

			// sanity check
			Debug.Assert(SanityCheck(node));
			Debug.Assert(SanityCheck(interm));
			Debug.Assert(SanityCheck(existing));
			Debug.Assert(SanityCheck(newchild));
			return newchild;
		}

		/// <summary>
		/// Traverse the quad tree passing each item that possibly intersects 'point','radius' to 'pred'
		/// 'pred' should return false to end the traversal, or true to continue.
		/// Returns 'true' if a full search occurred, false if 'pred' returned false ending the search early.</summary>
		public bool Traverse(float[] point, float radius, Func<TItem,Node,bool> pred, Node root = null)
		{
			Debug.Assert(point.Length >= 2);

			root = root ?? m_root;

			// Parse the items at this level to pred
			foreach (var item in root.m_items)
			{
				if (!pred(item, root))
					return false;
			}

			// Jump to each child node that might contain items that overlap with 'point','radius'
			foreach (var child in root.m_child)
			{
				if (child == null) continue;
				if (!Overlaps(child, point, radius)) continue;
				if (!Traverse(point, radius, pred, child))
					return false;
			}

			return true;
		}
		public bool Traverse(float[] point, float radius, Func<TItem,bool> pred, Node root = null)
		{
			return Traverse(point, radius, (i,n) => pred(i), root);
		}
		
		/// <summary>Returns true if 'node' can contain an item could overlay 'point'+'radius' (in region space).</summary>
		public bool Overlaps(Node node, float[] point, float radius)
		{
			Debug.Assert(point.Length >= 2);
			float[] min, max;
			NodeBounds(node, true, out min, out max);
			return !(point[0] + radius < min[0] || point[0] - radius > max[0] ||
					 point[1] + radius < min[1] || point[1] - radius > max[1]);
		}

		/// <summary>Return the bounds of 'node', optionally including the region that items in the node might overlap (in region space).</summary>
		public void NodeBounds(Node node, bool overlap_region, out float[] min, out float[] max)
		{
			min = new float[2];
			max = new float[2];
			float ovr = overlap_region ? 0.5f : 0.0f;
			float cell_sx = CellSizeX(node.m_level);
			float cell_sy = CellSizeY(node.m_level);
			min[0] = (node.m_x - (0.0f + ovr)) * cell_sx + m_minx;
			min[1] = (node.m_y - (0.0f + ovr)) * cell_sy + m_miny;
			max[0] = (node.m_x + (1.0f + ovr)) * cell_sx + m_minx;
			max[1] = (node.m_y + (1.0f + ovr)) * cell_sy + m_miny;
		}

		/// <summary>Sanity check a node</summary>
		internal bool SanityCheck(Node node)
		{
			for (int i = 0; i != 4; ++i)
			{
				var child = node.m_child[i];
				if (child == null) continue;

				// Child must be a lower level
				if (child.m_level <= node.m_level)
					return false;

				// The child coords, converted to the parent level should equal the parent's coords
				if (!Equals(node, CoordAtLevel(child, node.m_level)))
					return false;

				// Check each child is in the correct quad
				int quad = QuadAtLevel(child, node.m_level + 1);
				if (i != quad)
					return false;
			}

			if (node.m_parent != null)
			{
				// Node must be a lower level than parent
				if (node.m_level <= node.m_parent.m_level)
					return false;

				// The child coords, converted to the parent level should equal the parent's coords
				if (!Equals(node.m_parent, CoordAtLevel(node, node.m_parent.m_level)))
					return false;

				// Check node is in the correct quad in the parent
				int quad = QuadAtLevel(node, node.m_parent.m_level + 1);
				if (!ReferenceEquals(node.m_parent.m_child[quad], node))
					return false;
			}

			return true;
		}

		/// <summary>Sanity check a coord</summary>
		internal bool SanityCheck(Coord coord)
		{
			if (coord.m_level > m_max_levels)
				return false;
			if (coord.m_x >= MaxIndex(coord.m_level))
				return false;
			if (coord.m_y >= MaxIndex(coord.m_level))
				return false;

			return true;
		}

		/// <summary>Allocate a new node</summary>
		private Node NewNode(Coord coord, Node parent)
		{
			var node = new Node(coord, parent);
			m_nodes.Add(node);
			return node;
		}
	}
}


#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Linq;
	using container;
	using extn;
	using maths;

	[TestFixture] public class TestQuadTree
	{
		private static int Id { get { return ++m_impl_id; } }
		private static int m_impl_id = 0;
			
		public class Watzit
		{
			public float[] pos;
			public float radius;
			public int id;
			public bool flag;
			public Watzit(float x, float y, float r)
			{
				pos = new[]{x, y};
				radius = r;
				id = Id;
				flag = false;
			}
		}
			
		public static bool Collide(Watzit lhs, Watzit rhs)
		{
			float[] diff = new[]{rhs.pos[0] - lhs.pos[0], rhs.pos[1] - lhs.pos[1]};
			return Maths.Len2(diff[0], diff[1]) < lhs.radius + rhs.radius;
		}

		[Test] public void QuadTree()
		{
			var qtree = new QuadTree<Watzit>(-10,-5,20,10);

			// just inside quad0 at the root level
			var w0 = new Watzit(-0.5f*qtree.CellSizeX(15),-0.5f*qtree.CellSizeY(15), 0);
			var n0 = qtree.Insert(w0, w0.pos, w0.radius);
			Assert.AreEqual(2, qtree.Nodes.Count);
			Assert.AreEqual(15U, n0.m_level);
			Assert.AreEqual(0x4000U - 1, n0.m_x);
			Assert.AreEqual(0x4000U - 1, n0.m_y);

			// somewhere in quad3 at the root level
			var w1 = new Watzit(2.5f, 2.5f, 0.2f);
			var n1 = qtree.Insert(w1, w1.pos, w1.radius);
			Assert.AreEqual(3, qtree.Nodes.Count);
			Assert.AreEqual(4U, n1.m_level);
			Assert.AreEqual(10U, n1.m_x);
			Assert.AreEqual(12U, n1.m_y);

			// Outside the reqion but within the overhang at level 1
			var w2 = new Watzit(-14.99f, -7.2499f, 0);
			var n2 = qtree.Insert(w2, w2.pos, w2.radius);
			Assert.AreEqual(4, qtree.Nodes.Count);
			Assert.AreEqual(1U, n2.m_level);
			Assert.AreEqual(0U, n2.m_x);
			Assert.AreEqual(0U, n2.m_y);

			// Outside the reqion on y but within on x
			var w3 = new Watzit(6.5f, 7.24449f, 0);
			var n3 = qtree.Insert(w3, w3.pos, w3.radius);
			Assert.AreEqual(5, qtree.Nodes.Count);
			Assert.AreEqual(1U, n3.m_level);
			Assert.AreEqual(1U, n3.m_x);
			Assert.AreEqual(1U, n3.m_y);

			var rand = new Rand();
			Func<float,float,float> fltc = (avr,d) => rand.FloatC(avr, d);
			Func<float,float,float> fltr = (mn,mx) => rand.Float(mn, mx);

			for (int i = 0; i != 10000; ++i)
			{
				var w = new Watzit(fltc(0.0f, qtree.Size.Width), fltc(0.0f, qtree.Size.Height), 0.2f*fltr(0.0f, 0.5f * Maths.Len2(qtree.Size.Width, qtree.Size.Height)));
				var n = qtree.Insert(w, w.pos, w.radius);

				// the root node can have arbitrarily large objects in it
				if (n.m_level != 0)
				{
					float[] min,max;
					qtree.NodeBounds(n, true, out min, out max);
					Assert.True(w.pos[0] - w.radius >= min[0]);
					Assert.True(w.pos[1] - w.radius >= min[1]);
					Assert.True(w.pos[0] + w.radius <  max[0]);
					Assert.True(w.pos[1] + w.radius <  max[1]);
				}
			}

			// Sanity check
			int count = 0;
			foreach (var node in qtree.Nodes)
			{
				Assert.True(qtree.SanityCheck(node));
				count += node.m_items.Count;
			}
			Assert.AreEqual(qtree.Count, count);

			for (int i = 0; i != 100; ++i)
			{
				// reset flags
				foreach (var node in qtree.Nodes)
					foreach (var item in node.m_items)
						item.flag = false;

				var W = new Watzit(fltc(0.0f, qtree.Size.Width), fltc(0.0f, qtree.Size.Height), 0.2f*fltr(0.0f, 0.5f * Maths.Len2(qtree.Size.Width, qtree.Size.Height)));
				qtree.Traverse(W.pos, W.radius, (w,n) =>
					{
						w.flag = Collide(W, w);
						return true;
					});

				// All flagged should collide, not flagged should not
				foreach (var node in qtree.Nodes)
					foreach (var item in node.m_items)
						Assert.AreEqual(item.flag, Collide(W, item));
			}
		}
	}
}
#endif
