//***************************************************
// Control Extensions
//  Copyright © Rylogic Ltd 2010
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

		/// <summary>Returns a disposable object that preserves the current selected</summary>
		public static Scope SelectionScope(this TextBoxBase edit)
		{
			int start = 0, length = 0;
			return Scope.Create(
				() =>
				{
					start  = edit.SelectionStart;
					length = edit.SelectionLength;
				},
				() =>
				{
					edit.Select(start, length);
				});
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

		/// <summary>A smarter set text that does sensible things with the caret position</summary>
		public static void SetText(this TextBoxBase tb, string text)
		{
			var idx = tb.SelectionStart;
			tb.SelectedText = text;
			tb.SelectionStart = idx + text.Length;
		}

		/// <summary>A smarter set text that does sensible things with the caret position</summary>
		public static void SetText(this ComboBox cb, string text)
		{
			var idx = cb.SelectionStart;
			cb.SelectedText = text;
			cb.SelectionStart = idx + text.Length;
		}

		/// <summary>Add the current combo box text to the drop down list</summary>
		public static void AddTextToDropDownList(this ComboBox cb, int max_history = 10)
		{
			// Need to take a copy of the text, because Remove() will delete the text
			// if the current text is actually a selected item.
			var text = cb.Text;
			var selection = new Range(cb.SelectionStart, cb.SelectionStart + cb.SelectionLength);

			cb.Items.Remove(text);
			cb.Items.Insert(0, text);

			while (cb.Items.Count > max_history)
				cb.Items.RemoveAt(cb.Items.Count - 1);

			cb.SelectedIndex = 0;
			cb.Select(selection.Begini, selection.Sizei);
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
		private static class Tag
		{
			public const string Ctrl     = "ctrl";
			public const string Name     = "name";
			public const string Loc      = "loc";
			public const string Size     = "size";
			public const string Children = "children";
		}
		// Produces this:
		//  <ctrl>
		//    <name/>
		//    <location/>
		//    <size/>
		//    <children>
		//      <ctrl/>
		//      <ctrl/>
		//      ...
		//    </children>
		//  </ctrl>
		private string m_name;
		private Point m_location;
		private Size m_size;
		private Dictionary<string, ControlLocations> m_children;

		public ControlLocations()
		{
			m_name     = string.Empty;
			m_location = Point.Empty;
			m_size     = Size.Empty;
			m_children = new Dictionary<string,ControlLocations>();
		}
		public ControlLocations(Control ctrl) :this(ctrl, 0, 0)
		{}
		private ControlLocations(Control ctrl, int level, int index)
		{
			ReadInternal(ctrl, level, index);
		}

		/// <summary>Import from xml</summary>
		public ControlLocations(XElement node)
		{
			m_name     = node.Element(Tag.Name).As<string>();
			m_location = node.Element(Tag.Loc ).As<Point>();
			m_size     = node.Element(Tag.Size).As<Size>();

			var children = node.Element(Tag.Children);
			m_children = children != null
				? children.Elements(Tag.Ctrl).Select(s => new ControlLocations(s)).ToDictionary(s => s.m_name, s => s)
				: new Dictionary<string, ControlLocations>();
		}

		/// <summary>Export the location data as xml</summary>
		public XElement ToXml(XElement node)
		{
			node.Add(m_name    .ToXml(Tag.Name ,false));
			node.Add(m_location.ToXml(Tag.Loc  ,false));
			node.Add(m_size    .ToXml(Tag.Size ,false));

			if (m_children.Count != 0)
			{
				var children = node.Add2(new XElement(Tag.Children));
				foreach (var ch in m_children.Values)
					children.Add(ch.ToXml(Tag.Ctrl));
			}

			return node;
		}

		/// <summary>Populate these settings from a control</summary>
		public void Read(Control ctrl)
		{
			ReadInternal(ctrl, 0, 0);
		}
		private void ReadInternal(Control ctrl, int level, int index)
		{
			m_name     = UniqueName(ctrl, level, index);
			m_location = ctrl.Location;
			m_size     = ctrl.Size;
			m_children = new Dictionary<string,ControlLocations>();
			for (var i = 0; i != ctrl.Controls.Count; ++i)
			{
				var s = new ControlLocations(ctrl.Controls[i], level + 1, i);
				try { m_children.Add(s.m_name, s); }
				catch (ArgumentException ex) { throw new Exception("A sibling control with this name already exists. All controls must have a unique name in order to save position data", ex); }
			}
		}

		/// <summary>Apply the stored position data to 'ctrl'</summary>
		public void Apply(Control ctrl, bool layout_on_resume = true)
		{
			var name = UniqueName(ctrl, 0, 0);
			if (m_name != name) return;

			using (ctrl.SuspendLayout(layout_on_resume))
				ApplyInternal(ctrl, 0);
		}
		private void ApplyInternal(Control ctrl, int level)
		{
			ctrl.Location = m_location;
			ctrl.Size     = m_size;
			for (var i = 0; i != ctrl.Controls.Count; ++i)
			{
				ControlLocations s;
				var child = ctrl.Controls[i];
				if (m_children.TryGetValue(UniqueName(child, level + 1, i), out s))
					s.ApplyInternal(child, level + 1);
			}
		}

		/// <summary>Tries to generate a unique name for unnamed controls</summary>
		private string UniqueName(Control ctrl, int level, int index)
		{
			return ctrl.Name.HasValue() ? ctrl.Name : "{0}_{1}_{2}".Fmt(ctrl.GetType().Name, level, index);
		}

		public override string ToString()
		{
			return "{0} ({1} children)".Fmt(m_name, m_children.Count);
		}
	}
}
