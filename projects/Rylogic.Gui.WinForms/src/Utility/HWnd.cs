using System;
using System.Windows.Forms;

namespace Rylogic.Gui.WinForms
{
	/// <summary>A window handle wrapper</summary>
	public class HWnd :IWin32Window
	{
		public HWnd(IntPtr handle)
		{
			Handle = handle;
		}

		public IntPtr Handle { get; private set; }
	}
}
