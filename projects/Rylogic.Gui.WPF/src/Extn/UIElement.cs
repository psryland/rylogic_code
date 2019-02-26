using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public static partial class Gui_
	{
		/// <summary>Recursively enumerates all visual children of this DependencyObject (depth first)</summary>
		public static IEnumerable<DependencyObject> AllVisualChildren(this DependencyObject parent)
		{
			for (int i = 0, iend = VisualTreeHelper.GetChildrenCount(parent); i != iend; ++i)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				foreach (var c in child.AllVisualChildren())
					yield return c;
			}
			yield return parent;
		}

		/// <summary>Recursively enumerates all logical children of this DependencyObject (depth first)</summary>
		public static IEnumerable<DependencyObject> AllLogicalChildren(this DependencyObject parent)
		{
			foreach (var child in LogicalTreeHelper.GetChildren(parent).Cast<DependencyObject>())
			{
				foreach (var c in child.AllLogicalChildren())
					yield return c;
			}
			yield return parent;
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
			if (item == null)
				return null;
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

		/// <summary>Convert text to formatted text based on this control's font settings</summary>
		public static FormattedText ToFormattedText(this Control ui, string text, double emSize = 12.0, Brush brush = null)
		{
			var tf = ui.Typeface();
			return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, emSize, brush ?? Brushes.Black, 96.0);
		}
	}
}
