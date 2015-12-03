//***************************************************
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
using pr.win32;

namespace pr.extn
{
	public static class ControlExtensions
	{
		/// <summary>Get the user data for this control associated with 'guid'. If not found, and 'make' != null then 'make' is called with the result added and returned</summary>
		public static T UserData<T>(this Control ctrl, Guid guid, Func<T> make = null)
		{
			object user = default(T);

			// Look for existing user data associated with 'guid'
			var map = m_ctrl_user_data.GetOrAdd(ctrl);
			if (map.TryGetValue(guid, out user))
				return (T)user;

			// If not found, but a construction delegate is provided, create the user
			// data, add it to the map, and return it
			if (make != null)
				map.Add(guid, user = make());

			return (T)user;
		}

		/// <summary>Set some user data for this control. Setting 'data' == null removes the user data associated with 'guid'</summary>
		public static void UserData<T>(this Control ctrl, Guid guid, T data)
		{
			if (data != null)
			{
				var map = m_ctrl_user_data.GetOrAdd(ctrl);
				map[guid] = data;
			}
			else
			{
				Dictionary<Guid,object> map;
				if (m_ctrl_user_data.TryGetValue(ctrl, out map))
				{
					map.Remove(guid);
					if (map.Count == 0)
						m_ctrl_user_data.Remove(ctrl);
				}
			}
		}
		[ThreadStatic] private static Dictionary<Control, Dictionary<Guid, object>> m_ctrl_user_data = new Dictionary<Control,Dictionary<Guid,object>>();

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

		/// <summary>Set control style on this control</summary>
		public static void SetStyle(this Control ctrl, ControlStyles style, bool state)
		{
			var mi = ctrl.GetType().GetMethod("SetStyle", BindingFlags.Instance|BindingFlags.NonPublic);
			mi.Invoke(ctrl, new object[]{style, state});
		}

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

		/// <summary>Add to the controls collection and return the control for method chaining</summary>
		public static T Add2<T>(this Control.ControlCollection collection, T ctrl) where T:Control
		{
			collection.Add(ctrl);
			return ctrl;
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
		public static void ShowHintBalloon(this Control ctrl, ToolTip tt, string msg, int duration = 5000, Point? pt = null)
		{
			// Only show the tool tip if not currently showing or it's different
			var prev = tt.GetToolTip(ctrl);
			if (prev != msg)
			{
				// Show the tt
				tt.Show(msg, ctrl, pt ?? ctrl.ClientRectangle.Centre(), duration);

				// Set the tt for the control so we can detect expired
				tt.SetToolTip(ctrl, msg);
				tt.BeginInvokeDelayed(duration, () =>
					{
						if (tt.GetToolTip(ctrl) == msg)
							tt.SetToolTip(ctrl, null);
					});
			}
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
		public static Scope SuspendRedraw(this IWin32Window ctrl, bool refresh_on_resume)
		{
			return Scope.Create(
				() => Win32.SendMessage(ctrl.Handle, Win32.WM_SETREDRAW, 0, 0),
				() =>
					{
						Win32.SendMessage(ctrl.Handle, Win32.WM_SETREDRAW, 1, 0);
						if (refresh_on_resume)
							Win32.RedrawWindow(ctrl.Handle, IntPtr.Zero, IntPtr.Zero, (uint)(Win32.RDW_ERASE | Win32.RDW_FRAME | Win32.RDW_INVALIDATE | Win32.RDW_ALLCHILDREN));
					});
		}

		/// <summary>Block scrolling events</summary>
		public static Scope SuspendScroll(this IWin32Window ctrl)
		{
			var scroll_pos = new Win32.POINT();
			var event_mask = 0;
 			return Scope.Create(
				() =>
					{
						Win32.SendMessage(ctrl.Handle, Win32.EM_GETSCROLLPOS, (IntPtr)0, ref scroll_pos);
						event_mask = Win32.SendMessage(ctrl.Handle, Win32.EM_GETEVENTMASK, 0, 0);
					},
				() =>
					{
						Win32.SendMessage(ctrl.Handle, Win32.EM_SETSCROLLPOS, (IntPtr)0, ref scroll_pos);
						Win32.SendMessage(ctrl.Handle, Win32.EM_SETEVENTMASK, 0, event_mask);
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

		/// <summary>Return the ScrollBars value that represents the current visibility of the scroll bars</summary>
		public static ScrollBars ScrollBarVisibility(this Control ctrl)
		{
			var sty = Win32.GetWindowLong(ctrl.Handle, Win32.GWL_STYLE);
			var vis = ScrollBars.None;
			if ((sty & Win32.WS_HSCROLL) != 0) vis |= ScrollBars.Horizontal;
			if ((sty & Win32.WS_VSCROLL) != 0) vis |= ScrollBars.Vertical;
			return vis;
		}
		public static bool ScrollBarHVisible(this Control ctrl)
		{
			return ctrl.ScrollBarVisibility().HasFlag(System.Windows.Forms.ScrollBars.Horizontal);
		}
		public static bool ScrollBarVVisible(this Control ctrl)
		{
			return ctrl.ScrollBarVisibility().HasFlag(System.Windows.Forms.ScrollBars.Vertical);
		}

		/// <summary>Centre this control to its parent</summary>
		public static void CentreToParent(this Control ctrl)
		{
			Win32.CenterWindow(ctrl.Handle);
		}

		/// <summary>Return a form that wraps 'ctrl'</summary>
		public static T FormWrap<T>(this Control ctrl, Point? loc = null, Size? sz = null) where T:Form, new()
		{
			var f = new T();
			if (loc.HasValue)
			{
				f.Location = loc.Value;
				f.StartPosition = FormStartPosition.Manual;
			}
			if (sz.HasValue)
			{
				f.Size = sz.Value;
			}
			ctrl.Dock = DockStyle.Fill;
			f.Controls.Add(ctrl);
			return f;
		}

		/// <summary>Returns the name of this control and all parents in the hierarchy</summary>
		public static string HierarchyName(this Control ctrl)
		{
			var ancestors = new List<string>();
			for (var p = ctrl; p != null; p = p.Parent)
				ancestors.Add("{0}({1})".Fmt(p.Name, p.GetType().Name));
			ancestors.Reverse();
			return string.Join("->", ancestors);
		}
	}

	/// <summary>Used to persist control locations and sizes in XML</summary>
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

			//using (ctrl.SuspendLayout(layout_on_resume))
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
			// ToolStripPanels need to perform a layout before setting the location of child controls
			if (ctrl is ToolStripContainer || ctrl is ToolStripPanel)
				ctrl.PerformLayout();

			// Position the control
			ctrl.Location = m_location;
			if (ctrl.Dock == DockStyle.None && ctrl.Location != m_location && !(ctrl is MenuStrip && ctrl.AutoSize))
				System.Diagnostics.Debug.WriteLine("ControlLayout: Control {0} location is {1} instead of {2}".Fmt(ctrl.Name, ctrl.Location, m_location));

			// Set the control size
			ctrl.Size = m_size;
			if (!ctrl.AutoSize && ctrl.Size != m_size)
				System.Diagnostics.Debug.WriteLine("ControlLayout: Control {0} size is {1} instead of {2}".Fmt(ctrl.Name, ctrl.Size, m_size));

			// Set the bounds of the child controls
			for (var i = 0; i != ctrl.Controls.Count; ++i)
			{
				var child = ctrl.Controls[i];
				var name = UniqueName(child, level + 1, i);

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
