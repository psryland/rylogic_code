using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>
	/// Represents a node in the tree of dock panes.
	/// Each node has 5 children; Centre, Left, Right, Top, Bottom.
	/// A root branch can be hosted in a dock container, floating window, or auto hide window</summary>
	[DebuggerDisplay("{DumpDesc()}")]
	internal partial class Branch : DockPanel, IPaneOrBranch, IDisposable
	{
		public const int DockSiteCount = 5;

		public Branch(DockContainer dc, DockSizeData dock_sizes)
		{
			DockContainer = dc;
			DockSizes = new DockSizeData(dock_sizes) { Owner = this };

			// Create the collection to hold the five child controls (DockPanes or Branches)
			Descendants = new DescendantCollection(this);
		}
		public virtual void Dispose()
		{
			Util.Dispose(Descendants);
			DockContainer = null;
		}

		/// <summary>The dock container that this pane belongs too</summary>
		public DockContainer DockContainer { [DebuggerStepThrough] get; private set; }

		/// <summary>The branch that is the immediate parent of this branch (null if root)</summary>
		public Branch ParentBranch
		{
			[DebuggerStepThrough]
			get { return Parent as Branch; }
		}

		/// <summary>The branch at the top of the tree that contains this pane</summary>
		public Branch RootBranch
		{
			[DebuggerStepThrough]
			get
			{
				var b = this;
				for (; b.Parent is Branch; b = (Branch)b.Parent) { }
				return b;
			}
		}

		/// <summary>The main dock container, auto hide panel, or floating window that hosts this branch</summary>
		internal ITreeHost TreeHost
		{
			[DebuggerStepThrough]
			get
			{
				var p = Parent;
				for (; p != null && !(p is ITreeHost) && p is FrameworkElement fe; p = fe.Parent) { }
				return p as ITreeHost;
			}
		}

		/// <summary>The sizes of the child controls</summary>
		public DockSizeData DockSizes { get; private set; }

		/// <summary>Add a dockable instance to this tree at the position described by 'location'.</summary>
		public DockPane Add(DockControl dc, int index, params EDockSite[] location)
		{
			if (dc == null)
				throw new ArgumentNullException(nameof(dc), "Cannot add 'null' content");
			if (location.Length == 0)
				throw new ArgumentException("A valid tree location is required", nameof(location));

			// If already on a pane, remove first (raising events)
			// Removing panes causes PruneBranches to be called, so this must be called before
			// DockPane(...) otherwise the newly added pane/branches will be deleted.
			dc.DockPane = null;

			// Find the dock pane for this dock site, growing the tree as necessary
			var pane = DockPane(location.First(), location.Skip(1));

			// Add the content
			index = Math_.Clamp(index, 0, pane.Content.Count);
			pane.Content.Insert(index, dc);
			pane.ContentView.MoveCurrentToPosition(index);
			return pane;
		}

		/// <summary>Remove branches without any content to reduce the size of the tree</summary>
		internal void PruneBranches()
		{
			// Depth-first recursive
			foreach (var b in Descendants.Select(x => x.Item as Branch).NotNull())
				b.PruneBranches();

			// If any of the child branches only have a single child, replace the branch with it's child
			foreach (var c in Descendants.Where(x => x.Item is Branch).NotNull())
			{
				var b = (Branch)c.Item;

				// The child must, itself, have only one child
				if (b.Descendants.Count != 1)
					continue;

				// Replace the branch with it's single child
				var nue = b.Descendants[0].Item;
				var old = c.Item;
				c.Item = nue;
				Util.Dispose(ref old);
			}

			// If any of L,R,T,B contain empty panes, prune them. Don't prune
			// 'C', we need to leave somewhere for content to be dropped.
			foreach (var c in Descendants.Where(x => x.Item is DockPane))
			{
				var p = (DockPane)c.Item;

				if (p.Content.Count == 0 && c.DockSite != EDockSite.Centre)
				{
					// Dispose the pane
					c.Item = null;
					Util.Dispose(ref p);
				}
			}

			// If the branch has only one child, ensure it's in the Centre position
			if (Descendants.Count == 1 && Descendants[EDockSite.Centre].Item == null)
			{
				var ctrl = Descendants[0].Item;
				Descendants[0].Item = null;
				Descendants[EDockSite.Centre].Item = ctrl;
			}

			// Ensure there is always a centre pane
			if (Descendants[EDockSite.Centre].Item == null)
				Descendants[EDockSite.Centre].Item = new DockPane(DockContainer, show_pin: false);

			// Check all the logic is correct
			Debug.Assert(ValidateTree());
		}

		/// <summary>Add branches to the tree until 'rest' is empty.</summary>
		private Branch GrowBranches(EDockSite ds, IEnumerable<EDockSite> rest, out EDockSite last_ds)
		{
			Debug.Assert(ds >= EDockSite.Centre && ds < EDockSite.None, "Invalid dock site");

			// Note: When rest is empty, the site at 'ds' does not have a branch added.
			// This is deliberate so that dock addresses (arrays of EDockSite) can be
			// used to grow the tree to the point where a dock pane should be added.
			if (!rest.Any())
			{
				// If we've reached the end of the dock address, but there are still branches
				// keep going down the centre dock site until a null or dock pane is reached
				var b = this;
				for (; b.Descendants[ds].Item is Branch cb; b = cb, ds = EDockSite.Centre) { }

				Debug.Assert(ValidateTree());

				last_ds = ds;
				return b;
			}

			// No child at 'ds' add a branch
			if (Descendants[ds].Item == null)
			{
				var new_branch = new Branch(DockContainer, ds == EDockSite.Centre ? DockSizeData.Quarters : DockSizeData.Halves);
				Descendants[ds].Item = new_branch;
				return new_branch.GrowBranches(rest.First(), rest.Skip(1), out last_ds);
			}

			// A dock pane at 'ds'? Swap it with a branch
			// containing the dock pane as the centre child
			if (Descendants[ds].Item is DockPane)
			{
				var new_branch = new Branch(DockContainer, DockSizeData.Halves);
				new_branch.Descendants[EDockSite.Centre].Item = Descendants[ds].Item;
				Descendants[ds].Item = new_branch;
				return new_branch.GrowBranches(rest.First(), rest.Skip(1), out last_ds);
			}
			else
			{
				// Existing branch, recursive call into it
				var branch = Descendants[ds].Item as Branch;
				return branch.GrowBranches(rest.First(), rest.Skip(1), out last_ds);
			}
		}

		/// <summary>Get the pane at 'location', adding branches to the tree if necessary</summary>
		public DockPane DockPane(EDockSite ds, IEnumerable<EDockSite> rest)
		{
			// Grow the tree
			var branch = GrowBranches(ds, rest, out ds);

			// No child at 'ds'? Add a dock pane.
			if (branch.Descendants[ds].Item == null)
			{
				var pane = new DockPane(DockContainer, show_pin: ds.IsEdge());
				branch.Descendants[ds].Item = pane;
			}

			// Existing pane at 'ds'
			Debug.Assert(branch.Descendants[ds].Item is DockPane);
			return branch.Descendants[ds].Item as DockPane;
		}
		public DockPane DockPane(EDockSite ds)
		{
			return DockPane(ds, Enumerable.Empty<EDockSite>());
		}

		/// <summary>Get the child at 'location' or null if the given location is not a valid location in the tree</summary>
		public DescentantData DescendantAt(IEnumerable<EDockSite> location)
		{
			var b = this;
			var c = (DescentantData)null;
			foreach (var ds in location)
			{
				if (b == null) return null;
				c = b.Descendants[ds];
				b = c.Item as Branch;
			}
			return c;
		}

		/// <summary>Returns a reference to the dock sizes at a given level in the tree</summary>
		public DockSizeData GetDockSizes(IEnumerable<EDockSite> location)
		{
			var b = this;
			foreach (var ds in location)
			{
				if (b == null) return null;
				b = b.Descendants[ds].Item as Branch;
			}
			return b?.DockSizes;
		}

		/// <summary>The dock panes or branches of this branch (5 per branch)</summary>
		internal DescendantCollection Descendants { [DebuggerStepThrough] get; private set; }
		internal class DescendantCollection : IDisposable, IEnumerable<DescentantData>
		{
			/// <summary>The child controls (dock panes or branches) of this branch</summary>
			private readonly Branch m_branch;
			private DescentantData[] m_descendants;

			public DescendantCollection(Branch branch)
			{
				m_branch = branch;

				// Create an array to hold the five child controls (DockPanes or Branches)
				m_descendants = new DescentantData[DockSiteCount];
				for (var i = 0; i != m_descendants.Length; ++i)
					m_descendants[i] = new DescentantData(m_branch, (EDockSite)i);
			}
			public virtual void Dispose()
			{
				Util.DisposeRange(m_descendants.Select(x => x.Item));
			}

			/// <summary>The number of non-null children in this collection</summary>
			public int Count
			{
				get { return m_descendants.Count(x => x.Item != null); }
			}

			/// <summary>Access the non-null children in this collection</summary>
			public DescentantData this[int index]
			{
				get { return m_descendants.Where(x => x.Item != null).ElementAt(index); }
			}

			/// <summary>Get a child control in a given dock site</summary>
			public DescentantData this[EDockSite ds]
			{
				[DebuggerStepThrough]
				get { return m_descendants[(int)ds]; }
			}

			/// <summary>Enumerate the controls in the collection</summary>
			public IEnumerator<DescentantData> GetEnumerator()
			{
				foreach (var c in m_descendants)
					yield return c;
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		/// <summary>Wrapper of a pane or branch</summary>
		[DebuggerDisplay("{DockSite} {Item}")]
		internal class DescentantData
		{
			public DescentantData(Branch branch, EDockSite ds)
			{
				ParentBranch = branch;
				DockSite = ds;
			}
			public void Dispose()
			{ }

			/// <summary>The branch this is descendant from</summary>
			public Branch ParentBranch { get; private set; }

			/// <summary>The site that this child represents</summary>
			public EDockSite DockSite { get; private set; }

			/// <summary>A dock pane or branch</summary>
			public IPaneOrBranch Item
			{
				get { return m_item; }
				set
				{
					if (m_item == value) return;
					if (m_item != null)
					{
						// Remove the pane or branch descendant
						// Note: do not dispose the pane or branch because this could be a reassignment to a different dock site
						ParentBranch.Children.Remove(m_item as UIElement);
						ParentBranch.Children.Remove(m_splitter);

						// Notify of the tree change
						ParentBranch.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.Removed, pane: m_item as DockPane, branch: m_item as Branch));

						m_item = null;
						m_splitter = null;
					}
					m_item = value;
					if (m_item != null)
					{
						// Add the pane or branch as the descendant.
						// Note: the order of children is important. DockPanel assumes the last child is the "un-docked" child.
						// Also, the child elements are docked/clipped in reverse order, so Children[0] will have the smallest available area.
						// Also, the dock panel splitters operate on their immediate prior sibling.
						if (DockSite == EDockSite.Centre)
						{
							ParentBranch.Children.Add(m_item as UIElement);
						}
						else
						{
							// Always have Left/Right bigger than Top/Bottom or allow dock order to control layout?
							//// Find the insert index
							//var index = 0;
							//for (; index != ParentBranch.Children.Count; ++index)
							//{
							//	var child = ParentBranch.Children[index];
							//	if (child is Branch || child is
							//}

							// Dock 'item'
							var item = ParentBranch.Children.Insert2(0, m_item as UIElement);
							DockPanel.SetDock(item, DockContainer.ToDock(DockSite));

							// Add a splitter control for non-centre items
							m_splitter = ParentBranch.Children.Insert2(1, new DockPanelSplitter { Thickness = 5.0, ProportionalResize = true, Background = SystemColors.ControlBrush });
							DockPanel.SetDock(m_splitter, DockContainer.ToDock(DockSite));
						}

						// Notify of the tree change
						ParentBranch.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.Added, pane: m_item as DockPane, branch: m_item as Branch));
					}
				}
			}
			private IPaneOrBranch m_item;
			private DockPanelSplitter m_splitter;
		}

		/// <summary>Get the dock site for this branch within a parent branch.</summary>
		internal EDockSite DockSite
		{
			get { return ParentBranch?.Descendants.Single(x => x.Item == this).DockSite ?? EDockSite.Centre; }
		}

		/// <summary>Get the dock sites, from top to bottom, describing where this branch is located in the tree </summary>
		internal EDockSite[] DockAddress
		{
			get
			{
				var s = new Stack<EDockSite>();
				for (var b = this; b != null && b.Parent is Branch; b = b.Parent as Branch)
					s.Push(b.DockSite);
				return s.ToArray();
			}
		}

		/// <summary>A bitmap of the dock sites that have something docked</summary>
		internal EDockMask DockedMask
		{
			get
			{
				var mask = EDockMask.None;
				foreach (var c in Descendants.Where(x => x.Item != null))
					mask |= (EDockMask)(1 << (int)c.DockSite);
				return mask;
			}
		}

		/// <summary>True if the tree contains no content</summary>
		public bool Empty
		{
			get { return !AllContent.Any(); }
		}

		/// <summary>Enumerates the branches in this sub-tree (breadth first, order = order of EDockSite)</summary>
		private IEnumerable<Branch> AllBranches
		{
			get
			{
				var queue = new Queue<Branch>();
				for (queue.Enqueue(this); queue.Count != 0;)
				{
					var b = queue.Dequeue();
					foreach (var c in b.Descendants.Select(x => x.Item as Branch).NotNull())
						queue.Enqueue(c);

					yield return b;
				}
			}
		}

		/// <summary>Enumerate the panes (leaves) in this sub-tree (breadth first, order = order of EDockSite)</summary>
		public IEnumerable<DockPane> AllPanes
		{
			get
			{
				foreach (var b in AllBranches)
					foreach (var c in b.Descendants.Select(x => x.Item as DockPane).NotNull())
						yield return c;
			}
		}

		/// <summary>Enumerate the dockables in this sub-tree (breadth first, order = order of EDockSite)</summary>
		public IEnumerable<DockControl> AllContent
		{
			get
			{
				foreach (var p in AllPanes)
					foreach (var c in p.Content)
						yield return c;
			}
		}

		/// <summary>Get/Set the size for a dock site in this branch (in pixels)</summary>
		public double ChildSize(EDockSite location)
		{
			return DockSizes.GetSize(location, DisplayRectangle, DockedMask);
		}
		public void ChildSize(EDockSite location, int value)
		{
			DockSizes.SetSize(location, DisplayRectangle, value);
		}

		/// <summary>Get the bounds of a dock site in parent (DockContainer or containing Branch) space</summary>
		public Rect ChildBounds(EDockSite location)
		{
			var display_rect = new Rect(RenderSize);
			display_rect.Width = Math.Max(0, display_rect.Width);
			display_rect.Height = Math.Max(0, display_rect.Height);
			return DockContainer.DockSiteBounds(location, display_rect, DockedMask, DockSizes);
		}

		/// <summary>Return the child element relative to this sub-tree</summary>
		internal UIElement ChildAt(EDockSite[] address)
		{
			var c = (UIElement)this;
			foreach (var ds in address)
			{
				if (c is Branch b)
					c = (UIElement)b.Descendants[ds].Item;
				else
					throw new Exception($"Invalid child address");
			}
			return c;
		}

		/// <summary>Return the area that the control should cover when rendered</summary>
		private Rect DisplayRectangle
		{
			get { return new Rect(RenderSize); }
		}

		/// <summary>Raised whenever a child (pane or branch) is added to or removed from this sub tree</summary>
		public event EventHandler<TreeChangedEventArgs> TreeChanged;
		private void RaiseTreeChanged(TreeChangedEventArgs args)
		{
			TreeChanged?.Invoke(this, args);
			ParentBranch?.OnTreeChanged(args);
		}
		internal void OnTreeChanged(TreeChangedEventArgs args)
		{
			RaiseTreeChanged(args);
		}

		/// <summary>Record the layout of the tree</summary>
		public XElement ToXml(XElement node)
		{
			// Record the pane sizes
			node.Add2(XmlTag.DockSizes, DockSizes, false);

			// Recursively add the sub trees
			foreach (var ch in Descendants)
			{
				if (ch.Item is DockPane p)
				{
					var n = node.Add2(XmlTag.Pane, p, false);
					n.SetAttributeValue(XmlTag.Location, ch.DockSite);
				}
				if (ch.Item is Branch b)
				{
					var n = node.Add2(XmlTag.Tree, b, false);
					n.SetAttributeValue(XmlTag.Location, ch.DockSite);
				}
			}
			return node;
		}

		/// <summary>Apply layout to this tree</summary>
		public void ApplyState(XElement node)
		{
			foreach (var child_node in node.Elements())
			{
				switch (child_node.Name.LocalName)
				{
				case XmlTag.DockSizes:
					{
						var dock_sizes = child_node.As<DockSizeData>();
						DockSizes.Left = dock_sizes.Left;
						DockSizes.Top = dock_sizes.Top;
						DockSizes.Right = dock_sizes.Right;
						DockSizes.Bottom = dock_sizes.Bottom;
						break;
					}
				case XmlTag.Tree:
					{
						var ds = Enum<EDockSite>.Parse(child_node.Attribute(XmlTag.Location).Value);
						if (Descendants[ds].Item is Branch b)
							b.ApplyState(child_node);
						break;
					}
				case XmlTag.Pane:
					{
						var ds = Enum<EDockSite>.Parse(child_node.Attribute(XmlTag.Location).Value);
						if (Descendants[ds].Item is DockPane p)
							p.ApplyState(child_node);
						break;
					}
				}
			}
		}

		/// <summary>Output the tree structure as a string (Recursion method, call 'DockContainer.DumpTree()')</summary>
		public void DumpTree(StringBuilder sb, int indent, string prefix)
		{
			sb.Append(' ', indent).Append(prefix).Append("Branch: ").AppendLine();
			sb.Append(' ', indent).Append("{").AppendLine();
			foreach (var c in Descendants)
			{
				if (c.Item is Branch b) b.DumpTree(sb, indent + 4, $"{c.DockSite,-6} = ");
				else if (c.Item is DockPane p) p.DumpTree(sb, indent + 4, $"{c.DockSite,-6} = ");
				else sb.Append(' ', indent + 4).Append($"{c.DockSite,-6} = <null>").AppendLine();
			}
			sb.Append(' ', indent).Append("}").AppendLine();
		}

		/// <summary>Self consistency check (for debugging)</summary>
		public bool ValidateTree()
		{
			try
			{
				ValidateTree(this);
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.MessageFull());
				return false;
			}

			void ValidateTree(Branch branch)
			{
				if (branch.DockContainer == null)
					throw new ObjectDisposedException("This branch has been disposed");

				// Check each child
				foreach (var child in branch.Descendants)
				{
					if (child.Item == null)
						continue;

					if (branch.Descendants[child.DockSite] != child)
						throw new Exception("This child is not in it's appropriate child slot");

					if (branch.Descendants.Count(x => x == child) != 1)
						throw new Exception("This child is in more than one slot");

					if (branch.Descendants.Count(x => x.Item == child.Item) != 1)
						throw new Exception("This child's control is in more than one slot");

					// Recursive check children
					if (child.Item is Branch b)
						ValidateTree(b);
					if (child.Item is DockPane p)
						p.ValidateTree();
				}
			}
		}

		/// <summary>String description of the branch for debugging</summary>
		public string DumpDesc()
		{
			var lvl = 0;
			for (var p = this; p != null && p.Parent is Branch; p = (Branch)p.Parent) ++lvl;
			var c = Descendants[EDockSite.Centre];
			var l = Descendants[EDockSite.Left];
			var t = Descendants[EDockSite.Top];
			var r = Descendants[EDockSite.Right];
			var b = Descendants[EDockSite.Bottom];
			return
				$"Branch Lvl={(lvl == 0 ? TreeHost.GetType()?.Name : lvl.ToString())}  " +
				$"C=[{(c.Item as DockPane)?.DumpDesc() ?? (c.Item is Branch ? "Branch" : "null")}] " +
				$"L=[{(l.Item as DockPane)?.DumpDesc() ?? (l.Item is Branch ? "Branch" : "null")}] " +
				$"T=[{(t.Item as DockPane)?.DumpDesc() ?? (t.Item is Branch ? "Branch" : "null")}] " +
				$"R=[{(r.Item as DockPane)?.DumpDesc() ?? (r.Item is Branch ? "Branch" : "null")}] " +
				$"B=[{(b.Item as DockPane)?.DumpDesc() ?? (b.Item is Branch ? "Branch" : "null")}]";
		}
	}


}
