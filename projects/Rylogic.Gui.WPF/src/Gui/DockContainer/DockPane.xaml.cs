﻿using System;
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
	public partial class DockPane : DockPanel, IPaneOrBranch, INotifyPropertyChanged, IDisposable
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
			// Note: we don't own any of the content
			VisibleContent = null;
			AllContent = null;

			// Ensure this pane is removed from it's parent branch
			if (ParentBranch != null)
				ParentBranch.Descendants.Remove(this);
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
		public DockPanel Centre => m_content_pane;

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

								// Clear the dock pane on the content
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
						Centre.Children.Clear();

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
						Centre.Children.Add(m_visible_content.Owner);

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
			get { return TreeHost is FloatingWindow; }
			set
			{
				if (AllContent.Count == 0)
					return;

				var visible_content = VisibleContent;
				if (MoveVisibleContentOnly)
				{
					visible_content.IsFloating = value;
				}
				else
				{
					var pane = (DockPane)null;
					foreach (var c in AllContent.ToArray())
					{
						if (pane == null)
						{
							c.IsFloating = true;
							pane = c.DockPane;
						}
						else
						{
							pane.AllContent.Add(c);
						}
					}
					pane.VisibleContent = visible_content;
				}
			}
		}

		/// <summary>Get/Set the content in this pane to auto hide mode</summary>
		public bool IsAutoHide
		{
			get { return TreeHost is AutoHidePanel; }
			set
			{
				if (AllContent.Count == 0)
					return;

				var visible_content = VisibleContent;
				if (MoveVisibleContentOnly)
				{
					visible_content.IsAutoHide = value;
				}
				else
				{
					var pane = (DockPane)null;
					foreach (var c in AllContent.ToArray())
					{
						if (pane == null)
						{
							c.IsAutoHide = true;
							pane = c.DockPane;
						}
						else
						{
							pane.AllContent.Add(c);
						}
					}
					pane.VisibleContent = visible_content;
				}
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
			{
				// Only change Active to Inactive or Inactive to Active.
				// Don't change other button states.
				if (btn.DockControl == VisibleContent)
					btn.TabState = ETabState.Active;
				else if (btn.TabState == ETabState.Active)
					btn.TabState = ETabState.Inactive;
			}

			VisibleContentChanged?.Invoke(this, args);
		}

		/// <summary>Raised when the pane is activated</summary>
		public event EventHandler ActivatedChanged;
		internal void OnActivatedChanged()
		{
			ActivatedChanged?.Invoke(this, EventArgs.Empty);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Activated)));
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
			{// Set up close
				m_close.Click += (s, a) =>
				{
					if (VisibleContent == null) return;
					VisibleContent.DockPane = null;
					RootBranch.PruneBranches();
				};
			}
		}

		/// <summary>Record the layout of the pane</summary>
		public XElement ToXml(XElement node)
		{
			// Record the content that is the visible content in this pane
			if (VisibleContent != null)
				node.Add2(XmlTag.Visible, VisibleContent.PersistName, false);

			// Save the strip location
			if (TabStrip != null)
				node.Add2(XmlTag.StripLocation, TabStrip.StripLocation, false);

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

			// Restore the strip location
			var strip_location = node.Element(XmlTag.StripLocation)?.As<EDockSite>();
			if (strip_location != null)
				TabStrip.StripLocation = strip_location.Value;
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

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}