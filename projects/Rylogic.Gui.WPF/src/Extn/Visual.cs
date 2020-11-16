using System.Windows;
using System.Windows.Media;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public static class Visual_
	{
		/// <summary>Create an RAII rendering scope for this drawing visual</summary>
		public static Scope<DrawingContext> RenderScope(this DrawingVisual dv)
		{
			return Scope.Create(
				() => dv.RenderOpen(),
				dc => dc.Close());
		}

		/// <summary></summary>
		public static Rect TransformToDevice(this Rect rect, Visual visual)
		{
			Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
			return Rect.Transform(rect, matrix);
		}

		/// <summary></summary>
		public static Rect TransformFromDevice(this Rect rect, Visual visual)
		{
			Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
			return Rect.Transform(rect, matrix);
		}

		/// <summary></summary>
		public static Size TransformToDevice(this Size size, Visual visual)
		{
			Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
			return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
		}

		/// <summary></summary>
		public static Size TransformFromDevice(this Size size, Visual visual)
		{
			Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
			return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
		}

		/// <summary></summary>
		public static Point TransformToDevice(this Point point, Visual visual)
		{
			Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
			return new Point(point.X * matrix.M11, point.Y * matrix.M22);
		}

		/// <summary></summary>
		public static Point TransformFromDevice(this Point point, Visual visual)
		{
			Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
			return new Point(point.X * matrix.M11, point.Y * matrix.M22);
		}
	}
}
