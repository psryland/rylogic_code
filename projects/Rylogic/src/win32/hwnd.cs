using System;
using System.Windows.Forms;

namespace Rylogic.Windows32
{
	/// <summary>A window handle wrapper</summary>
	public class HWnd :IWin32Window
	{
		public IntPtr Handle { get; private set; }
		public HWnd(IntPtr handle) { Handle = handle; }
	}
}
