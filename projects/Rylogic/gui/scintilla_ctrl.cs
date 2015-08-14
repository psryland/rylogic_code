using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
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
		private Sci.DirectFunction m_func;
		private IntPtr m_ptr;

		static ScintillaCtrl()
		{
			try { LoadDll(); }
			catch (Exception) {}
		}
		public ScintillaCtrl(ushort ctrl_id = 517)
		{
			Child = HostedCtrl = new Ctrl(this, ctrl_id);
			m_func = MarshalEx.PtrToDelegate<Sci.DirectFunction>((IntPtr)Win32.SendMessage(HostedCtrl.Handle, Sci.SCI_GETDIRECTFUNCTION, 0, 0));
			m_ptr  = (IntPtr)Win32.SendMessage(HostedCtrl.Handle, Sci.SCI_GETDIRECTPOINTER, 0, 0);

			Cmd(Sci.SCI_SETCODEPAGE, Sci.SC_CP_UTF8, 0);
		}
		protected override void Dispose(bool disposing)
		{
			HostedCtrl = Util.Dispose(HostedCtrl);
			m_func = null;
			m_ptr = IntPtr.Zero;

			base.Dispose(disposing);
		}

		/// <summary>The scintilla control</summary>
		public Ctrl HostedCtrl { get; private set; }

		/// <summary>Call the direct function</summary>
		protected int Cmd(int code, IntPtr wparam, IntPtr lparam) { return m_func(m_ptr, code, wparam, lparam); }
		protected int Cmd(int code, int wparam, IntPtr lparam)    { return m_func(m_ptr, code, (IntPtr)wparam, lparam); }
		protected int Cmd(int code, int wparam, int lparam)       { return m_func(m_ptr, code, (IntPtr)wparam, (IntPtr)lparam); }
		protected int Cmd(int code, int wparam)                   { return m_func(m_ptr, code, (IntPtr)wparam, IntPtr.Zero); }
		protected int Cmd(int code)                               { return m_func(m_ptr, code, IntPtr.Zero, IntPtr.Zero); }

		#region Text
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
				var num = Cmd(Sci.SCI_GETTEXT, len + 1, h.Handle.AddrOfPinnedObject());
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
					Cmd(Sci.SCI_SETTEXT, 0, h.Handle.AddrOfPinnedObject());
			}

			TextChanged.Raise(this);
		}

		/// <summary>Raised when text in the control is changed</summary>
		public new event EventHandler TextChanged;
		#endregion

		#region Selection
		/// <summary>Records a selection</summary>
		public struct Selection
		{
			public int m_current;
			public int m_anchor;
			public Selection(int current, int anchor)
			{
				m_current = current;
				m_anchor  = anchor;
			}
			public static Selection Save(ScintillaCtrl ctrl)
			{
				return new Selection(
					ctrl.Cmd(Sci.SCI_GETCURRENTPOS),
					ctrl.Cmd(Sci.SCI_GETANCHOR));
			}
			public static void Restore(ScintillaCtrl ctrl, Selection sel)
			{
				ctrl.Cmd(Sci.SCI_SETCURRENTPOS, sel.m_current);
				ctrl.Cmd(Sci.SCI_SETANCHOR, sel.m_anchor);
			}
		}

		/// <summary>RAII scope for a selection</summary>
		public Scope<Selection> SelectionScope()
		{
			return Scope.Create(
				() => Selection.Save(this),
				sel => Selection.Restore(this, sel));
		}

		/// <summary>Raised whenever the selection changes</summary>
		public event EventHandler SelectionChanged;
		protected virtual void OnSelectionChanged()
		{
			SelectionChanged.Raise(this);
		}
		#endregion

		#region Scrolling

		/// <summary>Raised whenever a horizontal or vertical scrolling event occurs</summary>
		public event EventHandler<ScrollEventArgs> Scroll;
		protected virtual void OnScroll(ScrollEventArgs args)
		{
			Scroll.Raise(this, args);
		}

		/// <summary>Returns an RAII object that preserves (where possible) the currently visible line</summary>
		public Scope<int> ScrollScope()
		{
			return Scope.Create(
				() => Cmd(Sci.SCI_GETFIRSTVISIBLELINE),
				fv => Cmd(Sci.SCI_SETFIRSTVISIBLELINE, fv));
		}

		/// <summary>Gets the range of visible lines</summary>
		public Range VisibleLineIndexRange
		{
			get
			{
				var s = Cmd(Sci.SCI_GETFIRSTVISIBLELINE);
				var c = Cmd(Sci.SCI_LINESONSCREEN);
				return new Range(s, s + c);
			}
		}

		#endregion

		#region Clipboard
		/// <summary>Cut the current selection to the clipboard</summary>
		public virtual void Cut()
		{
			Cmd(Sci.SCI_CUT, 0, 0);
		}

		/// <summary>Copy the current selection to the clipboard</summary>
		public virtual void Copy()
		{
			Cmd(Sci.SCI_COPY, 0, 0);
		}

		/// <summary>Paste the clipboard contents over the current selection</summary>
		public virtual void Paste()
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
					var vk = (Keys)wparam;

					// We want tabs, arrow keys, and return/enter when the control is focused
					if (!handled && (
						wparam == (IntPtr)Win32.VK_TAB ||
						wparam == (IntPtr)Win32.VK_UP ||
						wparam == (IntPtr)Win32.VK_DOWN ||
						wparam == (IntPtr)Win32.VK_LEFT ||
						wparam == (IntPtr)Win32.VK_RIGHT))
					{
						Win32.SendMessage(HostedCtrl.Handle, (uint)msg, wparam, lparam);
						handled = true;
					}
					OnKeyDown(new KeyEventArgs(vk));
					break;
				}
				#endregion
			case Win32.WM_KEYUP:
				#region
				{
					var vk = (Keys)wparam;
					OnKeyUp(new KeyEventArgs(vk));
					break;
				}
				#endregion
			case Win32.WM_COMMAND:
				#region
				{// Watch for edit notifications
					var notif = Win32.HiWord(wparam.ToInt32());
					var id    = Win32.LoWord(wparam.ToInt32());
					if (notif == Win32.EN_CHANGE && id == HostedCtrl.Id)
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
			case Win32.WM_NOTIFY:
				#region
				{
					//Win32.WndProcDebug(hwnd, msg, wparam, lparam, "vt100");
					var nmhdr = MarshalEx.PtrToStructure<Win32.NMHDR>(lparam);
					if (nmhdr.idFrom == HostedCtrl.Id)
					{
						var notif = MarshalEx.PtrToStructure<Sci.SCNotification>(lparam);
						switch (notif.nmhdr.code)
						{
						case Sci.SCN_UPDATEUI:
							#region
							{
								switch (notif.updated)
								{
								case Sci.SC_UPDATE_SELECTION:
									#region
									{
										OnSelectionChanged();
										break;
									}
									#endregion
								case Sci.SC_UPDATE_H_SCROLL:
								case Sci.SC_UPDATE_V_SCROLL:
									#region
									{
										var ori = notif.nmhdr.code == Sci.SC_UPDATE_H_SCROLL ? ScrollOrientation.HorizontalScroll : ScrollOrientation.VerticalScroll;
										OnScroll(new ScrollEventArgs(ScrollEventType.ThumbPosition, 0, ori));
										break;
									}
									#endregion
								}
								break;
							}
							#endregion
						}
					}
					else
					{
						var i = nmhdr.code;
					}
					break;
				}
				#endregion
			}
		}

		#region Hosted Scintilla Control
		public class Ctrl :HwndHost ,System.Windows.Forms.IWin32Window
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
			IntPtr System.Windows.Forms.IWin32Window.Handle { get { return m_ctrl; } }

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
				return base.WndProc(hwnd, msg, wparam, lparam, ref handled);
			}
			protected override bool TranslateAcceleratorCore(ref MSG msg, System.Windows.Input.ModifierKeys modifiers)
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
			protected override bool TabIntoCore(System.Windows.Input.TraversalRequest request)
			{
				Win32.SetFocus(m_ctrl);
				return false;
			}
			protected override void OnWindowPositionChanged(Rect rect)
			{
				Win32.SetWindowPos(m_ctrl, IntPtr.Zero, 0, 0, (int)rect.Width, (int)rect.Height, (uint)(Win32.SWP_NOACTIVATE | Win32.SWP_NOMOVE | Win32.SWP_NOZORDER));
				base.OnWindowPositionChanged(rect);
			}
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
