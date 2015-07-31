using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Interop;
using pr.common;
using pr.extn;
using pr.util;
using pr.win32;
using Scintilla;

namespace pr.gui
{
	/// <summary>Typedef Scintilla.Scintilla to 'pr.gui.Sci'</summary>
	public class Sci :Scintilla.Scintilla {}

	/// <summary>A scintilla winforms/WPF control</summary>
	public class ScintillaCtrl :ElementHost
	{
		private Ctrl m_ctrl;
		private Sci.DirectFunction m_func;
		private IntPtr m_ptr;

		static ScintillaCtrl()
		{
			try { LoadDll(); }
			catch (Exception) {}
		}
		public ScintillaCtrl()
		{
			Child = m_ctrl = new Ctrl(this);
			m_func = MarshalEx.PtrToDelegate<Sci.DirectFunction>((IntPtr)Win32.SendMessage(m_ctrl.Handle, Sci.SCI_GETDIRECTFUNCTION, 0, 0));
			m_ptr  = (IntPtr)Win32.SendMessage(m_ctrl.Handle, Sci.SCI_GETDIRECTPOINTER, 0, 0);

			Cmd(Sci.SCI_SETCODEPAGE, Sci.SC_CP_UTF8, 0);
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_ctrl);
			m_func = null;
			m_ptr = IntPtr.Zero;

			base.Dispose(disposing);
		}

		/// <summary>Call the direct function</summary>
		protected int Cmd(int code, int wparam, int lparam)
		{
			return m_func(m_ptr, code, (IntPtr)wparam, (IntPtr)lparam);
		}
		protected int Cmd(int code, int wparam, IntPtr lparam)
		{
			return m_func(m_ptr, code, (IntPtr)wparam, lparam);
		}

		/// <summary>Clear all text from the control</summary>
		public void ClearAll()
		{
			Cmd(Sci.SCI_CLEARALL, 0, 0);
		}

		/// <summary>Gets the length of the text in the control</summary>
		public int TextLength
		{
			get { return m_text != null ? m_text.Length : Cmd(Sci.SCI_GETTEXTLENGTH, 0, 0); }
		}

		/// <summary>Gets or sets the current text</summary>
		public new string Text
		{
			get { return m_text ?? GetText(); }
			set { SetText(value); }
		}
		private string m_text;

		/// <summary>Read the text out of the control</summary>
		private string GetText()
		{
			var len = TextLength;
			var bytes = new byte[len];
			using (var h = GCHandleEx.Alloc(bytes, GCHandleType.Pinned))
			{
				var num = Cmd(Sci.SCI_GETTEXT, len + 1, h.State.AddrOfPinnedObject());
				return Encoding.UTF8.GetString(bytes, 0, num);
			}
		}

		/// <summary>Set the text in the control</summary>
		private void SetText(string text)
		{
			if (!text.HasValue())
				ClearAll();
			else
			{
				// Convert the string to utf-8
				var bytes = Encoding.UTF8.GetBytes(text);
				using (var h = GCHandleEx.Alloc(bytes, GCHandleType.Pinned))
					Cmd(Sci.SCI_SETTEXT, 0, h.State.AddrOfPinnedObject());
			}

			TextChanged.Raise(this);
		}

		/// <summary>Raised when text in the control is changed</summary>
		public new event EventHandler TextChanged;

		#region Clipboard
		/// <summary>Copy the current selection to the clipboard</summary>
		public void Copy()
		{
			Cmd(Sci.SCI_COPY, 0, 0);
		}

		/// <summary>Paste the clipboard contents over the current selection</summary>
		public void Paste()
		{
			Cmd(Sci.SCI_PASTE, 0, 0);
		}
		#endregion

		/// <summary>The wndproc of the hosted control</summary>
		protected virtual void CtrlWndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
		{
			//Win32.WndProcDebug(hwnd, msg, wparam, lparam, "scintilla");
			switch (msg)
			{
			case Win32.WM_KEYDOWN:
				#region
				{
					// We want tabs, arrow keys, and return/enter when the control is focused
					if (wparam == (IntPtr)Win32.VK_TAB ||
						wparam == (IntPtr)Win32.VK_UP ||
						wparam == (IntPtr)Win32.VK_DOWN ||
						wparam == (IntPtr)Win32.VK_LEFT ||
						wparam == (IntPtr)Win32.VK_RIGHT ||
						wparam == (IntPtr)Win32.VK_RETURN)
					{
						Win32.SendMessage(m_ctrl.Handle, (uint)msg, wparam, lparam);
						handled = true;
					}
					break;
				}
				#endregion
			case Win32.WM_COMMAND:
				#region
				{// Watch for edit notifications
					var notif = Win32.HiWord(wparam.ToInt32());
					var id    = Win32.LoWord(wparam.ToInt32());
					if (notif == Win32.EN_CHANGE && id == m_ctrl.Id)
						TextChanged.Raise(this);
					break;
				}
				#endregion
			case Win32.WM_DESTROY:
				#region
				{
					// Copy the control text before WM_DESTROY
					m_text = Text;
					break;
				}
				#endregion
			}
		}

		#region Control
		private class Ctrl :HwndHost ,IKeyboardInputSink
		{
			// See: http://blogs.msdn.com/b/ivo_manolov/archive/2007/10/07/5354351.aspx
			private ScintillaCtrl m_this;
			private IntPtr m_wrap;
			private IntPtr m_ctrl;

			/// <summary>
			/// 'hwndparent' is the hwnd of the ElementHost that will contain this HwndHost
			/// 'ctrl_id' sets the id of the control</summary>
			public Ctrl(ScintillaCtrl this_, ushort ctrl_id = 0)
			{
				m_this = this_;
				Id = (int)ctrl_id;

				// Create the control at construction time so that the text content can be set before the control is displayed.
				m_wrap = Win32.CreateWindowEx(0, "static", "", Win32.WS_CHILD|Win32.WS_VISIBLE, 0, 0, 190, 190, m_this.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				if (m_wrap == IntPtr.Zero)
					throw new Exception("Failed to create editor control. Error (0x{0:8X}) : {1}".Fmt(Win32.GetLastError(), Win32.GetLastErrorString()));

				m_ctrl = Win32.CreateWindowEx(0, "Scintilla", "", Win32.WS_CHILD|Win32.WS_VISIBLE|Win32.WS_HSCROLL|Win32.WS_VSCROLL, 0, 0, 190, 190, m_wrap, (IntPtr)Id, IntPtr.Zero, IntPtr.Zero);
				if (m_ctrl == IntPtr.Zero)
					throw new Exception("Failed to create editor control. Error (0x{0:8X}) : {1}".Fmt(Win32.GetLastError(), Win32.GetLastErrorString()));
			}

			/// <summary>The window handle of the scintilla control</summary>
			public new IntPtr Handle { get { return m_ctrl; } }

			/// <summary>The control id of the scintilla control</summary>
			public int Id { get; private set; }

			protected override HandleRef BuildWindowCore(HandleRef parent)
			{
				Win32.SetParent(m_wrap, parent.Handle);
				return new HandleRef(this, m_wrap);
			}
			protected override void DestroyWindowCore(HandleRef hwnd)
			{
				Win32.DestroyWindow(hwnd.Handle);
			}
			protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
			{
				// This WndProc gets calls when the native scintilla control calls ::DefWindowProc()
				// i.e. After the control has handled WM_CHAR, etc...
				//Win32.WndProcDebug(hwnd, msg, wparam, lparam, "scintilla");
				m_this.CtrlWndProc(hwnd, msg, wparam, lparam, ref handled);
				if (!handled)
				{
					switch ((uint)msg)
					{
					// Since this is the WindowProc of the parent HWND, we need do extra work in order to 
					// resize the child control upon resize of the parent HWND.
					case Win32.WM_WINDOWPOSCHANGED:
						{
							var pos = MarshalEx.PtrToStructure<Win32.WINDOWPOS>(lparam);
							Win32.SetWindowPos(m_ctrl, IntPtr.Zero, 0, 0, pos.cx, pos.cy, (uint)(Win32.SWP_NOACTIVATE | Win32.SWP_NOMOVE | Win32.SWP_NOZORDER));
							handled = true;
							break;
						}
					}
				}
				return base.WndProc(hwnd, msg, wparam, lparam, ref handled);
			}

			#region IKeyboardInputSink
			bool IKeyboardInputSink.TranslateAccelerator(ref MSG msg, System.Windows.Input.ModifierKeys modifiers)
			{
				// This is only called for WM_KEYDOWN or WM_SYSKEYDOWN messages.
				// Normally Tab, arrows, and, return are used for dialog navigation,
				// this allows the hosted control to use them.
				// Note: we never get WM_CHAR messages in the ElementHost::WndProc or the
				// HWndHost::WndProc, this is because TranslateMessage posts 'WM_CHAR' directly
				// to the window with keyboard focus, which is the native control.
				bool handled = false;
				m_this.CtrlWndProc(msg.hwnd, msg.message, msg.wParam, msg.lParam, ref handled);
				return handled || base.TranslateAcceleratorCore(ref msg, modifiers);
			}
			bool IKeyboardInputSink.TabInto(System.Windows.Input.TraversalRequest request)
			{
				Win32.SetFocus(m_ctrl);
				return true;
			}
			#endregion
		}
		#endregion

		/// <summary>Convert a scintilla message id to a string</summary>
		public static string IdToString(int sci_id)
		{
			string name;
			if (!m_sci_name.TryGetValue(sci_id, out name))
			{
				var fi = typeof(Sci).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static)
					.Where(x => x.IsLiteral)
					.Where(x => x.Name.StartsWith("SCI_") || x.Name.StartsWith("SCN_"))
					.FirstOrDefault(x =>
						{
							var val = x.GetValue(null);
							if (val is uint) return (uint)val == (uint)sci_id;
							if (val is int)  return (int)val == sci_id;
							return false;
						});

				name = fi != null ? fi.Name : string.Empty;
				m_sci_name.Add(sci_id, name);
			}
			return name;
		}
		private static Dictionary<int, string> m_sci_name = new Dictionary<int,string>();

		#region Scintilla Dll
		public const string Dll = "scintilla";

		public static bool ScintillaAvailable { get { return m_module != IntPtr.Zero; } }
		private static IntPtr m_module = IntPtr.Zero;

		/// <summary>Load the scintilla dll</summary>
		public static void LoadDll(string dir = @".\lib\$(platform)")
		{
			if (m_module != IntPtr.Zero) return; // Already loaded
			m_module = Win32.LoadDll(Dll+".dll", dir);
		}
		#endregion
	}
}
