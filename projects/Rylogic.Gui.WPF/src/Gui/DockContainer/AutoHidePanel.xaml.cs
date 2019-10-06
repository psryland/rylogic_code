using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>A panel that docks to the edges of the main dock container and auto hides when focus is lost</summary>
	[DebuggerDisplay("AutoHidePanel {DockSite}")]
	public sealed partial class AutoHidePanel : Grid, ITreeHost, IDisposable
	{
		// Notes:
		//  - An auto hide panel is a panel that pops out from the edges of the dock container.
		//  - It is basically a root branch with a single centre dock pane. The tab strip from the
		//    dock pane is used for the auto-hide tab strip displayed around the edges of the control.
		// Other behaviours:
		//  - Click pin on the pane only pins the active content
		//  - Clicking a tab makes that dockable the active content
		//  - 'Root' only uses the centre dock site

		public AutoHidePanel(DockContainer dc, EDockSite ds)
		{
			InitializeComponent();
			Visibility = Visibility.Collapsed;
			Canvas.SetZIndex(this, 1);

			DockContainer = dc;
			DockSite = ds;
			PoppedOut = false;

			var splitter_size = 5.0;
			var splitter = new GridSplitter
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				Background = SystemColors.ControlBrush,
				BorderBrush = new SolidColorBrush(Color.FromRgb(0xc0, 0xc0, 0xc0)),
				BorderThickness = DockSite.IsVertical() ? new Thickness(1, 0, 1, 0) : new Thickness(0, 1, 0, 1),
			};

			// Create a grid to contain the root branch and the splitter
			switch (DockSite)
			{
			default: throw new Exception($"Auto hide panels cannot be docked to {ds}");
			case EDockSite.Left:
			case EDockSite.Right:
				{
					// Vertical auto hide panel
					ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DockSite == EDockSite.Left ? 1 : 2, GridUnitType.Star), MinWidth = 10 });
					ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
					ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DockSite == EDockSite.Right ? 1 : 2, GridUnitType.Star), MinWidth = 10 });

					// Add the root branch to the appropriate column
					Root = Children.Add2(new Branch(dc, DockSizeData.Quarters));
					Grid.SetColumn(Root, DockSite == EDockSite.Left ? 0 : 2);

					// Add the splitter to the centre column
					splitter.Width = splitter_size;
					Children.Add2(splitter);
					Grid.SetColumn(splitter, 1);
					break;
				}
			case EDockSite.Top:
			case EDockSite.Bottom:
				{
					// Horizontal auto hide panel
					RowDefinitions.Add(new RowDefinition { Height = new GridLength(DockSite == EDockSite.Top ? 1 : 2, GridUnitType.Star), MinHeight = 10 });
					RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
					RowDefinitions.Add(new RowDefinition { Height = new GridLength(DockSite == EDockSite.Bottom ? 1 : 2, GridUnitType.Star), MinHeight = 10 });

					// Add the root branch to the appropriate row
					Root = Children.Add2(new Branch(dc, DockSizeData.Quarters));
					Grid.SetRow(Root, DockSite == EDockSite.Top ? 0 : 2);

					// Add the splitter to the centre row
					splitter.Height = splitter_size;
					Children.Add2(splitter);
					Grid.SetRow(splitter, 1);
					break;
				}
			}

			// Remove the tab strip from the dock pane child so we can use
			// it as the auto hide tab strip for this auto hide panel
			TabStrip = DockPane.TabStrip;
			TabStrip.Detach();
			TabStrip.AHPanel = this;
		}
		public void Dispose()
		{
			Root = null!;
			DockContainer = null!;
		}

		/// <summary>The dock container that owns this auto hide window</summary>
		public DockContainer DockContainer
		{
			get => m_dc;
			private set
			{
				if (m_dc == value) return;
				if (m_dc != null)
				{
					ActiveContentManager.ActiveContentChanged -= HandleActiveContentChanged;
				}
				m_dc = value;
				if (m_dc != null)
				{
					ActiveContentManager.ActiveContentChanged += HandleActiveContentChanged;
				}

				/// <summary>Handler for when the active content changes</summary>
				void HandleActiveContentChanged(object? sender, ActiveContentChangedEventArgs e)
				{
					// Hide the auto hide panel whenever content that isn't in our tree becomes active
					if (e.ContentNew == null || e.ContentNew.DockControl.DockPane?.RootBranch != Root)
						PoppedOut = false;

					// Show the auto hide panel whenever content that is in our tree becomes active
					if (e.ContentNew != null && e.ContentNew.DockControl.DockPane?.RootBranch == Root)
						PoppedOut = true;
				}
			}
		}
		private DockContainer m_dc = null!;
		DockContainer ITreeHost.DockContainer => DockContainer;

		/// <summary>The root level branch of the tree in this auto hide window</summary>
		internal Branch Root
		{
			get => m_root;
			private set
			{
				if (m_root == value) return;
				if (m_root != null)
				{
					m_root.TreeChanged -= HandleTreeChanged;
					Util.Dispose(ref m_root!);
				}
				m_root = value;
				if (m_root != null)
				{
					m_root.TreeChanged += HandleTreeChanged;
				}

				/// <summary>Handler for when the tree in this auto hide panel changes</summary>
				void HandleTreeChanged(object? sender, TreeChangedEventArgs args)
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
							// When the first content is added
							if (Root.AllContent.CountAtMost(2) == 1)
							{
								// Ensure the tab strip is visible
								TabStrip.Visibility = Visibility.Visible;

								// Make the first pane active
								ActivePane = Root.AllPanes.First();
							}

							// Change the behaviour of all dock panes in the auto hide panel to only operate on the visible content
							if (args.DockPane != null)
								args.DockPane.MoveVisibleContentOnly = true;

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
		private Branch m_root = null!;
		Branch ITreeHost.Root
		{
			get { return Root; }
		}

		/// <summary>Manages events and changing of active pane/content</summary>
		private ActiveContentManager ActiveContentManager => DockContainer.ActiveContentManager;

		/// <summary>Return the single dock pane for the auto-hide panel</summary>
		private DockPane DockPane => Root.DockPane(EDockSite.Centre);

		/// <summary>The tab strip associated with this auto hide panel</summary>
		public TabStrip TabStrip { get; }

		/// <summary>The site that this auto hide panel hides to</summary>
		public EDockSite DockSite { get; private set; }

		/// <summary>
		/// Get/Set the active content on this floating window. This will cause the pane that the content is on to also become active.
		/// To change the active content in a pane without making the pane active, assign to the pane's ActiveContent property</summary>
		public DockControl? ActiveContent
		{
			get => ActiveContentManager.ActiveContent;
			set => ActiveContentManager.ActiveContent = value;
		}

		/// <summary>Get/Set the active pane. Note, this pane may be within the dock container, a floating window, or an auto hide window</summary>
		public DockPane? ActivePane
		{
			get => ActiveContentManager.ActivePane;
			set => ActiveContentManager.ActivePane = value;
		}

		/// <summary>Get/Set the popped out state of the auto hide panel</summary>
		public bool PoppedOut
		{
			get { return Visibility == Visibility.Visible; }
			set
			{
				if (PoppedOut == value) return;

				// Show/Hide the panel
				Visibility = value ? Visibility.Visible : Visibility.Collapsed;

				// When no longer popped out, make the last active content active again
				if (!PoppedOut && ActivePane == DockPane)
					ActiveContentManager.ActivatePrevious();
			}
		}

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
	}
}
