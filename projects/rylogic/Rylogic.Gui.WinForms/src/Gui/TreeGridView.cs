//*****************************************************************************************
// Tree Grid View
// Copyright (c) Rylogic Ltd 2011
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
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WinForms
{
	[ComplexBindingProperties]
	[Docking(DockingBehavior.Ask)]
	public class TreeGridView :DataGridView
	{
		// Notes:
		//  - DataBinding works if the data type implements 'ITreeItem' or a 'DataBinder' method is
		//    provided that converts DataSource objects to 'ITreeItem'.
		//  - 'DataSource' does not use 'base.DataSource' because the DataGridView internals are set
		//    up for one row per item in the data source. In this tree the DataSource is used as the
		//    source of the top level items. The 'ITreeItem' interface is used to access child nodes.
		//  - The 'DataBinder' method is never null, but may return null if the data object does not
		//    implement 'ITreeItem'.

		public TreeGridView()
		{
			// Control when edit occurs because edit mode shouldn't start when expanding/collapsing
			//EditMode = DataGridViewEditMode.EditProgrammatically;

			// No support for adding or deleting rows by the user.
			AllowUserToAddRows = false;
			AllowUserToDeleteRows = false;
			ShowLines = true;
			DataBinder = null;
			DataProperty = new Dictionary<string, PropertyInfo>();
			RowTemplate = new TreeGridNode(this);

			// Create the root of the tree
			RootNode = new TreeGridNode(this);
			RootNode.Expand();

			// Cause all rows to be unshared
			base.Rows.CollectionChanged += (s,e) => {};
		}
		protected override void Dispose(bool disposing)
		{
			DataSource = null;
			base.Dispose(disposing);
		}
		protected override void OnDataSourceChanged(EventArgs e)
		{
			// We're not using 'base.DataSource' because it expects one row per item
			throw new Exception($"Data binding only works when accessed via {nameof(TreeGridView)}.{nameof(DataSource)}");
		}
		protected override void OnColumnDataPropertyNameChanged(DataGridViewColumnEventArgs e)
		{
			// Invalidate the cached data property accessors in the tree column
			DataProperty.Clear();
			base.OnColumnDataPropertyNameChanged(e);
		}
		protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
		{
			base.OnCellValueNeeded(e);
			if (!this.Within(e.ColumnIndex, e.RowIndex, out DataGridViewColumn col))
				return;

			// If the tree is data bound, read the value from the data source
			if (m_data_source != null && !IsDataSourceChanging)
			{
				// Get the data bound object
				var node = (TreeGridNode)Rows[e.RowIndex];
				var item = node.DataBoundItem;

				// If a data property name has been set, access the property value
				if (col.DataPropertyName != null)
				{
					var pi = DataProperty.GetOrAdd(col.DataPropertyName, name => item.GetType().GetProperty(name));
					if (pi != null)
						e.Value = pi.GetValue(item);
				}
			}
		}
		protected override void OnCellValuePushed(DataGridViewCellValueEventArgs e)
		{
			base.OnCellValuePushed(e);
			if (!this.Within(e.ColumnIndex, e.RowIndex, out DataGridViewColumn col))
				return;

			// If the tree is data bound, write the value to the data source
			if (m_data_source != null && !IsDataSourceChanging)
			{
				// Don't modify read only collections
				if (!m_data_source.IsReadOnly)
				{
					// Invalidate the row to be changed
					InvalidateRow(e.RowIndex);
				
					// If a data property name has been set, access the property
					if (col.DataPropertyName != null)
					{
						// Get the data bound object
						var item = ((TreeGridNode)Rows[e.RowIndex]).DataBoundItem;

						var pi = DataProperty.GetOrAdd(col.DataPropertyName, name => item.GetType().GetProperty(name));
						if (pi != null)
							pi.SetValue(item, e.Value);
					}
				}
			}
		}
		protected override void OnCurrentCellChanged(EventArgs e)
		{
			if (!InSetCurrentCell && DataSource is ICurrencyManagerProvider src)
			{
				// Find the top level item for the current row
				if (CurrentCell == null)
					src.CurrencyManager.Position = -1;
				else
					src.CurrencyManager.Position = ((TreeGridNode)CurrentCell.OwningRow).TopLevelNode.RowIndex;
			}
			base.OnCurrentCellChanged(e);
		}
		public void InvalidateNode(int node_index, bool recursive)
		{
			if (node_index.Within(0, Nodes.Count))
				Nodes[node_index].Invalidate(recursive);
		}

		/// <summary>If items do not implement 'ITreeItem', use this function to wrap the contained objects</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Func<object, ITreeItem> DataBinder
		{
			get { return m_data_binder; }
			set { m_data_binder = value ?? (x => x as ITreeItem); }
		}
		private Func<object, ITreeItem> m_data_binder;

		/// <summary>Data binding source.</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new object DataSource
		{
			[DebuggerStepThrough] get { return m_data_source; }
			set
			{
				if (m_data_source == value) return;
				using (DataSourceChanging())
				{
					// Unhook
					if (m_data_source is IBindingList lc0)
						lc0.ListChanged -= HandleDataSourceChanged;
					if (m_data_source is ICurrencyManagerProvider cm0)
						cm0.CurrencyManager.PositionChanged -= HandlePositionChanged;

					// Disable virtual mode until the data source and the row count are consistent
					base.VirtualMode = false;
					CurrentCell = null;

					// Change the data source
					m_data_source =
						(value is IBindingList bl) ? bl :
						(value != null) ? new BindingSource{ DataSource = value } :
						null;

					// Hookup
					if (m_data_source is IBindingList lc1)
						lc1.ListChanged += HandleDataSourceChanged;
					if (m_data_source is ICurrencyManagerProvider cm1)
						cm1.CurrencyManager.PositionChanged += HandlePositionChanged;

					// Use virtual mode so that cell values can be provided by overridden methods
					base.VirtualMode = m_data_source != null;
				}
			}
		}
		internal IBindingList m_data_source;

		/// <summary>Flag when the data source is out of sync with the grid</summary>
		private Scope DataSourceChanging()
		{
			return Scope.Create(
				() =>
				{
					++m_data_source_changing;
				},
				() =>
				{
					--m_data_source_changing;
					if (m_data_source_changing == 0)
						SyncToDataSource();
				});
		}
		internal bool IsDataSourceChanging
		{
			get { return m_data_source_changing != 0; }
		}
		private int m_data_source_changing;

		/// <summary>The collection of nodes in the grid</summary>
		[Category("Data")]
		[Description("The collection of nodes in the grid.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public TreeGridNodeCollection Nodes
		{
			[DebuggerStepThrough] get { return RootNode.Nodes; }
		}

		/// <summary>Get/Set the number of top level nodes in the grid </summary>
		public int NodeCount
		{
			get { return Nodes.Count; }
			set
			{
				// Don't let clients set the row count when data binding is in use
				if (DataSource != null)
					throw new Exception("Do not set the node count when data binding is used");

				if (value == NodeCount) return;
				Nodes.Count = value;
			}
		}

		/// <summary>Get the number of rows in the grid</summary>
		public new int RowCount
		{
			get { return base.RowCount; }
			private set
			{
				// The number of rows in the grid should only be changed by the setter on 'GridTreeNode.Parent'
				throw new Exception("Do not set RowCount explicitly. RowCount is determined from the NodeCount and the expanded nodes");
			}
		}

		/// <summary>The grid current node</summary>
		public TreeGridNode CurrentNode
		{
			[DebuggerStepThrough] get { return (TreeGridNode)base.CurrentRow; }
		}

		/// <summary>The internal root of the tree. Note: not visible in the tree</summary>
		public TreeGridNode RootNode { [DebuggerStepThrough] get; private set; }

		/// <summary>True if data binding is in use</summary>
		public bool IsDataBound
		{
			get { return DataSource != null; }
		}

		/// <summary>Show/hide the tree lines</summary>
		public bool ShowLines
		{
			[DebuggerStepThrough] get { return m_show_lines; }
			set
			{
				if (m_show_lines == value) return;
				m_show_lines = value;
				Invalidate();
			}
		}
		private bool m_show_lines;

		/// <summary>"Causes nodes to always show as expandable. Use the NodeExpanding event to add nodes</summary>
		public bool VirtualNodes
		{
			[DebuggerStepThrough] get { return m_virtual_nodes; }
			set
			{
				if (m_virtual_nodes == value) return;
				m_virtual_nodes = value;
				Invalidate();
			}
		}
		private bool m_virtual_nodes;

		/// <summary>Get/Set the associated image list</summary>
		public ImageList ImageList
		{
			get { return m_image_list; }
			set
			{
				if (m_image_list == value) return;
				Util.Dispose(ref m_image_list);
				m_image_list = value;
				Invalidate();
			}
		}
		private ImageList m_image_list;

		/// <summary>Recursively expand all nodes in the tree</summary>
		public void ExpandAll()
		{
			foreach (var node in Nodes)
				node.Expand(true);
		}

		/// <summary>Recursively collapse all nodes in the tree</summary>
		public void CollapseAll()
		{
			foreach (var node in Nodes)
				node.Collapse(true);
		}

		/// <summary>Called just before a node is expanded</summary>
		public event EventHandler<ExpandingEventArgs> NodeExpanding;

		/// <summary>Called after a node is expanded</summary>
		public event EventHandler<ExpandedEventArgs>  NodeExpanded;

		/// <summary>Called just before a node is collapsed</summary>
		public event EventHandler<CollapsingEventArgs> NodeCollapsing;

		/// <summary>Called after a node is collapsed</summary>
		public event EventHandler<CollapsedEventArgs>  NodeCollapsed;

		/// <summary>Prepare the grid for a node collapse.</summary>
		internal bool BeginCollapseNode(TreeGridNode node)
		{
			// Raise the collapsing event to allow cancel
			var args = new CollapsingEventArgs(node);
			OnNodeCollapsing(args);
			if (args.Cancel)
				return false;

			return true;
		}

		/// <summary>Complete changes to the grid after a node collapse.</summary>
		internal void EndCollapseNode(TreeGridNode node)
		{
			OnNodeCollapsed(new CollapsedEventArgs(node));

			// If data binding is used, remove the child nodes of 'node'
			if (DataSource != null)
				node.Nodes.Count = 0;
		}

		/// <summary>Prepare the grid for a node expansion</summary>
		internal bool BeginExpandNode(TreeGridNode node)
		{
			// Allow expand to be cancelled
			var exp = new ExpandingEventArgs(node);
			OnNodeExpanding(exp);
			if (exp.Cancel)
				return false;

			// If data binding is used, populate the child nodes of 'node' from the data source
			if (DataSource != null)
			{
				var tree_item = node.DataBoundTreeItem;
				if (tree_item != null)
					node.Nodes.Count = tree_item.ChildCount;
			}

			return true;
		}

		/// <summary>Compete changes to the grid after a node expand</summary>
		internal void EndExpandNode(TreeGridNode node)
		{
			OnNodeExpanded(new ExpandedEventArgs(node));
		}

		/// <summary>Sort the columns of the grid</summary>
		public override void Sort(DataGridViewColumn col, ListSortDirection direction)
		{
			EndEdit();
			using (this.SuspendLayout(true))
			{
				var rows = new List<TreeGridNode>();
				int col_idx = col.Index;
				int sign = col.HeaderCell.SortGlyphDirection == SortOrder.Ascending ? -1 : 1;
				foreach (DataGridViewColumn c in Columns) c.HeaderCell.SortGlyphDirection = SortOrder.None;
				col.HeaderCell.SortGlyphDirection = sign > 0 ? SortOrder.Ascending : SortOrder.Descending;

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

				// Sort within node levels
				sort(RootNode);
			}
		}

		/// <summary>Handle cell editing via F2</summary>
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
			if (CurrentCell is TreeGridCell tcell)
			{
				TreeGridNode node = tcell.OwningNode;
				if (node != null)
				{
					// Expand/Collapse using the space bar
					if (e.KeyCode == Keys.Space && node.HasChildren)
					{
						if (!node.IsExpanded) node.Expand();
						else node.Collapse();
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
				if (this[info.ColumnIndex, info.RowIndex] is TreeGridCell tcell &&
					tcell.GlyphClick(e.X - info.ColumnX, e.Y - info.RowY))
				{
					var node = tcell.OwningNode;
					if (node != null && (node.HasChildren || node.VirtualNodes))
					{
						if (node.IsExpanded) node.Collapse();
						else node.Expand();
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

		/// <summary>No support for virtual mode</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new bool VirtualMode
		{
			get { return false; }
			private set { throw new NotSupportedException("The TreeGridView does not support virtual mode"); }
		}

		/// <summary>None of the rows/nodes created use the row template, so it is hidden.</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new TreeGridNode RowTemplate
		{
			get { return (TreeGridNode)base.RowTemplate; }
			set { base.RowTemplate = value; }
		}

		/// <summary>A cache of Property Info for by property name</summary>
		private Dictionary<string, PropertyInfo> DataProperty { get; set; }

		/// <summary>
		/// Return the node in the tree that corresponds to 'item' or a parent of 'item'.
		/// If 'displayed' is true only nodes currently visible in the tree are considered.
		/// If false, collapsed nodes are also considered.</summary>
		public TreeGridNode FindNodeForItem(object item, bool displayed)
		{
			// Get the TreeItem for 'item'.
			var tree_item = TreeItem(item);
			if (tree_item == null)
			{
				// If there is no tree item, we have to search recursively through all nodes
				var stack = new Stack<TreeGridNodeCollection>(new[]{Nodes});
				for (;stack.Count != 0;)
				{
					var nodes = stack.Pop();
					foreach (var node in nodes)
					{
						// Found it
						if (Equals(node.DataBoundItem, item))
							return node;

						// Add child node collections to the stack
						if (node.HasChildren && (node.IsExpanded || !displayed))
							stack.Push(node.Nodes);
					}
				}

				// Not found
				return null;
			}

			// If 'item' has no parent then there should be a node in 'Nodes' that corresponds to it.
			if (tree_item.Parent == null)
			{
				return Nodes.FirstOrDefault(x => Equals(x.DataBoundItem, item));
			}
			// Otherwise, if 'item' has a parent, find the node for the parent then search it's nodes for 'item'
			else
			{
				var parent = FindNodeForItem(tree_item.Parent, displayed);
				return parent.Nodes.FirstOrDefault(x => Equals(x.DataBoundItem, item));
			}
		}

		/// <summary>Return the tree item representation of 'item' (if possible, otherwise null)</summary>
		internal ITreeItem TreeItem(object item)
		{
			if (item is ITreeItem ti) return ti;
			if (item != null) return DataBinder(item);
			return null;
		}

		/// <summary>Handle notification that the data source has changed</summary>
		private void HandleDataSourceChanged(object sender, ListChangedEventArgs e)
		{
			// When the data source changes we need to update the node collections.
			// At this point, the data source has already changed but the grid still has rows
			// representing the previous state.

			// Flag the grid as out of sync with the data source
			using (DataSourceChanging())
			{
				// Apply the changes to the nodes
				switch (e.ListChangedType)
				{
				case ListChangedType.Reset:
					{
						Nodes.Count = m_data_source.Count;
						break;
					}
				case ListChangedType.ItemAdded:
					{
						Nodes.Insert(e.NewIndex, new TreeGridNode(this));
						break;
					}
				case ListChangedType.ItemDeleted:
					{
						Nodes.RemoveAt(e.NewIndex);
						break;
					}
				case ListChangedType.ItemMoved:
					{
						var node = Nodes[e.OldIndex];
						Nodes.RemoveAt(e.OldIndex);
						Nodes.Insert(e.NewIndex + (e.OldIndex < e.NewIndex ? 1 : 0), node);
						break;
					}
				}

				// Invalidate affected rows
				if (e.NewIndex.Within(0, Nodes.Count))
				{
					var range = Nodes[e.NewIndex].RowIndexRange;
					foreach (var r in range)
						InvalidateRow((int)r);
				}
				if (e.OldIndex != e.NewIndex && e.OldIndex.Within(0, Nodes.Count))
				{
					var range = Nodes[e.OldIndex].RowIndexRange;
					foreach (var r in range)
						InvalidateRow((int)r);
				}
			}
		}

		/// <summary>Handle notification that the current position in the data source has changed</summary>
		private void HandlePositionChanged(object sender, EventArgs e)
		{
			// PositionChanged only occurs if the data source is 'ICurrencyManagerProvider'.
			// When the position changes, we want to update the current cell, but only if
			// the current cell is not a child of the current position.
			if (ColumnCount == 0)
				return;

			// Get the top level node that corresponds to the new current position in the data source
			var cm = ((ICurrencyManagerProvider)m_data_source).CurrencyManager;
			if (cm.Position == -1 || Nodes.Count == 0)
			{
				CurrentCell = null;
			}
			else
			{
				// If the current row is not a child of 'node' change the current row
				var node = Nodes[cm.Position];
				if (CurrentNode?.TopLevelNode != node)
				{
					var col_index = Math.Max(0, CurrentCellAddress.X);
					CurrentCell = this[col_index, node.RowIndex];
				}
			}
		}

		/// <summary>Update the Nodes collection to match the current state of the data source</summary>
		private void SyncToDataSource()
		{
			if (DataSource == null)
				return;

			// Ensure the child nodes are in-sync with the children of 'item'
			Nodes.Count = m_data_source.Count;
			foreach (var node in Nodes)
				node.SyncToDataSource();
		}
	}

	/// <summary>A node/row in the tree grid view control</summary>
	[ToolboxItem(false)]
	[DesignTimeVisible(false)]
	[DebuggerDisplay("Level={Level} Index={NodeIndex}")]
	public class TreeGridNode :DataGridViewRow
	{
		/// <summary>Construct the node</summary>
		internal TreeGridNode(TreeGridView grid)
		{
			Nodes = new TreeGridNodeCollection(grid, this);

			Grid = grid;
			Parent = null;
			ImageIndex = -1;
			VirtualNodes  = false;
		}
		internal TreeGridNode(TreeGridView grid, params object[] values)
		{
			Nodes = new TreeGridNodeCollection(grid, this);

			// Initialise the cells in the row
			if (values.Length < grid.ColumnCount)
				values.Resize(grid.ColumnCount, i => string.Empty);

			CreateCells(grid, values);

			Grid = grid;
			Parent = null;
			ImageIndex = -1;
			VirtualNodes  = false;
		}
		public void Invalidate(bool recursive)
		{
			if (!IsInGrid)
				return;

			Grid.InvalidateRow(RowIndex);
			if (recursive && IsExpanded)
			{
				foreach (var child in Nodes)
					child.Invalidate(recursive);
			}
		}

		/// <summary>A clone method must be provided for types derived from DataGridViewRow</summary>
		public override object Clone()
		{
			var r = new TreeGridNode(Grid);
			r.Grid       = Grid;
			r.Parent     = null;
			r.Image      = Image;
			r.ImageIndex = ImageIndex;
			r.IsExpanded = IsExpanded;
			r.IsInGrid   = false;
			return r;
		}

		/// <summary>True if data binding is in use</summary>
		public bool IsDataBound
		{
			get { return Grid?.IsDataBound ?? false; }
		}

		/// <summary>An item that this row is bound to</summary>
		public new object DataBoundItem
		{
			get
			{
				// Nothing is ever bound to the root item. It's not logical to access it's DataBoundItem property
				if (IsRoot)
					throw new Exception("The root node is never data bound.");

				// If a data source has been set, read the object from the data source
				if (Grid.DataSource is IList src)
				{
					// If this is a top level node, read from the DataSource (end recursion)
					if (IsTopLevel)
					{
						if (!NodeIndex.Within(0, src.Count))
							throw new Exception($"Top level tree item (index={NodeIndex}) is not within the DataSource");

						return src[NodeIndex];
					}

					// Otherwise get the parent object from the data source and return the child associated with this node
					var parent_tree_item = Parent.DataBoundTreeItem;
					if (parent_tree_item != null)
						return parent_tree_item.Child(NodeIndex);

					// Otherwise, a DataSource has been set, but we can't access the child nodes of the DataSource items.
					throw new Exception($"Data source items do not implement {nameof(ITreeItem)}. Child tree nodes cannot access their DataBoundItem");
				}

				// Otherwise return 'm_item', assuming this node was explicitly bound (using the Bind() method).
				return m_item;
			}
			internal set { m_item = value; }
		}
		internal ITreeItem DataBoundTreeItem
		{
			get { return Grid.TreeItem(DataBoundItem); }
		}
		private object m_item;

		/// <summary>Get/Set the grid that this node belongs to. Only root nodes can be assigned to a grid</summary>
		public TreeGridView Grid
		{
			[DebuggerStepThrough] get { return m_impl_grid; }
			private set
			{
				// Setter is private because a node should never be moved to a different grid
				// Each node has cells created based on the number of columns in the grid
				// and those cells hold the data for the node. Moving a node between grids
				// would mean storing that data temporarily while the node has no grid
				// which is just messy.
				if (m_impl_grid == value) return;
				if (m_impl_grid != null)
				{
					m_impl_grid.Rows.CollectionChanged -= HandleRowCollectionChanged;
				}
				m_impl_grid = value;
				if (m_impl_grid != null)
				{
					m_impl_grid.Rows.CollectionChanged += HandleRowCollectionChanged;
				}
			}
		}
		public new TreeGridView DataGridView
		{
			[DebuggerStepThrough] get { return Grid; }
		}
		private TreeGridView m_impl_grid;
		private void HandleRowCollectionChanged(object sender, CollectionChangeEventArgs e)
		{
			switch (e.Action)
			{
			case CollectionChangeAction.Add:
				if (e.Element == this)
					IsInGrid = true;
				break;
			case CollectionChangeAction.Remove:
				if (e.Element == this)
					IsInGrid = false;
				break;
			case CollectionChangeAction.Refresh:
				IsInGrid = Grid.Rows.Contains(this);
				break;
			}
		}

		/// <summary>
		/// Get/Set the parent node for this node.
		/// Note: The tree grid contains an internal root node.
		/// Root level tree items will return this internal node.
		/// You can use node.Parent.IsRoot or node.Level == 0 to find root level tree items</summary>
		public TreeGridNode Parent
		{
			[DebuggerStepThrough] get { return m_impl_parent; }
			set
			{
				if (m_impl_parent == value) return;

				// Check parented to the correct grid
				if (value != null && Grid != value.Grid)
					throw new InvalidOperationException("Assigned parent must be within the same grid");

				// Check for circular connections
				for (var p = value; p != null; p = p.Parent)
					if (p == this) throw new InvalidOperationException("Assigned parent is a child of this node");

				// Remove the rows of this node from the grid
				using (Grid.SuspendLayout(true))
					RemoveRowsFromGrid();

				// Remove this node from its current parent
				if (m_impl_parent != null)
				{
					m_impl_parent.Nodes.RemoveInternal(this);

					// Refresh the glyph next to the parent node
					if (m_impl_parent.IsInGrid && m_impl_parent.Nodes.Count == 0 && !m_impl_parent.IsRoot)
						Grid.InvalidateRow(m_impl_parent.RowIndex);
				}

				// Assign the new parent
				m_impl_parent = value;

				if (m_impl_parent != null)
				{
					int row_index = m_impl_parent.NextRowIndex;

					// Add to the parents node collection
					m_impl_parent.Nodes.AddInternal(this);

					// Refresh the glyph next to the parent node
					if (!m_impl_parent.IsRoot && m_impl_parent.IsInGrid && m_impl_parent.GlyphVisible)
						Grid.InvalidateRow(m_impl_parent.RowIndex);

					// If this node should be visible in the grid then display it
					if (m_impl_parent.IsInGrid && m_impl_parent.IsExpanded)
					{
						using (Grid.SuspendLayout(true))
							DisplayRowsInGrid(ref row_index);
					}
				}
			}
		}
		private TreeGridNode m_impl_parent;
		
		/// <summary>
		/// Returns the top most ancestor for this node (possibly itself if this is a root node)
		/// Note: The tree grid contains an internal root node which items added to the tree
		/// are parented to.
		/// You can use node.Parent.IsRoot or node.Level == 0 to find root level tree items</summary>
		public TreeGridNode RootNode
		{
			get
			{
				var n = this;
				for (; n.Parent != null; n = n.Parent) {}
				return n;
			}
		}

		/// <summary>Return the top-most parent of this node below the root node</summary>
		public TreeGridNode TopLevelNode
		{
			get
			{
				if (IsRoot)
					throw new Exception("The root node does not have a top level node. It's children are the top level nodes");

				var n = this;
				for (var root = RootNode; n.Parent != null && n.Parent != root; n = n.Parent) {}
				return n;
			}
		}

		/// <summary>The image to display for this node</summary>
		public Image Image
		{
			get
			{
				if (m_impl_image != null) return m_impl_image;
				var imgs = Grid?.ImageList?.Images;
				return imgs != null && imgs.Within(ImageIndex) ? imgs[ImageIndex] : null;
			}
			set
			{
				if (m_impl_image == value) return;
				m_impl_image = value;
				if (IsInGrid && !IsRoot)
				{
					Grid.InvalidateRow(base.Index);
				}
			}
		}
		private Image m_impl_image;

		/// <summary>The index of the image to use in 'Grid.ImageList' or -1 if none. 'Image' has higher precedence</summary>
		[Category("Appearance")]
		[Description("The index of the image to use in 'Grid.ImageList' or -1 if none"), DefaultValue(-1)]
		[TypeConverter(typeof(ImageIndexConverter))]
		[Editor("System.Windows.Forms.Design.ImageIndexEditor", typeof(UITypeEditor))]
		public int ImageIndex
		{
			get { return m_impl_image_index; }
			set
			{
				if (m_impl_image_index == value) return;
				m_impl_image_index = value;
				if (IsInGrid && !IsRoot)
				{
					Grid.InvalidateRow(base.Index);
				}
			}
		}
		private int m_impl_image_index;

		/// <summary>
		/// True if this node has no parent.
		/// Note: The tree grid contains an internal root node, so this
		/// will be false for what appear to be root level items in the tree.
		/// You can use node.Parent.IsRoot or node.Level == 0 to find root level tree items</summary>
		public bool IsRoot
		{
			get { return Parent == null; }
		}

		/// <summary>True if the parent of this node is the root node</summary>
		public bool IsTopLevel
		{
			get { return Parent == null || Parent.IsRoot; }
		}

		/// <summary>True if the node is expanded, false if collapsed</summary>
		public bool IsExpanded { get; private set; }

		/// <summary>True if this node has been added as a row in the grid (changes as nodes are expanded/collapsed)</summary>
		public bool IsInGrid
		{
			get
			{
				Debug.Assert(!(m_is_in_grid && Grid == null), "Cannot be added to a grid while the Grid is null");
				return m_is_in_grid || IsRoot;
			}
			set
			{
				if (m_is_in_grid == value) return;
				m_is_in_grid = value;
				if (m_is_in_grid)
				{
					Debug.Assert(Grid != null, "Cannot be added to a grid while the Grid is null");
				}
			}
		}
		private bool m_is_in_grid;

		/// <summary>True if this node has children</summary>
		public bool HasChildren
		{
			get
			{
				return 
					IsDataBound ? (DataBoundTreeItem?.ChildCount ?? 0) != 0 :
					Nodes.Count != 0;
			}
		}

		/// <summary>True if a '+' should be rendered next to this node even if it has no children. It's expected that children will be added in the NodeExpanding event</summary>
		public bool VirtualNodes
		{
			get { return m_impl_virtual_nodes || (Grid?.VirtualNodes ?? false); }
			set { m_impl_virtual_nodes = value; }
		}
		private bool m_impl_virtual_nodes;

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
			get
			{
				int lvl = -1;
				for (var p = Parent; p != null; p = p.Parent, ++lvl) {}
				return lvl;
			}
		}

		/// <summary>The index of this node within the children of it's parent</summary>
		public int NodeIndex
		{
			get { return (Parent == null ? Grid.Nodes : Parent.Nodes).IndexOf(this); }
		}

		/// <summary>Gets the index of this row in the Grid</summary>
		public int RowIndex
		{
			get { return Index; }
		}
		private new int Index
		{
			// hide the Index property of the row
			get { return IsInGrid ? base.Index : -1; }
		}

		/// <summary>Gets the index of the row after this node and its children (if visible in the grid). The number of rows used by this node is NextRowIndex - RowIndex</summary>
		private int NextRowIndex
		{
			get
			{
				if (!IsInGrid) return -1;
				if (IsExpanded && Nodes.Count != 0)
				{
					Debug.Assert(Nodes.Back().IsInGrid);
					return Nodes.Back().NextRowIndex;
				}
				return RowIndex + 1;
			}
		}

		/// <summary>The range of row indices required by this node</summary>
		public RangeI RowIndexRange
		{
			get { return new RangeI(RowIndex, NextRowIndex); }
		}

		/// <summary>Return the tree cell in this row</summary>
		public TreeGridCell TreeCell
		{
			get
			{
				var tree_cell = Cells.OfType<TreeGridCell>().FirstOrDefault();
				if (!DataGridView.IsInDesignMode() && tree_cell == null)
					throw new Exception("This grid does not have a 'TreeGridColumn'");

				return tree_cell;
			}
		}

		/// <summary>The collection of child nodes for this node</summary>
		[Category("Data")]
		[Description("The collection of child nodes for this node")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public TreeGridNodeCollection Nodes { [DebuggerStepThrough] get; private set; }

		/// <summary>The cell values for this row/node</summary>
		public IEnumerable Values
		{
			get { return Cells.OfType<DataGridViewCell>().Select(x => x.Value); }
		}

		/// <summary>Collapse the children of this node</summary>
		public bool Collapse(bool recursive = false)
		{
			if (!IsInGrid && Parent != null) Parent.Expand();
			if (!IsInGrid) throw new Exception("Cannot collapse nodes that are not displayed in the grid");
			if (!IsExpanded) return true;

			var grid = Grid;
			using (grid.SuspendLayout(true))
			{
				if (!grid.BeginCollapseNode(this))
					return false;

				// Remove all of the children from the grid
				foreach (var child in Nodes)
				{
					if (recursive)
						child.Collapse(recursive);
					child.RemoveRowsFromGrid();
				}

				IsExpanded = false;
				grid.EndCollapseNode(this);
				return true;
			}
		}

		/// <summary>Expand the children of this node</summary>
		public bool Expand(bool recusive = false)
		{
			if (!IsInGrid && Parent != null) Parent.Expand();
			if (!IsInGrid) throw new Exception("Cannot expand nodes that are not displayed in the grid");
			if (IsExpanded) return true;

			var grid = Grid;
			using (grid.SuspendLayout(true))
			{
				if (!grid.BeginExpandNode(this))
					return false;

				// Add all children to the grid
				int row_index = RowIndex + 1;
				foreach (var child in Nodes)
				{
					child.DisplayRowsInGrid(ref row_index);
					if (recusive)
						child.Expand(recusive);
				}

				IsExpanded = true;
				grid.EndExpandNode(this);
				return true;
			}
		}

		/// <summary>True if this node should display the '+' or '-' glyph</summary>
		internal bool GlyphVisible
		{
			get
			{
				if (HasChildren) return true;
				if (VirtualNodes) return true;
				if (DataBoundTreeItem is ITreeItem ti) return ti.ChildCount != 0;
				return false;
			}
		}

		/// <summary>Remove this node from the grid.</summary>
		internal void RemoveRowsFromGrid()
		{
			if (Grid == null) return;
			if (!IsInGrid || IsRoot) return;

			// Remove all children from the grid
			foreach (TreeGridNode child in Nodes)
				child.RemoveRowsFromGrid();

			// If the current cell is about to be removed, clear the current cell
			if (Grid.CurrentCell?.OwningRow == this)
				Grid.CurrentCell = null;

			// Remove this node from the grid
			Grid.Rows.Remove(this);
			Debug.Assert(!IsInGrid);
		}

		/// <summary>Add this node to the grid</summary>
		internal void DisplayRowsInGrid(ref int row_index)
		{
			if (Grid == null) return;
			if (IsInGrid || IsRoot) return;

			// Insert this node as a row in the grid
			Grid.Rows.Insert(row_index++, this);
			Debug.Assert(IsInGrid);
			TreeCell.Dirty = true;

			// If this node is expanded add all of it's children
			if (IsExpanded)
				foreach (var child in Nodes)
					child.DisplayRowsInGrid(ref row_index);
		}

		/// <summary>Synchronise the children of this node with the data source</summary>
		internal void SyncToDataSource()
		{
			if (Grid.DataSource == null)
				return;

			// Can't Sync unless the data bound item is a tree item
			var item = DataBoundTreeItem;
			if (item == null)
				return;

			// Ensure the child nodes are in-sync with the children of 'item'
			Nodes.Count = item.ChildCount;
			foreach (var node in Nodes)
				node.SyncToDataSource();
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
		public void DumpTreeC()
		{
			Console.Write(DumpTree());
		}
		public string DumpTree()
		{
			var sb = new StringBuilder();
			DumpTree(this, sb);
			return sb.ToString();
		}
		private static void DumpTree(TreeGridNode node, StringBuilder sb)
		{
			for (int i = node.Level; i-- != 0;) sb.Append(' ');
			sb.Append(node).Append('\n');
			foreach (var n in node.Nodes) DumpTree(n, sb);
		}
	}

	/// <summary>A collection of nodes (a.k.a rows).</summary>
	public class TreeGridNodeCollection :IList<TreeGridNode>, IList
	{
		// The node collection controls the parenting of nodes since it is the
		// only place that the child order is known. When a node is added to the
		// collection it will be displayed in the grid if the parent node is also
		// displayed and expanded.

		public TreeGridNodeCollection(TreeGridView grid, TreeGridNode owner)
		{
			Grid = grid;
			Owner = owner;
			List = new List<TreeGridNode>();
		}

		/// <summary>The grid that this collection belongs to</summary>
		public TreeGridView Grid { get; private set; }

		/// <summary>The node that this collection belongs to</summary>
		public TreeGridNode Owner { get; private set; }

		/// <summary>The collection of child nodes</summary>
		private List<TreeGridNode> List { [DebuggerStepThrough] get; set; } 

		/// <summary>The number of items in the collection</summary>
		public int Count
		{
			get{ return List.Count; }
			set
			{
				if (Count == value) return;
				using (Grid.SuspendLayout(true))
				{
					// Remove excess nodes
					for (int i = Count; i > value; --i)
						List[i-1].Parent = null;

					// Append new nodes
					for (int i = Count; i < value; ++i)
						((TreeGridNode)Grid.RowTemplate.Clone()).Parent = Owner;
				}
			}
		}

		/// <summary>Remove all nodes from the collection</summary>
		public void Clear()
		{
			Count = 0;
		}

		/// <summary>Add a node to this collection causing it to be parented to the node that owns this collection</summary>
		public TreeGridNode Add(TreeGridNode node)
		{
			node.Parent = Owner;
			return node;
		}

		/// <summary>Add a node with values for the following grid columns</summary>
		public TreeGridNode Add(params object[] values)
		{
			return Insert(List.Count, values);
		}

		/// <summary>Insert a node into the collection at 'index'</summary>
		public TreeGridNode Insert(int index, TreeGridNode node)
		{
			// Setting parent inserts 'node' at the end of the collection
			// If 'index' is not at the end we need to remove 'node' from the
			// end and insert it at the correct index.
			node.Parent = Owner;
			if (index != List.Count - 1)
			{
				List.RemoveAt(List.Count-1);
				List.Insert(index, node);
			}
			return node;
		}

		/// <summary>Add a node with values for the following grid columns</summary>
		public TreeGridNode Insert(int index, params object[] values)
		{
			if (Owner.Grid == null)
				throw new Exception("Can only insert child nodes after this node has been added to the tree grid");
			if (Owner.Grid.DataSource != null)
				throw new Exception($"{nameof(TreeGridView)} is data bound. Do not modify the node collection explicitly");

			return Insert(index, new TreeGridNode(Owner.Grid, values));
		}

		/// <summary>Add a node based on an object that will provide the values for the columns in the grid (available as DataBoundItem)</summary>
		public TreeGridNode Bind(object data)
		{
			var node = new TreeGridNode(Owner.Grid){ DataBoundItem = data };
			return Insert(List.Count, node);
		}

		/// <summary>Remove 'node' from this collection</summary>
		public bool Remove(TreeGridNode node)
		{
			if (!List.Contains(node)) return false;
			node.Parent = null; // This will remove 'node' from our collection
			return true;
		}

		/// <summary>Remove the node at 'index' from the collection</summary>
		public void RemoveAt(int index)
		{
			// This will remove 'node' from our collection
			List[index].Parent = null;
		}

		/// <summary>Add 'node' to the collection</summary>
		internal void AddInternal(TreeGridNode node)
		{
			Debug.Assert(node.Parent == Owner);
			List.Add(node);
		}

		/// <summary>Remove node from this collection</summary>
		internal void RemoveInternal(TreeGridNode node)
		{
			Debug.Assert(node.Parent == Owner);
			List.Remove(node);
		}

		/// <summary>Return the index of a node in the collection</summary>
		public int IndexOf(TreeGridNode node)
		{
			return List.IndexOf(node);
		}

		/// <summary>Returns true if 'node' is in this collection</summary>
		public bool Contains(TreeGridNode node)
		{
			return List.Contains(node);
		}

		/// <summary>Always false, the list is not readonly</summary>
		public bool IsReadOnly
		{
			get{ return false; }
		}

		/// <summary>Get/Set the node at a given index</summary>
		public TreeGridNode this[int index]
		{
			get { return List[index]; }
			set
			{
				RemoveAt(index);
				Insert(index, value);
			}
		}

		/// <summary>Copy the node collection to an array</summary>
		public void CopyTo(TreeGridNode[] array, int arrayIndex)
		{
			List.CopyTo(array, arrayIndex);
		}

		/// <summary></summary>
		public override string ToString()
		{
			return $"Nodes={Count}";
		}

		#region IList<TreeGridNode>
		int  IList<TreeGridNode>.IndexOf(TreeGridNode item)
		{
			return List.IndexOf(item);
		}
		void IList<TreeGridNode>.Insert(int index, TreeGridNode node)
		{
			Insert(index, node);
		}
		#endregion

		#region IList
		void IList.Remove(object value)
		{
			Remove(value as TreeGridNode);
		}
		int IList.Add(object value)
		{
			return Add((TreeGridNode)value).NodeIndex;
		}
		void IList.Insert(int index, object node)
		{
			Insert(index, (TreeGridNode)node);
		}
		void IList.RemoveAt(int index)
		{
			RemoveAt(index);
		}
		void IList.Clear()
		{
			Clear();
		}
		bool IList.IsReadOnly
		{
			get { return IsReadOnly; }
		}
		bool IList.IsFixedSize
		{
			get { return false; }
		}
		int IList.IndexOf(object item)
		{
			return IndexOf((TreeGridNode)item);
		}
		bool IList.Contains(object value)
		{
			return Contains((TreeGridNode)value);
		}
		object IList.this[int index]
		{
			get { return this[index]; }
			set { this[index] = (TreeGridNode)value; }
		}
		#endregion

		#region IEnumerable
		public IEnumerator<TreeGridNode> GetEnumerator()
		{
			return List.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion

		#region ICollection
		void ICollection<TreeGridNode>.Add(TreeGridNode node)
		{
			Add(node);
		}
		int ICollection.Count
		{
			get { return Count; }
		}
		void ICollection.CopyTo(Array array, int index)
		{
			CopyTo((TreeGridNode[])array, index);
		}
		bool ICollection.IsSynchronized
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}
		object ICollection.SyncRoot
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}
		#endregion
	}

	/// <summary>Column type for the tree column</summary>
	public class TreeGridColumn : DataGridViewTextBoxColumn
	{
		public TreeGridColumn()
		{
			CellTemplate = new TreeGridCell();
		}
		
		/// <summary></summary>
		public Image DefaultNodeImage { get; set; }

		/// <summary></summary>
		public override object Clone()
		{
			// Need to override Clone for design-time support
			var c = (TreeGridColumn)base.Clone();
			if (c != null) { c.DefaultNodeImage = DefaultNodeImage; }
			return c;
		}
	}

	/// <summary>A cell within the tree column of a TreeGridView</summary>
	public class TreeGridCell :DataGridViewTextBoxCell
	{
		public TreeGridCell()
			:base()
		{}

		/// <summary>Data controlling the indenting behaviour of tree cells</summary>
		private Indenting m_indent = DefaultIndenting; // Indenting parameters
		public static Indenting DefaultIndenting = new Indenting();
		public class Indenting
		{
			/// <summary>The size (in pixels) of the glyph '+' or '-' plus line leading to the text or image</summary>
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

		/// <summary>Flag the cell as needing layout</summary>
		public bool Dirty
		{
			set { m_calc_padding |= value; }
		}
		private bool m_calc_padding; 

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
		public Func<object,string> TextFormatter = DefaultTextFormater;
		private static readonly Func<object,string> DefaultTextFormater = o => o.ToString();

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

		/// <summary>Paint the tree column cell</summary>
		protected override void Paint(Graphics graphics, Rectangle clip_bounds, Rectangle cell_bounds, int row_index, DataGridViewElementStates cell_state, object value, object formatted_value, string error_text, DataGridViewCellStyle cell_style, DataGridViewAdvancedBorderStyle advanced_border_style, DataGridViewPaintParts paint_parts)
		{
			var node = OwningNode;
			if (node == null)
			{
				base.Paint(graphics, clip_bounds, cell_bounds, row_index, cell_state, value, formatted_value, error_text, cell_style, advanced_border_style, paint_parts);
				return;
			}

			var img = node.Image;
			var img_h = img != null ? Math.Min(img.Height, cell_bounds.Height) : 0;
			var img_w = img != null ? img_h * img.Width / img.Height : 0;

			// Update the cell padding if required
			if (m_calc_padding)
			{
				cell_style.Padding = Style.Padding = new Padding(m_indent.m_margin + m_indent.m_glyph_width + (Level * m_indent.m_width) + img_w + 2, 0, 0, 0);
				m_calc_padding = false;
			}

			// Paint the cell normally (including background and text)
			base.Paint(graphics, clip_bounds, cell_bounds, row_index, cell_state, value, formatted_value, error_text, cell_style, advanced_border_style, paint_parts);

			// The rectangle in which the lines and glyphs are drawn
			var glyph_rect = new Rectangle(cell_bounds.Left + m_indent.m_margin + (Level * m_indent.m_width), cell_bounds.Top, m_indent.m_glyph_width, cell_bounds.Height - 1);

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
					int top = cell_bounds.Top    + (is_first_sibling && Level == 0 ? cell_bounds.Height/2 : 0);
					int bot = cell_bounds.Bottom - (is_last_sibling  ? cell_bounds.Height/2 : 0);
					graphics.DrawLine(line_pen, glyph_rect.X + ghw, top, glyph_rect.X + ghw, bot);

					// Horizontal line
					top = cell_bounds.Top + cell_bounds.Height/2;
					graphics.DrawLine(line_pen, glyph_rect.X + ghw, top, glyph_rect.Right, top);

					// Paint lines of previous levels to the root
					int x = (glyph_rect.X + ghw) - m_indent.m_width;
					for (var p = node.Parent; p != null && !p.IsRoot; p = p.Parent, x -= m_indent.m_width)
					{
						if (p.HasChildren && !p.IsLastSibling) // paint vertical line
							graphics.DrawLine(line_pen, x, cell_bounds.Top, x, cell_bounds.Bottom);
					}
				}
			}

			// Paint node glyphs
			if (node.GlyphVisible)
			{
				var glyph = new Rectangle(glyph_rect.X, glyph_rect.Y + (glyph_rect.Height / 2) - 4, 10, 10);
				int midx = glyph.Left + glyph.Width / 2, midy = glyph.Top + glyph.Height / 2;
				graphics.FillRectangle(SystemBrushes.ControlLight, glyph);
				graphics.DrawRectangle(SystemPens.ControlDarkDark, glyph);
				graphics.DrawLine(SystemPens.ControlDarkDark, new Point(glyph.Left + 2, midy), new Point(glyph.Right - 2, midy));
				if (!node.IsExpanded) // make it a '+' if not expanded
					graphics.DrawLine(SystemPens.ControlDarkDark, new Point(midx, glyph.Top + 2), new Point(midx, glyph.Bottom - 2));
			}

			// Paint the associated image
			if (img != null)
			{
				using (graphics.SaveState())
				{
					var img_rect = new Rectangle(glyph_rect.Right, cell_bounds.Top + (cell_bounds.Height - img_h)/2, img_w, img_h);
					graphics.SetClip(img_rect);
					graphics.DrawImage(img, img_rect);
				}
			}
		}
	}

	/// <summary>An interface for tree item.</summary>
	public interface ITreeItem
	{
		// Notes:
		//  - The backing data for the tree does not have to implement this interface,
		//    but if it does, then data binding can be supported.
		//  - You might expect 'Child' and 'Parent' to return 'ITreeItem' but that would
		//    require the items used in the tree to implement 'ITreeItem' which isn't
		//    possible for third party types. 'object' is used along with the 'DataBinder'
		//    function to traverse the tree

		/// <summary>Return the number of children this tree item has</summary>
		int ChildCount { get; }

		/// <summary>Return a child of this item by index</summary>
		object Child(int index);

		/// <summary>Return the parent item of this tree item</summary>
		object Parent { get; }
	}

	#region EventArgs

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

	#endregion
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Gui.WinForms;

	[TestFixture] public class TestTreeGridView
	{
		private class Thing
		{
			public Thing(string one, int two)
			{
				One = one;
				Two = two;
			}
			public string One { get; set; }
			public int Two { get; set; }
		}

		[Test] public void TestTGV1()
		{
			var grid0 = new TreeGridView();
			grid0.Columns.Add(new TreeGridColumn{HeaderText="col0"});
			grid0.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="col1"});
			grid0.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="col2"});

			var a0 = grid0.Nodes.Add("a0");
			var a0_0 = a0.Nodes.Add("a0_0");
			var a0_1 = a0.Nodes.Add("a0_1");
			var a0_0_0 = a0_0.Nodes.Add("a0_0_0");
			var a0_0_1 = a0_0.Nodes.Add("a0_0_1");
			var a0_0_2 = a0_0.Nodes.Add("a0_0_2");

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
		[Test] public void TestTGV2()
		{
			var grid0 = new TreeGridView();
			grid0.Columns.Add(new TreeGridColumn{HeaderText="col0"});
			grid0.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="col1"});
			grid0.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="col2"});
			var grid1 = new TreeGridView();
			grid1.Columns.Add(new TreeGridColumn{HeaderText="col0"});

			var a0 = grid0.Nodes.Add("a0", 0, 0);
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
		[Test] public void TestTGV3()
		{
			var grid0 = new TreeGridView();
			grid0.Columns.Add(new TreeGridColumn{HeaderText="col0"});
			grid0.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="col1", DataPropertyName = nameof(Thing.One)});
			grid0.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="col2", DataPropertyName = nameof(Thing.Two)});

			var a0 = grid0.Nodes.Bind(new Thing("ONE", 1));
			a0.Nodes.Bind(new Thing("two", 2));
			a0.Nodes.Bind(new Thing("too", 11));
		}
	}
}
#endif
