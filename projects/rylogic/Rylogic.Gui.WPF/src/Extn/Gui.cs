﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using Rylogic.Windows;
using Rylogic.Windows.Extn;
using Rylogic.Gfx;
using Rylogic.Interop.Win32;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public static partial class Gui_
	{
		// Notes:
		//  - The Logical Tree is just the tree hierarchy given in the xaml
		//  - The Visual Tree is the actual hierarchy of rendered components, e.g. borders, etc.

		[Flags]
		public enum EDPFlags
		{
			/// <summary>No options are specified; the dependency property uses the default behavior of the Windows Presentation Foundation (WPF) property system.</summary>
			None = FrameworkPropertyMetadataOptions.None,

			/// <summary>The measure pass of layout compositions is affected by value changes to this dependency property.</summary>
			AffectsMeasure = FrameworkPropertyMetadataOptions.AffectsMeasure,

			/// <summary>The arrange pass of layout composition is affected by value changes to this dependency property.</summary>
			AffectsArrange = FrameworkPropertyMetadataOptions.AffectsArrange,

			/// <summary>The measure pass on the parent element is affected by value changes to this dependency property.</summary>
			AffectsParentMeasure = FrameworkPropertyMetadataOptions.AffectsParentMeasure,

			/// <summary>The arrange pass on the parent element is affected by value changes to this dependency property.</summary>
			AffectsParentArrange = FrameworkPropertyMetadataOptions.AffectsParentArrange,

			/// <summary>Some aspect of rendering or layout composition (other than measure or arrange) is affected by value changes to this dependency property.</summary>
			AffectsRender = FrameworkPropertyMetadataOptions.AffectsRender,

			/// <summary>The values of this dependency property are inherited by child elements.</summary>
			Inherits = FrameworkPropertyMetadataOptions.Inherits,

			/// <summary>The values of this dependency property span separated trees for purposes of property value inheritance.</summary>
			OverridesInheritanceBehavior = FrameworkPropertyMetadataOptions.OverridesInheritanceBehavior,

			/// <summary>Data binding to this dependency property is not allowed.</summary>
			NotDataBindable = FrameworkPropertyMetadataOptions.NotDataBindable,

			/// <summary>The System.Windows.Data.BindingMode for data bindings on this dependency property defaults to System.Windows.Data.BindingMode.TwoWay.</summary>
			TwoWay = FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,

			/// <summary>The values of this dependency property should be saved or restored by journaling processes, or when navigating by Uniform resource identifiers (URIs).</summary>
			Journal = FrameworkPropertyMetadataOptions.Journal,

			/// <summary>The sub-properties on the value of this dependency property do not affect any aspect of rendering.</summary>
			SubPropertiesDoNotAffectRender = FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender,
		}

		/// <summary>Framework property meta data</summary>
		private class DPMeta :FrameworkPropertyMetadata
		{
			public DPMeta(Type prop_type, object? def, FrameworkPropertyMetadataOptions flags = FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, UpdateSourceTrigger upd = UpdateSourceTrigger.PropertyChanged)
				:base(def, flags)
			{
				PropType = prop_type;
				DefaultUpdateSourceTrigger = upd;
			}

			/// <summary>The type of the dependency property</summary>
			public Type PropType { get; }

			/// <summary>Property value validation handler</summary>
			public ValidateValueCallback? Validate { get; set; }
		}

		/// <summary>Create framework property metadata for the given property name</summary>
		private static DPMeta DPMetaData(bool is_prop, Type class_type, string prop_name, object? def = null, FrameworkPropertyMetadataOptions flags = FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, UpdateSourceTrigger upd = UpdateSourceTrigger.PropertyChanged)
		{
			// Determine the type of the property
			// (Note: Null exception here means you've used 'nameof(CheeseProperty)' instead of 'nameof(Cheese)' for prop_name)
			var prop_type = is_prop
				? (class_type.GetProperty(prop_name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)?.PropertyType ?? throw new ArgumentNullException($"Property {prop_name} not found on {class_type.Name}"))
				: (class_type.GetMethod($"Get{prop_name}", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)?.ReturnType ?? throw new ArgumentNullException($"Method Get{prop_name} not found on {class_type.Name}"));

			// Set the default value
			def ??= (prop_type.IsValueType ? Activator.CreateInstance(prop_type) : null);

			// Don't set 'DefaultValue' unless 'def' is non-null, because the property type
			// may not be a reference type, and 'null' may not be a valid default value.
			var meta = new DPMeta(prop_type, def, flags, upd);

			// If the type defines a Changed handler, add a callback
			if (class_type.GetMethod($"{prop_name}_Changed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is MethodInfo changed_handler)
			{
				var param_count = changed_handler.GetParameters().Length;
				meta.PropertyChangedCallback = param_count switch
				{
					2 => (d, e) => changed_handler.Invoke(d, new object[] { e.NewValue, e.OldValue }),
					1 => (d, e) => changed_handler.Invoke(d, new object[] { e.NewValue }),
					0 => (d, e) => changed_handler.Invoke(d, null),
					_ => throw new Exception($"Incorrect function signature for handler {prop_name}_Changed"),
				};
			}
			if (class_type.GetMethod($"{prop_name}_Changed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) is MethodInfo changed_handler_s)
			{
				var param_count = changed_handler_s.GetParameters().Length;
				meta.PropertyChangedCallback = param_count switch
				{
					3 => (d, e) => changed_handler_s.Invoke(null, new object[] { d, e.NewValue, e.OldValue }),
					2 => (d, e) => changed_handler_s.Invoke(null, new object[] { d, e.NewValue }),
					1 => (d, e) => changed_handler_s.Invoke(null, new object[] { d }),
					_ => throw new Exception($"Incorrect function signature for handler {prop_name}_Changed"),
				};
			}

			// If the type defines a Coerce handler, add a callback
			if (class_type.GetMethod($"{prop_name}_Coerce", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is MethodInfo coerce_handler)
			{
				var param_count = coerce_handler.GetParameters().Length;
				meta.CoerceValueCallback = param_count switch
				{
					1 => (d, v) => coerce_handler.Invoke(d, new object[] { v }),
					_ => throw new Exception($"Incorrect function signature for handler {prop_name}_Coerce"),
				};
			}
			if (class_type.GetMethod($"{prop_name}_Coerce", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) is MethodInfo coerce_handler_s)
			{
				var param_count = coerce_handler_s.GetParameters().Length;
				meta.CoerceValueCallback = param_count switch
				{
					2 => (d, v) => coerce_handler_s.Invoke(null, new object[] { d, v }),
					_ => throw new Exception($"Incorrect function signature for handler {prop_name}_Coerce"),
				};
			}

			// IF the type defines a Validation handler
			if (class_type.GetMethod($"{prop_name}_Validate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) is MethodInfo validation_handler)
			{
				meta.Validate = new ValidateValueCallback((x) => (bool?)validation_handler.Invoke(null, new object[] { x }) ?? false);
			}

			// Return the meta data
			return meta;
		}

		/// <summary>Wrapper for DependencyProperty.Register that uses reflection to look for changed or coerce handlers</summary>
		public static DependencyProperty DPRegister<T>(string prop_name, object? def, EDPFlags flags, UpdateSourceTrigger upd = UpdateSourceTrigger.PropertyChanged) => DPRegister(typeof(T), prop_name, def, flags, upd);
		public static DependencyProperty DPRegister(Type class_type, string prop_name, object? def, EDPFlags flags, UpdateSourceTrigger upd = UpdateSourceTrigger.PropertyChanged)
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

			// Get the meta data
			var meta = DPMetaData(true, class_type, prop_name, def, (FrameworkPropertyMetadataOptions)flags, upd);

			// Register the property
			return DependencyProperty.Register(prop_name, meta.PropType, class_type, meta, meta.Validate);
		}

		/// <summary>Wrapper for DependencyProperty.RegisterAttached that uses reflection to look for changed or coerce handlers</summary>
		public static DependencyProperty DPRegisterAttached<T>(string prop_name, object? def, EDPFlags flags, UpdateSourceTrigger upd = UpdateSourceTrigger.PropertyChanged) => DPRegisterAttached(typeof(T), prop_name, def, flags, upd);
		public static DependencyProperty DPRegisterAttached(Type class_type, string prop_name, object? def, EDPFlags flags, UpdateSourceTrigger upd = UpdateSourceTrigger.PropertyChanged)
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
			var meta = DPMetaData(false, class_type, prop_name, def, (FrameworkPropertyMetadataOptions)flags, upd);

			// Register the attached property using the return type of 'Get<prop_name>'
			return DependencyProperty.RegisterAttached(prop_name, meta.PropType, class_type, meta);
		}

		/// <summary>Wrapper for 'DependencyProperty.AddOwner' that uses reflection to look for changed or coerce handlers</summary>
		public static DependencyProperty AddOwner<T>(this DependencyProperty dp, string prop_name, object? def = null) => AddOwner(dp, typeof(T), prop_name, def);
		public static DependencyProperty AddOwner(this DependencyProperty dp, Type class_type, string prop_name, object? def = null)
		{
			var meta = DPMetaData(true, class_type, prop_name, def);
			return dp.AddOwner(class_type, meta);
		}

		/// <summary>Attached to the Closed event of a window to clean up any child objects that are disposable</summary>
		public static void DisposeChildren(object? sender, EventArgs e) => DisposeChildren(sender as DependencyObject);
		public static void DisposeChildren(DependencyObject? obj)
		{
			if (obj == null) return;
			foreach (var child in obj.AllLogicalChildren().OfType<IDisposable>())
				child.Dispose();
		}

		/// <summary>Return the virtual screen rectangle (in DIP)</summary>
		public static Rect VirtualScreenRect()
		{
			return new Rect(
				SystemParameters.VirtualScreenLeft,
				SystemParameters.VirtualScreenTop,
				SystemParameters.VirtualScreenWidth,
				SystemParameters.VirtualScreenHeight);
		}

		/// <summary>Return the monitor rectangle (in DIP)</summary>
		public static Rect MonitorRect(IntPtr monitor)
		{
			var mon = User32.GetMonitorInfo(monitor);
			var dip = 96.0 / Dpi.DpiForMonitor(monitor);
			return new Rect(
				mon.rcWork.left * dip,
				mon.rcWork.top * dip,
				mon.rcWork.width * dip,
				mon.rcWork.height * dip);
		}

		/// <summary>Move a screen-space rectangle so that it is within the virtual screen</summary>
		public static Rect OnScreen(Rect rect)
		{
			var scn = VirtualScreenRect();

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

		/// <summary>Move a screen-space rectangle so that it is within the area of a monitor</summary>
		public static Rect OnMonitor(IntPtr monitor, Rect rect)
		{
			var scn = MonitorRect(monitor);

			var r = rect;
			if (rect.Right > scn.Right) r.X = scn.Right - rect.Width;
			if (rect.Bottom > scn.Bottom) r.Y = scn.Bottom - rect.Height;
			if (r.Left < scn.Left) r.X = scn.Left;
			if (r.Top < scn.Top) r.Y = scn.Top;
			return r;
		}
		public static Point OnMonitor(IntPtr monitor, Point location, Size size)
		{
			return OnMonitor(monitor, new Rect(location, size)).Location;
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

		/// <summary>Ensure 'item' is a child of 'panel', removing from its existing parent and adding if needed</summary>
		public static T Adopt<T>(this Panel panel, T item) where T:FrameworkElement
		{
			if (item.Parent is Panel parent)
			{
				// Already a child.
				if (parent == panel)
					return item;

				// Child of a different panel
				parent.Children.Remove(item);
			}

			// Add 'item' as a child of 'panel'
			return panel.Children.Add2(item);
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

		/// <summary>Finds a child in the visual tree matching the specified type (and, optionally, predicate). Depth-first search</summary>
		public static T? FindVisualChild<T>(this DependencyObject parent, Func<T, bool>? pred = null)
			where T : DependencyObject
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
		public static DependencyObject? FindVisualChild(this DependencyObject parent, Func<DependencyObject, bool>? pred = null)
		{
			return FindVisualChild<DependencyObject>(parent, pred);
		}

		/// <summary>Finds a child in the logical tree matching the specified type (and, optionally, predicate). Depth-first search</summary>
		public static T? FindLogicalChild<T>(this DependencyObject parent, Func<T, bool>? pred = null)
			where T : DependencyObject
		{
			foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<T>())
			{
				// Check each child
				if (child is T tchild && (pred == null || pred(tchild)))
					return tchild;

				// Recurse into children
				var item = child.FindLogicalChild<T>(pred);
				if (item != null)
					return item;
			}
			return null;
		}
		public static DependencyObject? FindLogicalChild(this DependencyObject parent, Func<DependencyObject, bool>? pred = null)
		{
			return FindLogicalChild<DependencyObject>(parent, pred);
		}

		/// <summary>
		/// Finds a parent in the visual tree matching the specified type.
		/// If 'pred' is a filter, typically used to find parents by name.
		/// if 'root' is given, the search stops if 'root' is encountered (after testing if it's a parent)</summary>
		public static T? FindVisualParent<T>(this DependencyObject item, Func<T, bool>? pred = null, DependencyObject? root = null)
			where T : DependencyObject
		{
			if (item == null)
				return null;
			if (item is not Visual && item is not Visual3D)
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
		public static DependencyObject? FindVisualParent(this DependencyObject item, Func<DependencyObject, bool>? pred = null, DependencyObject? root = null)
		{
			return FindVisualParent<DependencyObject>(item, pred, root);
		}

		/// <summary>
		/// Finds a parent in the logical tree matching the specified type.
		/// If 'pred' is a filter, typically used to find parents by name.
		/// if 'root' is given, the search stops if 'root' is encountered (after testing if it's a parent)</summary>
		public static T? FindLogicalParent<T>(this DependencyObject item, Func<T, bool>? pred = null, DependencyObject? root = null)
			where T : DependencyObject
		{
			if (item == null)
				return null;
			if (ReferenceEquals(item, root))
				return item as T;

			for (DependencyObject parent; (parent = LogicalTreeHelper.GetParent(item)) != null; item = parent)
			{
				if (parent is T tparent && (pred == null || pred(tparent)))
					return tparent;
				if (ReferenceEquals(parent, root))
					break;
			}
			return null;
		}
		public static DependencyObject? FindLogicalParent(this DependencyObject item, Func<DependencyObject, bool>? pred = null, DependencyObject? root = null)
		{
			return FindLogicalParent<DependencyObject>(item, pred, root);
		}

		/// <summary>Find the item at the root of the visual tree</summary>
		public static DependencyObject? FindVisualRoot(this DependencyObject item)
		{
			if (item == null)
				return null;

			for (DependencyObject parent; (parent = VisualTreeHelper.GetParent(item)) != null; item = parent) {}
			return item;
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
		public static FormattedText ToFormattedText(this Control ui, string text, double emSize = 12.0, Brush? brush = null)
		{
			var tf = ui.Typeface();
			return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, emSize, brush ?? Brushes.Black, 96.0);
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

		/// <summary>Return an RAII object for scoped mouse capture</summary>
		public static IDisposable CaptureMouseScope(this UIElement ui)
		{
			return Scope.Create(
				() => ui.CaptureMouse(),
				_ =>
				{
					if (Util.IsGCFinalizerThread) throw new Exception("Mouse capture shouldn't be cleaned up by the GC");
					if (!ui.IsMouseCaptured) return;
					ui.ReleaseMouseCapture();
				});
		}

		/// <summary>Convert a point from 'src' space to 'dst' space. Consider using 'TransformToAncestor/Descendant'</summary>
		public static Point MapPoint(Visual src, Visual dst, Point pt)
		{
			var p0 = src.PointToScreen(pt);
			var p1 = dst.PointFromScreen(p0);
			return p1;
		}
		public static System.Drawing.PointF MapPoint(Visual src, Visual dst, System.Drawing.PointF pt)
		{
			return MapPoint(src, dst, pt.ToPointD()).ToPointF();
		}

		/// <summary>Converts a Rect from screen space to the space of this visual</summary>
		public static Rect RectFromScreen(this Visual vis, Rect rect)
		{
			// if 'vis' is not connected to a presentation source, its position on screen is undefined.
			var pt = PresentationSource.FromVisual(vis) != null
				? vis.PointFromScreen(rect.TopLeft)
				: rect.TopLeft; // This should really be an error...

			return new Rect(pt, new Size(rect.Width, rect.Height));
		}

		/// <summary>Converts a Rect to screen space from the space of this visual</summary>
		public static Rect RectToScreen(this Visual vis, Rect rect)
		{
			// if 'vis' is not connected to a presentation source, its position on screen is undefined.
			var pt = PresentationSource.FromVisual(vis) != null
				? vis.PointToScreen(rect.TopLeft)
				: rect.TopLeft; // This should really be an error...
			
			return new Rect(pt, new Size(rect.Width, rect.Height));
		}

		/// <summary>Returns new Rect(0,0,RenderSize)</summary>
		public static Rect RenderArea(this UIElement vis, UIElement? relative_to_ancestor = null)
		{
			var pt = new Point();
			if (relative_to_ancestor != null && relative_to_ancestor.IsAncestorOf(vis))
				pt = vis.TransformToAncestor(relative_to_ancestor).Transform(pt);
			return new Rect(pt, vis.RenderSize);
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

		/// <summary>Return the mouse position relative to a given UI element allowing for DPI scaling</summary>
		public static Point GetPositionPixels(this MouseEventArgs args, IInputElement input)
		{
			var pt = args.GetPosition(input);
			if (input is DependencyObject obj)
			{
				var dpi = Dpi.DpiForWindow(obj.Hwnd());
				pt.X *= dpi / 96.0;
				pt.Y *= dpi / 96.0;
			}
			return pt;
		}

		/// <summary>True if this window was shown using ShowDialog</summary>
		public static bool IsModal(this Window window)
		{
			return (bool)m_fi_showing_as_dialog.GetValue(window)!;
		}
		private static readonly FieldInfo m_fi_showing_as_dialog = typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new Exception("_showingAsDialog field not found");

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
