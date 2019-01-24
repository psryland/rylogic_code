using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>A strip of tab buttons</summary>
	[DebuggerDisplay("{StripLocation}")]
	public partial class TabStrip : StackPanel
	{
		public TabStrip()
		{
			InitializeComponent();
			Buttons = new ObservableCollection<TabButton>();

			// All tab strips are horizontal with a layout transform for vertical strips
			Orientation = Orientation.Horizontal;
			StripLocation = EDockSite.None;
		}
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
		}

		/// <summary>Dock container options</summary>
		public OptionsData Options => DockPane?.Options ?? new OptionsData();

		/// <summary>The dock pane this tab strip is part of</summary>
		public DockPane DockPane => Parent as DockPane;

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

							if (Buttons.Count == 0 || (Buttons.Count == 1 && !Options.AlwaysShowTabs))
								Visibility = Visibility.Collapsed;

							break;
						}
					}
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
	}
}
