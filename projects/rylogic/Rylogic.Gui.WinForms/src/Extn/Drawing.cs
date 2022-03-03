//***************************************************
// Drawing Extensions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System.Drawing;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Maths;
using Matrix = System.Drawing.Drawing2D.Matrix;

namespace Rylogic.Gui.WinForms
{
	public static class Drawing_
	{
		/// <summary>Get the standard resize cursor for this box zone value</summary>
		public static Cursor ToCursor(this EBoxZone bz)
		{
			if (bz == EBoxZone.Left || bz == EBoxZone.Right  || bz == EBoxZone.LR) return Cursors.SizeWE;
			if (bz == EBoxZone.Top  || bz == EBoxZone.Bottom || bz == EBoxZone.TB) return Cursors.SizeNS;
			if (bz == EBoxZone.TL   || bz == EBoxZone.BR) return Cursors.SizeNWSE;
			if (bz == EBoxZone.BL   || bz == EBoxZone.TR) return Cursors.SizeNESW;
			if (bz == EBoxZone.Centre) return Cursors.SizeAll;
			return Cursors.Default;
		}

		/// <summary>Apply this transform to a single point</summary>
		public static Point TransformPoint(this Matrix m, Point pt)
		{
			var p = new[] {pt};
			m.TransformPoints(p);
			return p[0];
		}
		public static PointF TransformPoint(this Matrix m, PointF pt)
		{
			var p = new[] {pt};
			m.TransformPoints(p);
			return p[0];
		}
		public static v2 TransformPoint(this Matrix m, v2 pt)
		{
			return v2.From(m.TransformPoint(pt.ToPointF()));
		}

		/// <summary>Apply this transform to a rectangle</summary>
		public static Rectangle TransformRect(this Matrix m, Rectangle rect)
		{
			var p = new[] {rect.TopLeft(), rect.BottomRight()};
			m.TransformPoints(p);
			return Rectangle.FromLTRB(p[0].X, p[0].Y, p[1].X, p[1].Y);
		}
		public static RectangleF TransformRect(this Matrix m, RectangleF rect)
		{
			var p = new[] {rect.TopLeft(), rect.BottomRight()};
			m.TransformPoints(p);
			return RectangleF.FromLTRB(p[0].X, p[0].Y, p[1].X, p[1].Y);
		}

		/// <summary>Duplicate this font with the changes given</summary>
		public static Font Dup(this Font prototype, FontStyle style)
		{
			return new Font(prototype, style);
		}

		/// <summary>Duplicate this font with the changes given</summary>
		public static Font Dup(this Font prototype, FontFamily family = null, float? em_size = null, float? delta_size = null, FontStyle? style = null, GraphicsUnit? unit = null)
		{
			return new Font(
				family  ?? prototype.FontFamily, 
				em_size ?? ((delta_size ?? 0) + prototype.Size),
				style   ?? prototype.Style,
				unit    ?? prototype.Unit
				);
		}
	}
}
