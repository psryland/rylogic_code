using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>
	/// A pane groups a set of IDockable items together. Only one IDockable is displayed at a time in the pane,
	/// but tabs for all dockable items are displayed in the tab strip along the top, bottom, left, or right.</summary>
	[DebuggerDisplay("{DumpDesc()}")]
	public partial class DockPane : DockPanel, IPaneOrBranch, IDisposable
	{
		// Notes:
		//  - DockPane's don't move within the tree, only the content moves.
		//    DockPanes get created in new positions and deleted from old
		//    positions, with the content transferred from one to the other.

		internal DockPane(DockContainer owner)
		{
			InitializeComponent();
			Focusable = true;

			DockContainer = owner ?? throw new ArgumentNullException("The owning dock container cannot be null");
			MoveVisibleContentOnly = false;

			// Create the collection that manages the content within this pane
			AllContent = new ObservableCollection<DockControl>();

			// Bind to this instance
			DataContext = this;

			// Set up UI Elements
			SetupUI();
		}
		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			base.OnVisualParentChanged(oldParent);

			// Set the visibility of the pin
			m_pin.Visibility = DockSite != EDockSite.Centre || !(TreeHost is DockContainer)
				? Visibility.Visible : Visibility.Collapsed;
		}
		protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnIsKeyboardFocusWithinChanged(e);
			if (IsKeyboardFocusWithin)
				ActiveContentManager.ActivePane = this;
		}
		public virtual void Dispose()
		{
			// Ensure this pane is removed from it's parent branch
			if (ParentBranch != null)
				ParentBranch.Descendants.Remove(this);

			// Note: we don't own any of the content
			VisibleContent = null;
			AllContent = null;
		}

		/// <summary>The owning dock container</summary>
		public DockContainer DockContainer { [DebuggerStepThrough] get; }

		/// <summary>Manages events and changing of active pane/content</summary>
		private ActiveContentManager ActiveContentManager => DockContainer.ActiveContentManager;

		/// <summary>Control behaviour</summary>
		public OptionsData Options => DockContainer.Options;

		/// <summary>The title bar control for the pane</summary>
		public Panel TitleBar => m_titlebar;

		/// <summary>The tab strip for the pane (visibility controlled adding buttons to the tab strip)</summary>
		public TabStrip TabStrip => m_tabstrip;

		/// <summary>The panel that displays the visible content</summary>
		public ContentControl Centre => m_content_pane;

		/// <summary>The control that hosts the dock pane and branch tree</summary>
		internal ITreeHost TreeHost => ParentBranch?.TreeHost;

		/// <summary>The branch at the top of the tree that contains this pane</summary>
		internal Branch RootBranch => ParentBranch?.RootBranch;

		/// <summary>The branch that contains this pane</summary>
		internal Branch ParentBranch => Parent as Branch;
		Branch IPaneOrBranch.ParentBranch => ParentBranch;

		/// <summary>Get the dock site for this pane</summary>
		public EDockSite DockSite
		{
			get { return ParentBranch?.Descendants.Single(x => x.Item == this).DockSite ?? EDockSite.None; }
		}

		/// <summary>Get the dock sites, from top to bottom, describing where this pane is located in the tree </summary>
		public EDockSite[] DockAddress
		{
			get { return ParentBranch?.DockAddress.Concat(DockSite).ToArray() ?? new[] { DockSite }; }
		}

		/// <summary>The content hosted by this pane</summary>
		public ObservableCollection<DockControl> AllContent
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
								if (dc == null)
									throw new ArgumentNullException(nameof(dc), "Cannot add 'null' content to a dock pane");
								if (dc.DockPane == this)
									throw new Exception("Do not add content to the same pane twice");

								// Ensure 'dc' has been removed from any other pane's content collections
								// If 'dc.DockPane == this', this will throw because we can't modify the AllContent
								// collection within this handler.
								dc.DockPane = null;

								// Set this pane as the owning dock pane for the dockables.
								// Don't add the dockable as a child control yet, that happens when the active content changes.
								dc.SetDockPaneInternal(this);

								// Add a tab button for this item
								TabStrip.Buttons.Insert(AllContent.IndexOf(dc), new TabButton(dc));

								// Notify tree changed
								ParentBranch?.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.Added, dockcontrol: dc));

								// Make 'dc' the visible content
								VisibleContent = dc;
							}
							break;
						}
					case NotifyCollectionChangedAction.Remove:
						{
							foreach (var dc in e.OldItems.Cast<DockControl>())
							{
								if (dc == null)
									throw new ArgumentNullException(nameof(dc), "Cannot remove 'null' content from a dock pane");
								if (dc.DockPane != this)
									throw new Exception($"Dockable does not belong to this DockPane");
								if (VisibleContent == dc)
									VisibleContent = AllContent.Except(e.OldItems.Cast<DockControl>()).FirstOrDefault();

								// Clear the dock pane from the content
								dc.SetDockPaneInternal(null);

								// Remove the tab button
								TabStrip.Buttons.RemoveIf(x => x.DockControl == dc);

								// Notify tree changed
								ParentBranch?.OnTreeChanged(new TreeChangedEventArgs(TreeChangedEventArgs.EAction.Removed, dockcontrol: dc));

								// Remove empty branches
								RootBranch?.SignalPruneBranches();
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
		/// There should always be visible content while 'AllContent' is not empty. Empty panes are destroyed.
		/// If this pane is not the active pane, then setting the visible content only raises events for this pane.
		/// If this is the active pane, then the dock container events are also raised.</summary>
		public DockControl VisibleContent
		{
			[DebuggerStepThrough]
			get { return m_visible_content; }
			set
			{
				if (m_visible_content == value) return;
				if (m_in_visible_content != 0) return;
				using (Scope.Create(() => ++m_in_visible_content, () => --m_in_visible_content))
				{
					// Ensure 'value' is the current item in the Content collection
					// Only content that is in this pane can be made active for this pane.
					// 'MoveCurrentTo' recursively sets the visible content, hence the reentrancy protection.
					if (value != null && !ContentView.MoveCurrentTo(value))
						throw new Exception($"Dockable item '{value.TabText}' has not been added to this pane so can not be made the active content.");

					// Switch to the new active content
					if (m_visible_content != null)
					{
						// Save the control that had input focus at the time this content became inactive
						m_visible_content.SaveFocus();

						// Remove the element
						// Note: don't dispose the content, we don't own it
						Centre.Content = null;

						// Clear the title bar
						m_title.Text = string.Empty;
					}

					// Note: 'm_visible_content' is of type 'DockControl' rather than 'IDockable' because when the DockControl is Disposed
					// 'IDockable.DockControl' gets set to null before the disposing actually happens. This means we wouldn't be able to access
					// the 'Owner' to remove it from the pane. i.e. 'IDockable.DockControl.Owner' throws because 'DockControl' is null during dispose.
					var prev = m_visible_content;
					m_visible_content = value;

					if (m_visible_content != null)
					{
						// Add the element
						Centre.Content = m_visible_content.Owner;

						// Set the title bar
						m_title.Text = CaptionText;

						// Restore the input focus for this content
						m_visible_content.RestoreFocus();
					}

					// Ensure the tab for the active content is visible
					//TabStripCtrl.MakeTabVisible(ContentView.CurrentPosition);

					// Raise the visible content changed event on this pane.
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
				Debug.Assert(AllContent.All(x => x.IsFloating == floating), "All content in a pane should be in the same state");
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

				// Copy properties from this pane to the new pane that now hosts 'content'
				var pane = content.DockPane;
				// pane.TabStripCtrl.StripLocation = strip_location;

				// Add the remaining content to the same pane that 'content' is now in
				// Careful, moving all content from a pane causes it to be disposed.
				// If the pane only contained 'content' before, then Dispose will have
				// been called and 'AllContent' will be null. In this case, there is
				// nothing else to move anyway
				if (!MoveVisibleContentOnly && AllContent != null)
					pane.AllContent.AddRange(AllContent.ToArray());
			}
		}

		/// <summary>Get/Set the content in this pane to auto hide mode</summary>
		public bool IsAutoHide
		{
			get { return TreeHost is AutoHidePanel; }
			set
			{
				// Notes:
				//  - Setting the IsAutoHide state only moves the content to another pane.
				//    Panes don't move, only the content does.

				// All content in this pane matches the auto hide state of the active content
				var content = VisibleContent;
				if (content == null || content.IsAutoHide == value)
					return;

				// Change the state of the active content
				content.IsAutoHide = value;

				// Copy properties from this pane to the new pane that now hosts 'content'
				var pane = content.DockPane;

				// Add the remaining content to the same pane that 'content' is now in.
				// Careful, moving all content from a pane causes it to be disposed.
				// If the pane only contained 'content' before, then Dispose will have
				// been called and 'AllContent' will be null. In this case, there is
				// nothing else to move anyway
				if (!MoveVisibleContentOnly && AllContent != null)
					pane.AllContent.AddRange(AllContent.ToArray());
			}
		}

		/// <summary>Get/Set whether operations such as floating or auto hiding apply to all content or just the visible content</summary>
		public bool MoveVisibleContentOnly { get; set; }

		/// <summary>Raised whenever the visible content for this dock pane changes</summary>
		public event EventHandler<ActiveContentChangedEventArgs> VisibleContentChanged;
		protected void OnVisibleContentChanged(ActiveContentChangedEventArgs args)
		{
			// Set the active tab button
			foreach (var btn in TabStrip.Buttons)
				btn.IsActiveTab = btn.DockControl == VisibleContent;

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
			get { return ActiveContentManager.ActivePane == this; }
			set
			{
				// Assign this pane as the active one. This will cause the previously active pane
				// to have Activated = false called. Careful, need to handle 'Activated' or 'TreeHost.ActivePane'
				// being assigned to. The ActivePane handler will call OnActivatedChanged.
				ActiveContentManager.ActivePane = value ? this : null;
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

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			{// Set up dragging from the title bar
				bool dragging = false;
				Point? mouse_down_at = null;
				TitleBar.PreviewMouseDown += TitleBarOnMouseDown;
				TitleBar.PreviewMouseMove += TitleBarOnMouseMove;
				TitleBar.PreviewMouseUp += TitleBarOnMouseUp;

				void TitleBarOnMouseDown(object sender, MouseButtonEventArgs e)
				{
					if (e.LeftButton == MouseButtonState.Pressed && Options.AllowUserDocking)
					{
						// Record the mouse down location
						// Don't start dragging until the mouse has moved far enough
						mouse_down_at = e.GetPosition(TitleBar);
						dragging = false;
					}
				}
				void TitleBarOnMouseMove(object sender, MouseEventArgs e)
				{
					var loc = e.GetPosition(TitleBar);

					// See if dragging should start/continue
					if (!dragging && mouse_down_at != null && Point.Subtract(loc, mouse_down_at.Value).Length > 5)
					{
						// Begin dragging this pane
						dragging = true;
						DockContainer.DragBegin(this, PointToScreen(loc));
					}
				}
				void TitleBarOnMouseUp(object sender, MouseButtonEventArgs e)
				{
					mouse_down_at = null;
					dragging = false;
				}
			}
			{// Set up pin/unpin
				m_pin.Click += (s, a) =>
				{
					IsAutoHide = !IsAutoHide;
				};
			}
		}

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
				var content = AllContent.FirstOrDefault(x => x.PersistName == visible);
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
			return $"Count={AllContent.Count} Active={(AllContent.Count != 0 ? CaptionText : "<empty pane>")}";
		}

		/// <summary>A string description of where this pane is in the dock container</summary>
		public string LocationDesc()
		{
			if (TreeHost is AutoHidePanel ahp)
				return $"AutoHidePanel({ahp.DockSite}):{DockAddress.Description()}";
			if (TreeHost is FloatingWindow fw)
				return $"FloatingWindow:{DockAddress.Description()}";
			return $"MainWindow:{DockAddress.Description()}";
		}
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
