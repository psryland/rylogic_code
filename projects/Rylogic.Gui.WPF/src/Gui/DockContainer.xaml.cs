using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Win32;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	using DockContainerDetail;

	/// <summary>A dock container is the parent control that manages docking of controls that implement IDockable.</summary>
	public partial class DockContainer : DockPanel, ITreeHost, IDisposable
	{
		/// <summary>The number of valid dock sites</summary>
		private const int DockSiteCount = 5;

		/// <summary>Create a new dock container</summary>
		public DockContainer()
		{
			InitializeComponent();

			Options = new OptionsData();
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
		public OptionsData Options { get; private set; }

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
			if (draggee is DockPane p) dc = p.DockContainer;
			else if (draggee is DockControl c) dc = c.DockContainer;
			else throw new Exception("Dragging only supports dock pane or dock control");

			// Create a form for displaying the dock site locations and handling the drop of a pane or content
			using (var drop_handler = new DragHandler(dc, draggee, ss_start_pt))
				drop_handler.ShowDialog();
		}

		/// <summary>The floating windows associated with this dock container</summary>
		public FloatingWindowCollection FloatingWindows { get; private set; }
		public class FloatingWindowCollection : IEnumerable<FloatingWindow>, IDisposable
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
			case EDockSite.Left: r = new Rect(rect.Left, rect.Top, area.Left, rect.Height); break;
			case EDockSite.Right: r = new Rect(rect.Right - area.Right, rect.Top, area.Right, rect.Height); break;
			case EDockSite.Top: r = new Rect(rect.Left, rect.Top, rect.Width, area.Top); break;
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

		public class AutoHidePanelCollection : IEnumerable<AutoHidePanel>, IDisposable
		{
			private readonly AutoHidePanel[] m_auto_hide;
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
				Canvas.SetTop(m_auto_hide[(int)EDockSite.Left], 0);
				Canvas.SetLeft(m_auto_hide[(int)EDockSite.Top], 0);
				Canvas.SetTop(m_auto_hide[(int)EDockSite.Top], 0);
				Canvas.SetRight(m_auto_hide[(int)EDockSite.Right], 0);
				Canvas.SetTop(m_auto_hide[(int)EDockSite.Right], 0);
				Canvas.SetLeft(m_auto_hide[(int)EDockSite.Bottom], 0);
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
	}
}
