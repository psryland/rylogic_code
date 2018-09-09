using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF
{
	public static class XmlWPFExtensions
	{
		public static XmlConfig SupportWPFTypes(this XmlConfig cfg)
		{
			Xml_.ToMap[typeof(Point)] = (obj, node) =>
			{
				var pt = (Point)obj;
				node.SetValue($"{pt.X} {pt.Y}");
				return node;
			};
			Xml_.AsMap[typeof(Point)] = (elem, type, ctor) =>
			{
				var xy = elem.Value.Split(Xml_.WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
				return new Point(double.Parse(xy[0]), double.Parse(xy[1]));
			};

			Xml_.ToMap[typeof(Vector)] = (obj, node) =>
			{
				var vec = (Vector)obj;
				node.SetValue($"{vec.X} {vec.Y}");
				return node;
			};
			Xml_.AsMap[typeof(Vector)] = (elem, type, instance) =>
			{
				var xy = elem.Value.Split(Xml_.WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
				return new Vector(double.Parse(xy[0]), double.Parse(xy[1]));
			};

			Xml_.ToMap[typeof(Size)] = (obj, node) =>
			{
				var pt = (Size)obj;
				node.SetValue($"{pt.Width} {pt.Height}");
				return node;
			};
			Xml_.AsMap[typeof(Size)] = (elem, type, ctor) =>
			{
				var wh = elem.Value.Split(Xml_.WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
				return new Size(double.Parse(wh[0]), double.Parse(wh[1]));
			};

			Xml_.ToMap[typeof(Rect)] = (obj, node) =>
			{
				var rect = (Rect)obj;
				node.Add($"{rect.X} {rect.Y} {rect.Width} {rect.Height}");
				return node;
			};
			Xml_.AsMap[typeof(Rect)] = (elem, type, instance) =>
			{
				var xywh = elem.Value.Split(Xml_.WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
				return new Rect(double.Parse(xywh[0]), double.Parse(xywh[1]), double.Parse(xywh[2]), double.Parse(xywh[3]));
			};

			Xml_.ToMap[typeof(Color)] = (obj, node) =>
			{
				var col = (Color)obj;
				node.SetValue($"{col.A:X2}{col.R:X2}{col.G:X2}{col.B:X2}");
				return node;
			};
			Xml_.AsMap[typeof(Color)] = (elem, type, instance) =>
			{
				var argb = uint.Parse(elem.Value, NumberStyles.HexNumber);
				return Color.FromArgb(
					(byte)((argb >> 24) & 0xFF),
					(byte)((argb >> 16) & 0xFF),
					(byte)((argb >>  8) & 0xFF),
					(byte)((argb >>  0) & 0xFF));
			};

			return cfg;
		}
	}
}
