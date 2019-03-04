using System.Windows;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF.ChartDetail
{
	public class ChartPanel : View3dControl
	{
		static ChartPanel()
		{
			ChartBkColourProperty = Gui_.DPRegister<ChartPanel>(nameof(BackgroundColor), def:Colour32.White);
		}
		public ChartPanel()
		{
			try
			{
				MouseNavigation = false;
				DefaultKeyboardShortcuts = false;
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		//protected override void OnResize(EventArgs e)
		//{
		//	base.OnResize(e);
		//	if (Window != null)
		//		Window.RenderTargetSize = ClientSize;
		//}
		//protected override void OnPaintBackground(PaintEventArgs e)
		//{
		//	// Swallow
		//}
		//protected override void OnPaint(PaintEventArgs e)
		//{
		//	base.OnPaint(e);
		//	if (DesignMode) return;
		//	if (m_render_needed) DoPaint();
		//	Present();
		//
		//	// Add user graphics over the chart. Use a transform to make the
		//	// chart area appear in the same space as the non-chart area.
		//	using (e.Graphics.SaveState())
		//	{
		//		var dims = m_owner.ChartDimensions;
		//		e.Graphics.TranslateTransform(-dims.ChartArea.Left, -dims.ChartArea.Top);
		//		m_owner.OnAddOverlaysOnPaint(new AddOverlaysOnPaintEventArgs(e.Graphics, dims, m_owner.ChartToClientSpace(), EZone.Chart));
		//	}
		//}
		//protected override void OnInvalidated(InvalidateEventArgs e)
		//{
		//	m_render_needed = true;
		//	Window?.Invalidate();
		//	base.OnInvalidated(e);
		//}
		//protected override void WndProc(ref Message m)
		//{
		//	switch (m.Msg)
		//	{
		//	case Win32.WM_NCHITTEST:
		//		// Transparent to input events, let the owner control handle them
		//		m.Result = (IntPtr)Win32.HitTest.HTTRANSPARENT;
		//		return;
		//	}
		//	base.WndProc(ref m);
		//}

		/// <summary>The containing chart control</summary>
		private ChartControl Chart => Gui_.FindVisualParent<ChartControl>(Parent);

		/// <summary>Background colour for the chart</summary>
		public Colour32 BackgroundColor
		{
			get { return (Colour32)GetValue(ChartBkColourProperty); }
			set { SetValue(ChartBkColourProperty, value); }
		}
		public static readonly DependencyProperty ChartBkColourProperty;

		/// <summary>Add an object to the scene</summary>
		public void AddObject(View3d.Object obj)
		{
			Window.AddObject(obj);
		}

		/// <summary>Remove an object from the scene</summary>
		public void RemoveObject(View3d.Object obj)
		{
			Window.RemoveObject(obj);
		}

		/// <summary>Prepare the scene for rendering</summary>
		protected override void OnBuildScene()
		{
			// Update axis graphics
			Chart.Range.AddToScene(Window);

			// Add/Remove all chart elements
			foreach (var elem in Chart.Elements)
				elem.UpdateScene(Window);

			// Set Camera properties
			Camera.Orthographic = Chart.Options.Orthographic;

			// Set window properties
			Window.BackgroundColour = BackgroundColor;
			Window.FillMode = Chart.Options.FillMode;
			Window.CullMode = Chart.Options.CullMode;

			// Raise the BuildScene event
			base.OnBuildScene();
		}

		/// <summary>Returns a point in chart space from a point in ChartPanel-client space.</summary>
		private Point ClientToChart(Point point)
		{
			return Gui_.MapPoint(this, Chart, point);
		}

		/// <summary>Returns a point in ChartPanel-client space from a point in chart space. Inverse of ClientToChart</summary>
		private Point ChartToClient(Point point)
		{
			return Gui_.MapPoint(Chart, this, Chart.ChartToClient(point));
		}
	}
}
