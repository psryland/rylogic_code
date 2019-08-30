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
		/// <summary>Create a poly line geometry from points</summary>
		public static Geometry MakePolyline(IEnumerable<Point> pts)
		{
			var points = new PointCollection(pts);
			if (points.Count < 2)
				return Geometry.Empty;

			var path = new PathGeometry { };
			var fig = path.Figures.Add2(new PathFigure { });
			fig.StartPoint = points[0];
			fig.Segments.Add(new PolyLineSegment { Points = points });
			fig.IsClosed = false;
			return path;
		}

		/// <summary>Create a region from a list of x,y pairs defining line segments</summary>
		public static Geometry MakePolygon(IEnumerable<Point> pts)
		{
			var points = new PointCollection(pts);
			if (points.Count < 3)
				return Geometry.Empty;

			var path = new PathGeometry { };
			var fig = path.Figures.Add2(new PathFigure { });
			fig.StartPoint = points[0];
			fig.Segments.Add(new PolyLineSegment { Points = points });
			fig.IsClosed = true;
			return path;
		}

		/// <summary>Create a path geometry from a list of x,y pairs defining line segments</summary>
		public static Geometry MakePolygon(params double[] xy)
		{
			if ((xy.Length % 2) == 1) throw new Exception("Point list must be a list of X,Y pairs");
			return MakePolygon(xy.InPairs().Select(x => new Point(x.Item1, x.Item2)));
		}
	}
}
