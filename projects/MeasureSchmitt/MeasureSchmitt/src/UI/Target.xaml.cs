using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Interop.Win32;

namespace MeasureSchmitt
{
	public partial class Target :Window
	{
		private Point? m_offset;
		public Target()
		{
			InitializeComponent();
		}
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.LeftButton != MouseButtonState.Pressed) return;
			m_offset = e.GetPosition(this);
			CaptureMouse();
			e.Handled = true;
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (m_offset == null) return;
			
			// Note: Left/Top are in 1/96 DIP pixels, not real screen pixels
			//var pt = Win32.GetCursorPos();
			var pt = PointToScreen(e.GetPosition(this));
			var dpi_scale = 96.0 / Dpi.DpiForWindow(this.Hwnd());
			Left = pt.X * dpi_scale - m_offset.Value.X;
			Top  = pt.Y * dpi_scale - m_offset.Value.Y;
			e.Handled = true;
		}
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);
			ReleaseMouseCapture();
			m_offset = null;
		}

		/// <summary>The Centre point of the window</summary>
		public Vector CentrePoint
		{
			get
			{
				var dpi_scale = Dpi.DpiForWindow(this.Hwnd()) / 96.0;
				return new Vector(
					(Left + Width/2) * dpi_scale,
					(Top  + Height/2) * dpi_scale
				);
			}
		}
	}
}
