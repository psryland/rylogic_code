using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>A strip of tab buttons</summary>
	[DebuggerDisplay("{StripLocation}")]
	public partial class TabStrip : StackPanel
	{
		// Notes:
		//  - All tab strips are horizontal with a layout transform for vertical strips

		public TabStrip()
		{
			InitializeComponent();
			Orientation = Orientation.Horizontal;
			Buttons = new ObservableCollection<TabButton>();
		}
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			UpdateLayoutTransform();
		}

		/// <summary>Dock container options</summary>
		private OptionsData Options => TreeHost?.DockContainer.Options ?? new OptionsData();

		/// <summary>Returns the tree root that hosts this tab strip</summary>
		internal ITreeHost TreeHost => Gui_.FindVisualParent<ITreeHost>(this);

		/// <summary>The location of the tab strip. Only L,T,R,B are valid</summary>
		public EDockSite StripLocation => Parent is DockPanel ? DockPanel.GetDock(this).ToDockSite() : EDockSite.None;

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

		/// <summary>The auto hide panel this tab strip is associated with (null if none)</summary>
		public AutoHidePanel AHPanel { get; set; }

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
								Children.Add(btn);
							break;
						}
					case NotifyCollectionChangedAction.Remove:
						{
							foreach (var btn in e.OldItems.Cast<TabButton>())
								Children.Remove(btn);
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
		}
		private ObservableCollection<TabButton> m_buttons;

		/// <summary>Hit test the tab strip, returning the dockable associated with a hit tab button, or null. 'pt' is in TabStrip space</summary>
		private DockControl HitTestTabButton(Point pt)
		{
			var hit = InputHitTest(pt);
			return (hit as TabButton)?.DockControl;
		}

		/// <summary>Get the content associated with the tabs</summary>
		public IEnumerable<DockControl> Content
		{
			get { return Buttons.Select(x => x.DockControl); }
		}

		/// <summary>Set the layout transform based on the current strip location</summary>
		private void UpdateLayoutTransform()
		{
			switch (StripLocation)
			{
			default: throw new Exception($"Invalid tab strip location: {StripLocation}");
			case EDockSite.None:
				break;
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
		}
	}
}
