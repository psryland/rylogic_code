//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using HWND = System.IntPtr;

namespace Rylogic.Interop.Win32
{
	// Collection used to enumerate Window Objects
	public class Windows :IEnumerable, IEnumerator
	{
		private readonly ArrayList m_wnds = new ArrayList(); //array of windows
		private readonly bool m_invisible = false;  // filter out invisible windows
		private readonly bool m_no_title = false; // filter out windows with no title
		private int m_position = -1; // holds current index of m_wnds, necessary for IEnumerable

		public Windows() : this(false, false) { }
		public Windows(bool invisible, bool untitled)
		{
			m_invisible = invisible;
			m_no_title = untitled;

			// EnumWindows callback function
			Win32.EnumWindows(ewp, 0);
			bool ewp(HWND hwnd, int lParam)
			{
				if (m_invisible == false && !Win32.IsWindowVisible(hwnd))
					return true;

				var title = new StringBuilder(256);
				var module = new StringBuilder(256);
				Win32.GetWindowModuleFileName(hwnd, module, 256);
				Win32.GetWindowText(hwnd, title, 256);

				if (m_no_title == false && title.Length == 0)
					return true;

				m_wnds.Add(new CWindow(title.ToString(), hwnd, module.ToString()));
				return true;
			}
		}

		// Find all windows with a given name (or partial name)
		public static List<CWindow> GetWindowsByName(string name) => GetWindowsByName(name, false);
		public static List<CWindow> GetWindowsByName(string name, bool partial)
		{
			List<CWindow> wnd = new List<CWindow>();
			Win32.EnumWindows(ewp, 0);
			bool ewp(HWND hwnd, int lParam)
			{
				var title = new StringBuilder(256);
				var module = new StringBuilder(256);
				Win32.GetWindowModuleFileName(hwnd, module, 256);
				Win32.GetWindowText(hwnd, title, 256);

				string wnd_title = title.ToString();
				bool match = (!partial && wnd_title == name) || (partial && wnd_title.Contains(name));
				if (!match)
					return true;

				wnd.Add(new CWindow(wnd_title, hwnd, module.ToString()));
				return true;
			}
			return wnd;
		}

		// IEnumerable implementation
		public object? Current => m_wnds[m_position];
		public IEnumerator GetEnumerator()
		{
			return this;
		}
		public void Reset()
		{
			m_position = -1;
		}
		public bool MoveNext()
		{
			++m_position; return m_position < m_wnds.Count;
		}
	}

	// Represents another window
	// Wraps an HWND. Not using 'Window' as it conflicts with 'System.Windows.Window'
	public class CWindow
	{
		private readonly HWND m_hwnd;
		private readonly string m_title;
		private readonly string m_process;
		private bool m_visible = true;

		public CWindow(HWND hwnd)
		{
			StringBuilder title = new StringBuilder(256);
			StringBuilder module = new StringBuilder(256);
			Win32.GetWindowModuleFileName(hwnd, module, 256);
			Win32.GetWindowText(hwnd, title, 256);
			m_title = title.ToString();
			m_process = module.ToString();
		}
		public CWindow(string title, HWND hwnd, string process)
		{
			m_title = title;
			m_hwnd = hwnd;
			m_process = process;
		}

		public HWND hwnd { get { return m_hwnd; } }
		public string Title { get { return m_title; } }
		public string Process { get { return m_process; } }

		// Send a message to this window
		public int SendMessage(uint msg, int wparam, int lparam)
		{
			return (int)Win32.SendMessage(m_hwnd, msg, wparam, lparam);
		}

		// Sets this Window Object's visibility
		public bool Visible
		{
			get { return m_visible; }
			set
			{
				if (value == m_visible) return;
				Win32.ShowWindowAsync(m_hwnd, value ? Win32.SW_SHOW : Win32.SW_HIDE);
				m_visible = value;
			}
		}

		// Return the client rectangle for the window
		public Rectangle ClientRectangle
		{
			get { Win32.RECT rect; Win32.GetClientRect(m_hwnd, out rect); return rect.ToRectangle(); }
		}

		// Return the screen rectangle for the window
		public Rectangle WindowRectangle
		{
			get { Win32.RECT rect; Win32.GetWindowRect(m_hwnd, out rect); return rect.ToRectangle(); }
		}

		// Sets focus to this Window Object
		public void Activate()
		{
			if (m_hwnd == Win32.GetForegroundWindow())
				return;
			IntPtr proc_id = IntPtr.Zero;
			IntPtr thread_id0 = Win32.GetWindowThreadProcessId(Win32.GetForegroundWindow(), ref proc_id);
			IntPtr thread_id1 = Win32.GetWindowThreadProcessId(m_hwnd, ref proc_id);
			if (thread_id0 != thread_id1)
			{
				Win32.AttachThreadInput(thread_id0, thread_id1, 1);
				Win32.SetForegroundWindow(m_hwnd);
				Win32.AttachThreadInput(thread_id0, thread_id1, 0);
			}
			else
			{
				Win32.SetForegroundWindow(m_hwnd);
			}

			Win32.ShowWindowAsync(m_hwnd, Win32.SW_SHOW);
		}

		// Return the title if it has one, if not return the process name
		public override string ToString()
		{
			return m_title.Length != 0 ? m_title : m_process;
		}
	}
}
