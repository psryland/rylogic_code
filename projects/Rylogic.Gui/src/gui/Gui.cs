using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rylogic.Gui2
{
	public static class Gui
	{
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

		/// <summary>Finds a parent in the visual tree matching the specified type. (and, optionally, name)</summary>
		public static T FindVisualParent<T>(this DependencyObject item, string name = null) where T : DependencyObject
		{
			for (DependencyObject parent; (parent = VisualTreeHelper.GetParent(item)) != null; item = parent)
			{
				if (parent is T && (name == null || (parent is FrameworkElement fe && fe.Name == name)))
					return (T)parent;
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
	}
}
