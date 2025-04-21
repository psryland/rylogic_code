using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>A floating window that hosts a tree of dock panes</summary>
	[DebuggerDisplay("FloatingWindow")]
	public partial class FloatingWindow : Window, ITreeHost, IPinnable
	{
		public FloatingWindow(DockContainer dc)
		{
			// Don't set 'Owner' to 'Window.GetWindow(dc)', 'dc' may not have an owner
			// window yet. Also, it's nice to allow floating windows behind the main window.
			InitializeComponent();
			Content = new DockPanel { LastChildFill = true };

			DockContainer = dc;
			PinState = new PinData(this, EPin.Centre, pinned: false);
			Root = new Branch(dc, DockSizeData.Quarters);

			SizeChanged += delegate { DockContainer.NotifyLayoutChanged(); };
		}
		protected override void OnClosing(CancelEventArgs e)
		{
			// Move all the content back to the main dock container
			foreach (var dc in AllContent.ToArray())
				dc.IsFloating = false;

			base.OnClosing(e);
		}
		protected override void OnClosed(EventArgs e)
		{
			Root = null!;
			DockContainer = null!;
			base.OnClosed(e);
		}

		/// <summary>An identifier for a floating window</summary>
		public int Id { get; set; }

		/// <summary>The dock container that owns this floating window</summary>
		public DockContainer DockContainer
		{
			get => m_dc;
			private set
			{
				if (m_dc == value) return;
				if (m_dc != null)
				{
					m_dc.ActiveContentChanged -= HandleActiveContentChanged;
					m_dc.FloatingWindows?.Remove(this);
				}
				m_dc = value;
				if (m_dc != null)
				{
					m_dc.ActiveContentChanged += HandleActiveContentChanged;
				}

				/// <summary>Handler for when the active content changes</summary>
				void HandleActiveContentChanged(object? sender, ActiveContentChangedEventArgs e)
				{
					// If the new active content is within this floating window, update the window title
					if (ActiveContentManager.ActivePane?.RootBranch == Root)
					{
						var dc = e.ContentNew?.DockControl;
						var win = GetWindow(DockContainer);
						var window_title = win?.Title ?? string.Empty;
						var content_title = dc?.TabText ?? string.Empty;
						Title = $"{window_title}:{content_title}";
						Icon = dc?.TabIcon ?? win?.Icon;
					}
				}
			}
		}
		private DockContainer m_dc = null!;
		DockContainer ITreeHost.DockContainer => DockContainer;

		/// <summary>The window content as a control container</summary>
		private Panel ContentPanel => (Panel)Content;

		/// <summary>Support pinning this window</summary>
		public PinData PinState { get; }

		/// <summary>The root level branch of the tree in this floating window</summary>
		internal Branch Root
		{
			get => m_root;
			set
			{
				if (m_root == value) return;
				if (m_root != null)
				{
					m_root.TreeChanged -= HandleTreeChanged;
					ContentPanel.Children.Remove(m_root);
					Util.Dispose(ref m_root!);
				}
				m_root = value;
				if (m_root != null)
				{
					ContentPanel.Children.Add(m_root);
					m_root.TreeChanged += HandleTreeChanged;
				}

				/// <summary>Handler for when panes are added/removed from the tree</summary>
				void HandleTreeChanged(object? sender, TreeChangedEventArgs args)
				{
					switch (args.Action)
					{
					case TreeChangedEventArgs.EAction.Added:
					case TreeChangedEventArgs.EAction.Removed:
						{
							// This should be done by the mover, not here...
							//// Grab 'active' when the window gets its first pane
							//if (Root.AllContent.CountAtMost(2) == 1)
							//	ActiveContentManager.ActivePane = Root.AllPanes.First();

							// Don't bother with auto-closing the window when there is no content.
							// It's kinda handy to be able to have empty windows around for docking things into.
							// If you change your mind, don't close from here. The tree can become empty transiently.

							DockContainer.NotifyLayoutChanged();
							break;
						}
					}
				}
			}
		}
		private Branch m_root = null!;
		Branch ITreeHost.Root => Root;

		/// <summary>Manages events and changing of active pane/content</summary>
		private ActiveContentManager ActiveContentManager => DockContainer.ActiveContentManager;

		/// <summary>Enumerate the dockables in this sub-tree (breadth first, order = order of EDockSite)</summary>
		public IEnumerable<DockControl> AllContent => Root.AllContent;

		/// <summary>The current screen location and size of this window</summary>
		public Rect Bounds
		{
			get => new(Left, Top, Width, Height);
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
			return Add(dockable.DockControl, index, location);
		}
		public DockPane Add(IDockable dockable, params EDockSite[] location)
		{
			var addr = location.Length != 0 ? location : new[] { EDockSite.Centre };
			return Add(dockable, int.MaxValue, addr);
		}

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
}
