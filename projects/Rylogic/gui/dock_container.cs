//**********************************************************************
// DockContainer
//  Copyright (c) Rylogic Limited 2015
//**********************************************************************
// Use:
//   Add a dock container to a form.
//   Create controls that implement 'IDockable'
//   Add the IDockable's to the dock container
//   Use DockSite on the IDockable to control where the item is docked
//
// Design:
//   A DockContainer is a tree of Branches and DockPanes. Each branch in the tree has five
//   children; Centre, Left, Top, Right, Bottom

//   A DockContainer has 5 permanently visible dock zones; Centre, Left, Right, Top, Bottom;
//    4 auto-hide dock zones; Left, Right, Top, Bottom; and any number of floating windows that
//    each contain a single dock zone.
//   Each dock zone contains a tree data structure (a binary space partition tree) in which the
//    leaves of the trees are the DockPanes.
//   Each DockPane contains a collection of IDockable content, a title bar, and a tab strip. A
//    DockPane only displays one IDockable item at a time which can be switched using tabs.
//   The DockContainer supports dragging of either DockPanes or individual IDockable items. When
//    an item is docked it is inserted into the tree based on the region of the Indicator that
//    mouse is over.
//
//   The API for docking an IDockable is to either add it directly to the dock container, specifying
//    an EDockSite, or to access a specific DockZone, traverse the tree of 'Divider's and insert the
//    IDockable in the located DockPane. IDockable's can be docked in the same DockPane as existing
//    IDockable's by accessing the DockPane accessor via 'DockControl'.
//
//   DockPane's can also be docked programmatically either via DockZone or by an existing DockPane.
//    In the first case, the pane is inserted into the tree at the location of the highest level DockPane
//    dividing it horizontally or vertically. In the second case, docking a pane at an existing DockPane's
//    location causes a branch to be inserted into the tree with the existing and new DockPane forming
//    the leaves.
//
//   DockZones, Dividers, and DockPanes are all created and destroyed on demand as the caller or user
//    moves content around the dock container.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;
using pr.win32;

namespace pr.gui
{
	/// <summary>A control that can be docked within a DockContainer</summary>
	public interface IDockable
	{
		/// <summary>The docking implementation object that provides the docking functionality</summary>
		DockControl DockControl { get; }
	}

	/// <summary>Locations in the dock container where dock panes or trees of dock panes, can be docked.</summary>
	public enum EDockSite
	{
		/// <summary>Docked to the main central area of the window</summary>
		Centre,

		/// <summary>Docked to the top</summary>
		Top,

		/// <summary>Docked to the bottom</summary>
		Bottom,

		/// <summary>Docked to the left side</summary>
		Left,

		/// <summary>Docked to the right side</summary>
		Right,

		/// <summary>Not docked</summary>
		None,
	}

	/// <summary>A dock container is the parent control that manages docking of controls that implement IDockable.</summary>
	public class DockContainer :Panel
	{
		/// <summary>The number of valid dock sites within a branch</summary>
		public static readonly int DockSiteCount = 5;

		/// <summary>The size of the tab strip for auto-hide zones</summary>
		public const int AutoHideStripSize = 30;

		/// <summary>Dock site mask value</summary>
		[Flags] public enum EDockMask
		{
			None   = 0,
			Centre = 1 << (int)EDockSite.Centre,
			Top    = 1 << (int)EDockSite.Top   ,
			Bottom = 1 << (int)EDockSite.Bottom,
			Left   = 1 << (int)EDockSite.Left  ,
			Right  = 1 << (int)EDockSite.Right ,
		}

		/// <summary>Create a new dock container</summary>
		public DockContainer()
		{
			Options = new OptionData();
			AnimTimer = new AnimationTimer(this);
			using (this.SuspendLayout(true))
				Root = new Branch(this, DockAreaData.Quarters);
		}
		protected override void Dispose(bool disposing)
		{
			Root = null;
			base.Dispose(disposing);
		}

		/// <summary>Options for the dock container</summary>
		public OptionData Options { get; private set; }

		/// <summary>
		/// Get/Set the active content. This will cause the pane that the content is on to also become active.
		/// To change the active content in a pane without making the pane active, assign to the pane's ActiveContent property</summary>
		public IDockable ActiveContent
		{
			get { return ActivePane?.ActiveContent; }
			set
			{
				if (ActiveContent == value) return;

				// Remember the old content
				var old = ActiveContent;

				// Switch panes
				ActivePane = value?.DockControl.DockPane;

				// Ensure 'value' is the active content on its pane
				if (ActivePane != null)
					ActivePane.ActiveContent = value;

				// Raise ActiveContentChanged
				OnActiveContentChanged(new ActiveContentChangedEventArgs(old, value));
			}
		}

		/// <summary>Enumerate all IDockable's managed by this container</summary>
		public IEnumerable<IDockable> Contents
		{
			get { return Panes.SelectMany(p => p.Content); }
		}

		/// <summary>Get/Set the active pane</summary>
		public DockPane ActivePane
		{
			get { return m_impl_active_pane; }
			set
			{
				if (m_impl_active_pane == value) return;

				var old = m_impl_active_pane;
				if (m_impl_active_pane != null)
				{
				}
				m_impl_active_pane = value;
				if (m_impl_active_pane != null)
				{
				}

				// Notify observers of each pane about activation changed
				old?.OnActivatedChanged();
				value?.OnActivatedChanged();

				// Notify that the active pane has changed
				OnActivePaneChanged(new ActivePaneChangedEventArgs(old, value));
			}
		}
		private DockPane m_impl_active_pane;

		/// <summary>Enumerate through all panes managed by this container</summary>
		public IEnumerable<DockPane> Panes
		{
			get { return Root.AllPanes; }
		}

		/// <summary>The root of the tree</summary>
		private Branch Root
		{
			get { return m_impl_root; }
			set
			{
				if (m_impl_root == value) return;
				using (this.SuspendLayout(true))
				{
					if (m_impl_root != null)
					{
						Controls.Remove(m_impl_root);
						Util.Dispose(ref m_impl_root);
					}
					m_impl_root = value;
					if (m_impl_root != null)
					{
						Controls.Add(m_impl_root);
					}
				}
			}
		}
		private Branch m_impl_root;

		/// <summary>
		/// Add a dockable instance to the container.
		/// Dock it at the location described by 'location'.
		/// Each location value is a dock site at that branch level in the tree</summary>
		public void Add(IDockable dockable, params EDockSite[] location)
		{
			if (dockable == null)
				throw new ArgumentNullException(nameof(dockable), "Cannot add 'null' content to a dock container");

			// If already on a pane, remove first
			dockable.DockControl.DockPane?.Content.Remove(dockable);

			// Notify of a content about to be added
			OnContentChanged(new ContentChangedEventArgs(ContentChangedEventArgs.EChg.Adding, dockable));

			// Use the default location if none has been given.
			var ds = location.Length != 0 ? location[0] : dockable.DockControl.DefaultDockSite;

			// Find the dock pane for this dock site, growing the tree as necessary
			var pane = Root.DockPane(ds, location.Skip(1));

			// Add the content
			pane.Content.Add(dockable);

			// Notify that a content has been added
			OnContentChanged(new ContentChangedEventArgs(ContentChangedEventArgs.EChg.Added, dockable));
		}

		/// <summary>Remove a dockable instance from this dock container</summary>
		public void Remove(IDockable dockable)
		{
			if (dockable == null)
				throw new ArgumentNullException(nameof(dockable), "Cannot remove null items from this dock container");
			if (dockable.DockControl.DockPane == null)
				return; // Idempotent remove
			if (dockable.DockControl.DockPane.DockContainer != this)
				throw new Exception("'dockable' is not a member of this dock container and so can't be removed");

			// Notify of content about to be removed
			OnContentChanged(new ContentChangedEventArgs(ContentChangedEventArgs.EChg.Removing, dockable));

			// Get the pane that 'dockable' belongs to and remove it.
			// If 'dockable' is the last content on the pane, the pane will self-remove
			// from it's containing DockZone. If that makes the DockZone empty, it will self-remove.
			dockable.DockControl.DockPane.Content.Remove(dockable);

			// Notify that a dockable has been removed
			OnContentChanged(new ContentChangedEventArgs(ContentChangedEventArgs.EChg.Removed, dockable));
		}

		/// <summary>Raised when panes are added or removed from the dock container</summary>
		public event EventHandler<PanesChangedEventArgs> PanesChanged;
		protected void OnPanesChanged(PanesChangedEventArgs args)
		{
			PanesChanged.Raise(this, args);
		}

		/// <summary>Raised when content is added or removed from the dock container</summary>
		public event EventHandler<ContentChangedEventArgs> ContentChanged;
		protected void OnContentChanged(ContentChangedEventArgs args)
		{
			ContentChanged.Raise(this, args);
		}

		/// <summary>Raised whenever the active pane changes in the dock container</summary>
		public event EventHandler<ActivePaneChangedEventArgs> ActivePaneChanged;
		private void OnActivePaneChanged(ActivePaneChangedEventArgs args)
		{
			ActivePaneChanged.Raise(this, args);
		}

		/// <summary>Raised whenever the active content for the dock container changes</summary>
		public event EventHandler<ActiveContentChangedEventArgs> ActiveContentChanged;
		private void OnActiveContentChanged(ActiveContentChangedEventArgs args)
		{
			ActiveContentChanged.Raise(this, args);
		}

		/// <summary>Layout the control</summary>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (Root != null)
			{
				using (this.SuspendLayout(false))
					Root.Bounds = DisplayRectangle;
			}

			base.OnLayout(levent);
		}

		/// <summary>Initiate dragging of a pane or content</summary>
		private void DragBegin(object item)
		{
			// Create a form for displaying the dock site locations and handling the drop of a pane or content
			using (var drop_handler = new DragHandler(this, item))
				drop_handler.ShowDialog(this);
		}

		/// <summary>A timer used for animating parts of the UI</summary>
		private AnimationTimer AnimTimer { get; set; }

		#region Custom Controls

		/// <summary>
		/// Represents a node in the tree of dock panes.
		/// Each node has 5 children; Centre, Left, Right, Top, Bottom</summary>
		private class Branch :Control
		{
			public Branch(DockContainer dc, DockAreaData dock_sizes)
			{
				SetStyle(ControlStyles.Selectable, false);
				SetStyle(ControlStyles.ContainerControl, true);
				DockContainer = dc;
				DockSizes = dock_sizes;

				// Create the collection to hold the five child controls (DockPanes or Branches)
				Child = new ChildCollection(this);
			}
			protected override void Dispose(bool disposing)
			{
				Child = Util.Dispose(Child);
				DockContainer = null;
				base.Dispose(disposing);
			}
			public override string ToString()
			{
				var lvl = 0;
				for (var p = (Control)this; p != null && p != DockContainer.Root; p = p.Parent) ++lvl;
				var c = Child[EDockSite.Centre].Ctrl;
				var l = Child[EDockSite.Left  ].Ctrl;
				var t = Child[EDockSite.Top   ].Ctrl;
				var r = Child[EDockSite.Right ].Ctrl;
				var b = Child[EDockSite.Bottom].Ctrl;
				return "Branch Lvl={0}  C=[{1}] L=[{2}] T=[{3}] R=[{4}] B=[{5}]".Fmt( lvl,
					c is Branch ? "Branch" : (c as DockPane)?.ToString() ?? "null",
					l is Branch ? "Branch" : (l as DockPane)?.ToString() ?? "null",
					t is Branch ? "Branch" : (t as DockPane)?.ToString() ?? "null",
					r is Branch ? "Branch" : (r as DockPane)?.ToString() ?? "null",
					b is Branch ? "Branch" : (b as DockPane)?.ToString() ?? "null");
			}

			/// <summary>The dock container that this pane belongs too</summary>
			public DockContainer DockContainer { get; private set; }

			/// <summary>The sizes of the child controls</summary>
			public DockAreaData DockSizes { get; private set; }

			/// <summary>Get the pane at 'location', adding branches to the tree if necessary</summary>
			public DockPane DockPane(EDockSite ds, IEnumerable<EDockSite> location)
			{
				// No child at this site? Add a dock pane.
				// Ignore remaining parts of the dock site address.
				if (Child[ds].Ctrl == null)
				{
					Child[ds].Ctrl = new DockPane(DockContainer);
					return (DockPane)Child[ds].Ctrl;
				}

				// If the child at 'ds' is an existing DockPane, then return it if
				// 'location.Length == 0' or substitute it for a branch
				if (Child[ds].Ctrl is DockPane)
				{
					// No more parts to the location address? return this pane
					if (!location.Any())
						return (DockPane)Child[ds].Ctrl;

					// Insert a branch and make 'Child[ds].Ctrl' the
					// child in the 'EDockSite.Centre' position.
					var new_branch = new Branch(DockContainer, ds == EDockSite.Centre ? DockAreaData.Quarters : DockAreaData.Halves);
					new_branch.Child[EDockSite.Centre].Ctrl = Child[ds].Ctrl;
					Child[ds].Ctrl = new_branch;
				}
				
				// If there is no more to the location address then add to the 'EDockSite.Centre' location
				var branch = (Branch)Child[ds].Ctrl;
				return branch.DockPane(location.FirstOrDefault(), location.Skip(1)); // Recursive call
			}

			/// <summary>The child controls (dock panes or branches) of this branch</summary>
			internal ChildCollection Child { get; private set; }
			internal class ChildCollection :IDisposable, IEnumerable<ChildCtrl>
			{
				/// <summary>The child controls (dock panes or branches) of this branch</summary>
				private ChildCtrl[] m_child;
				private Branch This;

				public ChildCollection(Branch @this)
				{
					This = @this;

					// Create an array to hold the five child controls (DockPanes or Branches)
					m_child = Util.NewArray(Enum<EDockSite>.Count, i => new ChildCtrl(This, (EDockSite)i));
				}
				public void Dispose()
				{
					Util.DisposeAll(ref m_child);
				}

				/// <summary>Get a child control in a given dock site</summary>
				public ChildCtrl this[EDockSite ds]
				{
					get { return m_child[(int)ds]; }
				}

				/// <summary>Enumerate the controls in the collection</summary>
				public IEnumerator<ChildCtrl> GetEnumerator()
				{
					foreach (var c in m_child)
						yield return c;
				}
				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}
			}

			/// <summary>A bitmap of the dock sites that have something docked</summary>
			public EDockMask DockedMask
			{
				get
				{
					var mask = EDockMask.None;
					foreach (var c in Child.Where(x => x.Ctrl != null))
						mask |= (EDockMask)(1 << (int)c.DockSite);
					return mask;
				}
			}

			/// <summary>Enumerates the branches in this sub-tree (breadth first)</summary>
			private IEnumerable<Branch> AllBranches
			{
				get
				{
					var queue = new Queue<Branch>();
					for (queue.Enqueue(this); queue.Count != 0; )
					{
						var div = queue.Dequeue();
						foreach (var c in Child.Where(x => x.Ctrl is Branch))
							queue.Enqueue((Branch)c.Ctrl);
						yield return div;
					}
				}
			}

			/// <summary>Enumerate the panes (leaves) in this sub-tree (breadth first)</summary>
			public IEnumerable<DockPane> AllPanes
			{
				get
				{
					foreach (var c in AllBranches.SelectMany(x => x.Child).Where(x => x.Ctrl is DockPane))
						yield return (DockPane)c.Ctrl;
				}
			}

			/// <summary>Get/Set the size for a dock site in this branch (in pixels)</summary>
			public int ChildSize(EDockSite location)
			{
				return DockSizes.GetSize(location, DisplayRectangle, DockedMask);
			}
			public void ChildSize(EDockSite location, int value)
			{
				DockSizes.SetSize(location, DisplayRectangle, value);
			}

			/// <summary>Get the bounds of a dock site in parent (DockContainer or containing Branch) space</summary>
			public Rectangle ChildBounds(EDockSite location)
			{
				return DockSiteBounds(location, DisplayRectangle, DockedMask, DockSizes);
			}

			/// <summary>Position the child controls</summary>
			protected override void OnLayout(LayoutEventArgs levent)
			{
				using (this.SuspendLayout(false))
				{
					// Position the child controls at each dock site
					foreach (var c in Child)
						c.Layout();
				}
				base.OnLayout(levent);
			}

			/// <summary>Wrapper of a child control (pane or branch) and associated splitter</summary>
			internal class ChildCtrl :IDisposable
			{
				private Branch This;

				public ChildCtrl(Branch @this, EDockSite ds)
				{
					This = @this;
					DockSite = ds;
				}
				public void Dispose()
				{
					var ctrl = Ctrl;
					Ctrl = null;
					Split = null;
					Util.Dispose(ref ctrl);
				}

				/// <summary>The site that this child represents</summary>
				public EDockSite DockSite { get; private set; }

				/// <summary>A dock pane or branch</summary>
				public Control Ctrl
				{
					get { return m_ctrl; }
					set
					{
						Debug.Assert(value == null || value is DockPane || value is Branch);

						if (m_ctrl == value) return;
						using (This.SuspendLayout(true))
						{
							if (m_ctrl != null)
							{
								// Remove the child control and associated splitter
								// Do not dispose 'm_ctrl', 'Ctrl' is a reference to a pane or branch
								// either can be assigned or unassigned here when manipulating the tree.
								This.Controls.Remove(m_ctrl);
								Split = null;
							}
							m_ctrl = value;
							if (m_ctrl != null)
							{
								// Add the child control and associated splitter
								This.Controls.Add(m_ctrl);
								This.Controls.SetChildIndex(m_ctrl, 100 + (int)DockSite);
								switch (DockSite) {
								case EDockSite.Left  : Split = new Splitter(Orientation.Vertical); break;
								case EDockSite.Right : Split = new Splitter(Orientation.Vertical); break;
								case EDockSite.Top   : Split = new Splitter(Orientation.Horizontal); break;
								case EDockSite.Bottom: Split = new Splitter(Orientation.Horizontal); break;
								}
							}
						}
					}
				}
				private Control m_ctrl;

				/// <summary>A splitter used to resize this child control</summary>
				private Splitter Split
				{
					get { return m_split; }
					set
					{
						if (m_split == value) return;
						using (This.SuspendLayout(true))
						{
							if (m_split != null)
							{
								m_split.DragEnd -= HandleResized;
								This.Controls.Remove(m_split);
							}
							m_split = value;
							if (m_split != null)
							{
								m_split.DragEnd += HandleResized;
								This.Controls.Add(m_split);
								This.Controls.SetChildIndex(m_split, 0);
							}
						}
					}
				}
				private Splitter m_split;

				/// <summary>Position this child control</summary>
				public void Layout()
				{
					if (Ctrl == null)
						return;

					// Get the region for this child
					var bounds = new RectangleRef(This.ChildBounds(DockSite));
					var rect = This.DisplayRectangle;

					// Set the area over which the splitter can move, position the splitter,
					// and resize the control to make room for the splitter
					switch (DockSite)
					{
					case EDockSite.Left:
						{
							Split.Area = Rectangle.FromLTRB(bounds.Left, bounds.Top, rect.Right, bounds.Bottom);
							Split.Position = bounds.Right - Split.BarWidth/2;
							Ctrl.Bounds = new RectangleRef(Split.BoundsLT);
							break;
						}
					case EDockSite.Right:
						{
							Split.Area = Rectangle.FromLTRB(rect.Left, bounds.Top, bounds.Right, bounds.Bottom);
							Split.Position = bounds.Left + Split.BarWidth/2;
							Ctrl.Bounds = new RectangleRef(Split.BoundsRB);
							break;
						}
					case EDockSite.Top:
						{
							Split.Area = Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, rect.Bottom);
							Split.Position = bounds.Bottom - Split.BarWidth/2;
							Ctrl.Bounds = new RectangleRef(Split.BoundsLT);
							break;
						}
					case EDockSite.Bottom:
						{
							Split.Area = Rectangle.FromLTRB(bounds.Left, rect.Top, bounds.Right, bounds.Bottom);
							Split.Position = bounds.Top + Split.BarWidth/2;
							Ctrl.Bounds = new RectangleRef(Split.BoundsRB);
							break;
						}
					case EDockSite.Centre:
						{
							Ctrl.Bounds = bounds;
							break;
						}
					}
				}

				/// <summary>Set the size of 'Ctrl' as 'Split' is moved</summary>
				private void HandleResized(object sender, EventArgs e)
				{
					var hw = Split.BarWidth / 2f;
					switch (DockSite) {
					case EDockSite.Left:   This.DockSizes[DockSite] =     (Split.Position + hw) / Split.Area.Width; break;
					case EDockSite.Right:  This.DockSizes[DockSite] = 1 - (Split.Position - hw) / Split.Area.Width; break;
					case EDockSite.Top:    This.DockSizes[DockSite] =     (Split.Position + hw) / Split.Area.Height; break;
					case EDockSite.Bottom: This.DockSizes[DockSite] = 1 - (Split.Position - hw) / Split.Area.Height; break;
					}
					This.PerformLayout();
				}
			}

			/// <summary>Output the tree structure as a string</summary>
			public void DumpTree(StringBuilder sb, int indent, string prefix)
			{
				sb.Append(' ', indent).Append(prefix).Append("Branch: ").AppendLine();
				foreach (var c in Child)
				{
					if      (c.Ctrl is Branch  ) ((Branch  )c.Ctrl).DumpTree(sb, indent + 4, "{0}=".Fmt(c.DockSite));
					else if (c.Ctrl is DockPane) ((DockPane)c.Ctrl).DumpTree(sb, indent + 4, "{0}=".Fmt(c.DockSite));
					else sb.Append(' ', indent + 4).Append("{0}=<null>".Fmt(c.DockSite)).AppendLine();
				}
			}
		}

		/// <summary>
		/// A pane groups a set of IDockable items together. Only one IDockable is displayed at a time in the pane,
		/// but tabs for all dockable items are displayed along the top or bottom.</summary>
		public class DockPane :Control
		{
			public DockPane(DockContainer owner)
			{
				if (owner == null)
					throw new ArgumentNullException("The owning dock container cannot be null");

				SetStyle(ControlStyles.Selectable, false);
				DockContainer = owner;

				// Create the collection that manages the content within this pane
				Content = new BindingSource<IDockable>{DataSource = new BindingListEx<IDockable>()};
				Content.ListChanging += HandleContentListChanged;
				Content.PositionChanging += HandleContentPositionChanged;

				using (this.SuspendLayout(true))
				{
					TabStripCtrl = new TabStripControl(this);
					TitleCtrl    = new PaneTitleControl(this);
				}
			}
			protected override void Dispose(bool disposing)
			{
				// Note: we don't own any of the content
				ActiveContent = null;
				TitleCtrl = null;
				TabStripCtrl = null;
				base.Dispose(disposing);
			}
			public override string ToString()
			{
				return Content.Count != 0 ? CaptionText : "<empty pane>";
			}

			/// <summary>The dock container that this pane belongs too</summary>
			public DockContainer DockContainer { get; private set; }

			/// <summary>The branch that contains this pane</summary>
			private Branch Branch { get { return Parent as Branch; } }

			/// <summary>The content hosted by this pane</summary>
			public BindingSource<IDockable> Content { get; private set; }

			/// <summary>
			/// The content in this pane that was last active (Not necessarily the active content for the dock container)
			/// There should always be active content while 'Content' is not empty. Empty panes are destroyed.
			/// If this pane is not the active pane, then setting the active content only raises events for this pane.
			/// If this is the active pane, then the dock container events are also raised.</summary>
			public IDockable ActiveContent
			{
				get { return m_impl_active_content; }
				set
				{
					if (m_impl_active_content == value || m_in_active_content) return;
					using (Scope.Create(() => m_in_active_content = true, () => m_in_active_content = false))
					{
						// Only content that is in this pane can be made active for this pane
						if (value != null && !Content.Contains(value))
							throw new Exception("Dockable item '{0}' has not been added to this pane so can not be made the active content.".Fmt(value.DockControl.PersistName));

						// Ensure 'value' is the current item in the Content collection
						Content.Current = value;

						// Switch to the new active content
						var old0 = m_impl_active_content;       // Get the currently active dockable in this pane
						var old1 = DockContainer.ActiveContent; // Get the currently active dockable in the dock container
						using (this.SuspendLayout(true))
						{
							if (m_impl_active_content != null)
							{
								// Remove from the child controls collection
								// Note: don't dispose the content, we don't own it
								Controls.Remove(m_impl_active_content.DockControl.Owner);
							}

							m_impl_active_content = value;

							if (m_impl_active_content != null)
							{
								// When content becomes active, add it as a child control of the pane
								Controls.Add(m_impl_active_content.DockControl.Owner);
							}
						}

						// Raise the active content changed event on this pane first, then on
						// the dock container if this pane is also the active pane for the container.
						OnActiveContentChanged(new ActiveContentChangedEventArgs(old0, value));
						if (DockContainer.ActivePane == this)
							DockContainer.OnActiveContentChanged(new ActiveContentChangedEventArgs(old1, value));
					}
				}
			}
			private IDockable m_impl_active_content;
			private bool m_in_active_content;

			/// <summary>Get/Set the dock site for this pane and all it's content.</summary>
			internal EDockSite DockSite
			{
				get
				{
					var child = (Parent as Branch)?.Child.FirstOrDefault(x => x.Ctrl == this);
					return child != null ? child.DockSite : EDockSite.None;
				}
			}

			/// <summary>Get the text to display in the title bar for this pane</summary>
			public string CaptionText
			{
				get { return ActiveContent?.DockControl.TabText ?? string.Empty; }
			}

			/// <summary>A control that draws the caption title bar for the pane</summary>
			public PaneTitleControl TitleCtrl
			{
				get { return m_impl_caption_ctrl; }
				set
				{
					if (m_impl_caption_ctrl == value) return;
					using (this.SuspendLayout(true))
					{
						if (m_impl_caption_ctrl != null)
						{
							Controls.Remove(m_impl_caption_ctrl);
							Util.Dispose(ref m_impl_caption_ctrl);
						}
						m_impl_caption_ctrl = value;
						if (m_impl_caption_ctrl != null)
						{
							Controls.Add(m_impl_caption_ctrl);
						}
					}
				}
			}
			private PaneTitleControl m_impl_caption_ctrl;

			/// <summary>A control that draws the tabs for this pane</summary>
			public TabStripControl TabStripCtrl
			{
				get { return m_impl_tab_strip_ctrl; }
				set
				{
					if (m_impl_tab_strip_ctrl == value) return;
					using (this.SuspendLayout(true))
					{
						if (m_impl_tab_strip_ctrl != null)
						{
							Controls.Remove(m_impl_tab_strip_ctrl);
							Util.Dispose(ref m_impl_tab_strip_ctrl);
						}
						m_impl_tab_strip_ctrl = value;
						if (m_impl_tab_strip_ctrl != null)
						{
							Controls.Add(m_impl_tab_strip_ctrl);
						}
					}
				}
			}
			private TabStripControl m_impl_tab_strip_ctrl;

			/// <summary>Get/Set this pane as activated. Activation causes the active content in this pane to be activated</summary>
			public bool Activated
			{
				get { return DockContainer.ActivePane == this; }
				set
				{
					// Assign this pane as the active one. This will cause the previously active pane
					// to have Activated = false called. Careful, need to handle 'Activated' or
					// 'DockContainer.ActivePane' being assigned to. The ActivePane handler will call OnActivatedChanged.
					DockContainer.ActivePane = value ? this : null;
				}
			}

			/// <summary>Raised whenever the active content for this dock pane changes</summary>
			public event EventHandler<ActiveContentChangedEventArgs> ActiveContentChanged;
			protected void OnActiveContentChanged(ActiveContentChangedEventArgs args)
			{
				ActiveContentChanged.Raise(this, args);
			}

			/// <summary>Raised whenever this dock pane is docked in a new location or floated.</summary>
			public event EventHandler<DockSiteChangedEventArgs> DockSiteChanged;
			private void OnDockStateChanged(DockSiteChangedEventArgs args)
			{
				DockSiteChanged.Raise(this, args);
			}

			/// <summary>Raised when the pane is activated</summary>
			public event EventHandler ActivatedChanged;
			internal void OnActivatedChanged()
			{
				ActivatedChanged.Raise(this, EventArgs.Empty);
			}

			/// <summary>Handle the list of content in this pane changing</summary>
			private void HandleContentListChanged(object sender, ListChgEventArgs<IDockable> e)
			{
				var dockable = e.Item;
				switch (e.ChangeType)
				{
				case ListChg.ItemAdded:
					{
						if (dockable == null)
							throw new ArgumentNullException(nameof(dockable), "Cannot add 'null' content to a dock pane");

						// Ensure the dockable is not currently added to another pane (or even this pane)
						dockable.DockControl.DockPane?.Content.Remove(dockable);

						// Set the owning dock pane for the dockable.
						// Don't add the dockable as a child control yet,
						// that happens when the active content changes
						dockable.DockControl.DockPane = this;

						// If this is the first dockable added, make it the active content
						if (Content.Count == 1)
							ActiveContent = Content.First();

						PerformLayout();
						break;
					}
				case ListChg.ItemRemoved:
					{
						// Clear the dock pane from the content
						Debug.Assert(dockable.DockControl.DockPane == this);
						dockable.DockControl.DockPane = null;

						PerformLayout();
						break;
					}
				}
			}

			/// <summary>Handle the 'Position' value in the content collection changing</summary>
			private void HandleContentPositionChanged(object sender, PositionChgEventArgs e)
			{
				ActiveContent = Content.Current;
			}

			/// <summary>Move all content and event handlers from 'pane' to this pane</summary>
			private void Cannibolise(ref DockPane pane)
			{
				using (this.SuspendLayout(true))
				{
					// If currently empty then we'll need to set the active content
					var set_active = Content.Count == 0;

					// Transfer the content in 'pane' to our collection
					Content.AddRange(pane.Content.ToList());

					// Move the event handlers
					// what about Content.ListChanging, etc..?
					ActiveContentChanged      += pane.ActiveContentChanged;
					ActivatedChanged          += pane.ActivatedChanged;
					DockSiteChanged           += pane.DockSiteChanged;
					pane.ActiveContentChanged  = null;
					pane.ActivatedChanged      = null;
					pane.DockSiteChanged       = null;

					// Set the active content in 'pane'
					if (set_active && Content.Count != 0)
						Content.Position = 0;
				}
			}

			/// <summary>Layout the pane</summary>
			protected override void OnLayout(LayoutEventArgs e)
			{
				// Measure the remaining space and use that for the active content
				var content_rect = DisplayRectangle;
				if (!content_rect.Size.IsEmpty)
				{
					// Position the title bar
					if (TitleCtrl != null)
					{
						var bounds = TitleCtrl.CalcBounds(content_rect);
						content_rect = content_rect.Subtract(bounds);
						TitleCtrl.Bounds = bounds;
						TitleCtrl.ButtonAutoHide.Visible = DockSite != EDockSite.Centre;
					}

					// Position the tab strip
					if (TabStripCtrl != null)
					{
						var bounds = TabStripCtrl.CalcBounds(content_rect);
						content_rect = content_rect.Subtract(bounds);
						TabStripCtrl.Bounds = bounds;
					}

					// Use the remaining area for content
					if (ActiveContent != null)
					{
						ActiveContent.DockControl.Owner.Bounds = content_rect;
					}
				}

				base.OnLayout(e);
			}

			[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			protected override void WndProc(ref Message m)
			{
				// Watch for mouse activate. WM_MOUSEACTIVATE is sent to the inactive window under the
				// mouse. If that window passes the message to DefWindowProc then the parent window gets it.
				// We need to intercept WM_MOUSEACTIVATE going to a content control so we can activate the pane.
				if (m.Msg == (int)Win32.WM_MOUSEACTIVATE)
					Activated = true;

				base.WndProc(ref m);
			}

			/// <summary>Output the tree structure as a string</summary>
			public void DumpTree(StringBuilder sb, int indent, string prefix)
			{
				sb.Append(' ',indent).Append(prefix).Append("Pane: ").Append(ToString()).AppendLine();
			}
		}

		/// <summary>Records the proportions used for each dock site at a branch level</summary>
		public class DockAreaData
		{
			/// <summary>The minimum size of a child control</summary>
			public const int MinChildSize = 20;

			public DockAreaData(float left = 0.25f, float top = 0.25f, float right = 0.25f, float bottom = 0.25f)
			{
				Left   = left;
				Top    = top;
				Right  = right;
				Bottom = bottom;
			}
			public DockAreaData(DockAreaData rhs) :this(rhs.Left, rhs.Top, rhs.Right, rhs.Bottom)
			{}

			/// <summary>
			/// The size of the left, top, right, bottom panes. If >= 1, then the value is interpreted
			/// as pixels, if less than 1 then interpreted as a fraction of the ClientRectangle width/height</summary>
			public float Left { get; set; }
			public float Top { get; set; }
			public float Right { get; set; }
			public float Bottom { get; set; }

			/// <summary>Get/Set the dock site size by EDockSite</summary>
			public float this[EDockSite ds]
			{
				get
				{
					switch (ds) {
					case EDockSite.Left:   return Left;
					case EDockSite.Right:  return Right;
					case EDockSite.Top:    return Top;
					case EDockSite.Bottom: return Bottom;
					}
					return 1f;
				}
				set
				{
					switch (ds) {
					case EDockSite.Left:   Left   = value; break;
					case EDockSite.Right:  Right  = value; break;
					case EDockSite.Top:    Top    = value; break;
					case EDockSite.Bottom: Bottom = value; break;
					}
				}
			}

			/// <summary>Get the sizes for the edge dock sites assuming the given available area</summary>
			public Padding GetSizesForRect(Rectangle rect, EDockMask docked_mask, Size? centre_min_size = null)
			{
				// Ensure the centre dock pane is never fully obscured
				var csize = new SizeRef(centre_min_size ?? Size.Empty);
				if (csize.Width  == 0) csize.Width  = MinChildSize;
				if (csize.Height == 0) csize.Height = MinChildSize;

				// See which dock sites actually have something docked
				var existsL = docked_mask.HasFlag(EDockMask.Left  );
				var existsR = docked_mask.HasFlag(EDockMask.Right );
				var existsT = docked_mask.HasFlag(EDockMask.Top   );
				var existsB = docked_mask.HasFlag(EDockMask.Bottom);

				// Get the size of each area
				var l = existsL ? (Left   >= 1f ? (int)Left   : (int)(rect.Width  * Left  )) : 0;
				var r = existsR ? (Right  >= 1f ? (int)Right  : (int)(rect.Width  * Right )) : 0;
				var t = existsT ? (Top    >= 1f ? (int)Top    : (int)(rect.Height * Top   )) : 0;
				var b = existsB ? (Bottom >= 1f ? (int)Bottom : (int)(rect.Height * Bottom)) : 0;

				// If opposite zones overlap, reduce the sizes
				var over_w = rect.Width  - (l + csize.Width  + r);
				var over_h = rect.Height - (t + csize.Height + b);
				if (over_w < 0)
				{
					if      (existsL && existsR) { l += over_w / 2; r += (over_w + 1) / 2; }
					else if (existsL)            { l = rect.Width - csize.Width; }
					else if (existsR)            { r = rect.Width - csize.Width; }
				}
				if (over_h < 0)
				{
					if      (existsT && existsB) { t += over_h / 2; b += (over_h + 1) / 2; }
					else if (existsT)            { t = rect.Height - csize.Height; }
					else if (existsB)            { b = rect.Height - csize.Height; }
				}

				// Return the sizes in pixels
				return new Padding(Math.Max(0,l), Math.Max(0,t), Math.Max(0,r), Math.Max(0,b));
			}

			/// <summary>Get the size for a dock site in pixels, assuming an available area 'rect'</summary>
			public int GetSize(EDockSite location, Rectangle rect, EDockMask docked_mask)
			{
				var area = GetSizesForRect(rect, docked_mask);

				// Return the size of the requested site
				switch (location) {
				default: throw new Exception("No size value for dock zone {0}".Fmt(location));
				case EDockSite.Centre: return 0;
				case EDockSite.Left:   return area.Left;
				case EDockSite.Right:  return area.Right;
				case EDockSite.Top:    return area.Top;
				case EDockSite.Bottom: return area.Bottom;
				}
			}

			/// <summary>Get the size for a dock site in pixels, assuming an available area 'rect'</summary>
			public void SetSize(EDockSite location, Rectangle rect, int value)
			{
				// Assign a fractional value for the dock site size
				switch (location) {
				default: throw new Exception("No size value for dock zone {0}".Fmt(location));
				case EDockSite.Centre: break;
				case EDockSite.Left:   Left   = value / (Left   >= 1f ? 1f : rect.Width); break;
				case EDockSite.Right:  Right  = value / (Right  >= 1f ? 1f : rect.Width); break;
				case EDockSite.Top:    Top    = value / (Top    >= 1f ? 1f : rect.Height); break;
				case EDockSite.Bottom: Bottom = value / (Bottom >= 1f ? 1f : rect.Height); break;
				}
			}

			public static DockAreaData Halves { get { return new DockAreaData(0.5f, 0.5f, 0.5f, 0.5f); } }
			public static DockAreaData Quarters { get { return new DockAreaData(0.25f, 0.25f, 0.25f, 0.25f); } }
		}

		/// <summary>General options for the dock container</summary>
		public class OptionData
		{
			public OptionData()
			{
				AllowUserDocking = true;
				IndicatorPadding = 15;
				TitleBar         = new TitleBarData();
				TabStrip         = new TabStripData();
			}

			/// <summary>Get/Set whether the user is allowed to drag and drop panes</summary>
			public bool AllowUserDocking { get; set; }

			/// <summary>The gap between the edge of the dock container and the edge docking indicators</summary>
			public int IndicatorPadding { get; set; }

			/// <summary>Options for the dock pane title bars</summary>
			public TitleBarData TitleBar { get; private set; }
			public class TitleBarData
			{
				public TitleBarData()
				{
					ActiveGrad= new ColourSet(
						text: SystemColors.ActiveCaptionText,
						beg: SystemColors.GradientActiveCaption,
						end: SystemColors.InactiveCaption);

					InactiveGrad = new ColourSet(
						text: SystemColors.InactiveCaptionText,
						beg: SystemColors.GradientInactiveCaption,
						end: SystemColors.InactiveCaption,
						mode: LinearGradientMode.Vertical);

					Padding  = new Padding(2, 1, 2, 1);
					TextFont = SystemFonts.MenuFont;
				}

				///<summary>Colour gradient for the caption title bar in an active DockPane</summary>
				public ColourSet ActiveGrad { get; private set; }

				///<summary>Colour gradient for the caption title bar in an inactive DockPane</summary>
				public ColourSet InactiveGrad { get; private set; }

				/// <summary>Margin for the caption title bar text and buttons</summary>
				public Padding Padding { get; set; }

				/// <summary>The font to use for caption title bar text</summary>
				public Font TextFont { get; set; }
			}

			/// <summary>Options for the dock pane tab strips</summary>
			public TabStripData TabStrip { get; private set; }
			public class TabStripData
			{
				public TabStripData()
				{
					ActiveStrip = new ColourSet(
						text: SystemColors.ActiveCaptionText,
						beg: SystemColors.Control.Lerp(Color.Black, 0.0f),
						end: SystemColors.Control.Lerp(Color.Black, 0.0f));

					InactiveStrip = new ColourSet(
						text: SystemColors.InactiveCaptionText,
						beg: ActiveStrip.Beg,
						end: ActiveStrip.Beg);

					ActiveTab = new ColourSet(
						text: SystemColors.WindowText,
						beg: SystemColors.Window,
						end: SystemColors.Window,
						border: SystemColors.ControlDark);

					InactiveTab = new ColourSet(
						text: SystemColors.WindowText,
						beg: InactiveStrip.Beg,
						end: InactiveStrip.Beg);

					ActiveFont        = SystemFonts.MenuFont.Dup(FontStyle.Bold);
					InactiveFont      = SystemFonts.MenuFont;
					MinWidth          = 20;
					MaxWidth          = 200;
					TabSpacing        = 2;
					IconSize          = 16;
					IconToTextSpacing = 1;
					StripPadding      = new Padding(2, 2, 2, 0);
					TabPadding        = new Padding(2, 2, 2, 2);
				}

				///<summary>Colour gradient for the tab strip background in an active DockPane</summary>
				public ColourSet ActiveStrip { get; private set; }

				///<summary>Colour gradient for the tab strip background in an inactive DockPane</summary>
				public ColourSet InactiveStrip { get; private set; }

				///<summary>Colour gradient for the tab strip background in an active DockPane</summary>
				public ColourSet ActiveTab { get; private set; }

				///<summary>Colour gradient for the tab strip background in an inactive DockPane</summary>
				public ColourSet InactiveTab { get; private set; }

				/// <summary>The font to use for the active tab</summary>
				public Font ActiveFont { get; set; }

				/// <summary>The font used on the inactive tabs</summary>
				public Font InactiveFont { get; set; }

				/// <summary>The minimum width that a tab can have</summary>
				public int MinWidth { get; set; }

				/// <summary>The maximum width that a tab can have</summary>
				public int MaxWidth { get; set; }

				/// <summary>The gap size between tabs</summary>
				public int TabSpacing { get; set; }

				/// <summary>The size of icons displayed on the tabs</summary>
				public int IconSize { get; set; }

				/// <summary>The space between the icon and text tab text</summary>
				public int IconToTextSpacing { get; set; }

				/// <summary>The space between the edge of the tab strip background edge and the tabs</summary>
				public Padding StripPadding { get; set; }

				/// <summary>The space between the tab edge and the tab text,icon,etc</summary>
				public Padding TabPadding { get; set; }
			}

			/// <summary>Colours for drawing title bars and tab strips</summary>
			public class ColourSet
			{
				public ColourSet(Color? text = null, Color? beg = null, Color? end = null, Color? border = null, LinearGradientMode mode = LinearGradientMode.Horizontal, Blend blend = null)
				{
					Text   = text ?? SystemColors.WindowText;
					Beg    = beg ?? SystemColors.Control;
					End    = end ?? SystemColors.Control;
					Border = border ?? Color.Empty;
					Mode   = mode;
					Blend  = blend ?? new Blend(2)
					{
						Factors = new float[] { 0.0f, 1.0f },
						Positions = new float[] { 0.0f, 1.0f },
					};
				}
				public override string ToString()
				{
					return "{0} -> {1}".Fmt(Beg.Name, End.Name);
				}

				/// <summary>The start colour for the gradient</summary>
				public Color Beg { get; set; }

				/// <summary>The end colour for the gradient</summary>
				public Color End { get; set; }

				/// <summary>The gradient direction</summary>
				public LinearGradientMode Mode { get; set; }

				/// <summary>The border colour</summary>
				public Color Border { get; set; }

				/// <summary>The colour of associated text</summary>
				public Color Text { get; set; }

				/// <summary>The blend mode</summary>
				public Blend Blend { get; set; }
			}
		}

		/// <summary>A basic splitter control</summary>
		private class Splitter :Control
		{
			public const int DefaultWidth = 4;
			public const int DefaultMinPaneSize = 20;

			public Splitter(Orientation ori = Orientation.Vertical)
			{
				SetStyle(ControlStyles.Selectable, false);
				BackColor = SystemColors.ControlDarkDark;
				BarWidth = DefaultWidth;
				MinPaneSize = DefaultMinPaneSize;
				Orientation = ori;
				Area = default(Rectangle);
			}

			/// <summary>Get/Set the area within the parent over which the splitter can move (in parent space). Set to default to use the parent.DisplayRectangle</summary>
			public Rectangle Area
			{
				get { return m_impl_area ?? Parent?.DisplayRectangle ?? Rectangle.Empty; }
				set
				{
					Debug.Assert(value.Area() >= 0);
					m_impl_area = value != default(Rectangle) ? (Rectangle?)value : null;
					UpdateSplitterBounds();
				}
			}
			private Rectangle? m_impl_area;

			/// <summary>The width of the splitter</summary>
			public virtual int BarWidth { get; set; }

			/// <summary>The minimum size that the LT or RB pane can have</summary>
			public virtual int MinPaneSize { get; set; }

			/// <summary>The orientation of the splitter</summary>
			public Orientation Orientation
			{
				get { return m_impl_ori; }
				set
				{
					m_impl_ori = value;
					Cursor = value ==  Orientation.Vertical ? Cursors.VSplit : Cursors.HSplit;
					UpdateSplitterBounds();
				}
			}
			private Orientation m_impl_ori;

			/// <summary>The position of the splitter within the Parent control (in pixels) relative to Area</summary>
			public virtual int Position
			{
				get { return m_impl_pos; }
				set
				{
					if (m_impl_pos == value) return;

					// Clamp the position within the allowed range
					var max = Orientation == Orientation.Vertical ? Area.Width : Area.Height;
					m_impl_pos = Maths.Clamp(value, 0, max);

					// Update the bounds of the splitter within the parent
					UpdateSplitterBounds();

					// Notify that the position has changed
					OnPositionChanged();
				}
			}
			private int m_impl_pos;

			/// <summary>Raised when the splitter is moved</summary>
			public event EventHandler PositionChanged;
			protected virtual void OnPositionChanged()
			{
				PositionChanged.Raise(this);
			}

			/// <summary>Raised on left mouse down to start a resizing operation</summary>
			public event EventHandler DragBegin;
			protected virtual void OnDragBegin()
			{
				DragBegin.Raise(this);
			}

			/// <summary>Raised on mouse move during a resizing operation (after Position has been updated)</summary>
			public event EventHandler Dragging;
			protected virtual void OnDragging()
			{
				Dragging.Raise(this);
			}

			/// <summary>Raised on left mouse up to end a resizing operation</summary>
			public event EventHandler DragEnd;
			protected virtual void OnDragEnd()
			{
				DragEnd.Raise(this);
			}

			/// <summary>Gets the bounds of the left/top side of the splitter (in parent control space)</summary>
			public Rectangle BoundsLT
			{
				get
				{
					if (Parent == null) return Rectangle.Empty;
					var pt = Bounds.Centre();
					var rc = Area;
					return Orientation == Orientation.Vertical
						? Rectangle.FromLTRB(rc.Left, rc.Top, pt.X - BarWidth/2, rc.Bottom)
						: Rectangle.FromLTRB(rc.Left, rc.Top, rc.Right, pt.Y - BarWidth/2);
				}
			}

			/// <summary>Gets the bounds of the right/bottom side of the splitter (in parent control space)</summary>
			public Rectangle BoundsRB
			{
				get
				{
					if (Parent == null) return Rectangle.Empty;
					var pt = Bounds.Centre();
					var rc = Area;
					return Orientation == Orientation.Vertical
						? Rectangle.FromLTRB(pt.X + BarWidth/2, rc.Top, rc.Right, rc.Bottom)
						: Rectangle.FromLTRB(rc.Left, pt.Y + BarWidth/2, rc.Right, rc.Bottom);
				}
			}

			/// <summary>Update the bounds of the splitter within the parent</summary>
			public void UpdateSplitterBounds()
			{
				if (Parent == null)
					return;

				// Assigning to Bounds causes a Layout to be performed
				// We don't want to force a layout, just move the splitter bar
				using (Parent.SuspendLayout(false))
				{
					// Position the splitter within the parent
					var rc = Area;
					Parent.Invalidate(Bounds);
					Bounds = Orientation == Orientation.Vertical
						? new Rectangle(Position - BarWidth/2, rc.Top, BarWidth, rc.Height)
						: new Rectangle(rc.Left, Position - BarWidth/2, rc.Width, BarWidth);
					Parent.Invalidate(Bounds);
					Parent.Update();
					Invalidate();
					Update();
				}
			}

			/// <summary>Set the position of the splitter based on client space point 'point_cs'</summary>
			private void MoveSplitter(Point point_cs)
			{
				if (Parent == null) return;
				var pt = Parent.PointToClient(PointToScreen(point_cs));
				var rc = Area;

				var max = Orientation == Orientation.Vertical ? rc.Width : rc.Height;
				var pos = Orientation == Orientation.Vertical ? pt.X - rc.Left : pt.Y - rc.Top;

				// Don't the user drag less than the min size
				if (max > 2 * MinPaneSize)
					Position = Maths.Clamp(pos, MinPaneSize, max - MinPaneSize);
			}

			/// <summary>Paint the splitter</summary>
			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);

				var gfx = e.Graphics;
				var col = Capture ? SystemBrushes.ControlDarkDark : SystemBrushes.Control;
				gfx.FillRectangle(col, ClientRectangle);
			}

			/// <summary>Handle dragging</summary>
			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);
				if (e.Button != MouseButtons.Left)
					return;

				Capture = true;
				if (Capture)
				{
					OnDragBegin();
					MoveSplitter(e.Location);
				}
			}
			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);

				if (Capture)
				{
					MoveSplitter(e.Location);
					OnDragging();
				}
			}
			protected override void OnMouseUp(MouseEventArgs e)
			{
				base.OnMouseUp(e);
				if (e.Button != MouseButtons.Left)
					return;

				if (Capture)
				{
					Capture = false;
					MoveSplitter(e.Location);
					OnDragEnd();
				}
			}

			/// <summary>Update the splitter position when re-parented</summary>
			protected override void OnParentChanged(EventArgs e)
			{
				base.OnParentChanged(e);
				UpdateSplitterBounds();
			}
		}

		/// <summary>A timer used for animating parts of the dock container</summary>
		private class AnimationTimer :Timer
		{
			public AnimationTimer(DockContainer dc)
			{
				Interval = 10;
				Enabled = false;
			}

			/// <summary>The owner of this timer</summary>
			public DockContainer DockContainer { get; private set; }

			/// <summary>Handle animation timer ticks</summary>
			public new event EventHandler Tick
			{
				add
				{
					if (m_tick == null) Enabled = true;
					m_tick += value;
				}
				remove
				{
					m_tick -= value;
					if (m_tick == null) Enabled = false;
				}
			}
			protected override void OnTick(EventArgs e)
			{
				m_tick.Raise(DockContainer, e);
			}
			private event EventHandler m_tick;
		}

		/// <summary>A custom control for drawing the title bar, including text, close button and pin button</summary>
		public class PaneTitleControl :Control
		{
			public PaneTitleControl(DockPane owner)
			{
				SetStyle(ControlStyles.Selectable, false);

				DockPane = owner;
				using (this.SuspendLayout(true))
				{
					ButtonClose = new CaptionButton(Resources.dock_close);
					ButtonAutoHide = new CaptionButton(IsAutoHide(DockPane.DockSite) ? Resources.dock_unpinned : Resources.dock_pinned);
				}
			}
			protected override void Dispose(bool disposing)
			{
				ButtonClose = null;
				ButtonAutoHide = null;
				DockPane = null;
				base.Dispose(disposing);
			}

			/// <summary>The dock pane that hosts this control</summary>
			public DockPane DockPane
			{
				get { return m_impl_dock_pane; }
				set
				{
					if (m_impl_dock_pane == value) return;
					if (m_impl_dock_pane != null)
					{
						m_impl_dock_pane.ActivatedChanged -= Invalidate;
					}
					m_impl_dock_pane = value;
					if (m_impl_dock_pane != null)
					{
						m_impl_dock_pane.ActivatedChanged += Invalidate;
					}
				}
			}
			private DockPane m_impl_dock_pane;

			/// <summary>The close button</summary>
			public CaptionButton ButtonClose
			{
				get { return m_impl_btn_close; }
				private set
				{
					if (m_impl_btn_close == value) return;
					if (m_impl_btn_close != null)
					{
						m_impl_btn_close.Click -= HandleClose;
						Controls.Remove(m_impl_btn_close);
						Util.Dispose(ref m_impl_btn_close);
					}
					m_impl_btn_close = value;
					if (m_impl_btn_close != null)
					{
						Controls.Add(m_impl_btn_close);
						m_impl_btn_close.Click += HandleClose;
					}
				}
			}
			private CaptionButton m_impl_btn_close;

			/// <summary>The auto hide button</summary>
			public CaptionButton ButtonAutoHide
			{
				get { return m_impl_btn_auto_hide; }
				private set
				{
					if (m_impl_btn_auto_hide == value) return;
					if (m_impl_btn_auto_hide != null)
					{
						m_impl_btn_auto_hide.Click -= HandleAutoHide;
						Controls.Remove(m_impl_btn_auto_hide);
						Util.Dispose(ref m_impl_btn_auto_hide);
					}
					m_impl_btn_auto_hide = value;
					if (m_impl_btn_auto_hide != null)
					{
						Controls.Add(m_impl_btn_auto_hide);
						m_impl_btn_auto_hide.Click += HandleAutoHide;
					}
				}
			}
			private CaptionButton m_impl_btn_auto_hide;

			/// <summary>Measure the height required by this control based on the content in 'DockPane'</summary>
			public int MeasureHeight()
			{
				var opts = DockPane.DockContainer.Options.TitleBar;
				return opts.TextFont.Height + opts.Padding.Top + opts.Padding.Bottom;
			}

			/// <summary>Returns the size of the pane title control given the available 'display_area'</summary>
			public Rectangle CalcBounds(Rectangle display_area)
			{
				// Title bars are always at the top
				var r = new RectangleRef(display_area);
				r.Bottom = r.Top + MeasureHeight();
				return r;
			}

			/// <summary>Perform a hit test on the title bar</summary>
			public EHitItem HitTest(Point pt)
			{
				if (ButtonClose.Visible && ButtonClose.Bounds.Contains(pt)) return EHitItem.CloseBtn;
				if (ButtonAutoHide.Visible && ButtonAutoHide.Bounds.Contains(pt)) return EHitItem.AutoHideBtn;
				return EHitItem.Caption;
			}
			public enum EHitItem { Caption, CloseBtn, AutoHideBtn }

			/// <summary>Paint the control</summary>
			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);

				// No area to draw in...
				if (ClientRectangle.Size.IsEmpty)
					return;

				// Draw the caption control
				var gfx = e.Graphics;
				var opts = DockPane.DockContainer.Options.TitleBar;

				// If the owner pane is activated, draw the 'active' title bar
				// Otherwise draw the inactive title bar
				var cols = DockPane.Activated ? opts.ActiveGrad : opts.InactiveGrad;
				using (var brush = new LinearGradientBrush(ClientRectangle, cols.Beg, cols.End, cols.Mode) { Blend=cols.Blend })
					gfx.FillRectangle(brush, e.ClipRectangle);

				// Calculate the area available for the caption text
				RectangleF r = ClientRectangle;
				r.X      += opts.Padding.Left;
				r.Y      += opts.Padding.Top;
				r.Width  -= opts.Padding.Left + opts.Padding.Right;
				r.Height -= opts.Padding.Top + opts.Padding.Bottom;
				if (ButtonClose.Visible)
					r.Width -= ButtonClose.Width + opts.Padding.Right;
				if (ButtonAutoHide.Visible)
					r.Width -= ButtonAutoHide.Width + opts.Padding.Right;
				r = RtlTransform(this, r);

				// Text rendering format
				var fmt = new StringFormat(StringFormatFlags.NoWrap | StringFormatFlags.FitBlackBox | (RightToLeft == RightToLeft.Yes ? StringFormatFlags.DirectionRightToLeft : 0));

				// Draw the caption text
				using (var bsh = new SolidBrush(cols.Text))
					gfx.DrawString(DockPane.CaptionText, opts.TextFont, bsh, r, fmt);
			}

			/// <summary>Layout the pane title bar</summary>
			protected override void OnLayout(LayoutEventArgs e)
			{
				using (this.SuspendLayout(false))
				{
					var opts = DockPane.DockContainer.Options.TitleBar;
					var r = ClientRectangle;
					var x = r.Right - 1;
					var y = r.Y + opts.Padding.Top;
					var h = r.Height - opts.Padding.Bottom - opts.Padding.Top;

					// Position the close button
					if (ButtonClose != null && !ButtonClose.Size.IsEmpty)
					{
						x -= opts.Padding.Right + ButtonClose.Width;
						var w = h * ButtonClose.Image.Width / ButtonClose.Image.Height;
						ButtonClose.Bounds = RtlTransform(this, new Rectangle(x, y, w, h));
					}

					// Position the auto hide button
					if (ButtonAutoHide != null && !ButtonAutoHide.Size.IsEmpty)
					{
						// Update the image based on dock state
						ButtonAutoHide.Image = IsAutoHide(DockPane.DockSite) ? Resources.dock_unpinned : Resources.dock_pinned;

						x -= opts.Padding.Right + ButtonAutoHide.Width;
						var w = h * ButtonAutoHide.Image.Width / ButtonAutoHide.Image.Height;
						ButtonAutoHide.Bounds = RtlTransform(this, new Rectangle(x, y, w, h));
					}

					Invalidate();
				}
				base.OnLayout(e);
			}

			/// <summary>Redo layout when RTL changes</summary>
			protected override void OnRightToLeftChanged(EventArgs e)
			{
				base.OnRightToLeftChanged(e);
				PerformLayout();
			}

			/// <summary>Start dragging the pane on mouse-down over the title</summary>
			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);

				// If left click on the caption and user docking is allowed, start a drag operation
				if (e.Button == MouseButtons.Left && DockPane.DockContainer.Options.AllowUserDocking)
				{
					// Record the mouse down location
					m_mouse_down_at = e.Location;
					Capture = true;
				}
			}
			protected override void OnMouseMove(MouseEventArgs e)
			{
				if (Capture && Util.Distance(e.Location, m_mouse_down_at) > 5)
				{
					Capture = false;

					// Begin dragging the pane
					DockPane.DockContainer.DragBegin(DockPane);
				}
				base.OnMouseMove(e);
			}
			protected override void OnMouseUp(MouseEventArgs e)
			{
				Capture = false;
				base.OnMouseUp(e);
			}
			private Point m_mouse_down_at;

			/// <summary>Helper overload of invalidate</summary>
			private void Invalidate(object sender, EventArgs e)
			{
				Invalidate();
			}

			/// <summary>Handle the close button being clicked</summary>
			private void HandleClose(object sender, EventArgs e)
			{
				if (DockPane.ActiveContent != null)
					DockPane.Content.Remove(DockPane.ActiveContent);
			}

			/// <summary>Handle the auto hide button being clicked</summary>
			private void HandleAutoHide(object sender, EventArgs e)
			{
				//DockPane.DockSite = ToggleAutoHide(DockPane.DockSite);
				//if (DockHelper.IsDockStateAutoHide(DockPane.DockState))
				//{
				//	DockPane.DockPanel.ActiveAutoHideContent = null;
				//	DockPane.NestedDockingStatus.NestedPanes.SwitchPaneWithFirstChild(DockPane);
				//}
			}
		}

		/// <summary>A custom button drawn on the title bar of a pane</summary>
		public class CaptionButton :Control
		{
			public CaptionButton(Bitmap image)
			{
				SetStyle(ControlStyles.SupportsTransparentBackColor, true);
				BackColor = Color.Transparent;
				Image = image;
			}

			/// <summary>The image to display on the button</summary>
			public Bitmap Image { get; set; }

			/// <summary>Get/Set when the mouse is over the button</summary>
			private bool IsMouseOver
			{
				get { return m_mouse_over; }
				set
				{
					if (m_mouse_over == value) return;
					m_mouse_over = value;
					Invalidate();
				}
			}
			private bool m_mouse_over;

			/// <summary>The default size of the control</summary>
			protected override Size DefaultSize
			{
				get { return Resources.dock_close.Size; }
			}

			/// <summary>Detect mouse over</summary>
			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				IsMouseOver = ClientRectangle.Contains(e.X, e.Y);
			}
			protected override void OnMouseEnter(EventArgs e)
			{
				base.OnMouseEnter(e);
				IsMouseOver = true;
			}
			protected override void OnMouseLeave(EventArgs e)
			{
				base.OnMouseLeave(e);
				IsMouseOver = false;
			}

			/// <summary>Paint the button</summary>
			protected override void OnPaint(PaintEventArgs e)
			{
				if (IsMouseOver && Enabled)
				{
					using (var pen = new Pen(ForeColor))
						e.Graphics.DrawRectangle(pen, ClientRectangle.Inflated(-1, -1));// Rectangle.Inflate(ClientRectangle, -1, -1));
				}

				// Paint the button bitmap
				using (var attr = new ImageAttributes())
				{
					attr.SetRemapTable(new[]{
						new ColorMap{OldColor = Color.FromArgb(0, 0, 0), NewColor = ForeColor },
						new ColorMap{OldColor = Image.GetPixel(0, 0), NewColor = Color.Transparent }});

					e.Graphics.DrawImage(Image, new Rectangle(0, 0, Image.Width, Image.Height), 0, 0, Image.Width, Image.Height, GraphicsUnit.Pixel, attr);
				}

				base.OnPaint(e);
			}
		}

		/// <summary>Base class for a custom control containing a strip of tabs</summary>
		public class TabStripControl :Control
		{
			public TabStripControl(DockPane owner)
			{
				SetStyle(ControlStyles.Selectable, false);
				AllowDrop = true;

				DockPane = owner;
				StripLocation = EDockSite.Bottom;
				StripHeight = PreferredHeight;
				GhostTabContent = null;
				GhostTabIndex = -1;
			}

			/// <summary>Override the window creation parameters</summary>
			protected override CreateParams CreateParams
			{
				get
				{
					var p = base.CreateParams;
					p.ClassStyle |= Win32.CS_DBLCLKS;
					return p;
				}
			}

			/// <summary>The dock pane that hosts this control</summary>
			public DockPane DockPane
			{
				get { return m_impl_dock_pane; }
				set
				{
					if (m_impl_dock_pane == value) return;
					if (m_impl_dock_pane != null)
					{
						m_impl_dock_pane.ActivatedChanged -= Invalidate;
					}
					m_impl_dock_pane = value;
					if (m_impl_dock_pane != null)
					{
						m_impl_dock_pane.ActivatedChanged += Invalidate;
					}
				}
			}
			private DockPane m_impl_dock_pane;

			/// <summary>The location of the tab strip within the dock pane, Only L,T,R,B are valid</summary>
			public EDockSite StripLocation
			{
				get { return m_impl_strip_loc; }
				set
				{
					if (m_impl_strip_loc == value) return;
					if (value != EDockSite.Left && value != EDockSite.Top && value != EDockSite.Right && value != EDockSite.Bottom)
						throw new Exception("Invalid tab strip location");

					m_impl_strip_loc = value;
					DockPane.PerformLayout();
				}
			}
			private EDockSite m_impl_strip_loc;

			/// <summary>The height of the tab strip</summary>
			public int StripHeight
			{
				get { return m_height; }
				set
				{
					if (m_height == value) return;
					m_height = value;
					Invalidate();
				}
			}
			private int m_height;

			/// <summary>Measure the height required by this control based on the content in 'DockPane'</summary>
			public int PreferredHeight
			{
				get
				{
					var opts = DockPane.DockContainer.Options.TabStrip;
					var tabh = opts.TabPadding.Top + Maths.Max(opts.InactiveFont.Height, opts.ActiveFont.Height, opts.IconSize) + opts.TabPadding.Bottom;
					return opts.StripPadding.Top + tabh + opts.StripPadding.Bottom;
				}
			}

			/// <summary>Get the transform to apply to the strip and tabs based on the StripLocation</summary>
			public Matrix Transform
			{
				get
				{
					var m = new Matrix();
					if (!Horizontal)
					{
						m.Translate(Width, 0, MatrixOrder.Prepend);
						m.Rotate(90f, MatrixOrder.Prepend);
					}
					return m;
				}
			}

			/// <summary>Returns the size of the tab strip control given the available 'display_area' and strip location</summary>
			public Rectangle CalcBounds(Rectangle display_area)
			{
				// No tab strip if there is only one item in the pane
				if (DockPane.Content.Count <= 1)
					return Rectangle.Empty;

				var r = new RectangleRef(display_area);
				switch (StripLocation) {
				default: throw new Exception("Tab strip location is not valid");
				case EDockSite.Top:    r.Bottom = r.Top    + StripHeight; break;
				case EDockSite.Bottom: r.Top    = r.Bottom - StripHeight; break;
				case EDockSite.Left:   r.Right  = r.Left   + StripHeight; break;
				case EDockSite.Right:  r.Left   = r.Right  - StripHeight; break;
				}
				return r;
			}

			/// <summary>Content this is about to be added to this tab strip</summary>
			public IDockable GhostTabContent
			{
				get { return m_ghost_tab; }
				set
				{
					if (m_ghost_tab == value) return;
					m_ghost_tab = value;
					Invalidate();
					Update();
				}
			}
			private IDockable m_ghost_tab;

			/// <summary>The index position in which 'GhostTab' would be added. Used when a dockable is about to be inserted into the tab strip</summary>
			public int GhostTabIndex
			{
				get { return GhostTabContent != null ? m_ghost_tab_index : -1; }
				set
				{
					if (m_ghost_tab_index == value) return;
					m_ghost_tab_index = value;
					Invalidate();
					Update();
				}
			}
			private int m_ghost_tab_index;

			/// <summary>The number of tabs (equal to the number of dockables in the dock pane + plus the ghost tab if present)</summary>
			public int TabCount
			{
				get { return DockPane.Content.Count + (GhostTabContent != null ? 1 : 0); }
			}

			/// <summary>True if the strip is orientated horizontally, false if vertically</summary>
			private bool Horizontal
			{
				get { return StripLocation == EDockSite.Top || StripLocation == EDockSite.Bottom; }
			}

			/// <summary>Gets the content in 'DockPane' with 'GhostTab' inserted at the appropriate position</summary>
			private IEnumerable<IDockable> Content
			{
				get
				{
					// Insert's GhostTab into the collection of content (handles GhostTabIndex == -1 correctly)
					foreach (var c in DockPane.Content.Take(GhostTabIndex))
					{
						if (c == GhostTabContent) continue;
						yield return c;
					}
					if (GhostTabContent != null && GhostTabIndex != -1)
					{
						yield return GhostTabContent;
					}
					foreach (var c in DockPane.Content.Skip(GhostTabIndex))
					{
						if (c == GhostTabContent) continue;
						yield return c;
					}
				}
			}

			/// <summary>Return the tabs corresponding to the content in 'DockPane' (in tab strip space, starting at 'FirstVisibleTabIndex', assuming horizontal layout)</summary>
			public IEnumerable<TabBtn> VisibleTabs
			{
				get
				{
					var opts = DockPane.DockContainer.Options.TabStrip;

					// The range along the tab strip in which tab buttons are allowed
					var xbeg = opts.StripPadding.Left;
					var xend = (Horizontal ? Width : Height) - opts.StripPadding.Right;

					using (var gfx = CreateGraphics())
					{
						// Iterate over the visible tabs
						var x = xbeg;
						var tab_index = FirstVisibleTabIndex;
						foreach (var content in Content.Skip(tab_index))
						{
							// Create a tab to represent the content
							var tab = new TabBtn(this, content, tab_index, gfx, x);

							// Hand out the tab
							yield return tab;

							// Advance to the next tab position
							x += tab.Bounds.Width + opts.TabSpacing;
							++tab_index;
							if (x > xend) break;
						}
					}
				}
			}

			/// <summary>The index of the first tab to display</summary>
			public int FirstVisibleTabIndex { get; set; }

			/// <summary>
			/// Returns the index of the tab under client space point 'pt'.
			/// Returns -1 if 'pt' is not within the tab strip, or 'TabCount' if at or after the end of the last tab</summary>
			public int HitTest(Point pt)
			{
				// Not within the tab strip?
				if (!ClientRectangle.Contains(pt))
					return -1;

				// Search through the tabs and return the index of the one that contains 'pt' (if any)
				var tab_index = FirstVisibleTabIndex;
				foreach (var tab in VisibleTabs)
				{
					var rect = Transform.TransformRect(tab.Bounds);
					if (Horizontal)
					{
						if (pt.X < rect.Right)
							return tab_index;
					}
					else
					{
						if (pt.Y < rect.Bottom)
							return tab_index;
					}
					++tab_index;
				}
				return TabCount;
			}

			/// <summary>Returns the dockable content under client space point 'pt', or null</summary>
			public IDockable HitTestContent(Point pt)
			{
				// Get the hit tab index
				var index = HitTest(pt);

				// If the index is the ghost tab, return that
				if (GhostTabContent != null && index == GhostTabIndex)
					return GhostTabContent;

				// If the index is greater than the ghost tab, adjust the index
				if (GhostTabContent != null && index > GhostTabIndex)
					--index;

				// Return the content associated with the bat
				if (index >= 0 && index < DockPane.Content.Count)
					return DockPane.Content[index];

				// Missed...
				return null;
			}

			/// <summary>Paint the TabStrip control</summary>
			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);

				var gfx = e.Graphics;

				// Rotate to draw left/right strips
				gfx.Transform = Transform;

				// Draw each tab
				foreach (var tab in VisibleTabs)
				{
					// Draw the tab
					tab.Paint(gfx);

					// Exclude the tab from the clip region
					gfx.ExcludeClip(tab.Bounds);
				}

				// Rotate to draw left/right strips
				gfx.ResetTransform();

				// Paint the background
				// If the owner pane is activated, draw the 'active' strip background otherwise draw the inactive one
				var opts = DockPane.DockContainer.Options.TabStrip;
				var cols = DockPane.Activated ? opts.ActiveStrip : opts.InactiveStrip;
				using (var brush = new LinearGradientBrush(ClientRectangle, cols.Beg, cols.End, cols.Mode) { Blend=cols.Blend })
					gfx.FillRectangle(brush, e.ClipRectangle);
			}

			/// <summary>Handle mouse click on the tab strip</summary>
			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);

				// If left click on a tab and user docking is allowed, start a drag operation
				if (e.Button == MouseButtons.Left && DockPane.DockContainer.Options.AllowUserDocking)
				{
					// Record the mouse down location
					m_mouse_down_at = e.Location;
					Capture = true;
				}
			}
			protected override void OnMouseMove(MouseEventArgs e)
			{
				if (Capture && Util.Distance(e.Location, m_mouse_down_at) > 5)
				{
					Capture = false;

					// Begin dragging the content associated with the tab
					var dockable = HitTestContent(m_mouse_down_at);
					DockPane.DockContainer.DragBegin(dockable);
				}
				base.OnMouseMove(e);
			}
			protected override void OnMouseUp(MouseEventArgs e)
			{
				// If the mouse is still captured, then threat this as a mouse click instead of a drag
				if (Capture)
				{
					Capture = false;

					// Activate the content associated with the tab
					var dockable = HitTestContent(m_mouse_down_at);
					if (dockable != null)
					{
						DockPane.ActiveContent = dockable;
						Invalidate();
					}
				}
				base.OnMouseUp(e);
			}
			private Point m_mouse_down_at;

			/// <summary>Helper overload of invalidate</summary>
			private void Invalidate(object sender, EventArgs e)
			{
				Invalidate();
			}

			/// <summary>A tab that displays the name of the content and it's icon</summary>
			public class TabBtn
			{
				/// <summary>
				/// 'gfx' is a Graphics instance used to measure the content of the tab.
				/// 'x' is the distance along the tab strip (in pixels) for this tab.</summary>
				internal TabBtn(TabStripControl strip, IDockable content, int index, Graphics gfx, int x)
				{
					Strip = strip;
					Content = content;
					Index = index;

					// Calculate the bounding rectangle that would contain this Tab in tab strip space.
					var opts = Strip.DockPane.DockContainer.Options.TabStrip;
					var active = Content == Strip.DockPane.DockContainer.ActiveContent;
					var font = active ? opts.ActiveFont : opts.InactiveFont;

					var sz = gfx.MeasureString(Text, font, opts.MaxWidth, FmtFlags);
					var w = opts.TabPadding.Left + (Icon != null ? opts.IconSize + opts.IconToTextSpacing : 0) + (int)(sz.Width + 0.5f) + opts.TabPadding.Right;
					Bounds = Rectangle.FromLTRB(x, opts.StripPadding.Top, x + w, Strip.StripHeight - opts.StripPadding.Bottom);
				}

				/// <summary>The tab strip that owns this tab</summary>
				public TabStripControl Strip { get; private set; }

				/// <summary>The content that this tab is associated with</summary>
				public IDockable Content { get; private set; }

				/// <summary>The index of this tab within the owning tab strip</summary>
				public int Index { get; private set; }

				/// <summary>The bounds of this tab button in horizontal tab strip space</summary>
				public Rectangle Bounds { get; private set; }

				/// <summary>Text to display on the tab</summary>
				public string Text
				{
					get { return Content?.DockControl.TabText ?? string.Empty; }
				}

				/// <summary>The icon to display on the tab</summary>
				public Image Icon
				{
					get { return Content?.DockControl.TabIcon; }
				}

				/// <summary>Get the text format flags to use when rendering text for this tab</summary>
				public StringFormat FmtFlags
				{
					get { return new StringFormat(StringFormatFlags.NoWrap | StringFormatFlags.FitBlackBox | (Strip.RightToLeft == RightToLeft.Yes ? StringFormatFlags.DirectionRightToLeft : 0)); }
				}

				/// <summary>Draw the tab</summary>
				public void Paint(Graphics gfx)
				{
					var opts = Strip.DockPane.DockContainer.Options.TabStrip;
					var active = Content == Strip.DockPane.DockContainer.ActiveContent;
					var cols = active ? opts.ActiveTab : opts.InactiveTab;
					var font = active ? opts.ActiveFont : opts.InactiveFont;
					var rect= Bounds;

					// Fill the background
					using (var brush = new LinearGradientBrush(rect, cols.Beg, cols.End, cols.Mode) { Blend=cols.Blend })
						gfx.FillRectangle(brush, rect);

					// Paint a border if a border colour is given
					if (cols.Border != Color.Empty)
						using (var pen = new Pen(cols.Border))
							gfx.DrawRectangle(pen, rect);

					var x = rect.X + opts.TabPadding.Left;

					// Draw the icon
					if (Icon != null)
					{
						var r = new Rectangle(x, (rect.Height - opts.IconSize)/2, opts.IconSize, opts.IconSize);
						gfx.DrawImage(Icon, r);
						x += r.Width + opts.IconToTextSpacing;
					}

					// Draw the text
					if (Text.HasValue())
					{
						var r = new RectangleF(x, (rect.Height - font.Height)/2, rect.Right - opts.TabPadding.Right - x, font.Height);
						using (var bsh = new SolidBrush(cols.Text))
							gfx.DrawString(Text, font, bsh, r, FmtFlags);
					}
				}
			}
		}

		/// <summary>An invisible modal window that manages dragging of panes or contents</summary>
		private class DragHandler :Form
		{
			// Create a static instance of the dock site bitmap. 'Resources.dock_site_cross' returns a new instance each time
			private static readonly Bitmap DockSiteCrossLg = Resources.dock_site_cross;
			private static readonly Bitmap DockSiteCrossSm = Resources.dock_site_cross_sm;
			private static readonly Bitmap DockSiteLeft    = Resources.dock_site_left;
			private static readonly Bitmap DockSiteTop     = Resources.dock_site_top;
			private static readonly Bitmap DockSiteRight   = Resources.dock_site_right;
			private static readonly Bitmap DockSiteBottom  = Resources.dock_site_bottom;
			private static readonly Size DraggeeOfs        = new Size(50, 30);
			private static readonly Size DraggeeSize       = new Size(150, 150);

			/// <summary>Places where panes/content can be dropped</summary>
			[Flags] private enum EDropSite
			{
				// Dock sites within the current pane
				Pane = 1 << 16,
				PaneCentre = Pane | EDockSite.Centre ,
				PaneLeft   = Pane | EDockSite.Left   ,
				PaneTop    = Pane | EDockSite.Top    ,
				PaneRight  = Pane | EDockSite.Right  ,
				PaneBottom = Pane | EDockSite.Bottom ,

				// Dock sites within the current branch
				Branch = 1 << 17,
				BranchCentre = Branch | EDockSite.Centre ,
				BranchLeft   = Branch | EDockSite.Left   ,
				BranchTop    = Branch | EDockSite.Top    ,
				BranchRight  = Branch | EDockSite.Right  ,
				BranchBottom = Branch | EDockSite.Bottom ,

				// Dock sites in the root level branch
				Root = 1 << 18, 
				RootCentre = Root | EDockSite.Centre ,
				RootLeft   = Root | EDockSite.Left   ,
				RootTop    = Root | EDockSite.Top    ,
				RootRight  = Root | EDockSite.Right  ,
				RootBottom = Root | EDockSite.Bottom ,

				// A mask the gets the EDockSite bits
				DockSiteMask = 0xff,
			}

			public DragHandler(DockContainer owner, object item)
			{
				Debug.Assert(item is DockPane || item is IDockable, "Only panes and content should be being dragged");
				DockContainer = owner;

				// Create a form with a null region so that we have an invisible modal dialog
				Name = "Drop handler";
				FormBorderStyle = FormBorderStyle.None;
				ShowInTaskbar = false;
				KeyPreview = true;
				Region = new Region(Rectangle.Empty);

				// Create the semi-transparent non-modal form for the dragged item
				Ghost = new GhostPane(item);

				// Create the dock site indicators
				IndCrossLg = new Indicator(this, DockSiteCrossLg) {Name = "Cross Large"};
				IndCrossSm = new Indicator(this, DockSiteCrossSm) {Name = "Cross Small"};
				IndLeft    = new Indicator(this, DockSiteLeft) {Name = "Left"};
				IndTop     = new Indicator(this, DockSiteTop) {Name = "Top"};
				IndRight   = new Indicator(this, DockSiteRight) {Name = "Right"};
				IndBottom  = new Indicator(this, DockSiteBottom) {Name = "Bottom"};

				// Set the positions of the edge docking indicators
				var opts = DockContainer.Options;
				IndLeft  .Location = DockContainer.PointToScreen(new Point(opts.IndicatorPadding, (DockContainer.Height - IndLeft.Height)/2));
				IndTop   .Location = DockContainer.PointToScreen(new Point((DockContainer.Width - IndTop.Width)/2, opts.IndicatorPadding));
				IndRight .Location = DockContainer.PointToScreen(new Point(DockContainer.Width - opts.IndicatorPadding - IndRight.Width, (DockContainer.Height - IndRight.Height)/2));
				IndBottom.Location = DockContainer.PointToScreen(new Point((DockContainer.Width - IndBottom.Width)/2, DockContainer.Height - opts.IndicatorPadding - IndBottom.Height));
			}
			protected override void Dispose(bool disposing)
			{
				DockContainer.Focus();
				Ghost      = null;
				IndCrossLg = null;
				IndCrossSm = null;
				IndLeft    = null;
				IndTop     = null;
				IndRight   = null;
				IndBottom  = null;
				base.Dispose(disposing);
			}
			protected override void OnShown(EventArgs e)
			{
				base.OnShown(e);
				Capture = true;
				Ghost.Show(this);
				HitTestDropLocations(MousePosition);
			}
			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				HitTestDropLocations(PointToScreen(e.Location));
			}
			protected override void OnMouseUp(MouseEventArgs e)
			{
				base.OnMouseUp(e);
				HitTestDropLocations(PointToScreen(e.Location));
				Close();
			}
			protected override void OnMouseCaptureChanged(EventArgs e)
			{
				base.OnMouseCaptureChanged(e);
				Close();
			}
			protected override void OnFormClosed(FormClosedEventArgs e)
			{
				base.OnFormClosed(e);
				Capture = false;
			}

			/// <summary>The dock container that created this drop handler</summary>
			private DockContainer DockContainer { get; set; }

			/// <summary>A form used as graphics to show dragged items</summary>
			private GhostPane Ghost
			{
				get { return m_impl_ghost; }
				set
				{
					if (m_impl_ghost == value) return;
					if (m_impl_ghost != null)
					{
						m_impl_ghost.Close();
						Util.Dispose(ref m_impl_cross_lg);
					}
					m_impl_ghost = value;
					if (m_impl_ghost != null) {}
				}
			}
			private GhostPane m_impl_ghost;

			/// <summary>The cross of dock site locations displayed within the centre of a pane</summary>
			private Indicator IndCrossLg
			{
				get { return m_impl_cross_lg; }
				set
				{
					if (m_impl_cross_lg == value) return;
					if (m_impl_cross_lg != null)
					{
						m_impl_cross_lg.Close();
						Util.Dispose(ref m_impl_cross_lg);
					}
					m_impl_cross_lg = value;
					if (m_impl_cross_lg != null) {}
				}
			}
			private Indicator m_impl_cross_lg;

			/// <summary>The small cross of dock site locations displayed within the centre of a pane</summary>
			private Indicator IndCrossSm
			{
				get { return m_impl_cross_sm; }
				set
				{
					if (m_impl_cross_sm == value) return;
					if (m_impl_cross_sm != null)
					{
						m_impl_cross_sm.Close();
						Util.Dispose(ref m_impl_cross_sm);
					}
					m_impl_cross_sm = value;
					if (m_impl_cross_sm != null) {}
				}
			}
			private Indicator m_impl_cross_sm;

			/// <summary>The left edge dock site indicator</summary>
			private Indicator IndLeft
			{
				get { return m_impl_left; }
				set
				{
					if (m_impl_left == value) return;
					if (m_impl_left != null)
					{
						m_impl_left.Close();
						Util.Dispose(ref m_impl_left);
					}
					m_impl_left = value;
					if (m_impl_left != null)
					{
						m_impl_left.Show(this);
					}
				}
			}
			private Indicator m_impl_left;

			/// <summary>The top edge dock site indicator</summary>
			private Indicator IndTop
			{
				get { return m_impl_top; }
				set
				{
					if (m_impl_top == value) return;
					if (m_impl_top != null)
					{
						m_impl_top.Close();
						Util.Dispose(ref m_impl_top);
					}
					m_impl_top = value;
					if (m_impl_top != null)
					{
						m_impl_top.Show(this);
					}
				}
			}
			private Indicator m_impl_top;

			/// <summary>The left edge dock site indicator</summary>
			private Indicator IndRight
			{
				get { return m_impl_right; }
				set
				{
					if (m_impl_right == value) return;
					if (m_impl_right != null)
					{
						m_impl_right.Close();
						Util.Dispose(ref m_impl_right);
					}
					m_impl_right = value;
					if (m_impl_right != null)
					{
						m_impl_right.Show(this);
					}
				}
			}
			private Indicator m_impl_right;

			/// <summary>The left edge dock site indicator</summary>
			private Indicator IndBottom
			{
				get { return m_impl_bottom; }
				set
				{
					if (m_impl_bottom == value) return;
					if (m_impl_bottom != null)
					{
						m_impl_bottom.Close();
						Util.Dispose(ref m_impl_bottom);
					}
					m_impl_bottom = value;
					if (m_impl_bottom != null)
					{
						m_impl_bottom.Show(this);
					}
				}
			}
			private Indicator m_impl_bottom;

			/// <summary>Enumerate all indicators</summary>
			private IEnumerable<Indicator> Indicators
			{
				get
				{
					yield return IndCrossLg;
					yield return IndCrossSm;
					yield return IndLeft;
					yield return IndTop;
					yield return IndRight;
					yield return IndBottom;
				}
			}

			/// <summary>Position the ghost to indicate where the content would be dropped</summary>
			private void HitTestDropLocations(Point screen_pt)
			{
				var pane_point = screen_pt;
				var over_indicator = false;
				var snap_to = (EDropSite?)null;
				var tab_index = -1;

				// Look for an indicator that the mouse is over. Do this before updating the indicator
				// positions because if the mouse is over an indicator, we don't want to move it.
				foreach (var ind in Indicators)
				{
					if (!ind.Visible)
						continue;

					// Get the mouse point in indicator space and test if it's within the indicator's region
					var pt = ind.PointToClient(screen_pt);
					if (!ind.Region.IsVisible(pt.X, pt.Y))
						continue;

					// See if the ghost should snap to a dock site
					snap_to = ind.CheckSnapTo(pt);
					pane_point = ind.Bounds.Centre();
					over_indicator = true;
					break;
				}

				// Look for the dock pane under the mouse
				var pane = PaneAtPoint(pane_point, DockContainer, GetChildAtPointSkip.None);

				// Check whether the mouse is over the tab strip of the dock pane
				if (pane != null && !over_indicator)
				{
					var strip = pane.TabStripCtrl;
					tab_index = strip.HitTest(strip.PointToClient(screen_pt)) - strip.FirstVisibleTabIndex;
					if (tab_index != -1)
						snap_to = EDropSite.PaneCentre;
				}

				// Update the position of the ghost
				Ghost.UpdatePosition(screen_pt, snap_to, pane, tab_index);

				// If the mouse isn't over an indicator, update the positions of the indicators
				if (!over_indicator)
					PositionCrossIndicator(pane);
			}

			/// <summary>Update the positions of the indicators. 'pane' is the pane under the mouse, or null</summary>
			private void PositionCrossIndicator(DockPane pane)
			{
				var opts = DockContainer.Options;

				// Update the position of the cross indicators
				if (pane != null)
				{
					// Display the large cross indicator when over the Centre site
					// or the small cross indicator when over an edge site
					if (pane.DockSite == EDockSite.Centre)
					{
						IndCrossLg.Location = pane.PointToScreen(pane.ClientRectangle.Centre() - IndCrossLg.Size.Scaled(0.5f));

						if (!IndCrossLg.Visible) IndCrossLg.Show(this);
						if ( IndCrossSm.Visible) IndCrossSm.Hide();
					}
					else
					{
						IndCrossSm.Location = pane.PointToScreen(pane.ClientRectangle.Centre() - IndCrossSm.Size.Scaled(0.5f));

						if ( IndCrossLg.Visible) IndCrossLg.Hide();
						if (!IndCrossSm.Visible) IndCrossSm.Show(this);
					}
				}
				else
				{
					if (IndCrossLg.Visible) IndCrossLg.Hide();
					if (IndCrossSm.Visible) IndCrossSm.Hide();
				}
			}

			/// <summary>A form that displays a docking site indicator</summary>
			private class Indicator :Form
			{
				private DragHandler m_owner;
				private Hotspot[] m_spots;

				public Indicator(DragHandler owner, Bitmap bmp)
				{
					FormBorderStyle = FormBorderStyle.None;
					StartPosition = FormStartPosition.Manual;
					BackgroundImageLayout = ImageLayout.None;
					BackgroundImage = bmp;
					ShowInTaskbar = false;
					Opacity = 0.5f;
					this.ClickThru(true);
					Region = new Region(BitmapToGraphicsPath(bmp));
					Size = bmp.Size;

					m_owner = owner;

					// Set the hotspot locations
					if (bmp == DockSiteCrossLg)
					{
						m_spots = new Hotspot[]
						{
							new Hotspot( 0,  0, EDropSite.PaneCentre  ),
							new Hotspot(-1,  0, EDropSite.PaneLeft    ),
							new Hotspot( 0, -1, EDropSite.PaneTop     ),
							new Hotspot(+1,  0, EDropSite.PaneRight   ),
							new Hotspot( 0, +1, EDropSite.PaneBottom  ),
							new Hotspot(-2,  0, EDropSite.BranchLeft  ),
							new Hotspot( 0, -2, EDropSite.BranchTop   ),
							new Hotspot(+2,  0, EDropSite.BranchRight ),
							new Hotspot( 0, +2, EDropSite.BranchBottom),
						};
					}
					else if (bmp == DockSiteCrossSm)
					{
						m_spots = new Hotspot[]
						{
							new Hotspot( 0,  0, EDropSite.PaneCentre  ),
							new Hotspot(-1,  0, EDropSite.PaneLeft    ),
							new Hotspot( 0, -1, EDropSite.PaneTop     ),
							new Hotspot(+1,  0, EDropSite.PaneRight   ),
							new Hotspot( 0, +1, EDropSite.PaneBottom  ),
						};
					}
					else if (bmp == DockSiteLeft)
					{
						m_spots = new Hotspot[] { new Hotspot(0,0, EDropSite.RootLeft) };
					}
					else if (bmp == DockSiteTop)
					{
						m_spots = new Hotspot[] { new Hotspot(0,0, EDropSite.RootTop) };
					}
					else if (bmp == DockSiteRight)
					{
						m_spots = new Hotspot[] { new Hotspot(0,0, EDropSite.RootRight) };
					}
					else if (bmp == DockSiteBottom)
					{
						m_spots = new Hotspot[] { new Hotspot(0,0, EDropSite.RootBottom) };
					}
					else
					{
						Debug.Assert(false, "Unknown dock site bitmap");
					}
					
					// Debug - Draw hot spots
					//Paint += (s,a) => m_spots.ForEach(h => a.Graphics.FillRectangle(Brushes.Red, h.Area.Shifted(Width/2,Height/2)));
				}
				protected override CreateParams CreateParams
				{
					get
					{
						var cp = base.CreateParams;
						cp.ClassStyle |= Win32.CS_DROPSHADOW;
						cp.ExStyle |= Win32.WS_EX_LAYERED | Win32.WS_EX_NOACTIVATE | Win32.WS_EX_TOOLWINDOW;
						return cp;
					}
				}
				protected override void OnShown(EventArgs e)
				{
					base.OnShown(e);
					BringToFront();
				}
				protected override void OnKeyDown(KeyEventArgs e)
				{
					base.OnKeyDown(e);
					if (e.KeyCode == Keys.Escape)
						m_owner.Close();
				}

				/// <summary>Indicators never receive focus</summary>
				protected override bool ShowWithoutActivation
				{
					get { return true; }
				}

				/// <summary>Test 'pt' against the hotspots on this indicator</summary>
				public EDropSite? CheckSnapTo(Point pt)
				{
					pt -= Size.Scaled(0.5f);
					return m_spots.FirstOrDefault(x => x.Area.Contains(pt))?.DropSite;
				}

				/// <summary>A region within the indicate that corresponds to a dock site location</summary>
				private class Hotspot
				{
					public const int Size = 22;
					public const int Gap = 0;

					public Hotspot(int i, int j, EDropSite ds)
					{
						// Hard coded to 32x32 spots, spaced 4 pixels apart
						Area = new Rectangle(i*(Size+Gap) - Size/2, j*(Size+Gap) - Size/2, Size, Size);
						DropSite = ds;
					}

					/// <summary>The area in the indicator that maps to 'DockSite'</summary>
					public Rectangle Area;

					/// <summary>The drop site that this hot spot corresponds to</summary>
					public EDropSite DropSite;
				}
			}

			/// <summary>A form that acts as a graphic while a pane or content is being dragged</summary>
			private class GhostPane :Form
			{
				private Region m_region;

				public GhostPane(object item)
				{
					SetStyle(ControlStyles.Selectable, false);

					// Use a form because the outline needs to be dragged outside the parent window
					StartPosition = FormStartPosition.Manual;
					FormBorderStyle = FormBorderStyle.None;
					ShowInTaskbar = false;
					Opacity = 0.5f;
					BackColor = SystemColors.ActiveCaption;

					m_region = Region;
					DraggedItem = item;
					SetHoveredPane(null, -1);
				}
				protected override void Dispose(bool disposing)
				{
					SetHoveredPane(null, -1);
					base.Dispose(disposing);
				}
				protected override CreateParams CreateParams
				{
					get
					{
						var cp = base.CreateParams;
						cp.ExStyle |= Win32.WS_EX_NOACTIVATE | Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT; // Transparent and click-through
						return cp;
					}
				}

				/// <summary>The item being dragged (either a DockPane or IDockable)</summary>
				private object DraggedItem { get; set; }

				/// <summary>The dockable to use when inserting the dragged item into another pane</summary>
				private IDockable DraggedDockable
				{
					get { return (DraggedItem as IDockable) ?? ((DockPane)DraggedItem).ActiveContent; }
				}

				/// <summary>The dock pane this ghost was last over</summary>
				private void SetHoveredPane(DockPane pane, int tab_index)
				{
					if (m_impl_pane != pane && m_impl_pane != null)
					{
						m_impl_pane.TabStripCtrl.GhostTabContent = null;
						m_impl_pane.TabStripCtrl.GhostTabIndex = -1;
					}
					m_impl_pane = pane;
					if (m_impl_pane != null)
					{
						m_impl_pane.TabStripCtrl.GhostTabContent = DraggedDockable;
						m_impl_pane.TabStripCtrl.GhostTabIndex = tab_index;
					}
				}
				private DockPane m_impl_pane;

				/// <summary>Position the ghost form at the location implied by 'snap_to'</summary>
				public void UpdatePosition(Point screen_pt, EDropSite? snap_to, DockPane pane, int tab_index)
				{
					// If no snap_to is given, use the default bounds and region
					if (snap_to == null)
					{
						Bounds = new Rectangle(screen_pt - DraggeeOfs, DraggeeSize);
						Region = m_region;
					}

					// Otherwise change the shape of this ghost form to match 'snap_to'
					else if (pane != null)
					{
						var snap = snap_to.Value;
						var ds = (EDockSite)(snap & EDropSite.DockSiteMask);
						var dm = (EDockMask)(1 << (int)ds);

						// All EDockSite.Centre's dock to the centre of 'pane'
						if (ds == EDockSite.Centre)
						{
							Bounds = pane.RectangleToScreen(pane.ClientRectangle);
						}
						else if (snap.HasFlag(EDropSite.Pane))
						{
							// Snap to a site within the current pane
							// Calculate the bounds assuming a branch replaces 'pane'
							var dock_size = DockAreaData.Halves;
							var bounds = DockSiteBounds(ds, pane.ClientRectangle, dm, dock_size);
							Bounds = pane.RectangleToScreen(bounds);
						}
						else if (snap.HasFlag(EDropSite.Branch))
						{
							// Snap to a site in the branch that owns 'pane'
							// Snap to an edge dock site in pane's containing branch
							var branch = (Branch)pane.Parent;
							var bounds = DockSiteBounds(ds, branch.ClientRectangle, branch.DockedMask | dm, branch.DockSizes);
							Bounds = branch.RectangleToScreen(bounds);
						}
						else if (snap.HasFlag(EDropSite.Root))
						{
							// Snap to an edge dock site at the root branch level
							var branch = pane.DockContainer.Root;
							var bounds = DockSiteBounds(ds, branch.ClientRectangle, branch.DockedMask | dm, branch.DockSizes);
							Bounds = pane.DockContainer.RectangleToScreen(bounds);
						}
					}

					// Update the pane being hovered
					SetHoveredPane(pane, tab_index);

					// If a pane is being hovered, limit the shape of the ghost to the pane content area
					if (pane != null && snap_to != null)
					{
						var region = new Region(ClientRectangle);
						region.Exclude(pane.TitleCtrl.Bounds);
						region.Exclude(pane.TabStripCtrl.Bounds);

						// If a tab on the dock pane is being hovered, include that area in the ghost form's region
						if (tab_index != -1)
						{
							var strip = pane.TabStripCtrl;
							var visible_index = Maths.Min(tab_index, strip.TabCount - 1) - strip.FirstVisibleTabIndex;
							var ghost_tab = strip.VisibleTabs.Skip(visible_index).FirstOrDefault();
							if (ghost_tab != null)
							{
								var bounds = strip.Transform.TransformRect(ghost_tab.Bounds);
								var rect = pane.RectangleToClient(strip.RectangleToScreen(bounds));
								region.Union(rect);
							}
						}

						Region = region;
					}
				}
			}
		}

		#endregion

		#region Utility methods

		private static bool IsAutoHide(EDockSite ds)
		{
			return false;
		}
		private static EDockSite ToggleAutoHide(EDockSite ds)
		{
			return ds;
		}

		/// <summary>Return a pane belonging to 'dc' at the given screen space point (or null)</summary>
		private static DockPane PaneAtPoint(Point pt, DockContainer dc, GetChildAtPointSkip flags)
		{
			// WindowFromPoint returns the parent Form. If the IDockables used in the dock container
			// are Forms then they will be found by WindowFromPoint, otherwise the form containing the 
			// dock container will be found. In the first case we need to go up the hierarchy to the parent
			// dock container, in the second case we need to search down the hierarchy for a child dock
			// container and its contained panes.

			// Find the window (Form) that contains the point
			var wind = Control.FromHandle(Win32.WindowFromPoint(pt));
			if (wind == null)
				return null;

			// Find the child control at 'pt'
			var ctrl = wind.GetChildAtScreenPointRec(pt, flags);

			// Search up the hierarchy for a pane
			for (; ctrl != null; ctrl = ctrl.Parent)
			{
				// If we hit a dockable, jump straight to its dock pane
				var content = ctrl as IDockable;
				if (content != null && content.DockControl.DockContainer == dc)
					return content.DockControl.DockPane;

				// Otherwise if we hit a dock pane...
				var pane = ctrl as DockPane;
				if (pane != null && pane.DockContainer == dc)
					return pane;
			}
			return null;
		}

		/// <summary>Get the bounds of a dock site. 'rect' is the available area. 'docked_mask' are the </summary>
		private static Rectangle DockSiteBounds(EDockSite location, Rectangle rect, EDockMask docked_mask, DockAreaData dock_site_sizes)
		{
			var area = dock_site_sizes.GetSizesForRect(rect, docked_mask);

			// Get the initial rectangle for the dock site assuming no other docked children
			var r = Rectangle.Empty;
			switch (location) {
			default: throw new Exception("No bounds value for dock zone {0}".Fmt(location));
			case EDockSite.Centre: r = rect; break;
			case EDockSite.Left:   r = new Rectangle(rect.Left, rect.Top, area.Left, rect.Height); break;
			case EDockSite.Right:  r = new Rectangle(rect.Right - area.Right, rect.Top, area.Right, rect.Height); break;
			case EDockSite.Top:    r = new Rectangle(rect.Left, rect.Top, rect.Width, area.Top); break;
			case EDockSite.Bottom: r = new Rectangle(rect.Left, rect.Bottom - area.Bottom, rect.Width, area.Bottom); break;
			}

			// Remove areas from 'r' for sites with EDockSite values greater than 'location' (if present).
			// Note: order of subtracting areas is important, subtract from highest priority to lowest
			// so that the result is always still a rectangle.
			for (var i = (EDockSite)DockSiteCount - 1; i > location; --i)
			{
				if (!docked_mask.HasFlag((EDockMask)(1 << (int)i))) continue;
				var sub = DockSiteBounds(i, rect, docked_mask, dock_site_sizes);
				r = r.Subtract(sub);
			}

			// Return the bounds of the dock zone for 'location'
			return r;
		}

		/// <summary>Convert 'point' to Right To Left layout if 'control' is in RTL mode</summary>
		private static Point RtlTransform(Control control, Point point)
		{
			return control.RightToLeft == RightToLeft.Yes ? new Point(control.Right - point.X, point.Y) : point;
		}

		/// <summary>Convert 'rect' to Right To Left layout if 'control' is in RTL mode</summary>
		private static Rectangle RtlTransform(Control control, Rectangle rect)
		{
			return control.RightToLeft == RightToLeft.Yes ? new Rectangle(control.ClientRectangle.Right - rect.Right, rect.Y, rect.Width, rect.Height) : rect;
		}

		/// <summary>Convert 'rect' to Right To Left layout if 'control' is in RTL mode</summary>
		private static RectangleF RtlTransform(Control control, RectangleF rect)
		{
			return control.RightToLeft == RightToLeft.Yes ? new RectangleF(control.ClientRectangle.Right - rect.Right, rect.Y, rect.Width, rect.Height) : rect;
		}

		/// <summary>Generate a GraphicsPath from a bitmap</summary>
		private static GraphicsPath BitmapToGraphicsPath(Bitmap bitmap)
		{
			GraphicsPath gp;
			if (!m_bm_to_gfx_path.TryGetValue(bitmap, out gp))
				m_bm_to_gfx_path.Add(bitmap, gp = bitmap.ToGraphicsPath());
			return gp;
		}
		private static Dictionary<Bitmap, GraphicsPath> m_bm_to_gfx_path = new Dictionary<Bitmap, GraphicsPath>();

		/// <summary>Output the tree structure</summary>
		public string DumpTree()
		{
			var sb = new StringBuilder();
			Root.DumpTree(sb, 0, string.Empty);
			return sb.ToString();
		}

		#endregion
	}

	/// <summary>Provides the implementation of the docking functionality</summary>
	public class DockControl
	{
		/// <summary>Create the docking functionality helper.</summary>
		/// <param name="owner">The control that docking is being provided for</param>
		/// <param name="persist_name">The name to use for this instance when saving the layout to XML</param>
		public DockControl(Control owner, string persist_name, EDockSite default_dock_site = EDockSite.Centre)
		{
			if (!(owner is IDockable))
				throw new Exception("'owner' must implement IDockable");

			Owner           = owner;
			PersistName     = persist_name;
			DefaultDockSite = default_dock_site;
			TabText         = null;
			TabIcon         = null;
		}

		/// <summary>Get the control we're providing docking functionality for</summary>
		public Control Owner
		{
			get { return m_impl_owner; }
			private set
			{
				if (m_impl_owner == value) return;
				if (m_impl_owner != null)
				{
					// Focus only works on the child control that gets focus
					//m_impl_owner.GotFocus -= HandleGotFocus;
				}
				m_impl_owner = value;
				if (m_impl_owner != null)
				{
					//m_impl_owner.GotFocus += HandleGotFocus;
				}
			}
		}
		private Control m_impl_owner;

		/// <summary>Get/Set the pane that this content resides within.</summary>
		public DockContainer.DockPane DockPane
		{
			get { return m_impl_pane; }
			set
			{
				if (m_impl_pane == value || m_in_dock_pane) return;
				using (Scope.Create(() => m_in_dock_pane = true, () => m_in_dock_pane = false))
				{
					if (m_impl_pane != null)
					{
						m_impl_pane.Content.Remove((IDockable)Owner);
					}
					m_impl_pane = value;
					if (m_impl_pane != null)
					{
						m_impl_pane.Content.Add((IDockable)Owner);
					}
				}
			}
		}
		private DockContainer.DockPane m_impl_pane;
		private bool m_in_dock_pane;

		/// <summary>Get/Set the pane that this content resides within.</summary>
		public DockContainer DockContainer
		{
			get { return DockPane?.DockContainer; }
			set
			{
				if (DockContainer == value) return;
				DockContainer?.Remove((IDockable)Owner);
				value?.Add((IDockable)Owner);
			}
		}

		/// <summary>The name to use for this instance when saving layout to XML</summary>
		public string PersistName { get; private set; }

		/// <summary>The dock state to use if not otherwise given</summary>
		public EDockSite DefaultDockSite { get; private set; }

		/// <summary>
		/// Get/Set the dock site within the DockContainer. Setting the dock site will move
		/// the content to the largest DockPane in the target DockSite</summary>
		public EDockSite DockSite
		{
			get { return DockPane?.DockSite ?? EDockSite.Centre; }
			set
			{
				if (DockSite == value) return;

				// Must have an existing pane
				if (DockPane == null)
					throw new Exception("Can only change the dock site of this item after it has been added to a dock container");

				// Save the previous dock site
				var old = DockSite;

				// Get the owning dock container
				var dc = DockPane.DockContainer;

				// Add this content to the requested dock site
				dc.Add((IDockable)Owner, value);

				// Notify observers
				OnDockStateChanged(new DockSiteChangedEventArgs(old, value));
			}
		}

		/// <summary>Remove this dockable from whatever dock pane it's in</summary>
		public void Remove()
		{
			if (DockPane == null) return;
			DockContainer.Remove((IDockable)Owner);
		}

		/// <summary>
		/// Raised whenever this dockable item is docked in a new location or floated
		/// 'sender' is the owner of the DockControl whose state changed.</summary>
		public event EventHandler<DockSiteChangedEventArgs> DockStateChanged;
		internal void OnDockStateChanged(DockSiteChangedEventArgs args)
		{
			DockStateChanged.Raise(Owner, args);
		}

		/// <summary>The text to display on the tab. Defaults to 'Owner.Text'</summary>
		public string TabText
		{
			get { return m_impl_tab_text ?? Owner.Text; }
			set { m_impl_tab_text = value; }
		}
		private string m_impl_tab_text;

		/// <summary>The icon to display on the tab. Defaults to '(Owner as Form)?.Icon'</summary>
		public Image TabIcon
		{
			get
			{
				// Note: not using Icon as the type for TabIcon because GDI+ doesn't support
				// drawing icons. The DrawIcon function calls the win API function DrawIconEx
				// which doesn't handle transforms properly

				// If an icon image has been specified, return that
				if (m_impl_icon != null) return m_impl_icon;

				// If the content is a Form and it has an icon, use that
				var form_icon = (Owner as Form)?.Icon;
				if (form_icon != null && m_impl_form_icon != form_icon)
				{
					m_impl_form_icon = form_icon;
					m_impl_icon = form_icon.ToBitmap();
				}

				// Otherwise, no icon
				return null;
			}
			set { m_impl_icon = value; }
		}
		private Image m_impl_icon;
		private Icon m_impl_form_icon;

		/// <summary></summary>
		private void HandleGotFocus(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}
	}

	#region Event Args

	/// <summary>Args for the DockStateChanged event</summary>
	public class DockSiteChangedEventArgs :EventArgs
	{
		public DockSiteChangedEventArgs(EDockSite old, EDockSite nue)
		{
			StateOld = old;
			StateNew = nue;
		}

		/// <summary>The old dock state</summary>
		public EDockSite StateOld { get; private set; }

		/// <summary>The new dock state</summary>
		public EDockSite StateNew { get; private set; }
	}

	/// <summary>Args for when the active content on the dock container or dock pane changes</summary>
	public class ActiveContentChangedEventArgs :EventArgs
	{
		public ActiveContentChangedEventArgs(IDockable old, IDockable nue)
		{
			ContentOld = old;
			ContentNew = nue;
		}

		/// <summary>The content that was active</summary>
		public IDockable ContentOld { get; private set; }

		/// <summary>The content that is becoming active</summary>
		public IDockable ContentNew { get; private set; }
	}

	/// <summary>Args for when the active content on the dock container or dock pane changes</summary>
	public class ActivePaneChangedEventArgs :EventArgs
	{
		public ActivePaneChangedEventArgs(DockContainer.DockPane old, DockContainer.DockPane nue)
		{
			PaneOld = old;
			PaneNew = nue;
		}

		/// <summary>The pane that was active</summary>
		public DockContainer.DockPane PaneOld { get; private set; }

		/// <summary>The pane that is becoming active</summary>
		public DockContainer.DockPane PaneNew { get; private set; }
	}

	/// <summary>Args for when IDockables are added or removed from a dock container</summary>
	public class ContentChangedEventArgs :EventArgs
	{
		public ContentChangedEventArgs(EChg what, IDockable who)
		{
			Change = what;
			Content = who;
		}

		/// <summary>The type of change that occurred</summary>
		public EChg Change { get; private set; }
		public enum EChg { Adding, Added, Removing, Removed }

		/// <summary>The content item involved in the change</summary>
		public IDockable Content { get; private set; }
	}

	/// <summary>Args for when DockPanes are added or removed from a dock container</summary>
	public class PanesChangedEventArgs :EventArgs
	{
		public PanesChangedEventArgs(EChg what, DockContainer.DockPane who)
		{
			Change = what;
			Pane = who;
		}

		/// <summary>The type of change that occurred</summary>
		public EChg Change { get; private set; }
		public enum EChg { Adding, Added, Removing, Removed }

		/// <summary>The content item involved in the change</summary>
		public DockContainer.DockPane Pane { get; private set; }
	}
	#endregion
}
