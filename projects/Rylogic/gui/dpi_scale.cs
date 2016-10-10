using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.maths;

namespace pr.gui
{
	public class DpiScale96
	{
		public DpiScale96(Control control)
		{
			using (var gfx = control.CreateGraphics())
			{
				DpiX = gfx.DpiX;
				DpiY = gfx.DpiY;
			}
		}

		/// <summary>The horizontal pixel density</summary>
		public float DpiX { get; private set; }

		/// <summary>The vertical pixel density</summary>
		public float DpiY { get; private set; }

		/// <summary>Scale a point from 96x96 DPI to the current DPI</summary>
		public Point Scale(Point pt)
		{
			return new Point(
				(int)Math.Round(pt.X * DpiX / 96f, MidpointRounding.AwayFromZero),
				(int)Math.Round(pt.Y * DpiY / 96f, MidpointRounding.AwayFromZero));
		}
		public PointF Scale(PointF pt)
		{
			return new PointF(
				(float)Math.Round(pt.X * DpiX / 96f, MidpointRounding.AwayFromZero),
				(float)Math.Round(pt.Y * DpiY / 96f, MidpointRounding.AwayFromZero));
		}

		/// <summary>Scale a point from 96x96 DPI to the current DPI</summary>
		public Size Scale(Size sz)
		{
			return new Size(
				(int)Math.Round(sz.Width  * DpiX / 96f, MidpointRounding.AwayFromZero),
				(int)Math.Round(sz.Height * DpiY / 96f, MidpointRounding.AwayFromZero));
		}
		public SizeF Scale(SizeF sz)
		{
			return new SizeF(
				(float)Math.Round(sz.Width  * DpiX / 96f, MidpointRounding.AwayFromZero),
				(float)Math.Round(sz.Height * DpiY / 96f, MidpointRounding.AwayFromZero));
		}
	}
}
