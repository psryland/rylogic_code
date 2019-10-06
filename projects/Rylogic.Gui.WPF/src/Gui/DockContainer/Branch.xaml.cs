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
	internal sealed partial class Branch : DockPanel, IPaneOrBranch, IDisposable
	{
		public const int DockSiteCount = 5;

		public Branch(DockContainer dc, DockSizeData dock_sizes)
		{
			DockContainer = dc;
			DockSizes = new DockSizeData(dock_sizes) { Owner = this };

			// Create the collection to hold the five child controls (DockPanes or Branches)
			Descendants = new DescendantCollection(this);
		}
		protected override void OnChildDesiredSizeChanged(UIElement child)
		{
			base.OnChildDesiredSizeChanged(child);
			if (child is IPaneOrBranch c)
			{
				var ds = Descendants[c]?.DockSite ?? throw new Exception("Descendant data missing");
				if (ds == EDockSite.Centre)
					return;

				// Ignore changes until this branch is displayed
				if (RenderSize.Width == 0 || RenderSize.Height == 0)
					return;

				var item = (FrameworkElement)child;
				var size = ds.IsVertical() ? item.DesiredSize.Width : item.DesiredSize.Height;
				ChildSize(ds, size);
			}
		}
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);

			// Update the sizes of the descendent
			if (Descendants[EDockSite.Centre]?.Item is FrameworkElement c)
			{
				c.Width = double.NaN;
				c.Height = double.NaN;
			}
			if (Descendants[EDockSite.Left]?.Item is FrameworkElement l)
			{
				l.Width = ChildSize(EDockSite.Left);
				l.Height = double.NaN;
			}
			if (Descendants[EDockSite.Top]?.Item is FrameworkElement t)
			{
				t.Width = double.NaN;
				t.Height = ChildSize(EDockSite.Top);
			}
			if (Descendants[EDockSite.Right]?.Item is FrameworkElement r)
			{
				r.Width = ChildSize(EDockSite.Right);
				r.Height = double.NaN;
			}
			if (Descendants[EDockSite.Bottom]?.Item is FrameworkElement b)
			{
				b.Width = double.NaN;
				b.Height = ChildSize(EDockSite.Bottom);
			}
		}
		public void Dispose()
		{
			// Ensure this branch is removed from it's parent branch
			if (ParentBranch != null)
				ParentBranch.Descendants.Remove(this);

			Util.Dispose(Descendants);
		}

		/// <summary>The dock container that this pane belongs too</summary>
		public DockContainer DockContainer { get; }

		/// <summary>The branch that is the immediate parent of this branch (null if root)</summary>
		public Branch? ParentBranch => Parent as Branch;

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
		internal ITreeHost? TreeHost
		{
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
			var pane = DockPane(location);

			// Add the content
			index = Math_.Clamp(index, 0, pane.AllContent.Count);
			pane.AllContent.Insert(index, dc);
			return pane;
		}

		/// <summary>Signal PruneBranches on the next message</summary>
		internal void SignalPruneBranches()
		{
			if (m_prune_pending) return;
			m_prune_pending = true;
			Dispatcher.BeginInvoke(PruneBranches);
		}
		private bool m_prune_pending;

		/// <summary>Remove branches without any content to reduce the size of the tree</summary>
		internal void PruneBranches()
		{
			// Notes:
			//  - This method can change the addresses of dock panes.

			m_prune_pending = false;

			// A branch can be disposed between 'SignalPruneBranches' and the method
			// being invoked. In this case, ignore the prune branches.
			if (DockContainer == null)
				return;

			// Depth-first recursive
			foreach (var b in Descendants.Select(x => x.Item as Branch).NotNull())
				b.PruneBranches();

			// Remove empty panes. If any of L,R,T,B contain empty panes, prune them.
			// Don't prune 'C', we need to leave somewhere for content to be dropped.
			foreach (var c in Descendants.Where(x => x.Item is DockPane))
			{
				var p = (DockPane)c.Item!;

				if (p.AllContent.Count == 0 && c.DockSite != EDockSite.Centre)
				{
					// Dispose the pane
					c.Item = null;
					Util.Dispose(ref p!);
				}
			}

			// If any of the child branches only have a single child, replace the branch with it's child
			foreach (var c in Descendants.Where(x => x.Item is Branch).NotNull())
			{
				var b = (Branch)c.Item!;

				// The child must, itself, have only one child
				if (b.Descendants.Count != 1)
					continue;

				// Replace the branch with it's single child
				var old = c.Item;
				var nue = b.Descendants[0].Item;
				b.Descendants[0].Item = null;
				c.Item = nue;
				Util.Dispose(ref old!);
			}

			// If the branch has only one child, ensure it's in the Centre position.
			if (Descendants.Count == 1 && Descendants[EDockSite.Centre].Item == null)
			{
				var item = Descendants[0].Item;
				Descendants[0].Item = null;
				Descendants[EDockSite.Centre].Item = item;
			}

			// If the centre position is empty, and there is only one other descendant, move it to the centre.
			if (Descendants.Count == 2 && Descendants[EDockSite.Centre]?.Item is DockPane dp && dp.AllContent.Count == 0)
			{
				var item = Descendants[1].Item;
				Descendants[1].Item = null;
				Descendants[0].Item = null;
				Descendants[EDockSite.Centre].Item = item;
				Util.Dispose(ref dp!);
			}

			// Ensure there is always a centre pane
			if (Descendants[EDockSite.Centre].Item == null)
				Descendants[EDockSite.Centre].Item = new DockPane(DockContainer);

			// Check all the logic is correct
			Debug.Assert(ValidateTree());
		}

		/// <summary>Add branches to the tree until 'rest' is empty.</summary>
		private Branch GrowBranches(IEnumerable<EDockSite> address, out EDockSite last_ds)
		{
			var ds = address.First();
			var rest = address.Skip(1);

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
				return new_branch.GrowBranches(rest, out last_ds);
			}

			// If there's a dock pane at 'ds' swap it with a branch
			// containing that dock pane as the centre child
			if (Descendants[ds].Item is DockPane)
			{
				// Detach from the current location
				var existing = Descendants[ds].Item;
				Descendants[ds].Item = null;

				// Create a new branch with 'existing' in the centre
				var new_branch = new Branch(DockContainer, DockSizeData.Halves);
				new_branch.Descendants[EDockSite.Centre].Item = existing;

				// Replace in the current location
				Descendants[ds].Item = new_branch;
				return new_branch.GrowBranches(rest, out last_ds);
			}
			else
			{
				// Existing branch, recursive call into it
				var branch = (Branch?)Descendants[ds].Item ?? throw new Exception("Branch expected");
				return branch.GrowBranches(rest, out last_ds);
			}
		}

		/// <summary>Get the pane at 'location', adding branches to the tree if necessary</summary>
		public DockPane DockPane(IEnumerable<EDockSite> address)
		{
			// Grow the tree
			var branch = GrowBranches(address, out var ds);

			// No child at 'ds'? Add a dock pane.
			if (branch.Descendants[ds].Item == null)
			{
				var pane = new DockPane(DockContainer);
				branch.Descendants[ds].Item = pane;
			}

			// Existing pane at 'ds'
			return branch.Descendants[ds].Item is DockPane dp ? dp : throw new Exception($"DockPane expected at {string.Join(",", address)}");
		}
		public DockPane DockPane(EDockSite ds)
		{
			return DockPane(new[] { ds });
		}

		/// <summary>Get the child at 'location' or null if the given location is not a valid location in the tree</summary>
		public DescentantData? DescendantAt(IEnumerable<EDockSite> location)
		{
			var b = (Branch?)this;
			var c = (DescentantData?)null;
			foreach (var ds in location)
			{
				if (b == null) return null;
				c = b.Descendants[ds];
				b = c.Item as Branch;
			}
			return c;
		}

		/// <summary>Returns a reference to the dock sizes at a given level in the tree</summary>
		public DockSizeData? GetDockSizes(IEnumerable<EDockSite> location)
		{
			var b = (Branch?)this;
			foreach (var ds in location)
			{
				if (b == null) return null;
				b = b.Descendants[ds].Item as Branch;
			}
			return b?.DockSizes;
		}

		/// <summary>The dock panes or branches of this branch (5 per branch)</summary>
		internal DescendantCollection Descendants { get; }

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
				for (var b = (Branch?)this; b != null && b.Parent is Branch; b = b.Parent as Branch)
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
					foreach (var c in p.AllContent)
						yield return c;
			}
		}

		/// <summary>Get/Set the size for a dock site in this branch (in pixels)</summary>
		public double ChildSize(EDockSite location)
		{
			return DockSizes.GetSize(location, DisplayRectangle, DockedMask);
		}
		public void ChildSize(EDockSite location, double value)
		{
			if (value < 0 || double.IsNaN(value))
				throw new Exception($"Invalid child size ({value}) for {location}");
			if (RenderSize.Width == 0 || RenderSize.Height == 0)
				throw new Exception($"Invalid size for this branch, can't determine dock size for {location}");

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
				if (c is Branch b && b.Descendants[ds].Item is UIElement child)
					c = child;
				else
					throw new Exception($"Invalid child address");
			}
			return c;
		}

		/// <summary>Return the area that the control should cover when rendered</summary>
		private Rect DisplayRectangle => new Rect(RenderSize);

		/// <summary>Return the desired size of the control</summary>
		private Rect DesiredRectangle => new Rect(DesiredSize);

		/// <summary>Raised whenever a child (pane or branch) is added to or removed from this sub tree</summary>
		public event EventHandler<TreeChangedEventArgs>? TreeChanged;
		private void NotifyTreeChanged(TreeChangedEventArgs args)
		{
			TreeChanged?.Invoke(this, args);
			ParentBranch?.OnTreeChanged(args);
		}
		internal void OnTreeChanged(TreeChangedEventArgs args)
		{
			NotifyTreeChanged(args);
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
			return
				$"{TreeHost?.GetType()?.Name ?? "NoParent"}{Lvl(this)} " +
				$"C=[{Str(Descendants[EDockSite.Centre])}] " +
				$"L=[{Str(Descendants[EDockSite.Left  ])}] " +
				$"T=[{Str(Descendants[EDockSite.Top   ])}] " +
				$"R=[{Str(Descendants[EDockSite.Right ])}] " +
				$"B=[{Str(Descendants[EDockSite.Bottom])}]";

			static string Lvl(Branch branch)
			{
				var lvl = 0;
				for (var p = branch; p != null && p.Parent is Branch; p = (Branch)p.Parent) ++lvl;
				return lvl != 0 ? $"({lvl})" : string.Empty;
			}
			static string Str(DescentantData d)
			{
				if (d.Item is DockPane dp) return $"Pane({dp.CaptionText})";
				if (d.Item is Branch) return "Branch";
				return "null";
			}
		}

		/// <summary>A collection of branches or panes descendant from this branch</summary>
		[DebuggerDisplay("{m_branch.DumpDesc()}")]
		internal class DescendantCollection : IDisposable, IEnumerable<DescentantData>
		{
			/// <summary>The child controls (dock panes or branches) of this branch</summary>
			private readonly Branch m_branch;
			private readonly DescentantData[] m_descendants;

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

			/// <summary>Remove 'child' from the collection of descendants</summary>
			public void Remove(IPaneOrBranch child)
			{
				for (int i = 0; i != m_descendants.Length; ++i)
					if (m_descendants[i].Item == child)
						m_descendants[i].Item = null;
			}

			/// <summary>The number of non-null children in this collection</summary>
			public int Count => m_descendants.Count(x => x.Item != null);

			/// <summary>Access the non-null children in this collection</summary>
			public DescentantData this[int index] => m_descendants.Where(x => x.Item != null).ElementAt(index);

			/// <summary>Get a child control in a given dock site</summary>
			public DescentantData this[EDockSite ds] => m_descendants[(int)ds];

			/// <summary>Return the descendent data associated with 'c' (or null)</summary>
			public DescentantData? this[IPaneOrBranch c] => m_descendants.FirstOrDefault(x => x.Item == c);

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
		internal sealed class DescentantData :IDisposable
		{
			public DescentantData(Branch branch, EDockSite ds)
			{
				ParentBranch = branch;
				DockSite = ds;
			}
			public void Dispose()
			{
				Item = null;
			}

			/// <summary>The branch this is descendant from</summary>
			public Branch ParentBranch { get; }

			/// <summary>The site that this child represents</summary>
			public EDockSite DockSite { get; }

			/// <summary>A dock pane or branch</summary>
			public IPaneOrBranch? Item
			{
				get => m_item;
				set
				{
					if (m_item == value) return;
					if (m_item != null)
					{
						var item = (FrameworkElement)m_item;

						// Remove the pane or branch descendant
						// Note: do not dispose the pane or branch because this could be a reassignment to a different dock site
						ParentBranch.Children.Remove(item);
						ParentBranch.Children.Remove(m_splitter);

						// Notify of the tree change
						ParentBranch.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.Removed, pane: m_item as DockPane, branch: m_item as Branch));

						m_item = null!;
						m_splitter = null;
					}
					m_item = value;
					if (m_item != null)
					{
						var item = (FrameworkElement)m_item;

						// Check that 'item' doesn't already have a parent.
						// Can't just remove 'm_item' from it's parent, because there are other
						// things that happen when an item is removed, e.g. the associated splitter
						// is also removed, events generated, etc.
						if (m_item.ParentBranch != null)
							throw new Exception("Pane or Branch already has a parent");

						// Add the pane or branch as the descendant.
						// Note: the order of children is important. DockPanel assumes the last child is the "un-docked" child.
						// Also, the child elements are docked/clipped in reverse order, so Children[0] will have the smallest available area.
						// Also, the dock panel splitters operate on their immediate prior sibling.
						if (DockSite == EDockSite.Centre)
						{
							ParentBranch.Children.Add(item);
							item.Width = double.NaN;
							item.Height = double.NaN;
						}
						else
						{
							// Set the initial size of the docked item
							if (DockSite.IsVertical())
							{
								item.Width = ParentBranch.ChildSize(DockSite);
								item.Height = double.NaN;
							}
							else
							{
								item.Width = double.NaN;
								item.Height = ParentBranch.ChildSize(DockSite);
							}

							// Dock 'item'
							ParentBranch.Children.Insert2(0, item);
							DockPanel.SetDock(item, DockContainer.ToDock(DockSite));

							// Add a splitter control for non-centre items
							m_splitter = ParentBranch.Children.Insert2(1, new DockPanelSplitter
							{
								Thickness = 5.0,
								ProportionalResize = true,
								Background = SystemColors.ControlBrush,
								BorderBrush = new SolidColorBrush(Color.FromRgb(0xc0, 0xc0, 0xc0)),
								BorderThickness = DockSite.IsVertical() ? new Thickness(1, 0, 1, 0) : new Thickness(0, 1, 0, 1),
							});
							DockPanel.SetDock(m_splitter, DockContainer.ToDock(DockSite));
						}

						// Notify of the tree change
						ParentBranch.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.Added, pane: m_item as DockPane, branch: m_item as Branch));
					}
				}
			}
			private IPaneOrBranch? m_item;
			private DockPanelSplitter? m_splitter;
		}
	}
}
