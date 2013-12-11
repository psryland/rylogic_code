//*****************************************************************************************
// Copyright © Rylogic Ltd 2011
// Inspired by Mark Rideout's TreeGridView control but massively refactored
//*****************************************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace pr.gui
{
	[DesignerCategory("Code")]
	[ComplexBindingProperties]
	[Docking(DockingBehavior.Ask)]
	public class TreeGridView :DataGridView
	{
		private readonly TreeGridNode  m_root;           // The root of the tree
		private bool                   m_show_lines;     // Display the tree lines

		/// <summary>Called just before a node is expanded</summary>
		public event EventHandler<ExpandingEventArgs> NodeExpanding;

		/// <summary>Called after a node is expanded</summary>
		public event EventHandler<ExpandedEventArgs>  NodeExpanded;

		/// <summary>Called just before a node is collapsed</summary>
		public event EventHandler<CollapsingEventArgs> NodeCollapsing;

		/// <summary>Called after a node is collapsed</summary>
		public event EventHandler<CollapsedEventArgs>  NodeCollapsed;

		public TreeGridView()
		{
			// Control when edit occurs because edit mode shouldn't start when expanding/collapsing
			//EditMode = DataGridViewEditMode.EditProgrammatically;

			// No support for adding or deleting rows by the user.
			AllowUserToAddRows = false;
			AllowUserToDeleteRows = false;
			ShowLines = true;

			// Create the root of the tree
			m_root = new TreeGridNode(this, true);

			// Causes all rows to be unshared
			base.Rows.CollectionChanged += (s,e)=>{};
		}

		/// <summary>Get/Set the number of top level nodes in the grid </summary>
		public int NodeCount
		{
			get { return RowCount; }
			set { RowCount = value; }
		}
		private new int RowCount // hide the row count property
		{
			get { return Nodes.Count; }
			set
			{
				if (value == 0) Nodes.Clear();
				for (int i = RowCount; i > value; --i) Nodes.RemoveAt(i-1);
				for (int i = RowCount; i < value; ++i) Nodes.Add(new TreeGridNode(this, false));
			}
		}

		/// <summary>Show/hide the tree lines</summary>
		public bool ShowLines
		{
			get { return m_show_lines; }
			set { if (value != m_show_lines) { m_show_lines = value; Invalidate(); } }
		}

		/// <summary>"Causes nodes to always show as expandable. Use the NodeExpanding event to add nodes</summary>
		[Description("Causes nodes to always show as expandable. Use the NodeExpanding event to add nodes.")]
		public bool VirtualNodes { get; set; }

		/// <summary>The grid current node</summary>
		public TreeGridNode CurrentNode
		{
			get { return CurrentRow; }
		}
		private new TreeGridNode CurrentRow
		{
			get { return base.CurrentRow as TreeGridNode; }
		}

		/// <summary>The collection of nodes in the grid</summary>
		[Category("Data")]
		[Description("The collection of nodes in the grid.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public TreeGridNodeCollection Nodes
		{
			get { return m_root.Nodes; }
		}

		/// <summary>Get/Set the associated image list</summary>
		public ImageList ImageList { get; set; }

		/// <summary>Prepare the grid for a node collapse.</summary>
		internal bool BeginCollapseNode(TreeGridNode node)
		{
			// Raise the collapsing event
			var  exp = new CollapsingEventArgs(node);
			if (NodeCollapsing != null) NodeCollapsing(this, exp);
			if (exp.Cancel) return false;
			SuspendLayout();
			return true;
		}

		/// <summary>Complete changes to the grid after a node collapse.</summary>
		internal void EndCollapseNode(TreeGridNode node)
		{
			ResumeLayout(true);
			if (NodeCollapsed != null) NodeCollapsed(this, new CollapsedEventArgs(node));
		}

		/// <summary>Prepare the grid for a node expansion</summary>
		internal bool BeginExpandNode(TreeGridNode node)
		{
			var exp = new ExpandingEventArgs(node);
			OnNodeExpanding(exp);
			if (exp.Cancel) return false;
			SuspendLayout();
			return true;
		}

		/// <summary>Compete changes to the grid after a node expand</summary>
		internal void EndExpandNode(TreeGridNode node)
		{
			OnNodeExpanded(new ExpandedEventArgs(node));
			ResumeLayout(true);
		}

		/// <summary>Sort the columns of the grid</summary>
		public override void Sort(DataGridViewColumn col, ListSortDirection direction)
		{
			EndEdit();
			SuspendLayout();
			var rows = new List<TreeGridNode>();
			int col_idx = col.Index;
			int sign = col.HeaderCell.SortGlyphDirection == SortOrder.Ascending ? -1 : 1;
			foreach (DataGridViewColumn c in Columns) c.HeaderCell.SortGlyphDirection = SortOrder.None;
			col.HeaderCell.SortGlyphDirection = sign > 0 ? SortOrder.Ascending : SortOrder.Descending;

			// ReSharper disable PossibleNullReferenceException
			// ReSharper disable AccessToModifiedClosure
			Action<TreeGridNode> sort = null;
			sort = node =>
			{
				foreach (TreeGridNode n in node.Nodes)
					if (n.Nodes.Count >= 2)
						sort(n);

				rows.Clear();
				rows.AddRange(node.Nodes);
				rows.Sort((lhs,rhs)=>
				{
					object v0 = col_idx < lhs.Cells.Count ? lhs.Cells[col_idx].Value : null;
					object v1 = col_idx < rhs.Cells.Count ? rhs.Cells[col_idx].Value : null;
					int result;
					if (v0 is IComparable || v1 is IComparable)
					{
						result = Comparer.Default.Compare(v0, v1);
					}
					else
					{
						if      (v0 == null) { result = v1 == null ? 0 : 1; }
						else if (v1 == null) { result = -1; }
						else                 { result = Comparer.Default.Compare(v0.ToString(), v1.ToString()); }
					}
					if (result == 0)
					{
						result = sign > 0 ? lhs.RowIndex - rhs.RowIndex : rhs.RowIndex - lhs.RowIndex;
					}
					return sign * result;
				});
				node.Nodes.Clear();
				foreach (TreeGridNode n in rows) node.Nodes.Add(n);
			};
			// ReSharper restore AccessToModifiedClosure
			// ReSharper restore PossibleNullReferenceException

			// Sort within node levels
			sort(m_root);
			ResumeLayout(true);
		}

		/// <summary>Handle cell editting via F2</summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (IsCurrentCellInEditMode)
			{
				base.OnKeyDown(e);
				return;
			}

			// Cause edit mode to begin since edit mode is disabled to support expanding/collapsing
			if (e.KeyCode == Keys.F2 && CurrentCellAddress.X > -1 && CurrentCellAddress.Y > -1)
			{
				if (!CurrentCell.Displayed) FirstDisplayedScrollingRowIndex = CurrentCellAddress.Y;
				BeginEdit(true);
				base.OnKeyDown(e);
				return;
			}

			// Handle special keys for tree cells
			var tcell = CurrentCell as TreeGridCell;
			if (tcell != null)
			{
				TreeGridNode node = tcell.OwningNode;
				if (node != null)
				{
					// Expand/Collapse using the space bar
					if (e.KeyCode == Keys.Space && node.HasChildren)
					{
						if (!node.IsExpanded) node.Expand();
						else                  node.Collapse();
						e.Handled = true;
					}

					// Left/Right keys jump up/down levels
					if (e.KeyCode == Keys.Left && e.Modifiers == Keys.None)
					{
						if (node.HasChildren && node.IsExpanded)
						{
							node.Collapse();
							e.Handled = true;
						}
						else if (node.Parent != null && !node.Parent.IsRoot)
						{
							node.Parent.Collapse();
							CurrentCell = node.Parent.TreeCell;
							e.Handled = true;
						}
					}
					if (e.KeyCode == Keys.Right && e.Modifiers == Keys.None)
					{
						if (node.HasChildren && !node.IsExpanded)
						{
							node.Expand();
							e.Handled = true;
						}
					}
				}
			}
			base.OnKeyDown(e);
		}

		/// <summary>Handle mouse clicks to detection tree node expands/collapses</summary>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			HitTestInfo info = HitTest(e.X, e.Y);
			if (info.Type == DataGridViewHitTestType.Cell)
			{
				var tcell = this[info.ColumnIndex, info.RowIndex] as TreeGridCell;
				if (tcell != null && tcell.GlyphClick(e.X - info.ColumnX, e.Y - info.RowY))
				{
					TreeGridNode node = tcell.OwningNode;
					if (node != null && (node.HasChildren || node.VirtualNodes))
					{
						if (node.IsExpanded) node.Collapse();
						else                 node.Expand();
						return;
					}
				}
			}
			base.OnMouseDown(e);
		}

		/// <summary>Raises the NodeExpanding event</summary>
		protected virtual void OnNodeExpanding(ExpandingEventArgs e)
		{
			if (NodeExpanding == null) return;
			NodeExpanding(this, e);
		}

		/// <summary>Raises the NodeExpanded event</summary>
		protected virtual void OnNodeExpanded(ExpandedEventArgs e)
		{
			if (NodeExpanded == null) return;
			NodeExpanded(this, e);
		}

		/// <summary>Raises the node collapsing event</summary>
		protected virtual void OnNodeCollapsing(CollapsingEventArgs e)
		{
			if (NodeCollapsing == null) return;
			NodeCollapsing(this, e);
		}

		/// <summary>Raises the node collapsed event</summary>
		protected virtual void OnNodeCollapsed(CollapsedEventArgs e)
		{
			if (NodeCollapsed == null) return;
			NodeCollapsed(this, e);
		}

		/// <summary>No support for databinding</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new object DataSource
		{
			get { return null; }
			set { throw new NotSupportedException("The TreeGridView does not support databinding"); }
		}

		/// <summary>No support for databinding</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new object DataMember
		{
			get { return null; }
			set { throw new NotSupportedException("The TreeGridView does not support databinding"); }
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new DataGridViewRowCollection Rows
		{
			get { return base.Rows; }
		}

		/// <summary>No support for virtual mode</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new bool VirtualMode
		{
			get { return false; }
			set { throw new NotSupportedException("The TreeGridView does not support virtual mode"); }
		}

		// ReSharper disable UnusedMember.Local
		/// <summary>None of the rows/nodes created use the row template, so it is hidden.</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		private new DataGridViewRow RowTemplate
		{
			get { return base.RowTemplate; }
			set { base.RowTemplate = value; }
		}
		// ReSharper restore UnusedMember.Local
	}

	/// <summary>A node/row in the tree grid view control</summary>
	[ToolboxItem(false)]
	[DesignTimeVisible(false)]
	public class TreeGridNode :DataGridViewRow
	{
		private TreeGridView           m_grid;                  // The grid that this node belongs to
		private TreeGridNode           m_parent;                // The parent node
		private TreeGridNodeCollection m_nodes;                 // The child nodes
		private Image                  m_image;                 // The image associated with this node
		private int                    m_image_index = -1;      // The image index, 'm_image' has a higher precedence
		private bool                   m_cells_created;         // True once the cells for this node have been created
		private bool                   m_is_displayed_in_grid;  // True if visible in the grid, unsited means not in the grid, but still in the child collection of 'm_parent'
		private bool                   m_is_expanded;           // True when the node is expanded
		private bool                   m_virtual_nodes;         // Per node level virtual nodes

		/// <summary>Construct the node</summary>
		internal TreeGridNode(TreeGridView grid) :this(grid, false) {}
		internal TreeGridNode(TreeGridView grid, bool displayed_in_grid)
		{
			// 'displayed_in_grid' will be false except for the 'm_root' node created by the grid
			m_grid = grid;
			m_parent = null;
			m_cells_created = false;
			m_is_displayed_in_grid = displayed_in_grid;
			m_is_expanded = displayed_in_grid;
			m_virtual_nodes = false;
		}

		/// <summary>A clone method must be provided for types derived from DataGridViewRow</summary>
		public override object Clone()
		{
			var r = (TreeGridNode)base.Clone();
			if (r != null)
			{
				r.m_grid         = m_grid;
				r.m_parent       = null;
				r.m_image        = m_image;
				r.m_image_index  = m_image_index;
				r.m_is_expanded  = m_is_expanded;
				r.m_is_displayed_in_grid = false;
			}
			return r;
		}

		/// <summary>Removed from the interface</summary>
		private new DataGridView DataGridView { get { return base.DataGridView; } }

		/// <summary>Get/Set the grid that this node belongs to. Only root nodes can be assigned to a grid</summary>
		public TreeGridView Grid
		{
			get { return m_grid; }
			// No setter because a node should never be moved to a different grid
			// Each node has cells created based on the number of columns in the grid
			// and those cells hold the data for the node. Moving a node between grids
			// would mean storing that data temporarily while the node has no grid
			// which is just messy.
		}

		/// <summary>Get/Set the parent node for this node</summary>
		public TreeGridNode Parent
		{
			get { return m_parent; }
			set
			{
				if (m_parent == value) return;
				if (value != null && m_grid != value.m_grid) throw new InvalidOperationException("Assigned parent must be within the same grid");

				// Check for circular connections
				for (TreeGridNode p = value; p != null; p = p.Parent)
					if (p == this) throw new InvalidOperationException("Assigned parent is a child of this node");

				// Remove the rows of this node from the grid
				RemoveRowsFromGrid();

				// Remove this node from its current parent
				if (m_parent != null)
				{
					m_parent.Nodes.RemoveInternal(this);

					// Refresh the glyph next to the parent node
					if (m_parent.IsDisplayedInGrid && m_parent.Nodes.Count == 0 && !m_parent.IsRoot)
						Grid.InvalidateRow(m_parent.RowIndex);
				}

				// Assign the new parent
				m_parent = value;

				if (m_parent != null)
				{
					int row_index = m_parent.NextRowIndex;

					// Add to the parents node collection
					m_parent.Nodes.AddInternal(this);

					// Refresh the glyph next to the parent node
					if (m_parent.IsDisplayedInGrid && m_parent.Nodes.Count == 1 && !m_parent.IsRoot)
						Grid.InvalidateRow(m_parent.RowIndex);

					// If this node should be visible in the grid then display it
					if (m_parent.IsDisplayedInGrid && m_parent.IsExpanded)
						DisplayRowsInGrid(ref row_index);
				}
			}
		}

		/// <summary>True if this node has no parent</summary>
		public bool IsRoot
		{
			get { return m_parent == null; }
		}

		/// <summary>True if the node is expanded, false if collapsed</summary>
		public bool IsExpanded
		{
			get { return m_is_expanded; }
		}

		/// <summary>True if this node is visible in the grid</summary>
		public bool IsDisplayedInGrid
		{
			get { Debug.Assert(!m_is_displayed_in_grid || Grid != null); return m_is_displayed_in_grid; }
		}

		/// <summary>True if this node has children</summary>
		public bool HasChildren
		{
			get { return m_nodes != null && Nodes.Count != 0; }
		}

		/// <summary>True if a '+' should be rendered next to this node even if it has no children. It's expected that children will be added in the NodeExpanding event</summary>
		public bool VirtualNodes
		{
			get { TreeGridView g; return m_virtual_nodes || ((g=Grid) != null && g.VirtualNodes); }
			set { m_virtual_nodes = value; }
		}

		/// <summary>Return true if this node is the first sibling</summary>
		public bool IsFirstSibling
		{
			get { return Parent != null && NodeIndex == 0; }
		}

		/// <summary>Return true if this node is the last sibling</summary>
		public bool IsLastSibling
		{
			get { return Parent != null && NodeIndex == Parent.Nodes.Count - 1; }
		}

		/// <summary>The level of this node in the tree</summary>
		public int Level
		{
			// Start at -1 because the root node is not shown in the grid
			get { int lvl = -1; for (TreeGridNode p = m_parent; p != null; p = p.m_parent, ++lvl) {} return lvl; }
		}

		/// <summary>The index of this node within the children of it's parent</summary>
		public int NodeIndex
		{
			get { return m_parent == null ? -1 : m_parent.Nodes.IndexOf(this); }
		}

		/// <summary>Gets the index of this row in the Grid</summary>
		public int RowIndex
		{
			get { return Index; }
		}
		private new int Index // hide the Index property of the row
		{
			get { return IsDisplayedInGrid ? base.Index : -1; }
		}

		/// <summary>Gets the index of the row after this node and its children (if visible in the grid).
		/// The number of rows used by this node is NextRowIndex - RowIndex</summary>
		private int NextRowIndex
		{
			get
			{
				if (!m_is_displayed_in_grid) return -1;
				if (m_is_expanded && Nodes.Count != 0)
				{
					Debug.Assert(Nodes.Last().IsDisplayedInGrid);
					return Nodes.Last().NextRowIndex;
				}
				return RowIndex + 1;
			}
		}

		/// <summary>Return the tree cell in this row</summary>
		public TreeGridCell TreeCell
		{
			get
			{
				TreeGridCell tree_cell = null;
				foreach (DataGridViewCell c in Cells) { if ((tree_cell = c as TreeGridCell) != null) break; } // Find the tree cell
				return tree_cell;
			}
		}

		/// <summary>The collection of child nodes for this node</summary>
		[Category("Data")]
		[Description("The collection of child nodes for this node")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public TreeGridNodeCollection Nodes
		{
			get { return m_nodes ?? (m_nodes = new TreeGridNodeCollection(this)); }
		}

		/// <summary>The Cells of this node/row</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new DataGridViewCellCollection Cells
		{
			get
			{
				if (!m_cells_created && DataGridView == null)
				{
					CreateCells(Grid);
					m_cells_created = true;
				}
				return base.Cells;
			}
		}

		/// <summary>Collapse the children of this node</summary>
		public bool Collapse()
		{
			if (!IsDisplayedInGrid) throw new Exception("Cannot collapse nodes that are not displayed in the grid");
			if (!m_is_expanded) return true;
			TreeGridView grid = Grid;
			if (!grid.BeginCollapseNode(this))
				return false;

			// Remove all of the children from the grid
			foreach (TreeGridNode child in Nodes)
				child.RemoveRowsFromGrid();

			m_is_expanded = false;
			grid.EndCollapseNode(this);
			return true;
		}

		/// <summary>Expand the children of this node</summary>
		public bool Expand()
		{
			if (!IsDisplayedInGrid) throw new Exception("Cannot expand nodes that are not displayed in the grid");
			if (m_is_expanded) return true;
			TreeGridView grid = Grid;
			if (!grid.BeginExpandNode(this))
				return false;

			// Add all children to the grid
			int row_index = RowIndex + 1;
			foreach (TreeGridNode child in Nodes)
				child.DisplayRowsInGrid(ref row_index);

			m_is_expanded = true;
			grid.EndExpandNode(this);
			return true;
		}

		/// <summary>Remove this node from the grid.</summary>
		internal void RemoveRowsFromGrid()
		{
			if (Grid == null) return;
			if (!IsDisplayedInGrid) return;

			// Remove all children from the grid
			foreach (TreeGridNode child in Nodes)
				child.RemoveRowsFromGrid();

			// Remove this node from the grid
			Grid.Rows.Remove(this);
			m_is_displayed_in_grid = false;
		}

		/// <summary>Add this node to the grid</summary>
		internal void DisplayRowsInGrid(ref int row_index)
		{
			if (Grid == null) return;
			if (IsDisplayedInGrid) return;

			// Insert this node as a row in the grid
			Grid.Rows.Insert(row_index++, this);
			m_is_displayed_in_grid = true;
			TreeCell.Dirty = true;

			// If this node is expanded add all of it's children
			if (m_is_expanded)
				foreach (TreeGridNode child in Nodes)
					child.DisplayRowsInGrid(ref row_index);
		}

		/// <summary>The index of the image to use in 'Grid.ImageList' or -1 if none</summary>
		[Category("Appearance")]
		[Description("The index of the image to use in 'Grid.ImageList' or -1 if none"), DefaultValue(-1)]
		[TypeConverter(typeof(ImageIndexConverter))]
		[Editor("System.Windows.Forms.Design.ImageIndexEditor", typeof(UITypeEditor))]
		public int ImageIndex
		{
			get { return m_image_index; }
			set { if (value != m_image_index) {m_image_index = value; if (IsDisplayedInGrid) Grid.InvalidateRow(base.Index);} }
		}

		/// <summary>The image to display for this node</summary>
		public Image Image
		{
			get
			{
				if (m_image != null) return m_image;
				TreeGridView g = Grid;
				return g != null && g.ImageList != null && (uint) m_image_index < g.ImageList.Images.Count ? g.ImageList.Images[m_image_index] : null;
			}
			set { if (value != m_image) {m_image = value; if (IsDisplayedInGrid) Grid.InvalidateRow(base.Index);} }
		}

		/// <summary>A useful string version of this node</summary>
		public override string ToString()
		{
			var sb = new StringBuilder(36);
			sb.Append("[").Append(Level).Append(",").Append(NodeIndex).Append("] ");
			if (TreeCell != null) sb.Append(TreeCell.Text);
			return sb.ToString();
		}

		/// <summary>Debugging helper for visualising the node tree</summary>
		public void DumpTreeC() { Console.Write(DumpTree()); }
		public string DumpTree() { var sb = new StringBuilder(); DumpTree(this, sb); return sb.ToString(); }
		private static void DumpTree(TreeGridNode node, StringBuilder sb)
		{
			for (int i = node.Level; i-- != 0;) sb.Append(' ');
			sb.Append(node).Append('\n');
			foreach (var n in node.Nodes) DumpTree(n, sb);
		}
	}

	/// <summary>A collection of nodes.
	/// The node collection controls the parenting of nodes since it is the
	/// only place that the child order is known. When a node is added to the
	/// collection it will be displayed in the grid if the parent node is also
	/// displayed and expanded.</summary>
	public class TreeGridNodeCollection :IList<TreeGridNode>, IList
	{
		private readonly TreeGridNode       m_owner; // The node that this collection belongs to
		private readonly List<TreeGridNode> m_list;  // The collection of child nodes

		public TreeGridNodeCollection(TreeGridNode owner)
		{
			m_owner = owner;
			m_list = new List<TreeGridNode>();
		}

		/// <summary>The node that this collection belongs to</summary>
		public TreeGridNode Owner
		{
			get { return m_owner; }
		}

		/// <summary>Add 'node' to the collection</summary>
		internal void AddInternal(TreeGridNode node)
		{
			Debug.Assert(node.Parent == m_owner);
			m_list.Add(node);
		}

		/// <summary>Add a node to this collection causing it to be parented to the node that owns this collection</summary>
		public TreeGridNode Add(TreeGridNode node)
		{
			node.Parent = m_owner;
			return node;
		}

		/// <summary>Add a node with values for the following grid columns</summary>
		public TreeGridNode Add(params object[] values)
		{
			return Insert(m_list.Count, values);
		}

		/// <summary>Insert a node into the collection at 'index'</summary>
		public TreeGridNode Insert(int index, TreeGridNode node)
		{
			node.Parent = m_owner;
			if (index != m_list.Count - 1)
			{
				m_list.RemoveAt(m_list.Count-1);
				m_list.Insert(index, node);
			}
			return node;
		}

		/// <summary>Add a node with values for the following grid columns</summary>
		public TreeGridNode Insert(int index, params object[] values)
		{
			Debug.Assert(m_owner.Grid != null, "Can only use this method after the owner node has been added to the tree grid");
			TreeGridNode node = Insert(index, new TreeGridNode(m_owner.Grid));
			int iend = Math.Min(values.Length, node.Cells.Count);
			for (int i = 0; i != iend; ++i) node.Cells[i].Value = values[i];
			return node;
		}

		/// <summary>Remove node from this collection</summary>
		internal void RemoveInternal(TreeGridNode node)
		{
			Debug.Assert(node.Parent == m_owner);
			m_list.Remove(node);
		}

		/// <summary>Remove 'node' from this collection</summary>
		public bool Remove(TreeGridNode node)
		{
			if (!m_list.Contains(node)) return false;
			node.Parent = null; // This will remove 'node' from our collection
			return true;
		}

		/// <summary>Remove the node at 'index' from the collection</summary>
		public void RemoveAt(int index)
		{
			m_list[index].Parent = null; // This will remove 'node' from our collection
		}

		/// <summary>Remove all nodes from the collection</summary>
		public void Clear()
		{
			for (int i = m_list.Count; i-- != 0;) m_list[i].Parent = null;
			Debug.Assert(m_list.Count == 0);
		}

		/// <summary>Return the index of a node in the collection</summary>
		public int IndexOf(TreeGridNode node)
		{
			return m_list.IndexOf(node);
		}

		/// <summary>The number of items in the collection</summary>
		public int Count
		{
			get{ return m_list.Count; }
		}

		/// <summary>Returns true if 'node' is in this collection</summary>
		public bool Contains(TreeGridNode node)
		{
			return m_list.Contains(node);
		}

		/// <summary>Always false, the list is not readonly</summary>
		public bool IsReadOnly
		{
			get{ return false; }
		}

		public TreeGridNode this[int index]
		{
			get { return m_list[index]; }
			set { RemoveAt(index); Insert(index, value); }
		}

		public void CopyTo(TreeGridNode[] array, int arrayIndex)
		{
			m_list.CopyTo(array, arrayIndex);
		}

		// Generic IList interface
		int  IList<TreeGridNode>.IndexOf(TreeGridNode item)           { return m_list.IndexOf(item); }
		void IList<TreeGridNode>.Insert(int index, TreeGridNode node) { Insert(index, node); }

		// IList interface
		void IList.Remove(object value)                               { Remove(value as TreeGridNode); }
		int  IList.Add(object value)                                  { var item = (TreeGridNode)value; Add(item); return item.NodeIndex; }
		void IList.Insert(int index, object node)                     { Insert(index, (TreeGridNode )node); }
		void IList.RemoveAt(int index)                                { RemoveAt(index); }
		void IList.Clear()                                            { Clear(); }
		bool IList.IsReadOnly                                         { get {return IsReadOnly;} }
		bool IList.IsFixedSize                                        { get {return false;} }
		int  IList.IndexOf(object item)                               { return IndexOf(item as TreeGridNode); }
		bool IList.Contains(object value)                             { return Contains(value as TreeGridNode); }
		object IList.this[int index]                                  { get {return this[index];} set {this[index] = value as TreeGridNode;} }

		// Enumerator/Enumerable methods
		public IEnumerator<TreeGridNode> GetEnumerator() { return m_list.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator()          { return GetEnumerator(); }

		// ICollection Members
		void ICollection<TreeGridNode>.Add(TreeGridNode node) { Add(node); }
		int  ICollection.Count                                { get {return Count;} }
		void ICollection.CopyTo(Array array, int index)       { CopyTo((TreeGridNode[])array, index); }
		bool ICollection.IsSynchronized                       { get {throw new Exception("The method or operation is not implemented.");} }
		object ICollection.SyncRoot                           { get {throw new Exception("The method or operation is not implemented.");} }
	}

	/// <summary>A cell within the tree column of a TreeGridView</summary>
	public class TreeGridCell :DataGridViewTextBoxCell
	{
		// ReSharper disable FieldCanBeMadeReadOnly.Global
		/// <summary>Data controlling the indenting behaviour of tree cells</summary>
		public class Indenting
		{
			/// <summary>The size (in px) of the glyph '+' or '-' plus line leading to the text or image</summary>
			public int m_glyph_width;

			/// <summary>The distance from the left cell edge to the glyph</summary>
			public int m_margin;

			/// <summary>The number of pixels to move in by with each child node</summary>
			public int m_width;

			public Indenting(int glyph_width = 15, int margin = 5, int width = 20)
			{
				m_glyph_width = glyph_width;
				m_margin      = margin;
				m_width       = width;
			}
		}
		public static Indenting DefaultIndenting = new Indenting();
		// ReSharper restore FieldCanBeMadeReadOnly.Global

		private Indenting m_indent = DefaultIndenting; // Indenting parameters
		private bool m_calc_padding;                   // Dirty flag for when the cell padding needs calculating

		public bool Dirty
		{
			set { m_calc_padding |= value; }
		}

		/// <summary>Duplicate this cell</summary>
		public override object Clone()
		{
			var c = (TreeGridCell)base.Clone();
			c.m_indent = DefaultIndenting;
			c.m_calc_padding = true;
			return c;
		}

		/// <summary>Get/Set the text for this cell</summary>
		public string Text
		{
			get { return TextFormatter(Value); }
			set { Value = value; }
		}

		/// <summary>A customisable method for converting 'Value' to a string</summary>
		// ReSharper disable FieldCanBeMadeReadOnly.Global
		public Func<object,string> TextFormatter = DefaultTextFormater;
		private static readonly Func<object,string> DefaultTextFormater = o => o.ToString();
		// ReSharper restore FieldCanBeMadeReadOnly.Global

		/// <summary>Get/Set the indenting parameters</summary>
		public Indenting IndentParams
		{
			get { return m_indent; }
			set { m_indent = value; }
		}

		/// <summary>The level in the tree of this node. Returns -1 if the cell does not belong to a node.</summary>
		public int Level
		{
			get { TreeGridNode node = OwningNode; return node != null ? node.Level : -1; }
		}

		/// <summary>Return the node/row that contains this cell</summary>
		public TreeGridNode OwningNode
		{
			get { return OwningRow; }
		}
		private new TreeGridNode OwningRow
		{
			get { return base.OwningRow as TreeGridNode; }
		}

		/// <summary>Returns true if (x,y) is on the '+' or '-' symbol for the tree node</summary>
		internal bool GlyphClick(int x, int y)
		{
			int l = m_indent.m_margin + m_indent.m_glyph_width + (Level-1) * m_indent.m_width;
			int r = l + m_indent.m_width;
			return x > l && x < r; // Should really test y as well...
		}

		/// <summary>Set the cell padding based on being at indent level 'level'</summary>
		private void SetCellPadding(Image image)
		{
			int imgw = (image != null ? image.Width  : 0);
			Style.Padding = new Padding(m_indent.m_margin + m_indent.m_glyph_width + (Level * m_indent.m_width) + imgw + 2, 0, 0, 0);
			m_calc_padding = false;
		}

		/// <summary>Paint the tree column cell</summary>
		protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates cellState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
		{
			TreeGridNode node = OwningNode;
			if (node == null) return;

			// Update the cell padding if required
			if (m_calc_padding)
			{
				SetCellPadding(node.Image);
				cellStyle.Padding = Style.Padding;
			}

			// Paint the cell normally
			base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);

			// A helper rectangle in which the lines, glyphs, and images are drawn
			var rect = new Rectangle(cellBounds.X + m_indent.m_margin + Level * m_indent.m_width, cellBounds.Y, m_indent.m_glyph_width, cellBounds.Height - 1);

			// Paint the associated image
			if (node.Image != null)
			{
				var pt = new Point(rect.X + m_indent.m_glyph_width, 2 + cellBounds.Y + (cellBounds.Height - node.Image.Height)/2);
				GraphicsContainer gc = graphics.BeginContainer();
				graphics.SetClip(cellBounds);
				graphics.DrawImageUnscaled(node.Image, pt);
				graphics.EndContainer(gc);
			}

			// Paint tree lines
			if (node.Grid.ShowLines)
			{
				using (var line_pen = new Pen(SystemBrushes.ControlDark, 1.0f))
				{
					line_pen.DashStyle = DashStyle.Dot;
					bool is_last_sibling  = node.IsLastSibling;
					bool is_first_sibling = node.IsFirstSibling;
					const int ghw = 4;

					// Vertical line
					int top = cellBounds.Top    + (is_first_sibling && Level == 0 ? cellBounds.Height/2 : 0);
					int bot = cellBounds.Bottom - (is_last_sibling  ? cellBounds.Height/2 : 0);
					graphics.DrawLine(line_pen, rect.X + ghw, top, rect.X + ghw, bot);

					// Horizontal line
					top = cellBounds.Top + cellBounds.Height/2;
					graphics.DrawLine(line_pen, rect.X + ghw, top, rect.Right, top);

					// Paint lines of previous levels to the root
					int x = (rect.X + ghw) - m_indent.m_width;
					for (TreeGridNode p = node.Parent; !p.IsRoot; p = p.Parent, x -= m_indent.m_width)
					{
						if (p.HasChildren && !p.IsLastSibling) // paint vertical line
							graphics.DrawLine(line_pen, x, cellBounds.Top, x, cellBounds.Bottom);
					}
				}
			}

			// Paint node glyphs
			if (node.HasChildren || node.VirtualNodes)
			{
				var glyph = new Rectangle(rect.X, rect.Y + (rect.Height / 2) - 4, 10, 10);
				int midx = glyph.Left + glyph.Width / 2, midy = glyph.Top + glyph.Height / 2;
				graphics.FillRectangle(SystemBrushes.ControlLight, glyph);
				graphics.DrawRectangle(SystemPens.ControlDarkDark, glyph);
				graphics.DrawLine(SystemPens.ControlDarkDark, new Point(glyph.Left + 2, midy), new Point(glyph.Right - 2, midy));
				if (!node.IsExpanded) // make it a '+' if not expanded
					graphics.DrawLine(SystemPens.ControlDarkDark, new Point(midx, glyph.Top + 2), new Point(midx, glyph.Bottom - 2));
			}
		}
	}

	/// <summary>Column type for the tree column</summary>
	public sealed class TreeGridColumn : DataGridViewTextBoxColumn
	{
		private Image m_default_node_image;

		public TreeGridColumn()
		{
			CellTemplate = new TreeGridCell();
		}
		public Image DefaultNodeImage
		{
			get { return m_default_node_image; }
			set { m_default_node_image = value; }
		}
		public override object Clone() // Need to override Clone for design-time support.
		{
			var c = (TreeGridColumn)base.Clone();
			if (c != null) { c.m_default_node_image = m_default_node_image; }
			return c;
		}
	}

	/// <summary>Arguments for node collapsing events</summary>
	public class CollapsingEventArgs :CancelEventArgs
	{
		private readonly TreeGridNode m_node;
		public CollapsingEventArgs(TreeGridNode node) { m_node = node; }
		public TreeGridNode Node                      { get {return m_node;} }
	}

	/// <summary>Arguments for node collapsed events</summary>
	public class CollapsedEventArgs :EventArgs
	{
		private readonly TreeGridNode m_node;
		public CollapsedEventArgs(TreeGridNode node) { m_node = node; }
		public TreeGridNode Node                     { get {return m_node;} }
	}

	/// <summary>Arguments for node expanding events</summary>
	public class ExpandingEventArgs :CancelEventArgs
	{
		private readonly TreeGridNode m_node;
		public ExpandingEventArgs(TreeGridNode node) { m_node = node; }
		public TreeGridNode Node                     { get {return m_node;} }
	}

	/// <summary>Arguments for node expanded events</summary>
	public class ExpandedEventArgs :EventArgs
	{
		private readonly TreeGridNode m_node;
		public ExpandedEventArgs(TreeGridNode node) { m_node = node; }
		public TreeGridNode Node                    { get {return m_node;} }
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using gui;

	[TestFixture] internal static partial class UnitTests
	{
		internal static class TestTreeGridView
		{
			[Test] public static void TestTGV1()
			{
				var grid0 = new TreeGridView();
				grid0.Columns.Add(new TreeGridColumn{HeaderText="col0"});
				grid0.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="col1"});
				grid0.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="col2"});

				var a0 = grid0.Nodes.Add("a0");
				TreeGridNode a0_0 = a0.Nodes.Add("a0_0");
				TreeGridNode a0_1 = a0.Nodes.Add("a0_1");
				TreeGridNode a0_0_0 = a0_0.Nodes.Add("a0_0_0");
				TreeGridNode a0_0_1 = a0_0.Nodes.Add("a0_0_1");
				TreeGridNode a0_0_2 = a0_0.Nodes.Add("a0_0_2");

				Assert.True(grid0.Nodes.Count == 1);
				Assert.True(a0.Nodes.Count == 2);
				Assert.True(a0_0.Nodes.Count == 3);
				Assert.True(a0_1.Nodes.Count == 0);

				// Moving branches around in the tree
				a0_0_0.Parent = a0_1;
				a0_0_1.Parent = a0_1;
				a0_0_2.Parent = a0_1;
				Assert.True(grid0.Nodes.Count == 1);
				Assert.True(a0.Nodes.Count == 2);
				Assert.True(a0_0.Nodes.Count == 0);
				Assert.True(a0_1.Nodes.Count == 3);

				// Detaching branches
				a0_1.Parent = null;

				Assert.True(grid0.Nodes.Count == 1);
				Assert.True(a0.Nodes.Count == 1);
				Assert.True(a0_0.Nodes.Count == 0);
				Assert.True(a0_1.Nodes.Count == 3);

				// Circular connections disallowed
				Assert.Throws(typeof(InvalidOperationException), ()=>{a0_1.Parent = a0_0_0;});
			}

			[Test] public static void TestTGV2()
			{
				var grid0 = new TreeGridView();
				grid0.Columns.Add(new TreeGridColumn{HeaderText="col0"});
				grid0.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="col1"});
				grid0.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="col2"});
				var grid1 = new TreeGridView();
				grid1.Columns.Add(new TreeGridColumn{HeaderText="col0"});

				TreeGridNode a0 = grid0.Nodes.Add("a0", 0, 0);
				a0.Nodes.Add("a0_0", 0, 0);
				a0.Nodes.Add("a0_1", 0, 0);

				// Can't move nodes between grids
				Assert.Throws(typeof(InvalidOperationException), ()=>{grid1.Nodes.Add(a0);});

			//    grid0.Nodes.Add(a0);
			//    grid0.Nodes.Insert(1, b0);

			//    Assert.True(grid0.Nodes.Count == 2 && grid1.Nodes.Count == 0);
			//    Assert.AreSame(a0, grid0.Nodes[0]);
			//    Assert.AreSame(b0, grid0.Nodes[1]);
			//    Assert.AreSame(grid0, a0.Grid);
			//    Assert.AreSame(grid0, b0.Grid);

			//    grid0.Nodes.Remove(a0);

			//    Assert.True(grid0.Nodes.Count == 1 && grid1.Nodes.Count == 0);
			//    Assert.AreSame(b0, grid0.Nodes[0]);
			//    Assert.AreSame(null, a0.Grid);
			//    Assert.AreSame(grid0, b0.Grid);

			//    grid1.Nodes.Add(a0);

			//    Assert.True(grid0.Nodes.Count == 1 && grid1.Nodes.Count == 1);
			//    Assert.AreSame(a0, grid1.Nodes[0]);
			//    Assert.AreSame(b0, grid0.Nodes[0]);
			//    Assert.AreSame(grid1, a0.Grid);
			//    Assert.AreSame(grid0, b0.Grid);

			//    grid1.Nodes.Add(b0);

			//    Assert.True(grid0.Nodes.Count == 0 && grid1.Nodes.Count == 2);
			//    Assert.AreSame(a0, grid1.Nodes[0]);
			//    Assert.AreSame(b0, grid1.Nodes[1]);
			//    Assert.AreSame(grid1, a0.Grid);
			//    Assert.AreSame(grid1, b0.Grid);

			//    grid0.Nodes.Add(a0);
			//    grid0.Nodes.Add(b0);

			//    Assert.True(grid0.Nodes.Count == 2 && grid1.Nodes.Count == 0);
			//    Assert.AreSame(a0, grid0.Nodes[0]);
			//    Assert.AreSame(b0, grid0.Nodes[1]);
			//    Assert.AreSame(grid0, a0.Grid);
			//    Assert.AreSame(grid0, b0.Grid);

			//    grid0.Nodes.Remove(a0);
			//    grid0.Nodes.Remove(b0);

			//    Assert.True(grid0.Nodes.Count == 0 && grid1.Nodes.Count == 0);
			//    Assert.AreSame(null, a0.Grid);
			//    Assert.AreSame(null, b0.Grid);
			}
		}
	}
}
#endif
