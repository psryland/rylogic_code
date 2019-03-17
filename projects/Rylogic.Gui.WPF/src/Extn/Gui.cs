using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rylogic.Extn;
using Rylogic.Extn.Windows;

namespace Rylogic.Gui.WPF
{
	public static partial class Gui_
	{
		/// <summary>Wrapper for DependencyProperty.Register that uses reflection to look for changed or coerce handlers</summary>
		public static DependencyProperty DPRegister<T>(string prop_name, object def = null)
		{
			// Use:
			//  In your class with property 'prop_name':
			//  Define:
			//    <prop_name>_Changed() or,
			//    <prop_name>_Changed(<prop_type> new_value) or,
			//    <prop_name>_Changed(<prop_type> old_value, <prop_type> new_value)
			//    to have that method called when the property changes
			//  Define:
			//    <prop_type> <prop_name>_Coerce(<prop_type> value)
			//    to have values coerced (i.e. massaged into a valid value).

			// Don't set 'DefaultValue' unless 'def' is non-null, because the property type
			// may not be a reference type, and 'null' may not be a valid default value.
			var meta = new PropertyMetadata();
			if (def != null) meta.DefaultValue = def;

			// If the type defines a Changed handler, add a callback
			var changed_handler = typeof(T).GetMethod($"{prop_name}_Changed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (changed_handler != null)
			{
				var param_count = changed_handler.GetParameters().Length;
				switch (param_count)
				{
				default: throw new Exception($"Incorrect function signature for handler {prop_name}_Changed");
				case 2: meta.PropertyChangedCallback = (d, e) => changed_handler.Invoke(d, new object[] { e.OldValue, e.NewValue }); break;
				case 1: meta.PropertyChangedCallback = (d, e) => changed_handler.Invoke(d, new object[] { e.NewValue }); break;
				case 0: meta.PropertyChangedCallback = (d, e) => changed_handler.Invoke(d, null); break;
				}
			}

			// If the type defines a Validate handle, add a callback
			var coerce_handler = typeof(T).GetMethod($"{prop_name}_Coerce", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (coerce_handler != null)
			{
				var param_count = coerce_handler.GetParameters().Length;
				switch (param_count)
				{
				default: throw new Exception($"Incorrect function signature for handler {prop_name}_Coerce");
				case 1: meta.CoerceValueCallback = (d, v) => coerce_handler.Invoke(d, new object[] { v }); break;
				}
			}

			var prop_type = typeof(T).GetProperty(prop_name).PropertyType;
			return DependencyProperty.Register(prop_name, prop_type, typeof(T), meta);
		}

		/// <summary>Attached to the Closed event of a window to clean up any child objects that are disposable</summary>
		public static void DisposeChildren(object sender, EventArgs e)
		{
			var obj = (DependencyObject)sender;
			foreach (var child in obj.AllLogicalChildren().OfType<IDisposable>())
				child.Dispose();
		}

		/// <summary>Move a screen-space rectangle so that it is within the virtual screen</summary>
		public static Rect OnScreen(Rect rect)
		{
			var scn = new Rect(
				SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
				SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);

			var r = rect;
			if (rect.Right > scn.Right) r.X = scn.Right - rect.Width;
			if (rect.Bottom > scn.Bottom) r.Y = scn.Bottom - rect.Height;
			if (r.Left < scn.Left) r.X = scn.Left;
			if (r.Top < scn.Top) r.Y = scn.Top;
			return r;
		}
		public static Point OnScreen(Point location, Size size)
		{
			return OnScreen(new Rect(location, size)).Location;
		}

		/// <summary>Fluent Add function</summary>
		public static T Add2<T>(this ItemCollection collection, T item)
		{
			collection.Add(item);
			return item;
		}

		/// <summary>Fluent Add function</summary>
		public static T Add2<T>(this UIElementCollection collection, T item) where T:UIElement
		{
			collection.Add(item);
			return item;
		}

		/// <summary>Fluent Insert function</summary>
		public static T Insert2<T>(this ItemCollection collection, int index, T item) where T : UIElement
		{
			collection.Insert(index, item);
			return item;
		}

		/// <summary>Fluent Insert function</summary>
		public static T Insert2<T>(this UIElementCollection collection, int index, T item) where T : UIElement
		{
			collection.Insert(index, item);
			return item;
		}

		/// <summary>Return a typeface derived from this controls font</summary>
		public static Typeface Typeface(this Control ui)
		{
			return new Typeface(ui.FontFamily, ui.FontStyle, ui.FontWeight, ui.FontStretch);
		}
		public static Typeface Typeface(this TextBlock ui)
		{
			return new Typeface(ui.FontFamily, ui.FontStyle, ui.FontWeight, ui.FontStretch);
		}

		/// <summary>Set the typeface on this control</summary>
		public static void Typeface(this Control ui, Typeface tf)
		{
			ui.FontFamily = tf.FontFamily;
			ui.FontStyle = tf.Style;
			ui.FontWeight = tf.Weight;
			ui.FontStretch = tf.Stretch;
		}
		public static void Typeface(this Control ui, Typeface tf, double size)
		{
			ui.Typeface(tf);
			ui.FontSize = size;
		}
		public static void Typeface(this TextBlock ui, Typeface tf)
		{
			ui.FontFamily = tf.FontFamily;
			ui.FontStyle = tf.Style;
			ui.FontWeight = tf.Weight;
			ui.FontStretch = tf.Stretch;
		}
		public static void Typeface(this TextBlock ui, Typeface tf, double size)
		{
			ui.Typeface(tf);
			ui.FontSize = size;
		}

		/// <summary>Convert a point from 'src' space to 'dst' space</summary>
		public static Point MapPoint(Visual src, Visual dst, Point pt)
		{
			var p0 = src.PointToScreen(pt);
			var p1 = dst.PointFromScreen(p0);
			return p1;
		}
		public static System.Drawing.PointF MapPoint(Visual src, Visual dst, System.Drawing.PointF pt)
		{
			return MapPoint(src, dst, pt.ToSysWinPoint()).ToPointF();
		}

		/// <summary>Converts a Rect from screen space to the space of this visual</summary>
		public static Rect RectFromScreen(this Visual vis, Rect rect)
		{
			return new Rect(vis.PointFromScreen(rect.TopLeft), new Size(rect.Width, rect.Height));
		}

		/// <summary>Converts a Rect to screen space from the space of this visual</summary>
		public static Rect RectToScreen(this Visual vis, Rect rect)
		{
			return new Rect(vis.PointToScreen(rect.TopLeft), new Size(rect.Width, rect.Height));
		}

		/// <summary>Returns new Rect(0,0,RenderSize)</summary>
		public static Rect RenderArea(this UIElement vis, UIElement relative_to_ancestor = null)
		{
			var pt = new Point();
			if (relative_to_ancestor != null) pt = vis.TransformToAncestor(relative_to_ancestor).Transform(pt);
			return new Rect(pt, vis.RenderSize);
		}

		/// <summary>Create a path geometry from a list of x,y pairs defining line segments</summary>
		public static Geometry MakePolygonGeometry(params double[] xy)
		{
			if ((xy.Length % 2) == 1) throw new Exception("Point list must be a list of X,Y pairs");
			return MakePolygonGeometry(xy.InPairs().Select(x => new Point(x.Item1, x.Item2)).ToArray());
		}

		/// <summary>Create a region from a list of x,y pairs defining line segments</summary>
		public static Geometry MakePolygonGeometry(params Point[] pts)
		{
			if (pts.Length < 3) return Geometry.Empty;
			var start = pts[0];
			var segments = pts.Skip(1).Select(pt => new LineSegment(pt, false));
			return new PathGeometry(new[]{ new PathFigure(start, segments, true) });
		}

		/// <summary>Set the position of this window to 'pt'</summary>
		public static void SetLocation(this Window wnd, Point pt)
		{
			wnd.Left = pt.X;
			wnd.Top = pt.Y;
		}

		/// <summary>True if this window was shown using ShowDialog</summary>
		public static bool IsModal(this Window window)
		{
			return (bool)m_fi_showingAsDialog.GetValue(window);
		}
		private static FieldInfo m_fi_showingAsDialog = typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic);
	}
}
