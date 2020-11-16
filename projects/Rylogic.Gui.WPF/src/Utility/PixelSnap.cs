using System;
using System.Windows;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	/// <summary>Methods for aligning on integer pixels values.</summary>
	public static class PixelSnap
	{
		/// <summary>Gets the pixel size on the screen containing visual. This method does not take transforms on visual into account.</summary>
		public static Size PixelSize(Visual visual)
		{
			if (PresentationSource.FromVisual(visual) is PresentationSource source)
			{
				var matrix = source.CompositionTarget.TransformFromDevice;
				return new Size(matrix.M11, matrix.M22);
			}
			return new Size(1, 1);
		}

		/// <summary>Aligns <paramref name="value"/> on the next middle of a pixel.</summary>
		/// <param name="value">The value that should be aligned</param>
		/// <param name="pixel_size">The size of one pixel</param>
		public static double PixelAlign(double value, double pixel_size)
		{
			// 0 -> 0.5
			// 0.1 -> 0.5
			// 0.5 -> 0.5
			// 0.9 -> 0.5
			// 1 -> 1.5
			return pixel_size * (Math.Round((value / pixel_size) + 0.5) - 0.5);
		}

		/// <summary>Aligns the borders of rect on the middles of pixels.</summary>
		public static Rect PixelAlign(Rect rect, Size pixel_size)
		{
			rect.X = PixelAlign(rect.X, pixel_size.Width);
			rect.Y = PixelAlign(rect.Y, pixel_size.Height);
			rect.Width = Round(rect.Width, pixel_size.Width);
			rect.Height = Round(rect.Height, pixel_size.Height);
			return rect;
		}

		/// <summary>Rounds <paramref name="point"/> to whole number of pixels.</summary>
		public static Point Round(Point point, Size pixel_size)
		{
			return new Point(Round(point.X, pixel_size.Width), Round(point.Y, pixel_size.Height));
		}

		/// <summary>Rounds val to whole number of pixels.</summary>
		public static Rect Round(Rect rect, Size pixel_size)
		{
			return new Rect(
				Round(rect.X, pixel_size.Width),
				Round(rect.Y, pixel_size.Height),
				Round(rect.Width, pixel_size.Width),
				Round(rect.Height, pixel_size.Height));
		}

		/// <summary>Rounds <paramref name="value"/> to a whole number of pixels.</summary>
		public static double Round(double value, double pixel_size)
		{
			return pixel_size * Math.Round(value / pixel_size);
		}

		/// <summary>Rounds <paramref name="value"/> to an whole odd number of pixels.</summary>
		public static double RoundToOdd(double value, double pixel_size)
		{
			return Round(value - pixel_size, pixel_size * 2) + pixel_size;
		}
	}

}