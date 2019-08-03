using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using Rylogic.Core.Windows;
using Rylogic.Extn;
using Rylogic.Extn.Windows;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public static partial class Gui_
	{
		/// <summary>Wrapper for DependencyProperty.Register that uses reflection to look for changed or coerce handlers</summary>
		public static DependencyProperty DPRegister<T>(string prop_name, object def = null, FrameworkPropertyMetadataOptions flags = FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
		{
			// Use:
			//  In your class with property 'prop_name':
			//  Define:
			//    private void <prop_name>_Changed() or,
			//    private void <prop_name>_Changed(<prop_type> new_value) or,
			//    private void <prop_name>_Changed(<prop_type> new_value, <prop_type> old_value)
			//    to have that method called when the property changes
			//  Define:
			//    private <prop_type> <prop_name>_Coerce(<prop_type> value)
			//    to have values coerced (i.e. massaged into a valid value).
			//  Define:
			//    private static bool <prop_name>_Validate(<prop_type> value)
			//    to have values validated (has to be static, if you need per-binding validation
			//    var binding = BindingOperations.GetBinding(<control>, ComboBox.DepProperty);
			//    binding.ValidationRules.Clear(); etc).

			// Don't set 'DefaultValue' unless 'def' is non-null, because the property type
			// may not be a reference type, and 'null' may not be a valid default value.
			var meta = new FrameworkPropertyMetadata(null, flags);

			// Determine the type of the property
			// (Note: Null exception here means you've used 'nameof(CheeseProperty)' instead of 'nameof(Cheese)' for prop_name)
			var prop_type = typeof(T).GetProperty(prop_name).PropertyType;
			meta.DefaultValue = def ?? (prop_type.IsValueType ? Activator.CreateInstance(prop_type) : null);

			// If the type defines a Changed handler, add a callback
			var changed_handler = typeof(T).GetMethod($"{prop_name}_Changed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (changed_handler != null)
			{
				var param_count = changed_handler.GetParameters().Length;
				switch (param_count)
				{
				default: throw new Exception($"Incorrect function signature for handler {prop_name}_Changed");
				case 2: meta.PropertyChangedCallback = (d, e) => changed_handler.Invoke(d, new object[] { e.NewValue, e.OldValue }); break;
				case 1: meta.PropertyChangedCallback = (d, e) => changed_handler.Invoke(d, new object[] { e.NewValue }); break;
				case 0: meta.PropertyChangedCallback = (d, e) => changed_handler.Invoke(d, null); break;
				}
			}

			// If the type defines a Coerce handler, add a callback
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

			// IF the type defines a Validation handler
			var validate_cb = (ValidateValueCallback)null;
			var validation_handler = typeof(T).GetMethod($"{prop_name}_Validate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (validation_handler != null)
			{
				validate_cb = new ValidateValueCallback((x) => (bool)validation_handler.Invoke(null, new object[] { x }));
			}

			// Register the property
			return DependencyProperty.Register(prop_name, prop_type, typeof(T), meta, validate_cb);
		}

		/// <summary>Wrapper for DependencyProperty.RegisterAttached that uses reflection to look for changed or coerce handlers</summary>
		public static DependencyProperty DPRegisterAttached<T>(string prop_name, object def = null, FrameworkPropertyMetadataOptions flags = FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
		{
			// Use:
			//  In your class with property 'prop_name':
			//  Define:
			//    private static void <prop_name>_Changed(DependencyObject obj) or,
			//    private static void <prop_name>_Changed(DependencyObject obj, <prop_type> new_value) or,
			//    private static void <prop_name>_Changed(DependencyObject obj, <prop_type> new_value, <prop_type> old_value)
			//    to have that method called when the property changes
			//  Define:
			//    private static <prop_type> <prop_name>_Coerce(<prop_type> value)
			//    to have values coerced (i.e. massaged into a valid value).

			// Don't set 'DefaultValue' unless 'def' is non-null, because the property type
			// may not be a reference type, and 'null' may not be a valid default value.
			var meta = new FrameworkPropertyMetadata(null, flags);

			// Determine the type of the property
			var prop_type = typeof(T).GetMethod($"Get{prop_name}", BindingFlags.Static | BindingFlags.Public).ReturnType;
			meta.DefaultValue = def ?? (prop_type.IsValueType ? Activator.CreateInstance(prop_type) : null);

			// If the type defines a Changed handler, add a callback
			var changed_handler = typeof(T).GetMethod($"{prop_name}_Changed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (changed_handler != null)
			{
				var param_count = changed_handler.GetParameters().Length;
				switch (param_count)
				{
				default: throw new Exception($"Incorrect function signature for handler {prop_name}_Changed");
				case 3: meta.PropertyChangedCallback = (d, e) => changed_handler.Invoke(null, new object[] { d, e.NewValue, e.OldValue }); break;
				case 2: meta.PropertyChangedCallback = (d, e) => changed_handler.Invoke(null, new object[] { d, e.NewValue }); break;
				case 1: meta.PropertyChangedCallback = (d, e) => changed_handler.Invoke(null, new object[] { d }); break;
				}
			}

			// If the type defines a Validate handle, add a callback
			var coerce_handler = typeof(T).GetMethod($"{prop_name}_Coerce", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (coerce_handler != null)
			{
				var param_count = coerce_handler.GetParameters().Length;
				switch (param_count)
				{
				default: throw new Exception($"Incorrect function signature for handler {prop_name}_Coerce");
				case 2: meta.CoerceValueCallback = (d, v) => coerce_handler.Invoke(null, new object[] { d, v }); break;
				}
			}
			
			// Register the attached property using the return type of 'Get<prop_name>'
			return DependencyProperty.RegisterAttached(prop_name, prop_type, typeof(T), meta);
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
		public static T FindVisualChild<T>(this DependencyObject parent, Func<T, bool> pred = null) where T : DependencyObject
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
		public static DependencyObject FindVisualChild(this DependencyObject parent, Func<DependencyObject, bool> pred = null)
		{
			return FindVisualChild<DependencyObject>(parent, pred);
		}

		/// <summary>
		/// Finds a parent in the visual tree matching the specified type.
		/// If 'pred' is a filter, typically used to find parents by name.
		/// if 'root' is given, the search stops if 'root' is encountered (after testing if it's a parent)</summary>
		public static T FindVisualParent<T>(this DependencyObject item, Func<T, bool> pred = null, DependencyObject root = null) where T : DependencyObject
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
		public static DependencyObject FindVisualParent(this DependencyObject item, Func<DependencyObject, bool> pred = null, DependencyObject root = null)
		{
			return FindVisualParent<DependencyObject>(item, pred, root);
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
			for (; s != send && items[s] is FrameworkElement fe && fe.Visibility == Visibility.Collapsed; ++s) { }
			for (; e != eend && items[e] is FrameworkElement fe && fe.Visibility == Visibility.Collapsed; --e) { }

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

		/// <summary>Set the position of this window Note: Fluent return allows 'SetLocation(x,y).OnScreen()'</summary>
		public static Window SetLocation(this Window wnd, Point pt)
		{
			return SetLocation(wnd, pt.X, pt.Y);
		}
		public static Window SetLocation(this Window wnd, double left, double top)
		{
			wnd.Left = left;
			wnd.Top = top;
			return wnd;
		}

		/// <summary>True if this window was shown using ShowDialog</summary>
		public static bool IsModal(this Window window)
		{
			return (bool)m_fi_showingAsDialog.GetValue(window);
		}
		private static FieldInfo m_fi_showingAsDialog = typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>Show the folder browser dialog</summary>
		public static bool ShowDialog(this OpenFolderUI dlg, DependencyObject dep)
		{
			var hwnd = dep != null ? ((HwndSource)PresentationSource.FromDependencyObject(dep)).Handle : IntPtr.Zero;
			return dlg.ShowDialog(hwnd);
		}
		public static bool? ShowDialog(this CommonDialog dlg, DependencyObject dep)
		{
			return dlg.ShowDialog(Window.GetWindow(dep));
		}
	}
}
