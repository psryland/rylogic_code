using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Microsoft.Win32;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;
using Geometry = System.Windows.Media.Geometry;

namespace Rylogic.Gui2
{
	using DockContainerDetail;

	/// <summary>Locations in the dock container where dock panes or trees of dock panes, can be docked.</summary>
	public enum EDockSite
	{
		// Note: Order here is important.
		// These values are also used as indices into arrays.

		/// <summary>Docked to the main central area of the window</summary>
		Centre = 0,

		/// <summary>Docked to the left side</summary>
		Left = 1,

		/// <summary>Docked to the right side</summary>
		Right = 2,

		/// <summary>Docked to the top</summary>
		Top = 3,

		/// <summary>Docked to the bottom</summary>
		Bottom = 4,

		/// <summary>Not docked</summary>
		None = 5,
	}

	/// <summary>A control that can be docked within a DockContainer</summary>
	public interface IDockable
	{
		/// <summary>The docking implementation object that provides the docking functionality</summary>
		DockControl DockControl { get; }
	}

	/// <summary>Interface for classes that have a Root branch and active content</summary>
	internal interface ITreeHost
	{
		/// <summary>The dock container that owns this instance</summary>
		DockContainer DockContainer { get; }

		/// <summary>The root of the tree of dock panes</summary>
		Branch Root { get; }

		/// <summary>The dockable that is active on the container</summary>
		DockControl ActiveContent { get; set; }

		/// <summary>The pane that contains the active content on the container</summary>
		DockPane ActivePane { get; set; }

		/// <summary>Add a dockable instance to this branch at the position described by 'location'. 'index' is the index within the destination dock pane</summary>
		DockPane Add(IDockable dockable, int index, params EDockSite[] location);
	}

	/// <summary>A dock container is the parent control that manages docking of controls that implement IDockable.</summary>
	public class DockContainer : DockPanel, ITreeHost, IDisposable
	{
		/// <summary>The number of valid dock sites</summary>
		private const int DockSiteCount = 5;

		/// <summary>Create a new dock container</summary>
		public DockContainer()
		{
			Options = new OptionData();
			m_all_content = new HashSet<DockControl>();
			m_active_content = new ActiveContentImpl(this);

			// The auto hide tab strips dock around the edge of the main dock container
			// but remain hidden unless there are auto hide panels with content.
			// The rest of the docking area is within 'centre' which contains the 
			// root branch (filling the canvas) and four AutoHidePanels.

			// Add a Canvas for the centre area and add the root branch (filling the canvas)
			var centre = new Canvas { Name = "DockContainerCentre" };

			// Create the auto hide panels
			AutoHidePanels = new AutoHidePanelCollection(this, centre);

			// Create a collection for floating windows
			FloatingWindows = new FloatingWindowCollection(this);

			// Add the centre content
			Children.Add2(centre);
			Root = centre.Children.Add2(new Branch(this, DockSizeData.Quarters));
			Root.SetBinding(Branch.WidthProperty, new Binding(nameof(Canvas.ActualWidth)) { Source = centre });
			Root.SetBinding(Branch.HeightProperty, new Binding(nameof(Canvas.ActualHeight)) { Source = centre });
			Root.PruneBranches(); // Ensure the default centre pane exists

			// Create Commands
			CmdLoadLayout = new LoadLayoutCommand(this);
			CmdSaveLayout = new SaveLayoutCommand(this);
			CmdResetLayout = new ResetLayoutCommand(this);
		}
		public virtual void Dispose()
		{
			Root = null;
			m_all_content.Clear();
			Util.Dispose(FloatingWindows);
			Util.Dispose(AutoHidePanels);
		}

		/// <summary>The dock container that owns this instance</summary>
		DockContainer ITreeHost.DockContainer
		{
			get { return this; }
		}

		/// <summary>Options for the dock container</summary>
		public OptionData Options { get; private set; }

		/// <summary>Get/Set the globally active content (i.e. owned by this dock container). This will cause the pane that the content is on to also become active.</summary>
		[Browsable(false)]
		public IDockable ActiveDockable
		{
			get { return ActiveContent?.Dockable; }
			set { ActiveContent = value?.DockControl; } // Note: this setter isn't called when the user clicks on some content, ActivePane.set is called.
		}

		/// <summary>Get/Set the globally active content (across the main control, all floating windows, and all auto hide panels)</summary>
		[Browsable(false)]
		public DockControl ActiveContent
		{
			get { return m_active_content.ActiveContent; }
			set { m_active_content.ActiveContent = value; }
		}
		private ActiveContentImpl m_active_content;

		/// <summary>Get/Set the globally active pane (i.e. owned by this dock container). Note, this pane may be within the dock container, a floating window, or an auto hide window</summary>
		[Browsable(false)]
		public DockPane ActivePane
		{
			get { return m_active_content.ActivePane; }
			set { m_active_content.ActivePane = value; }
		}

		/// <summary>Activate the content/pane that was previously active</summary>
		public void ActivatePrevious()
		{
			m_active_content.ActivatePrevious();
		}

		/// <summary>Return all tree hosts associated with this dock container</summary>
		internal IEnumerable<ITreeHost> AllTreeHosts
		{
			get
			{
				yield return this;
				foreach (var ahp in AutoHidePanels) yield return ahp;
				foreach (var fw in FloatingWindows) yield return fw;
			}
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
			get { return AllContentInternal.Select(x => x.Dockable); }
		}
		internal IEnumerable<DockControl> AllContentInternal
		{
			get { return m_all_content; }
		}
		private HashSet<DockControl> m_all_content;

		/// <summary>Return the dock pane at the given dock site or null if the site does not contain a dock pane</summary>
		public DockPane GetPane(params EDockSite[] location)
		{
			return Root.DescendantAt(location)?.Item as DockPane;
		}

		/// <summary>
		/// Returns a reference to the dock sizes at a given level in the tree.
		/// 'location' should point to a branch otherwise null is returned.
		/// location.Length == 0 returns the root level sizes</summary>
		public DockSizeData GetDockSizes(params EDockSite[] location)
		{
			return Root.GetDockSizes(location);
		}

		/// <summary>Save a reference to 'dockable' in this dock container</summary>
		internal void Remember(DockControl dc)
		{
			m_all_content.Add(dc);
		}

		/// <summary>Release the reference to 'dockable' in this dock container</summary>
		internal void Forget(DockControl dc)
		{
			m_all_content.Remove(dc);
		}

		/// <summary>The root of the tree in this dock container</summary>
		internal Branch Root
		{
			[DebuggerStepThrough]
			get { return m_root; }
			set
			{
				if (m_root == value) return;
				if (m_root != null)
				{
					m_root.TreeChanged -= HandleTreeChanged;
					Util.Dispose(ref m_root);
				}
				m_root = value;
				if (m_root != null)
				{
					m_root.TreeChanged += HandleTreeChanged;
				}

				/// <summary>Handler for when panes are added/removed from the tree</summary>
				void HandleTreeChanged(object sender, TreeChangedEventArgs obj)
				{
					switch (obj.Action)
					{
					case TreeChangedEventArgs.EAction.ActiveContent:
						{
							if (obj.DockPane == ActivePane)
								ActiveContent = obj.DockControl;
							break;
						}
					case TreeChangedEventArgs.EAction.Added:
					case TreeChangedEventArgs.EAction.Removed:
						{
							if (obj.DockControl != null)
								OnDockableMoved(new DockableMovedEventArgs((DockableMovedEventArgs.EAction)obj.Action, obj.DockControl.Dockable));
							break;
						}
					}
				}
			}
		}
		Branch ITreeHost.Root
		{
			get { return Root; }
		}
		private Branch m_root;

		/// <summary>Add a dockable instance to this branch at the position described by 'location'.</summary>
		internal DockPane Add(DockControl dc, int index, params EDockSite[] location)
		{
			dc = dc ?? throw new ArgumentNullException(nameof(dc), "'dockable' or 'dockable.DockControl' cannot be 'null'");

			// If no location is given, add the dockable to its default location
			if (location.Length == 0)
			{
				return Add(dc, dc.DefaultDockLocation);
			}
			else
			{
				return Root.Add(dc, index, location);
			}
		}
		public DockPane Add(IDockable dockable, int index, params EDockSite[] location)
		{
			return Add(dockable?.DockControl, int.MaxValue, location);
		}
		public DockPane Add(IDockable dockable, params EDockSite[] location)
		{
			return Add(dockable, int.MaxValue, location);
		}
		public TDockable Add2<TDockable>(TDockable dockable, int index, params EDockSite[] location) where TDockable : IDockable
		{
			Add(dockable, index, location);
			return dockable;
		}
		public TDockable Add2<TDockable>(TDockable dockable, params EDockSite[] location) where TDockable : IDockable
		{
			return Add2(dockable, int.MaxValue, location);
		}

		/// <summary>Add the dockable to the dock container, auto hide panel, or float window, as described by 'loc'</summary>
		internal DockPane Add(DockControl dc, DockLocation loc)
		{
			dc = dc ?? throw new ArgumentNullException(nameof(dc), "'dockable' or 'dockable.DockControl' cannot be 'null'");
			loc = loc ?? dc.DefaultDockLocation;

			var pane = (DockPane)null;
			if (loc.FloatingWindowId != null)
			{
				// If a floating window id is given, locate the floating window and add to that
				var id = loc.FloatingWindowId.Value;
				var fw = FloatingWindows.GetOrAdd(id);
				pane = fw.Add(dc, loc.Index, loc.Address);
				fw.Show();
			}
			else if (loc.AutoHide != null)
			{
				// If an auto hide site is given, add to the auto hide panel
				var side = loc.AutoHide.Value;
				pane = AutoHidePanels[side].Root.Add(dc, loc.Index, loc.Address);
				dc.DefaultDockLocation.AutoHide = loc.AutoHide;
			}
			else
			{
				// Otherwise, dock to the dock container
				pane = Root.Add(dc, loc.Index, loc.Address);
				dc.DefaultDockLocation.AutoHide = null;
			}
			return pane;
		}
		public DockPane Add(IDockable dockable, DockLocation loc)
		{
			return Add(dockable?.DockControl, loc);
		}
		public TDockable Add2<TDockable>(TDockable dockable, DockLocation loc) where TDockable : IDockable
		{
			Add(dockable, loc);
			return dockable;
		}

		/// <summary>Remove a dockable from this dock container</summary>
		public void Remove(DockControl dc)
		{
			// Idempotent remove
			if (dc?.DockContainer == null)
				return;

			// Throw if 'dc' is not in this container
			if (dc.DockContainer != this)
				throw new InvalidOperationException("The given dockable is not managed by this dock container");

			// The DockControl object can actually remove itself, but this method gives extra checking
			dc.Remove();
		}
		public void Remove(IDockable dockable)
		{
			Remove(dockable?.DockControl);
		}

		/// <summary>Raised whenever the active pane changes in this dock container or associated floating window or auto hide panel</summary>
		public event EventHandler<ActivePaneChangedEventArgs> ActivePaneChanged
		{
			add { m_active_content.ActivePaneChanged += value; }
			remove { m_active_content.ActivePaneChanged -= value; }
		}

		/// <summary>Raised whenever the active content changes in this dock container or associated floating window or auto hide panel</summary>
		public event EventHandler<ActiveContentChangedEventArgs> ActiveContentChanged
		{
			add { m_active_content.ActiveContentChanged += value; }
			remove { m_active_content.ActiveContentChanged -= value; }
		}

		/// <summary>Raised whenever content is moved within the dock container, floating windows, or auto hide panels</summary>
		public event EventHandler<DockableMovedEventArgs> DockableMoved;
		protected virtual void OnDockableMoved(DockableMovedEventArgs args)
		{
			DockableMoved?.Invoke(this, args);
		}

		/// <summary>Initiate dragging of a pane or content</summary>
		internal static void DragBegin(object draggee, Point ss_start_pt)
		{
			var dc = (DockContainer)null;
			if      (draggee is DockPane p) dc = p.DockContainer;
			else if (draggee is DockControl c) dc = c.DockContainer;
			else throw new Exception("Dragging only supports dock pane or dock control");

			// Create a form for displaying the dock site locations and handling the drop of a pane or content
			using (var drop_handler = new DragHandler(dc, draggee, ss_start_pt))
				drop_handler.ShowDialog();
		}

		/// <summary>The floating windows associated with this dock container</summary>
		public FloatingWindowCollection FloatingWindows { get; private set; }
		public class FloatingWindowCollection :IEnumerable<FloatingWindow>, IDisposable
		{
			private readonly DockContainer m_dc;
			private ObservableCollection<FloatingWindow> m_floaters;
			public FloatingWindowCollection(DockContainer dc)
			{
				m_dc = dc;
				m_floaters = new ObservableCollection<FloatingWindow>();
			}
			public void Dispose()
			{
				Util.DisposeAll(m_floaters);
			}

			/// <summary>Return the floating window with Id = 'id'</summary>
			public FloatingWindow Get(int id)
			{
				return m_floaters.FirstOrDefault(x => x.Id == id);
			}

			/// <summary>Returns an existing floating window with id 'id', or a new floating window with the Id set to 'id'</summary>
			public FloatingWindow GetOrAdd(int id)
			{
				var fw = GetOrAdd(x => x.Id == id);
				fw.Id = id;
				return fw;
			}

			/// <summary>Returns an existing floating window that satisfies 'pred', or a new floating window</summary>
			public FloatingWindow GetOrAdd(Func<FloatingWindow, bool> pred)
			{
				foreach (var fw in m_floaters)
				{
					if (!pred(fw)) continue;
					return fw;
				}
				return m_floaters.Add2(new FloatingWindow(m_dc));
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

			/// <summary></summary>
			public IEnumerator<FloatingWindow> GetEnumerator()
			{
				return m_floaters.GetEnumerator();
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		/// <summary>Panels that auto hide when their contained tree does not contain the active pane</summary>
		public AutoHidePanelCollection AutoHidePanels { get; private set; }
		public class AutoHidePanelCollection :IEnumerable<AutoHidePanel>, IDisposable
		{
			private AutoHidePanel[] m_auto_hide;
			public AutoHidePanelCollection(DockContainer dc, Canvas centre)
			{
				m_auto_hide = new AutoHidePanel[DockSiteCount];
				for (var i = (int)EDockSite.Left; i != m_auto_hide.Length; ++i)
				{
					var ds = (EDockSite)i;
					var ah = new AutoHidePanel(dc, ds);

					// Add the auto hide panel to the centre canvas
					m_auto_hide[i] = centre.Children.Add2(ah);

					// Add the associated tab strip to the top-level dock panel
					dc.Children.Add2(ah.TabStrip);
					DockContainer.SetDock(ah.TabStrip, DockContainer.ToDock(ds));

					// Position the auto hide panels within 'centre'
					ah.SetBinding(AutoHidePanel.WidthProperty, new Binding(nameof(Canvas.ActualWidth)) { Source = centre });
					ah.SetBinding(AutoHidePanel.HeightProperty, new Binding(nameof(Canvas.ActualHeight)) { Source = centre });
				}
				Canvas.SetLeft(m_auto_hide[(int)EDockSite.Left], 0);
				Canvas.SetTop (m_auto_hide[(int)EDockSite.Left], 0);
				Canvas.SetLeft(m_auto_hide[(int)EDockSite.Top], 0);
				Canvas.SetTop (m_auto_hide[(int)EDockSite.Top], 0);
				Canvas.SetRight(m_auto_hide[(int)EDockSite.Right], 0);
				Canvas.SetTop  (m_auto_hide[(int)EDockSite.Right], 0);
				Canvas.SetLeft  (m_auto_hide[(int)EDockSite.Bottom], 0);
				Canvas.SetBottom(m_auto_hide[(int)EDockSite.Bottom], 0);
			}
			public void Dispose()
			{
				Util.DisposeAll(m_auto_hide);
			}

			/// <summary>Get the auto hide panel by dock site</summary>
			public AutoHidePanel this[EDockSite ds]
			{
				get
				{
					if (ds >= EDockSite.Left && ds < EDockSite.None)
						return m_auto_hide[(int)ds];
					else
						throw new Exception($"No auto hide panel for dock site {ds}");
				}
			}

			/// <summary>Enumerable auto hide panels</summary>
			/// <returns></returns>
			public IEnumerator<AutoHidePanel> GetEnumerator()
			{
				return m_auto_hide.Skip(1).GetEnumerator();
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		/// <summary>A command to load a UI layout</summary>
		public ICommand CmdLoadLayout { get; private set; }

		/// <summary>A command to save the UI layout</summary>
		public ICommand CmdSaveLayout { get; private set; }

		/// <summary>A command to reset the UI layout</summary>
		public ICommand CmdResetLayout { get; private set; }

		/// <summary>Creates an instance of a menu with options to select specific content</summary>
		public MenuItem WindowsMenu(string menu_name = "Windows")
		{
			var menu = new MenuItem { Header = menu_name };
			var sep = new Separator();

			// Load layout from disk
			var load = new MenuItem { Header = "Load Layout", Command = CmdLoadLayout };

			// Save the layout to disk
			var save = new MenuItem { Header = "Save Layout", Command = CmdSaveLayout };

			// Reset to default layout
			var reset = new MenuItem { Header = "Reset Layout", Command = CmdResetLayout };

			// Repopulate the content items on drop down
			menu.SubmenuOpened += RepopulateMenu;
			void RepopulateMenu(object s, EventArgs a)
			{
				menu.Items.Clear();

				// Add a menu item for each content
				foreach (var content in AllContentInternal.OrderBy(x => x.TabText))
				{
					var opt = menu.Items.Add2(new MenuItem
					{
						Header = content.TabText,
						Icon = content.TabIcon,
						IsChecked = content == ActiveContent,
					});
					opt.Click += (ss, aa) =>
					{
						FindAndShow(content);
					};

					// If the content has a tab context menu, show the context menu on right click on the menu option
					if (content.TabCMenu != null)
					{
						opt.MouseDown += (ss, aa) =>
						{
							if (aa.RightButton == MouseButtonState.Pressed)
								content.TabCMenu.IsOpen = true;
							//Show(opt.PointToScreen(aa.Location));
						};
					}
				}

				// Add a separator, then the layout items
				menu.Items.Add(sep);
				menu.Items.Add(load);
				menu.Items.Add(save);
				menu.Items.Add(reset);
			}
			return menu;
		}

		/// <summary>Make 'content' visible to the user, by making it's containing window visible, on-screen, popped-out, etc</summary>
		public void FindAndShow(IDockable content)
		{
			FindAndShow(content.DockControl);
		}
		public void FindAndShow(DockControl dc)
		{
			if (dc.DockContainer != this)
				throw new Exception($"Cannot show content '{dc.TabText}', it is not managed by this DockContainer");

			// Find the window that contains 'content'
			var pane = dc.DockPane;
			if (pane == null)
			{
				// Restore 'content' to the dock container
				var address = dc.DockAddressFor(this);
				Add(dc, int.MaxValue, address);
				pane = dc.DockPane;
				if (pane == null)
					throw new Exception($"Cannot show content '{dc.TabText}'. Could not add it to a visible dock pane");
			}

			// Make the selected item the visible item on the pane
			pane.VisibleContent = dc;

			// Make it the active item in the dock container
			var container = pane.TreeHost;
			if (container != null)
			{
				container.ActiveContent = dc;

				// If the container is an auto hide panel, make sure it's popped out
				if (container is AutoHidePanel ah)
				{
					ah.PoppedOut = true;
				}

				// If the container is a floating window, make sure it's visible and on-screen
				if (container is FloatingWindow fw)
				{
					fw.Bounds = Gui_.OnScreen(fw.Bounds);
					fw.Visibility = Visibility.Collapsed;
//					fw.BringToFront();
					if (fw.WindowState == WindowState.Minimized)
						fw.WindowState = WindowState.Normal;
				}
			}
		}

		/// <summary>Get the bounds of a dock site. 'rect' is the available area. 'docked_mask' are the </summary>
		internal static Rect DockSiteBounds(EDockSite location, Rect rect, EDockMask docked_mask, DockSizeData dock_site_sizes)
		{
			var area = dock_site_sizes.GetSizesForRect(rect, docked_mask);

			// Get the initial rectangle for the dock site assuming no other docked children
			var r = Rect.Empty;
			switch (location)
			{
			default: throw new Exception($"No bounds value for dock zone {location}");
			case EDockSite.Centre: r = rect; break;
			case EDockSite.Left:   r = new Rect(rect.Left, rect.Top, area.Left, rect.Height); break;
			case EDockSite.Right:  r = new Rect(rect.Right - area.Right, rect.Top, area.Right, rect.Height); break;
			case EDockSite.Top:    r = new Rect(rect.Left, rect.Top, rect.Width, area.Top); break;
			case EDockSite.Bottom: r = new Rect(rect.Left, rect.Bottom - area.Bottom, rect.Width, area.Bottom); break;
			}

			// Remove areas from 'r' for sites with EDockSite values greater than 'location' (if present).
			// Note: order of subtracting areas is important, subtract from highest priority to lowest
			// so that the result is always still a rectangle.
			for (var i = (EDockSite)DockSiteCount - 1; i > location; --i)
			{
				if (!docked_mask.HasFlag((EDockMask)(1 << (int)i))) continue;
				var sub = DockSiteBounds(i, rect, docked_mask, dock_site_sizes);
				r = Gui_.Subtract(r, sub);
				if (r.Width < 0) r.Width = 0;
				if (r.Height < 0) r.Height = 0;
			}

			// Return the bounds of the dock zone for 'location'
			return r;
		}

		/// <summary>Convert a dock site to a dock panel dock location</summary>
		internal static Dock ToDock(EDockSite ds)
		{
			switch (ds)
			{
			default: throw new Exception($"Cannot convert {ds} to a DockPanel 'Dock' value");
			case EDockSite.Left: return Dock.Left;
			case EDockSite.Right: return Dock.Right;
			case EDockSite.Top: return Dock.Top;
			case EDockSite.Bottom: return Dock.Bottom;
			}
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
			foreach (var content in AllContentInternal)
				content_node.Add2(XmlTag.Content, content, false);

			// Record the name of the active content
			if (ActiveContent != null)
				node.Add2(XmlTag.Active, ActiveContent.PersistName, false);

			// Record the structure of the main tree
			node.Add2(XmlTag.Tree, Root, false);

			// Record position information for the floating windows
			var fw_node = node.Add2(new XElement(XmlTag.FloatingWindows));
			foreach (var fw in FloatingWindows.Where(x => !x.Root.Empty))
				fw_node.Add2(XmlTag.FloatingWindow, fw, false);

			return node;
		}

		/// <summary>Record the layout of this dock container</summary>
		public virtual XElement SaveLayout()
		{
			return ToXml(new XElement(XmlTag.DockContainerLayout));
		}

		/// <summary>
		/// Update the layout of this dock container using the data in 'node'
		/// Use 'content_factory' to create content on demand during loading.
		/// 'content_factory(string persist_name, string type_name, XElement user_data)'</summary>
		public virtual void LoadLayout(XElement node, Func<string, string, XElement, DockControl> content_factory = null)
		{
			if (node.Name != XmlTag.DockContainerLayout)
				throw new Exception("XML data does not contain dock container layout information");

			// Get a collection of the content currently loaded
			var all_content = AllContentInternal.ToDictionary(x => x.PersistName, x => x);

			// For each content node find a content object with matching PersistName and
			// move it to the location indicated in the saved state.
			foreach (var content_node in node.Elements(XmlTag.Contents, XmlTag.Content))
			{
				// Find a content object with a matching persistence name
				var name = content_node.Element(XmlTag.Name).As(string.Empty);
				var type = content_node.Element(XmlTag.Type).As(string.Empty);
				var udat = content_node.Element(XmlTag.UserData);
				if (all_content.TryGetValue(name, out var content) ||
					(content = content_factory?.Invoke(name, type, udat)) != null)
				{
					var loc = new DockLocation(content_node);
					if (loc.Address.First() != EDockSite.None)
						Add(content, loc);
					else
						content.DockPane = null;
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
				var fw = FloatingWindows.GetOrAdd(id);
				if (fw != null)
					fw.ApplyState(fw_node);
			}

			// Restore the active content
			var active = node.Element(XmlTag.Active)?.As<string>();
			if (active != null && all_content.TryGetValue(active, out var active_content))
			{
				if (active_content.DockPane != null)
					ActiveContent = active_content;
			}
		}

		/// <summary>Move all content to it's default dock address</summary>
		public virtual void ResetLayout()
		{
			// Adding each dockable without an address causes it to be moved to its default location
			foreach (var dc in AllContentInternal.ToArray())
				Add(dc, dc.DefaultDockLocation);
		}

		/// <summary>Output the tree structure</summary>
		public string DumpTree()
		{
			var sb = new StringBuilder();
			Root.DumpTree(sb, 0, string.Empty);
			return sb.ToString();
		}

		#region Commands
		public class CommandBase : ICommand
		{
			protected readonly DockContainer m_dc;
			public CommandBase(DockContainer dc)
			{
				m_dc = dc;
			}
			public Window Wnd
			{
				get { return Window.GetWindow(m_dc); }
			}
			public string LayoutFileFilter
			{
				get { return Util.FileDialogFilter("Layout Files", "*.xml"); }
			}
			public virtual void Execute(object _)
			{
			}
			public virtual bool CanExecute(object _)
			{
				return true;
			}
			public event EventHandler CanExecuteChanged { add { } remove { } }
		}
		public class LoadLayoutCommand : CommandBase
		{
			public LoadLayoutCommand(DockContainer dc)
				: base(dc)
			{ }

			/// <summary>Load the UI layout</summary>
			public override void Execute(object _)
			{
				// Prompt for a layout file
				var fd = new OpenFileDialog { Title = "Load layout", Filter = LayoutFileFilter };
				if (fd.ShowDialog(Wnd) != true)
					return;

				// Load the layout
				try { m_dc.LoadLayout(XDocument.Load(fd.FileName).Root); }
				catch (Exception ex)
				{
					MessageBox.Show(Wnd, $"Layout could not be loaded\r\n{ex.Message}", "Load Layout Failed", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
		public class SaveLayoutCommand : CommandBase
		{
			public SaveLayoutCommand(DockContainer dc)
				: base(dc)
			{ }

			/// <summary>Save the UI layout</summary>
			public override void Execute(object _)
			{
				var fd = new SaveFileDialog { Title = "Save layout", Filter = LayoutFileFilter, FileName = "Layout.xml", DefaultExt = "XML" };
				if (fd.ShowDialog(Wnd) != true)
					return;

				try { m_dc.SaveLayout().Save(fd.FileName); }
				catch (Exception ex)
				{
					MessageBox.Show(Wnd, $"Layout could not be saved\r\n{ex.Message}", "Save Layout Failed", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
		public class ResetLayoutCommand : CommandBase
		{
			public ResetLayoutCommand(DockContainer dc)
				: base(dc)
			{ }

			/// <summary>Reset the UI layout back to defaults</summary>
			public override void Execute(object _)
			{
				m_dc.ResetLayout();
			}
		}
		#endregion
	}

	/// <summary>
	/// Provides the implementation of the docking functionality.
	/// Classes that implement IDockable have one of these as a public property.</summary>
	[DebuggerDisplay("DockControl {TabText}")]
	public class DockControl : IDisposable
	{
		// Cut'n'Paste Boilerplate
		// Inherit:
		//   IDockable
		//
		// Constructor:
		//  DockControl = new DockControl(this, name)
		//  {
		//  	TabText = name, // optional
		//  	ShowTitle = false, // optional
		//  	DefaultDockLocation = new DockContainer.DockLocation(new[]{EDockSite.Centre}, 1), // optional
		//  	TabCMenu = CreateTabCMenu(), // optional
		//  };
		//
		// Destructor:
		//  DockControl = null;
		//
		// Property:
		//  /// <summary>Provides support for the DockContainer</summary>
		//  [Browsable(false)]
		//  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		//  public DockControl DockControl
		//  {
		//  	[DebuggerStepThrough] get { return m_dock_control; }
		//  	private set
		//  	{
		//  		if (m_dock_control == value) return;
		//  		Util.Dispose(ref m_dock_control);
		//  		m_dock_control = value;
		//  	}
		//  }
		//  private DockControl m_dock_control;

		/// <summary>Create the docking functionality helper.</summary>
		/// <param name="owner">The control that docking is being provided for</param>
		/// <param name="persist_name">The name to use for this instance when saving the layout to XML</param>
		public DockControl(FrameworkElement owner, string persist_name)
		{
			if (!(owner is IDockable))
				throw new Exception("'owner' must implement IDockable");

			Owner = owner;
			PersistName = persist_name;
			DefaultDockLocation = new DockLocation();
			TabText = null;
			TabIcon = null;
			TabCMenu = DefaultTabCMenu();

			DockAddresses = new Dictionary<Branch, EDockSite[]>();
		}
		public virtual void Dispose()
		{
			DockPane = null;
			DockContainer = null;
		}

		/// <summary>Get the control we're providing docking functionality for</summary>
		public UIElement Owner { [DebuggerStepThrough] get; private set; }

		/// <summary>Get/Set the dock container that manages this content.</summary>
		public DockContainer DockContainer
		{
			[DebuggerStepThrough]
			get { return m_dc; }
			set
			{
				if (m_dc == value) return;
				var old = m_dc;
				var nue = value;
				if (m_dc != null)
				{
					m_dc.ActiveContentChanged -= HandleActiveContentChanged;
					m_dc.Forget(this);
				}
				m_dc = value;
				if (m_dc != null)
				{
					m_dc.Remember(this);
					m_dc.ActiveContentChanged += HandleActiveContentChanged;
				}
				DockContainerChanged?.Invoke(this, new DockContainerChangedEventArgs(old, nue));

				// Handlers
				void HandleActiveContentChanged(object sender, ActiveContentChangedEventArgs e)
				{
					if (e.ContentNew == Owner || e.ContentOld == Owner)
						ActiveChanged?.Invoke(this, e);
				}
			}
		}
		private DockContainer m_dc;

		/// <summary>Raised when this DockControl is assigned to a dock container (or possibly to null)</summary>
		public event EventHandler<DockContainerChangedEventArgs> DockContainerChanged;

		/// <summary>Remove this DockControl from whatever dock container it is in</summary>
		public void Remove()
		{
			DockPane = null;
			DockContainer = null;
		}

		/// <summary>The dock container, auto-hide panel, or floating window that this content is docked within</summary>
		internal ITreeHost TreeHost
		{
			get { return DockPane?.TreeHost; }
		}

		/// <summary>Gets 'Owner' as an IDockable</summary>
		public IDockable Dockable
		{
			[DebuggerStepThrough]
			get { return (IDockable)Owner; }
		}

		/// <summary>Get/Set the pane that this content resides within.</summary>
		public DockPane DockPane
		{
			[DebuggerStepThrough]
			get { return m_pane; }
			set
			{
				if (m_pane == value) return;
				if (m_pane != null)
				{
					m_pane.Content.Remove(this);
				}
				SetDockPaneInternal(value);
				if (m_pane != null)
				{
					m_pane.Content.Add(this);
				}
				PaneChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		private DockPane m_pane;
		internal void SetDockPaneInternal(DockPane pane)
		{
			// Calling 'DockControl.set_DockPane' changes the Content list in the DockPane
			// which calls this method to change the DockPane on the content without recursively
			// changing the content list.
			m_pane = pane;

			// Record the dock container that 'pane' belongs to
			if (pane != null)
				DockContainer = pane.DockContainer;

			// If a pane is given, save the dock address for the root branch (if the pane is within a tree)
			if (pane != null && pane.ParentBranch != null)
				DockAddresses[m_pane.RootBranch] = DockAddress;
		}

		/// <summary>Raised when the pane this dockable is on is changing (possibly to null)</summary>
		public event EventHandler PaneChanged;

		/// <summary>Raised when the dockable is not in a pane (i.e. when DockPane becomes null)</summary>
		public event EventHandler Closed;

		/// <summary>Raised when this becomes the active content</summary>
		public event EventHandler<ActiveContentChangedEventArgs> ActiveChanged;

		/// <summary>Raised during 'ToXml' to allow clients to add extra data to the XML data for this object</summary>
		public event EventHandler<DockContainerSavingLayoutEventArgs> SavingLayout;

		/// <summary>The name to use for this instance when saving layout to XML</summary>
		public string PersistName { get; private set; }

		/// <summary>The dock location to use if not otherwise given</summary>
		public DockLocation DefaultDockLocation { get; set; }

		/// <summary>Get the current dock location</summary>
		public DockLocation CurrentDockLocation
		{
			get
			{
				if (TreeHost is FloatingWindow fw)
					return new DockLocation(DockAddress, ContentIndex, float_window_id: fw.Id);

				if (TreeHost is AutoHidePanel ah)
					return new DockLocation(DockAddress, ContentIndex, auto_hide: ah.DockSite);

				return new DockLocation(DockAddress, ContentIndex);
			}
		}

		/// <summary>Get/Set the dock site within the branch that contains 'DockPane'</summary>
		public EDockSite DockSite
		{
			get { return DockPane?.DockSite ?? EDockSite.Centre; }
			set
			{
				if (DockSite == value) return;

				// Must have an existing pane
				if (DockPane == null || DockPane.ParentBranch == null)
					throw new Exception("Can only change the dock site of this item after it has been added to a dock container");

				// Move this content to a different dock site within 'branch'
				var branch = DockPane.ParentBranch;
				var pane = branch.DockPane(value);
				pane.Content.Add(this);
			}
		}

		/// <summary>The full location of this content in the tree of docked windows from Root branch to 'DockPane'</summary>
		public EDockSite[] DockAddress
		{
			get { return DockPane != null ? DockPane.DockAddress : new[] { EDockSite.None }; }
		}

		/// <summary>The index of this dockable within the content of 'DockPane'</summary>
		public int ContentIndex
		{
			get { return DockPane?.Content.IndexOf(this) ?? int.MaxValue; }
		}

		/// <summary>Get/Set whether this content is in a floating window</summary>
		public bool IsFloating
		{
			get { return TreeHost is FloatingWindow; }
			set
			{
				if (IsFloating == value) return;

				//// Record properties of the Pane we're currently in, so we can
				//// set them on new pane that will host this content 
				//var strip_location = DockPane?.TabStripCtrl.StripLocation;

				// Float this dockable
				if (value)
				{
					// Get a floating window (preferably one that has hosted this item before)
					var fw = DockContainer.FloatingWindows.GetOrAdd(x => DockAddresses.ContainsKey(x.Root));

					// Get the dock address associated with this floating window
					var hs = DockAddressFor(fw);

					// Add this dockable to the floating window
					fw.Add(Dockable, hs);

					// Ensure the floating window is visible
					if (!fw.IsVisible)
						fw.Show();
				}

				// Dock this content back in the dock container
				else
				{
					// Get the dock address associated with the dock container
					var hs = DockAddressFor(DockContainer);

					// Return this content to the dock container
					DockContainer.Add(Dockable, hs);
				}

				// Copy pane state to the new pane
				if (DockPane != null)
				{
					//if (strip_location != null)
					//	DockPane.TabStripCtrl.StripLocation = strip_location.Value;
				}
			}
		}

		///// <summary>The floating window that hosts this content (if on a floating window, otherwise null)</summary>
		//public FloatingWindow HostFloatingWindow
		//{
		//	get { return TreeHost as FloatingWindow; }
		//}

		/// <summary>Get/Set whether this content is in an auto-hide panel</summary>
		public bool IsAutoHide
		{
			get { return TreeHost is AutoHidePanel; }
			set
			{
				if (IsAutoHide == value) return;

				// Auto hide this dockable
				if (value)
				{
					// If currently docked in the centre, don't allow auto hide
					var side = DockPane?.AutoHideSide ?? EDockSite.Centre;
					if (side == EDockSite.Centre)
						throw new Exception("Cannot auto hide this dockable. It is either not within a pane, or the pane is docked in the centre dock site");

					// Get the auto-hide panel associated with this dock site
					var ah = DockContainer.AutoHidePanels[side];

					// Add this dockable to the auto-hide panel
					ah.Add(Dockable);
				}

				// Dock this content back in the dock container
				else
				{
					var old_host = TreeHost as AutoHidePanel;

					// Get the dock address associated with the dock container
					var hs = DockAddressFor(DockContainer);

					// Return this content to the dock container
					DockContainer.Add(Dockable, hs);

					// Hide the auto hide panel so that it doesn't obscure the now pinned content
					if (old_host != null)
						old_host.PoppedOut = false;
				}
			}
		}

		/// <summary>A record of where this pane was located in a dock container, auto hide window, or floating window</summary>
		internal EDockSite[] DockAddressFor(ITreeHost tree)
		{
			return DockAddresses.GetOrAdd(tree.Root, x =>
			{
				if (DefaultDockLocation.FloatingWindowId != null)
				{
					// If the default dock location is a floating window, and 'root' is
					// the root branch of that floating window, then return the default address.
					// Otherwise, return sensible defaults appropriate for 'root'
					var fw = DockContainer.FloatingWindows.Get(DefaultDockLocation.FloatingWindowId.Value);
					if (fw != null)
					{
						if (fw.Root == tree.Root) return DefaultDockLocation.Address;
						return new[] { EDockSite.Centre };
					}
				}
				else if (DefaultDockLocation.AutoHide != null)
				{
					// If the default dock location is an auto hide window, and 'root' is
					// the root branch of that auto hide window, then return the default address.
					// If 'root' is a floating window, return Centre
					// If 'root' is the dock container, return the edge site that matches the auto hide panel's dock site
					var ah = DockContainer.AutoHidePanels[DefaultDockLocation.AutoHide.Value];
					if (ah != null)
					{
						if (ah.Root == tree.Root) return DefaultDockLocation.Address;
						if (tree is FloatingWindow) return new[] { EDockSite.Centre };
						return new[] { ah.DockSite };
					}
				}
				else if (DockContainer.Root == tree.Root)
				{
					// If the default dock location is the dock container and 'root' is
					// the root branch of the dock container, then return the default address
					return DefaultDockLocation.Address;
				}
				// Otherwise, default to the centre dock site
				return new[] { EDockSite.Centre };
			});
		}
		private Dictionary<Branch, EDockSite[]> DockAddresses { get; set; }

		/// <summary>Get/Set whether the dock pane title control is visible while this content is active. Null means inherit default</summary>
		public bool? ShowTitle { get; set; }

		/// <summary>Get/Set whether the location of the tab strip control while this content is active. Null means inherit default</summary>
		public EDockSite? TabStripLocation { get; set; }

		/// <summary>The text to display on the tab. Defaults to 'Owner.Text'</summary>
		public string TabText
		{
			get { return m_tab_text ?? (Owner as Window)?.Title; }
			set
			{
				if (m_tab_text == value) return;

				// Have to invalidate the whole tab strip, because the text length
				// will change causing the other tabs to move.
				m_tab_text = value;
				InvalidateTitle();
				InvalidateTabStrip();
			}
		}
		private string m_tab_text;

		/// <summary>The icon to display on the tab. Defaults to '(Owner as Window)?.Icon'</summary>
		public ImageSource TabIcon
		{
			get
			{
				// Note: not using Icon as the type for TabIcon because GDI+ doesn't support
				// drawing icons. The DrawIcon function calls the win API function DrawIconEx
				// which doesn't handle transforms properly

				// If an icon image has been specified, return that
				// If the content is a Window and it has an icon, use that
				return m_icon ?? (Owner as Window)?.Icon;
			}
			set
			{
				if (m_icon == value) return;
				m_icon = value;
				InvalidateTabStrip();
			}
		}
		private ImageSource m_icon;
#if false
			/// <summary>The colour to use for this tab's text. If null, defaults to the colour set of the containing TabStrip</summary>
			public OptionData.ColourSet TabColoursActive
			{
				get { return m_tab_colours_active; }
				set
				{
					if (m_tab_colours_active == value) return;
					m_tab_colours_active = value;
					InvalidateTab();
				}
			}
			private OptionData.ColourSet m_tab_colours_active;

			/// <summary>The colour to use for this tab's text. If null, defaults to the colour set of the containing TabStrip</summary>
			public OptionData.ColourSet TabColoursInactive
			{
				get { return m_tab_colours_inactive; }
				set
				{
					if (m_tab_colours_inactive == value) return;
					m_tab_colours_inactive = value;
					InvalidateTab();
				}
			}
			private OptionData.ColourSet m_tab_colours_inactive;

			/// <summary>The font to use for the active tab. If null, defaults to the fonts of the containing TabStrip</summary>
			public Font TabFontActive
			{
				get { return m_tab_font_active; }
				set
				{
					if (m_tab_font_active == value) return;

					// Changing the font can effect the size of the tab, and therefore
					// the layout of the whole tab strip
					m_tab_font_active = value;
					InvalidateTabStrip();
				}
			}
			private Font m_tab_font_active;

			/// <summary>The font used on the inactive tabs. If null, defaults to the fonts of the containing TabStrip</summary>
			public Font TabFontInactive
			{
				get { return m_tab_font_inactive; }
				set
				{
					if (m_tab_font_inactive == value) return;

					// Changing the font can effect the size of the tab, and therefore
					// the layout of the whole tab strip
					m_tab_font_inactive = value;
					InvalidateTabStrip();
				}
			}
			private Font m_tab_font_inactive;
#endif
		/// <summary>A tool tip to display when the mouse hovers over the tab for this content</summary>
		public string TabToolTip
		{
			get { return m_impl_tab_tt ?? TabText; }
			set { m_impl_tab_tt = value; }
		}
		private string m_impl_tab_tt;

		/// <summary>A context menu to display when the tab for this content is right clicked</summary>
		public ContextMenu TabCMenu
		{
			get { return m_impl_tab_cmenu; }
			set { m_impl_tab_cmenu = value; }
		}
		private ContextMenu m_impl_tab_cmenu;

		/// <summary>Creates a default context menu for the tab. Use: TabCMenu = DefaultTabCMenu()</summary>
		public ContextMenu DefaultTabCMenu()
		{
			var cmenu = new ContextMenu();
			{
				var opt = cmenu.Items.Add2(new MenuItem { Header = "Close" });
				opt.Click += (s, a) =>
				{
					DockPane = null;
					Closed?.Invoke(this, EventArgs.Empty);
				};
			}
			return cmenu;
		}

		/// <summary>True if this content is the active content within the owning dock container</summary>
		public bool IsActiveContent
		{
			get { return DockContainer?.ActiveContent == this; }
			set
			{
				if (value && DockContainer != null)
					DockContainer.ActiveContent = this;
			}
		}

		/// <summary>True if this content is the active content within its containing dock pane</summary>
		public bool IsActiveContentInPane
		{
			get { return DockPane?.ContentView.CurrentItem == this; }
			set
			{
				if (!value || DockPane == null) return;
				DockPane.ContentView.MoveCurrentTo(this);
			}
		}

		/// <summary>Save/Restore the control (possibly child control) that had input focus when the content was last active</summary>
		internal void SaveFocus()
		{
			m_kb_focus = Owner.IsKeyboardFocusWithin ? Keyboard.FocusedElement : null;
		}
		internal void RestoreFocus()
		{
			// Only restore focus if 'm_kb_focus' is still a child of 'Owner'
			if (m_kb_focus is DependencyObject dep && Owner.IsVisualDescendant(dep))
				Keyboard.Focus(m_kb_focus);

			m_kb_focus = null;
		}
		private IInputElement m_kb_focus;

		/// <summary>Save to XML</summary>
		public XElement ToXml(XElement node)
		{
			// Allow clients to add extra data that will be provided in LoadLayout
			var user = new XElement(XmlTag.UserData);
			SavingLayout?.Invoke(this, new DockContainerSavingLayoutEventArgs(user));

			// Add the name to identify the content
			node.Add2(XmlTag.Name, PersistName, false);
			node.Add2(XmlTag.Type, Owner.GetType().FullName, false);
			CurrentDockLocation.ToXml(node);
			node.Add2(user);

			return node;
		}

		/// <summary>Invalidate the tab that represents this content</summary>
		public void InvalidateTab()
		{
			if (DockPane == null) return;
			//var tab = DockPane.TabStripCtrl.VisibleTabs.FirstOrDefault(x => x.Content == this);
			//if (tab != null)
			//	DockPane.TabStripCtrl.Invalidate(tab.DisplayBounds);
			//else
			//	InvalidateTabStrip();
		}

		/// <summary>Invalidate the tab strip that contains the tab for this content</summary>
		public void InvalidateTabStrip()
		{
			if (DockPane == null) return;
			//DockPane.TabStripCtrl.Invalidate();
		}

		/// <summary>Invalidate the title that represents this content</summary>
		public void InvalidateTitle()
		{
			if (DockPane == null) return;
			//DockPane.TitleCtrl.Invalidate();
		}

		/// <summary></summary>
		//public override string ToString()
		//{
		//	if (TabText.HasValue()) return TabText;
		//	if (PersistName.HasValue()) return PersistName;
		//	if (Owner is FrameworkElement fe && fe.Name.HasValue()) return fe.Name;
		//	return Owner.GetType().Name;
		//}
	}

	/// <summary>A wrapper control that hosts a control and implements IDockable</summary>
	public class Dockable : DockPanel, IDockable
	{
		public Dockable(Control hostee, string persist_name, DockLocation location = null)
		{
			MinWidth = hostee.MinWidth;
			MinHeight = hostee.MinHeight;
			MaxWidth = hostee.MaxWidth;
			MaxHeight = hostee.MaxHeight;
			Children.Add(hostee);

			DockControl = new DockControl(this, persist_name) { TabText = persist_name };
			if (location != null)
				DockControl.DefaultDockLocation = location;
		}
		public virtual void Dispose()
		{
			DockControl = null;
		}

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false)]
		public DockControl DockControl
		{
			get { return m_dock_control; }
			private set
			{
				if (m_dock_control == value) return;
				Util.Dispose(ref m_dock_control);
				m_dock_control = value;
			}
		}
		private DockControl m_dock_control;
	}

	/// <summary>Dock container classes</summary>
	namespace DockContainerDetail
	{
		/// <summary>
		/// A pane groups a set of IDockable items together. Only one IDockable is displayed at a time in the pane,
		/// but tabs for all dockable items are displayed in the tab strip along the top, bottom, left, or right.</summary>
		[DebuggerDisplay("{DumpDesc()}")]
		public class DockPane : DockPanel, IPaneOrBranch, IDisposable
		{
			internal DockPane(DockContainer owner, bool show_pin = true, bool show_close = true)
			{
				DockContainer = owner ?? throw new ArgumentNullException("The owning dock container cannot be null");
				ApplyToVisibleContentOnly = false;

				// Create the collection that manages the content within this pane
				Content = new ObservableCollection<DockControl>();

				// Add the title bar
				TitleBar = Children.Add2(new TitleBar(this, DockContainer.Options) { ShowPin = show_pin, ShowClose = show_close });
				DockPanel.SetDock(TitleBar, Dock.Top);

				// Add the tab strip
				TabStrip = Children.Add2(new TabStrip(EDockSite.Bottom, owner.Options));
				DockPanel.SetDock(TabStrip, Dock.Bottom);

				// Add the panel for the content
				Centre = Children.Add2(new Grid { Name = "DockPaneContent" });
			}
			public virtual void Dispose()
			{
				// Note: we don't own any of the content
				VisibleContent = null;
				Content = null;
				DockContainer = null;
			}

			/// <summary>The owning dock container</summary>
			public DockContainer DockContainer { [DebuggerStepThrough] get; private set; }

			/// <summary>The title bar control for the pane</summary>
			public TitleBar TitleBar { get; private set; }

			/// <summary>The tab strip for the pane (visibility controlled adding buttons to the tab strip)</summary>
			public TabStrip TabStrip { get; private set; }

			/// <summary>The panel that displays the visible content</summary>
			public Panel Centre { get; private set; }

			/// <summary>The control that hosts the dock pane and branch tree</summary>
			internal ITreeHost TreeHost
			{
				[DebuggerStepThrough]
				get { return ParentBranch?.TreeHost; }
			}

			/// <summary>The branch at the top of the tree that contains this pane</summary>
			internal Branch RootBranch
			{
				[DebuggerStepThrough]
				get { return ParentBranch?.RootBranch; }
			}

			/// <summary>The branch that contains this pane</summary>
			internal Branch ParentBranch
			{
				[DebuggerStepThrough]
				get { return Parent as Branch; }
			}
			Branch IPaneOrBranch.ParentBranch
			{
				get { return ParentBranch; }
			}

			/// <summary>Get the dock site for this pane</summary>
			internal EDockSite DockSite
			{
				get { return ParentBranch?.Descendants.Single(x => x.Item == this).DockSite ?? EDockSite.None; }
			}

			/// <summary>Get the dock sites, from top to bottom, describing where this pane is located in the tree </summary>
			internal EDockSite[] DockAddress
			{
				get { return ParentBranch.DockAddress.Concat(DockSite).ToArray(); }
			}

			/// <summary>The content hosted by this pane</summary>
			public ObservableCollection<DockControl> Content
			{
				[DebuggerStepThrough]
				get { return m_content; }
				private set
				{
					if (m_content == value) return;
					if (m_content != null)
					{
						// Note: The DockPane does not own the content
						ContentView = null;
						m_content.CollectionChanged -= HandleCollectionChanged;
					}
					m_content = value;
					if (m_content != null)
					{
						ContentView = CollectionViewSource.GetDefaultView(m_content);
						m_content.CollectionChanged += HandleCollectionChanged;
					}

					/// <summary>Handle the list of content in this pane changing</summary>
					void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
					{
						// Update the pane with the change to the content.
						// Note: the 'ContentView' updates the ActiveContent.
						switch (e.Action)
						{
						case NotifyCollectionChangedAction.Add:
							{
								foreach (var dc in e.NewItems.Cast<DockControl>())
								{
									// Before adding dockables to the content list, remove them from any previous panes (or even this pane)
									if (dc == null) throw new ArgumentNullException(nameof(dc), "Cannot add 'null' content to a dock pane");
									dc.DockPane = null;

									// Set this pane as the owning dock pane for the dockables.
									// Don't add the dockable as a child control yet, that happens when the active content changes.
									dc.SetDockPaneInternal(this);

									// Add a tab button for this item
									TabStrip.Buttons.Insert(Content.IndexOf(dc), new TabButton(dc, DockContainer.Options));

									// Notify tree changed
									ParentBranch?.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.Added, dockcontrol: dc));
								}
								break;
							}
						case NotifyCollectionChangedAction.Remove:
							{
								foreach (var dc in e.OldItems.Cast<DockControl>())
								{
									// Clear the dock pane from the content
									if (dc == null) throw new ArgumentNullException(nameof(dc), "Cannot remove 'null' content from a dock pane");
									if (dc.DockPane != this) throw new Exception($"Dockable does not belong to this DockPane");
									dc.SetDockPaneInternal(null);

									// Remove the tab button
									TabStrip.Buttons.RemoveIf(x => x.DockControl == dc);

									// Notify tree changed
									ParentBranch?.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.Removed, dockcontrol: dc));

									// Remove empty branches
									RootBranch?.PruneBranches();
								}
								break;
							}
						}
					}
				}
			}
			private ObservableCollection<DockControl> m_content;

			/// <summary>The view of the content in this pane</summary>
			public ICollectionView ContentView
			{
				get { return m_content_view; }
				private set
				{
					if (m_content_view == value) return;
					if (m_content_view != null)
					{
						m_content_view.CurrentChanged -= HandleCurrentChanged;
					}
					m_content_view = value;
					if (m_content_view != null)
					{
						m_content_view.CurrentChanged += HandleCurrentChanged;
					}

					// When the current position changes, update the active content
					void HandleCurrentChanged(object sender, EventArgs e)
					{
						VisibleContent = (DockControl)ContentView.CurrentItem;
					}
				}
			}
			private ICollectionView m_content_view;

			/// <summary>
			/// The content in this pane that was last active (Not necessarily the active content for the dock container)
			/// There should always be visible content while 'Content' is not empty. Empty panes are destroyed.
			/// If this pane is not the active pane, then setting the visible content only raises events for this pane.
			/// If this is the active pane, then the dock container events are also raised.</summary>
			public DockControl VisibleContent
			{
				[DebuggerStepThrough]
				get { return m_visible_content; }
				set
				{
					if (m_visible_content == value || m_in_visible_content != 0) return;
					using (Scope.Create(() => ++m_in_visible_content, () => --m_in_visible_content))
					{
						// Ensure 'value' is the current item in the Content collection
						// Only content that is in this pane can be made active for this pane
						if (value != null && !ContentView.MoveCurrentTo(value))
							throw new Exception($"Dockable item '{value.TabText}' has not been added to this pane so can not be made the active content.");

						// Switch to the new active content
						var prev = m_visible_content;
						if (m_visible_content != null)
						{
							// Save the control that had input focus at the time this content became inactive
							m_visible_content.SaveFocus();

							// Remove the element
							// Note: don't dispose the content, we don't own it
							Centre.Children.Remove(m_visible_content.Owner);

							// Clear the title bar
							TitleBar.Title = string.Empty;

							//							// Clear the scroll offset
							//							AutoScrollPosition = Point.Empty;
						}

						// Note: 'm_visible_content' is of type 'DockControl' rather than 'IDockable' because when the DockControl is Disposed
						// 'IDockable.DockControl' gets set to null before the disposing actually happens. This means we wouldn't be able to access
						// the 'Owner' to remove it from the pane. i.e. 'IDockable.DockControl.Owner' throws because 'DockControl' is null during dispose.
						m_visible_content = value;

						if (m_visible_content != null)
						{
							// Add the element
							Centre.Children.Add(m_visible_content.Owner);

							// Set the title bar
							TitleBar.Title = m_visible_content.TabText;

							// Restore the input focus for this content
							m_visible_content.RestoreFocus();
						}

						//						// Ensure the tab for the active content is visible
						//						TabStripCtrl.MakeTabVisible(ContentView.CurrentPosition);

						// Raise the active content changed event on this pane.
						OnVisibleContentChanged(new ActiveContentChangedEventArgs(prev?.Dockable, value?.Dockable));

						// Notify the container of this tree that the active content in this pane was changed
						// (allowing the globally active content notification to be sent if this is the active pane)
						ParentBranch.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.ActiveContent, pane: this, dockcontrol: value));
					}
				}
			}
			private DockControl m_visible_content;
			private int m_in_visible_content;

			/// <summary>Get the text to display in the title bar for this pane</summary>
			public string CaptionText
			{
				get { return VisibleContent?.TabText ?? string.Empty; }
			}

			/// <summary>Get/Set the content in the pane floating</summary>
			public bool IsFloating
			{
				get
				{
					if (VisibleContent == null) return false;
					var floating = VisibleContent.IsFloating;

					// All content in a pane should be in the same state. This is true even if
					// 'ApplyToVisibleContentOnly' is true because any float/auto hide operation should
					// move that content to a different pane
					Debug.Assert(Content.All(x => x.IsFloating == floating), "All content in a pane should be in the same state");
					return floating;
				}
				set
				{
					// All content in this pane matches the floating state of the active content
					var content = VisibleContent;
					if (content == null)
						return;

					// Record properties of this pane so we can set them on the new pane
					// (this pane will be disposed if empty after the content has changed floating state)
					//					var strip_location = TabStripCtrl.StripLocation;

					// Change the state of the active content
					content.IsFloating = value;

					// Copy properties form this pane to the new pane that hosts 'content'
					var pane = content.DockPane;
					//					pane.TabStripCtrl.StripLocation = strip_location;

					// Add the remaining content to the same pane that 'content' is now in
					if (!ApplyToVisibleContentOnly)
						pane.Content.AddRange(Content.ToArray());
				}
			}

			/// <summary>Get/Set the content in this pane to auto hide mode</summary>
			public bool IsAutoHide
			{
				get
				{
					if (VisibleContent == null) return false;
					var auto_hide = VisibleContent.IsAutoHide;
					Debug.Assert(Content.All(x => x.IsAutoHide == auto_hide), "All content in a pane should be in the same state");
					return auto_hide;
				}
				set
				{
					// All content in this pane matches the auto hide state of the active content
					var content = VisibleContent;
					if (content == null)
						return;

					// Change the state of the active content
					content.IsAutoHide = value;

					// Add the remaining content to the same pane that 'content' is now in
					if (!ApplyToVisibleContentOnly)
					{
						var pane = content.DockPane;
						pane.Content.AddRange(Content.ToArray());
					}
				}
			}

			/// <summary>Get/Set whether operations such as floating or auto hiding apply to all content or just the visible content</summary>
			public bool ApplyToVisibleContentOnly { get; set; }

			/// <summary>Raised whenever the visible content for this dock pane changes</summary>
			public event EventHandler<ActiveContentChangedEventArgs> VisibleContentChanged;
			protected void OnVisibleContentChanged(ActiveContentChangedEventArgs args)
			{
				VisibleContentChanged?.Invoke(this, args);
			}

			/// <summary>Raised when the pane is activated</summary>
			public event EventHandler ActivatedChanged;
			internal void OnActivatedChanged()
			{
				ActivatedChanged?.Invoke(this, EventArgs.Empty);
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

			/// <summary>Get the side of the dock container to auto-hide to, based on this pane's location in the tree. Returns centre if no side is preferred</summary>
			internal EDockSite AutoHideSide
			{
				get
				{
					var side = DockSite;

					// Find the highest, non-centre position, ancestor of this pane
					for (var b = ParentBranch; side == EDockSite.Centre && b != null; b = b.ParentBranch)
					{
						if (b.DockSite == EDockSite.Centre) continue;
						side = b.DockSite;
					}
					return side;
				}
			}
#if false
			/// <summary>Layout the pane</summary>
			protected override void OnLayout(LayoutEventArgs e)
			{
				// Measure the remaining space and use that for the active content
				var rect = DisplayRectangle;
				if (rect.Area() > 0 && !IsDisposed)
				{
					using (this.SuspendLayout(false))
					{
						// Reset the scroll position
						HorizontalScroll.Value = 0;
						VerticalScroll.Value = 0;

						// Position the title bar
						if (TitleCtrl != null)
						{
							var bounds = TitleCtrl.CalcBounds(rect);
							TitleCtrl.Bounds = bounds;

							// Update the available area if the title control is visible
							if (TitleCtrl.Visible)
								rect = rect.Subtract(bounds);

							// Auto hide is hidden if the pane is in the centre dock site
							TitleCtrl.ButtonAutoHide.Visible = AutoHideSide != EDockSite.Centre || IsAutoHide; // Always visible if the pane is within an auto-hide panel
						}

						// Position the tab strip
						if (TabStripCtrl != null)
						{
							var bounds = TabStripCtrl.CalcBounds(rect);
							TabStripCtrl.Bounds = bounds;

							// Update the available area if the tab strip is visible
							if (TabStripCtrl.Visible)
								rect = rect.Subtract(bounds);
						}

						// Use the remaining area for content
						if (VisibleContent != null)
							VisibleContent.Owner.Bounds = rect;
					}
				}

				base.OnLayout(e);
			}
#endif
#if false
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
#endif

			/// <summary>Record the layout of the pane</summary>
			public XElement ToXml(XElement node)
			{
				// Record the content that is the visible content in this pane
				if (VisibleContent != null)
					node.Add2(XmlTag.Visible, VisibleContent.PersistName, false);

				//				// Save the strip location
				//				if (TabStripCtrl != null)
				//					node.Add2(XmlTag.StripLocation, TabStripCtrl.StripLocation, false);

				return node;
			}

			/// <summary>Apply layout state to this pane</summary>
			public void ApplyState(XElement node)
			{
				// Restore the visible content
				var visible = node.Element(XmlTag.Visible)?.As<string>();
				if (visible != null)
				{
					var content = Content.FirstOrDefault(x => x.PersistName == visible);
					if (content != null)
						VisibleContent = content;
				}

				//				// Restore the strip location
				//				var strip_location = node.Element(XmlTag.StripLocation)?.As<EDockSite>();
				//				if (strip_location != null)
				//					TabStripCtrl.StripLocation = strip_location.Value;
			}

			/// <summary>Output the tree structure as a string. (Recursion method, call 'DockContainer.DumpTree()')</summary>
			public void DumpTree(StringBuilder sb, int indent, string prefix)
			{
				sb.Append(' ', indent).Append(prefix).Append("Pane: ").Append(DumpDesc()).AppendLine();
			}

			/// <summary>Self consistency check (Recursion method, call 'DockContainer.ValidateTree()')</summary>
			public void ValidateTree()
			{
				if (DockContainer == null)
					throw new ObjectDisposedException("This dock pane has been disposed");

				if (ParentBranch?.Descendants[DockSite].Item as DockPane != this)
					throw new Exception("Dock pane parent does not link to this dock pane");
			}

			/// <summary>A string description of the pane</summary>
			public string DumpDesc()
			{
				return $"Count={Content.Count} Active={(Content.Count != 0 ? CaptionText : "<empty pane>")}";
			}
#if false
			/// <summary>A custom control for drawing the title bar, including text, close button and pin button</summary>
			public class PaneTitleControl : Control ,IDisposable
			{
				public PaneTitleControl(DockPane owner)
				{
//					SetStyle(ControlStyles.Selectable, false);
//					Text = GetType().FullName;
//					ResizeRedraw = true;
					Hidden = false;

					DockPane = owner;
//					using (this.SuspendLayout(false))
					{
//						ContextMenuStrip = CreateContextMenu(owner);
						ButtonClose = new CaptionButton(Resources.dock_close);
						ButtonAutoHide = new CaptionButton(DockPane.IsAutoHide ? Resources.dock_unpinned : Resources.dock_pinned);
						ButtonMenu = new CaptionButton(Resources.dock_menu);
					}
				}
				public virtual void Dispose()
				{
//					using (this.SuspendLayout(false))
					{
						ButtonClose = null;
						ButtonAutoHide = null;
						ButtonMenu = null;
					}
//					ContextMenuStrip = null;
					DockPane = null;
				}
				protected override void OnVisibleChanged(EventArgs e)
				{
					Visible &= !Hidden;
					base.OnVisibleChanged(e);
				}

				/// <summary>Get/Set whether this control is displayed</summary>
				public bool Hidden
				{
					get
					{
						if (m_hidden) return true;
						if (DockPane?.VisibleContent?.ShowTitle is bool show0 && show0 == false) return true;
						if (DockPane?.DockContainer?.Options.TitleBar.ShowTitleBars is bool show1 && show1 == false) return true;
						return false;
					}
					set { Visible = !(m_hidden = value); }
				}
				private bool m_hidden;

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
				private new void ContextMenu() { }

				/// <summary>Measure the height required by this control based on the content in 'DockPane'</summary>
				public double MeasureHeight()
				{
					var opts = DockPane.DockContainer.Options.TitleBar;
					return opts.Padding.Top + opts.TextFont.Height + opts.Padding.Bottom;
				}

				/// <summary>Returns the size of the pane title control given the available 'display_area'</summary>
				public Rect CalcBounds(Rect display_area)
				{
					// No title for empty panes
					if (DockPane.Content.Count == 0)
						return Rect.Empty;

					// Title bars are always at the top
					var r = display_area;
					r.Height = MeasureHeight();
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
					using (var brush = new LinearGradientBrush(ClientRectangle, cols.Beg, cols.End, cols.Mode) { Blend = cols.Blend })
						gfx.FillRectangle(brush, e.ClipRectangle);

					// Calculate the area available for the caption text
					RectangleF r = ClientRectangle;
					r.X += opts.Padding.Left;
					r.Y += opts.Padding.Top;
					r.Width -= opts.Padding.Left + opts.Padding.Right;
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
					DockPane.TriggerLayout();
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
					Visible = !Hidden;
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
					using (cmenu.SuspendLayout(false))
					{
						{ // Float the pane in a new window
							var opt = cmenu.Items.Add2(new ToolStripMenuItem("Float"));
							cmenu.Opening += (s, a) =>
							{
								opt.Enabled = !pane.IsFloating;
							};
							opt.Click += (s, a) =>
							{
								pane.IsFloating = true;
							};
						}
						{ // Return the pane to the dock container
							var opt = cmenu.Items.Add2(new ToolStripMenuItem("Dock"));
							cmenu.Opening += (s, a) =>
							{
								opt.Enabled = pane.IsFloating;
							};
							opt.Click += (s, a) =>
							{
								pane.IsFloating = false;
							};
						}
						cmenu.Items.Add2(new ToolStripSeparator());
						foreach (var c in pane.Content)
						{
							var opt = cmenu.Items.Add2(new ToolStripMenuItem(c.TabText, c.TabIcon));
							cmenu.Opening += (s, a) =>
							{
								// Copy the tab context menu to this menu item (copy because otherwise the items get disposed)
								opt.DropDownItems.Clear();
								if (c.TabCMenu != null)
									foreach (var tab_item in c.TabCMenu.Items.OfType<ToolStripMenuItem>())
										opt.DropDownItems.Add(tab_item.Text, tab_item.Image, (ss, aa) => tab_item.PerformClick());
							};
							opt.Click += (s, a) =>
							{
								pane.VisibleContent = c;
							};
						}
					}
					return cmenu;
				}

				/// <summary>Update when the dock pane content changes</summary>
				private void HandleContentChanging(object sender, ListChgEventArgs<DockControl> e)
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
				public class CaptionButton : Control
				{
					public CaptionButton(Bitmap image)
					{
						SetStyle(ControlStyles.SupportsTransparentBackColor, true);
						Text = GetType().FullName;
						BackColor = Color.Transparent;
						Image = image;
						Hidden = false;
					}
					protected override void OnVisibleChanged(EventArgs e)
					{
						Visible &= !Hidden;
						base.OnVisibleChanged(e);
					}

					/// <summary>Get/Set whether this control is visible</summary>
					public bool Hidden
					{
						get { return m_hidden; }
						set { Visible = !(m_hidden = value); }
					}
					private bool m_hidden;

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
						Visible = !Hidden;
					}
				}
			}
#endif
		}

		/// <summary>
		/// Represents a node in the tree of dock panes.
		/// Each node has 5 children; Centre, Left, Right, Top, Bottom.
		/// A root branch can be hosted in a dock container, floating window, or auto hide window</summary>
		[DebuggerDisplay("{DumpDesc()}")]
		internal class Branch : DockPanel, IPaneOrBranch, IDisposable
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
					Descendants[EDockSite.Centre].Item = new DockPane(DockContainer, show_pin:false);

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

		/// <summary>A floating window that hosts a tree of dock panes</summary>
		[DebuggerDisplay("FloatingWindow")]
		public class FloatingWindow : Window, ITreeHost, IDisposable
		{
			private Panel m_content;
			public FloatingWindow(DockContainer dc)
			{
				ShowInTaskbar = true;
				ResizeMode = ResizeMode.CanResizeWithGrip;
				WindowStartupLocation = WindowStartupLocation.CenterOwner;
				//HideOnClose = true;
				//Text = string.Empty;
				//ShowIcon = false;
				Owner = GetWindow(dc);
				Content = m_content = new Canvas();

				DockContainer = dc;
				Root = new Branch(dc, DockSizeData.Quarters);
			}
			public virtual void Dispose()
			{
				Root = null;
				DockContainer = null;
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

					/// <summary>Handler for when the active content changes</summary>
					void HandleActiveContentChanged(object sender, ActiveContentChangedEventArgs e)
					{
						//						InvalidateVisual();
					}
				}
			}
			DockContainer ITreeHost.DockContainer
			{
				get { return DockContainer; }
			}
			private DockContainer m_impl_dc;

			/// <summary>The root level branch of the tree in this floating window</summary>
			internal Branch Root
			{
				[DebuggerStepThrough]
				get { return m_root; }
				set
				{
					if (m_root == value) return;
					if (m_root != null)
					{
						m_root.TreeChanged -= HandleTreeChanged;
						m_content.Children.Remove(m_root);
						Util.Dispose(ref m_root);
					}
					m_root = value;
					if (m_root != null)
					{
						m_content.Children.Add(m_root);
						m_root.TreeChanged += HandleTreeChanged;
					}

					/// <summary>Handler for when panes are added/removed from the tree</summary>
					void HandleTreeChanged(object sender, TreeChangedEventArgs args)
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
								//								InvalidateArrange();
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
								//								InvalidateArrange();
								if (!Root.AllContent.Any())
								{
									Hide();
									UpdateUI();
								}
								break;
							}
						}

						/// <summary>Update the floating window</summary>
						void UpdateUI()
						{
#if false
							var content = ActiveContent;
							if (content != null)
							{
								Text = content.TabText;
								var bm = content.TabIcon as Bitmap;
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
#endif
						}
					}
				}
			}
			Branch ITreeHost.Root
			{
				get { return Root; }
			}
			private Branch m_root;

			/// <summary>
			/// Get/Set the active content on this floating window. This will cause the pane that the content is on to also become active.
			/// To change the active content in a pane without making the pane active, assign to the pane's ActiveContent property</summary>
			public DockControl ActiveContent
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

			/// <summary>The current screen location and size of this window</summary>
			public Rect Bounds
			{
				get { return new Rect(Left, Top, Width, Height); }
				set
				{
					Left = value.Left;
					Top = value.Top;
					Width = value.Width;
					Height = value.Height;
				}
			}

			/// <summary>Add a dockable instance to this branch at the position described by 'location'.</summary>
			internal DockPane Add(DockControl dc, int index, params EDockSite[] location)
			{
				if (dc == null)
					throw new ArgumentNullException(nameof(dc), "'dockable' or 'dockable.DockControl' cannot be 'null'");

				return Root.Add(dc, index, location);
			}
			public DockPane Add(IDockable dockable, int index, params EDockSite[] location)
			{
				return Add(dockable?.DockControl, index, location);
			}
			public DockPane Add(IDockable dockable, params EDockSite[] location)
			{
				var addr = location.Length != 0 ? location : new[] { EDockSite.Centre };
				return Add(dockable, int.MaxValue, addr);
			}
#if false
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
							(int)Win32.SendMessage(Handle, Win32.WM_NCHITTEST, IntPtr.Zero, m.LParam) == (int)Win32.HitTest.HTCAPTION)
						{
							// Move all content back to the dock container
							using (this.SuspendLayout(layout_on_resume: false))
							{
								var content = Root.AllPanes.SelectMany(x => x.Content).ToArray();
								foreach (var c in content)
									c.IsFloating = false;

								Root?.TriggerLayout();
							}

							Hide();
							return;
						}
						break;
					}
				}
				base.WndProc(ref m);
			}
#endif
			/// <summary>Save state to XML</summary>
			public XElement ToXml(XElement node)
			{
				// Save the ID assigned to this window
				node.Add2(XmlTag.Id, Id, false);

				//				// Save whether the floating window is pinned to the dock container
				//				node.Add2(XmlTag.Pinned, PinWindow, false);

				// Save the screen-space location of the floating window. If pinned, save the offset bounds
				var bnds = Bounds;
				//				if (PinWindow) bnds = bnds.Shifted(-TargetFrame.Left, -TargetFrame.Top);
				node.Add2(XmlTag.Bounds, bnds, false);

				// Save whether the floating window is shown or now
				node.Add2(XmlTag.Visible, IsVisible, false);

				// Save the tree structure of the floating window
				node.Add2(XmlTag.Tree, Root, false);
				return node;
			}

			/// <summary>Apply state to this floating window</summary>
			public void ApplyState(XElement node)
			{
				//				// Restore the pinned state
				//				var pinned = node.Element(XmlTag.Pinned)?.As<bool>();
				//				if (pinned != null)
				//					PinWindow = pinned.Value;

				// Move the floating window to the saved position (clamped by the virtual screen)
				var bounds = node.Element(XmlTag.Bounds)?.As<Rect>();
				if (bounds != null)
				{
					// If 'PinWindow' is set, then the bounds are relative to the parent window
					var bnds = bounds.Value;
					//					if (PinWindow) bnds = bnds.Shifted(TargetFrame.Left, TargetFrame.Top);
					Bounds = Gui_.OnScreen(bnds);
				}

				// Update the tree layout
				var tree_node = node.Element(XmlTag.Tree);
				if (tree_node != null)
					Root.ApplyState(tree_node);

				// Restore visibility
				var visible = node.Element(XmlTag.Visible)?.As<bool>();
				if (visible != null)
					Visibility = visible.Value ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		/// <summary>A panel that docks to the edges of the main dock container and auto hides when focus is lost</summary>
		[DebuggerDisplay("AutoHidePanel {DockSite}")]
		public class AutoHidePanel : Grid, ITreeHost, IDisposable
		{
			// An auto hide panel is a panel that pops out from the edges of the dock container.
			// It is basically a root branch with a single centre dock pane. The tab strip from the
			// dock pane is used for the auto-hide tab strip displayed around the edges of the control.
			// Other behaviours:
			//  - Click pin on the pane only pins the active content
			//  - Clicking a tab makes that dockable the active content
			//  - 'Root' only uses the centre dock site

			public AutoHidePanel(DockContainer dc, EDockSite ds)
			{
				DockContainer = dc;
				DockSite = ds;
				PoppedOut = false;
				Visibility = Visibility.Collapsed;

				// Create a grid to container the root branch and the splitter
				var splitter_size = 5.0;
				switch (DockSite)
				{
				default: throw new Exception($"Auto hide panels cannot be docked to {ds}");
				case EDockSite.Left:
				case EDockSite.Right:
					{
						// Vertical auto hide panel
						var gut0 = DockSite == EDockSite.Left ? GridUnitType.Pixel : GridUnitType.Star;
						var gut2 = DockSite == EDockSite.Left ? GridUnitType.Star : GridUnitType.Pixel;
						ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, gut0), MinWidth = 10 });
						ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
						ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, gut2), MinWidth = 10 });

						// Add the root branch to the appropriate column
						Root = Children.Add2(new Branch(dc, DockSizeData.Quarters));
						Grid.SetColumn(Root, DockSite == EDockSite.Left ? 0 : 2);

						// Add the splitter to the centre column
						var splitter = Children.Add2(new GridSplitter { Width = splitter_size, HorizontalAlignment = HorizontalAlignment.Stretch });
						Grid.SetColumn(splitter, 1);
						break;
					}
				case EDockSite.Top:
				case EDockSite.Bottom:
					{
						// Horizontal auto hide panel
						var gut0 = DockSite == EDockSite.Top ? GridUnitType.Pixel : GridUnitType.Star;
						var gut2 = DockSite == EDockSite.Bottom ? GridUnitType.Star : GridUnitType.Pixel;
						RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, gut0), MinHeight = 10 });
						RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
						RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, gut2), MinHeight = 10 });

						// Add the root branch to the appropriate row
						Root = Children.Add2(new Branch(dc, DockSizeData.Quarters));
						Grid.SetRow(Root, DockSite == EDockSite.Top ? 0 : 2);

						// Add the splitter to the centre row
						var splitter = Children.Add2(new GridSplitter { Height = splitter_size, VerticalAlignment = VerticalAlignment.Stretch });
						Grid.SetRow(splitter, 1);
						break;
					}
				}

				// Create an empty dock pane in the root branch
				var pane = Root.DockPane(EDockSite.Centre);

				// Remove the tab strip from the dock pane children so we can use
				// it as the auto hide tab strip for this auto hide panel
				pane.Children.Remove(pane.TabStrip);
				pane.TabStrip.StripLocation = ds;
			}
			public virtual void Dispose()
			{
				Root = null;
				DockContainer = null;
			}
			//protected override void OnVisualParentChanged(DependencyObject oldParent)
			//{
			//	base.OnVisualParentChanged(oldParent);
			//	switch (DockSite)
			//	{
			//	default: throw new Exception($"Auto hide panels cannot be docked to {DockSite}");
			//	case EDockSite.Left:
			//	case EDockSite.Right:
			//		{
			//			Root.Width = 0.25 * (Parent as FrameworkElement)?.ActualWidth ?? 50.0;
			//			break;
			//		}
			//	case EDockSite.Top:
			//	case EDockSite.Bottom:
			//		{
			//			Root.Height = 0.25 * (Parent as FrameworkElement)?.ActualHeight ?? 50.0;
			//			break;
			//		}
			//	}
			//}

			/// <summary>The dock container that owns this auto hide window</summary>
			public DockContainer DockContainer
			{
				get { return m_dc; }
				private set
				{
					if (m_dc == value) return;
					if (m_dc != null)
					{
						m_dc.ActiveContentChanged -= HandleActiveContentChanged;
					}
					m_dc = value;
					if (m_dc != null)
					{
						m_dc.ActiveContentChanged += HandleActiveContentChanged;
					}

					/// <summary>Handler for when the active content changes</summary>
					void HandleActiveContentChanged(object sender, ActiveContentChangedEventArgs e)
					{
						// Auto hide the auto hide panel whenever content that isn't in our tree becomes active
						if (e.ContentNew == null || e.ContentNew.DockControl.DockPane?.RootBranch != Root)
							PoppedOut = false;
					}
				}
			}
			DockContainer ITreeHost.DockContainer
			{
				get { return DockContainer; }
			}
			private DockContainer m_dc;

			/// <summary>The root level branch of the tree in this auto hide window</summary>
			internal Branch Root
			{
				[DebuggerStepThrough]
				get { return m_root; }
				private set
				{
					if (m_root == value) return;
					if (m_root != null)
					{
						m_root.TreeChanged -= HandleTreeChanged;
						Util.Dispose(ref m_root);
					}
					m_root = value;
					if (m_root != null)
					{
						m_root.TreeChanged += HandleTreeChanged;
					}

					/// <summary>Handler for when the tree in this auto hide panel changes</summary>
					void HandleTreeChanged(object sender, TreeChangedEventArgs args)
					{
						switch (args.Action)
						{
						case TreeChangedEventArgs.EAction.ActiveContent:
							{
								if (args.DockPane == DockPane)
									PoppedOut = true;

								break;
							}
						case TreeChangedEventArgs.EAction.Added:
							{
								// When the first content is added, 
								if (Root.AllContent.CountAtMost(2) == 1)
								{
									// Ensure the tab strip is visible
									TabStrip.Visibility = Visibility.Visible;

									// Make the first pane active
									ActivePane = Root.AllPanes.First();
								}

								// Change the behaviour of all dock panes in the auto hide panel to only operate on the visible content
								if (args.DockPane != null)
									args.DockPane.ApplyToVisibleContentOnly = true;

								break;
							}
						case TreeChangedEventArgs.EAction.Removed:
							{
								// When the last content is removed, hide the panel and the tab strip
								if (!Root.AllContent.Any())
								{
									TabStrip.Visibility = Visibility.Collapsed;
									PoppedOut = false;
								}
								break;
							}
						}
					}
				}
			}
			Branch ITreeHost.Root
			{
				get { return Root; }
			}
			private Branch m_root;

			/// <summary>Return the single dock pane for the auto-hide panel</summary>
			private DockPane DockPane
			{
				get { return Root.DockPane(EDockSite.Centre); }
			}

			/// <summary>The tab strip associated with this auto hide panel</summary>
			public TabStrip TabStrip
			{
				get { return Root.DockPane(EDockSite.Centre).TabStrip; }
			}

			/// <summary>The site that this auto hide panel hides to</summary>
			public EDockSite DockSite { get; private set; }

			/// <summary>
			/// Get/Set the active content on this floating window. This will cause the pane that the content is on to also become active.
			/// To change the active content in a pane without making the pane active, assign to the pane's ActiveContent property</summary>
			public DockControl ActiveContent
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

			/// <summary>Get/Set the popped out state of the auto hide panel</summary>
			public bool PoppedOut
			{
				get { return m_popped_out; }
				set
				{
					if (m_popped_out == value) return;
					m_popped_out = value;

					// Show/Hide the panel
					Visibility = m_popped_out ? Visibility.Visible : Visibility.Collapsed;

					// When no longer popped out, make the last active content active again
					if (!m_popped_out)
						DockContainer.ActivatePrevious();
				}
			}
			private bool m_popped_out;

			/// <summary>Add a dockable instance to this auto hide panel. 'location' is ignored, all content is added to the centre site within an auto hide panel.</summary>
			public DockPane Add(IDockable dockable, int index, params EDockSite[] location)
			{
				if (dockable?.DockControl == null)
					throw new ArgumentNullException(nameof(dockable), "'dockable' or 'dockable.DockControl' cannot be 'null'");

				return Root.Add(dockable.DockControl, index, EDockSite.Centre);
			}
			public DockPane Add(IDockable dockable)
			{
				return Add(dockable, int.MaxValue);
			}

#if false
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
#endif
		}

		/// <summary>Handles all docking operations</summary>
		internal class DragHandler : Window, IDisposable
		{
			// Notes:
			// This works by displaying an invisible modal window while the drag operation is in process.
			// The modal window ensures the rest of the UI is disabled for the duration of the drag.
			// The modal window spawns other non-interactive windows that provide the overlays for the drop targets.
			private static readonly Vector DraggeeOfs = new Vector(-50, -30);
			private static readonly Rect DraggeeRect = new Rect(0, 0, 150, 150);
			private TabButton m_ghost_button;
			private Point m_ss_start_pt;

			public DragHandler(DockContainer owner, object item, Point ss_start_pt)
			{
				var item_name =
					item is DockPane p ? p.Name :
					item is DockControl c ? c.TabText :
					throw new Exception("Only panes and content should be being dragged");

				m_ss_start_pt = ss_start_pt;
				m_ghost_button = new TabButton(item_name, owner.Options);
				Owner = GetWindow(owner);
				DockContainer = owner;
				DraggedItem = item;
				TreeHost = null;
				DropAddress = new EDockSite[0];
				DropIndex = null;

				// Hide all auto hide panels, since they are not valid drop targets
				DockContainer.AutoHidePanels.ForEach(ah => ah.PoppedOut = false);

				// Create an invisible modal dialog
				Title = "Drop Handler";
				ShowInTaskbar = false;
				WindowStyle = WindowStyle.None;
				ResizeMode = ResizeMode.NoResize;
				Left = 0;
				Top = 0;
				Width = 0;
				Height = 0;
			}
			public void Dispose()
			{
				SetHoveredPane(null);
				Ghost = null;
				IndCrossLg = null;
				IndCrossSm = null;
				IndLeft = null;
				IndTop = null;
				IndRight = null;
				IndBottom = null;
			}
			protected override void OnSourceInitialized(EventArgs e)
			{
				base.OnSourceInitialized(e);

				// Set the positions of the edge docking indicators
				var distance_from_edge = 15;
				var loc_ghost = Point.Add(m_ss_start_pt, DraggeeOfs);
				var loc_top = DockContainer.PointToScreen(new Point((DockContainer.Width - DimensionsFor(EIndicator.dock_site_top).Width) / 2, distance_from_edge));
				var loc_left = DockContainer.PointToScreen(new Point(distance_from_edge, (DockContainer.Height - DimensionsFor(EIndicator.dock_site_left).Height) / 2));
				var loc_right = DockContainer.PointToScreen(new Point(DockContainer.Width - distance_from_edge - DimensionsFor(EIndicator.dock_site_right).Width, (DockContainer.Height - DimensionsFor(EIndicator.dock_site_right).Height) / 2));
				var loc_bottom = DockContainer.PointToScreen(new Point((DockContainer.Width - DimensionsFor(EIndicator.dock_site_bottom).Width) / 2, DockContainer.Height - distance_from_edge - DimensionsFor(EIndicator.dock_site_bottom).Height));

				// Create the semi-transparent non-modal window for the dragged item
				Ghost = new GhostPane(this, DraggedItem, loc_ghost) { Visibility = Visibility.Visible };

				// Create the dock site indicators
				IndTop = new Indicator(this, EIndicator.dock_site_top, DockContainer.AutoHidePanels[EDockSite.Top].Root.DockPane(EDockSite.Centre), loc_top) { Visibility = Visibility.Visible };
				IndLeft = new Indicator(this, EIndicator.dock_site_left, DockContainer.AutoHidePanels[EDockSite.Left].Root.DockPane(EDockSite.Centre), loc_left) { Visibility = Visibility.Visible };
				IndRight = new Indicator(this, EIndicator.dock_site_right, DockContainer.AutoHidePanels[EDockSite.Right].Root.DockPane(EDockSite.Centre), loc_right) { Visibility = Visibility.Visible };
				IndBottom = new Indicator(this, EIndicator.dock_site_bottom, DockContainer.AutoHidePanels[EDockSite.Bottom].Root.DockPane(EDockSite.Centre), loc_bottom) { Visibility = Visibility.Visible };
				IndCrossLg = new Indicator(this, EIndicator.dock_site_cross_lg) { Visibility = Visibility.Collapsed };
				IndCrossSm = new Indicator(this, EIndicator.dock_site_cross_sm) { Visibility = Visibility.Collapsed };

				// The dock handler deals with the mouse
				CaptureMouse();
			}
			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				HitTestDropLocations(PointToScreen(e.GetPosition(this)));
			}
			protected override void OnMouseUp(MouseButtonEventArgs e)
			{
				base.OnMouseUp(e);
				HitTestDropLocations(PointToScreen(e.GetPosition(this)));
				//DialogResult = true;
				Close();
			}
			protected override void OnLostMouseCapture(MouseEventArgs e)
			{
				base.OnLostMouseCapture(e);
				Close();
			}
			protected override void OnKeyDown(KeyEventArgs e)
			{
				base.OnKeyDown(e);
				if (e.Key == Key.Escape)
				{
					DialogResult = false;
					Close();
				}
			}
			protected override void OnClosed(EventArgs e)
			{
				Ghost = null;
				IndCrossLg = null;
				IndCrossSm = null;
				IndLeft = null;
				IndTop = null;
				IndRight = null;
				IndBottom = null;
				SetHoveredPane(null);
				ReleaseMouseCapture();
				DockContainer.Focus();

				// Commit the move on successful close
				if (DialogResult == true)
				{
					// Preserve the active content
					var active = DockContainer.ActiveContent;

					// No address means float in a new floating window
					if (DropAddress.Length == 0)
					{
						// Float the dragged content
						var dc = DraggedItem as DockControl;
						if (dc != null)
						{
							dc.IsFloating = true;
						}

						// Or, float the dragged dock pane
						if (DraggedItem is DockPane dockpane)
						{
							dc = dockpane.VisibleContent;
							dockpane.IsFloating = true;
						}

						// Set the location of the floating window to the last position of the ghost
						if (dc?.TreeHost is FloatingWindow fw)
						{
							fw.Left = Ghost.Left;
							fw.Top = Ghost.Top;
						}
					}

					// Otherwise dock the dragged item at the dock address
					else
					{
						var target = TreeHost.Root.DockPane(DropAddress.First(), DropAddress.Skip(1));
						var index = DropIndex ?? target.Content.Count;

						// Dock the dragged item
						if (DraggedItem is DockControl dc)
						{
							TreeHost.Add(dc.Dockable, index, DropAddress);
						}

						// Dock the dragged pane
						if (DraggedItem is DockPane dockpane)
						{
							foreach (var c in dockpane.Content.Reversed().ToArray())
								target.Content.Insert(index, c);
						}
					}

					// Restore the active content
					DockContainer.ActiveContent = active;
				}
				base.OnClosed(e);
			}

			/// <summary>The dock container that created this drop handler</summary>
			private DockContainer DockContainer { get; set; }

			/// <summary>The item being dragged (either a DockPane or IDockable)</summary>
			private object DraggedItem { get; set; }

			/// <summary>The tree to drop into</summary>
			private ITreeHost TreeHost { get; set; }

			/// <summary>Where to dock the dropped item. If empty drop in a new floating window</summary>
			private EDockSite[] DropAddress { get; set; }

			/// <summary>The index position of where to drop within a pane</summary>
			private int? DropIndex { get; set; }

			/// <summary>A form used as graphics to show dragged items</summary>
			private GhostPane Ghost
			{
				get { return m_ghost; }
				set
				{
					if (m_ghost == value) return;
					m_ghost?.Close();
					m_ghost = value;
				}
			}
			private GhostPane m_ghost;

			/// <summary>The cross of dock site locations displayed within the centre of a pane</summary>
			private Indicator IndCrossLg
			{
				get { return m_cross_lg; }
				set
				{
					if (m_cross_lg == value) return;
					m_cross_lg?.Close();
					m_cross_lg = value;
				}
			}
			private Indicator m_cross_lg;

			/// <summary>The small cross of dock site locations displayed within the centre of a pane</summary>
			private Indicator IndCrossSm
			{
				get { return m_cross_sm; }
				set
				{
					if (m_cross_sm == value) return;
					m_cross_sm?.Close();
					m_cross_sm = value;
				}
			}
			private Indicator m_cross_sm;

			/// <summary>The left edge dock site indicator</summary>
			private Indicator IndLeft
			{
				get { return m_left; }
				set
				{
					if (m_left == value) return;
					m_left?.Close();
					m_left = value;
				}
			}
			private Indicator m_left;

			/// <summary>The top edge dock site indicator</summary>
			private Indicator IndTop
			{
				get { return m_top; }
				set
				{
					if (m_top == value) return;
					m_top?.Close();
					m_top = value;
				}
			}
			private Indicator m_top;

			/// <summary>The left edge dock site indicator</summary>
			private Indicator IndRight
			{
				get { return m_right; }
				set
				{
					if (m_right == value) return;
					m_right?.Close();
					m_right = value;
				}
			}
			private Indicator m_right;

			/// <summary>The left edge dock site indicator</summary>
			private Indicator IndBottom
			{
				get { return m_bottom; }
				set
				{
					if (m_bottom == value) return;
					m_bottom?.Close();
					m_bottom = value;
				}
			}
			private Indicator m_bottom;

			/// <summary>The pane in the position that the dropped item should dock to</summary>
			private void SetHoveredPane(DockPane pane, int? index = null)
			{
				if (m_hovered_pane != null)
				{
					m_hovered_pane.TabStrip.Buttons.Remove(m_ghost_button);
				}
				m_hovered_pane = pane;
				if (m_hovered_pane != null)
				{
					var idx = index ?? m_hovered_pane.TabStrip.Buttons.Count;
					m_hovered_pane.TabStrip.Buttons.Insert(idx, m_ghost_button);
				}
			}
			private DockPane m_hovered_pane;

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

			/// <summary>Test the location 'screen_pt' as a possible drop location, and update the ghost and indicators</summary>
			private void HitTestDropLocations(Point screen_pt)
			{
				var pane = (DockPane)null;
				var snap_to = (EDropSite?)null;
				var index = (int?)null;
				var over_indicator = false;

				// Look for an indicator that the mouse is over. Do this before updating the indicator
				// positions because if the mouse is over an indicator, we don't want to move it.
				foreach (var ind in Indicators)
				{
					if (!ind.IsVisible)
						continue;

					// Get the mouse point in indicator space and test if it's within the indicator regions
					var pt = ind.PointFromScreen(screen_pt);
					snap_to = ind.CheckSnapTo(pt);
					if (snap_to == null)
						continue;

					// The mouse is over an indicator, get the associated pane
					over_indicator = true;
					pane = ind.DockPane;
					break;
				}

				// Look for the dock pane under the mouse
				if (pane == null)
				{
					foreach (var tree in DockContainer.AllTreeHosts)
					{
						var hit = VisualTreeHelper.HitTest(tree.Root, tree.Root.PointFromScreen(screen_pt));
						pane = hit != null ? hit.VisualHit as DockPane ?? Gui_.FindVisualParent<DockPane>(hit.VisualHit, root: tree.Root) : null;
						if (pane != null) break;
					}
				}

				// Check whether the mouse is over the tab strip of the dock pane
				if (pane != null && !over_indicator)
				{
					var hit = VisualTreeHelper.HitTest(pane.TabStrip, pane.TabStrip.PointFromScreen(screen_pt));
					var tab = hit != null ? hit.VisualHit as TabButton ?? Gui_.FindVisualParent<TabButton>(hit.VisualHit, root: pane.TabStrip) : null;
					if (tab != null)
					{
						snap_to = EDropSite.PaneCentre;
						index = pane.TabStrip.Children.IndexOf(tab);
					}
				}

				// Check whether the mouse is over the title bar of the dock pane
				if (pane != null && !over_indicator)
				{
					var hit = VisualTreeHelper.HitTest(pane.TitleBar, pane.TitleBar.PointFromScreen(screen_pt));
					var title = hit != null ? hit.VisualHit as TitleBar ?? Gui_.FindVisualParent<TitleBar>(hit.VisualHit, root: pane.TitleBar) : null;
					if (title != null)
						snap_to = EDropSite.PaneCentre;
				}

				// Determine the drop address
				if (pane == null || snap_to == null)
				{
					// No pane or snap-to means float in a new window
					DropAddress = new EDockSite[0];
					TreeHost = null;
				}
				else
				{
					var snap = snap_to.Value;
					var ds = (EDockSite)(snap & EDropSite.DockSiteMask);
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
						var branch = pane.ParentBranch;
						var address = branch.DockAddress.ToList();

						// While the child at 'ds' is a branch, append 'EDockSite.Centre's to the address
						for (; branch.Descendants[ds].Item is Branch b; branch = b, ds = EDockSite.Centre) { address.Add(ds); }
						address.Add(ds);

						DropAddress = address.ToArray();
					}
					else if (snap.HasFlag(EDropSite.Root))
					{
						// Snap to an edge dock site at the root branch level
						var branch = pane.RootBranch;
						var address = branch.DockAddress.ToList();

						// While the child at 'ds' is a branch, append 'EDockSite.Centre's to the address
						for (; branch.Descendants[ds].Item is Branch b; branch = b, ds = EDockSite.Centre) { address.Add(ds); }
						address.Add(ds);

						DropAddress = address.ToArray();
					}

					// Drop in the same tree as 'pane'
					TreeHost = pane.TreeHost;
					DropIndex = index;
				}

				// Update the position of the ghost
				PositionGhost(screen_pt);

				// If the mouse isn't over an indicator, update the positions of the indicators
				if (!over_indicator)
					PositionCrossIndicator(pane);
			}

			/// <summary>Position the ghost window at the current drop address</summary>
			public void PositionGhost(Point screen_pt)
			{
				// Bounds in screen space covering the whole pane.
				// Clip in window space, clipping out the title, tab strip etc
				var bounds = Rect.Empty;
				var clip = Geometry.Empty;

				// No address means floating
				if (TreeHost == null || DropAddress.Length == 0)
				{
					bounds = new Rect(Point.Add(screen_pt, DraggeeOfs), DraggeeRect.Size);
					clip = new RectangleGeometry(DraggeeRect);
				}
				else
				{
					// Navigate as far down the tree as we can for the drop address.
					// The last dock site in the address should be a null child or a dock pane.
					// The second to last dock site should be a branch or a pane.
					var branch = TreeHost.Root;
					var ds = DropAddress.GetEnumerator();
					for (var at_end = ds.MoveNext(); !at_end && branch.Descendants[(EDockSite)ds.Current].Item is Branch b; branch = b, at_end = ds.MoveNext()) { }
					var target = branch.Descendants[(EDockSite)ds.Current];

					// If there is a pane at the target position then snap to fill the pane or some child area within the pane.
					if (target.Item is DockPane pane)
					{
						// Update the pane being hovered
						SetHoveredPane(pane, DropIndex);

						// If there is one more dock site in the address, then the drop target is a child area within the content area of 'pane'.
						if (ds.MoveNext())
						{
							var rect = DockContainer.DockSiteBounds((EDockSite)ds.Current, pane.Centre.RenderArea(pane), (EDockMask)(1 << (int)ds.Current), DockSizeData.Halves);
							bounds = pane.RectToScreen(pane.RenderArea());
							clip = new RectangleGeometry(rect);
						}
						// Otherwise, the target area is the pane itself.
						else
						{
							var rect = pane.RenderArea();
							bounds = pane.RectToScreen(rect);

							// Limit the shape of the ghost to the pane content area
							clip = new RectangleGeometry(rect);
							clip = Geometry.Combine(clip, new RectangleGeometry(pane.TitleBar.RenderArea(pane)), GeometryCombineMode.Exclude, Transform.Identity);
							clip = Geometry.Combine(clip, new RectangleGeometry(pane.TabStrip.RenderArea(pane)), GeometryCombineMode.Exclude, Transform.Identity);

							// If a tab on the dock pane is being hovered, include that area in the ghost form's region
							if (DropIndex != null)
							{
								//								var strip = pane.TabStrip;
								//								var visible_index = Math.Min(DropIndex, strip.TabCount - 1) - strip.FirstVisibleTabIndex;
								//								var ghost_tab = strip.VisibleTabs.Skip(visible_index).FirstOrDefault();
								//								if (ghost_tab != null)
								//								{
								//									// Include the area of the ghost tab
								//									var b = strip.Transform.TransformRect(ghost_tab.Bounds);
								//									var r = pane.RectangleToClient(strip.RectangleToScreen(b));
								//									region.Union(r);
								//								}
							}
						}
					}
					else if (target.Item is Branch)
					{
						throw new Exception("The address ends at a branch node, not a pane or null-child");
					}
					else
					{
						// If the target is empty, then just fill the whole target area
						var docksite = DropAddress.Back();

						var area = branch.DockPane(docksite).RenderArea();
						var rect = DockContainer.DockSiteBounds(docksite, area, branch.DockedMask | (EDockMask)(1 << (int)docksite), branch.DockSizes);
						bounds = branch.RectToScreen(rect);
						clip = new RectangleGeometry(rect);
					}
				}

				// Update the bounds and region of the ghost
				Ghost.Left = bounds.X;
				Ghost.Top = bounds.Y;
				Ghost.Width = bounds.Width;
				Ghost.Height = bounds.Height;
				Ghost.Clip = clip;
			}

			/// <summary>Update the positions of the indicators. 'pane' is the pane under the mouse, or null</summary>
			private void PositionCrossIndicator(DockPane pane)
			{
				// Update the pane associated with the indicator
				IndCrossLg.DockPane = pane;
				IndCrossSm.DockPane = pane;

				// Update the position of the cross indicators
				// Auto hide panels are not valid drop targets, there are special indicators for dropping onto an auto hide pane.
				if (pane == null)
				{
					IndCrossLg.Visibility = Visibility.Collapsed;
					IndCrossSm.Visibility = Visibility.Collapsed;
					return;
				}

				// Display the large cross indicator when over the Centre site
				// or the small cross indicator when over an edge site
				if (pane.DockSite == EDockSite.Centre)
				{
					var pt = Point.Subtract(Gui_.Centre(pane.Centre.RenderArea(pane)), new Vector(IndCrossLg.Width * 0.5, IndCrossLg.Height * 0.5));
					IndCrossLg.SetLocation(pane.PointToScreen(pt));

					IndCrossLg.Visibility = Visibility.Visible;
					IndCrossSm.Visibility = Visibility.Collapsed;
				}
				else
				{
					var pt = Point.Subtract(Gui_.Centre(pane.Centre.RenderArea(pane)), new Vector(IndCrossSm.Width * 0.5, IndCrossSm.Height * 0.5));
					IndCrossSm.SetLocation(pane.PointToScreen(pt));

					IndCrossLg.Visibility = Visibility.Collapsed;
					IndCrossSm.Visibility = Visibility.Visible;
				}
			}

			/// <summary>Return the hotspots for the given indicator</summary>
			private static Size DimensionsFor(EIndicator indy)
			{
				// These are the expected dimensions of the indicators in WPF virtual pixels
				// On high DPI screens the bitmaps will be scaled to these sizes.
				switch (indy)
				{
				default: throw new Exception("Unknown indicator type");
				case EIndicator.dock_site_cross_lg: return new Size(128, 128);
				case EIndicator.dock_site_cross_sm: return new Size(64, 64);
				case EIndicator.dock_site_left: return new Size(25, 25);
				case EIndicator.dock_site_top: return new Size(25, 25);
				case EIndicator.dock_site_right: return new Size(25, 25);
				case EIndicator.dock_site_bottom: return new Size(25, 25);
				}
			}

			/// <summary>Return the hotspots for the given indicator</summary>
			private static Hotspot[] HotSpotsFor(EIndicator indy)
			{
				// Set the hotspot locations
				switch (indy)
				{
				default:
					{
						throw new Exception("Unknown indicator type");
					}
				case EIndicator.dock_site_cross_lg:
					{
						double a = 32, b = 50, c = 77, d = 95, cx = c - b;
						return new Hotspot[]
						{
								new Hotspot(new RectangleGeometry(new Rect(b,b,cx,cx)), EDropSite.PaneCentre),
								new Hotspot(Gui_.MakePolygonGeometry(a,a, b,b, b,c, a,d), EDropSite.PaneLeft),
								new Hotspot(Gui_.MakePolygonGeometry(d,d, c,c, c,b, d,a), EDropSite.PaneRight),
								new Hotspot(Gui_.MakePolygonGeometry(a,a, d,a, c,b, b,b), EDropSite.PaneTop),
								new Hotspot(Gui_.MakePolygonGeometry(d,d, a,d, b,c, c,c), EDropSite.PaneBottom),
								new Hotspot(new RectangleGeometry(new Rect(0,a,32,64)), EDropSite.BranchLeft),
								new Hotspot(new RectangleGeometry(new Rect(d,a,32,64)), EDropSite.BranchRight),
								new Hotspot(new RectangleGeometry(new Rect(a,0,64,32)), EDropSite.BranchTop),
								new Hotspot(new RectangleGeometry(new Rect(a,d,64,32)), EDropSite.BranchBottom),
						};
					}
				case EIndicator.dock_site_cross_sm:
					{
						double a = 0, b = 18, c = 45, d = 63, cx = c - b;
						return new Hotspot[]
						{
								new Hotspot(new RectangleGeometry(new Rect(b,b,cx,cx)), EDropSite.PaneCentre),
								new Hotspot(Gui_.MakePolygonGeometry(a,a, b,b, b,c, a,d), EDropSite.PaneLeft),
								new Hotspot(Gui_.MakePolygonGeometry(d,d, c,c, c,b, d,a), EDropSite.PaneRight),
								new Hotspot(Gui_.MakePolygonGeometry(a,a, d,a, c,b, b,b), EDropSite.PaneTop),
								new Hotspot(Gui_.MakePolygonGeometry(d,d, a,d, b,c, c,c), EDropSite.PaneBottom),
						};
					}
				case EIndicator.dock_site_left:
					{
						return new Hotspot[] { new Hotspot(new RectangleGeometry(new Rect(0, 0, 25, 25)), EDropSite.RootLeft) };
					}
				case EIndicator.dock_site_top:
					{
						return new Hotspot[] { new Hotspot(new RectangleGeometry(new Rect(0, 0, 25, 25)), EDropSite.RootTop) };
					}
				case EIndicator.dock_site_right:
					{
						return new Hotspot[] { new Hotspot(new RectangleGeometry(new Rect(0, 0, 25, 25)), EDropSite.RootRight) };
					}
				case EIndicator.dock_site_bottom:
					{
						return new Hotspot[] { new Hotspot(new RectangleGeometry(new Rect(0, 0, 25, 25)), EDropSite.RootBottom) };
					}
				}
			}

			/// <summary>Return the clipping region for the given indicator</summary>
			private static Geometry ClipFor(EIndicator indy)
			{
				switch (indy)
				{
				default:
					{
						throw new Exception("Unknown indicator type");
					}
				case EIndicator.dock_site_cross_lg:
					{
						var clip = Geometry.Empty;
						clip = Geometry.Combine(clip, new RectangleGeometry(new Rect(33, 43, 62, 42)), GeometryCombineMode.Union, Transform.Identity);
						clip = Geometry.Combine(clip, new RectangleGeometry(new Rect(43, 33, 42, 62)), GeometryCombineMode.Union, Transform.Identity);
						clip = Geometry.Combine(clip, new RectangleGeometry(new Rect(0, 39, 25, 50)), GeometryCombineMode.Union, Transform.Identity);
						clip = Geometry.Combine(clip, new RectangleGeometry(new Rect(103, 39, 25, 50)), GeometryCombineMode.Union, Transform.Identity);
						clip = Geometry.Combine(clip, new RectangleGeometry(new Rect(39, 0, 50, 25)), GeometryCombineMode.Union, Transform.Identity);
						clip = Geometry.Combine(clip, new RectangleGeometry(new Rect(39, 103, 50, 25)), GeometryCombineMode.Union, Transform.Identity);
						return clip;
					}
				case EIndicator.dock_site_cross_sm:
					{
						var clip = Geometry.Empty;
						clip = Geometry.Combine(clip, new RectangleGeometry(new Rect(1, 11, 62, 42)), GeometryCombineMode.Union, Transform.Identity);
						clip = Geometry.Combine(clip, new RectangleGeometry(new Rect(11, 1, 42, 62)), GeometryCombineMode.Union, Transform.Identity);
						return clip;
					}
				case EIndicator.dock_site_left:
					{
						return new RectangleGeometry(new Rect(0, 0, 25, 25));
					}
				case EIndicator.dock_site_top:
					{
						return new RectangleGeometry(new Rect(0, 0, 25, 25));
					}
				case EIndicator.dock_site_right:
					{
						return new RectangleGeometry(new Rect(0, 0, 25, 25));
					}
				case EIndicator.dock_site_bottom:
					{
						return new RectangleGeometry(new Rect(0, 0, 25, 25));
					}
				}
			}

			/// <summary>Base class for windows used as indicator graphics</summary>
			private class GhostBase : Window
			{
				public GhostBase(Window owner)
				{
					Owner = owner;
					ShowInTaskbar = false;
					ShowActivated = false;
					AllowsTransparency = true;
					WindowStartupLocation = WindowStartupLocation.Manual;
					WindowStyle = WindowStyle.None;
					ResizeMode = ResizeMode.NoResize;
					Visibility = Visibility.Collapsed;
					Background = Brushes.Black;
					Opacity = 0.5f;
				}
			}

			/// <summary>A window that acts as a graphic while a pane or content is being dragged</summary>
			[DebuggerDisplay("{Name}")]
			private class GhostPane : GhostBase
			{
				public GhostPane(Window owner, object item, Point loc)
					: base(owner)
				{
					Name = "Ghost";
					Background = SystemColors.ActiveCaptionBrush;
					Left = loc.X;
					Top = loc.Y;
					Width = DraggeeRect.Width;
					Height = DraggeeRect.Height;
				}
			}

			/// <summary>A form that displays a docking site indicator</summary>
			[DebuggerDisplay("{Name}")]
			private class Indicator : GhostBase
			{
				private Hotspot[] m_spots;

				public Indicator(Window owner, EIndicator indy, DockPane pane = null, Point? loc = null)
					: base(owner)
				{
					Name = indy.ToString();
					DockPane = pane;

					if (loc != null)
					{
						Left = loc.Value.X;
						Top = loc.Value.Y;
					}
					var sz = DimensionsFor(indy);
					Width = sz.Width;
					Height = sz.Height;

					// Add an image UI element
					AddChild(new Image { Source = (ImageSource)Resources[indy.ToString()], Width = sz.Width, Height = sz.Height });

					// Add the hotspots for this indicator
					m_spots = HotSpotsFor(indy);

					// Set the clip for the window
					Clip = ClipFor(indy);
				}

				/// <summary>Test 'pt' against the hotspots on this indicator</summary>
				public EDropSite? CheckSnapTo(Point pt)
				{
					return m_spots.FirstOrDefault(x => x.Area.FillContains(pt))?.DropSite;
				}

				/// <summary>The dock pane associated with this indicator</summary>
				public DockPane DockPane { get; set; }
			}

			/// <summary>A region within the indicate that corresponds to a dock site location</summary>
			private class Hotspot
			{
				public Hotspot(Geometry area, EDropSite ds)
				{
					Area = area;
					DropSite = ds;
				}

				/// <summary>The area in the indicator that maps to 'DockSite'</summary>
				public Geometry Area;

				/// <summary>The drop site that this hot spot corresponds to</summary>
				public EDropSite DropSite;
			}

			/// <summary>Indicator types</summary>
			private enum EIndicator
			{
				dock_site_cross_lg,
				dock_site_cross_sm,
				dock_site_left,
				dock_site_top,
				dock_site_right,
				dock_site_bottom,
			}

			/// <summary>Places where panes/content can be dropped</summary>
			[Flags]
			private enum EDropSite
			{
				// Dock sites within the current pane
				Pane = 1 << 16,
				PaneCentre = Pane | EDockSite.Centre,
				PaneLeft = Pane | EDockSite.Left,
				PaneTop = Pane | EDockSite.Top,
				PaneRight = Pane | EDockSite.Right,
				PaneBottom = Pane | EDockSite.Bottom,

				// Dock sites within the current branch
				Branch = 1 << 17,
				BranchCentre = Branch | EDockSite.Centre,
				BranchLeft = Branch | EDockSite.Left,
				BranchTop = Branch | EDockSite.Top,
				BranchRight = Branch | EDockSite.Right,
				BranchBottom = Branch | EDockSite.Bottom,

				// Dock sites in the root level branch
				Root = 1 << 18,
				RootCentre = Root | EDockSite.Centre,
				RootLeft = Root | EDockSite.Left,
				RootTop = Root | EDockSite.Top,
				RootRight = Root | EDockSite.Right,
				RootBottom = Root | EDockSite.Bottom,

				// A mask the gets the EDockSite bits
				DockSiteMask = 0xff,
			}
		}

		/// <summary>General options for the dock container</summary>
		public class OptionData
		{
			public OptionData()
			{
				AllowUserDocking = true;
				DoubleClickTitleBarToDock = true;
				DoubleClickTabsToFloat = true;
				AlwaysShowTabs = false;
				ShowTitleBars = true;
				DragThresholdInPixels = 5;
			}
			public OptionData(XElement node)
			{
				AllowUserDocking = node.Element(nameof(AllowUserDocking)).As<bool>();
				DoubleClickTitleBarToDock = node.Element(nameof(DoubleClickTitleBarToDock)).As<bool>();
				DoubleClickTabsToFloat = node.Element(nameof(DoubleClickTabsToFloat)).As<bool>();
				AlwaysShowTabs = node.Element(nameof(AlwaysShowTabs)).As<bool>();
				ShowTitleBars = node.Element(nameof(ShowTitleBars)).As<bool>();
				DragThresholdInPixels = node.Element(nameof(DragThresholdInPixels)).As<double>();
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(AllowUserDocking), AllowUserDocking, false);
				node.Add2(nameof(DoubleClickTitleBarToDock), DoubleClickTitleBarToDock, false);
				node.Add2(nameof(DoubleClickTabsToFloat), DoubleClickTabsToFloat, false);
				node.Add2(nameof(AlwaysShowTabs), AlwaysShowTabs, false);
				node.Add2(nameof(ShowTitleBars), ShowTitleBars, false);
				node.Add2(nameof(DragThresholdInPixels), DragThresholdInPixels, false);
				return node;
			}

			/// <summary>Get/Set whether the user is allowed to drag and drop panes</summary>
			public bool AllowUserDocking { get; set; }

			/// <summary>Get/Set whether double clicking on the title bar of a floating window returns its contents to the dock container</summary>
			public bool DoubleClickTitleBarToDock { get; set; }

			/// <summary>Get/Set whether double clicking on a tab in the tab strip causes the content to move to/from a floating window</summary>
			public bool DoubleClickTabsToFloat { get; set; }

			/// <summary>True to always show the tabs, even if there's only one</summary>
			public bool AlwaysShowTabs { get; set; }

			/// <summary>Show/Hide all title bars</summary>
			public bool ShowTitleBars { get; set; }

			/// <summary>The minimum distance the mouse must move before a drag operation starts</summary>
			public double DragThresholdInPixels { get; set; }
		}

		/// <summary>A record of a dock location within the dock container</summary>
		public class DockLocation
		{
			public DockLocation(EDockSite[] address = null, int index = int.MaxValue, EDockSite? auto_hide = null, int? float_window_id = null)
			{
				Address = address ?? new[] { EDockSite.Centre };
				Index = index;
				AutoHide = auto_hide;
				FloatingWindowId = float_window_id;
			}
			public DockLocation(XElement node)
			{
				Address = node.Element(XmlTag.Address).As<string>().Split(',').Select(x => Enum<EDockSite>.Parse(x)).ToArray();
				Index = node.Element(XmlTag.Index).As(int.MaxValue);
				AutoHide = node.Element(XmlTag.AutoHide).As((EDockSite?)null);
				FloatingWindowId = node.Element(XmlTag.FloatingWindow).As((int?)null);
			}
			public XElement ToXml(XElement node)
			{
				// Add info about the host of this content
				if (FloatingWindowId != null)
				{
					// If hosted in a floating window, record the window id
					node.Add2(XmlTag.FloatingWindow, FloatingWindowId.Value, false);
				}
				else if (AutoHide != null)
				{
					// If hosted in an auto hide panel, record the panel id
					node.Add2(XmlTag.AutoHide, AutoHide.Value, false);
				}
				else
				{
					// Otherwise we're hosted in the main dock container by default
				}

				// Add the location of where in the host this content is stored
				node.Add2(XmlTag.Address, string.Join(",", Address), false);
				node.Add2(XmlTag.Index, Index, false);
				return node;
			}

			/// <summary>The location within the host's tree</summary>
			public EDockSite[] Address { get; set; }

			/// <summary>The index within the content of the dock pace at 'Address'</summary>
			public int Index { get; set; }

			/// <summary>The auto hide site (or null if not in an auto site location)</summary>
			public EDockSite? AutoHide { get; set; }

			/// <summary>The Id of a floating window to dock to (or null if not in a floating window)</summary>
			public int? FloatingWindowId { get; set; }

			/// <summary></summary>
			public override string ToString()
			{
				var addr = string.Join(",", Address);
				if (FloatingWindowId != null) return $"Floating Window {FloatingWindowId.Value}: {addr} (Index:{Index})";
				if (AutoHide != null) return $"Auto Hide {AutoHide.Value}: {addr} (Index:{Index})";
				return $"Dock Container: {addr} (Index:{Index})";
			}

			/// <summary>Implicit conversion from array of dock sites to a doc location</summary>
			public static implicit operator DockLocation(EDockSite[] site)
			{
				return new DockLocation(address: site);
			}
		}

		/// <summary>Records the proportions used for each dock site at a branch level</summary>
		public class DockSizeData
		{
			/// <summary>The minimum size of a child control</summary>
			public const int MinChildSize = 20;

			public DockSizeData(double left = 0.25, double top = 0.25, double right = 0.25, double bottom = 0.25)
			{
				Left = left;
				Top = top;
				Right = right;
				Bottom = bottom;
			}
			public DockSizeData(DockSizeData rhs)
				: this(rhs.Left, rhs.Top, rhs.Right, rhs.Bottom)
			{ }
			public DockSizeData(XElement node)
			{
				Left = node.Element(nameof(Left)).As(Left);
				Top = node.Element(nameof(Top)).As(Top);
				Right = node.Element(nameof(Right)).As(Right);
				Bottom = node.Element(nameof(Bottom)).As(Bottom);
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

			/// <summary>The branch associated with this sizes</summary>
			internal Branch Owner { get; set; }

			/// <summary>
			/// The size of the left, top, right, bottom panes. If >= 1, then the value is interpreted
			/// as pixels, if less than 1 then interpreted as a fraction of the ClientRectangle width/height</summary>
			public double Left
			{
				get { return m_left; }
				set { SetProp(ref m_left, value); }
			}
			private double m_left;
			public double Top
			{
				get { return m_top; }
				set { SetProp(ref m_top, value); }
			}
			private double m_top;
			public double Right
			{
				get { return m_right; }
				set { SetProp(ref m_right, value); }
			}
			private double m_right;
			public double Bottom
			{
				get { return m_bottom; }
				set { SetProp(ref m_bottom, value); }
			}
			private double m_bottom;

			/// <summary>Update a property and raise an event if different</summary>
			private void SetProp(ref double prop, double value)
			{
				if (Equals(prop, value)) return;
				Debug.Assert(value >= 0);
				prop = value;
			}

			/// <summary>Get/Set the dock site size by EDockSite</summary>
			public double this[EDockSite ds]
			{
				get
				{
					switch (ds)
					{
					case EDockSite.Left: return Left;
					case EDockSite.Right: return Right;
					case EDockSite.Top: return Top;
					case EDockSite.Bottom: return Bottom;
					}
					return 1f;
				}
				set
				{
					switch (ds)
					{
					case EDockSite.Left: Left = value; break;
					case EDockSite.Right: Right = value; break;
					case EDockSite.Top: Top = value; break;
					case EDockSite.Bottom: Bottom = value; break;
					}
				}
			}

			/// <summary>Get the sizes for the edge dock sites assuming the given available area</summary>
			internal Thickness GetSizesForRect(Rect rect, EDockMask docked_mask, Size? centre_min_size = null)
			{
				// Ensure the centre dock pane is never fully obscured
				var csize = centre_min_size ?? Size.Empty;
				if (csize.Width == 0) csize.Width = MinChildSize;
				if (csize.Height == 0) csize.Height = MinChildSize;

				// See which dock sites actually have something docked
				var existsL = docked_mask.HasFlag(EDockMask.Left);
				var existsR = docked_mask.HasFlag(EDockMask.Right);
				var existsT = docked_mask.HasFlag(EDockMask.Top);
				var existsB = docked_mask.HasFlag(EDockMask.Bottom);

				// Get the size of each area
				var l = existsL ? (Left >= 1f ? Left : rect.Width * Left) : 0.0;
				var r = existsR ? (Right >= 1f ? Right : rect.Width * Right) : 0.0;
				var t = existsT ? (Top >= 1f ? Top : rect.Height * Top) : 0.0;
				var b = existsB ? (Bottom >= 1f ? Bottom : rect.Height * Bottom) : 0.0;

				// If opposite zones overlap, reduce the sizes
				var over_w = rect.Width - (l + csize.Width + r);
				var over_h = rect.Height - (t + csize.Height + b);
				if (over_w < 0)
				{
					if (existsL && existsR) { l += over_w * 0.5; r += (over_w + 1) * 0.5; }
					else if (existsL) { l = rect.Width - csize.Width; }
					else if (existsR) { r = rect.Width - csize.Width; }
				}
				if (over_h < 0)
				{
					if (existsT && existsB) { t += over_h * 0.5; b += (over_h + 1) * 0.5; }
					else if (existsT) { t = rect.Height - csize.Height; }
					else if (existsB) { b = rect.Height - csize.Height; }
				}

				// Return the sizes in pixels
				return new Thickness(Math.Max(0, l), Math.Max(0, t), Math.Max(0, r), Math.Max(0, b));
			}

			/// <summary>Get the size for a dock site in pixels, assuming an available area 'rect'</summary>
			internal double GetSize(EDockSite location, Rect rect, EDockMask docked_mask)
			{
				var area = GetSizesForRect(rect, docked_mask);

				// Return the size of the requested site
				switch (location)
				{
				default: throw new Exception($"No size value for dock zone {location}");
				case EDockSite.Centre: return 0;
				case EDockSite.Left: return area.Left;
				case EDockSite.Right: return area.Right;
				case EDockSite.Top: return area.Top;
				case EDockSite.Bottom: return area.Bottom;
				}
			}

			/// <summary>Get the size for a dock site in pixels, assuming an available area 'rect'</summary>
			internal void SetSize(EDockSite location, Rect rect, int value)
			{
				// Assign a fractional value for the dock site size
				switch (location)
				{
				default: throw new Exception($"No size value for dock zone {location}");
				case EDockSite.Centre: break;
				case EDockSite.Left: Left = value / (Left >= 1 ? 1.0 : rect.Width); break;
				case EDockSite.Right: Right = value / (Right >= 1 ? 1.0 : rect.Width); break;
				case EDockSite.Top: Top = value / (Top >= 1 ? 1.0 : rect.Height); break;
				case EDockSite.Bottom: Bottom = value / (Bottom >= 1 ? 1.0 : rect.Height); break;
				}
			}

			public static DockSizeData Halves { get { return new DockSizeData(0.5, 0.5, 0.5, 0.5); } }
			public static DockSizeData Quarters { get { return new DockSizeData(0.25, 0.25, 0.25, 0.25); } }
		}

		/// <summary>The title bar for a dock pane</summary>
		public class TitleBar : Grid
		{
			private OptionData m_opts;
			private TextBlock m_title;
			private PinButton m_pin;
			private CloseButton m_close;

			public TitleBar(DockPane owner, OptionData opts)
			{
				m_opts = opts;
				DockPane = owner;
				Background = Brushes.LightSteelBlue;

				ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
				ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
				ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });

				// Add the Title text
				m_title = Children.Add2(new TextBlock { Text = "Title", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(3, 0, 0, 0) });
				Grid.SetColumn(m_title, 0);

				// Add the pin button
				m_pin = Children.Add2(new PinButton(DockPane));
				Grid.SetColumn(m_pin, 1);

				// Add the close button
				m_close = Children.Add2(new CloseButton(DockPane));
				Grid.SetColumn(m_close, 2);
			}
			protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
			{
				base.OnPreviewMouseDown(e);

				// If left click on the title bar and user docking is allowed, start a drag operation
				if (e.LeftButton == MouseButtonState.Pressed && m_opts.AllowUserDocking)
				{
					// Record the mouse down location.
					// Don't start a drag operation until the mouse has moved a bit.
					m_mouse_down_at = e.GetPosition(this);
					m_dragging = false;
				}
			}
			protected override void OnPreviewMouseMove(MouseEventArgs e)
			{
				base.OnPreviewMouseMove(e);

				// Start dragging when the mouse moves more than a minimum distance
				if (m_mouse_down_at != null && !m_dragging)
				{
					m_dragging |= Point.Subtract(m_mouse_down_at.Value, e.GetPosition(this)).Length > m_opts.DragThresholdInPixels;
					if (m_dragging)
						DockContainer.DragBegin(DockPane, PointToScreen(e.GetPosition(this)));
				}
			}
			protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
			{
				base.OnPreviewMouseUp(e);
				m_mouse_down_at = null;
				m_dragging = false;
			}
			private Point? m_mouse_down_at;
			private bool m_dragging;

			/// <summary>The DockPane that owns this title bar</summary>
			public DockPane DockPane { get; private set; }

			/// <summary>The title bar text</summary>
			public string Title
			{
				get { return m_title.Text; }
				set { m_title.Text = value ?? string.Empty; }
			}

			/// <summary>Show/Hide the pin button</summary>
			public bool ShowPin
			{
				get { return m_pin.Visibility == Visibility.Visible; }
				set { m_pin.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
			}

			/// <summary>Show/Hide the close button</summary>
			public bool ShowClose
			{
				get { return m_close.Visibility == Visibility.Visible; }
				set { m_close.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
			}
		}

		/// <summary>A button for moving a dock pane to/from an auto hide panel</summary>
		public class PinButton : Button
		{
			public PinButton(DockPane pane)
			{
				DockPane = pane;
				Content = IsPinned ? "P" : "d";
			}
			protected override void OnClick()
			{
				base.OnClick();
				IsPinned = !IsPinned;
			}

			/// <summary>The dock pane to be pinned/unpinned</summary>
			public DockPane DockPane { get; private set; }

			/// <summary>True when displaying the 'pinned' state</summary>
			public bool IsPinned
			{
				get { return !DockPane.IsAutoHide; }
				set { DockPane.IsAutoHide = !value; }
			}
		}

		/// <summary>A button for closing/hiding a dock pane</summary>
		public class CloseButton : Button
		{
			public CloseButton(DockPane pane)
			{
				DockPane = pane;
				Content = "X";
			}
			protected override void OnClick()
			{
				base.OnClick();
			}

			/// <summary>The dock pane to be pinned/unpinned</summary>
			public DockPane DockPane { get; private set; }

		}

		/// <summary>A strip of tab buttons</summary>
		[DebuggerDisplay("{StripLocation}")]
		public class TabStrip : StackPanel
		{
			protected OptionData m_opts;
			private Point? m_mouse_down_at;
			private bool m_dragging;
			private ToolTip m_tt;

			public TabStrip(EDockSite ds, OptionData opts)
			{
				// All tab strips are horizontal with a layout transform for vertical strips
				m_opts = opts;
				m_tt = new ToolTip { StaysOpen = true, HasDropShadow = true };
				Orientation = Orientation.Horizontal;
				Buttons = new ObservableCollection<TabButton>();
				StripLocation = ds;
			}
			protected override void OnMouseDown(MouseButtonEventArgs e)
			{
				base.OnMouseDown(e);

				// If left click on a tab and user docking is allowed, start a drag operation
				if (e.LeftButton == MouseButtonState.Pressed && m_opts.AllowUserDocking)
				{
					// Record the mouse down location
					m_mouse_down_at = e.GetPosition(this);
					m_dragging = false;
				}

				// Display a tab specific context menu on right click
				if (e.RightButton == MouseButtonState.Pressed)
				{
					//					var hit = HitTestTabButton(e.GetPosition(this));
					//					if (hit?.TabCMenu != null)
					//						hit.TabCMenu.Show(this, e.GetPosition(this));
				}
			}
			protected override void OnMouseMove(MouseEventArgs e)
			{
				var loc = e.GetPosition(this);
				m_dragging |= m_mouse_down_at != null && Point.Subtract(loc, m_mouse_down_at.Value).Length > 5;
				if (m_dragging)
				{
					// Begin dragging the content associated with the tab
					var hit = HitTestTabButton(m_mouse_down_at.Value);
					if (hit != null)
					{
						m_mouse_down_at = null;
						DockContainer.DragBegin(hit, PointToScreen(e.GetPosition(this)));
					}
				}

				// Check for the mouse hovering over a tab
				if (e.LeftButton == MouseButtonState.Released)
				{
					var hit = HitTestTabButton(loc);
					//					if (hit != m_hovered_tab)
					{
						//						m_hovered_tab = hit;
						//						var tt = string.Empty;
						//						if (hit != null)
						//						{
						//							// If the tool tip has been set explicitly, or the tab content is clipped, set the tool tip string
						//							var tabbtn = VisibleTabs.First(x => x.Content == hit);
						//							if (tabbtn.Clipped || !ReferenceEquals(hit.TabToolTip, hit.TabText))
						//								tt = hit.TabToolTip;
						//						}
						//						this.ToolTip(m_tt, tt);
					}
				}

				base.OnMouseMove(e);
			}
			//protected override void OnMouseUp(MouseButtonEventArgs e)
			//{
			//	// If the mouse is still captured, then treat this as a mouse click instead of a drag
			//	if (m_mouse_down_at != null)
			//	{
			//		// Find the tab that was clicked
			//		var hit = HitTestTabButton(m_mouse_down_at.Value);
			//		OnTabClick(new TabClickEventArgs(hit, e));
			//	}
			//	m_mouse_down_at = null;
			//	base.OnMouseUp(e);
			//}
			//			protected override void OnMouseDoubleClick(MouseEventArgs e)
			//			{
			//				base.OnMouseDoubleClick(e);
			//
			//				// Toggle floating when double clicked
			//				var hit = HitTestTabButton(e.GetPosition(this));
			//				OnTabDblClick(new TabClickEventArgs(hit, e));
			//			}

			/// <summary>The location of the tab strip. Only L,T,R,B are valid</summary>
			public EDockSite StripLocation
			{
				get { return m_strip_loc; }
				set
				{
					if (m_strip_loc == value) return;
					m_strip_loc = value;

					if (m_strip_loc != EDockSite.None)
					{
						switch (m_strip_loc)
						{
						default: throw new Exception("Invalid tab strip location");
						case EDockSite.Left:
						case EDockSite.Right:
							{
								LayoutTransform = new RotateTransform(90);
								break;
							}
						case EDockSite.Top:
						case EDockSite.Bottom:
							{
								LayoutTransform = Transform.Identity;
								break;
							}
						}

						// Update the dock location on the parent
						if (Parent is DockPanel dp)
						{
							Name = $"TabStrip{m_strip_loc}";
							DockPanel.SetDock(this, DockContainer.ToDock(m_strip_loc));
						}
					}
					else
					{
						// Hide if not docked
						Visibility = Visibility.Collapsed;
					}
				}
			}
			private EDockSite m_strip_loc;

			/// <summary>The size of the tab strip</summary>
			public double StripSize
			{
				get
				{
					return
						Orientation == Orientation.Horizontal ? ActualHeight :
						Orientation == Orientation.Vertical ? ActualWidth :
						throw new Exception("Orientation unknown");
				}
			}

			/// <summary>The tab buttons in this tab strip</summary>
			public ObservableCollection<TabButton> Buttons
			{
				get { return m_buttons; }
				private set
				{
					if (m_buttons == value) return;
					if (m_buttons != null)
					{
						m_buttons.CollectionChanged -= HandleCollectionChanged;
					}
					m_buttons = value;
					if (m_buttons != null)
					{
						m_buttons.CollectionChanged += HandleCollectionChanged;
					}

					// Handle buttons added or removed from this tab strip
					void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
					{
						switch (e.Action)
						{
						case NotifyCollectionChangedAction.Add:
							{
								foreach (var btn in e.NewItems.Cast<TabButton>())
								{
									Children.Add(btn);
								}

								if (Buttons.Count != 0)
									Visibility = Visibility.Visible;

								break;
							}
						case NotifyCollectionChangedAction.Remove:
							{
								foreach (var btn in e.OldItems.Cast<TabButton>())
								{
									Children.Remove(btn);
								}

								if (Buttons.Count == 0 || (Buttons.Count == 1 && !m_opts.AlwaysShowTabs))
									Visibility = Visibility.Collapsed;

								break;
							}
						}
					}
				}
			}
			private ObservableCollection<TabButton> m_buttons;

			/// <summary>Hit test the tab strip, returning the dockable associated with a hit tab button, or null. 'pt' is in TabStrip space</summary>
			public DockControl HitTestTabButton(Point pt)
			{
				var hit = InputHitTest(pt);
				return (hit as TabButton)?.DockControl;
			}

			/// <summary>Get the content associated with the tabs</summary>
			public IEnumerable<DockControl> Content
			{
				get { return Buttons.Select(x => x.DockControl); }
			}
		}

		/// <summary>A tab that displays within a tab strip</summary>
		[DebuggerDisplay("{DockControl}")]
		public class TabButton : Button
		{
			private OptionData m_opt;
			public TabButton(string text, OptionData opt)
			{
				m_opt = opt;
				DockControl = null;
				Content = text;
			}
			public TabButton(DockControl content, OptionData opt)
			{
				m_opt = opt;
				DockControl = content;
				Content = content.TabText;
			}
			protected override void OnClick()
			{
				base.OnClick();

				// Make our content the active content on its dock pane
				if (DockControl?.DockPane != null)
					DockControl.DockPane.VisibleContent = DockControl;
			}
			protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
			{
				base.OnMouseDoubleClick(e);

				// Float the content on double click
				if (DockControl != null && m_opt.DoubleClickTabsToFloat)
					DockControl.IsFloating = !DockControl.IsFloating;
			}

			/// <summary>Returns the DockControl for the associated dockable item</summary>
			public DockControl DockControl { get; private set; }
		}

		/// <summary>Implementation of active panes and active content for dock containers, floating windows, and auto hide windows</summary>
		internal class ActiveContentImpl
		{
			public ActiveContentImpl(ITreeHost tree_host)
			{
				TreeHost = tree_host;
			}

			/// <summary>The root of the tree in this dock container</summary>
			private ITreeHost TreeHost { [DebuggerStepThrough] get; set; }

			/// <summary>
			/// Get/Set the active content. This will cause the pane that the content is on to also become active.
			/// To change the active content in a pane without making the pane active, assign to the pane's ActiveContent property</summary>
			public DockControl ActiveContent
			{
				get { return ActivePane?.VisibleContent; }
				set
				{
					if (ActiveContent == value) return;

					// Ensure 'value' is the active content on its pane
					if (value != null)
						value.DockPane.VisibleContent = value;

					// Set value's pane as the active one.
					// If value's pane was the active one before, then setting 'value' as the active content
					// on the pane will also have caused an OnActiveContentChanged to be called. If not, then
					// changing the active pane here will result in OnActiveContentChanged being called.
					ActivePane = value?.DockPane;
				}
			}

			/// <summary>Get/Set the active pane. Note, this pane may be within the dock container, a floating window, or an auto hide window</summary>
			public DockPane ActivePane
			{
				get { return m_active_pane; }
				set
				{
					if (m_active_pane == value) return;
					var old_pane = m_active_pane;
					var old_content = ActiveContent;

					// Change the pane
					if (m_active_pane != null)
					{
						m_active_pane.VisibleContentChanged -= HandleActiveContentChanged;
						m_active_pane.VisibleContent?.SaveFocus();
					}
					m_prev_pane = new WeakReference<DockPane>(m_active_pane);
					m_active_pane = value;
					if (m_active_pane != null)
					{
						m_active_pane.VisibleContent?.RestoreFocus();
						m_active_pane.VisibleContentChanged += HandleActiveContentChanged;
					}

					// Notify observers of each pane about activation changed
					old_pane?.OnActivatedChanged();
					value?.OnActivatedChanged();

					// Notify that the active pane has changed and therefore the active content too
					if (old_pane != value)
						RaiseActivePaneChanged(new ActivePaneChangedEventArgs(old_pane, value));
					if (old_content?.Dockable != ActiveContent?.Dockable)
						RaiseActiveContentChanged(new ActiveContentChangedEventArgs(old_content?.Dockable, ActiveContent?.Dockable));

					/// <summary>Watch for the content in the active pane changing</summary>
					void HandleActiveContentChanged(object sender, ActiveContentChangedEventArgs e)
					{
						RaiseActiveContentChanged(e);
					}
				}
			}
			private DockPane m_active_pane;
			private WeakReference<DockPane> m_prev_pane;

			/// <summary>Make the previously active dock pane the active pane</summary>
			public void ActivatePrevious()
			{
				var pane = m_prev_pane.TryGetTarget(out var p) ? p : null;
				if (pane != null && pane.DockContainer != null)
					ActivePane = pane;
			}

			/// <summary>Raised whenever the active pane changes in the dock container</summary>
			public event EventHandler<ActivePaneChangedEventArgs> ActivePaneChanged;
			internal void RaiseActivePaneChanged(ActivePaneChangedEventArgs args)
			{
				ActivePaneChanged?.Invoke(TreeHost, args);
			}

			/// <summary>Raised whenever the active content for the dock container changes</summary>
			public event EventHandler<ActiveContentChangedEventArgs> ActiveContentChanged;
			internal void RaiseActiveContentChanged(ActiveContentChangedEventArgs args)
			{
				ActiveContentChanged?.Invoke(TreeHost, args);
			}
		}

		/// <summary>Extension method helpers</summary>
		internal static class Extn
		{
			/// <summary>True if this dock site is a edge</summary>
			public static bool IsEdge(this EDockSite ds)
			{
				return ds == EDockSite.Left || ds == EDockSite.Top || ds == EDockSite.Right || ds == EDockSite.Bottom;
			}
		}

		/// <summary>Tags used in persisting layout to XML</summary>
		internal static class XmlTag
		{
			public const string DockContainerLayout = "DockContainerLayout";
			public const string Version = "version";
			public const string Name = "name";
			public const string Type = "type";
			public const string Id = "id";
			public const string Index = "index";
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
			public const string AutoHide = "auto_hide";
			public const string FloatingWindow = "floating_window";
			public const string Bounds = "bounds";
			public const string Pinned = "pinned";
			public const string DockSizes = "dock_sizes";
			public const string Visible = "visible";
			public const string StripLocation = "strip_location";
			public const string Active = "active";
			public const string UserData = "user_data";
		}

		/// <summary>Marker interface for DockPanes and Branches</summary>
		internal interface IPaneOrBranch : IDisposable
		{
			/// <summary>The parent of this dock pane or branch</summary>
			Branch ParentBranch { get; }
		}

		/// <summary>Dock site mask value</summary>
		[Flags]
		internal enum EDockMask
		{
			None = 0,
			Centre = 1 << EDockSite.Centre,
			Top = 1 << EDockSite.Top,
			Bottom = 1 << EDockSite.Bottom,
			Left = 1 << EDockSite.Left,
			Right = 1 << EDockSite.Right,
		}

		#region Event Args

		/// <summary>Args for when the active content on the dock container or dock pane changes</summary>
		public class ActiveContentChangedEventArgs : EventArgs
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
		public class ActivePaneChangedEventArgs : EventArgs
		{
			public ActivePaneChangedEventArgs(DockPane old, DockPane nue)
			{
				PaneOld = old;
				PaneNew = nue;
			}

			/// <summary>The pane that was active</summary>
			public DockPane PaneOld { get; private set; }

			/// <summary>The pane that is becoming active</summary>
			public DockPane PaneNew { get; private set; }
		}

		/// <summary>Args for when dockables are moved within the dock container</summary>
		public class DockableMovedEventArgs : EventArgs
		{
			public DockableMovedEventArgs(EAction action, IDockable who)
			{
				Action = action;
				Dockable = who;
			}

			/// <summary>What happened to the dockable</summary>
			public EAction Action { get; private set; }
			public enum EAction
			{
				Added = TreeChangedEventArgs.EAction.Added,
				Removed = TreeChangedEventArgs.EAction.Removed,
			}

			/// <summary>The dockable that is being added or removed</summary>
			public IDockable Dockable { get; private set; }
		}

		/// <summary>Args for when the DockContainer is changed on a DockControl</summary>
		public class DockContainerChangedEventArgs : EventArgs
		{
			public DockContainerChangedEventArgs(DockContainer old, DockContainer nue)
			{
				Previous = old;
				Current = nue;
			}

			/// <summary>The old dock container</summary>
			public DockContainer Previous { get; private set; }

			/// <summary>The new dock container</summary>
			public DockContainer Current { get; private set; }
		}

		/// <summary>Args for when layout is being saved</summary>
		public class DockContainerSavingLayoutEventArgs : EventArgs
		{
			public DockContainerSavingLayoutEventArgs(XElement node)
			{
				Node = node;
			}

			/// <summary>The XML element to add data to</summary>
			public XElement Node { get; private set; }
		}

		/// <summary>Args for when panes or branches are added to a tree</summary>
		internal class TreeChangedEventArgs : EventArgs
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

			public TreeChangedEventArgs(EAction action, DockControl dockcontrol = null, DockPane pane = null, Branch branch = null)
			{
				Action = action;
				DockControl = dockcontrol;
				DockPane = pane;
				Branch = branch;
			}

			/// <summary>The type of change that occurred</summary>
			public EAction Action { get; private set; }

			/// <summary>Non-null if it was a dockable that was added or removed</summary>
			public DockControl DockControl { get; private set; }

			/// <summary>Non-null if it was a dock pane that was added or removed</summary>
			public DockPane DockPane { get; private set; }

			/// <summary>Non-null if it was a branch that was added or removed</summary>
			public Branch Branch { get; private set; }
		}

		#endregion
	}
}
