using System;
using System.Windows;
using System.Windows.Interop;
using Rylogic.Gui.WPF;

namespace Rylogic.Extn
{
	public static class View3d_
	{
		/// <summary>Set the render target size of this D3D11Image from the given visual</summary>
		public static void SetRenderTargetSize(this Gfx.D3DImage d3d_image, FrameworkElement element, double scale = 1)
		{
			// Ensure the Window that contains 'element' is also the owner of 'd3d_image'
			d3d_image.WindowOwner = Window.GetWindow(element).Hwnd();

			// Set the screen render target size, accounting for DPI
			var win = PresentationSource.FromVisual(element)?.CompositionTarget as HwndTarget;
			var dpi_scaleX = (float)(win?.TransformToDevice.M11 ?? 1.0);
			var dpi_scaleY = (float)(win?.TransformToDevice.M22 ?? 1.0);

			// Determine the texture size in pixels
			var width  = (int)(scale * Math.Max(1, Math.Ceiling(element.ActualWidth * dpi_scaleX)));
			var height = (int)(scale * Math.Max(1, Math.Ceiling(element.ActualHeight * dpi_scaleY)));

			d3d_image.SetRenderTargetSize(new(width, height), new(dpi_scaleX, dpi_scaleY));
		}
	}
}
