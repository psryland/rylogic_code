using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Rylogic.Gfx;

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
			foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
			{
				foreach (var c in child.AllLogicalChildren())
					yield return c;
			}
			yield return parent;
		}

		/// <summary>Finds a child in the visual tree matching the specified type (and, optionally, name). Depth-first search</summary>
		public static T FindVisualChild<T>(this DependencyObject parent, Func<T,bool> pred = null) where T : DependencyObject
		{
			for (int i = 0, iend = VisualTreeHelper.GetChildrenCount(parent); i != iend; ++i)
			{
				// Check each child
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is T tchild && (pred == null || pred(tchild)))
					return tchild;

				// Recurse into children
				var item = child.FindVisualChild<T>(pred);
				if (item != null)
					return item;
			}
			return null;
		}

		/// <summary>
		/// Finds a parent in the visual tree matching the specified type.
		/// If 'pred' is a filter, typically used to find parents by name.
		/// if 'root' is given, the search stops if 'root' is encountered (after testing if it's a parent)</summary>
		public static T FindVisualParent<T>(this DependencyObject item, Func<T,bool> pred = null, DependencyObject root = null) where T : DependencyObject
		{
			if (item == null)
				return null;
			if (ReferenceEquals(item, root))
				return item as T;

			for (DependencyObject parent; (parent = VisualTreeHelper.GetParent(item)) != null; item = parent)
			{
				if (parent is T tparent && (pred == null || pred(tparent)))
					return tparent;
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

		/// <summary>Set the render target size of this D3D11Image from the given visual</summary>
		public static void SetRenderTargetSize(this D3D11Image d3d_image, FrameworkElement element)
		{
			// Set the screen render target size, accounting for DPI
			var win = PresentationSource.FromVisual(element)?.CompositionTarget as HwndTarget;
			var dpi_scale = win?.TransformToDevice.M11 ?? 1.0;

			// Determine the texture size
			var width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth * dpi_scale));
			var height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight * dpi_scale));

			d3d_image.SetRenderTargetSize(width, height);
		}

		/// <summary>Hide successive or start/end separators</summary>
		public static void TidySeparators(this ItemCollection items, bool recursive = true)
		{
			// Note: Not using MenuItem.IsVisible because that also includes the parent's
			// visibility (which during construction is usually false).
			int s = 0, e = items.Count - 1, send = items.Count, eend = -1;

			// Find the first and last non-collapsed item
			for (; s != send && items[s] is FrameworkElement fe && fe.Visibility == Visibility.Collapsed; ++s) {}
			for (; e != eend && items[e] is FrameworkElement fe && fe.Visibility == Visibility.Collapsed; --e) {}

			// Hide starting separators
			for (; s != e + 1 && items[s] is Separator sep; ++s)
				sep.Visibility = Visibility.Collapsed;

			// Hide ending separators
			for (; e != s - 1 && items[e] is Separator sep; --e)
				sep.Visibility = Visibility.Collapsed;

			// Hide successive separators
			for (int i = s + 1; i < e; ++i)
			{
				if (!(items[i] is Separator sep)) continue;

				// Make the first separator in the contiguous sequence visible
				sep.Visibility = Visibility.Visible;
				for (int j = i + 1; j < e; i = j, ++j)
				{
					if (items[j] is FrameworkElement fe && fe.Visibility == Visibility.Collapsed) continue;
					if (items[j] is Separator sep2) { sep2.Visibility = Visibility.Collapsed; continue; }
					break;
				}
			}

			// Tidy sub menus as well
			if (recursive)
			{
				foreach (var item in items.OfType<MenuItem>())
					item.Items.TidySeparators(recursive);
			}
		}

		/// <summary>Hide successive or start/end separators. Attached this to ContextMenu.Opened or MenuItem.SubmenuOpened</summary>
		public static void TidySeparators(object sender, EventArgs args)
		{
			var cmenu = (ContextMenu)sender;
			cmenu.Items.TidySeparators();
		}
	}
}
