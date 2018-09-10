//***************************************************
// Colour128
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Drawing;
using System.Globalization;
using Rylogic.Extn;

namespace Rylogic.Gfx
{
	/// <summary>Static functions related to graphics</summary>
	public static class Gfx_
	{
		/// <summary>Linearly interpolate two colours</summary>
		public static Color Blend(Color c0, Color c1, float t)
		{
			return Color.FromArgb(
				(int)(c0.A*(1f - t) + c1.A*t),
				(int)(c0.R*(1f - t) + c1.R*t),
				(int)(c0.G*(1f - t) + c1.G*t),
				(int)(c0.B*(1f - t) + c1.B*t));
		}

		/// <summary>Add XML serialisation support for graphics types</summary>
		public static XmlConfig SupportRylogicGraphicsTypes(this XmlConfig cfg)
		{
			Xml_.ToMap[typeof(Size)] = (obj, node) =>
			{
				var sz = (Size)obj;
				node.SetValue($"{sz.Width} {sz.Height}");
				return node;
			};
			Xml_.AsMap[typeof(Size)] = (elem, type, ctor) =>
			{
				var wh = elem.Value.Split(Xml_.WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
				return new Size(int.Parse(wh[0]), int.Parse(wh[1]));
			};

			Xml_.ToMap[typeof(SizeF)] = (obj, node) =>
			{
				var sz = (SizeF)obj;
				node.SetValue($"{sz.Width} {sz.Height}");
				return node;
			};
			Xml_.AsMap[typeof(SizeF)] = (elem, type, ctor) =>
			{
				var wh = elem.Value.Split(Xml_.WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
				return new SizeF(float.Parse(wh[0]), float.Parse(wh[1]));
			};

			Xml_.ToMap[typeof(Point)] = (obj, node) =>
			{
				var pt = (Point)obj;
				node.SetValue($"{pt.X} {pt.Y}");
				return node;
			};
			Xml_.AsMap[typeof(Point)] = (elem, type, ctor) =>
			{
				var xy = elem.Value.Split(Xml_.WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
				return new Point(int.Parse(xy[0]), int.Parse(xy[1]));
			};

			Xml_.ToMap[typeof(PointF)] = (obj, node) =>
			{
				var pt = (PointF)obj;
				node.SetValue($"{pt.X} {pt.Y}");
				return node;
			};
			Xml_.AsMap[typeof(PointF)] = (elem, type, ctor) =>
			{
				var xy = elem.Value.Split(Xml_.WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
				return new PointF(float.Parse(xy[0]), float.Parse(xy[1]));
			};

			Xml_.ToMap[typeof(Rectangle)] = (obj, node) =>
			{
				var rc = (Rectangle)obj;
				node.SetValue($"{rc.X} {rc.Y} {rc.Width} {rc.Height}");
				return node;
			};
			Xml_.AsMap[typeof(Rectangle)] = (elem, type, ctor) =>
			{
				var xywh = elem.Value.Split(Xml_.WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
				return new Rectangle(int.Parse(xywh[0]), int.Parse(xywh[1]), int.Parse(xywh[2]), int.Parse(xywh[3]));
			};

			Xml_.ToMap[typeof(RectangleF)] = (obj, node) =>
			{
				var rc = (RectangleF)obj;
				node.SetValue($"{rc.X} {rc.Y} {rc.Width} {rc.Height}");
				return node;
			};
			Xml_.AsMap[typeof(RectangleF)] = (elem, type, ctor) =>
			{
				var xywh = elem.Value.Split(Xml_.WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
				return new RectangleF(float.Parse(xywh[0]), float.Parse(xywh[1]), float.Parse(xywh[2]), float.Parse(xywh[3]));
			};

			Xml_.ToMap[typeof(Color)] = (obj, node) =>
			{
				var col = ((Color)obj).ToArgb().ToString("X8");
				node.SetValue(col);
				return node;
			};
			Xml_.AsMap[typeof(Color)] = (elem, type, ctor) =>
			{
				return Color_.FromArgb(uint.Parse(elem.Value, NumberStyles.HexNumber));
			};

			Xml_.ToMap[typeof(Colour32)] = (obj, node) =>
			{
				var col = ((Colour32)obj).ARGB.ToString("X8");
				node.SetValue(col);
				return node;
			};
			Xml_.AsMap[typeof(Colour32)] = (elem, type, ctor) =>
			{
				return new Colour32(uint.Parse(elem.Value, NumberStyles.HexNumber));
			};

			return cfg;
		}
	}
}
