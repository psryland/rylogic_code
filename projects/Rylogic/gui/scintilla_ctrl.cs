using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.util;
using pr.win32;
using Scintilla;

namespace pr.gui
{
	/// <summary>Typedef Scintilla.Scintilla to 'pr.gui.Sci' and add pr specific features</summary>
	public class Sci :Scintilla.Scintilla
	{
		/// <summary>Helper for sending text to scintilla</summary>
		public class CellBuf
		{
			public CellBuf(int capacity = 1024)
			{
				m_cells = new Cell[capacity];
				Length = 0;
			}

			/// <summary>The capacity of the buffer</summary>
			public int Capacity
			{
				get { return m_cells.Length; }
				set
				{
					Array.Resize(ref m_cells, value);
					Length = Math.Min(Length, value);
				}
			}

			/// <summary>The number of valid cells in the buffer</summary>
			public int Length { get; set; }

			/// <summary>The buffer of scintilla cells</summary>
			public Cell[] Cells { get { return m_cells; } }
			protected Cell[] m_cells;

			/// <summary>Add a single character to the buffer, along with a style</summary>
			public void Add(byte ch, byte sty)
			{
				EnsureSpace(Length + 1);
				Append(ch, sty);
				++Length;
			}

			/// <summary>Add an array of bytes all with the same style</summary>
			public void Add(byte[] bytes, byte sty)
			{
				EnsureSpace(Length + bytes.Length);
				Append(bytes, sty);
				Length += bytes.Length;
			}

			/// <summary>Add a string to the buffer, all using the style 'sty'</summary>
			public void Add(string text, byte sty)
			{
				Add(Encoding.UTF8.GetBytes(text), sty);
			}

			/// <summary>Remove the last 'n' cells from the buffer</summary>
			public void Pop(int n)
			{
				Length -= Math.Min(n, Length);
			}

			/// <summary>Pin the buffer so it can be passed to Scintilla</summary>
			public virtual BufPtr Pin()
			{
				return new BufPtr(m_cells, 0, Length);
			}

			/// <summary>Grow the internal array to ensure it can hold at least 'new_size' cells</summary>
			public void EnsureSpace(int new_size)
			{
				if (new_size <= Capacity) return;
				new_size = Math.Max(new_size, Capacity * 3/2);
				Resize(new_size);
			}

			/// <summary>Add a single byte and style to the buffer</summary>
			protected virtual void Append(byte ch, byte sty)
			{
				m_cells[Length] = new Cell(ch, sty);
			}

			/// <summary>Add an array of bytes and style to the buffer</summary>
			protected virtual void Append(byte[] bytes, byte sty)
			{
				var ofs = Length;
				for (int i = 0; i != bytes.Length; ++i, ++ofs)
					m_cells[ofs] = new Cell(bytes[i], sty);
			}

			/// <summary>Resize the cell buffer</summary>
			protected virtual void Resize(int new_size)
			{
				Array.Resize(ref m_cells, new_size);
			}

			public class BufPtr :IDisposable
			{
				private GCHandleScope m_scope;
				public BufPtr(Cell[] cells, int ofs, int length)
				{
					m_scope = GCHandleEx.Alloc(cells, GCHandleType.Pinned);
					Pointer = m_scope.Handle.AddrOfPinnedObject() + ofs * R<Cell>.SizeOf;
					SizeInBytes = length * R<Cell>.SizeOf;
				}
				public void Dispose()
				{
					Util.Dispose(ref m_scope);
				}

				/// <summary>The pointer to the pinned memory</summary>
				public IntPtr Pointer { get; private set; }

				/// <summary>The size of the buffered data</summary>
				public int SizeInBytes { get; private set; }
			}
		}

		/// <summary>A cell buffer that fills from back to front</summary>
		public class BackFillCellBuf :CellBuf
		{
			public BackFillCellBuf(int capacity = 1024) :base(capacity)
			{}

			/// <summary>Add a single character to the buffer, along with a style</summary>
			protected override void Append(byte ch, byte sty)
			{
				m_cells[m_cells.Length - Length] = new Cell(ch, sty);
			}

			/// <summary>Add a string to the buffer, all using the style 'sty'</summary>
			protected override void Append(byte[] bytes, byte sty)
			{
				var ofs = m_cells.Length - Length - bytes.Length;
				for (int i = 0; i != bytes.Length; ++i, ++ofs)
					m_cells[ofs] = new Cell(bytes[i], sty);
			}

			/// <summary>Resize the cell buffer, copying the contents to the end of the new buffer</summary>
			protected override void Resize(int new_size)
			{
				var new_cells = new Cell[new_size];
				Array.Copy(m_cells, m_cells.Length - Length, new_cells, new_cells.Length - Length, Length);
				m_cells = new_cells;
			}

			/// <summary>Pin the buffer so it can be passed to Scintilla</summary>
			public override BufPtr Pin()
			{
				return new BufPtr(m_cells, m_cells.Length - Length, Length);
			}
		}

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

	/// <summary>A win forms wrapper of the scintilla control</summary>
	public class ScintillaCtrl :Control
	{
		protected class CellBuf :Sci.CellBuf {}

		private Sci.DirectFunction m_func;
		private IntPtr m_ptr;

		static ScintillaCtrl()
		{
			try { Sci.LoadDll(); }
			catch (Exception) {}
		}
		public ScintillaCtrl()
		{
		}
		protected override CreateParams CreateParams
		{
			get
			{
				if (Util.IsInDesignMode)
					return base.CreateParams;

				if (!Sci.ScintillaAvailable)
					throw new Exception("Scintilla dll not loaded");

				var cp = base.CreateParams;
				cp.ClassName = "Scintilla";
				return cp;
			}
		}
		protected override void OnHandleCreated(EventArgs e)
		{
			m_func = Marshal_.PtrToDelegate<Sci.DirectFunction>((IntPtr)Win32.SendMessage(Handle, Sci.SCI_GETDIRECTFUNCTION, 0, 0));
			m_ptr  = (IntPtr)Win32.SendMessage(Handle, Sci.SCI_GETDIRECTPOINTER, 0, 0);
			Cmd(Sci.SCI_SETCODEPAGE, Sci.SC_CP_UTF8, 0);
			Cmd(Sci.SCI_CLEARALL);
			Cmd(Sci.SCI_CLEARDOCUMENTSTYLE);
			Cmd(Sci.SCI_SETSTYLEBITS, 7);
			Cmd(Sci.SCI_SETTABWIDTH, 4);
			Cmd(Sci.SCI_SETINDENT, 4);
	
			Cursor = Cursors.IBeam;
			base.OnHandleCreated(e);
		}
		protected override void OnHandleDestroyed(EventArgs e)
		{
			m_func = null;
			m_ptr = IntPtr.Zero;
			base.OnHandleDestroyed(e);
		}
		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
			case Win32.WM_PAINT:
				#region
				{
					Cmd(Win32.WM_PAINT);
					return;
				}
				#endregion
			case Win32.WM_COMMAND:
				#region
				{
					// Watch for edit notifications
					var notif = Win32.HiWord(m.WParam.ToInt32());
					var id    = Win32.LoWord(m.WParam.ToInt32());
					if (notif == Win32.EN_CHANGE)// && id == Id)
						TextChanged.Raise(this);
					break;
				}
				#endregion
			case Win32.WM_DESTROY:
				#region
				{
					// Copy the control text before WM_DESTROY
					//m_text = Text;
					break;
				}
				#endregion
			case Win32.WM_REFLECT + Win32.WM_NOTIFY:
				#region
				{
					var nmhdr = Marshal_.PtrToStructure<Win32.NMHDR>(m.LParam);
					var notif = Marshal_.PtrToStructure<Sci.SCNotification>(m.LParam);
					HandleSCNotification(ref nmhdr, ref notif);
					break;
				}
				#endregion
			}
			base.WndProc(ref m);
		}

		/// <summary>Handle notification from the native scintilla control</summary>
		protected virtual void HandleSCNotification(ref Win32.NMHDR nmhdr, ref Scintilla.Scintilla.SCNotification notif)
		{
			switch (notif.nmhdr.code)
			{
			case Sci.SCN_CHARADDED:
				#region
				{
					#region Auto Indent
					if (AutoIndent)
					{
						var lem = Cmd(Sci.SCI_GETEOLMODE);
						var lend =
							(lem == Sci.SC_EOL_CR   && notif.ch == '\r') ||
							(lem == Sci.SC_EOL_LF   && notif.ch == '\n') ||
							(lem == Sci.SC_EOL_CRLF && notif.ch == '\n');
						if (lend)
						{
							var line = Cmd(Sci.SCI_LINEFROMPOSITION, Cmd(Sci.SCI_GETCURRENTPOS));
							var indent = line > 0 ? Cmd(Sci.SCI_GETLINEINDENTATION, line - 1) : 0;
							Cmd(Sci.SCI_SETLINEINDENTATION, line, indent);
							Cmd(Sci.SCI_GOTOPOS, Cmd(Sci.SCI_GETLINEENDPOSITION, line));
						}
					}
					#endregion

					// Notify text changed
					TextChanged.Raise(this);
					break;
				}
				#endregion
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

		/// <summary>Handle notification that the control has been invalidated</summary>
		protected override void NotifyInvalidate(Rectangle invalidatedArea)
		{
			var rect = Win32.RECT.FromRectangle(invalidatedArea);
			Win32.InvalidateRect(Handle, ref rect, true);
			base.NotifyInvalidate(invalidatedArea);
		}

		/// <summary>Call the direct function</summary>
		public int Cmd(int code, IntPtr wparam, IntPtr lparam) { return Handle != null ? m_func(m_ptr, code, wparam, lparam) : 0; }
		public int Cmd(int code, int wparam, IntPtr lparam)    { return Cmd(code, (IntPtr)wparam, lparam); }
		public int Cmd(int code, int wparam, int lparam)       { return Cmd(code, (IntPtr)wparam, (IntPtr)lparam); }
		public int Cmd(int code, int wparam)                   { return Cmd(code, (IntPtr)wparam, IntPtr.Zero); }
		public int Cmd(int code)                               { return Cmd(code, IntPtr.Zero, IntPtr.Zero); }

		#region Text
		/// <summary>Clear all text from the control</summary>
		public void ClearAll()
		{
			Cmd(Sci.SCI_CLEARALL, 0, 0);
		}

		/// <summary>Gets the length of the text in the control</summary>
		public int TextLength
		{
			get { return Cmd(Sci.SCI_GETTEXTLENGTH); }
		}

		/// <summary>Gets or sets the current text</summary>
		public new string Text
		{
			get { return GetText(); }
			set { SetText(value); }
		}

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

		#region Selection/Navigation
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

		#region Indenting
		/// <summary>Enable/Disable auto indent mode</summary>
		public bool AutoIndent { get; set; }
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

		#region DragDrop
		public override bool AllowDrop
		{
			get { return (Win32.GetWindowLong(Handle, Win32.GWL_EXSTYLE) & Win32.WS_EX_ACCEPTFILES) != 0; }
			set { Win32.DragAcceptFiles(Handle, value); }
		}
		#endregion
	}
}
