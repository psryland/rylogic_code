using System;
using System.Windows;
using System.Windows.Interop;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;

namespace Rylogic.Extn
{
	public static class View3d_
	{
		/// <summary>Set the render target size of this D3D11Image from the given visual</summary>
		public static void SetRenderTargetSize(this D3D11Image d3d_image, FrameworkElement element, double scale = 1)
		{
			// Ensure the Window that contains 'element' is also the owner of 'd3d_image'
			d3d_image.WindowOwner = Window.GetWindow(element).Hwnd();

			// Set the screen render target size, accounting for DPI
			var win = PresentationSource.FromVisual(element)?.CompositionTarget as HwndTarget;
			var dpi_scaleX = win?.TransformToDevice.M11 ?? 1.0;
			var dpi_scaleY = win?.TransformToDevice.M22 ?? 1.0;

			// Determine the texture size
			var width  = (int)(scale * Math.Max(1, Math.Ceiling(element.ActualWidth * dpi_scaleX)));
			var height = (int)(scale * Math.Max(1, Math.Ceiling(element.ActualHeight * dpi_scaleY)));

			d3d_image.SetRenderTargetSize(width, height, dpi_scaleX, dpi_scaleY);
		}
	}
}
