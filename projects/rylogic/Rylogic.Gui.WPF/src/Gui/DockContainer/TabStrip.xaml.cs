using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>A strip of tab buttons</summary>
	[DebuggerDisplay("{StripLocation}")]
	public partial class TabStrip : WrapPanel
	{
		// Notes:
		//  - All tab strips are horizontal with a layout transform for vertical strips

		public TabStrip()
		{
			InitializeComponent();
			Buttons = new ObservableCollection<TabButton>();
		}
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);

			if (Buttons.Count != 0)
			{
				var max_size_per_button = (ActualWidth - 2) / Buttons.Count;
				foreach (var btn in Buttons)
					btn.MaxWidth = Math.Max(btn.MinWidth, max_size_per_button);
			}
			UpdateLayoutTransform();
		}
		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			base.OnVisualParentChanged(oldParent);
			UpdateLayoutTransform();
		}

		/// <summary>Dock container options</summary>
		private OptionsData Options => TreeHost?.DockContainer.Options ?? new OptionsData();

		/// <summary>Returns the tree root that hosts this tab strip</summary>
		internal ITreeHost? TreeHost => (ITreeHost?)Gui_.FindVisualParent<DependencyObject>(this, x => x is ITreeHost);

		/// <summary>The location of the tab strip. Only L,T,R,B are valid</summary>
		public EDockSite StripLocation
		{
			get => Parent is DockPanel ? DockPanel.GetDock(this).ToDockSite() : EDockSite.None;
			set
			{
				if (Parent is DockPanel && value.IsEdge())
					DockPanel.SetDock(this, value.ToDock());
			}
		}

		/// <summary>The auto hide panel this tab strip is associated with (null if none)</summary>
		public AutoHidePanel? AHPanel { get; set; }

		/// <summary>The tab buttons in this tab strip</summary>
		public ObservableCollection<TabButton> Buttons
		{
			get;
			private set
			{
				if (field == value) return;
				if (field != null)
				{
					field.CollectionChanged -= HandleCollectionChanged;
				}
				field = value;
				if (field != null)
				{
					field.CollectionChanged += HandleCollectionChanged;
				}

				// Handle buttons added or removed from this tab strip
				void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
				{
					switch (e.Action)
					{
						case NotifyCollectionChangedAction.Add:
						{
							// Ensure the order of children matches the order of the buttons
							m_tabs.Children.Clear();
							foreach (var btn in field)
								m_tabs.Children.Add(btn);

							break;
						}
						case NotifyCollectionChangedAction.Remove:
						{
							foreach (var btn in e.OldItems<TabButton>())
								m_tabs.Children.Remove(btn);
							break;
						}
					}

					// Show/Hide the tab strip
					Visibility =
						AHPanel != null ||
						Buttons.Count > 1 ||
						(Buttons.Count == 1 && Options.AlwaysShowTabs)
						? Visibility.Visible : Visibility.Collapsed;
				}
			}
		} = null!;

		/// <summary>Hit test the tab strip, returning the dockable associated with a hit tab button, or null. 'pt' is in TabStrip space</summary>
		private DockControl? HitTestTabButton(Point pt)
		{
			var hit = InputHitTest(pt);
			return (hit as TabButton)?.DockControl;
		}

		/// <summary>Get the content associated with the tabs</summary>
		public IEnumerable<DockControl> AllContent => Buttons.Select(x => x.DockControl);

		/// <summary>Set the layout transform based on the current strip location</summary>
		private void UpdateLayoutTransform()
		{
			switch (StripLocation)
			{
				case EDockSite.None:
				{
					break;
				}
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
				default:
				{
					throw new Exception($"Invalid tab strip location: {StripLocation}");
				}
			}
		}
	}
}
