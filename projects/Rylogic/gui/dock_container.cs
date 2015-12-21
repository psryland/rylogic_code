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
using System.Xml.Linq;
using pr.common;
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;
using pr.win32;

namespace pr.gui
{
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

	/// <summary>A control that can be docked within a DockContainer</summary>
	public interface IDockable
	{
		/// <summary>The docking implementation object that provides the docking functionality</summary>
		DockControl DockControl { get; }
	}

	/// <summary>A dock container is the parent control that manages docking of controls that implement IDockable.</summary>
	public class DockContainer :Panel ,DockContainer.ITreeHost
	{
		/// <summary>The number of valid dock sites</summary>
		private const int DockSiteCount = 5;

		/// <summary>Implementation of ActiveContent and ActivePane, shared with floating windows and auto hide windows</summary>
		private ActiveContentImpl m_active_content;

		/// <summary>Create a new dock container</summary>
		public DockContainer()
		{
			Options = new OptionData();
			m_all_content = new HashSet<IDockable>();
			m_floaters = new BindingListEx<FloatingWindow>();
			m_auto_hide = new AutoHidePanel[DockSiteCount];
			m_active_content = new ActiveContentImpl(this);

			using (this.SuspendLayout(false))
			{
				Root = new Branch(this, DockSizeData.Quarters);
				for (var i = 1; i != m_auto_hide.Length; ++i) // skip centre
				{
					m_auto_hide[i] = new AutoHidePanel(this, (EDockSite)i);
					Controls.Add(m_auto_hide[i]);
					Controls.SetChildIndex(m_auto_hide[i], 0);
				}
			}
		}
		protected override void Dispose(bool disposing)
		{
			Root = null;
			m_all_content.Clear();
			m_floaters = Util.DisposeAll(m_floaters);
			m_auto_hide = Util.DisposeAll(m_auto_hide);
			base.Dispose(disposing);
		}

		/// <summary>The dock container that owns this instance</summary>
		DockContainer ITreeHost.DockContainer { get { return this; } }

		/// <summary>Options for the dock container</summary>
		public OptionData Options { get; private set; }

		/// <summary>Get/Set the globally active content (i.e. owned by this dock container). This will cause the pane that the content is on to also become active.</summary>
		public IDockable ActiveContent
		{
			get { return m_active_content.ActiveContent; }
			set { m_active_content.ActiveContent = value; } // Note: this setter isn't called when the user clicks on some content, ActivePane.set is called.
		}

		/// <summary>Get/Set the globally active pane (i.e. owned by this dock container). Note, this pane may be within the dock container, a floating window, or an auto hide window</summary>
		public DockPane ActivePane
		{
			get { return m_active_content.ActivePane; }
			set { m_active_content.ActivePane = value; }
		}

		/// <summary>Enumerate through all panes managed by this container</summary>
		public IEnumerable<DockPane> AllPanes
		{
			get
			{
				// Return all the panes hosted in the dock container
				foreach (var p in Root.AllPanes)
					yield return p;

				// Return all panes in the auto hide windows
				foreach (var ah in AutoHidePanels)
					foreach (var p in ah.Root.AllPanes)
						yield return p;

				// Return all panes hosted in the floating windows
				foreach (var fw in FloatingWindows)
					foreach (var p in fw.Root.AllPanes)
						yield return p;
			}
		}

		/// <summary>Enumerate all IDockable's managed by this container</summary>
		public IEnumerable<IDockable> AllContent
		{
			get { return m_all_content; }
		}
		private HashSet<IDockable> m_all_content;

		/// <summary>Save a reference to 'dockable' in this dock container</summary>
		internal void Remember(IDockable dockable)
		{
			m_all_content.Add(dockable);
		}

		/// <summary>Release the reference to 'dockable' in this dock container</summary>
		internal void Forget(IDockable dockable)
		{
			m_all_content.Remove(dockable);
		}

		/// <summary>The root of the tree in this dock container</summary>
		private Branch Root
		{
			[DebuggerStepThrough] get { return m_impl_root; }
			set
			{
				if (m_impl_root == value) return;
				using (this.SuspendLayout(true))
				{
					if (m_impl_root != null)
					{
						m_impl_root.TreeChanged -= HandleTreeChanged;
						Controls.Remove(m_impl_root);
						Util.Dispose(ref m_impl_root);
					}
					m_impl_root = value;
					if (m_impl_root != null)
					{
						Controls.Add(m_impl_root);
						m_impl_root.TreeChanged += HandleTreeChanged;
					}
				}
			}
		}
		private Branch m_impl_root;
		Branch ITreeHost.Root { get { return Root; } }

		/// <summary>Add a dockable instance to this branch at the position described by 'location'.</summary>
		public void Add(IDockable dockable, params EDockSite[] location)
		{
			Root.Add(dockable, location);
		}

		/// <summary>Raised whenever the active pane changes in this dock container</summary>
		public event EventHandler<ActivePaneChangedEventArgs> ActivePaneChanged
		{
			add    { m_active_content.ActivePaneChanged += value; }
			remove { m_active_content.ActivePaneChanged -= value; }
		}
		private void OnActivePaneChanged(ActivePaneChangedEventArgs args)
		{
			m_active_content.OnActivePaneChanged(args);
		}

		/// <summary>Raised whenever the active content changes in this dock container</summary>
		public event EventHandler<ActiveContentChangedEventArgs> ActiveContentChanged
		{
			add    { m_active_content.ActiveContentChanged += value; }
			remove { m_active_content.ActiveContentChanged -= value; }
		}
		private void OnActiveContentChanged(ActiveContentChangedEventArgs args)
		{
			m_active_content.OnActiveContentChanged(args);
		}

		/// <summary>Layout the control</summary>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			var rect = new RectangleRef(DisplayRectangle);
			if (rect.Area() > 0 && Root != null)
			{
				using (this.SuspendLayout(false))
				{
					// Set the bounds of the auto hide panels
					// These bounds in the popped out panel if visible
					foreach (var p in AutoHidePanels)
						p.Bounds = p.CalcBounds(rect);

					// Get the area for the dock container's Root tree.
					// This is aligned to the strip part of the auto hide panels.
					foreach (var p in AutoHidePanels)
					{
						switch (p.StripLocation) {
						case EDockSite.Left:   rect.Left   += p.StripSize; break;
						case EDockSite.Right:  rect.Right  -= p.StripSize; break;
						case EDockSite.Top:    rect.Top    += p.StripSize; break;
						case EDockSite.Bottom: rect.Bottom -= p.StripSize; break;
						}
					}
					
					// Set the bounds of the main content area
					Root.Bounds = rect;
				}
			}
			base.OnLayout(levent);
		}

		/// <summary>Initiate dragging of a pane or content</summary>
		private static void DragBegin(object item)
		{
			Debug.Assert(item is DockPane || item is IDockable, "Only panes and content should be being dragged");
			var parent =
				(item as IDockable)?.DockControl.DockContainer ??
				(item as DockPane)?.DockContainer;

			// Create a form for displaying the dock site locations and handling the drop of a pane or content
			using (var drop_handler = new DragHandler(parent, item))
				drop_handler.ShowDialog(parent);
		}

		/// <summary>The floating windows associated with this dock container</summary>
		public IEnumerable<FloatingWindow> FloatingWindows { get { return m_floaters; } }
		private BindingListEx<FloatingWindow> m_floaters;

		/// <summary>Returns an existing floating window that has a root branch in 'root', or returns a new floating window</summary>
		private FloatingWindow GetFloatingWindow(Func<FloatingWindow, bool> pred)
		{
			foreach (var fw in FloatingWindows)
			{
				if (!pred(fw)) continue;
				return fw;
			}
			return m_floaters.Add2(new FloatingWindow(this));
		}

		/// <summary>Release floating windows that have no content</summary>
		public void PurgeCachedFloatingWindows()
		{
			foreach (var fw in m_floaters.ToArray())
			{
				if (fw.ActiveContent != null) continue;
				m_floaters.Remove(fw);
				Util.Dispose(fw);
			}
		}

		/// <summary>Panels that auto hide when their contained tree does not contain the active pane</summary>
		public IEnumerable<AutoHidePanel> AutoHidePanels { get { return m_auto_hide.Skip(1); } }
		private AutoHidePanel[] m_auto_hide;

		/// <summary>Returns an existing floating window that has a root branch in 'root', or returns a new floating window</summary>
		private AutoHidePanel GetAutoHidePanel(EDockSite site)
		{
			return m_auto_hide[(int)site];
		}

		/// <summary>Handler for when panes are added/removed from the tree</summary>
		private void HandleTreeChanged(object sender, TreeChangedEventArgs obj)
		{
			switch (obj.Action)
			{
			case TreeChangedEventArgs.EAction.ActiveContent:
				{
					if (obj.DockPane == ActivePane)
						ActiveContent = obj.Dockable;
					break;
				}
			case TreeChangedEventArgs.EAction.Added:
				{
					PerformLayout();
					break;
				}
			case TreeChangedEventArgs.EAction.Removed:
				{
					PerformLayout();
					break;
				}
			}
		}

		/// <summary>Creates an instance of a menu with options to select specific content</summary>
		public ToolStripMenuItem WindowsMenu(string menu_name = "Windows")
		{
			var menu = new ToolStripMenuItem(menu_name);
			var filter = Util.FileDialogFilter("Layout Files","*.xml");

			// Load layout from disk
			var load = menu.DropDownItems.Add2(new ToolStripMenuItem("Load Layout"));
			load.Click += (ss,aa) =>
			{
				using (var fd = new OpenFileDialog { Title = "Load layout", Filter = filter })
				{
					if (fd.ShowDialog(this) != DialogResult.OK) return;
					try { LoadLayout(XDocument.Load(fd.FileName).Root); }
					catch (Exception ex) { MsgBox.Show(this, "Layout could not be loaded\r\n{0}".Fmt(ex.Message), "Load Layout Failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
				}
			};

			// Save the layout to disk
			var save = menu.DropDownItems.Add2(new ToolStripMenuItem("Save Layout"));
			save.Click += (ss,aa) =>
			{
				using (var fd = new SaveFileDialog { Title = "Save layout", Filter = filter, FileName = "Layout.xml", DefaultExt = "xml" })
				{
					if (fd.ShowDialog(this) != DialogResult.OK) return;
					try { SaveLayout().Save(fd.FileName); }
					catch (Exception ex) { MsgBox.Show(this, "Layout could not be saved\r\n{0}".Fmt(ex.Message), "Save Layout Failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
				}
			};

			menu.DropDownItems.Add(new ToolStripSeparator());

			// Repopulate the content items on drop down
			menu.DropDownOpening += (s,a) =>
			{
				// Menu item for each content
				for (;menu.DropDownItems.Count > 3; menu.DropDownItems.RemoveAt(3)) {}
				foreach (var content in AllContent.OrderBy(x => x.DockControl.TabText))
					menu.DropDownItems.Add(content.DockControl.TabText, content.DockControl.TabIcon, (ss,aa) => FindAndShow(content));
				
			};
			return menu;
		}

		/// <summary>Make 'content' visible to the user, by making it's containing window visible, on-screen, popped-out, etc</summary>
		public void FindAndShow(IDockable content)
		{
			if (content.DockControl.DockContainer != this)
				throw new Exception("Cannot show content '{0}', it is not managed by this DockContainer".Fmt(content.DockControl.TabText));

			// Find the window that contains 'content'
			var pane = content.DockControl.DockPane;
			if (pane != null)
			{
				// Make the selected item the visible item on the pane
				pane.VisibleContent = content;

				// Make it the active item in the dock container
				var container = pane.TreeHost;
				if (container != null)
				{
					container.ActiveContent = content;

					// If the container is an auto hide panel, make sure it's popped out
					var ah = container as AutoHidePanel;
					if (ah != null)
					{
						ah.PoppedOut = true;
					}

					// If the container is a floating window, make sure it's visible and on-screen
					var fw = container as FloatingWindow;
					if (fw != null)
					{
						fw.Bounds = Util.OnScreen(fw.Bounds);
						fw.Visible = true;
						fw.BringToFront();
						if (fw.WindowState == FormWindowState.Minimized)
							fw.WindowState = FormWindowState.Normal;
					}
				}
			}

			// Otherwise, restore 'content' to the dock container
			else
			{
				var address = content.DockControl.DockAddressFor(Root);
				Add(content, address);
			}
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
		private static Rectangle DockSiteBounds(EDockSite location, Rectangle rect, EDockMask docked_mask, DockSizeData dock_site_sizes)
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

		/// <summary>Dock site mask value</summary>
		[Flags] internal enum EDockMask
		{
			None   = 0,
			Centre = 1 << EDockSite.Centre,
			Top    = 1 << EDockSite.Top,
			Bottom = 1 << EDockSite.Bottom,
			Left   = 1 << EDockSite.Left  ,
			Right  = 1 << EDockSite.Right ,
		}

		/// <summary>Save state to XML</summary>
		public XElement ToXml(XElement node)
		{
			// Loop through the floating windows assigning an incrementing ID to each one with content
			var id = 0;
			foreach (var fw in FloatingWindows.Where(x => !x.Root.Empty))
				fw.Id = id++;

			// Record the version number of this data format
			node.Add2(XmlTag.Version, 1, false);

			// Record the state of the hosted content
			var content_node = node.Add2(new XElement(XmlTag.Contents));
			foreach (var content in AllContent)
				content_node.Add2(XmlTag.Content, content.DockControl, false);

			// Record the name of the active content
			if (ActiveContent != null)
				node.Add2(XmlTag.Active, ActiveContent.DockControl.PersistName, false);

			// Record the structure of the main tree
			node.Add2(XmlTag.Tree, Root, false);

			// Record position information for the floating windows
			var fw_node = node.Add2(new XElement(XmlTag.FloatingWindows));
			foreach (var fw in FloatingWindows.Where(x => !x.Root.Empty))
				fw_node.Add2(XmlTag.FloatingWindow, fw, false);

			return node;
		}

		/// <summary>Record the layout of this dock container</summary>
		public XElement SaveLayout()
		{
			return ToXml(new XElement(XmlTag.DockContainerLayout));
		}

		/// <summary>Update the layout of this dock container using the data in 'node'</summary>
		public void LoadLayout(XElement node)
		{
			if (node.Name != XmlTag.DockContainerLayout)
				throw new Exception("XML data does not contain dock container layout information");

			using (this.SuspendLayout(true))
			{
				// Get a collection of the content currently loaded
				var all_content = AllContent.ToDictionary(x => x.DockControl.PersistName, x => x);
				IDockable content; 

				// For each content node find a content object with matching PersistName and
				// move it to the location indicated in the saved state.
				foreach (var content_node in node.Elements(XmlTag.Contents, XmlTag.Content))
				{
					// Find a content object with a matching persistence name
					var name = content_node.Element(XmlTag.Name).As<string>();
					if (all_content.TryGetValue(name, out content))
					{
						var host    = content_node.Element(XmlTag.Host).As<string>().Split(',');
						var address = content_node.Element(XmlTag.Address).As<string>().Split(',').Select(x => Enum<EDockSite>.Parse(x)).ToArray();

						// Determine where the content was docked
						switch (host[0])
						{
						case nameof(DockContainer):
							{
								Root.Add(content, address);
								break;
							}
						case nameof(AutoHidePanel):
							{
								var side = Enum<EDockSite>.Parse(host[1]);
								m_auto_hide[(int)side].Root.Add(content, address);
								break;
							}
						case nameof(FloatingWindow):
							{
								var id = int.Parse(host[1]);
								var fw = GetFloatingWindow(x => x.Id == id);
								fw.Root.Add(content, address);
								fw.Id = id;
								break;
							}
						}
					}
				}

				// Load the tree layout
				var tree_node = node.Element(XmlTag.Tree);
				if (tree_node != null)
					Root.ApplyState(tree_node);

				// Apply layout to floating windows
				foreach (var fw_node in node.Elements(XmlTag.FloatingWindows, XmlTag.FloatingWindow))
				{
					var id = fw_node.Element(XmlTag.Id).As<int>();
					var fw = GetFloatingWindow(x => x.Id == id);
					if (fw != null)
						fw.ApplyState(fw_node);
				}

				// Restore the active content
				var active = node.Element(XmlTag.Active)?.As<string>();
				if (active != null && all_content.TryGetValue(active, out content))
					ActiveContent = content;
			}
		}

		/// <summary>Output the tree structure</summary>
		public string DumpTree()
		{
			var sb = new StringBuilder();
			Root.DumpTree(sb, 0, string.Empty);
			return sb.ToString();
		}

		#region Custom Controls

		/// <summary>Interface for classes that have a Root branch and active content</summary>
		internal interface ITreeHost
		{
			/// <summary>The dock container that owns this instance</summary>
			DockContainer DockContainer { get; }

			/// <summary>The root of the tree of dock panes</summary>
			Branch Root { get; }

			/// <summary>The dockable that is active on the container</summary>
			IDockable ActiveContent { get; set; }

			/// <summary>The pane that contains the active content on the container</summary>
			DockPane ActivePane { get; set; }

			/// <summary>Add a dockable instance to this branch at the position described by 'location'.</summary>
			void Add(IDockable dockable, params EDockSite[] location);
		}

		/// <summary>
		/// Represents a node in the tree of dock panes.
		/// Each node has 5 children; Centre, Left, Right, Top, Bottom.
		/// A root branch can be hosted in a dock container, floating window, or auto hide window</summary>
		[DebuggerDisplay("{DumpDesc()}")]
		internal class Branch :Control
		{
			public Branch(DockContainer dc, DockSizeData dock_sizes)
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

			/// <summary>The dock container that this pane belongs too</summary>
			public DockContainer DockContainer { [DebuggerStepThrough] get; private set; }

			/// <summary>The control that hosts the dock pane and branch tree</summary>
			internal ITreeHost TreeHost { [DebuggerStepThrough] get { return (ITreeHost)Parent; } }

			/// <summary>The branch at the top of the tree that contains this pane</summary>
			public Branch RootBranch
			{
				[DebuggerStepThrough] get
				{
					var b = this;
					for (; b != null && b.Parent is Branch; b = (Branch)b.Parent) {}
					return b;
				}
			}

			/// <summary>The sizes of the child controls</summary>
			public DockSizeData DockSizes { get; private set; }

			/// <summary>Add a dockable instance to this tree at the position described by 'location'.</summary>
			public void Add(IDockable dockable, params EDockSite[] location)
			{
				if (dockable == null)
					throw new ArgumentNullException(nameof(dockable), "Cannot add 'null' content");

				// If already on a pane, remove first (raising events)
				dockable.DockControl.DockPane = null;

				// Use the default location if none has been given.
				var ds = location.Length != 0 ? location[0] : dockable.DockControl.DefaultDockSite;

				// Find the dock pane for this dock site, growing the tree as necessary
				var pane = DockPane(ds, location.Skip(1));

				// Add the content
				pane.Content.Add(dockable);
			}

			/// <summary>Remove branches without any content to reduce the size of the tree</summary>
			internal void PruneBranches()
			{
				// Depth-first recursive
				foreach (var b in Child.Where(x => x.Ctrl is Branch).Select(x => x.Branch))
					b.PruneBranches();

				// If any of L,R,T,B contain empty panes, prune them. Don't prune
				// 'C', we need to leave somewhere for content to be dropped.
				foreach (var c in Child)
				{
					if (c.Ctrl is Branch) continue;
					if (c.DockPane?.Content.Count == 0)
					{
						var pane = c.DockPane;
						c.Ctrl = null;
						pane.Dispose();
					}
				}

				// If the branch has only one child, ensure it's in the Centre position
				if (Child.Count == 1 && Child[EDockSite.Centre].Ctrl == null)
				{
					var ctrl = Child[0].Ctrl;
					Child[0].Ctrl = null;
					Child[EDockSite.Centre].Ctrl = ctrl;
				}

				// If the branch has <= 1 child, then remove this branch and replace the slot in the parent with the child (if it exists)
				if (Child.Count <= 1 && Parent is Branch)
				{
					var ds = DockSite;
					var parent = (Branch)Parent;

					// Find the single child
					var child = Child.SingleOrDefault(x => x.Ctrl != null);
					if (child != null)
					{
						var ctrl = child.Ctrl;
						child.Ctrl = null;
						parent.Child[ds].Ctrl = ctrl;
					}

					// Release this branch
					Dispose();
				}
				else
				{
					// Ensure there is always a centre pane
					if (Child[EDockSite.Centre].Ctrl == null)
						Child[EDockSite.Centre].Ctrl = new DockPane(DockContainer);
				}
			}

			/// <summary>Add branches to the tree until 'rest' is empty.</summary>
			private Branch GrowBranches(EDockSite ds, IEnumerable<EDockSite> rest, out EDockSite last_ds)
			{
				// Note, this means the last 'ds' does not have a branch added.
				// This is deliberate so that dock addresses (arrays of EDockSite) can be
				// used to grow the tree to the point where a dock pane should be added
				if (!rest.Any())
				{
					// If we've reached the end of the dock address, but there are still branches
					// keep going down the centre dock site until a null or dock pane is reached
					var b = this;
					for (; b.Child[ds].Branch != null; b = b.Child[ds].Branch, ds = EDockSite.Centre) {}

					last_ds = ds;
					return b;
				}

				// No child at 'ds' add a branch
				if (Child[ds].Ctrl == null)
				{
					var new_branch = new Branch(DockContainer, ds == EDockSite.Centre ? DockSizeData.Quarters : DockSizeData.Halves);
					Child[ds].Ctrl = new_branch;
					return new_branch.GrowBranches(rest.First(), rest.Skip(1), out last_ds);
				}

				// A dock pane at 'ds'? Swap it with a branch
				// containing the dock pane as the centre child
				if (Child[ds].Ctrl is DockPane)
				{
					var new_branch = new Branch(DockContainer, DockSizeData.Halves);
					new_branch.Child[EDockSite.Centre].Ctrl = Child[ds].Ctrl;
					Child[ds].Ctrl = new_branch;
					return new_branch.GrowBranches(rest.First(), rest.Skip(1), out last_ds);
				}

				// Existing branch, recursive call into it
				var branch = Child[ds].Branch;
				return branch.GrowBranches(rest.First(), rest.Skip(1), out last_ds);
			}

			/// <summary>Get the pane at 'location', adding branches to the tree if necessary</summary>
			public DockPane DockPane(EDockSite ds, IEnumerable<EDockSite> rest)
			{
				// Grow the tree
				var branch = GrowBranches(ds, rest, out ds);

				// No child at 'ds'? Add a dock pane.
				if (branch.Child[ds].Ctrl == null)
					branch.Child[ds].Ctrl = new DockPane(DockContainer);

				// Existing pane at 'ds'
				Debug.Assert(branch.Child[ds].Ctrl is DockPane);
				return branch.Child[ds].DockPane;
			}
			public DockPane DockPane(EDockSite ds)
			{
				return DockPane(ds, Enumerable.Empty<EDockSite>());
			}

			/// <summary>The child controls (dock panes or branches) of this branch</summary>
			internal ChildCollection Child { [DebuggerStepThrough] get; private set; }
			internal class ChildCollection :IDisposable, IEnumerable<ChildCtrl>
			{
				/// <summary>The child controls (dock panes or branches) of this branch</summary>
				private ChildCtrl[] m_child;
				private Branch This;

				public ChildCollection(Branch @this)
				{
					This = @this;

					// Create an array to hold the five child controls (DockPanes or Branches)
					m_child = Util.NewArray((int)DockSiteCount, i => new ChildCtrl(This, (EDockSite)i));
				}
				public void Dispose()
				{
					foreach (var c in m_child)
						c.Ctrl = Util.Dispose(c.Ctrl);
				}

				/// <summary>The number of non-null children in this collection</summary>
				public int Count
				{
					get { return m_child.Count(x => x.Ctrl != null); }
				}

				/// <summary>Access the non-null children in this collection</summary>
				public ChildCtrl this[int index]
				{
					get { return m_child.Where(x => x.Ctrl != null).ElementAt(index); }
				}

				/// <summary>Get a child control in a given dock site</summary>
				public ChildCtrl this[EDockSite ds]
				{
					[DebuggerStepThrough] get { return m_child[(int)ds]; }
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

			/// <summary>Wrapper of a child control (pane or branch) and associated splitter</summary>
			[DebuggerDisplay("{DockSite} {Ctrl}")]
			internal class ChildCtrl
			{
				private Branch This;

				public ChildCtrl(Branch @this, EDockSite ds)
				{
					This = @this;
					DockSite = ds;
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

								// Notify of the tree change
								This.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.Removed, pane:m_ctrl as DockPane, branch:m_ctrl as Branch));
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

								// Notify of the tree change
								This.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.Added, pane:m_ctrl as DockPane, branch:m_ctrl as Branch));
							}
						}
					}
				}
				private Control m_ctrl;

				/// <summary>Get 'Ctrl' as a branch, or null</summary>
				public Branch Branch
				{
					get { return Ctrl as Branch; }
				}

				/// <summary>Get 'Ctrl' as a dock pane, or null</summary>
				public DockPane DockPane
				{
					get { return Ctrl as DockPane; }
				}

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

			/// <summary>Get the dock site for this branch within a parent branch.</summary>
			internal EDockSite DockSite
			{
				get { return (Parent as Branch)?.Child.First(x => x.Ctrl == this).DockSite ?? EDockSite.None; }
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
					foreach (var c in Child.Where(x => x.Ctrl != null))
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
					for (queue.Enqueue(this); queue.Count != 0; )
					{
						var b = queue.Dequeue();
						foreach (var c in b.Child.Where(x => x.Ctrl is Branch))
							queue.Enqueue(c.Branch);

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
						foreach (var p in b.Child.Where(x => x.Ctrl is DockPane))
							yield return p.DockPane;
				}
			}

			/// <summary>Enumerate the dockables in this sub-tree (breadth first, order = order of EDockSite)</summary>
			public IEnumerable<IDockable> AllContent
			{
				get
				{
					foreach (var p in AllPanes)
						foreach (var c in p.Content)
							yield return c;
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

			/// <summary>Return the node relative to this sub-tree</summary>
			internal Control ChildAt(EDockSite[] address)
			{
				var c = (Control)this;
				foreach (var ds in address)
				{
					Debug.Assert(c != null && c is Branch);
					c = ((Branch)c).Child[ds].Ctrl;
				}
				return c;
			}

			/// <summary>Raised whenever a child (pane or branch) is added to or removed from this sub tree</summary>
			public event EventHandler<TreeChangedEventArgs> TreeChanged;
			internal void OnTreeChanged(TreeChangedEventArgs args)
			{
				TreeChanged.Raise(this, args);
				(Parent as Branch)?.OnTreeChanged(args);
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

			/// <summary>Record the layout of the tree</summary>
			public XElement ToXml(XElement node)
			{
				// Record the pane sizes
				node.Add2(XmlTag.DockSizes, DockSizes, false);

				// Recursively add the sub trees
				foreach (var ch in Child)
				{
					if (ch.DockPane != null)
					{
						var n = node.Add2(XmlTag.Pane, ch.DockPane, false);
						n.SetAttributeValue(XmlTag.Location, ch.DockSite);
					}
					if (ch.Branch != null)
					{
						var n = node.Add2(XmlTag.Tree, ch.Branch, false);
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
							if (Child[ds].Branch != null)
								Child[ds].Branch.ApplyState(child_node);
							break;
						}
					case XmlTag.Pane:
						{
							var ds = Enum<EDockSite>.Parse(child_node.Attribute(XmlTag.Location).Value);
							if (Child[ds].DockPane != null)
								Child[ds].DockPane.ApplyState(child_node);
							break;
						}
					}
				}
			}

			/// <summary>Output the tree structure as a string</summary>
			public void DumpTree(StringBuilder sb, int indent, string prefix)
			{
				sb.Append(' ', indent).Append(prefix).Append("Branch: ").AppendLine();
				sb.Append(' ', indent).Append("{").AppendLine();
				foreach (var c in Child)
				{
					if      (c.Ctrl is Branch  ) c.Branch  .DumpTree(sb, indent + 4, "{0,-6} = ".Fmt(c.DockSite));
					else if (c.Ctrl is DockPane) c.DockPane.DumpTree(sb, indent + 4, "{0,-6} = ".Fmt(c.DockSite));
					else sb.Append(' ', indent + 4).Append("{0,-6} = <null>".Fmt(c.DockSite)).AppendLine();
				}
				sb.Append(' ', indent).Append("}").AppendLine();
			}

			/// <summary>String description of the branch for debugging</summary>
			public string DumpDesc()
			{
				var lvl = 0;
				for (var p = this; p != null && p.Parent is Branch; p = (Branch)p.Parent) ++lvl;
				var c = Child[EDockSite.Centre];
				var l = Child[EDockSite.Left  ];
				var t = Child[EDockSite.Top   ];
				var r = Child[EDockSite.Right ];
				var b = Child[EDockSite.Bottom];
				return "Branch Lvl={0}  C=[{1}] L=[{2}] T=[{3}] R=[{4}] B=[{5}]".Fmt(
					lvl == 0 && Parent != null ? Parent.GetType().Name : lvl.ToString(),
					c.Branch != null ? "Branch" : c.DockPane?.DumpDesc() ?? "null",
					l.Branch != null ? "Branch" : l.DockPane?.DumpDesc() ?? "null",
					t.Branch != null ? "Branch" : t.DockPane?.DumpDesc() ?? "null",
					r.Branch != null ? "Branch" : r.DockPane?.DumpDesc() ?? "null",
					b.Branch != null ? "Branch" : b.DockPane?.DumpDesc() ?? "null");
			}
		}

		/// <summary>
		/// A pane groups a set of IDockable items together. Only one IDockable is displayed at a time in the pane,
		/// but tabs for all dockable items are displayed along the top, bottom, left, or right.</summary>
		[DebuggerDisplay("{DumpDesc()}")]
		public class DockPane :Control
		{
			public DockPane(DockContainer owner)
			{
				if (owner == null)
					throw new ArgumentNullException("The owning dock container cannot be null");

				SetStyle(ControlStyles.Selectable, false);
				SetStyle(ControlStyles.ContainerControl, true);
				DockContainer = owner;
				ApplyToVisibleContentOnly = false;

				// Create the collection that manages the content within this pane
				Content = new BindingSource<IDockable>{DataSource = new BindingListEx<IDockable>{PerItemClear = true}};
				Content.ListChanging += HandleContentListChanged;
				Content.PositionChanging += HandleContentPositionChanged;

				using (this.SuspendLayout(false))
				{
					TabStripCtrl = new TabStripControl(this);
					TitleCtrl    = new PaneTitleControl(this);
				}
			}
			protected override void Dispose(bool disposing)
			{
				// Note: we don't own any of the content
				VisibleContent = null;
				TitleCtrl = null;
				TabStripCtrl = null;
				base.Dispose(disposing);
			}

			/// <summary>The owning dock container</summary>
			public DockContainer DockContainer { [DebuggerStepThrough] get; private set; }

			/// <summary>The control that hosts the dock pane and branch tree</summary>
			internal ITreeHost TreeHost { [DebuggerStepThrough] get { return RootBranch.TreeHost; } }

			/// <summary>The branch at the top of the tree that contains this pane</summary>
			internal Branch RootBranch { [DebuggerStepThrough] get { return Branch?.RootBranch; } }

			/// <summary>The branch that contains this pane</summary>
			internal Branch Branch { [DebuggerStepThrough] get { return Parent as Branch; } }

			/// <summary>Get the dock site for this pane</summary>
			internal EDockSite DockSite
			{
				get { return (Parent as Branch)?.Child.First(x => x.Ctrl == this).DockSite ?? EDockSite.None; }
			}

			/// <summary>Get the dock sites, from top to bottom, describing where this pane is located in the tree </summary>
			internal EDockSite[] DockAddress
			{
				get { return Branch.DockAddress.Concat(DockSite).ToArray(); }
			}

			/// <summary>The content hosted by this pane</summary>
			public BindingSource<IDockable> Content { [DebuggerStepThrough] get; private set; }

			/// <summary>
			/// The content in this pane that was last active (Not necessarily the active content for the dock container)
			/// There should always be visible content while 'Content' is not empty. Empty panes are destroyed.
			/// If this pane is not the active pane, then setting the visible content only raises events for this pane.
			/// If this is the active pane, then the dock container events are also raised.</summary>
			public IDockable VisibleContent
			{
				get { return m_impl_visible_content; }
				set
				{
					if (m_impl_visible_content == value || m_in_visible_content) return;
					using (Scope.Create(() => m_in_visible_content = true, () => m_in_visible_content = false))
					{
						// Only content that is in this pane can be made active for this pane
						if (value != null && !Content.Contains(value))
							throw new Exception("Dockable item '{0}' has not been added to this pane so can not be made the active content.".Fmt(value.DockControl.PersistName));

						// Ensure 'value' is the current item in the Content collection
						Content.Current = value;

						// Switch to the new active content
						var prev = m_impl_visible_content;
						using (this.SuspendLayout(true))
						{
							if (m_impl_visible_content != null)
							{
								// Remove from the child controls collection
								// Note: don't dispose the content, we don't own it
								Controls.Remove(m_impl_visible_content.DockControl.Owner);
							}

							m_impl_visible_content = value;

							if (m_impl_visible_content != null)
							{
								// When content becomes active, add it as a child control of the pane
								Controls.Add(m_impl_visible_content.DockControl.Owner);
								Controls.SetChildIndex(m_impl_visible_content.DockControl.Owner, 0);
							}
						}

						// Ensure the tab for the active content is visible
						TabStripCtrl.MakeTabVisible(Content.Position);

						// Raise the active content changed event on this pane.
						OnVisibleContentChanged(new ActiveContentChangedEventArgs(prev, value));

						// Notify the container of this tree that the 
						Branch.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.ActiveContent, pane:this, dockable:value));

						// Refresh the Pane
						Invalidate(true);
					}
				}
			}
			private IDockable m_impl_visible_content;
			private bool m_in_visible_content;

			/// <summary>Get the text to display in the title bar for this pane</summary>
			public string CaptionText
			{
				get { return VisibleContent?.DockControl.TabText ?? string.Empty; }
			}

			/// <summary>Get/Set the content in the pane floating</summary>
			public bool IsFloating
			{
				get
				{
					if (VisibleContent == null) return false;
					var floating = VisibleContent.DockControl.IsFloating;

					// All content in a pane should be in the same state. This is true even if
					// 'ApplyToVisibleContentOnly' is true because any float/auto hide operation should
					// move that content to a different pane
					Debug.Assert(Content.All(x => x.DockControl.IsFloating == floating), "All content in a pane should be in the same state");
					return floating;
				}
				set
				{
					// All content in this pane matches the floating state of the active content
					var content = VisibleContent;
					if (content == null)
						return;

					using (this.SuspendLayout(true))
					{
						// Record properties of this pane so we can set them on the new pane
						// (this pane will be disposed if empty after the content has changed floating state)
						var strip_location = TabStripCtrl.StripLocation;

						// Change the state of the active content
						content.DockControl.IsFloating = value;

						// Copy properties form this pane to the new pane that hosts 'content'
						var pane = content.DockControl.DockPane;
						pane.TabStripCtrl.StripLocation = strip_location;

						// Add the remaining content to the same pane that 'content' is now in
						if (!ApplyToVisibleContentOnly)
						{
							using (pane.SuspendLayout(true))
								pane.Content.AddRange(Content.ToArray());
						}
					}
				}
			}

			/// <summary>Get/Set the content in this pane to auto hide mode</summary>
			public bool IsAutoHide
			{
				get
				{
					if (VisibleContent == null) return false;
					var auto_hide = VisibleContent.DockControl.IsAutoHide;
					Debug.Assert(Content.All(x => x.DockControl.IsAutoHide == auto_hide), "All content in a pane should be in the same state");
					return auto_hide;
				}
				set
				{
					// All content in this pane matches the auto hide state of the active content
					var content = VisibleContent;
					if (content == null)
						return;

					using (this.SuspendLayout(true))
					{
						// Change the state of the active content
						content.DockControl.IsAutoHide = value;

						// Add the remaining content to the same pane that 'content' is now in
						if (!ApplyToVisibleContentOnly)
						{
							var pane = content.DockControl.DockPane;
							using (pane.SuspendLayout(true))
								pane.Content.AddRange(Content.ToArray());
						}
					}
				}
			}

			/// <summary>Get/Set whether operations such as floating or auto hiding apply to all content or just the visible content</summary>
			public bool ApplyToVisibleContentOnly { get; set; }

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
							Controls.SetChildIndex(m_impl_caption_ctrl, 100);
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
							Controls.SetChildIndex(m_impl_tab_strip_ctrl, 100);
						}
					}
				}
			}
			private TabStripControl m_impl_tab_strip_ctrl;

			/// <summary>Raised whenever the visible content for this dock pane changes</summary>
			public event EventHandler<ActiveContentChangedEventArgs> VisibleContentChanged;
			protected void OnVisibleContentChanged(ActiveContentChangedEventArgs args)
			{
				VisibleContentChanged.Raise(this, args);
			}

			/// <summary>Raised when the pane is activated</summary>
			public event EventHandler ActivatedChanged;
			internal void OnActivatedChanged()
			{
				ActivatedChanged.Raise(this, EventArgs.Empty);
			}

			/// <summary>Get/Set this pane as activated. Activation causes the active content in this pane to be activated</summary>
			public bool Activated
			{
				get { return TreeHost.ActivePane == this; }
				set
				{
					// Assign this pane as the active one. This will cause the previously active pane
					// to have Activated = false called. Careful, need to handle 'Activated' or 'TreeHost.ActivePane'
					// being assigned to. The ActivePane handler will call OnActivatedChanged.
					TreeHost.ActivePane = value ? this : null;
				}
			}

			/// <summary>Handle the list of content in this pane changing</summary>
			private void HandleContentListChanged(object sender, ListChgEventArgs<IDockable> e)
			{
				// Note: the PositionChanged handler on Content updates the ActiveContent
				var dockable = e.Item;
				switch (e.ChangeType)
				{
				case ListChg.ItemAdded:
					{
						if (dockable == null)
							throw new ArgumentNullException(nameof(dockable), "Cannot add 'null' content to a dock pane");

						// Before adding 'dockable' to this content list, remove it from any previous pane (or even this pane)
						dockable.DockControl.DockPane = null;

						// Set this pane as the owning dock pane for the dockable.
						// Don't add the dockable as a child control yet, that happens when the active content changes.
						dockable.DockControl.SetDockPaneInternal(this);

						// Notify tree changed
						Branch.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.Added, dockable:dockable));

						// Perform layout, since a tab has been added which might mean the tab strip needs to shown
						Invalidate(true);
						PerformLayout();
						break;
					}
				case ListChg.ItemRemoved:
					{
						if (dockable == null)
							throw new ArgumentNullException(nameof(dockable), "Cannot remove 'null' content from a dock pane");

						// Clear the dock pane from the content
						Debug.Assert(dockable.DockControl.DockPane == this);
						dockable.DockControl.SetDockPaneInternal(null);

						// Notify tree changed
						Branch.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.Removed, dockable:dockable));

						// Remove empty branches
						RootBranch.PruneBranches();

						// Perform layout, since a tab has been removed which might mean the tab strip needs to hide
						Invalidate(true);
						PerformLayout();
						break;
					}
				}
			}

			/// <summary>Handle the 'Position' value in the content collection changing</summary>
			private void HandleContentPositionChanged(object sender, PositionChgEventArgs e)
			{
				VisibleContent = Content.Current;
			}

			/// <summary>Layout the pane</summary>
			protected override void OnLayout(LayoutEventArgs e)
			{
				// Measure the remaining space and use that for the active content
				var rect = DisplayRectangle;
				if (rect.Area() > 0)
				{
					using (this.SuspendLayout(false))
					{
						// Position the title bar
						if (TitleCtrl != null && TitleCtrl.Visible)
						{
							var bounds = TitleCtrl.CalcBounds(rect);
							rect = rect.Subtract(bounds);
							TitleCtrl.Bounds = bounds;
							TitleCtrl.ButtonAutoHide.Visible = DockSite != EDockSite.Centre || IsAutoHide;
						}

						// Position the tab strip
						if (TabStripCtrl != null && TabStripCtrl.Visible)
						{
							var bounds = TabStripCtrl.CalcBounds(rect);
							rect = rect.Subtract(bounds);
							TabStripCtrl.Bounds = bounds;
						}

						// Use the remaining area for content
						if (VisibleContent != null)
							VisibleContent.DockControl.Owner.Bounds = rect;
					}
				}

				base.OnLayout(e);
			}

			/// <summary>Handle mouse activation of the content within this pane</summary>
			[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			protected override void WndProc(ref Message m)
			{
				// Watch for mouse activate. WM_MOUSEACTIVATE is sent to the inactive window under the
				// mouse. If that window passes the message to DefWindowProc then the parent window gets it.
				// We need to intercept WM_MOUSEACTIVATE going to a content control so we can activate the pane.
				if (m.Msg == Win32.WM_MOUSEACTIVATE)
					Activated = true;

				base.WndProc(ref m);
			}

			/// <summary>Record the layout of the pane</summary>
			public XElement ToXml(XElement node)
			{
				// Record the content that is the visible content in this pane
				if (VisibleContent != null)
					node.Add2(XmlTag.Visible, VisibleContent.DockControl.PersistName, false);

				// Save the strip location
				if (TabStripCtrl != null)
					node.Add2(XmlTag.StripLocation, TabStripCtrl.StripLocation, false);

				return node;
			}

			/// <summary>Apply layout state to this pane</summary>
			public void ApplyState(XElement node)
			{
				// Restore the visible content
				var visible = node.Element(XmlTag.Visible)?.As<string>();
				if (visible != null)
				{
					var content = Content.FirstOrDefault(x => x.DockControl.PersistName == visible);
					if (content != null)
						VisibleContent = content;
				}

				// Restore the strip location
				var strip_location = node.Element(XmlTag.StripLocation)?.As<EDockSite>();
				if (strip_location != null)
					TabStripCtrl.StripLocation = strip_location.Value;
			}

			/// <summary>Output the tree structure as a string</summary>
			public void DumpTree(StringBuilder sb, int indent, string prefix)
			{
				sb.Append(' ',indent).Append(prefix).Append("Pane: ").Append(DumpDesc()).AppendLine();
			}

			/// <summary>A string description of the pane</summary>
			public string DumpDesc()
			{
				return "Count={0} Active={1}".Fmt(Content.Count, Content.Count != 0 ? CaptionText : "<empty pane>");
			}

			/// <summary>A custom control for drawing the title bar, including text, close button and pin button</summary>
			public class PaneTitleControl :Control
			{
				public PaneTitleControl(DockPane owner)
				{
					SetStyle(ControlStyles.Selectable, false);
					ResizeRedraw = true;
					Visible = true;

					DockPane = owner;
					using (this.SuspendLayout(false))
					{
						ContextMenuStrip = CreateContextMenu(owner);
						ButtonClose = new CaptionButton(Resources.dock_close);
						ButtonAutoHide = new CaptionButton(DockPane.IsAutoHide ? Resources.dock_unpinned : Resources.dock_pinned);
						ButtonMenu = new CaptionButton(Resources.dock_menu);
					}
				}
				protected override void Dispose(bool disposing)
				{
					using (this.SuspendLayout(false))
					{
						ButtonClose = null;
						ButtonAutoHide = null;
						ButtonMenu = null;
					}
					ContextMenuStrip = null;
					DockPane = null;
					base.Dispose(disposing);
				}

				/// <summary>Get/Set whether this control is visible</summary>
				public new bool Visible
				{
					get { return m_impl_visible; }
					set { base.Visible = m_impl_visible = value; }
				}
				private bool m_impl_visible;

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
							m_impl_dock_pane.Content.ListChanging -= HandleContentChanging;
						}
						m_impl_dock_pane = value;
						if (m_impl_dock_pane != null)
						{
							m_impl_dock_pane.Content.ListChanging += HandleContentChanging;
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

				/// <summary>The drop down menu for the pane</summary>
				public CaptionButton ButtonMenu
				{
					get { return m_impl_btn_menu; }
					private set
					{
						if (m_impl_btn_menu == value) return;
						if (m_impl_btn_menu != null)
						{
							m_impl_btn_menu.Click -= HandleMenu;
							Controls.Remove(m_impl_btn_menu);
							Util.Dispose(ref m_impl_btn_menu);
						}
						m_impl_btn_menu = value;
						if (m_impl_btn_menu != null)
						{
							Controls.Add(m_impl_btn_menu);
							m_impl_btn_menu.Click += HandleMenu;
						}
					}
				}
				private CaptionButton m_impl_btn_menu;

				/// <summary>Hide the Control's normal context menu</summary>
				private new void ContextMenu() {}

				/// <summary>Measure the height required by this control based on the content in 'DockPane'</summary>
				public int MeasureHeight()
				{
					var opts = DockPane.DockContainer.Options.TitleBar;
					return opts.Padding.Top + opts.TextFont.Height + opts.Padding.Bottom;
				}

				/// <summary>Returns the size of the pane title control given the available 'display_area'</summary>
				public Rectangle CalcBounds(Rectangle display_area)
				{
					// No title for empty panes
					if (DockPane.Content.Count == 0)
						return Rectangle.Empty;

					// Title bars are always at the top
					var r = new RectangleRef(display_area);
					r.Bottom = r.Top + MeasureHeight();
					return r;
				}

				/// <summary>Perform a hit test on the title bar at client space point 'pt'</summary>
				public EHitItem HitTest(Point pt)
				{
					if (ButtonClose.Visible && ButtonClose.Bounds.Contains(pt)) return EHitItem.CloseBtn;
					if (ButtonAutoHide.Visible && ButtonAutoHide.Bounds.Contains(pt)) return EHitItem.AutoHideBtn;
					if (ButtonMenu.Visible && ButtonMenu.Bounds.Contains(pt)) return EHitItem.MenuBtn;
					return EHitItem.Caption;
				}
				public enum EHitItem { Caption, CloseBtn, AutoHideBtn, MenuBtn }

				/// <summary>Layout the pane title bar</summary>
				protected override void OnLayout(LayoutEventArgs e)
				{
					if (ClientRectangle.Area() > 0)
					{
						using (this.SuspendLayout(false))
						{
							var opts = DockPane.DockContainer.Options.TitleBar;
							var r = ClientRectangle;
							var x = r.Right - opts.Padding.Right - 1;
							var y = r.Y + opts.Padding.Top;
							var h = r.Height - opts.Padding.Bottom - opts.Padding.Top;

							// Position the close button
							if (ButtonClose != null)
							{
								if (ButtonClose.Visible) x -= ButtonClose.Width;
								var w = h * ButtonClose.Image.Width / ButtonClose.Image.Height;
								ButtonClose.Bounds = new Rectangle(x, y, w, h);
							}

							// Position the auto hide button
							if (ButtonAutoHide != null)
							{
								// Update the image based on dock state
								ButtonAutoHide.Image = DockPane.IsAutoHide ? Resources.dock_unpinned : Resources.dock_pinned;

								if (ButtonAutoHide.Visible) x -= ButtonAutoHide.Width;
								var w = h * ButtonAutoHide.Image.Width / ButtonAutoHide.Image.Height;
								ButtonAutoHide.Bounds = new Rectangle(x, y, w, h);
							}

							// Position the menu button
							if (ButtonMenu != null)
							{
								if (ButtonMenu.Visible) x -= ButtonMenu.Width;
								var w = h * ButtonMenu.Image.Width / ButtonMenu.Image.Height;
								ButtonMenu.Bounds = new Rectangle(x, y, w, h);
							}

							Invalidate(true);
						}
					}
					base.OnLayout(e);
				}

				/// <summary>Paint the control</summary>
				protected override void OnPaint(PaintEventArgs e)
				{
					base.OnPaint(e);

					// No area to draw in...
					if (ClientRectangle.Area() <= 0)
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

					// Text rendering format
					var fmt = new StringFormat(StringFormatFlags.NoWrap | StringFormatFlags.FitBlackBox | (RightToLeft == RightToLeft.Yes ? StringFormatFlags.DirectionRightToLeft : 0)) { Trimming = StringTrimming.EllipsisCharacter };

					// Draw the caption text
					using (var bsh = new SolidBrush(cols.Text))
						gfx.DrawString(DockPane.CaptionText, opts.TextFont, bsh, r, fmt);
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
					if (e.Button == MouseButtons.Left && DockPane.DockContainer.Options.AllowUserDocking && HitTest(e.Location) == EHitItem.Caption)
					{
						// Record the mouse down location
						m_mouse_down_at = e.Location;
						Capture = true;
					}
				}
				protected override void OnMouseMove(MouseEventArgs e)
				{
					if (e.Button == MouseButtons.Left && Util.Distance(e.Location, m_mouse_down_at) > 5)
					{
						Capture = false;

						// Begin dragging the pane
						DragBegin(DockPane);
					}
					base.OnMouseMove(e);
				}
				protected override void OnMouseUp(MouseEventArgs e)
				{
					Capture = false;
					base.OnMouseUp(e);
				}
				private Point m_mouse_down_at;

				/// <summary>Toggle floating when double clicked</summary>
				protected override void OnMouseDoubleClick(MouseEventArgs e)
				{
					base.OnMouseDoubleClick(e);
					DockPane.IsFloating = !DockPane.IsFloating;
				}

				/// <summary>See initial visibility</summary>
				protected override void OnHandleCreated(EventArgs e)
				{
					base.OnHandleCreated(e);
					base.Visible = m_impl_visible;
				}

				/// <summary>Helper overload of invalidate</summary>
				private void Invalidate(object sender, EventArgs e)
				{
					Invalidate(true);
				}

				/// <summary>Create a default content menu for 'pane'</summary>
				private static ContextMenuStrip CreateContextMenu(DockPane pane)
				{
					var cmenu = new ContextMenuStrip();
					using (cmenu.SuspendLayout(true))
					{
						{ // Float the pane in a new window
							var opt = cmenu.Items.Add2(new ToolStripMenuItem("Float"));
							cmenu.Opening += (s,a) =>
							{
								opt.Enabled = !pane.IsFloating;
							};
							opt.Click += (s,a) =>
							{
								pane.IsFloating = true;
							};
						}
						{ // Return the pane to the dock container
							var opt = cmenu.Items.Add2(new ToolStripMenuItem("Dock"));
							cmenu.Opening += (s,a) =>
							{
								opt.Enabled = pane.IsFloating;
							};
							opt.Click += (s,a) =>
							{
								pane.IsFloating = false;
							};
						}
						cmenu.Items.Add2(new ToolStripSeparator());
						foreach (var c in pane.Content)
						{
							var opt = cmenu.Items.Add2(new ToolStripMenuItem(c.DockControl.TabText, c.DockControl.TabIcon));
							cmenu.Opening += (s,a) =>
							{
								opt.Enabled = pane.VisibleContent != c;
							};
							opt.Click += (s,a) =>
							{
								pane.VisibleContent = c;
							};
						}
					}
					return cmenu;
				}

				/// <summary>Update when the dock pane content changes</summary>
				private void HandleContentChanging(object sender, ListChgEventArgs<IDockable> e)
				{
					if (e.ChangeType == ListChg.ItemAdded || e.ChangeType == ListChg.ItemRemoved || e.ChangeType == ListChg.Reset)
						ContextMenuStrip = DockPane != null ? CreateContextMenu(DockPane) : null;
				}

				/// <summary>Handle the close button being clicked</summary>
				private void HandleClose(object sender, EventArgs e)
				{
					if (DockPane.VisibleContent != null)
						DockPane.Content.Remove(DockPane.VisibleContent);
				}

				/// <summary>Handle the auto hide button being clicked</summary>
				private void HandleAutoHide(object sender, EventArgs e)
				{
					DockPane.IsAutoHide = !DockPane.IsAutoHide;
				}

				/// <summary>Handle the dock pane content menu being clicked</summary>
				private void HandleMenu(object sender, EventArgs e)
				{
					var btn = (CaptionButton)sender;
					if (ContextMenuStrip == null) return;
					ContextMenuStrip.Show(PointToScreen(btn.Bounds.BottomLeft()));
				}

				/// <summary>A custom button drawn on the title bar of a pane</summary>
				public class CaptionButton :Control
				{
					public CaptionButton(Bitmap image)
					{
						SetStyle(ControlStyles.SupportsTransparentBackColor, true);
						BackColor = Color.Transparent;
						Image = image;
						Visible = true;
					}

					/// <summary>Get/Set whether this control is visible</summary>
					public new bool Visible
					{
						get { return m_impl_visible; }
						set { base.Visible = m_impl_visible = value; }
					}
					private bool m_impl_visible;

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

					/// <summary>See initial visibility</summary>
					protected override void OnHandleCreated(EventArgs e)
					{
						base.OnHandleCreated(e);
						base.Visible = m_impl_visible;
					}
				}
			}

			/// <summary>A custom control containing a strip of tabs</summary>
			public class TabStripControl :TabStrip
			{
				public TabStripControl(DockPane owner)
					:base(EDockSite.Bottom, owner.DockContainer.Options, owner.DockContainer.Options.TabStrip)
				{
					DockPane = owner;
					GhostTabContent = null;
					GhostTabIndex = -1;
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

				/// <summary>True if the tab strip is 'activated'</summary>
				protected override bool IsActivated
				{
					get { return DockPane.Activated; }
				}

				/// <summary>The number of tabs (equal to the number of dockables in the dock pane + plus the ghost tab if present)</summary>
				public override int TabCount
				{
					get { return DockPane.Content.Count + (GhostTabContent != null ? 1 : 0); }
				}

				/// <summary>Gets the content in 'DockPane' with 'GhostTab' inserted at the appropriate position</summary>
				public override IEnumerable<IDockable> Content
				{
					get
					{
						// Insert's GhostTab into the collection of content and skip over 'GhostTabContent'
						// if it appears in the collection 'DockPane.Content'
						var c = DockPane.Content.GetIterator();
						for (int i = 0; !c.AtEnd && i != GhostTabIndex; c.MoveNext())
						{
							if (c.Current != GhostTabContent) ++i; else continue;
							yield return c.Current;
						}
						if (GhostTabContent != null && GhostTabIndex != -1)
						{
							yield return GhostTabContent;
						}
						for (; !c.AtEnd; c.MoveNext())
						{
							if (c.Current == GhostTabContent) continue;
							yield return c.Current;
						}
					}
				}

				/// <summary>Returns the dockable content under client space point 'pt', or null</summary>
				public override IDockable HitTestContent(Point pt)
				{
					// Get the hit tab index
					var index = HitTest(pt);

					// If the index is the ghost tab, return that
					if (GhostTabContent != null && index == GhostTabIndex)
						return GhostTabContent;

					// If the index is greater than the ghost tab, adjust the index
					if (GhostTabContent != null && index > GhostTabIndex)
						--index;

					// Return the content associated with the tab
					if (index >= 0 && index < DockPane.Content.Count)
						return DockPane.Content[index];

					// Missed...
					return null;
				}
			}
		}

		/// <summary>Provides the implementation of the docking functionality</summary>
		public class DockControl :IDisposable
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

				DockAddresses = new Dictionary<Branch, EDockSite[]>();
			}
			public void Dispose()
			{
				DockPane = null;
				DockContainer = null;
			}

			/// <summary>Get/Set the dock container that manages this content.</summary>
			public DockContainer DockContainer
			{
				[DebuggerStepThrough] get { return m_impl_dock_container; }
				set
				{
					if (m_impl_dock_container == value) return;
					if (m_impl_dock_container != null)
					{
						m_impl_dock_container.Forget(Dockable);
					}
					m_impl_dock_container = value;
					if (m_impl_dock_container != null)
					{
						m_impl_dock_container.Remember(Dockable);
					}
				}
			}
			private DockContainer m_impl_dock_container;

			/// <summary>The dock container, auto-hide panel, or floating window that this content is docked within</summary>
			internal ITreeHost TreeHost
			{
				get { return DockPane?.TreeHost; }
			}

			/// <summary>Get the control we're providing docking functionality for</summary>
			public Control Owner { [DebuggerStepThrough] get; private set; }

			/// <summary>Gets 'Owner' as an IDockable</summary>
			public IDockable Dockable
			{
				[DebuggerStepThrough] get { return (IDockable)Owner; }
			}

			/// <summary>Get/Set the pane that this content resides within.</summary>
			public DockPane DockPane
			{
				[DebuggerStepThrough] get { return m_impl_pane; }
				set
				{
					if (m_impl_pane == value) return;
					if (m_impl_pane != null)
					{
						m_impl_pane.Content.Remove(Dockable);
					}
					SetDockPaneInternal(value);
					if (m_impl_pane != null)
					{
						m_impl_pane.Content.Add(Dockable);
					}
				}
			}
			private DockPane m_impl_pane;
			internal void SetDockPaneInternal(DockPane pane)
			{
				// Calling 'DockControl.set_DockPane' changes the Content list in the DockPane
				// which calls this method to change the DockPane on the content without recursively
				// changing the content list.
				m_impl_pane = pane;

				// Record the dock container that 'pane' belongs to
				if (pane != null)
					DockContainer = pane.DockContainer;

				// If a pane is given, save the dock address for the root branch (if the pane is within a tree)
				if (pane != null && pane.Branch != null)
					DockAddresses[m_impl_pane.RootBranch] = DockAddress;
			}

			/// <summary>The name to use for this instance when saving layout to XML</summary>
			public string PersistName { get; private set; }

			/// <summary>The dock state to use if not otherwise given</summary>
			public EDockSite DefaultDockSite { get; private set; }

			/// <summary>Get/Set the dock site within the branch that contains 'DockPane'</summary>
			public EDockSite DockSite
			{
				get { return DockPane?.DockSite ?? EDockSite.Centre; }
				set
				{
					if (DockSite == value) return;

					// Must have an existing pane
					if (DockPane == null || DockPane.Branch == null)
						throw new Exception("Can only change the dock site of this item after it has been added to a dock container");

					// Move this content to a different dock site within 'branch'
					var branch = DockPane.Branch;
					var pane = branch.DockPane(value);
					pane.Content.Add(Dockable);
				}
			}

			/// <summary>The full location of this content in the tree of docked windows from Root branch to 'DockPane'</summary>
			public EDockSite[] DockAddress
			{
				get { return DockPane != null ? DockPane.DockAddress : new[] {EDockSite.None}; }
			}

			/// <summary>Get/Set whether this content is in a floating window</summary>
			public bool IsFloating
			{
				get { return DockPane?.RootBranch.TreeHost is FloatingWindow; }
				set
				{
					if (IsFloating == value) return;

					// Record properties of the Pane we're currently in, so we can
					// set them on new pane that will host this content 
					var strip_location = DockPane?.TabStripCtrl.StripLocation;

					// Float this dockable
					if (value)
					{
						// Get a floating window (preferably one that has hosted this item before)
						var fw = DockContainer.GetFloatingWindow(x => DockAddresses.ContainsKey(x.Root));

						// Get the dock address associated with this floating window
						var hs = DockAddressFor(fw.Root);

						// Add this dockable to the floating window
						fw.Add(Dockable, hs);

						// Ensure the floating window is visible
						if (!fw.Visible)
							fw.Show(DockContainer);
					}

					// Dock this content back in the dock container
					else
					{
						// Get the dock address associated with the dock container
						var hs = DockAddressFor(DockContainer.Root);

						// Return this content to the dock container
						DockContainer.Add(Dockable, hs);
					}

					// Copy pane state to the new pane
					if (DockPane != null)
					{
						if (strip_location != null)
							DockPane.TabStripCtrl.StripLocation = strip_location.Value;
					}
				}
			}

			/// <summary>Get/Set whether this content is in an auto-hide panel</summary>
			public bool IsAutoHide
			{
				get { return DockPane?.RootBranch.TreeHost is AutoHidePanel; }
				set
				{
					if (IsAutoHide == value) return;

					// Auto hide this dockable
					if (value)
					{
						// If currently docked in the centre, don't allow auto hide
						if (DockSite == EDockSite.Centre)
							throw new Exception("Panes docked in the centre position cannot auto hide");

						// Get the auto-hide panel associated with this dock site
						var ah = DockContainer.GetAutoHidePanel(DockSite);

						// Get the dock address associated with this panel
						var hs = DockAddressFor(ah.Root);

						// Add this dockable to the auto-hide panel
						ah.Add(Dockable, hs);
					}

					// Dock this content back in the dock container
					else
					{
						// Get the dock address associated with the dock container
						var hs = DockAddressFor(DockContainer.Root);

						// Return this content to the dock container
						DockContainer.Add(Dockable, hs);
					}
				}
			}

			/// <summary>A record of where this pane was located in a dock container, auto hide window, or floating window</summary>
			internal EDockSite[] DockAddressFor(Branch root)
			{
				return DockAddresses.GetOrAdd(root, x => new []{DefaultDockSite});
			}
			private Dictionary<Branch, EDockSite[]> DockAddresses { get; set; }

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

			/// <summary>A tool tip to display when the mouse hovers over the tab for this content</summary>
			public string TabToolTip
			{
				get { return m_impl_tab_tt ?? TabText; }
				set { m_impl_tab_tt = value; }
			}
			private string m_impl_tab_tt;

			/// <summary>A context menu to display when the tab for this content is right clicked</summary>
			public ContextMenuStrip TabCMenu
			{
				get { return m_impl_tab_cmenu; }
				set { m_impl_tab_cmenu = value; }
			}
			private ContextMenuStrip m_impl_tab_cmenu;

			/// <summary>Save state to XML</summary>
			public XElement ToXml(XElement node)
			{
				// Add the name to identify the content
				node.Add2(XmlTag.Name, PersistName, false);

				// Add info about the host of this content
				if (IsFloating)
				{
					// If hosted in a floating window, record the window id
					var fw = (FloatingWindow)DockPane.RootBranch.TreeHost;
					node.Add2(XmlTag.Host, "{0},{1}".Fmt(nameof(FloatingWindow),fw.Id), false);
				}
				else if (IsAutoHide)
				{
					// If hosted in an auto hide panel, record the panel id
					var ah = DockPane?.RootBranch.TreeHost as AutoHidePanel;
					node.Add2(XmlTag.Host, "{0},{1}".Fmt(nameof(AutoHidePanel), ah.StripLocation), false);
				}
				else
				{
					// Otherwise record that we're hosted in the main dock container
					node.Add2(XmlTag.Host, "{0},0".Fmt(nameof(DockContainer)), false);
				}

				// Add the location of where in the host this content is stored
				node.Add2(XmlTag.Address, string.Join(",", DockAddress), false);

				return node;
			}
		}

		/// <summary>General options for the dock container</summary>
		public class OptionData
		{
			public OptionData()
			{
				AllowUserDocking          = true;
				DoubleClickTitleBarToDock = true;
				IndicatorPadding          = 15;
				TitleBar                  = new TitleBarData();
				TabStrip                  = new TabStripData();
				AutoHideStrip             = new TabStripData();
			}

			/// <summary>Get/Set whether the user is allowed to drag and drop panes</summary>
			public bool AllowUserDocking { get; set; }

			/// <summary>Get/Set whether double clicking on the title bar of a floating window returns its contents to the dock container</summary>
			public bool DoubleClickTitleBarToDock { get; set; }

			/// <summary>The gap between the edge of the dock container and the edge docking indicators</summary>
			public int IndicatorPadding { get; set; }

			/// <summary>Options for the dock pane title bars</summary>
			public TitleBarData TitleBar { get; private set; }

			/// <summary>Options for the dock pane tab strips</summary>
			public TabStripData TabStrip { get; private set; }

			/// <summary>Options for the auto-hide panel tab strips</summary>
			public TabStripData AutoHideStrip { get; private set; }

			/// <summary>Save the options as XML</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(AllowUserDocking)         , AllowUserDocking, false);
				node.Add2(nameof(DoubleClickTitleBarToDock), DoubleClickTitleBarToDock, false);
				node.Add2(nameof(IndicatorPadding)         , IndicatorPadding, false);
				node.Add2(nameof(TitleBar)                 , TitleBar, false);
				node.Add2(nameof(TabStrip)                 , TabStrip, false);
				node.Add2(nameof(AutoHideStrip)            , AutoHideStrip, false);
				return node;
			}

			/// <summary>Settings for pane title bars</summary>
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

				/// <summary>Save the options as XML</summary>
				public XElement ToXml(XElement node)
				{
					node.Add2(nameof(ActiveGrad), ActiveGrad, false);
					node.Add2(nameof(InactiveGrad), InactiveGrad, false);
					node.Add2(nameof(Padding), Padding, false);
					node.Add2(nameof(TextFont), TextFont, false);
					return node;
				}
			}

			/// <summary>Settings for tab strips</summary>
			public class TabStripData
			{
				public TabStripData()
				{
					ActiveStrip = new ColourSet(
						text: SystemColors.ActiveCaptionText,
						beg: SystemColors.Control.Lerp(Color.Black, 0.1f),
						end: SystemColors.Control.Lerp(Color.Black, 0.1f));

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
					TabPadding        = new Padding(3, 3, 3, 3);
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

				/// <summary>
				/// The space between the edge of the tab strip background edge and the tabs.
				/// Padding is relative to Top aligned strip. So 'Top' is interpreted as furtherest from the content
				///  and 'Bottom' is nearest to the content, regardless of strip orientation/top/bottom.</summary>
				public Padding StripPadding { get; set; }

				/// <summary>The space between the tab edge and the tab text,icon,etc</summary>
				public Padding TabPadding { get; set; }

				/// <summary>Save the options as XML</summary>
				public XElement ToXml(XElement node)
				{
					node.Add2(nameof(ActiveStrip), ActiveStrip, false);
					node.Add2(nameof(InactiveStrip), InactiveStrip, false);
					node.Add2(nameof(ActiveTab), ActiveTab, false);
					node.Add2(nameof(InactiveTab), InactiveTab, false);
					node.Add2(nameof(ActiveFont), ActiveFont, false);
					node.Add2(nameof(InactiveFont), InactiveFont, false);
					node.Add2(nameof(MinWidth), MinWidth, false);
					node.Add2(nameof(MaxWidth), MaxWidth, false);
					node.Add2(nameof(TabSpacing), TabSpacing, false);
					node.Add2(nameof(IconToTextSpacing), IconToTextSpacing, false);
					node.Add2(nameof(StripPadding), StripPadding, false);
					node.Add2(nameof(TabPadding), TabPadding, false);
					return node;
				}
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

				/// <summary>Save the options as XML</summary>
				public XElement ToXml(XElement node)
				{
					node.Add2(nameof(Beg), Beg, false);
					node.Add2(nameof(End), End, false);
					node.Add2(nameof(Mode), Mode, false);
					node.Add2(nameof(Border), Border, false);
					node.Add2(nameof(Text), Text, false);
					node.Add2(nameof(Blend), Blend, false);
					return node;
				}
			}
		}

		/// <summary>Records the proportions used for each dock site at a branch level</summary>
		public class DockSizeData
		{
			/// <summary>The minimum size of a child control</summary>
			public const int MinChildSize = 20;

			public DockSizeData(float left = 0.25f, float top = 0.25f, float right = 0.25f, float bottom = 0.25f)
			{
				Left   = left;
				Top    = top;
				Right  = right;
				Bottom = bottom;
			}
			public DockSizeData(DockSizeData rhs) :this(rhs.Left, rhs.Top, rhs.Right, rhs.Bottom)
			{}
			public DockSizeData(XElement node)
			{
				Left   = node.Element(nameof(Left)).As(Left);
				Top    = node.Element(nameof(Top)).As(Top);
				Right  = node.Element(nameof(Right)).As(Right);
				Bottom = node.Element(nameof(Bottom)).As(Bottom);
			}

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
			internal Padding GetSizesForRect(Rectangle rect, EDockMask docked_mask, Size? centre_min_size = null)
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
			internal int GetSize(EDockSite location, Rectangle rect, EDockMask docked_mask)
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
			internal void SetSize(EDockSite location, Rectangle rect, int value)
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

			/// <summary>Save the state as XML</summary>
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(Left), Left, false);
				node.Add2(nameof(Top), Top, false);
				node.Add2(nameof(Right), Right, false);
				node.Add2(nameof(Bottom), Bottom, false);
				return node;
			}

			public static DockSizeData Halves { get { return new DockSizeData(0.5f, 0.5f, 0.5f, 0.5f); } }
			public static DockSizeData Quarters { get { return new DockSizeData(0.25f, 0.25f, 0.25f, 0.25f); } }
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

					// Update the bounds of the splitter within the parent (if not dragging)
					if (m_drag_bar == null)
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

				// Use a form as the drag indicator so we can move it to outside the parent if necessary
				// Also helps avoid troublesome Parenting when the splitter is used as an edge resizer.
				m_drag_bar = new Form
				{
					ShowInTaskbar = false,
					StartPosition = FormStartPosition.Manual,
					FormBorderStyle = FormBorderStyle.None,
					MinimumSize = new Size(1,1),
					ControlBox = false,
					Margin = Padding.Empty,
					Padding = Padding.Empty,
					BackColor = SystemColors.ControlDarkDark,
				};
				m_drag_bar.Load += (s,a) =>
				{
					m_drag_bar.Location = Parent.PointToScreen(Orientation == Orientation.Vertical
						? new Point(Position - BarWidth/2, Area.Top)
						: new Point(Area.Left, Position - BarWidth/2));
					m_drag_bar.Size = Orientation == Orientation.Vertical
						? new Size(BarWidth, Area.Height)
						: new Size(Area.Width, BarWidth);
				};
				m_drag_bar.Show();
			}

			/// <summary>Raised on mouse move during a resizing operation (after Position has been updated)</summary>
			public event EventHandler Dragging;
			protected virtual void OnDragging()
			{
				m_drag_bar.Location = Parent.PointToScreen(Orientation == Orientation.Vertical
					? new Point(Area.Left + Position - BarWidth/2, Area.Top)
					: new Point(Area.Left, Area.Top + Position - BarWidth/2));

				Dragging.Raise(this);
			}

			/// <summary>Raised on left mouse up to end a resizing operation</summary>
			public event EventHandler DragEnd;
			protected virtual void OnDragEnd()
			{
				Util.Dispose(ref m_drag_bar);
				DragEnd.Raise(this);

				// Update the splitter bounds after the drag bar has been released
				// Also, after DragEnd has been raised so that if the splitter is dragged outside
				// the parent, the event handler allows the observer to resize the parent
				UpdateSplitterBounds();
			}

			/// <summary>Gets the bounds of the left/top side of the splitter (in parent control space)</summary>
			public Rectangle BoundsLT
			{
				get
				{
					if (Parent == null) return Rectangle.Empty;
					var rc = Area;
					return Orientation == Orientation.Vertical
						? Rectangle.FromLTRB(rc.Left, rc.Top, rc.Left + Position - BarWidth/2, rc.Bottom)
						: Rectangle.FromLTRB(rc.Left, rc.Top, rc.Right, rc.Top + Position - BarWidth/2);
				}
			}

			/// <summary>Gets the bounds of the right/bottom side of the splitter (in parent control space)</summary>
			public Rectangle BoundsRB
			{
				get
				{
					if (Parent == null) return Rectangle.Empty;
					var rc = Area;
					return Orientation == Orientation.Vertical
						? Rectangle.FromLTRB(rc.Left + Position + BarWidth/2, rc.Top, rc.Right, rc.Bottom)
						: Rectangle.FromLTRB(rc.Left, rc.Top + Position + BarWidth/2, rc.Right, rc.Bottom);
				}
			}

			/// <summary>Update the bounds of the splitter within the parent</summary>
			public void UpdateSplitterBounds()
			{
				// No parent, or currently dragging
				if (Parent == null)
					return;

				// Position the splitter within the parent
				var rc = Area;
				Parent.Invalidate(Bounds);
				Bounds = Orientation == Orientation.Vertical
					? new Rectangle(rc.Left + Position - BarWidth/2, rc.Top, BarWidth, rc.Height)
					: new Rectangle(rc.Left, rc.Top + Position - BarWidth/2, rc.Width, BarWidth);
				Parent.Invalidate(Bounds);
				Invalidate();
				Update();
			}

			/// <summary>Set the position of the splitter based on client space point 'point_cs'</summary>
			private void SetPositionFromPoint(Point point_cs)
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
				var r = ClientRectangle;
				gfx.FillRectangle(SystemBrushes.Control, r);

				// Draw a shadow line down
				if (Orientation == Orientation.Vertical)
				{
					gfx.DrawLine(SystemPens.ControlDark, r.Left, r.Top, r.Left, r.Bottom-1);
					gfx.DrawLine(SystemPens.ControlDark, r.Right-1, r.Top, r.Right-1, r.Bottom-1);
				}
				else
				{
					gfx.DrawLine(SystemPens.ControlDark, r.Left, r.Top, r.Right-1, r.Top);
					gfx.DrawLine(SystemPens.ControlDark, r.Left, r.Bottom-1, r.Right-1, r.Bottom-1);
				}
			}

			/// <summary>Handle dragging</summary>
			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);
				if (e.Button == MouseButtons.Left)
				{
					Capture = true;

					SetPositionFromPoint(e.Location);
					OnDragBegin();
				}
			}
			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				if (Capture)
				{
					SetPositionFromPoint(e.Location);
					OnDragging();
				}
			}
			protected override void OnMouseUp(MouseEventArgs e)
			{
				base.OnMouseUp(e);
				if (Capture)
				{
					Capture = false;
					SetPositionFromPoint(e.Location);
					OnDragEnd();
				}
			}
			private Form m_drag_bar;

			/// <summary>Update the splitter position when re-parented</summary>
			protected override void OnParentChanged(EventArgs e)
			{
				base.OnParentChanged(e);
				UpdateSplitterBounds();
			}
		}

		/// <summary>Common tab strip implementation</summary>
		public class TabStrip :Control
		{
			protected OptionData m_opts;
			private ToolTip m_tt;

			public TabStrip(EDockSite ds, OptionData opts, OptionData.TabStripData tabstrip_opts)
			{
				SetStyle(ControlStyles.Selectable, false);
				ResizeRedraw = true;
				AllowDrop = true;

				m_opts = opts;
				TabStripOpts = tabstrip_opts;
				m_tt = new ToolTip{ShowAlways = true, InitialDelay = 2000};
				Visible = true;

				StripLocation = ds;
				StripSize = PreferredStripSize;
			}
			protected override void Dispose(bool disposing)
			{
				Util.Dispose(ref m_tt);
				base.Dispose(disposing);
			}

			/// <summary>Get/Set whether this control is visible</summary>
			public new bool Visible
			{
				get { return m_impl_visible; }
				set { base.Visible = m_impl_visible = value; }
			}
			private bool m_impl_visible;

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

			/// <summary>Get the options specific to this tab strip</summary>
			protected OptionData.TabStripData TabStripOpts { get; private set; }

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
					Parent?.PerformLayout();
				}
			}
			private EDockSite m_impl_strip_loc;

			/// <summary>The size of the tab strip</summary>
			public virtual int StripSize
			{
				get { return m_impl_strip_size; }
				set
				{
					if (m_impl_strip_size == value) return;
					m_impl_strip_size = value;
					Invalidate();
				}
			}
			private int m_impl_strip_size;

			/// <summary>Measure the size required by this tab strip based on the user settings</summary>
			public int PreferredStripSize
			{
				get
				{
					var opts = TabStripOpts;
					var tabh = opts.TabPadding.Top + Maths.Max(opts.InactiveFont.Height, opts.ActiveFont.Height, opts.IconSize) + opts.TabPadding.Bottom;
					return opts.StripPadding.Top + tabh + opts.StripPadding.Bottom;
				}
			}

			/// <summary>Get the area for the tab strip</summary>
			public virtual Rectangle StripRect
			{
				get { return ClientRectangle; }
			}

			/// <summary>
			/// Returns the index of the tab under client space point 'pt'.
			/// Returns -1 if 'pt' is not within the tab strip, or 'TabCount' if at or after the end of the last tab</summary>
			public int HitTest(Point pt)
			{
				// Not within the tab strip?
				if (!StripRect.Contains(pt))
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
			public virtual IDockable HitTestContent(Point pt)
			{
				// Get the hit tab index
				var index = HitTest(pt);

				// Return the content associated with the tab
				if (index >= 0 && index < TabCount)
					return Content.ElementAt(index);

				// Missed...
				return null;
			}

			/// <summary>Get the transform to apply to the strip and tabs based on the StripLocation</summary>
			public Matrix Transform
			{
				get
				{
					var m = new Matrix();
					switch (StripLocation)
					{
					case EDockSite.Left:
						{
							m.Translate(StripSize, 0, MatrixOrder.Prepend);
							m.Rotate(90f, MatrixOrder.Prepend);
							break;
						}
					case EDockSite.Right:
						{
							m.Translate(DisplayRectangle.Width, 0, MatrixOrder.Prepend);
							m.Rotate(90f, MatrixOrder.Prepend);
							break;
						}
					case EDockSite.Top:
						{
							break;
						}
					case EDockSite.Bottom:
						{
							m.Translate(0, DisplayRectangle.Height - StripSize, MatrixOrder.Prepend);
							break;
						}
					}
					return m;
				}
			}

			/// <summary>True if the strip is orientated horizontally, false if vertically</summary>
			protected bool Horizontal
			{
				get { return StripLocation == EDockSite.Top || StripLocation == EDockSite.Bottom; }
			}

			/// <summary>True if the tab strip is 'activated'</summary>
			protected virtual bool IsActivated { get { return false; } }

			/// <summary>The number of tabs</summary>
			public virtual int TabCount { get { return Content.Count(); } }

			/// <summary>Get the content that we're showing tabs for</summary>
			public virtual IEnumerable<IDockable> Content { get { yield break; } }

			/// <summary>Returns the size of the tab strip control given the available 'display_area' and strip location</summary>
			public virtual Rectangle CalcBounds(Rectangle display_area)
			{
				// No tab strip if there is only one item in the pane
				if (TabCount <= 1)
					return Rectangle.Empty;

				var r = new RectangleRef(display_area);
				switch (StripLocation) {
				default: throw new Exception("Tab strip location is not valid");
				case EDockSite.Top:    r.Bottom = r.Top    + StripSize; break;
				case EDockSite.Bottom: r.Top    = r.Bottom - StripSize; break;
				case EDockSite.Left:   r.Right  = r.Left   + StripSize; break;
				case EDockSite.Right:  r.Left   = r.Right  - StripSize; break;
				}
				return r;
			}

			/// <summary>Return the tabs corresponding to the content in 'DockPane' (in tab strip space, starting at 'FirstVisibleTabIndex', assuming horizontal layout)</summary>
			public IEnumerable<TabBtn> VisibleTabs
			{
				get
				{
					if (TabCount == 0)
						yield break;

					var opts = TabStripOpts;
					 
					// The range along the tab strip in which tab buttons are allowed
					var xbeg = opts.StripPadding.Left;
					var xend = (Horizontal ? Width : Height) - opts.StripPadding.Right;

					var width = xend - xbeg;
					var min_width = opts.MinWidth;
					var max_width = Maths.Clamp(width/TabCount, opts.MinWidth, opts.MaxWidth);

					using (var gfx = CreateGraphics())
					{
						// Iterate over the visible tabs
						var x = xbeg;
						var tab_index = FirstVisibleTabIndex;
						foreach (var content in Content.Skip(tab_index))
						{
							// Create a tab to represent the content
							var tab = new TabBtn(this, content, tab_index, gfx, x, min_width, max_width);

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

			/// <summary>Ensure the tab with index value 'idx' is visible in the tab strip</summary>
			public void MakeTabVisible(int idx)
			{}

			/// <summary>Paint the TabStrip control</summary>
			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);

				if (DisplayRectangle.Area() > 0)
				{
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
					var opts = TabStripOpts;
					var cols = IsActivated ? opts.ActiveStrip : opts.InactiveStrip;
					using (var brush = new LinearGradientBrush(ClientRectangle, cols.Beg, cols.End, cols.Mode) { Blend=cols.Blend })
						gfx.FillRectangle(brush, e.ClipRectangle);
				}
			}

			/// <summary>Handle mouse click on the tab strip</summary>
			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);

				// If left click on a tab and user docking is allowed, start a drag operation
				if (e.Button == MouseButtons.Left && m_opts.AllowUserDocking)
				{
					// Record the mouse down location
					m_mouse_down_at = e.Location;
					Capture = true;
				}

				// Display a tab specific context menu on right click
				if (e.Button == MouseButtons.Right)
				{
					var content = HitTestContent(e.Location);
					if (content != null && content.DockControl.TabCMenu != null)
						content.DockControl.TabCMenu.Show(PointToScreen(e.Location));
				}
			}
			protected override void OnMouseMove(MouseEventArgs e)
			{
				if (Capture && Util.Distance(e.Location, m_mouse_down_at) > 5)
				{
					Capture = false;

					// Begin dragging the content associated with the tab
					var content = HitTestContent(m_mouse_down_at);
					if (content != null)
						DragBegin(content);
				}

				// Check for the mouse hovering over a tab
				if (e.Button == MouseButtons.None)
				{
					var content = HitTestContent(e.Location);
					if (content != m_hovered_tab)
					{
						m_hovered_tab = content;
						var tt = string.Empty;
						if (content != null)
						{
							// If the tool tip has been set explicitly, or the tab content is clipped, set the tool tip string
							var tabbtn = VisibleTabs.First(x => x.Content == content);
							if (tabbtn.Clipped || !ReferenceEquals(content.DockControl.TabToolTip, content.DockControl.TabText))
								tt = content.DockControl.TabToolTip;
						}
						this.ToolTip(m_tt, tt);
					}
				}

				base.OnMouseMove(e);
			}
			protected override void OnMouseUp(MouseEventArgs e)
			{
				// If the mouse is still captured, then treat this as a mouse click instead of a drag
				if (Capture)
				{
					Capture = false;

					// Find the tab that was clicked
					var content = HitTestContent(m_mouse_down_at);
					OnTabClick(new TabClickEventArgs(content, e));
				}
				base.OnMouseUp(e);
			}
			private Point m_mouse_down_at;
			private IDockable m_hovered_tab;

			/// <summary>Toggle floating when double clicked</summary>
			protected override void OnMouseDoubleClick(MouseEventArgs e)
			{
				base.OnMouseDoubleClick(e);

				var content = HitTestContent(e.Location);
				OnTabDblClick(new TabClickEventArgs(content, e));
			}

			/// <summary>Handle mouse clicks on a tab</summary>
			protected virtual void OnTabClick(TabClickEventArgs e)
			{
				// Make the content the active content on its dock pane
				if (e.Content?.DockControl.DockPane != null)
					e.Content.DockControl.DockPane.VisibleContent = e.Content;

				Invalidate();
			}

			/// <summary>Handle double click on a tab</summary>
			protected virtual void OnTabDblClick(TabClickEventArgs e)
			{
				if (e.Content != null)
					e.Content.DockControl.IsFloating = !e.Content.DockControl.IsFloating;
			}

			/// <summary>Args for when a tab button is clicked</summary>
			protected class TabClickEventArgs :MouseEventArgs
			{
				public TabClickEventArgs(IDockable content, MouseEventArgs mouse)
					:base(mouse.Button, mouse.Clicks, mouse.X, mouse.Y, mouse.Delta)
				{
					Content = content;
				}

				/// <summary>The content that was clicked</summary>
				public IDockable Content { get; private set; }
			}

			/// <summary>Helper overload of invalidate</summary>
			protected void Invalidate(object sender, EventArgs e)
			{
				Invalidate();
			}

			/// <summary>See initial visibility</summary>
			protected override void OnHandleCreated(EventArgs e)
			{
				base.OnHandleCreated(e);
				base.Visible = m_impl_visible;
			}
		}

		/// <summary>A tab that displays the name of the content and it's icon</summary>
		public class TabBtn
		{
			/// <summary>
			/// 'gfx' is a Graphics instance used to measure the content of the tab.
			/// 'x' is the distance along the tab strip (in pixels) for this tab.</summary>
			internal TabBtn(TabStrip strip, IDockable content, int index, Graphics gfx, int x, int min_width, int max_width)
			{
				Strip = strip;
				Content = content;
				Index = index;

				// Calculate the bounding rectangle that would contain this Tab in tab strip space.
				var opts = Content.DockControl.DockContainer.Options.TabStrip;
				var active = Content == Content.DockControl.DockContainer.ActiveContent;
				var font = active ? opts.ActiveFont : opts.InactiveFont;

				var sz = gfx.MeasureString(Text, font, max_width, FmtFlags);
				var w = opts.TabPadding.Left + (Icon != null ? opts.IconSize + opts.IconToTextSpacing : 0) + (int)(sz.Width + 0.5f) + opts.TabPadding.Right;
				var width = Maths.Clamp(w, min_width, max_width);
				var above = strip.StripLocation == EDockSite.Top || strip.StripLocation == EDockSite.Right;
				var top = above ? opts.StripPadding.Top : opts.StripPadding.Bottom;
				var bot = above ? opts.StripPadding.Bottom : opts.StripPadding.Top;
				Bounds = Rectangle.FromLTRB(x, top, x + width, Strip.StripSize - bot);
				Clipped = width != w;
			}

			/// <summary>The tab strip that owns this tab</summary>
			internal TabStrip Strip { get; private set; }

			/// <summary>The content that this tab is associated with</summary>
			public IDockable Content { get; private set; }

			/// <summary>The index of this tab within the owning tab strip</summary>
			public int Index { get; private set; }

			/// <summary>The bounds of this tab button in horizontal tab strip space</summary>
			public Rectangle Bounds { get; private set; }

			/// <summary>True if the tab content is larger than the size of the tab</summary>
			public bool Clipped { get; private set; }

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
				get { return new StringFormat(StringFormatFlags.NoWrap | StringFormatFlags.FitBlackBox) { Trimming = StringTrimming.EllipsisCharacter }; }
			}

			/// <summary>Draw the tab</summary>
			public void Paint(Graphics gfx)
			{
				var opts = Content.DockControl.DockContainer.Options.TabStrip;
				var active = Content == Content.DockControl.DockContainer.ActiveContent;
				var cols = active ? opts.ActiveTab : opts.InactiveTab;
				var font = active ? opts.ActiveFont : opts.InactiveFont;
				var rect = Bounds;

				// Fill the background
				using (var brush = new LinearGradientBrush(rect, cols.Beg, cols.End, cols.Mode) { Blend=cols.Blend })
					gfx.FillRectangle(brush, rect);

				// Paint a border if a border colour is given
				if (cols.Border != Color.Empty)
				{
					using (var pen = new Pen(cols.Border))
					{
						gfx.DrawLine(pen, rect.Left, rect.Top, rect.Left, rect.Bottom);
						gfx.DrawLine(pen, rect.Right-1, rect.Top, rect.Right-1, rect.Bottom);
						if (Strip.StripLocation == EDockSite.Right ) gfx.DrawLine(pen, rect.Left, rect.Top+1, rect.Right-1, rect.Top+1);
						if (Strip.StripLocation == EDockSite.Top   ) gfx.DrawLine(pen, rect.Left, rect.Top, rect.Right-1, rect.Top);
						if (Strip.StripLocation == EDockSite.Bottom) gfx.DrawLine(pen, rect.Left, rect.Bottom-1, rect.Right-1, rect.Bottom-1);
						if (Strip.StripLocation == EDockSite.Left  ) gfx.DrawLine(pen, rect.Left, rect.Bottom, rect.Right-1, rect.Bottom);
					}
				}

				var x = rect.X + opts.TabPadding.Left;

				// Draw the icon
				if (Icon != null)
				{
					var r = new Rectangle(x, (rect.Height - opts.IconSize)/2, opts.IconSize, opts.IconSize);
					if (r.Area() > 0)
					{
						gfx.DrawImage(Icon, r);
						x += r.Width + opts.IconToTextSpacing;
					}
				}

				// Draw the text
				if (Text.HasValue())
				{
					var r = new RectangleF(x, (rect.Height - font.Height)/2, rect.Right - opts.TabPadding.Right - x, font.Height);
					if (r.Area() > 0)
						using (var bsh = new SolidBrush(cols.Text))
							gfx.DrawString(Text, font, bsh, r, FmtFlags);
				}
			}
		}

		/// <summary>An invisible modal window that manages dragging of panes or contents</summary>
		private class DragHandler :Form
		{
			// Create a static instance of the dock site bitmap. 'Resources.dock_site_cross' returns a new instance each time
			private static readonly Bitmap DockSiteCrossLg = Resources.dock_site_cross_lg;
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
				DraggedItem = item;
				DropRootBranch = null;
				DropAddress = new EDockSite[0];
				DropIndex = -1;

				// Create a form with a null region so that we have an invisible modal dialog
				Name = "Drop handler";
				FormBorderStyle = FormBorderStyle.None;
				ShowInTaskbar = false;
				Region = new Region(Rectangle.Empty);

				// Create the semi-transparent non-modal form for the dragged item
				Ghost = new GhostPane(item, this) {Name = "Ghost"};

				// Create the dock site indicators
				IndCrossLg = new Indicator(DockSiteCrossLg, this) {Name = "Cross Large"};
				IndCrossSm = new Indicator(DockSiteCrossSm, this) {Name = "Cross Small"};
				IndLeft    = new Indicator(DockSiteLeft   , this) {Name = "Left"};
				IndTop     = new Indicator(DockSiteTop    , this) {Name = "Top"};
				IndRight   = new Indicator(DockSiteRight  , this) {Name = "Right"};
				IndBottom  = new Indicator(DockSiteBottom , this) {Name = "Bottom"};

				// Set the positions of the edge docking indicators
				var opts = DockContainer.Options;
				IndLeft  .Location = DockContainer.PointToScreen(new Point(opts.IndicatorPadding, (DockContainer.Height - IndLeft.Height)/2));
				IndTop   .Location = DockContainer.PointToScreen(new Point((DockContainer.Width - IndTop.Width)/2, opts.IndicatorPadding));
				IndRight .Location = DockContainer.PointToScreen(new Point(DockContainer.Width - opts.IndicatorPadding - IndRight.Width, (DockContainer.Height - IndRight.Height)/2));
				IndBottom.Location = DockContainer.PointToScreen(new Point((DockContainer.Width - IndBottom.Width)/2, DockContainer.Height - opts.IndicatorPadding - IndBottom.Height));

				DialogResult = DialogResult.Cancel;
			}
			protected override void Dispose(bool disposing)
			{
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
				IndLeft  .Visible = true;
				IndTop   .Visible = true;
				IndRight .Visible = true;
				IndBottom.Visible = true;
				HitTestDropLocations(MousePosition);
			}
			protected override void OnKeyDown(KeyEventArgs e)
			{
				base.OnKeyDown(e);
				if (e.KeyCode == Keys.Escape)
				{
					DialogResult = DialogResult.Cancel;
					Close();
				}
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
				DialogResult = DialogResult.OK;
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
				DockContainer.Focus();
				HoveredPane = null;

				// Commit the move on successful close
				if (DialogResult == DialogResult.OK)
				{
					// No address means float in a new floating window
					if (DropAddress.Length == 0)
					{
						// Float the dragged content
						var dockable = DraggedItem as IDockable;
						if (dockable != null)
						{
							dockable.DockControl.IsFloating = true;
						}

						// Or, float the dragged dock pane
						var dockpane = DraggedItem as DockPane;
						if (dockpane != null)
						{
							dockable = dockpane.VisibleContent;
							dockpane.IsFloating = true;
						}

						// Set the location of the floating window to the last position of the ghost
						var fw = dockable?.DockControl.TreeHost as FloatingWindow;
						if (fw != null) fw.Location = Ghost.Location;
					}

					// Otherwise dock the dragged item at the dock address
					else
					{
						// Dock the dragged item
						var dockable = DraggedItem as IDockable;
						if (dockable != null)
							DropRootBranch.Add(dockable, DropAddress);

						var dockpane = DraggedItem as DockPane;
						if (dockpane != null)
						{
							var target = DropRootBranch.DockPane(DropAddress.First(), DropAddress.Skip(1));
							using (dockpane.SuspendLayout(true))
							using (target.SuspendLayout(true))
								target.Content.AddRange(dockpane.Content.ToArray());
						}
					}
				}

				Capture = false;
			}

			/// <summary>The dock container that created this drop handler</summary>
			private DockContainer DockContainer { get; set; }

			/// <summary>The item being dragged (either a DockPane or IDockable)</summary>
			public object DraggedItem { get; private set; }

			/// <summary>The dockable to use when inserting the dragged item into another pane</summary>
			public IDockable DraggedDockable
			{
				get { return (DraggedItem as IDockable) ?? ((DockPane)DraggedItem).VisibleContent; }
			}

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
						Util.Dispose(ref m_impl_ghost);
					}
					m_impl_ghost = value;
					if (m_impl_ghost != null)
					{}
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
					if (m_impl_cross_lg != null)
					{}
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
					if (m_impl_cross_sm != null)
					{}
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
					{}
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
					{}
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
					{}
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
					{}
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

			/// <summary>The root of the tree in which to drop the item</summary>
			private Branch DropRootBranch { get; set; }

			/// <summary>Where to dock the dropped item. If empty drop in a new floating window</summary>
			private EDockSite[] DropAddress { get; set; }

			/// <summary>The index position of where to drop within a pane</summary>
			private int DropIndex { get; set; }

			/// <summary>Test the location 'screen_pt' at a possible drop location, and update the ghost and indicators</summary>
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

					// Get the mouse point in indicator space and test if it's within the indicator regions
					var pt = ind.PointToClient(screen_pt);
					snap_to = ind.CheckSnapTo(pt);
					if (snap_to == null)
						continue;

					// See if the ghost should snap to a dock site
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

				// Check whether the mouse is over the title bar of the dock pane
				if (pane != null && !over_indicator)
				{
					var title = pane.TitleCtrl;
					if (title.ClientRectangle.Contains(title.PointToClient(screen_pt)))
						snap_to = EDropSite.PaneCentre;
				}

				// Determine the drop address
				GetDropAddress(snap_to, pane, tab_index);

				// Update the position of the ghost
				PositionGhost(screen_pt);

				// If the mouse isn't over an indicator, update the positions of the indicators
				if (!over_indicator)
					PositionCrossIndicator(pane);
			}

			/// <summary>Record the location to drop</summary>
			private void GetDropAddress(EDropSite? snap_to, DockPane pane, int tab_index)
			{
				// No pane or snap-to means float in a new window
				if (pane == null || snap_to == null)
				{
					DropAddress = new EDockSite[0];
					DropRootBranch = null;
					return;
				}

				var snap = snap_to.Value;
				var ds = (EDockSite)(snap & EDropSite.DockSiteMask);
				var dm = (EDockMask)(1 << (int)ds);

				// Drop in the same tree as 'pane'
				DropRootBranch = pane.RootBranch;
				DropIndex = tab_index;

				if (ds == EDockSite.Centre)
				{
					// All EDockSite.Centre's dock to 'pane'
					DropAddress = pane.DockAddress;
				}
				else if (snap.HasFlag(EDropSite.Pane))
				{
					// Dock to a site within the current pane
					DropAddress = pane.DockAddress.Concat(ds).ToArray();
				}
				else if (snap.HasFlag(EDropSite.Branch))
				{
					// Snap to a site in the branch that owns 'pane'
					var branch = pane.Branch;
					var address = branch.DockAddress.ToList();

					// While the child at 'ds' is a branch, append 'EDockSite.Centre's to the address
					for (; branch.Child[ds].Ctrl is Branch; branch = branch.Child[ds].Branch, ds = EDockSite.Centre) { address.Add(ds); }
					address.Add(ds);

					DropAddress = address.ToArray();
				}
				else if (snap.HasFlag(EDropSite.Root))
				{
					// Snap to an edge dock site at the root branch level
					var branch = pane.RootBranch;
					var address = branch.DockAddress.ToList();

					// While the child at 'ds' is a branch, append 'EDockSite.Centre's to the address
					for (; branch.Child[ds].Ctrl is Branch; branch = branch.Child[ds].Branch, ds = EDockSite.Centre) { address.Add(ds); }
					address.Add(ds);

					DropAddress = address.ToArray();
				}
			}

			/// <summary>Position the ghost form at the current drop address</summary>
			public void PositionGhost(Point screen_pt)
			{
				var region = (Region)null;
				var bounds = Rectangle.Empty;

				// No address means floating
				if (DropAddress.Length == 0 || DropRootBranch == null)
				{
					bounds = new Rectangle(screen_pt - DraggeeOfs, DraggeeSize);
					region = new Region(bounds.Size.ToRect());
				}
				else
				{
					// Navigate as far down the tree as we can for the drop address.
					// The last dock site in the address should be a null child or a dock pane.
					// The second to last dock site should be a branch or a pane.
					var branch = DropRootBranch;
					var ds = DropAddress.GetIterator();
					for (; !ds.AtEnd && branch.Child[ds.Current].Ctrl is Branch; branch = branch.Child[ds.Current].Branch, ds.MoveNext()) {}
					Debug.Assert(!ds.AtEnd, "The address ends at a branch node, not a pane or null-child");
					var target = branch.Child[ds.Current];

					// If the target is empty, then just fill the whole target area
					var pane = target.DockPane;
					if (pane == null)
					{
						var docksite = DropAddress.Last();
						var rect = DockSiteBounds(docksite, branch.ClientRectangle, branch.DockedMask | (EDockMask)(1 << (int)docksite), branch.DockSizes);
						bounds = branch.RectangleToScreen(rect);
						region = new Region(bounds.Size.ToRect());
					}
					// If there is a pane at the target position then snap to fill the pane or some child area within the pane.
					else
					{
						// Update the pane being hovered
						HoveredPane = pane;
						HoveredPaneTabIndex = DropIndex;

						// If there is one more dock site in the address, then the drop target is a child area within 'pane'.
						if (ds.MoveNext())
						{
							var rect = DockSiteBounds(ds.Current, pane.ClientRectangle, (EDockMask)(1 << (int)ds.Current), DockSizeData.Halves);
							bounds = pane.RectangleToScreen(rect);
							region = new Region(bounds.Size.ToRect());
						}
						// Otherwise, the target area is the pane itself.
						else
						{
							// If 'pane' is the last dock site in the address, fill the pane. Otherwise get the child area within the pane
							var rect = pane.ClientRectangle;
							bounds = pane.RectangleToScreen(rect);

							// Limit the shape of the ghost to the pane content area
							region = new Region(rect);
							region.Exclude(pane.TitleCtrl.Bounds);
							region.Exclude(pane.TabStripCtrl.Bounds);

							// If a tab on the dock pane is being hovered, include that area in the ghost form's region
							if (DropIndex != -1)
							{
								var strip = pane.TabStripCtrl;
								var visible_index = Maths.Min(DropIndex, strip.TabCount - 1) - strip.FirstVisibleTabIndex;
								var ghost_tab = strip.VisibleTabs.Skip(visible_index).FirstOrDefault();
								if (ghost_tab != null)
								{
									// Include the area of the ghost tab
									var b = strip.Transform.TransformRect(ghost_tab.Bounds);
									var r = pane.RectangleToClient(strip.RectangleToScreen(b));
									region.Union(r);
								}
							}
						}
					}
				}

				// Update the bounds and region of the ghost
				Ghost.Bounds = bounds;
				Ghost.Region = region;
				Ghost.Visible = true;
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

						IndCrossLg.Visible = true;
						IndCrossSm.Visible = false;
					}
					else
					{
						IndCrossSm.Location = pane.PointToScreen(pane.ClientRectangle.Centre() - IndCrossSm.Size.Scaled(0.5f));

						IndCrossLg.Visible = false;
						IndCrossSm.Visible = true;
					}
				}
				else
				{
					if (IndCrossLg.Visible) IndCrossLg.Visible = false;
					if (IndCrossSm.Visible) IndCrossSm.Visible = false;
				}
			}

			/// <summary>The pane in the position that the dropped item should dock to</summary>
			private DockPane HoveredPane
			{
				get { return m_impl_pane; }
				set
				{
					if (m_impl_pane == value) return;
					if (m_impl_pane != null)
					{
						m_impl_pane.TabStripCtrl.GhostTabContent = null;
						m_impl_pane.TabStripCtrl.GhostTabIndex = -1;
					}
					m_impl_pane = value;
					if (m_impl_pane != null)
					{
						m_impl_pane.TabStripCtrl.GhostTabContent = DraggedDockable;
						m_impl_pane.TabStripCtrl.GhostTabIndex = HoveredPaneTabIndex;
					}
				}
			}
			private DockPane m_impl_pane;

			/// <summary>The index within the pane that the dropped item should dock to</summary>
			private int HoveredPaneTabIndex
			{
				get { return m_impl_tab_index; }
				set
				{
					if (m_impl_tab_index == value) return;
					m_impl_tab_index = value;
					if (HoveredPane != null)
						HoveredPane.TabStripCtrl.GhostTabIndex = value;
				}
			}
			private int m_impl_tab_index;

			/// <summary>Base class for forms used as indicator graphics</summary>
			private class GhostBase :Form
			{
				public GhostBase(Form owner)
				{
					SetStyle(ControlStyles.Selectable, false);
					FormBorderStyle = FormBorderStyle.None;
					StartPosition = FormStartPosition.Manual;
					ShowInTaskbar = false;
					Opacity = 0.5f;

					CreateHandle();
					Owner = owner;
				}
				protected override CreateParams CreateParams
				{
					get
					{
						var cp = base.CreateParams;
						cp.ClassStyle |= Win32.CS_DROPSHADOW;
						cp.Style &= ~Win32.WS_VISIBLE;
						cp.ExStyle |= Win32.WS_EX_NOACTIVATE | Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT;
						return cp;
					}
				}
				protected override bool ShowWithoutActivation
				{
					get { return true; }
				}
			}

			/// <summary>A form that acts as a graphic while a pane or content is being dragged</summary>
			private class GhostPane :GhostBase
			{
				public GhostPane(object item, Form owner) :base(owner)
				{
					BackColor = SystemColors.ActiveCaption;
				}
			}

			/// <summary>A form that displays a docking site indicator</summary>
			private class Indicator :GhostBase
			{
				private Hotspot[] m_spots;

				public Indicator(Bitmap bmp, Form owner) :base(owner)
				{
					BackgroundImageLayout = ImageLayout.None;
					BackgroundImage = bmp;
					Size = bmp.Size;

					// Set the hotspot locations
					#region Hot Spots
					if (bmp == DockSiteCrossLg)
					{
						int a = 32, b = 50, c = 77, d = 95, cx = c-b;
						m_spots = new Hotspot[]
						{
							new Hotspot(new Region(new Rectangle(b,b,cx,cx)), EDropSite.PaneCentre),
							new Hotspot(MakeRegion(a,a, b,b, b,c, a,d), EDropSite.PaneLeft),
							new Hotspot(MakeRegion(d,d, c,c, c,b, d,a), EDropSite.PaneRight),
							new Hotspot(MakeRegion(a,a, d,a, c,b, b,b), EDropSite.PaneTop),
							new Hotspot(MakeRegion(d,d, a,d, b,c, c,c), EDropSite.PaneBottom),
							new Hotspot(new Region(new Rectangle(0,a,32,64)), EDropSite.BranchLeft),
							new Hotspot(new Region(new Rectangle(d,a,32,64)), EDropSite.BranchRight),
							new Hotspot(new Region(new Rectangle(a,0,64,32)), EDropSite.BranchTop),
							new Hotspot(new Region(new Rectangle(a,d,64,32)), EDropSite.BranchBottom),
						};
						var region = new Region(Rectangle.Empty);
						region.Union(new Rectangle(33,43,62,42));
						region.Union(new Rectangle(43,33,42,62));
						region.Union(new Rectangle(  0,39,25,50));
						region.Union(new Rectangle(103,39,25,50));
						region.Union(new Rectangle(39,  0,50,25));
						region.Union(new Rectangle(39,103,50,25));
						Region = region;
					}
					else if (bmp == DockSiteCrossSm)
					{
						int a = 0, b = 18, c = 45, d = 63, cx = c-b;
						m_spots = new Hotspot[]
						{
							new Hotspot(new Region(new Rectangle(b,b,cx,cx)), EDropSite.PaneCentre),
							new Hotspot(MakeRegion(a,a, b,b, b,c, a,d), EDropSite.PaneLeft),
							new Hotspot(MakeRegion(d,d, c,c, c,b, d,a), EDropSite.PaneRight),
							new Hotspot(MakeRegion(a,a, d,a, c,b, b,b), EDropSite.PaneTop),
							new Hotspot(MakeRegion(d,d, a,d, b,c, c,c), EDropSite.PaneBottom),
						};
						var region = new Region(Rectangle.Empty);
						region.Union(new Rectangle(1,11,62,42));
						region.Union(new Rectangle(11,1,42,62));
						Region = region;
					}
					else if (bmp == DockSiteLeft)
					{
						m_spots = new Hotspot[] { new Hotspot(new Region(bmp.Size.ToRect()), EDropSite.RootLeft) };
					}
					else if (bmp == DockSiteTop)
					{
						m_spots = new Hotspot[] { new Hotspot(new Region(bmp.Size.ToRect()), EDropSite.RootTop) };
					}
					else if (bmp == DockSiteRight)
					{
						m_spots = new Hotspot[] { new Hotspot(new Region(bmp.Size.ToRect()), EDropSite.RootRight) };
					}
					else if (bmp == DockSiteBottom)
					{
						m_spots = new Hotspot[] { new Hotspot(new Region(bmp.Size.ToRect()), EDropSite.RootBottom) };
					}
					else
					{
						Debug.Assert(false, "Unknown dock site bitmap");
					}
					#endregion
				}
				protected override void OnPaint(PaintEventArgs a)
				{
					base.OnPaint(a);

					// Debug - Draw hot spots - only works when Region is not set on Ghost for some reason
					//m_spots.ForEach(h => a.Graphics.FillRegion(Brushes.Red, h.Area));
				}

				/// <summary>Create a region from a list of points</summary>
				private Region MakeRegion(params int[] pts)
				{
					return GfxExtensions.MakeRegion(pts);
				}

				/// <summary>Test 'pt' against the hotspots on this indicator</summary>
				public EDropSite? CheckSnapTo(Point pt)
				{
					//pt -= Size.Scaled(0.5f);
					return m_spots.FirstOrDefault(x => x.Area.IsVisible(pt))?.DropSite;
				}

				/// <summary>A region within the indicate that corresponds to a dock site location</summary>
				private class Hotspot
				{
					public Hotspot(Region area, EDropSite ds)
					{
						Area = area;
						DropSite = ds;
					}

					/// <summary>The area in the indicator that maps to 'DockSite'</summary>
					public Region Area;

					/// <summary>The drop site that this hot spot corresponds to</summary>
					public EDropSite DropSite;
				}
			}
		}

		/// <summary>A floating window that hosts a tree of dock panes</summary>
		public class FloatingWindow :ToolForm ,ITreeHost
		{
			public FloatingWindow(DockContainer dc) :base(dc)
			{
				ShowInTaskbar = false;
				FormBorderStyle = FormBorderStyle.Sizable;
				HideOnClose = true;
				Text = string.Empty;
				Icon = null;
				Owner = dc.TopLevelControl as Form;

				using (this.SuspendLayout(false))
				{
					DockContainer = dc;
					Root = new Branch(dc, DockSizeData.Quarters);
				}
			}
			protected override void Dispose(bool disposing)
			{
				Root = null;
				DockContainer = null;
				base.Dispose(disposing);
			}

			/// <summary>An identifier for a floating window</summary>
			public int Id { get; set; }

			/// <summary>The dock container that owns this floating window</summary>
			public DockContainer DockContainer
			{
				get { return m_impl_dc; }
				private set
				{
					if (m_impl_dc == value) return;
					if (m_impl_dc != null)
					{
						m_impl_dc.ActiveContentChanged -= HandleActiveContentChanged;
					}
					m_impl_dc = value;
					if (m_impl_dc != null)
					{
						m_impl_dc.ActiveContentChanged += HandleActiveContentChanged;
					}
				}
			}
			private DockContainer m_impl_dc;
			DockContainer ITreeHost.DockContainer { get { return DockContainer; } }

			/// <summary>The root level branch of the tree in this floating window</summary>
			internal Branch Root
			{
				[DebuggerStepThrough] get { return m_impl_root; }
				set
				{
					if (m_impl_root == value) return;
					using (this.SuspendLayout(true))
					{
						if (m_impl_root != null)
						{
							m_impl_root.TreeChanged -= HandleTreeChanged;
							Controls.Remove(m_impl_root);
							Util.Dispose(ref m_impl_root);
						}
						m_impl_root = value;
						if (m_impl_root != null)
						{
							Controls.Add(m_impl_root);
							m_impl_root.Dock = DockStyle.Fill;
							m_impl_root.TreeChanged += HandleTreeChanged;
						}
					}
				}
			}
			private Branch m_impl_root;
			Branch ITreeHost.Root { get { return Root; } }

			/// <summary>
			/// Get/Set the active content on this floating window. This will cause the pane that the content is on to also become active.
			/// To change the active content in a pane without making the pane active, assign to the pane's ActiveContent property</summary>
			public IDockable ActiveContent
			{
				get { return DockContainer.ActiveContent; }
				set { DockContainer.ActiveContent = value; }
			}

			/// <summary>Get/Set the active pane. Note, this pane may be within the dock container, a floating window, or an auto hide window</summary>
			public DockPane ActivePane
			{
				get { return DockContainer.ActivePane; }
				set { DockContainer.ActivePane = value; }
			}

			/// <summary>Add a dockable instance to this branch at the position described by 'location'.</summary>
			public void Add(IDockable dockable, params EDockSite[] location)
			{
				Root.Add(dockable, location);
			}

			/// <summary>Handler for when panes are added/removed from the tree</summary>
			private void HandleTreeChanged(object sender, TreeChangedEventArgs args)
			{
				switch (args.Action)
				{
				case TreeChangedEventArgs.EAction.ActiveContent:
					{
						UpdateUI();
						break;
					}
				case TreeChangedEventArgs.EAction.Added:
					{
						// When the first pane is added to the window, update the title
						Invalidate();
						if (Root.AllContent.CountAtMost(2) == 1)
						{
							ActivePane = Root.AllPanes.First();
							UpdateUI();
						}
						break;
					}
				case TreeChangedEventArgs.EAction.Removed:
					{
						// Whenever content is removed, check to see if the floating
						// window is empty, if so, then hide the floating window.
						Invalidate();
						if (!Root.AllContent.Any())
						{
							Hide();
							UpdateUI();
						}
						break;
					}
				}
			}

			/// <summary>Handler for when the active content changes</summary>
			private void HandleActiveContentChanged(object sender, ActiveContentChangedEventArgs e)
			{
				Invalidate();
			}

			/// <summary>Customise floating window behaviour</summary>
			protected override void WndProc(ref Message m)
			{
				switch (m.Msg)
				{
				default:
					{
						break;
					}
				case Win32.WM_NCLBUTTONDBLCLK:
					{
						// Double clicking the title bar returns the contents to the dock container
						if (DockContainer.Options.DoubleClickTitleBarToDock &&
							Win32.SendMessage(Handle, Win32.WM_NCHITTEST, IntPtr.Zero, m.LParam) == (int)Win32.HitTest.HTCAPTION)
						{
							// Move all content back to the dock container
							using (this.SuspendLayout(true))
							{
								var content = Root.AllPanes.SelectMany(x => x.Content).ToArray();
								foreach (var c in content)
									c.DockControl.IsFloating = false;
							}

							Hide();
							return;
						}
						break;
					}
				}
				base.WndProc(ref m);
			}

			/// <summary>Update the floating window</summary>
			private void UpdateUI()
			{
				var content = ActiveContent;
				if (content != null)
				{
					Text = content.DockControl.TabText;
					var bm = content.DockControl.TabIcon as Bitmap;
					if (bm != null)
					{
						// Can destroy the icon created from the bitmap because
						// the form creates it's own copy of the icon.
						var hicon = bm.GetHicon();
						Icon = Icon.FromHandle(hicon); // Copy assignment
						Win32.DestroyIcon(hicon);
					}
				}
				else
				{
					Text = string.Empty;
					Icon = null;
				}
			}

			/// <summary>Save state to XML</summary>
			public XElement ToXml(XElement node)
			{
				// Save the ID assigned to this window
				node.Add2(XmlTag.Id, Id, false);

				// Save the screen-space location of the floating window
				node.Add2(XmlTag.Bounds, Bounds, false);

				// Save whether the floating window is shown or now
				node.Add2(XmlTag.Visible, Visible, false);

				// Save whether the floating window is pinned to the dock container
				node.Add2(XmlTag.Pinned, PinWindow, false);

				// Save the tree structure of the floating window
				node.Add2(XmlTag.Tree, Root, false);
				return node;
			}

			/// <summary>Apply state to this floating window</summary>
			public void ApplyState(XElement node)
			{
				// Move the floating window to the saved position (clamped by the virtual screen)
				var bounds = node.Element(XmlTag.Bounds)?.As<Rectangle>();
				if (bounds != null)
					Bounds = Util.OnScreen(bounds.Value);

				// Restore visibility
				var visible = node.Element(XmlTag.Visible)?.As<bool>();
				if (visible != null)
					Visible = visible.Value;

				// Restore the pinned state
				var pinned = node.Element(XmlTag.Pinned)?.As<bool>();
				if (pinned != null)
					PinWindow = pinned.Value;

				// Update the tree layout
				var tree_node = node.Element(XmlTag.Tree);
				if (tree_node != null)
					Root.ApplyState(tree_node);
			}
		}

		/// <summary>A panel that displays one pane at a time, and automatically hides to the edge of the screen</summary>
		[DebuggerDisplay("AutoHidePanel {StripLocation}")]
		public class AutoHidePanel :TabStrip ,ITreeHost
		{
			// An auto hide panel looks like a tab strip joined to a panel that can pop out.
			// The strip part of the control is visible when 'StripSize != 0' and other sibling
			// controls should be aligned around it. When popped out, the panel part of the control
			// renders over the other sibling controls.
			// - Each tab is a dockable item
			// - 'Root' only uses the centre dock site
			// - Clicking a tab makes that dockable the active content
			// - The tab strip is hidden in the active pane
			// - Click pin on the pane only pins the active content

			public AutoHidePanel(DockContainer dc, EDockSite ds)
				:base(ds, dc.Options, dc.Options.AutoHideStrip)
			{
				DockContainer = dc;
				PoppedOut = false;
				PoppedOutSize = 200;

				using (this.SuspendLayout(false))
				{
					Root = new Branch(dc, DockSizeData.Quarters);
					Split = new Splitter(ds == EDockSite.Left || ds == EDockSite.Right ? Orientation.Vertical : Orientation.Horizontal);
				}
			}
			protected override void Dispose(bool disposing)
			{
				Root = null;
				Split = null;
				DockContainer = null;
				base.Dispose(disposing);
			}

			/// <summary>The dock container that owns this auto hide window</summary>
			public DockContainer DockContainer
			{
				get { return m_impl_dc; }
				private set
				{
					if (m_impl_dc == value) return;
					if (m_impl_dc != null)
					{
						m_impl_dc.ActiveContentChanged -= HandleActiveContentChanged;
					}
					m_impl_dc = value;
					if (m_impl_dc != null)
					{
						m_impl_dc.ActiveContentChanged += HandleActiveContentChanged;
					}
				}
			}
			private DockContainer m_impl_dc;
			DockContainer ITreeHost.DockContainer
			{
				get { return DockContainer; }
			}

			/// <summary>The root level branch of the tree in this auto hide window</summary>
			internal Branch Root
			{
				[DebuggerStepThrough] get { return m_impl_root; }
				set
				{
					if (m_impl_root == value) return;
					using (this.SuspendLayout(true))
					{
						if (m_impl_root != null)
						{
							m_impl_root.TreeChanged -= HandleTreeChanged;
							Controls.Remove(m_impl_root);
							Util.Dispose(ref m_impl_root);
						}
						m_impl_root = value;
						if (m_impl_root != null)
						{
							Controls.Add(m_impl_root);
							m_impl_root.TreeChanged += HandleTreeChanged;
						}
					}
				}
			}
			private Branch m_impl_root;
			Branch ITreeHost.Root
			{
				get { return Root; }
			}

			/// <summary>A splitter used to resize this child control</summary>
			private Splitter Split
			{
				get { return m_split; }
				set
				{
					if (m_split == value) return;
					using (this.SuspendLayout(true))
					{
						if (m_split != null)
						{
							m_split.DragEnd -= HandleResized;
							Controls.Remove(m_split);
						}
						m_split = value;
						if (m_split != null)
						{
							m_split.DragEnd += HandleResized;
							Controls.Add(m_split);
							Controls.SetChildIndex(m_split, 0);
						}
					}
				}
			}
			private Splitter m_split;

			/// <summary>
			/// Get/Set the active content on this floating window. This will cause the pane that the content is on to also become active.
			/// To change the active content in a pane without making the pane active, assign to the pane's ActiveContent property</summary>
			public IDockable ActiveContent
			{
				get { return DockContainer.ActiveContent; }
				set { DockContainer.ActiveContent = value; }
			}

			/// <summary>Get/Set the active pane. Note, this pane may be within the dock container, a floating window, or an auto hide window</summary>
			public DockPane ActivePane
			{
				get { return DockContainer.ActivePane; }
				set { DockContainer.ActivePane = value; }
			}

			/// <summary>Get the content that we're showing tabs for</summary>
			public override IEnumerable<IDockable> Content
			{
				get { return Root.AllContent; }
			}

			/// <summary>Get/Set the popped out state of the auto hide panel</summary>
			public bool PoppedOut
			{
				get { return m_impl_popped_out; }
				set
				{
					if (m_impl_popped_out == value) return;
					m_impl_popped_out = value;
					Parent?.PerformLayout();
				}
			}
			private bool m_impl_popped_out;

			/// <summary>The size of the panel part of the auto hide panel when popped out</summary>
			public int PoppedOutSize
			{
				get { return m_impl_popped_out_size; }
				set
				{
					if (m_impl_popped_out_size == value) return;
					m_impl_popped_out_size = value;
					if (PoppedOut)
						Parent?.PerformLayout();
				}
			}
			private int m_impl_popped_out_size;

			/// <summary>Add a dockable instance to this branch at the position described by 'location'.</summary>
			public void Add(IDockable dockable, params EDockSite[] location)
			{
				Root.Add(dockable, location);
			}

			/// <summary>The size of the tab strip part of this auto hide panel</summary>
			public override int StripSize
			{
				get { return Root.AllContent.Any() ? base.StripSize : 0; }
				set { base.StripSize = value; }
			}

			/// <summary>Remove area due to the other auto hide strips</summary>
			private Rectangle TrimArea(Rectangle rect)
			{
				var r = new RectangleRef(rect);
				foreach (var p in DockContainer.AutoHidePanels.Where(x => x != this))
				{
					switch (p.StripLocation) {
					case EDockSite.Left:   r.Left   += p.StripSize; break;
					case EDockSite.Right:  r.Right  -= p.StripSize; break;
					case EDockSite.Top:    r.Top    += p.StripSize; break;
					case EDockSite.Bottom: r.Bottom -= p.StripSize; break;
					}
				}
				return r;
			}

			/// <summary>
			/// Returns the bounds of this auto hide control given the available display area.
			/// Note however, that the parent control should align other controls assuming the size of this
			/// control is 'StripHeight', the panel part of the control draws over other controls.</summary>
			public override Rectangle CalcBounds(Rectangle display_area)
			{
				var size = StripSize + (PoppedOut ? PoppedOutSize : 0);
				if (size == 0)
					return Rectangle.Empty;

				// Remove area due to the other auto hide strips
				var r = new RectangleRef(TrimArea(display_area));
				switch (StripLocation) {
				case EDockSite.Left:   r.Right  = r.Left   + size; break;
				case EDockSite.Right:  r.Left   = r.Right  - size; break;
				case EDockSite.Top:    r.Bottom = r.Top    + size; break;
				case EDockSite.Bottom: r.Top    = r.Bottom - size; break;
				}

				return r;
			}

			/// <summary>Layout the panel</summary>
			protected override void OnLayout(LayoutEventArgs levent)
			{
				// Remember the Splitter is parented to this AutoHidePanel even
				// thought it's Area is set to the area of the DockContainer.
				var rect = DisplayRectangle;
				if (rect.Area() > 0 && Root != null)
				{
					using (this.SuspendLayout(false))
					{
						// Calculate the area available for the main tree in the dock container.
						// Can't use 'DockContainer.Root.Bounds' yet because it isn't set until
						// after the auto hide panels have been laid out.
						var prect = new RectangleRef(TrimArea(DockContainer.DisplayRectangle));

						// Set the area that the splitter can move within and the size of the displayed Pane
						Root.Visible = Split.Visible = PoppedOut;
						Root.Child[EDockSite.Centre].DockPane.TabStripCtrl.Visible = false;
						if (PoppedOut)
						{
							// Note: the area is the area of the dock container, but in Parent space (where Parent is this auto hide panel)
							switch (StripLocation) {
							case EDockSite.Left:
								{
									var dc_area = Rectangle.FromLTRB(prect.Left + StripSize, prect.Top, prect.Right, prect.Bottom);
									Split.Area = RectangleToClient(DockContainer.RectangleToScreen(dc_area));
									Split.Position = rect.Width - StripSize - Split.BarWidth/2;
									Root.Bounds = Split.BoundsLT;
									break;
								}
							case EDockSite.Right:
								{
									var dc_area = Rectangle.FromLTRB(prect.Left, prect.Top, prect.Right - StripSize, prect.Bottom);
									Split.Area = RectangleToClient(DockContainer.RectangleToScreen(dc_area));
									Split.Position = Split.Area.Width - (rect.Width - StripSize - Split.BarWidth/2);
									Root.Bounds = Split.BoundsRB;
									break;
								}
							case EDockSite.Top:
								{
									var dc_area = Rectangle.FromLTRB(prect.Left, prect.Top + StripSize, prect.Right, prect.Bottom);
									Split.Area = RectangleToClient(DockContainer.RectangleToScreen(dc_area));
									Split.Position = rect.Height - StripSize - Split.BarWidth/2;
									Root.Bounds = Split.BoundsLT;
									break;
								}
							case EDockSite.Bottom:
								{
									var dc_area = Rectangle.FromLTRB(prect.Left, prect.Top, prect.Right, prect.Bottom - StripSize);
									Split.Area = RectangleToClient(DockContainer.RectangleToScreen(dc_area));
									Split.Position = Split.Area.Height - (rect.Height - StripSize - Split.BarWidth/2);
									Root.Bounds = Split.BoundsRB;
									break;
								}
							}
						}
					}
				}
				base.OnLayout(levent);
			}

			/// <summary>Draw the tab strip</summary>
			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);
			}

			/// <summary>Handle mouse clicks on a tab</summary>
			protected override void OnTabClick(TabClickEventArgs e)
			{
				if (ActiveContent != e.Content)
				{
					ActiveContent = e.Content;
					PoppedOut = true;
				}
				else
				{
					PoppedOut = !PoppedOut;
				}
			}

			/// <summary>Handle double click on a tab</summary>
			protected override void OnTabDblClick(TabClickEventArgs e)
			{
				// Do nothing for double clicks on auto hide panel tabs
			}

			/// <summary>Set the size of 'Root' as 'Split' is moved</summary>
			private void HandleResized(object sender, EventArgs e)
			{
				switch (StripLocation) {
				case EDockSite.Left:   PoppedOutSize = Math.Max(0, Split.BoundsLT.Width + Split.BarWidth); break;
				case EDockSite.Right:  PoppedOutSize = Math.Max(0, Split.BoundsRB.Width + Split.BarWidth); break;
				case EDockSite.Top:    PoppedOutSize = Math.Max(0, Split.BoundsLT.Height + Split.BarWidth); break;
				case EDockSite.Bottom: PoppedOutSize = Math.Max(0, Split.BoundsRB.Height + Split.BarWidth); break;
				}
			}

			/// <summary>Handler for when panes are added/removed from the tree</summary>
			private void HandleTreeChanged(object sender, TreeChangedEventArgs args)
			{
				switch (args.Action)
				{
				case TreeChangedEventArgs.EAction.ActiveContent:
					{
						// Quirk: when the first content is added to the auto hide panel, the 'ActiveContent' event
						// occurs before the 'Added' event. This is because the binding list, that the dockable is added
						// to, updates its current position before raising the ItemAdded event.
						Invalidate();
						break;
					}
				case TreeChangedEventArgs.EAction.Added:
					{
						// When the first pane is added to the window, update the layout of the dock container
						// because the tab strip for this auto hide panel is now visible
						Invalidate();
						if (Root.AllContent.CountAtMost(2) == 1)
						{
							ActivePane = Root.AllPanes.First();
							DockContainer.PerformLayout();
						}

						// Change the behaviour of all dock panes in the auto hide panel to only operate on the visible content
						if (args.DockPane != null)
							args.DockPane.ApplyToVisibleContentOnly = true;

						break;
					}
				case TreeChangedEventArgs.EAction.Removed:
					{
						// When the last content is removed, update the layout of the dock container
						// because the tab strip for this auto hide panel is now hidden
						Invalidate();
						if (!Root.AllContent.Any())
						{
							PoppedOut = false;
							DockContainer.PerformLayout();
						}
						break;
					}
				}
			}

			/// <summary>Handler for when the active content changes</summary>
			private void HandleActiveContentChanged(object sender, ActiveContentChangedEventArgs e)
			{
				// Auto hide the auto hide panel whenever content that isn't in our tree becomes active
				if (e.ContentNew == null || e.ContentNew.DockControl.DockPane.RootBranch != Root)
					PoppedOut = false;

				Invalidate();
			}
		}

		/// <summary>Implementation of active panes and active content for dock containers, floating windows, and auto hide windows</summary>
		internal class ActiveContentImpl
		{
			public ActiveContentImpl(ITreeHost tree_host)
			{
				TreeHost = tree_host;
			}

			/// <summary>
			/// Get/Set the active content. This will cause the pane that the content is on to also become active.
			/// To change the active content in a pane without making the pane active, assign to the pane's ActiveContent property</summary>
			public IDockable ActiveContent
			{
				get { return ActivePane?.VisibleContent; }
				set
				{
					if (ActiveContent == value) return;

					// Ensure 'value' is the active content on its pane
					if (value != null)
						value.DockControl.DockPane.VisibleContent = value;

					// Set value's pane as the active one.
					// If value's pane was the active one before, then setting 'value' as the active content
					// on the pane will also have caused an OnActiveContentChanged to be called. If not, then
					// changing the active pane here will result in OnActiveContentChanged being called.
					ActivePane = value?.DockControl.DockPane;
				}
			}

			/// <summary>Get/Set the active pane. Note, this pane may be within the dock container, a floating window, or an auto hide window</summary>
			public DockPane ActivePane
			{
				get { return m_impl_active_pane; }
				set
				{
					if (m_impl_active_pane == value) return;
					var old_pane = m_impl_active_pane;
					var old_content = ActiveContent;

					// Change the pane
					if (m_impl_active_pane != null)
					{
						m_impl_active_pane.VisibleContentChanged -= HandleActiveContentChanged;
					}
					m_impl_active_pane = value;
					if (m_impl_active_pane != null)
					{
						m_impl_active_pane.VisibleContentChanged += HandleActiveContentChanged;
					}

					// Notify observers of each pane about activation changed
					old_pane?.OnActivatedChanged();
					value?.OnActivatedChanged();

					// Notify that the active pane has changed and therefore the active content too
					OnActivePaneChanged(new ActivePaneChangedEventArgs(old_pane, value));
					OnActiveContentChanged(new ActiveContentChangedEventArgs(old_content, ActiveContent));
				}
			}
			private DockPane m_impl_active_pane;

			/// <summary>Raised whenever the active pane changes in the dock container</summary>
			public event EventHandler<ActivePaneChangedEventArgs> ActivePaneChanged;
			public void OnActivePaneChanged(ActivePaneChangedEventArgs args)
			{
				ActivePaneChanged.Raise(TreeHost, args);
			}

			/// <summary>Raised whenever the active content for the dock container changes</summary>
			public event EventHandler<ActiveContentChangedEventArgs> ActiveContentChanged;
			public void OnActiveContentChanged(ActiveContentChangedEventArgs args)
			{
				ActiveContentChanged.Raise(TreeHost, args);
			}

			/// <summary>The root of the tree in this dock container</summary>
			private ITreeHost TreeHost { [DebuggerStepThrough] get; set; }

			/// <summary>Watch for the content in the active pane changing</summary>
			private void HandleActiveContentChanged(object sender, ActiveContentChangedEventArgs e)
			{
				OnActiveContentChanged(e);
			}
		}

		/// <summary>Args for when panes or branches are added to a tree</summary>
		internal class TreeChangedEventArgs :EventArgs
		{
			public enum EAction
			{
				/// <summary>The non-null Dockable, DockPane, or Branch was added to the tree</summary>
				Added,

				/// <summary>The non-null Dockable, DockPane, or Branch was removed from the tree</summary>
				Removed,

				/// <summary>The Dockable that has became active within DockPane in the tree</summary>
				ActiveContent,
			}

			public TreeChangedEventArgs(EAction action, IDockable dockable = null, DockPane pane = null, Branch branch = null)
			{
				Action = action;
				Dockable = dockable;
				DockPane = pane;
				Branch = branch;
			}

			/// <summary>The type of change that occurred</summary>
			public EAction Action { get; private set; }

			/// <summary>Non-null if it was a dockable that was added or removed</summary>
			public IDockable Dockable { get; private set; }

			/// <summary>Non-null if it was a dock pane that was added or removed</summary>
			public DockPane DockPane { get; private set; }

			/// <summary>Non-null if it was a branch that was added or removed</summary>
			public Branch Branch { get; private set; }
		}

		/// <summary>Tags used in persisting layout to XML</summary>
		internal static class XmlTag
		{
			public const string DockContainerLayout = "DockContainerLayout";
			public const string Version = "version";
			public const string Name = "name";
			public const string Id = "id";
			public const string DockPane = "pane";
			public const string Contents = "contents";
			public const string Tree = "tree";
			public const string Pane = "pane";
			public const string FloatingWindows = "floating_windows";
			public const string Options = "options";
			public const string Content = "content";
			public const string Host = "host";
			public const string Location = "location";
			public const string Address = "address";
			public const string FloatingWindow = "floating_window";
			public const string Bounds = "bounds";
			public const string Pinned = "pinned";
			public const string DockSizes = "dock_sizes";
			public const string Visible = "visible";
			public const string StripLocation = "strip_location";
			public const string Active = "active";
		}

		#endregion
	}

	/// <summary>Provides the implementation of the docking functionality</summary>
	public class DockControl :DockContainer.DockControl
	{
		/// <summary>Create the docking functionality helper.</summary>
		/// <param name="owner">The control that docking is being provided for</param>
		/// <param name="persist_name">The name to use for this instance when saving the layout to XML</param>
		public DockControl(Control owner, string persist_name, EDockSite default_dock_site = EDockSite.Centre)
			:base(owner, persist_name, default_dock_site)
		{}
	}

	#region Event Args

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

	#endregion
}
