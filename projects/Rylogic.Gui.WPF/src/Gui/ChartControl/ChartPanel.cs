using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Wpf.Interop.DirectX;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.ChartDetail
{
	public class ChartPanel : System.Windows.Controls.Image, IDisposable
	{
		private bool m_render_needed; // True when the scene needs rendering again
		public D3D11Image m_d3d_image;

		public ChartPanel(/*ChartControl owner*/)
		{
			//SetStyle(ControlStyles.Selectable, false);
			//if (this.IsInDesignMode())
			//	return;
			try
			{
				// Create a D3D11 off-screen render target image source
				m_d3d_image = new D3D11Image();
				m_d3d_image.SetPixelSize(100, 100);
				m_d3d_image.OnRender += (surf, is_new) =>
				{
					if (is_new)
					{
						// Set the viewport to match the texture size
						var tex = View3d.Texture.FromShared(surf, View3d.TextureOptions.New());
						Window.Viewport = new View3d.Viewport(0, 0, tex.Info.m_width, tex.Info.m_height);
						Window.SetRT(tex);
					}

					Window.Render();
					Window.Present();
				};
				Source = m_d3d_image;

				// No GDI compatible back buffer, cause we want anti aliasing...
				var opts = new View3d.WindowOptions(null, IntPtr.Zero)
				{
					DbgName = "Chart",
					//Multisampling = owner.Options.AntiAliasing ? 4 : 1,
				};

				//m_owner = owner;
				View3d = View3d.Create();
				Window = new View3d.Window(View3d, IntPtr.Zero, opts)
				{
					LightProperties = View3d.LightInfo.Directional(-v4.ZAxis, Colour32.Zero, Colour32.Gray, Colour32.Zero, 0f, 0f),
					FocusPointVisible = false,
					OriginPointVisible = false,
				};
				Window.CreateDemoScene();

				Camera = Window.Camera;
				Camera.Orthographic = true;
				Camera.SetPosition(new v4(0, 0, 10, 1), v4.Origin, v4.YAxis);
				Camera.ClipPlanes(0.01f, 1000f, true);

				Loaded += (s, a) =>
				{
					var win = System.Windows.Window.GetWindow(this);
					if (win != null)
					{
						m_d3d_image.WindowOwner = new WindowInteropHelper(win).Handle;
						m_d3d_image.RequestRender();
					}
				};
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public void Dispose()
		{
			Window = Util.Dispose(Window);
			View3d = Util.Dispose(View3d);
		}
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);

			// When the size changes, update the size of the render target
			m_d3d_image.SetPixelSize((int)sizeInfo.NewSize.Width, (int)sizeInfo.NewSize.Height);
			m_d3d_image.RequestRender();
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
		private ChartControl Owner => Gui_.FindVisualParent<ChartControl>(Parent);

		/// <summary>Renderer</summary>
		public View3d View3d { get; private set; }

		/// <summary>The view3d window for this control instance</summary>
		public View3d.Window Window { get; private set; }

		/// <summary>The view of the chart</summary>
		public View3d.Camera Camera { get; }

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

		/// <summary>Render the chart 3d scene</summary>
		public void DoPaint()
		{
			//if (Window == null || this.IsInDesignMode())
			//	return;

			// Block scene changed events while clearing/re-adding objects to the window
			//using (Window.SuspendSceneChanged())
			{
				// Update axis graphics
				Owner.Range.AddToScene(Window);

				// Add/Remove all chart elements
				foreach (var elem in Owner.Elements)
					elem.UpdateScene(Window);

				// Add/Remove user graphics
				//m_owner.OnChartRendering(new ChartControl.ChartRenderingEventArgs(m_owner, Window));
			}

			// Start the render
			Window.BackgroundColour = Owner.Options.ChartBkColour;
			Window.FillMode = Owner.Options.FillMode;
			Window.CullMode = Owner.Options.CullMode;
			Window.Render();
		}
		public void Present()
		{
			Window?.Present();
		}

		/// <summary>Returns a point in chart space from a point in ChartPanel-client space.</summary>
		private PointF ClientToChart(PointF point)
		{
			return Gui_.MapPoint(this, Owner, point);
		}

		/// <summary>Returns a point in ChartPanel-client space from a point in chart space. Inverse of ClientToChart</summary>
		private PointF ChartToClient(PointF point)
		{
			return Gui_.MapPoint(Owner, this, Owner.ChartToClient(point));
		}
	}
}
