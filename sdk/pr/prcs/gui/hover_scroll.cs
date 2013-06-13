//***************************************************
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace pr.gui
{
	// To use:
	//  m_hoverscroll = new HoverScroll();
	//  Application.AddMessageFilter(m_hoverscroll);
	//  Application.RemoveMessageFilter(m_hoverscroll);
	public class HoverScroll :IMessageFilter
	{
		// P/Invoke declarations
		[DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point pt);
		[DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

		/// <summary>The window handles of the controls that should detect hoverscrolling</summary>
		public List<IntPtr> WindowHandles { get; private set; }

		public HoverScroll()                     { WindowHandles = new List<IntPtr>(); }
		public HoverScroll(params IntPtr[] wnds) { WindowHandles = new List<IntPtr>(wnds); }

		public bool PreFilterMessage(ref Message m)
		{
			const int WM_MOUSEWHEEL = 0x20a;
			if (m.Msg != WM_MOUSEWHEEL) return false;

			// WM_MOUSEWHEEL, find the control at screen position m.LParam
			Point pos = new Point(m.LParam.ToInt32() & 0xffff, m.LParam.ToInt32() >> 16);
			IntPtr hWnd = WindowFromPoint(pos);

			if (hWnd == IntPtr.Zero) return false;              // No window...
			if (hWnd == m.HWnd)	return false;                   // Hovering over the correct control already
			if (Control.FromHandle(hWnd) == null) return false; // Not over a control
			if (WindowHandles.Count != 0 && !WindowHandles.Contains(hWnd)) return false; // Not a control we're hoverscrolling

			SendMessage(hWnd, m.Msg, m.WParam, m.LParam);
			return true;
		}
	}
}
