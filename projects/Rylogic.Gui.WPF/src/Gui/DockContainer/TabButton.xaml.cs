using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Rylogic.Common;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>A tab that displays within a tab strip</summary>
	[DebuggerDisplay("{Content}")]
	public partial class TabButton : Button, INotifyPropertyChanged
	{
		// Notes:
		// - TabButtons are children of TabStrips, which are children of DockPanes or
		//   
		private Point? m_mouse_down_at;
		private bool m_dragging;

		public TabButton()
		{
			InitializeComponent();
			DataContext = this;
			DockControl = null!;
			m_def_tab_text = string.Empty;
			m_def_tab_icon = null;
			
		}
		public TabButton(string? text, ImageSource? icon)
			: this()
		{
			m_def_tab_text = text ?? string.Empty;
			m_def_tab_icon = icon;
		}
		public TabButton(DockControl content)
			: this()
		{
			DockControl = content;
			DockControl.PropertyChanged += WeakRef.MakeWeak(HandlePropertyChanged, h => DockControl.PropertyChanged -= h);
			void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				switch (e.PropertyName)
				{
					case nameof(DockControl.TabText):
					{
						NotifyPropertyChanged(nameof(TabText));
						NotifyPropertyChanged(nameof(TabToolTip));
						break;
					}
					case nameof(DockControl.TabToolTip):
					{
						NotifyPropertyChanged(nameof(TabToolTip));
						break;
					}
					case nameof(DockControl.TabIcon):
					{
						NotifyPropertyChanged(nameof(TabIcon));
						break;
					}
				}
			}
		}
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			NotifyPropertyChanged(nameof(Clipped));
		}
		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			base.OnVisualParentChanged(oldParent);
			NotifyPropertyChanged(nameof(EdgeBorder));
			NotifyPropertyChanged(nameof(EdgeMargin));
		}
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);

			// If left clicking on a tab and user docking is allowed, start a drag operation
			if (e.LeftButton == MouseButtonState.Pressed && Options.AllowUserDocking)
			{
				// Record the mouse down location
				// Don't start dragging until the mouse has moved far enough
				m_mouse_down_at = e.GetPosition(this);
				m_dragging = false;
			}

			// Display a tab specific context menu on right click
			if (e.RightButton == MouseButtonState.Pressed)
			{
				if (TabCMenu != null)
				{
					TabCMenu.PlacementTarget = this;
					TabCMenu.IsOpen = true;
				}
			}
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			var loc = e.GetPosition(this);

			// See if dragging should start/continue
			if (!m_dragging && m_mouse_down_at != null && Point.Subtract(loc, m_mouse_down_at.Value).Length > 5)
			{
				// Begin dragging the content associated with the tab
				m_dragging = true;
				if (DockControl != null)
					DockContainer.DragBegin(DockControl, PointToScreen(loc));

				return;
			}
			base.OnMouseMove(e);
		}
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			// If not dragging, then treat this as a mouse click instead of a drag
			if (m_mouse_down_at != null)
			{
				// Notify the owner of this tab that it's been clicked...
				//OnTabClick(new TabClickEventArgs(hit, e));
			}

			m_mouse_down_at = null;
			m_dragging = false;
			base.OnMouseUp(e);
		}
		protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
		{
			base.OnMouseDoubleClick(e);

			// Float the content on double click
			if (DockControl != null && Options.DoubleClickTabsToFloat)
				DockControl.IsFloating = !DockControl.IsFloating;
			// Otherwise notify the owner of this tab of the double click
			//else
			// OnTabDblClick(new TabClickEventArgs(hit, e));
		}
		protected override void OnClick()
		{
			base.OnClick();

			if (DockControl == null)
				return;

			if (!DockControl.IsActiveContent)
			{
				// Make our content the active content on its dock pane
				DockControl.IsActiveContent = true;
				if (TabStrip?.AHPanel is AutoHidePanel ahp)
					ahp.PoppedOut = true;
			}
			else
			{
				// If this is a tab button on an auto hide panel, display the panel
				if (TabStrip?.AHPanel is AutoHidePanel ahp)
					ahp.PoppedOut = !ahp.PoppedOut;
			}
		}

		/// <summary>Control behaviour</summary>
		private OptionsData Options => TreeHost?.DockContainer.Options ?? new OptionsData();

		/// <summary>The tab strip that contains this tab button</summary>
		internal TabStrip? TabStrip => Gui_.FindVisualParent<TabStrip>(this);

		/// <summary>Returns the tree root that hosts this tab (Remember AHP's don't host tab strips)</summary>
		internal ITreeHost? TreeHost => (ITreeHost?)Gui_.FindVisualParent<DependencyObject>(this, x => x is ITreeHost);

		/// <summary>The DockControl for the associated dockable item</summary>
		public DockControl DockControl { get; }

		/// <summary>True if the tab content is larger than the size of the tab</summary>
		public bool Clipped => Math.Abs(ActualWidth - MaxWidth) < 0.0001;

		/// <summary>The text displayed on this tab</summary>
		public string TabText => DockControl?.TabText ?? m_def_tab_text;
		private string m_def_tab_text;

		/// <summary>The icon for the tab</summary>
		public ImageSource? TabIcon => DockControl?.TabIcon ?? m_def_tab_icon;
		private ImageSource? m_def_tab_icon;

		/// <summary>The tool tip string for this tab</summary>
		public string TabToolTip => DockControl?.TabToolTip ?? string.Empty;

		/// <summary>The context menu for this tab</summary>
		public ContextMenu? TabCMenu => DockControl?.TabCMenu;

		/// <summary>The thickness of the tab button borders, based on tab strip direction</summary>
		public Thickness EdgeBorder
		{
			get
			{
				switch (TabStrip?.StripLocation ?? EDockSite.None)
				{
					case EDockSite.None: return new Thickness(1);
					case EDockSite.Left: return new Thickness(1, 1, 0, 1);
					case EDockSite.Top: return new Thickness(1, 1, 1, 0);
					case EDockSite.Right: return new Thickness(0, 1, 1, 1);
					case EDockSite.Bottom: return new Thickness(1, 0, 1, 1);
					default: throw new Exception("Unknown strip location");
				}
			}
		}

		/// <summary>The margin for the button</summary>
		public Thickness EdgeMargin
		{
			get
			{
				switch (TabStrip?.StripLocation ?? EDockSite.None)
				{
					case EDockSite.None: return new Thickness(0);
					case EDockSite.Left: return new Thickness(2, 0, 0, 0);
					case EDockSite.Top: return new Thickness(0, 2, 0, 0);
					case EDockSite.Right: return new Thickness(0, 0, 2, 0);
					case EDockSite.Bottom: return new Thickness(0, 0, 0, 2);
					default: throw new Exception("Unknown strip location");
				}
			}
		}

		/// <summary>True if this button is the active one</summary>
		public ETabState TabState
		{
			get => m_tab_state;
			set
			{
				if (m_tab_state == value) return;
				m_tab_state = value;
				NotifyPropertyChanged(nameof(TabState));
			}
		}
		private ETabState m_tab_state;

		/// <summary>Property changed</summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
