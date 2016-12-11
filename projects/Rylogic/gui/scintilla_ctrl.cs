using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gfx;
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
		// Notes:
		// - this is roughly a port of pr::gui::ScintillaCtrl
		// - function comments are mostly incomplete because it'd take too long.. Fill them in as needed
		// - Copy/port #pragma regions from the native control on demand

		protected class CellBuf :Sci.CellBuf
		{ }
		protected struct StyleDesc
		{
			public StyleDesc(int id, Colour32 fore, Colour32 back, string font)
			{
				this.id   = id;
				this.fore = fore;
				this.back = back;
				this.font = font;
			}
			public int id;
			public Colour32 fore;
			public Colour32 back;
			[MarshalAs(UnmanagedType.LPStr)] public string font;
		};

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
			// Get the function pointer for direct calling the WndProc (rather than windows messages)
			var ptr = Win32.SendMessage(Handle, Sci.SCI_GETDIRECTFUNCTION, IntPtr.Zero, IntPtr.Zero);
			m_ptr  = Win32.SendMessage(Handle, Sci.SCI_GETDIRECTPOINTER, IntPtr.Zero, IntPtr.Zero);
			m_func = Marshal_.PtrToDelegate<Sci.DirectFunction>(ptr);

			// Reset the style
			Cmd(Sci.SCI_SETCODEPAGE, Sci.SC_CP_UTF8, 0);
			ClearAll();
			ClearDocumentStyle();
			StyleBits = 7;
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

		/// <summary>Set up this control for Ldr script</summary>
		public void InitLdrStyle(bool dark = false)
		{
			ClearDocumentStyle();
			StyleBits = 7;
			IndentationGuides = true;
			AutoIndent = true;
			TabWidth = 4;
			Indent = 4;
			CaretFore = dark ? 0xFFffffff : 0xFF000000;
			CaretPeriod = 400;
			ConvertEOLs(Sci.SC_EOL_LF);
			EOLMode = Sci.SC_EOL_LF;
			Property("fold", "1");
			MultipleSelection = true;
			AdditionalSelectionTyping = true;
			VirtualSpace = Sci.SCVS_RECTANGULARSELECTION;

			var dark_style = new StyleDesc[]
			{
				new StyleDesc(Sci.STYLE_DEFAULT     , 0xc8c8c8 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.STYLE_LINENUMBER  , 0xc8c8c8 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.STYLE_INDENTGUIDE , 0x484439 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.STYLE_BRACELIGHT  , 0x98642b , 0x5e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_DEFAULT   , 0xc8c8c8 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_COMMENT   , 0x4aa656 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_STRING    , 0x859dd6 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_NUMBER    , 0xf7f7f8 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_KEYWORD   , 0xd69c56 , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_PREPROC   , 0xc563bd , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_OBJECT    , 0x81c93d , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_NAME      , 0xffffff , 0x1e1e1e , "courier new"),
				new StyleDesc(Sci.SCE_LDR_COLOUR    , 0x7c97c3 , 0x1e1e1e , "courier new"),
			};
			var light_style = new StyleDesc[]
			{
				new StyleDesc(Sci.STYLE_DEFAULT     , 0x120700 , 0xffffff , "courier new"),
				new StyleDesc(Sci.STYLE_LINENUMBER  , 0x120700 , 0xffffff , "courier new"),
				new StyleDesc(Sci.STYLE_INDENTGUIDE , 0xc0c0c0 , 0xffffff , "courier new"),
				new StyleDesc(Sci.STYLE_BRACELIGHT  , 0x2b6498 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_DEFAULT   , 0x120700 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_COMMENT   , 0x008100 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_STRING    , 0x154dc7 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_NUMBER    , 0x1e1e1e , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_KEYWORD   , 0xff0000 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_PREPROC   , 0x8a0097 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_OBJECT    , 0x81962a , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_NAME      , 0x000000 , 0xffffff , "courier new"),
				new StyleDesc(Sci.SCE_LDR_COLOUR    , 0x83573c , 0xffffff , "courier new"),
			};
			Debug.Assert(dark_style.Length == light_style.Length);

			var style = dark ? dark_style : light_style;
			for (int i = 0; i != style.Length; ++i)
			{
				var s = style[i];
				StyleSetFont(s.id, s.font);
				StyleSetFore(s.id, s.fore);
				StyleSetBack(s.id, s.back);
			}

			MarginTypeN(0, Sci.SC_MARGIN_NUMBER);
			MarginTypeN(1, Sci.SC_MARGIN_SYMBOL);
			
			MarginMaskN(1, Sci.SC_MASK_FOLDERS);

			MarginWidthN(0, TextWidth(Sci.STYLE_LINENUMBER, "_9999"));
			MarginWidthN(1, 0);

			// set marker symbol for marker type 0 - bookmark
			MarkerDefine(0, Sci.SC_MARK_CIRCLE);

			//// display all margins
			//DisplayLinenumbers(TRUE);
			//SetDisplayFolding(TRUE);
			//SetDisplaySelection(TRUE);

			// Initialise UTF-8 with the ldr lexer
			CodePage = Sci.SC_CP_UTF8;
			Lexer = Sci.SCLEX_LDR;
			LexerLanguage("ldr");
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
		public int Cmd(int code, IntPtr wparam, IntPtr lparam)
		{
			if (Handle == null) return 0;
			return m_func(m_ptr, code, wparam, lparam);
		}
		public int Cmd(int code, int wparam, IntPtr lparam)    { return Cmd(code, (IntPtr)wparam, lparam); }
		public int Cmd(int code, int wparam, int lparam)       { return Cmd(code, (IntPtr)wparam, (IntPtr)lparam); }
		public int Cmd(int code, int wparam)                   { return Cmd(code, (IntPtr)wparam, IntPtr.Zero); }
		public int Cmd(int code)                               { return Cmd(code, IntPtr.Zero, IntPtr.Zero); }

		#region Text

		/// <summary>Clear all text from the control</summary>
		public void ClearAll()
		{
			Cmd(Sci.SCI_CLEARALL);
		}

		/// <summary>Clear style for the document</summary>
		public void ClearDocumentStyle()
		{
			Cmd(Sci.SCI_CLEARDOCUMENTSTYLE);
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

		/// <summary>Get/Set style bits</summary>
		public int StyleBits
		{
			get { return Cmd(Sci.SCI_GETSTYLEBITS); }
			set {  Cmd(Sci.SCI_SETSTYLEBITS, value); }
		}
	
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

		/// <summary></summary>
		public void SelectAll()
		{
			Cmd(Sci.SCI_SELECTALL);
		}

		/// <summary></summary>
		public int SelectionMode
		{
			get { return Cmd(Sci.SCI_GETSELECTIONMODE); }
			set { Cmd(Sci.SCI_SETSELECTIONMODE, value); }
		}

		/// <summary></summary>
		public int CurrentPos
		{
			get { return Cmd(Sci.SCI_GETCURRENTPOS); }
			set { Cmd(Sci.SCI_SETCURRENTPOS, value); }
		}

		/// <summary></summary>
		public int SelectionStart
		{
			get { return Cmd(Sci.SCI_GETSELECTIONSTART); }
			set { Cmd(Sci.SCI_SETSELECTIONSTART, value); }
		}

		/// <summary></summary>
		public int SelectionEnd
		{
			get { return Cmd(Sci.SCI_GETSELECTIONEND); }
			set { Cmd(Sci.SCI_SETSELECTIONEND, value); }
		}

		/// <summary></summary>
		public void SetSel(int start, int end)
		{
			Cmd(Sci.SCI_SETSEL, start, end);
		}

		/// <summary></summary>
		public int GetSelText(out string text)
		{
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_GETSELTEXT, 0, text);
		}

		public int GetCurLine(out string text, int length)
		{
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_GETCURLINE, length, text);
		}
		public int GetLineSelStartPosition(int line)
		{
			return Cmd(Sci.SCI_GETLINESELSTARTPOSITION, line);
		}
		public int GetLineSelEndPosition(int line)
		{
			return Cmd(Sci.SCI_GETLINESELENDPOSITION, line);
		}
		public int GetFirstVisibleLine()
		{
			return Cmd(Sci.SCI_GETFIRSTVISIBLELINE);
		}
		public int LinesOnScreen()
		{
			return Cmd(Sci.SCI_LINESONSCREEN);
		}
		public bool GetModify()
		{
			return Cmd(Sci.SCI_GETMODIFY) != 0;
		}
		public void GotoPos(int pos)
		{
			Cmd(Sci.SCI_GOTOPOS, pos);
		}
		public void GotoLine(int line)
		{
			Cmd(Sci.SCI_GOTOLINE, line);
		}
		public new int Anchor
		{
			get { return Cmd(Sci.SCI_GETANCHOR); }
			set { Cmd(Sci.SCI_SETANCHOR, value); }
		}
		public int LineFromPosition(int pos)
		{
			return Cmd(Sci.SCI_LINEFROMPOSITION, pos);
		}
		public int PositionFromLine(int line)
		{
			return Cmd(Sci.SCI_POSITIONFROMLINE, line);
		}
		public int GetLineEndPosition(int line)
		{
			return Cmd(Sci.SCI_GETLINEENDPOSITION, line);
		}
		public int LineLength(int line)
		{
			return Cmd(Sci.SCI_LINELENGTH, line);
		}
		public int GetColumn(int pos)
		{
			return Cmd(Sci.SCI_GETCOLUMN, pos);
		}
		public int FindColumn(int line, int column)
		{
			return Cmd(Sci.SCI_FINDCOLUMN, line, column);
		}
		public int PositionFromPoint(int x, int y)
		{
			return Cmd(Sci.SCI_POSITIONFROMPOINT, x, y);
		}
		public int PositionFromPointClose(int x, int y)
		{
			return Cmd(Sci.SCI_POSITIONFROMPOINTCLOSE, x, y);
		}
		public int PointXFromPosition(int pos)
		{
			return Cmd(Sci.SCI_POINTXFROMPOSITION, 0, pos);
		}
		public int PointYFromPosition(int pos)
		{
			return Cmd(Sci.SCI_POINTYFROMPOSITION, 0, pos);
		}
		public void HideSelection(bool normal)
		{
			Cmd(Sci.SCI_HIDESELECTION, normal ? 1 : 0);
		}
		public bool SelectionIsRectangle()
		{
			return Cmd(Sci.SCI_SELECTIONISRECTANGLE) != 0;
		}
		public void MoveCaretInsideView()
		{
			Cmd(Sci.SCI_MOVECARETINSIDEVIEW);
		}
		public int WordStartPosition(int pos, bool onlyWordCharacters)
		{
			return Cmd(Sci.SCI_WORDSTARTPOSITION, pos, onlyWordCharacters ? 1 : 0);
		}
		public int WordEndPosition(int pos, bool onlyWordCharacters)
		{
			return Cmd(Sci.SCI_WORDENDPOSITION, pos, onlyWordCharacters ? 1 : 0);
		}
		public int PositionBefore(int pos)
		{
			return Cmd(Sci.SCI_POSITIONBEFORE, pos);
		}
		public int PositionAfter(int pos)
		{
			return Cmd(Sci.SCI_POSITIONAFTER, pos);
		}
		public int TextWidth(int style, string text)
		{ 
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_TEXTWIDTH, style, text);
		}
		public int TextHeight(int line)
		{
			return Cmd(Sci.SCI_TEXTHEIGHT, line);
		}
		public void ChooseCaretX()
		{
			Cmd(Sci.SCI_CHOOSECARETX);
		}

		/// <summary>
		/// Enable/disable multiple selection. When multiple selection is disabled, it is not
		/// possible to select multiple ranges by holding down the Ctrl key while dragging with the mouse.</summary>
		public bool MultipleSelection
		{
			get { return Cmd(Sci.SCI_GETMULTIPLESELECTION) != 0; }
			set { Cmd(Sci.SCI_SETMULTIPLESELECTION, value ? 1 : 0); }
		}
		
		/// <summary>Whether typing, backspace, or delete works with multiple selections simultaneously.</summary>
		public bool AdditionalSelectionTyping
		{
			get { return Cmd(Sci.SCI_GETADDITIONALSELECTIONTYPING) != 0; }
			set { Cmd(Sci.SCI_SETADDITIONALSELECTIONTYPING, value ? 1 : 0); }
		}

		/// <summary>
		/// When pasting into multiple selections, the pasted text can go into just the main selection
		/// with SC_MULTIPASTE_ONCE=0 or into each selection with SC_MULTIPASTE_EACH=1. SC_MULTIPASTE_ONCE is the default.</summary>
		public int MutliPaste
		{
			get { return Cmd(Sci.SCI_GETMULTIPASTE); }
			set { Cmd(Sci.SCI_SETMULTIPASTE, value);  }
		}

		/// <summary>
		/// Virtual space can be enabled or disabled for rectangular selections or in other circumstances or in both.
		/// There are two bit flags SCVS_RECTANGULARSELECTION=1 and SCVS_USERACCESSIBLE=2 which can be set independently.
		/// SCVS_NONE=0, the default, disables all use of virtual space.</summary>
		public int VirtualSpace
		{
			get { return Cmd(Sci.SCI_GETVIRTUALSPACEOPTIONS); }
			set { Cmd(Sci.SCI_SETVIRTUALSPACEOPTIONS, value); }
		}

		/// <summary>Insert/Overwrite</summary>
		public bool Overtype
		{
			get { return Cmd(Sci.SCI_GETOVERTYPE) != 0; }
			set { Cmd(Sci.SCI_SETOVERTYPE, value ? 1 : 0); }
		}

		#endregion

		#region Indenting
		/// <summary>Enable/Disable auto indent mode</summary>
		public bool AutoIndent { get; set; }
		#endregion

		#region Cut, Copy And Paste
		
		/// <summary></summary>
		public void Cut()
		{
			Cmd(Sci.SCI_CUT);
		}

		/// <summary></summary>
		public void Copy()
		{
			Cmd(Sci.SCI_COPY);
		}

		/// <summary></summary>
		public void Paste()
		{
			Cmd(Sci.SCI_PASTE);
		}

		/// <summary></summary>
		public bool CanPaste()
		{
			return Cmd(Sci.SCI_CANPASTE) != 0;
		}

		/// <summary></summary>
		public void Clear()
		{
			Cmd(Sci.SCI_CLEAR);
		}

		/// <summary></summary>
		public void CopyRange(int first, int last)
		{
			Cmd(Sci.SCI_COPYRANGE, first, last);
		}

		/// <summary></summary>
		public void CopyText(StringBuilder text, int length)
		{
			throw new NotImplementedException();
			//Cmd(Sci.SCI_COPYTEXT, length, text);
		}

		/// <summary></summary>
		public void SetPasteConvertEndings(bool convert)
		{
			Cmd(Sci.SCI_SETPASTECONVERTENDINGS, convert ? 1 : 0);
		}

		/// <summary></summary>
		public bool GetPasteConvertEndings()
		{
			return Cmd(Sci.SCI_GETPASTECONVERTENDINGS) != 0;
		}

		#endregion

		#region Undo/Redo
		
		/// <summary></summary>
		public void Undo()
		{
			Cmd(Sci.SCI_UNDO);
		}

		/// <summary></summary>
		public void Redo()
		{
			Cmd(Sci.SCI_REDO);
		}

		/// <summary></summary>
		public bool CanUndo()
		{
			return Cmd(Sci.SCI_CANUNDO) != 0;
		}

		/// <summary></summary>
		public bool CanRedo()
		{
			return Cmd(Sci.SCI_CANREDO) != 0;
		}

		/// <summary></summary>
		public void EmptyUndoBuffer()
		{
			Cmd(Sci.SCI_EMPTYUNDOBUFFER);
		}

		/// <summary></summary>
		public void SetUndoCollection(bool collectUndo)
		{
			Cmd(Sci.SCI_SETUNDOCOLLECTION, collectUndo ? 1 : 0);
		}

		/// <summary></summary>
		public bool GetUndoCollection()
		{
			return Cmd(Sci.SCI_GETUNDOCOLLECTION) != 0;
		}

		/// <summary></summary>
		public void BeginUndoAction()
		{
			Cmd(Sci.SCI_BEGINUNDOACTION);
		}

		/// <summary></summary>
		public void EndUndoAction()
		{
			Cmd(Sci.SCI_ENDUNDOACTION);
		}

		#endregion

		#region Find/Search/Replace

		/// <summary></summary>
		public int Find(int flags, out Sci.TextToFind ttf)
		{
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_FINDTEXT, flags, &ttf);
		}

		/// <summary></summary>
		public void SearchAnchor()
		{
			Cmd(Sci.SCI_SEARCHANCHOR);
		}

		/// <summary></summary>
		public int SearchNext(int flags, string text)
		{
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_SEARCHNEXT, flags, text);
		}

		/// <summary></summary>
		public int SearchPrev(int flags, string text)
		{
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_SEARCHPREV, flags, text);
		}

		/// <summary></summary>
		public int GetTargetStart()
		{
			return Cmd(Sci.SCI_GETTARGETSTART);
		}

		/// <summary></summary>
		public void SetTargetStart(int pos)
		{
			Cmd(Sci.SCI_SETTARGETSTART, pos);
		}

		/// <summary></summary>
		public int GetTargetEnd()
		{
			return Cmd(Sci.SCI_GETTARGETEND);
		}

		/// <summary></summary>
		public void SetTargetEnd(int pos)
		{
			Cmd(Sci.SCI_SETTARGETEND, pos);
		}

		/// <summary></summary>
		public void TargetFromSelection()
		{
			Cmd(Sci.SCI_TARGETFROMSELECTION);
		}

		/// <summary></summary>
		public int GetSearchFlags()
		{
			return Cmd(Sci.SCI_GETSEARCHFLAGS);
		}

		/// <summary></summary>
		public void SetSearchFlags(int flags)
		{
			Cmd(Sci.SCI_SETSEARCHFLAGS, flags);
		}

		/// <summary></summary>
		public int SearchInTarget(string text, int length)
		{
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_SEARCHINTARGET, length, text);
		}

		/// <summary></summary>
		public int ReplaceTarget(string text, int length)
		{
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_REPLACETARGET, length, text);
		}

		/// <summary></summary>
		public int ReplaceTargetRE(string text, int length)
		{
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_REPLACETARGETRE, length, text);
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

		#region DragDrop
		public override bool AllowDrop
		{
			get { return (Win32.GetWindowLong(Handle, Win32.GWL_EXSTYLE) & Win32.WS_EX_ACCEPTFILES) != 0; }
			set { Win32.DragAcceptFiles(Handle, value); }
		}
		#endregion

		#region End of Line

		/// <summary></summary>
		public int EOLMode
		{
			get { return Cmd(Sci.SCI_GETEOLMODE); }
			set { Cmd(Sci.SCI_SETEOLMODE, value); }
		}

		/// <summary></summary>
		public void ConvertEOLs(int eolMode)
		{
			Cmd(Sci.SCI_CONVERTEOLS, eolMode);
		}

		/// <summary></summary>
		public bool ViewEOL
		{
			get { return Cmd(Sci.SCI_GETVIEWEOL) != 0; }
			set { Cmd(Sci.SCI_SETVIEWEOL, value ? 1 : 0); }
		}

		#endregion

		#region Style

		/// <summary></summary>
		public void StyleClearAll()
		{
			Cmd(Sci.SCI_STYLECLEARALL);
		}

		/// <summary></summary>
		public void StyleSetFont(int style, string fontName)
		{
			throw new NotImplementedException();
			//Cmd(Sci.SCI_STYLESETFONT, style, fontName);
		}

		/// <summary></summary>
		public void StyleSetSize(int style, int sizePoints)
		{
			Cmd(Sci.SCI_STYLESETSIZE, style, sizePoints);
		}

		/// <summary></summary>
		public void StyleSetBold(int style, bool bold)
		{
			Cmd(Sci.SCI_STYLESETBOLD, style, bold ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetItalic(int style, bool italic)
		{
			Cmd(Sci.SCI_STYLESETITALIC, style, italic ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetUnderline(int style, bool underline)
		{
			Cmd(Sci.SCI_STYLESETUNDERLINE, style, underline ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetFore(int style, Colour32 fore)
		{
			Cmd(Sci.SCI_STYLESETFORE, style, (int)fore.ARGB);
		}

		/// <summary></summary>
		public void StyleSetBack(int style, Colour32 back)
		{
			Cmd(Sci.SCI_STYLESETBACK, style, (int)back.ARGB);
		}

		/// <summary></summary>
		public void StyleSetEOLFilled(int style, bool filled)
		{
			Cmd(Sci.SCI_STYLESETEOLFILLED, style, filled ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetCharacterSet(int style, int characterSet)
		{
			Cmd(Sci.SCI_STYLESETCHARACTERSET, style, characterSet);
		}

		/// <summary></summary>
		public void StyleSetCase(int style, int caseForce)
		{
			Cmd(Sci.SCI_STYLESETCASE, style, caseForce);
		}

		/// <summary></summary>
		public void StyleSetVisible(int style, bool visible)
		{
			Cmd(Sci.SCI_STYLESETVISIBLE, style, visible ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetChangeable(int style, bool changeable)
		{
			Cmd(Sci.SCI_STYLESETCHANGEABLE, style, changeable ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetHotSpot(int style, bool hotspot)
		{
			Cmd(Sci.SCI_STYLESETHOTSPOT, style, hotspot ? 1 : 0);
		}

		/// <summary></summary>
		public int GetEndStyled()
		{
			return Cmd(Sci.SCI_GETENDSTYLED);
		}

		/// <summary></summary>
		public void StartStyling(int pos, int mask)
		{
			Cmd(Sci.SCI_STARTSTYLING, pos, mask);
		}

		/// <summary></summary>
		public void SetStyling(int length, int style)
		{
			Cmd(Sci.SCI_SETSTYLING, length, style);
		}

		/// <summary></summary>
		public void SetStylingEx(int length, string styles)
		{
			throw new NotImplementedException();
			//Cmd(Sci.SCI_SETSTYLINGEX, length, styles);
		}

		/// <summary></summary>
		public int LineState(int line)
		{
			return Cmd(Sci.SCI_GETLINESTATE, line);
		}
		public void LineState(int line, int state)
		{
			Cmd(Sci.SCI_SETLINESTATE, line, state);
		}

		/// <summary></summary>
		public int GetMaxLineState()
		{
			return Cmd(Sci.SCI_GETMAXLINESTATE);
		}

		/// <summary></summary>
		public void StyleResetDefault()
		{
			Cmd(Sci.SCI_STYLERESETDEFAULT);
		}
	
		#endregion

		#region Control Char Symbol

		/// <summary></summary>
		public int GetControlCharSymbol()
		{
			return Cmd(Sci.SCI_GETCONTROLCHARSYMBOL);
		}

		/// <summary></summary>
		public void SetControlCharSymbol(int symbol)
		{
			Cmd(Sci.SCI_SETCONTROLCHARSYMBOL, symbol);
		}

		#endregion

		#region Caret Style

		/// <summary></summary>
		public void SetXCaretPolicy(int caretPolicy, int caretSlop)
				{
					Cmd(Sci.SCI_SETXCARETPOLICY, caretPolicy, caretSlop);
				}
		public void SetYCaretPolicy(int caretPolicy, int caretSlop)
		{
			Cmd(Sci.SCI_SETYCARETPOLICY, caretPolicy, caretSlop);
		}

		/// <summary></summary>
		public void SetVisiblePolicy(int visiblePolicy, int visibleSlop)
		{
			Cmd(Sci.SCI_SETVISIBLEPOLICY, visiblePolicy, visibleSlop);
		}

		/// <summary></summary>
		public void ToggleCaretSticky()
		{
			Cmd(Sci.SCI_TOGGLECARETSTICKY);
		}

		/// <summary></summary>
		public Colour32 CaretFore
		{
			get { return (uint)Cmd(Sci.SCI_GETCARETFORE); }
			set { Cmd(Sci.SCI_SETCARETFORE, (int)value.ARGB); }
		}

		/// <summary></summary>
		public bool CaretLineVisible
		{
			get { return Cmd(Sci.SCI_GETCARETLINEVISIBLE) != 0; }
			set { Cmd(Sci.SCI_SETCARETLINEVISIBLE, value ? 1 : 0); }
		}

		/// <summary></summary>
		public Colour32 CaretLineBack
		{
			get { return (uint)Cmd(Sci.SCI_GETCARETLINEBACK); }
			set { Cmd(Sci.SCI_SETCARETLINEBACK, (int)value.ARGB); }
		}

		/// <summary>Caret blink period (in ms)</summary>
		public int CaretPeriod
		{
			get { return Cmd(Sci.SCI_GETCARETPERIOD); }
			set { Cmd(Sci.SCI_SETCARETPERIOD, value); }
		}

		/// <summary></summary>
		public int CaretWidth
		{
			get { return Cmd(Sci.SCI_GETCARETWIDTH); }
			set { Cmd(Sci.SCI_SETCARETWIDTH, value); }
		}

		/// <summary></summary>
		public bool CaretSticky
		{
			get { return Cmd(Sci.SCI_GETCARETSTICKY) != 0; }
			set { Cmd(Sci.SCI_SETCARETSTICKY, value ? 1 : 0); }
		}

		#endregion

		#region Selection Style
		
		/// <summary></summary>
		public void SetSelFore(bool useSetting, Colour32 fore)
		{
			Cmd(Sci.SCI_SETSELFORE, useSetting ? 1 : 0, (int)fore.ARGB);
		}

		/// <summary></summary>
		public void SetSelBack(bool useSetting, Colour32 back)
		{
			Cmd(Sci.SCI_SETSELBACK, useSetting ? 1 : 0, (int)back.ARGB);
		}

		#endregion

		#region Hotspot Style

		/// <summary></summary>
		public void SetHotspotActiveFore(bool useSetting, Colour32 fore)
		{
			Cmd(Sci.SCI_SETHOTSPOTACTIVEFORE, useSetting ? 1 : 0, (int)fore.ARGB);
		}
		
		/// <summary></summary>
		public void SetHotspotActiveBack(bool useSetting, Colour32 back)
		{
			Cmd(Sci.SCI_SETHOTSPOTACTIVEBACK, useSetting ? 1 : 0, (int)back.ARGB);
		}
		
		/// <summary></summary>
		public void SetHotspotActiveUnderline(bool underline)
		{
			Cmd(Sci.SCI_SETHOTSPOTACTIVEUNDERLINE, underline ? 1 : 0);
		}
		
		/// <summary></summary>
		public void SetHotspotSingleLine(bool singleLine)
		{
			Cmd(Sci.SCI_SETHOTSPOTSINGLELINE, singleLine ? 1 : 0);
		}

		#endregion

		#region Margins

		/// <summary></summary>
		public int MarginTypeN(int margin)
		{
			return Cmd(Sci.SCI_GETMARGINTYPEN, margin);
		}

		/// <summary></summary>
		public void MarginTypeN(int margin, int marginType)
		{
			Cmd(Sci.SCI_SETMARGINTYPEN, margin, marginType);
		}

		/// <summary></summary>
		public int MarginWidthN(int margin)
		{
			return Cmd(Sci.SCI_GETMARGINWIDTHN, margin);
		}

		/// <summary></summary>
		public void MarginWidthN(int margin, int pixelWidth)
		{
			Cmd(Sci.SCI_SETMARGINWIDTHN, margin, pixelWidth);
		}

		/// <summary></summary>
		public int MarginMaskN(int margin)
		{
			return Cmd(Sci.SCI_GETMARGINMASKN, margin);
		}

		/// <summary></summary>
		public void MarginMaskN(int margin, int mask)
		{
			Cmd(Sci.SCI_SETMARGINMASKN, margin, mask);
		}

		/// <summary></summary>
		public bool MarginSensitiveN(int margin)
		{
			return Cmd(Sci.SCI_GETMARGINSENSITIVEN, margin) != 0;
		}

		/// <summary></summary>
		public void MarginSensitiveN(int margin, bool sensitive)
		{
			Cmd(Sci.SCI_SETMARGINSENSITIVEN, margin, sensitive ? 1 : 0);
		}

		/// <summary></summary>
		public int MarginLeft()
		{
			return Cmd(Sci.SCI_GETMARGINLEFT);
		}

		/// <summary></summary>
		public void MarginLeft(int pixelWidth)
		{
			Cmd(Sci.SCI_SETMARGINLEFT, 0, pixelWidth);
		}

		/// <summary></summary>
		public int MarginRight
		{
			get { return Cmd(Sci.SCI_GETMARGINRIGHT); }
			set { Cmd(Sci.SCI_SETMARGINRIGHT, 0, value); }
		}

		#endregion

		#region Brace Highlighting
		
		/// <summary></summary>
		public void BraceHighlight(int pos1, int pos2)
		{
			Cmd(Sci.SCI_BRACEHIGHLIGHT, pos1, pos2);
		}

		/// <summary></summary>
		public void BraceBadLight(int pos)
		{
			Cmd(Sci.SCI_BRACEBADLIGHT, pos);
		}

		/// <summary></summary>
		public int BraceMatch(int pos)
		{
			return Cmd(Sci.SCI_BRACEMATCH, pos);
		}

		#endregion

		#region Tabs

		/// <summary>Tab width</summary>
		public int TabWidth
		{
			get { return Cmd(Sci.SCI_GETTABWIDTH); }
			set { Cmd(Sci.SCI_SETTABWIDTH, value); }
		}

		/// <summary></summary>
		public bool UseTabs
		{
			get { return Cmd(Sci.SCI_GETUSETABS) != 0; }
			set { Cmd(Sci.SCI_SETUSETABS, value ? 1 : 0); }
		}

		/// <summary></summary>
		public int Indent
		{
			get { return Cmd(Sci.SCI_GETINDENT); }
			set { Cmd(Sci.SCI_SETINDENT, value); }
		}

		/// <summary></summary>
		public bool TabIndents
		{
			get { return Cmd(Sci.SCI_GETTABINDENTS) != 0; }
			set { Cmd(Sci.SCI_SETTABINDENTS, value ? 1 : 0); }
		}

		/// <summary></summary>
		public bool BackSpaceUnIndents
		{
			get { return Cmd(Sci.SCI_GETBACKSPACEUNINDENTS) != 0; }
			set { Cmd(Sci.SCI_SETBACKSPACEUNINDENTS, value ? 1 : 0); }
		}

		/// <summary></summary>
		public int LineIndentation(int line)
		{
			return Cmd(Sci.SCI_GETLINEINDENTATION, line);
		}
		public void LineIndentation(int line, int indentSize)
		{
			Cmd(Sci.SCI_SETLINEINDENTATION, line, indentSize);
		}

		/// <summary></summary>
		public int LineIndentPosition(int line)
		{
			return Cmd(Sci.SCI_GETLINEINDENTPOSITION, line);
		}

		/// <summary></summary>
		public bool IndentationGuides
		{
			get { return Cmd(Sci.SCI_GETINDENTATIONGUIDES) != 0; }
			set { Cmd(Sci.SCI_SETINDENTATIONGUIDES, value ? 1 : 0); }
		}

		/// <summary></summary>
		public int HighlightGuide
		{
			get { return Cmd(Sci.SCI_GETHIGHLIGHTGUIDE); }
			set { Cmd(Sci.SCI_SETHIGHLIGHTGUIDE, value); }
		}

		#endregion

		#region Markers

		/// <summary></summary>
		public void MarkerDefine(int markerNumber, int markerSymbol)
		{
			Cmd(Sci.SCI_MARKERDEFINE, markerNumber, markerSymbol);
		}

		/// <summary></summary>
		public void MarkerDefinePixmap(int markerNumber, byte[] pixmap)
		{
			throw new NotImplementedException();
			//Cmd(Sci.SCI_MARKERDEFINEPIXMAP, markerNumber, pixmap);
		}

		/// <summary></summary>
		public void MarkerSetFore(int markerNumber, Colour32 fore)
		{
			Cmd(Sci.SCI_MARKERSETFORE, markerNumber, (int)fore.ARGB);
		}

		/// <summary></summary>
		public void MarkerSetBack(int markerNumber, Colour32 back)
		{
			Cmd(Sci.SCI_MARKERSETBACK, markerNumber, (int)back.ARGB);
		}

		/// <summary></summary>
		public int MarkerAdd(int line, int markerNumber)
		{
			return Cmd(Sci.SCI_MARKERADD, line, markerNumber);
		}

		/// <summary></summary>
		public int MarkerAddSet(int line, int markerNumber)
		{
			return Cmd(Sci.SCI_MARKERADDSET, line, markerNumber);
		}

		/// <summary></summary>
		public void MarkerDelete(int line, int markerNumber)
		{
			Cmd(Sci.SCI_MARKERDELETE, line, markerNumber);
		}

		/// <summary></summary>
		public void MarkerDeleteAll(int markerNumber)
		{
			Cmd(Sci.SCI_MARKERDELETEALL, markerNumber);
		}

		/// <summary></summary>
		public int MarkerGet(int line)
		{
			return Cmd(Sci.SCI_MARKERGET, line);
		}

		/// <summary></summary>
		public int MarkerNext(int lineStart, int markerMask)
		{
			return Cmd(Sci.SCI_MARKERNEXT, lineStart, markerMask);
		}

		/// <summary></summary>
		public int MarkerPrevious(int lineStart, int markerMask)
		{
			return Cmd(Sci.SCI_MARKERPREVIOUS, lineStart, markerMask );
		}

		/// <summary></summary>
		public int MarkerLineFromHandle(int handle)
		{
			return Cmd(Sci.SCI_MARKERLINEFROMHANDLE, handle);
		}

		/// <summary></summary>
		public void MarkerDeleteHandle(int handle)
		{
			Cmd(Sci.SCI_MARKERDELETEHANDLE, handle);
		}

		#endregion

		#region Lexer

		/// <summary></summary>
		public int Lexer
		{
			get { return Cmd(Sci.SCI_GETLEXER); }
			set { Cmd(Sci.SCI_SETLEXER, value); }
		}

		/// <summary></summary>
		public void LexerLanguage(string language)
		{
			var bytes = Encoding.UTF8.GetBytes(language);
			using (var handle = GCHandleEx.Alloc(bytes, GCHandleType.Pinned))
				Cmd(Sci.SCI_SETLEXERLANGUAGE, 0, handle.Handle.AddrOfPinnedObject());
		}

		/// <summary></summary>
		public void LoadLexerLibrary(string path)
		{
			var bytes = Encoding.UTF8.GetBytes(path);
			using (var handle = GCHandleEx.Alloc(bytes, GCHandleType.Pinned))
				Cmd(Sci.SCI_LOADLEXERLIBRARY, 0, handle.Handle.AddrOfPinnedObject());
		}

		/// <summary></summary>
		public void Colourise(int start, int end)
		{
			Cmd(Sci.SCI_COLOURISE, start, end);
		}

		/// <summary>Get/Set a property by key</summary>
		public string Property(string key)
		{
			throw new NotImplementedException();
			//todo return Cmd(Sci.SCI_GETPROPERTY, key, buf );
		}
		public void Property(string key, string value)
		{
			throw new NotImplementedException();
			//todo Cmd(Sci.SCI_SETPROPERTY, key, value);
		}
		public string PropertyExpanded(string key)
		{
			throw new NotImplementedException();
			//Cmd(Sci.SCI_GETPROPERTYEXPANDED, key, buf);
		}

		/// <summary></summary>
		public int PropertyInt(string key)
		{
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_GETPROPERTYINT, key);
		}

		/// <summary></summary>
		public void SetKeyWords(int keywordSet, string[] keyWords)
		{
			throw new NotImplementedException();
			//Cmd(Sci.SCI_SETKEYWORDS, keywordSet, keyWords);
		}

		/// <summary></summary>
		public int GetStyleBitsNeeded()
		{
			return Cmd(Sci.SCI_GETSTYLEBITSNEEDED);
		}

		#endregion

		#region Misc

		/// <summary></summary>
		void Allocate(int bytes)
		{
			Cmd(Sci.SCI_ALLOCATE, bytes);
		}

		/// <summary></summary>
		public void SetSavePoint()
		{
			Cmd(Sci.SCI_SETTEXT);
		}

		/// <summary></summary>
		public bool BufferedDraw
		{
			get { return Cmd(Sci.SCI_GETBUFFEREDDRAW) != 0; }
			set { Cmd(Sci.SCI_SETBUFFEREDDRAW, value ? 1 : 0); }
		}

		/// <summary></summary>
		public bool TwoPhaseDraw
		{
			get { return Cmd(Sci.SCI_GETTWOPHASEDRAW) != 0; }
			set { Cmd(Sci.SCI_SETTWOPHASEDRAW, value ? 1 : 0); }
		}

		/// <summary></summary>
		public int CodePage
		{
			get { return Cmd(Sci.SCI_GETCODEPAGE); }
			set { Cmd(Sci.SCI_SETCODEPAGE, value); }
		}

		/// <summary></summary>
		public void SetWordChars(string characters)
		{
			throw new NotImplementedException();
			//Cmd(Sci.SCI_SETWORDCHARS, 0, characters);
		}

		/// <summary></summary>
		public void SetWhitespaceChars(string characters)
		{
			throw new NotImplementedException();
			//Cmd(Sci.SCI_SETWHITESPACECHARS, 0, characters);
		}

		/// <summary></summary>
		public void SetCharsDefault()
		{
			Cmd(Sci.SCI_SETCHARSDEFAULT);
		}

		/// <summary></summary>
		public void GrabFocus()
		{
			Cmd(Sci.SCI_GRABFOCUS);
		}

		/// <summary></summary>
		public new bool Focus
		{
			get { return Cmd(Sci.SCI_GETFOCUS) != 0; }
			set { Cmd(Sci.SCI_SETFOCUS, value ? 1 : 0); }
		}

		/// <summary></summary>
		public bool ReadOnly
		{
			get { return Cmd(Sci.SCI_GETREADONLY) != 0; }
			set { Cmd(Sci.SCI_SETREADONLY, value ? 1 : 0); }
		}

		#endregion

		#region Status/Errors

		/// <summary></summary>
		public int Status
		{
			get { return Cmd(Sci.SCI_GETSTATUS); }
			set { Cmd(Sci.SCI_SETSTATUS, value); }
		}

		#endregion
	}
}
