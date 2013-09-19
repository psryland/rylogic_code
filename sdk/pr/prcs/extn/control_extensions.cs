//***************************************************
// Control Extensions
//  Copyright © Rylogic Ltd 2010
//***************************************************

using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using pr.util;

namespace pr.extn
{
	public static class ControlExtensions
	{
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
			var parent = ctrl.FindForm();
			if (parent == null) return;
			var pt = ctrl.ClientRectangle.Centre();

			tt.SetToolTip(ctrl, msg);
			tt.Show(msg, ctrl, pt, duration);
			tt.BeginInvokeDelayed(duration, () => tt.SetToolTip(ctrl, null));
			//tt.Popup += (s,a) => tt.SetToolTip(ctrl,null);
		}

		/// <summary>Display the hint balloon.</summary>
		public static void ShowHintBalloon(this ToolStripItem item, ToolTip tt, string msg, int duration = 5000)
		{
			var parent = item.GetCurrentParent();
			if (parent == null) return;
			var pt = item.Bounds.Centre();

			tt.SetToolTip(parent, msg);
			tt.Show(msg, parent, pt, duration);
			tt.BeginInvokeDelayed(duration, () => tt.SetToolTip(parent, null));
		}

		/// <summary>Returns the location of this item in screen space</summary>
		public static Point ScreenLocation(this Control item)
		{
			var parent = item.Parent;
			if (parent == null) return item.Bounds.Location;
			return parent.PointToScreen(item.Bounds.Location);
		}

		/// <summary>Returns the location of this item in screen space</summary>
		public static Point ScreenLocation(this ToolStripItem item)
		{
			var parent = item.GetCurrentParent();
			if (parent == null) return item.Bounds.Location;
			return parent.PointToScreen(item.Bounds.Location);
		}

		/// <summary>Returns the bounds of this item in form space</summary>
		public static Rectangle ClientRectangle(this ToolStripItem item)
		{
			var parent = item.GetCurrentParent();
			var top = parent != null ? parent.TopLevelControl : null;
			if (top == null) return Rectangle.Empty;
			return top.RectangleToClient(parent.RectangleToClient(item.Bounds));
		}

		/// <summary>Returns the bounds of this item in screen space</summary>
		public static Rectangle ScreenRectangle(this Control item)
		{
			var parent = item.Parent;
			if (parent == null) return item.Bounds;
			return parent.RectangleToScreen(item.Bounds);
		}

		/// <summary>Returns the bounds of this item in screen space</summary>
		public static Rectangle ScreenRectangle(this ToolStripItem item)
		{
			var parent = item.GetCurrentParent();
			if (parent == null) return item.Bounds;
			return parent.RectangleToScreen(item.Bounds);
		}

		/// <summary>BeginInvoke a action after 'delay' milliseconds (roughly)</summary>
		public static void BeginInvokeDelayed(this IComponent ctrl, int delay, Action action)
		{
			new Timer{Enabled = true, Interval = delay}.Tick += (s,a) =>
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
	}
}
