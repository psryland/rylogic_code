using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>A tab that displays within a tab strip</summary>
	[DebuggerDisplay("{DockControl}")]
	public partial class TabButton : Button
	{
		private readonly OptionsData m_opts;
		private Point? m_mouse_down_at;
		private bool m_dragging;
		private ToolTip m_tt;

		public TabButton(string text, OptionsData opts)
		{
			InitializeComponent();

			m_opts = opts;
			m_tt = new ToolTip { StaysOpen = true, HasDropShadow = true };
			DockControl = null;
			Content = text;
		}
		public TabButton(DockControl content, OptionsData opts)
		{
			InitializeComponent();

			m_opts = opts;
			m_tt = new ToolTip { StaysOpen = true, HasDropShadow = true };
			DockControl = content;
			Content = content.TabText;
		}
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);

			// If left clicking on a tab and user docking is allowed, start a drag operation
			if (e.LeftButton == MouseButtonState.Pressed && m_opts.AllowUserDocking)
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
			m_dragging |= m_mouse_down_at != null && Point.Subtract(loc, m_mouse_down_at.Value).Length > 5;
			if (m_dragging)
			{
				// Begin dragging the content associated with the tab
				if (DockControl != null)
				{
					m_mouse_down_at = null;
					DockContainer.DragBegin(DockControl, PointToScreen(loc));
				}
			}

			// Check for the mouse hovering over the tab
			if (e.LeftButton == MouseButtonState.Released)
			{
				// If the tool tip has been set explicitly, or the tab content is clipped, set the tool tip string
				var tt = string.Empty;
				if (Clipped || !ReferenceEquals(TabToolTip, TabText))
					tt = TabToolTip;

				m_tt.PlacementTarget = this;
				m_tt.Content = tt;
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
			if (DockControl != null && m_opts.DoubleClickTabsToFloat)
				DockControl.IsFloating = !DockControl.IsFloating;
			// Otherwise notify the owner of this tab of the double click
			//else
			// OnTabDblClick(new TabClickEventArgs(hit, e));
		}

		/// <summary>Returns the DockControl for the associated dockable item</summary>
		public DockControl DockControl { get; }

		/// <summary>True if the tab content is larger than the size of the tab</summary>
		public bool Clipped { get; private set; }

		/// <summary>The text displayed on this tab</summary>
		public string TabText => DockControl?.TabText ?? string.Empty;

		/// <summary>The tool tip string for this tab</summary>
		public string TabToolTip => DockControl?.TabToolTip ?? string.Empty;

		/// <summary>The context menu for this tab</summary>
		public ContextMenu TabCMenu => DockControl?.TabCMenu;
	}
}
