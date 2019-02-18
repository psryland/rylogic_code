using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF
{
	public static class Gui_
	{
		/// <summary>Infinite size</summary>
		public static readonly Size SizeInfinity = new Size(double.PositiveInfinity, double.PositiveInfinity);

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

		/// <summary>Return the centre position of the rectangle</summary>
		public static Point Centre(this Rect rect)
		{
			return new Point(rect.Left + rect.Width * 0.5, rect.Top + rect.Height * 0.5);
		}

		/// <summary>Reduces the size of this rectangle by excluding the area 'x'. The result must be a rectangle or an exception is thrown</summary>
		public static Rect Subtract(this Rect r, Rect x)
		{
			// If 'x' has no area, then subtraction is identity
			if (x.Width * x.Height <= 0)
				return r;

			// If the rectangles do not overlap. Right/Bottom is not considered 'in' the rectangle
			if (r.Left >= x.Right || r.Right <= x.Left || r.Top >= x.Bottom || r.Bottom <= x.Top)
				return r;

			// If 'x' completely covers 'r' then the result is empty
			if (x.Left <= r.Left && x.Right >= r.Right && x.Top <= r.Top && x.Bottom >= r.Bottom)
				return Rect.Empty;

			// If 'x' spans 'r' horizontally
			if (x.Left <= r.Left && x.Right >= r.Right)
			{
				// If the top edge of 'r' is aligned with the top edge of 'x', or within 'x'
				// then the top edge of the resulting rectangle is 'x.Bottom'.
				if (x.Top <= r.Top) return new Rect(r.Left, x.Bottom, r.Right - r.Left, r.Bottom - x.Bottom);
				if (x.Bottom >= r.Bottom) return new Rect(r.Left, r.Top, r.Right - r.Left, x.Top - r.Top);
				throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
			}

			// If 'x' spans 'r' vertically
			if (x.Top <= r.Top && x.Bottom >= r.Bottom)
			{
				if (x.Left <= r.Left) return new Rect(x.Right, r.Top, r.Right - x.Right, r.Bottom - r.Top);
				if (x.Right >= r.Right) return new Rect(r.Left, r.Top, x.Left - r.Left, r.Bottom - r.Top);
				throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
			}

			throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
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

		/// <summary>Finds a child in the visual tree matching the specified type (and, optionally, name). Depth-first search</summary>
		public static T FindVisualChild<T>(this DependencyObject parent, string name = null) where T : DependencyObject
		{
			for (int i = 0, iend = VisualTreeHelper.GetChildrenCount(parent); i != iend; ++i)
			{
				// Check each child
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is T && (name == null || (child is FrameworkElement fe && fe.Name == name)))
					return (T)child;

				// Recurse into children
				var item = child.FindVisualChild<T>(name);
				if (item != null)
					return item;
			}
			return null;
		}

		/// <summary>
		/// Finds a parent in the visual tree matching the specified type.
		/// If 'name' is given, the parent must have the given name.
		/// if 'root' is given, the search stops if 'root' is encountered (after testing if it's a parent)</summary>
		public static T FindVisualParent<T>(this DependencyObject item, string name = null, DependencyObject root = null) where T : class
		{
			if (ReferenceEquals(item, root))
				return item as T;

			for (DependencyObject parent; (parent = VisualTreeHelper.GetParent(item)) != null; item = parent)
			{
				if (parent is T && (name == null || (parent is FrameworkElement fe && fe.Name == name)))
					return parent as T;
				if (ReferenceEquals(parent, root))
					break;
			}
			return null;
		}

		/// <summary>Returns true if 'child' is a descendant of this object in the visual tree</summary>
		public static bool IsVisualDescendant(this DependencyObject item, DependencyObject child)
		{
			for (DependencyObject parent; (parent = VisualTreeHelper.GetParent(child)) != null; child = parent)
				if (ReferenceEquals(item, parent))
					return true;

			return false;
		}

		/// <summary>Remove 'item' from it's parent.</summary>
		public static void Detach(this FrameworkElement item)
		{
			if (item.Parent == null)
			{
				return;
			}
			if (item.Parent is Panel panel)
			{
				panel.Children.Remove(item);
				return;
			}
			if (item.Parent is Decorator decorator)
			{
				if (decorator.Child == item)
					decorator.Child = null;
				return;
			}
			if (item.Parent is ContentPresenter presenter)
			{
				if (presenter.Content == item)
					presenter.Content = null;
				return;
			}
			if (item.Parent is ContentControl cc)
			{
				if (cc.Content == item)
					cc.Content = null;
				return;
			}
			// maybe more
			throw new Exception("Unknown parent type. Cannot Detach");
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
	}
}
