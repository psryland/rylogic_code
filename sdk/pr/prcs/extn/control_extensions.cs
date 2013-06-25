//***************************************************
// Control Extensions
//  Copyright © Rylogic Ltd 2010
//***************************************************

using System.Reflection;
using System.Windows.Forms;

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
	}
}
