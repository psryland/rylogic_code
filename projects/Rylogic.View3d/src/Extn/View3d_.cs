using System;
using System.Windows;
using System.Windows.Interop;
using Rylogic.Gfx;

namespace Rylogic.Extn
{
	public static class View3d_
	{
		/// <summary>Set the render target size of this D3D11Image from the given visual</summary>
		public static void SetRenderTargetSize(this D3D11Image d3d_image, FrameworkElement element)
		{
			// Set the screen render target size, accounting for DPI
			var win = PresentationSource.FromVisual(element)?.CompositionTarget as HwndTarget;
			var dpi_scale = win?.TransformToDevice.M11 ?? 1.0;

			// Determine the texture size
			var width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth * dpi_scale));
			var height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight * dpi_scale));

			d3d_image.SetRenderTargetSize(width, height);
		}
	}
}
