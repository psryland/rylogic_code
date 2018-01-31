//***************************************************
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Rylogic.Windows32;

namespace Rylogic.Gui
{
	// To use:
	//  m_hover_scroll = new HoverScroll();
	//  Application.AddMessageFilter(m_hover_scroll);
	//  Application.RemoveMessageFilter(m_hover_scroll);
	public class HoverScroll :IMessageFilter ,IDisposable
	{
		// P/Invoke declarations
		[DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point pt);
		[DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

		/// <summary>The window handles of the controls that should detect hover scrolling</summary>
		public List<IntPtr> WindowHandles { get; private set; }

		public HoverScroll(params IntPtr[] wnds)
		{
			WindowHandles = new List<IntPtr>(wnds);
			Application.AddMessageFilter(this);
		}
		public virtual void Dispose()
		{
			Application.RemoveMessageFilter(this);
		}

		public bool PreFilterMessage(ref Message m)
		{
			if (m.Msg != Win32.WM_MOUSEWHEEL)
				return false;

			// WM_MOUSEWHEEL, find the control at screen position m.LParam
			var hWnd = WindowFromPoint(Control.MousePosition);
			if (hWnd == IntPtr.Zero) return false;              // No window...
			if (hWnd == m.HWnd)	return false;                   // Hovering over the correct control already
			if (Control.FromHandle(hWnd) == null) return false; // Not over a control
			if (WindowHandles.Count != 0 && !WindowHandles.Contains(hWnd)) return false; // Not a control we're hover scrolling

			SendMessage(hWnd, m.Msg, m.WParam, m.LParam);
			return true;
		}
	}
}
