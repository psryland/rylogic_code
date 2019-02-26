namespace Rylogic.Extn
{
	public static class Drawing_
	{
		// Notes:
		//  - System.Drawing is available in .NET core, System.Windows is not.
		//    So use System.Drawing primitive types for shared code. System.Windows
		//    primitive types are used by WPF however.

		// Points

		public static System.Drawing.PointF ToPointF(this System.Windows.Point pt)
		{
			return new System.Drawing.PointF((float)pt.X, (float)pt.Y);
		}
		public static Maths.v2 ToV2(this System.Windows.Point pt)
		{
			return new Maths.v2((float)pt.X, (float)pt.Y);
		}
		public static System.Windows.Point ToSysWinPoint(this System.Drawing.PointF pt)
		{
			return new System.Windows.Point(pt.X, pt.Y);
		}
		public static System.Windows.Point ToSysWinPoint(this Maths.v2 pt)
		{
			return new System.Windows.Point(pt.x, pt.y);
		}

		// Sizes

		// Rectangles

		public static System.Drawing.RectangleF ToRectF(this System.Windows.Rect rect)
		{
			return new System.Drawing.RectangleF((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
		}
		public static System.Windows.Rect ToSysWinRect(this System.Drawing.RectangleF rect)
		{
			return new System.Windows.Rect(rect.X, rect.Y, rect.Width, rect.Height);
		}
	}
}
