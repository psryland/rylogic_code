﻿//***************************************************
// Control Extensions
//  Copyright (c) Rylogic Ltd 2010
//***************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.maths;
using pr.util;

namespace pr.extn
{
	public static class ControlExtensions
	{
		/// <summary>Tests if this component is being used by the VS designer</summary>
		public static bool IsInDesignMode<TComponent>(this TComponent c) where TComponent:Component
		{
			if (Util.IsInDesignMode)
				return true;

			// Test the protected DesignMode component property
			if ((bool)DesignModeProp.GetValue(c, null))
				return true;

			// 'DesignMode' doesn't work in constructors or grandchild controls... test recursively
			var ctrl = c as Control;
			if (ctrl != null && ctrl.Parent != null && ctrl.Parent.IsInDesignMode())
				return true;

			return false;
		}
		private static PropertyInfo DesignModeProp
		{
			get
			{
				if (m_impl_DesignModeProp == null)
				{
					m_impl_DesignModeProp = typeof(Component).GetProperty("DesignMode", BindingFlags.NonPublic|BindingFlags.Instance);
					if (m_impl_DesignModeProp == null) throw new Exception("Component.DesignMode property missing");
				}
				return m_impl_DesignModeProp;
			}
		}
		private static PropertyInfo m_impl_DesignModeProp;

		/// <summary>Wrapper of begin invoke that takes a lambda</summary>
		public static IAsyncResult BeginInvoke(this Form form, Action action)
		{
			// Has to be form or the overload isn't found
			// Note, the params overload isn't needed because 'action' is a lambda
			return form.BeginInvoke(action);
		}
		public static IAsyncResult BeginInvoke(this Control ctrl, Action action)
		{
			// Note, the params overload isn't needed because 'action' is a lambda
			return ctrl.BeginInvoke(action);
		}

		/// <summary>Invoke that takes a lambda</summary>
		public static object Invoke(this Form form, Action action)
		{
			// Has to be form or the overload isn't found
			// Note, the params overload isn't needed because 'action' is a lambda
			return form.Invoke(action);
		}
		public static object Invoke(this Control ctrl, Action action)
		{
			// Note, the params overload isn't needed because 'action' is a lambda
			return ctrl.Invoke(action);
		}

		/// <summary>Return 'Tag' for this control as type 'T'. Creates a new T if Tag is currently null</summary>
		public static T TagAs<T>(this Control ctrl) where T:new()
		{
			return (T)(ctrl.Tag ?? (ctrl.Tag = new T()));
		}

		/// <summary>Set the tooltip for this control</summary>
		public static void ToolTip(this Control ctrl, ToolTip tt, string caption)
		{
			tt.SetToolTip(ctrl, caption);
		}

		/// <summary>Display a hint balloon.</summary>
		public static void ShowHintBalloon(this Control ctrl, ToolTip tt, string msg, int duration = 5000)
		{
			//var parent = ctrl.FindForm();
			//if (parent == null) return;
			var pt = ctrl.ClientRectangle.Centre();

			tt.SetToolTip(ctrl, msg);
			tt.Show(msg, ctrl, pt, duration);
			tt.BeginInvokeDelayed(duration, () => tt.SetToolTip(ctrl, null));
			//tt.Popup += (s,a) => tt.SetToolTip(ctrl,null);
		}

		/// <summary>Returns the location of this item in screen space</summary>
		public static Point ScreenLocation(this Control item)
		{
			var parent = item.Parent;
			if (parent == null) return item.Bounds.Location;
			return parent.PointToScreen(item.Bounds.Location);
		}

		/// <summary>Returns the bounds of this item in form space</summary>
		public static Rectangle ParentFormRectangle(this Control item)
		{
			var parent = item.Parent;
			var top    = item.TopLevelControl;
			var srect  = parent == null ? item.Bounds : parent.RectangleToScreen(item.Bounds);
			return top != null ? top.RectangleToClient(srect) : srect;
		}

		/// <summary>Returns the bounds of this item in screen space</summary>
		public static Rectangle ScreenRectangle(this Control item)
		{
			var parent = item.Parent;
			if (parent == null) return item.Bounds;
			return parent.RectangleToScreen(item.Bounds);
		}

		/// <summary>BeginInvoke an action after 'delay' milliseconds (roughly)</summary>
		public static void BeginInvokeDelayed(this IComponent ctrl, int delay, Action action)
		{
			new Timer{Enabled = true, Interval = Math.Max(delay,1)}.Tick += (s,a) =>
			{
				var timer = (Timer)s;
				timer.Enabled = false;
				timer.Dispose();
				action();
			};
		}

		/// <summary>Enable double buffering for the control. Note, it's probably better to subclass the control to turn this on</summary>
		public static void DoubleBuffered(this Control ctrl, bool state)
		{
			PropertyInfo pi = ctrl.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance|BindingFlags.NonPublic);
			pi.SetValue(ctrl, state, null);
			MethodInfo mi = ctrl.GetType().GetMethod("SetStyle", BindingFlags.Instance|BindingFlags.NonPublic);
			mi.Invoke(ctrl, new object[]{ControlStyles.DoubleBuffer|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint, state});
		}

		/// <summary>Returns an RAII scope for suspending layout</summary>
		public static Scope SuspendLayout(this Control ctrl, bool layout_on_resume)
		{
			return Scope.Create(ctrl.SuspendLayout, () => ctrl.ResumeLayout(layout_on_resume));
		}

		/// <summary>Block redrawing of the control</summary>
		public static Scope SuspendRedraw(this Control ctrl, bool refresh_on_resume)
		{
			return Scope.Create(
				() => Win32.SendMessage(ctrl.Handle, Win32.WM_SETREDRAW, 0, 0),
				() =>
					{
						Win32.SendMessage(ctrl.Handle, Win32.WM_SETREDRAW, 1, 0);
						if (refresh_on_resume) ctrl.Refresh();
					});
		}

		/// <summary>Recursively calls 'GetChildAtPoint' until the control with no children at that point is found</summary>
		public static Control GetChildAtScreenPointRec(this Control ctrl, Point screen_pt)
		{
			Control parent = null;
			for (var child = ctrl; child != null;)
			{
				parent = child;
				child = parent.GetChildAtPoint(parent.PointToClient(screen_pt));
			}
			return parent;
		}

		/// <summary>Recursively calls 'GetChildAtPoint' until the control with no children at that point is found</summary>
		public static Control GetChildAtPointRec(this Control ctrl, Point pt)
		{
			return GetChildAtScreenPointRec(ctrl, ctrl.PointToScreen(pt));
		}

		/// <summary>Returns the coordinates of the control's form-relative location.</summary>
		public static Rectangle FormRelativeCoordinates(this Control control)
		{
			var form = (Form)control.TopLevelControl;
			if (form == null) throw new NullReferenceException("Control does not have a top level control (Form)");
			return control == form ? form.ClientRectangle : form.RectangleToClient(control.Parent.RectangleToScreen(control.Bounds));
		}

		/// <summary>Set click through mode for this window</summary>
		public static void ClickThruEnable(this Control control, bool enabled)
		{
			uint style = Win32.GetWindowLong(control.Handle, Win32.GWL_EXSTYLE);
			style = enabled
				? Bit.SetBits(style, Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT, true)
				: Bit.SetBits(style, Win32.WS_EX_TRANSPARENT, false);
			Win32.SetWindowLong(control.Handle, Win32.GWL_EXSTYLE, style);
		}

		/// <summary>Return a bitmap of this control</summary>
		public static Bitmap ToBitmap(this Control control)
		{
			var bm = new Bitmap(control.Width, control.Height);
			control.DrawToBitmap(bm, bm.Size.ToRect());
			return bm;
		}

		/// <summary>Exports location data for this control</summary>
		public static ControlLocations SaveLocations(this Control cont)
		{
			return new ControlLocations(cont);
		}

		/// <summary>Imports location data for this control</summary>
		public static void LoadLocations(this Control cont, ControlLocations data)
		{
			data.Apply(cont);
		}

		/// <summary>Temporarily change the cursor while over this control</summary>
		public static Scope ChangeCursor(this Control ctrl, Cursor new_cursor)
		{
			var old_cursor = ctrl.Cursor;
			return Scope.Create(
				() => ctrl.Cursor = new_cursor,
				() => ctrl.Cursor = old_cursor);
		}
	}

	/// <summary>Used to persist control locations and sizes in xml</summary>
	public class ControlLocations
	{
		// Produces this:
		//  <ctrl>
		//    <name/>
		//    <location/>
		//    <size/>
		//    <ctrl/>
		//    <ctrl/>
		//    ...
		//  </ctrl>

		private static class Tag
		{
			public const string Ctrl     = "ctrl";
			public const string Name     = "name";
			public const string Loc      = "loc";
			public const string Size     = "size";
			public const string Children = "children";
		}

		private string m_name;
		private Point  m_location;
		private Size   m_size;
		private Dictionary<string, ControlLocations> m_children;

		public ControlLocations()
		{
			m_name     = string.Empty;
			m_location = Point.Empty;
			m_size     = Size.Empty;
			m_children = new Dictionary<string,ControlLocations>();
		}
		public ControlLocations(Control ctrl, int level = 0, int index = 0) :this()
		{
			ReadInternal(ctrl, level, index);
		}
		public ControlLocations(XElement node)
		{
			m_name     = node.Element(Tag.Name).As<string>();
			m_location = node.Element(Tag.Loc ).As<Point>();
			m_size     = node.Element(Tag.Size).As<Size>();
			m_children = new Dictionary<string, ControlLocations>();
			foreach (var child in node.Elements(Tag.Ctrl))
			{
				var loc = new ControlLocations(child);
				m_children.Add(loc.m_name, loc);
			}
		}
		public XElement ToXml(XElement node)
		{
			node.Add(m_name    .ToXml(Tag.Name ,false));
			node.Add(m_location.ToXml(Tag.Loc  ,false));
			node.Add(m_size    .ToXml(Tag.Size ,false));
			foreach (var ch in m_children.Values)
				node.Add(ch.ToXml(Tag.Ctrl, false));
			return node;
		}

		/// <summary>Populate these settings from a control</summary>
		public void Read(Control ctrl)
		{
			ReadInternal(ctrl, 0, 0);
		}

		/// <summary>Apply the stored position data to 'ctrl'</summary>
		public void Apply(Control ctrl, bool layout_on_resume = true)
		{
			var name = UniqueName(ctrl, 0, 0);
			if (m_name != name)
			{
				System.Diagnostics.Debug.WriteLine("Control locations ignored due to name mismatch.\nControl Name {0} != Layout Data Name {1}".Fmt(name, m_name));
				return;
			}

			using (ctrl.SuspendLayout(layout_on_resume))
				ApplyInternal(ctrl, 0);
		}

		/// <summary>Recursively populate this object from 'ctrl'</summary>
		private void ReadInternal(Control ctrl, int level, int index)
		{
			m_name     = UniqueName(ctrl, level, index);
			m_location = ctrl.Location;
			m_size     = ctrl.Size;
			m_children = new Dictionary<string,ControlLocations>();
			for (var i = 0; i != ctrl.Controls.Count; ++i)
			{
				var s = new ControlLocations(ctrl.Controls[i], level + 1, i);
				try
				{
					m_children.Add(s.m_name, s);
				}
				catch (ArgumentException ex)
				{
					throw new Exception("A sibling control with this name already exists. All controls must have a unique name in order to save position data", ex);
				}
			}
		}

		// Recursively position 'ctrl' and it's children
		private void ApplyInternal(Control ctrl, int level)
		{
			// Position the control
			ctrl.Location = m_location;
			if (ctrl.Dock == DockStyle.None && ctrl.Location != m_location)
				System.Diagnostics.Debug.WriteLine("Setting control {0} location failed".Fmt(ctrl.Name));

			// Set the control size
			ctrl.Size = m_size;
			if (!ctrl.AutoSize && ctrl.Size != m_size)
				System.Diagnostics.Debug.WriteLine("Setting control {0} size failed".Fmt(ctrl.Name));

			// Set the bounds of the child controls
			for (var i = 0; i != ctrl.Controls.Count; ++i)
			{
				var child = ctrl.Controls[i];
				var name = UniqueName(child, level + 1, i);

				// No idea why this helps, but it does...
				if (ctrl is ToolStripPanel)
					child.Dock = DockStyle.Right;

				ControlLocations s;
				if (m_children.TryGetValue(name, out s))
					s.ApplyInternal(child, level + 1);
			}
		}

		/// <summary>Generates a unique name for 'ctrl'</summary>
		private string UniqueName(Control ctrl, int level, int index)
		{
			return "{0}({1},{2})".Fmt(ctrl.Name.HasValue() ? ctrl.Name : ctrl.GetType().Name, level, index);
		}

		public override string ToString()
		{
			return "{0} (child count: {1})".Fmt(m_name, m_children.Count);
		}
	}
}