//***************************************************
// Control Extensions
//  Copyright © Rylogic Ltd 2010
//***************************************************

using System.Reflection;
using System.Windows.Forms;
using pr.maths;
using pr.util;

namespace pr.extn
{
	public static class ControlExtensions
	{
		/// <summary>Enable double buffering for the control. Note, it's probably better to subclass the control to turn this on</summary>
		public static void DoubleBuffered(this Control ctl, bool state)
		{
			PropertyInfo pi = ctl.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance|BindingFlags.NonPublic);
			pi.SetValue(ctl, state, null);
			MethodInfo mi = ctl.GetType().GetMethod("SetStyle", BindingFlags.Instance|BindingFlags.NonPublic);
			mi.Invoke(ctl, new object[]{ControlStyles.DoubleBuffer|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint, state});
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
