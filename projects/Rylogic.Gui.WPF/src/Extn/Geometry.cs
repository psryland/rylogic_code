using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF
{
	public static class Geometry_
	{
		/// <summary>Fluent overload of 'Freeze'</summary>
		public static T Freeze2<T>(this T freezable) where T : Freezable
		{
			freezable.Freeze();
			return freezable;
		}

		/// <summary>Create a region from a list of x,y pairs defining line segments</summary>
		public static Geometry MakePolygon(bool closed, IEnumerable<Point> pts)
		{
			var points = new PointCollection(pts);

			var path = new PathGeometry { };
			var fig = path.Figures.Add2(new PathFigure { });
			fig.StartPoint = points[0];
			fig.Segments.Add(new PolyLineSegment { Points = points });
			fig.IsClosed = closed;
			return path;
		}

		/// <summary>Create a path geometry from a list of x,y pairs defining line segments</summary>
		public static Geometry MakePolygon(bool closed, params double[] xy)
		{
			if ((xy.Length % 2) == 1) throw new Exception("Point list must be a list of X,Y pairs");
			return MakePolygon(closed, xy.InPairs().Select(x => new Point(x.Item1, x.Item2)));
		}
		public static Geometry MakePolygon(bool closed, params Point[] xy)
		{
			return MakePolygon(closed, (IEnumerable<Point>)xy);
		}
	}
}
