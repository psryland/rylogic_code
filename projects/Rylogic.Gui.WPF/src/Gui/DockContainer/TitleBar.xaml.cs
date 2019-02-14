using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>The title bar for a dock pane</summary>
	public partial class TitleBar : StackPanel
	{
		public TitleBar()
		{
			InitializeComponent();
		}
		protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseDown(e);

			// If left click on the title bar and user docking is allowed, start a drag operation
			if (e.LeftButton == MouseButtonState.Pressed && Options.AllowUserDocking)
			{
				// Record the mouse down location.
				// Don't start a drag operation until the mouse has moved a bit.
				m_mouse_down_at = e.GetPosition(this);
				m_dragging = false;
			}
		}
		protected override void OnPreviewMouseMove(MouseEventArgs e)
		{
			base.OnPreviewMouseMove(e);

			// Start dragging when the mouse moves more than a minimum distance
			if (m_mouse_down_at != null && !m_dragging)
			{
				m_dragging |= Point.Subtract(m_mouse_down_at.Value, e.GetPosition(this)).Length > Options.DragThresholdInPixels;
				if (m_dragging)
					DockContainer.DragBegin(DockPane, PointToScreen(e.GetPosition(this)));
			}
		}
		protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseUp(e);
			m_mouse_down_at = null;
			m_dragging = false;
		}
		private Point? m_mouse_down_at;
		private bool m_dragging;

		/// <summary>Control behaviour</summary>
		public OptionsData Options => DockPane?.Options ?? new OptionsData();

		/// <summary>The DockPane that owns this title bar</summary>
		public DockPane DockPane => Parent as DockPane;

		/// <summary>The pin button on the title bar</summary>
		public PinButton PinButton => m_pin;

		/// <summary>The close button on the title bar</summary>
		public CloseButton CloseButton => m_close;

		/// <summary>The title bar text</summary>
		public string Title
		{
			get { return m_title.Text; }
			set { m_title.Text = value ?? string.Empty; }
		}

		/// <summary>Show/Hide the pin button</summary>
		public bool ShowPin
		{
			get { return m_pin.Visibility == Visibility.Visible; }
			set { m_pin.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
		}

		/// <summary>Show/Hide the close button</summary>
		public bool ShowClose
		{
			get { return m_close.Visibility == Visibility.Visible; }
			set { m_close.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
		}
	}
}
