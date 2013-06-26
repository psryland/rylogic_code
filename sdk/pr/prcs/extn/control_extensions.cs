//***************************************************
// Control Extensions
//  Copyright © Rylogic Ltd 2010
//***************************************************

using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using pr.maths;
using pr.util;

namespace pr.extn
{
	public static class ControlExtensions
	{
		private struct TTAssociation { public ToolTip tt; public string msg; }
		private static readonly Dictionary<Control,TTAssociation> m_tt_texts = new Dictionary<Control, TTAssociation>();

		/// <summary>Set the tooltip for this control</summary>
		public static void ToolTip(this Control ctrl, ToolTip tt, string caption)
		{
			m_tt_texts[ctrl] = new TTAssociation{tt = tt, msg = caption};
			tt.SetToolTip(ctrl, caption);
		}

		/// <summary>Returns the tool tip text last set on this control, or null if none</summary>
		public static string ToolTipText(this Control ctrl)
		{
			TTAssociation tta;
			return m_tt_texts.TryGetValue(ctrl, out tta) ? tta.msg : null;
		}

		/// <summary>Display the tool tip for this control using the capture last set on it</summary>
		public static void ShowToolTip(this Control ctrl)
		{
			TTAssociation tta;
			if (m_tt_texts.TryGetValue(ctrl, out tta))
				tta.tt.Show(tta.msg, ctrl);
		}

		/// <summary>Display the tool tip for this control using the capture last set on it</summary>
		public static void ShowToolTip(this Control ctrl, int duration)
		{
			TTAssociation tta;
			if (m_tt_texts.TryGetValue(ctrl, out tta))
				tta.tt.Show(tta.msg, ctrl, duration);
		}

		/// <summary>Display the tool tip for this control using the capture last set on it</summary>
		public static void ShowToolTip(this Control ctrl, int x, int y, int duration)
		{
			TTAssociation tta;
			if (m_tt_texts.TryGetValue(ctrl, out tta))
				tta.tt.Show(tta.msg, ctrl, x, y, duration);
		}

		/// <summary>Hide the tool tip for this control</summary>
		public static void HideToolTip(this Control ctrl)
		{
			TTAssociation tta;
			if (m_tt_texts.TryGetValue(ctrl, out tta))
				tta.tt.Hide(ctrl);
		}

		/// <summary>Display a hint balloon</summary>
		public static void ShowHintBalloon(this Control ctrl, ToolTip tt, string msg, int duration = 5000)
		{
			var parent = ctrl.FindForm();
			if (parent == null) return;
			var pt = ctrl.ClientRectangle.Centre();
			tt.SetToolTip(ctrl, msg);
			tt.Show(msg, ctrl, pt, duration);
			tt.Popup += (s,a) => tt.SetToolTip(ctrl,null);
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
			int start = 0, end = 0;
			return Scope.Create(
				() =>
				{
					start = edit.SelectionStart;
					end   = start + edit.SelectionLength;
				},
				() =>
				{
					edit.SelectionStart  = Maths.Clamp(start, 0, edit.TextLength);
					edit.SelectionLength = Maths.Clamp(end, 0, edit.TextLength) - edit.SelectionStart;
				});
		}
	}
}
