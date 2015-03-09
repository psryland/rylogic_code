using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace pr.win32
{
	/// <summary>A window handle wrapper</summary>
	public class HWnd :IWin32Window
	{
		public IntPtr Handle { get; private set; }
		public HWnd(IntPtr handle) { Handle = handle; }
	}
}
