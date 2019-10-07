using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Interop.Win32;
using Rylogic.Scintilla;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class ScintillaControl :UserControl
	{
		// Notes:
		//  - See http://www.scintilla.org/ScintillaDoc.html for documentation
		//  - This is roughly a port of pr::gui::ScintillaCtrl
		//  - Function comments are mostly incomplete because it'd take too long.. Fill them in as needed
		//  - Copy/port #pragma regions from the native control on demand
		//  - Dispose is handled internally via the Closed event on the owning window.

		static ScintillaControl()
		{
			Sci.LoadDll(throw_if_missing: false);
		}
		public ScintillaControl()
		{
			InitializeComponent();
			CreateNativeControl();
		}

		/// <summary>Instantiate the native scintilla control</summary>
		private void CreateNativeControl()
		{
			m_native_control = new NativeControl();
			m_native_control.Initialized += HandleInitialised;
			m_native_control.MessageHook += HandleMessages;
			m_native_control.HorizontalAlignment = HorizontalAlignment.Stretch;
			m_native_control.VerticalAlignment = VerticalAlignment.Stretch;
			m_native_control.Loaded += (s,a) =>
			{
				// When the owning window is closed, call dispose on the native control
				var owner = Window.GetWindow((DependencyObject)s) ?? throw new Exception("Native scintilla control must be a child of a window");
				owner.Closed += HandleClosed;
			};

			// Add the native control as a child of the host
			m_host.Child = m_native_control;

			void HandleInitialised(object? sender, EventArgs e)
			{
				// Reset the style
				CodePage = Sci.SC_CP_UTF8;
				Cursor = Cursors.IBeam;
				ClearAll();
				ClearDocumentStyle();
				StyleBits = 7;
				TabWidth = 4;
				Indent = 4;
			}
			void HandleClosed(object sender, EventArgs e)
			{
				if (m_native_control == null) return;
				m_native_control.Initialized -= HandleInitialised;
				m_native_control.MessageHook -= HandleMessages;
				Util.Dispose(ref m_native_control!);
			}
			IntPtr HandleMessages(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
			{
				switch (msg)
				{
				case Win32.WM_CLOSE:
					{
						break;
					}
				case Win32.WM_COMMAND:
					{
						// Watch for edit notifications
						var notif = Win32.HiWord(wparam.ToInt32());
						var id = Win32.LoWord(wparam.ToInt32());
						if (notif == Win32.EN_CHANGE)// && id == Id)
							TextChanged?.Invoke(this, EventArgs.Empty);
						break;
					}
				case Win32.WM_REFLECT + Win32.WM_NOTIFY:
					{
						var nmhdr = Marshal_.PtrToStructure<Win32.NMHDR>(lparam);
						var notif = Marshal_.PtrToStructure<Sci.SCNotification>(lparam);
						HandleSCNotification(ref nmhdr, ref notif);
						break;
					}
				}
				return IntPtr.Zero;
			}
			void HandleSCNotification(ref Win32.NMHDR nmhdr, ref global::Scintilla.Scintilla.SCNotification notif)
			{
						/// <summary>Handle notification from the native scintilla control</summary>
				switch (notif.nmhdr.code)
				{
				case Sci.SCN_CHARADDED:
					{
						if (AutoIndent)
						{
							var lem = EOLMode;
							var lend =
								(lem == Sci.EEndOfLineMode.CR && notif.ch == '\r') ||
								(lem == Sci.EEndOfLineMode.LF && notif.ch == '\n') ||
								(lem == Sci.EEndOfLineMode.CRLF && notif.ch == '\n');
							if (lend)
							{
								var line = LineFromPosition(CurrentPos);
								var indent = line > 0 ? LineIndentation(line - 1) : 0;
								LineIndentation(line, indent);
								GotoPos(FindColumn(line, indent));
							}
						}

						// Notify text changed
						TextChanged?.Invoke(this, EventArgs.Empty);
						break;
					}
				case Sci.SCN_UPDATEUI:
					{
						switch (notif.updated)
						{
						case Sci.SC_UPDATE_SELECTION:
							{
								OnSelectionChanged();
								break;
							}
						case Sci.SC_UPDATE_H_SCROLL:
						case Sci.SC_UPDATE_V_SCROLL:
							{
								//var ori = notif.nmhdr.code == Sci.SC_UPDATE_H_SCROLL ? ScrollOrientation.HorizontalScroll : ScrollOrientation.VerticalScroll;
								//OnScroll(new ScrollEventArgs(ScrollEventType.ThumbPosition, 0, ori));
								break;
							}
						}
						break;
					}
				}
			}
		}
		private NativeControl m_native_control = null!;

		/// <summary>Call the direct function</summary>
		public int Cmd(int code, IntPtr wparam, IntPtr lparam) => (int)m_native_control.Call(code, wparam, lparam);
		public int Cmd(int code, int wparam, IntPtr lparam) => Cmd(code, (IntPtr)wparam, lparam);
		public int Cmd(int code, int wparam, int lparam) => Cmd(code, (IntPtr)wparam, (IntPtr)lparam);
		public int Cmd(int code, int wparam) => Cmd(code, (IntPtr)wparam, IntPtr.Zero);
		public int Cmd(int code) => Cmd(code, IntPtr.Zero, IntPtr.Zero);

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
		public int TextLength => Cmd(Sci.SCI_GETTEXTLENGTH);

		/// <summary>Gets or sets the current text</summary>
		public string Text
		{
			get => GetText();
			set => SetText(value);
		}

		/// <summary>Read the text out of the control</summary>
		private string GetText()
		{
			var len = TextLength;
			if (len == 0)
				return string.Empty;

			var bytes = new byte[len];
			using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			var num = Cmd(Sci.SCI_GETTEXT, len + 1, h.Handle.AddrOfPinnedObject());
			return Encoding.UTF8.GetString(bytes, 0, num);
		}

		/// <summary>Set the text in the control</summary>
		private void SetText(string text)
		{
			if (text.Length == 0)
			{
				ClearAll();
			}
			else
			{
				// Convert the string to UTF-8
				var bytes = Encoding.UTF8.GetBytes(text);
				using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
				Cmd(Sci.SCI_SETTEXT, 0, h.Handle.AddrOfPinnedObject());
			}

			TextChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Raised when text in the control is changed</summary>
		public event EventHandler? TextChanged;

		/// <summary></summary>
		public char GetCharAt(int pos)
		{
			return (char)(Cmd(Sci.SCI_GETCHARAT, pos) & 0xFF);
		}

		/// <summary>Return a line of text</summary>
		public string GetLine(int line)
		{
			var len = LineLength(line);
			var bytes = new byte[len];
			using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			var num = Cmd(Sci.SCI_GETLINE, line, h.Handle.AddrOfPinnedObject());
			return Encoding.UTF8.GetString(bytes, 0, num);
		}

		/// <summary></summary>
		public int LineCount => Cmd(Sci.SCI_GETLINECOUNT);

		/// <summary></summary>
		public Range TextRange
		{
			get
			{
				throw new NotImplementedException();
				//Sci.TextRange tr;
				//Cmd(Sci.SCI_GETTEXTRANGE, 0, &tr);
			}
		}

		/// <summary></summary>
		public void AppendText(string text)
		{
			// Convert the string to UTF-8
			var bytes = Encoding.UTF8.GetBytes(text);
			using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			Cmd(Sci.SCI_APPENDTEXT, bytes.Length, h.Handle.AddrOfPinnedObject());
		}

		/// <summary>Append text and style data to the document</summary>
		public void AppendStyledText(Sci.CellBuf cells)
		{
			using var c = cells.Pin();
			Cmd(Sci.SCI_APPENDSTYLEDTEXT, c.SizeInBytes, c.Pointer);
		}

		/// <summary></summary>
		public void InsertText(int pos, string text)
		{
			// Ensure null termination
			var bytes = Encoding.UTF8.GetBytes(text + "\0");
			using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			Cmd(Sci.SCI_INSERTTEXT, pos, h.Handle.AddrOfPinnedObject());
		}

		/// <summary></summary>
		public void ReplaceSel(string text)
		{
			// Ensure null termination
			var bytes = Encoding.UTF8.GetBytes(text + "\0");
			using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			Cmd(Sci.SCI_REPLACESEL, 0, h.Handle.AddrOfPinnedObject());
		}

		/// <summary></summary>
		public new void AddText(string text)
		{
			var bytes = Encoding.UTF8.GetBytes(text);
			using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			Cmd(Sci.SCI_ADDTEXT, bytes.Length, h.Handle.AddrOfPinnedObject());
		}

		/// <summary></summary>
		public void AddStyledText(string text)
		{
			var bytes = Encoding.UTF8.GetBytes(text);
			using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			Cmd(Sci.SCI_ADDSTYLEDTEXT, bytes.Length, h.Handle.AddrOfPinnedObject());
		}

		/// <summary>Insert text and style data into the document at 'pos'</summary>
		public void InsertStyledText(int pos, Sci.CellBuf cells)
		{
			using var c = cells.Pin();
			Cmd(Sci.SCI_INSERTSTYLEDTEXT, pos, c.Pointer);
		}

		/// <summary>Delete a character range from the document</summary>
		public void DeleteRange(int start, int length)
		{
			Cmd(Sci.SCI_DELETERANGE, start, length);
		}

		/// <summary></summary>
		public int GetStyleAt(int pos)
		{
			return Cmd(Sci.SCI_GETSTYLEAT, pos);
		}

		/// <summary></summary>
		public int GetStyledText(out Sci.TextRange tr)
		{
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_GETSTYLEDTEXT, 0, &tr);
		}
		public int GetStyledText(StringBuilder text, long first, long last)
		{
			throw new NotImplementedException();
			//TxtRng tr(text, first, last);
			//return Cmd(Sci.SCI_GETSTYLEDTEXT, 0, &tr);
		}

		/// <summary>Get/Set style bits</summary>
		public int StyleBits
		{
			get => Cmd(Sci.SCI_GETSTYLEBITS);
			set => Cmd(Sci.SCI_SETSTYLEBITS, value);
		}

		/// <summary></summary>
		public int TargetAsUTF8(StringBuilder text)
		{
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_TARGETASUTF8, 0, text);
		}

		/// <summary></summary>
		public int EncodedFromUTF8(byte[] utf8, byte[] encoded)
		{
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_ENCODEDFROMUTF8, (WPARAM)utf8, (LPARAM )encoded);
		}

		/// <summary></summary>
		public void SetLengthForEncode(int bytes)
		{
			Cmd(Sci.SCI_SETLENGTHFORENCODE, bytes);
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
				m_anchor = anchor;
			}
			public static Selection Save(ScintillaControl ctrl)
			{
				return new Selection(
					ctrl.Cmd(Sci.SCI_GETCURRENTPOS),
					ctrl.Cmd(Sci.SCI_GETANCHOR));
			}
			public static void Restore(ScintillaControl ctrl, Selection sel)
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
		public event EventHandler? SelectionChanged;
		protected virtual void OnSelectionChanged()
		{
			SelectionChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary></summary>
		public void SelectAll()
		{
			Cmd(Sci.SCI_SELECTALL);
		}

		/// <summary></summary>
		public int SelectionMode
		{
			get => Cmd(Sci.SCI_GETSELECTIONMODE);
			set => Cmd(Sci.SCI_SETSELECTIONMODE, value);
		}

		/// <summary></summary>
		public int CurrentPos
		{
			get => Cmd(Sci.SCI_GETCURRENTPOS);
			set => Cmd(Sci.SCI_SETCURRENTPOS, value);
		}

		/// <summary></summary>
		public int SelectionStart
		{
			get => Cmd(Sci.SCI_GETSELECTIONSTART);
			set => Cmd(Sci.SCI_SETSELECTIONSTART, value);
		}

		/// <summary></summary>
		public int SelectionEnd
		{
			get => Cmd(Sci.SCI_GETSELECTIONEND);
			set => Cmd(Sci.SCI_SETSELECTIONEND, value);
		}

		/// <summary>Set the range of selected text</summary>
		public void SetSel(int start, int end)
		{
			Cmd(Sci.SCI_SETSEL, start, end);
		}

		/// <summary>
		/// Returns the currently selected text. This method allows for rectangular and discontiguous
		/// selections as well as simple selections. See Multiple Selection for information on how multiple
		/// and rectangular selections and virtual space are copied.</summary>
		public string GetSelText()
		{
			var len = Cmd(Sci.SCI_GETSELTEXT);
			var buf = new byte[len];
			using var t = GCHandle_.Alloc(buf, GCHandleType.Pinned);
			len = Cmd(Sci.SCI_GETSELTEXT, 0, t.Handle.AddrOfPinnedObject());
			return Encoding.UTF8.GetString(buf, 0, len);
		}

		/// <summary>Retrieves the text of the line containing the caret and returns the position within the line of the caret.</summary>
		public string GetCurLine(out int caret_offset)
		{
			var len = Cmd(Sci.SCI_GETCURLINE);
			var buf = new byte[len];
			using var t = GCHandle_.Alloc(buf, GCHandleType.Pinned);
			caret_offset = Cmd(Sci.SCI_GETCURLINE, buf.Length, t.Handle.AddrOfPinnedObject());
			return Encoding.UTF8.GetString(buf, 0, len);
		}
		public string GetCurLine()
		{
			return GetCurLine(out var caret_offset);
		}

		/// <summary></summary>
		public int GetLineSelStartPosition(int line)
		{
			return Cmd(Sci.SCI_GETLINESELSTARTPOSITION, line);
		}

		/// <summary></summary>
		public int GetLineSelEndPosition(int line)
		{
			return Cmd(Sci.SCI_GETLINESELENDPOSITION, line);
		}

		/// <summary>Get/Set the index of the first visible line</summary>
		public int FirstVisibleLine
		{
			get => Cmd(Sci.SCI_GETFIRSTVISIBLELINE);
			set => Cmd(Sci.SCI_SETFIRSTVISIBLELINE, value);
		}

		/// <summary>Get the number of lines visible on screen</summary>
		public int LinesOnScreen => Cmd(Sci.SCI_LINESONSCREEN);

		/// <summary></summary>
		public bool GetModify()
		{
			return Cmd(Sci.SCI_GETMODIFY) != 0;
		}

		/// <summary>
		/// Removes any selection, sets the caret at caret and scrolls the view to make the caret visible, if necessary.
		/// It is equivalent to SCI_SETSEL(caret, caret). The anchor position is set the same as the current position.</summary>
		public void GotoPos(int pos)
		{
			Cmd(Sci.SCI_GOTOPOS, pos);
		}

		/// <summary>
		/// Removes any selection and sets the caret at the start of line number line and scrolls the view (if needed)
		/// to make it visible. The anchor position is set the same as the current position. If line is outside the lines
		/// in the document (first line is 0), the line set is the first or last.</summary>
		public void GotoLine(int line)
		{
			Cmd(Sci.SCI_GOTOLINE, line);
		}

		/// <summary></summary>
		public int Anchor
		{
			get => Cmd(Sci.SCI_GETANCHOR);
			set => Cmd(Sci.SCI_SETANCHOR, value);
		}

		/// <summary>Get the line index from a character index</summary>
		public int LineFromPosition(int pos)
		{
			return Cmd(Sci.SCI_LINEFROMPOSITION, pos);
		}

		/// <summary>Return the character index for the start of 'line'</summary>
		public int PositionFromLine(int line)
		{
			return Cmd(Sci.SCI_POSITIONFROMLINE, line);
		}

		/// <summary></summary>
		public int GetLineEndPosition(int line)
		{
			return Cmd(Sci.SCI_GETLINEENDPOSITION, line);
		}

		/// <summary>Return the length of line 'line'</summary>
		public int LineLength(int line)
		{
			return Cmd(Sci.SCI_LINELENGTH, line);
		}

		/// <summary>
		/// Returns the column number of a position 'pos' within the document taking the width of tabs into account.
		/// This returns the column number of the last tab on the line before pos, plus the number of characters between
		/// the last tab and pos. If there are no tab characters on the line, the return value is the number of characters
		/// up to the position on the line. In both cases, double byte characters count as a single character.
		/// This is probably only useful with mono-spaced fonts.</summary>
		public int GetColumn(int pos)
		{
			return Cmd(Sci.SCI_GETCOLUMN, pos);
		}

		/// <summary>
		/// Returns the character position of a column on a line taking the width of tabs into account.
		/// It treats a multi-byte character as a single column. Column numbers, like lines start at 0.</summary>
		public int GetPos(int line, int column)
		{
			return FindColumn(line, column);
		}
		public int FindColumn(int line, int column)
		{
			return Cmd(Sci.SCI_FINDCOLUMN, line, column);
		}

		/// <summary></summary>
		public int PositionFromPoint(int x, int y)
		{
			return Cmd(Sci.SCI_POSITIONFROMPOINT, x, y);
		}

		/// <summary></summary>
		public int PositionFromPointClose(int x, int y)
		{
			return Cmd(Sci.SCI_POSITIONFROMPOINTCLOSE, x, y);
		}

		/// <summary></summary>
		public int PointXFromPosition(int pos)
		{
			return Cmd(Sci.SCI_POINTXFROMPOSITION, 0, pos);
		}

		/// <summary></summary>
		public int PointYFromPosition(int pos)
		{
			return Cmd(Sci.SCI_POINTYFROMPOSITION, 0, pos);
		}

		/// <summary></summary>
		public void HideSelection(bool normal)
		{
			Cmd(Sci.SCI_HIDESELECTION, normal ? 1 : 0);
		}

		/// <summary></summary>
		public bool SelectionIsRectangle()
		{
			return Cmd(Sci.SCI_SELECTIONISRECTANGLE) != 0;
		}

		/// <summary></summary>
		public void MoveCaretInsideView()
		{
			Cmd(Sci.SCI_MOVECARETINSIDEVIEW);
		}

		/// <summary></summary>
		public int WordStartPosition(int pos, bool onlyWordCharacters)
		{
			return Cmd(Sci.SCI_WORDSTARTPOSITION, pos, onlyWordCharacters ? 1 : 0);
		}

		/// <summary></summary>
		public int WordEndPosition(int pos, bool onlyWordCharacters)
		{
			return Cmd(Sci.SCI_WORDENDPOSITION, pos, onlyWordCharacters ? 1 : 0);
		}

		/// <summary></summary>
		public int PositionBefore(int pos)
		{
			return Cmd(Sci.SCI_POSITIONBEFORE, pos);
		}

		/// <summary></summary>
		public int PositionAfter(int pos)
		{
			return Cmd(Sci.SCI_POSITIONAFTER, pos);
		}

		/// <summary>
		/// Returns the width (in pixels) of a string drawn in the given style.
		/// Can be used, for example, to decide how wide to make the line number margin in order to display a given number of numerals.</summary>
		public int TextWidth(int style, string text)
		{
			var text_bytes = Encoding.UTF8.GetBytes(text);
			using var t = GCHandle_.Alloc(text_bytes, GCHandleType.Pinned);
			return Cmd(Sci.SCI_TEXTWIDTH, style, t.Handle.AddrOfPinnedObject());
		}

		/// <summary>Returns the height (in pixels) of a particular line. Currently all lines are the same height.</summary>
		public int TextHeight(int line)
		{
			return Cmd(Sci.SCI_TEXTHEIGHT, line);
		}

		/// <summary>
		/// Scintilla remembers the x value of the last position horizontally moved to explicitly by the user
		/// and this value is then used when moving vertically such as by using the up and down keys.
		/// This message sets the current x position of the caret as the remembered value.</summary>
		public void ChooseCaretX()
		{
			Cmd(Sci.SCI_CHOOSECARETX);
		}

		/// <summary>
		/// Enable/disable multiple selection. When multiple selection is disabled, it is not
		/// possible to select multiple ranges by holding down the Ctrl key while dragging with the mouse.</summary>
		public bool MultipleSelection
		{
			get => Cmd(Sci.SCI_GETMULTIPLESELECTION) != 0;
			set => Cmd(Sci.SCI_SETMULTIPLESELECTION, value ? 1 : 0);
		}

		/// <summary>Whether typing, backspace, or delete works with multiple selections simultaneously.</summary>
		public bool AdditionalSelectionTyping
		{
			get => Cmd(Sci.SCI_GETADDITIONALSELECTIONTYPING) != 0;
			set => Cmd(Sci.SCI_SETADDITIONALSELECTIONTYPING, value ? 1 : 0);
		}

		/// <summary>
		/// When pasting into multiple selections, the pasted text can go into just the main selection
		/// with SC_MULTIPASTE_ONCE=0 or into each selection with SC_MULTIPASTE_EACH=1. SC_MULTIPASTE_ONCE is the default.</summary>
		public int MutliPaste
		{
			get => Cmd(Sci.SCI_GETMULTIPASTE);
			set => Cmd(Sci.SCI_SETMULTIPASTE, value);
		}

		/// <summary>
		/// Virtual space can be enabled or disabled for rectangular selections or in other circumstances or in both.
		/// There are two bit flags SCVS_RECTANGULARSELECTION=1 and SCVS_USERACCESSIBLE=2 which can be set independently.
		/// SCVS_NONE=0, the default, disables all use of virtual space.</summary>
		public int VirtualSpace
		{
			get => Cmd(Sci.SCI_GETVIRTUALSPACEOPTIONS);
			set => Cmd(Sci.SCI_SETVIRTUALSPACEOPTIONS, value);
		}

		/// <summary>Insert/Overwrite</summary>
		public bool Overtype
		{
			get => Cmd(Sci.SCI_GETOVERTYPE) != 0;
			set => Cmd(Sci.SCI_SETOVERTYPE, value ? 1 : 0);
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

		/// <summary>Clear the *selected* text</summary>
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

		/// <summary>Enable/Disable undo history collection</summary>
		public bool UndoCollection
		{
			get => Cmd(Sci.SCI_GETUNDOCOLLECTION) != 0;
			set => Cmd(Sci.SCI_SETUNDOCOLLECTION, value ? 1 : 0);
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
		//public event EventHandler<ScrollEventArgs>? Scroll;
		//private void OnScroll(ScrollEventArgs args)
		//{
		//	Scroll?.Invoke(this, args);
		//}

		/// <summary>Returns an RAII object that preserves (where possible) the currently visible line</summary>
		public Scope<int> ScrollScope()
		{
			return Scope.Create(
				() => FirstVisibleLine,
				fv => FirstVisibleLine = fv);
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

		/// <summary></summary>
		public void ScrollToLine(int line)
		{
			LineScroll(0, line - LineFromPosition(CurrentPos));
		}

		/// <summary>Scroll the last line into view</summary>
		public void ScrollToBottom()
		{
			// Move the caret to the end
			Cmd(Sci.SCI_SETEMPTYSELECTION, Cmd(Sci.SCI_GETTEXTLENGTH));

			// Only scroll if the last line isn't visible
			var last_line_index = Cmd(Sci.SCI_LINEFROMPOSITION, Cmd(Sci.SCI_GETTEXTLENGTH));
			var last_vis_line = Cmd(Sci.SCI_GETFIRSTVISIBLELINE) + Cmd(Sci.SCI_LINESONSCREEN);
			if (last_line_index >= last_vis_line)
				Cmd(Sci.SCI_SCROLLCARET);
		}

		/// <summary></summary>
		public void LineScroll(int columns, int lines)
		{
			Cmd(Sci.SCI_LINESCROLL, columns, lines);
		}

		/// <summary></summary>
		public void ScrollCaret()
		{
			Cmd(Sci.SCI_SCROLLCARET);
		}

		/// <summary>Get/Set horizontal scroll bar visibility</summary>
		public bool HScrollBar
		{
			get => Cmd(Sci.SCI_GETHSCROLLBAR) != 0;
			set => Cmd(Sci.SCI_SETHSCROLLBAR, value ? 1 : 0);
		}

		/// <summary>Get/Set vertical scroll bar visibility</summary>
		public bool VScrollBar
		{
			get => Cmd(Sci.SCI_GETVSCROLLBAR) != 0;
			set => Cmd(Sci.SCI_SETVSCROLLBAR, value ? 1 : 0);
		}

		/// <summary></summary>
		public int XOffset
		{
			get => Cmd(Sci.SCI_GETXOFFSET);
			set => Cmd(Sci.SCI_SETXOFFSET, value);
		}

		/// <summary>Scroll width (in pixels)</summary>
		public int ScrollWidth
		{
			get => Cmd(Sci.SCI_GETSCROLLWIDTH);
			set => Cmd(Sci.SCI_SETSCROLLWIDTH, value);
		}

		/// <summary>Allow/Disallow scrolling up to one page past the last line</summary>
		public bool EndAtLastLine
		{
			get => Cmd(Sci.SCI_GETENDATLASTLINE) != 0;
			set => Cmd(Sci.SCI_SETENDATLASTLINE, value ? 1 : 0);
		}

		#endregion

		#region DragDrop

		public new bool AllowDrop
		{
			get => (Win32.GetWindowLong(m_native_control.Hwnd, Win32.GWL_EXSTYLE) & Win32.WS_EX_ACCEPTFILES) != 0;
			set => Win32.DragAcceptFiles(m_native_control.Hwnd, value);
		}

		#endregion

		#region End of Line

		/// <summary>Get/Set the characters that are added into the document when the user presses the Enter key</summary>
		public Sci.EEndOfLineMode EOLMode
		{
			get => (Sci.EEndOfLineMode)Cmd(Sci.SCI_GETEOLMODE);
			set => Cmd(Sci.SCI_SETEOLMODE, (int)value);
		}

		/// <summary>Changes all the end of line characters in the document to match 'mode'</summary>
		public void ConvertEOLs(Sci.EEndOfLineMode mode)
		{
			Cmd(Sci.SCI_CONVERTEOLS, (int)mode);
		}

		/// <summary>Get/Set whether EOL characters are visible</summary>
		public bool ViewEOL
		{
			get => Cmd(Sci.SCI_GETVIEWEOL) != 0;
			set => Cmd(Sci.SCI_SETVIEWEOL, value ? 1 : 0);
		}

		#endregion

		#region Style

		/// <summary>Clear all styles</summary>
		public void StyleClearAll()
		{
			Cmd(Sci.SCI_STYLECLEARALL);
		}

		/// <summary>Set the font for style index 'idx'</summary>
		public void StyleSetFont(int idx, string font_name)
		{
			var bytes = Encoding.UTF8.GetBytes(font_name);
			using var handle = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			Cmd(Sci.SCI_STYLESETFONT, idx, handle.Handle.AddrOfPinnedObject());
		}

		/// <summary>Set the font size for style index 'idx'</summary>
		public void StyleSetSize(int idx, int sizePoints)
		{
			Cmd(Sci.SCI_STYLESETSIZE, idx, sizePoints);
		}

		/// <summary>Set bold or regular for style index 'idx'</summary>
		public void StyleSetBold(int idx, bool bold)
		{
			Cmd(Sci.SCI_STYLESETBOLD, idx, bold ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetItalic(int idx, bool italic)
		{
			Cmd(Sci.SCI_STYLESETITALIC, idx, italic ? 1 : 0);
		}

		/// <summary>Set underlined for style index 'idx'</summary>
		public void StyleSetUnderline(int idx, bool underline)
		{
			Cmd(Sci.SCI_STYLESETUNDERLINE, idx, underline ? 1 : 0);
		}

		/// <summary>Set the text colour for style index 'idx'</summary>
		public void StyleSetFore(int idx, Colour32 fore)
		{
			Cmd(Sci.SCI_STYLESETFORE, idx, (int)(fore.ARGB & 0x00FFFFFF));
		}

		/// <summary>Set the background colour for style index 'idx'</summary>
		public void StyleSetBack(int idx, Colour32 back)
		{
			Cmd(Sci.SCI_STYLESETBACK, idx, (int)(back.ARGB & 0x00FFFFFF));
		}

		/// <summary></summary>
		public void StyleSetEOLFilled(int idx, bool filled)
		{
			Cmd(Sci.SCI_STYLESETEOLFILLED, idx, filled ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetCharacterSet(int idx, int characterSet)
		{
			Cmd(Sci.SCI_STYLESETCHARACTERSET, idx, characterSet);
		}

		/// <summary></summary>
		public void StyleSetCase(int idx, int caseForce)
		{
			Cmd(Sci.SCI_STYLESETCASE, idx, caseForce);
		}

		/// <summary></summary>
		public void StyleSetVisible(int idx, bool visible)
		{
			Cmd(Sci.SCI_STYLESETVISIBLE, idx, visible ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetChangeable(int idx, bool changeable)
		{
			Cmd(Sci.SCI_STYLESETCHANGEABLE, idx, changeable ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetHotSpot(int idx, bool hotspot)
		{
			Cmd(Sci.SCI_STYLESETHOTSPOT, idx, hotspot ? 1 : 0);
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
			get => (uint)Cmd(Sci.SCI_GETCARETFORE);
			set => Cmd(Sci.SCI_SETCARETFORE, (int)value.ARGB);
		}

		/// <summary></summary>
		public bool CaretLineVisible
		{
			get => Cmd(Sci.SCI_GETCARETLINEVISIBLE) != 0;
			set => Cmd(Sci.SCI_SETCARETLINEVISIBLE, value ? 1 : 0);
		}

		/// <summary></summary>
		public Colour32 CaretLineBack
		{
			get => (uint)Cmd(Sci.SCI_GETCARETLINEBACK);
			set => Cmd(Sci.SCI_SETCARETLINEBACK, (int)value.ARGB);
		}

		/// <summary>Caret blink period (in ms)</summary>
		public int CaretPeriod
		{
			get => Cmd(Sci.SCI_GETCARETPERIOD);
			set => Cmd(Sci.SCI_SETCARETPERIOD, value);
		}

		/// <summary></summary>
		public int CaretWidth
		{
			get => Cmd(Sci.SCI_GETCARETWIDTH);
			set => Cmd(Sci.SCI_SETCARETWIDTH, value);
		}

		/// <summary></summary>
		public bool CaretSticky
		{
			get => Cmd(Sci.SCI_GETCARETSTICKY) != 0;
			set => Cmd(Sci.SCI_SETCARETSTICKY, value ? 1 : 0);
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
			get => Cmd(Sci.SCI_GETMARGINRIGHT);
			set => Cmd(Sci.SCI_SETMARGINRIGHT, 0, value);
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

		/// <summary>
		/// Gets/Sets the size of a tab as a multiple of the size of a space character in STYLE_DEFAULT.
		/// The default tab width is 8 characters. There are no limits on tab sizes, but values less than
		/// 1 or large values may have undesirable effects.</summary>
		public int TabWidth
		{
			get => Cmd(Sci.SCI_GETTABWIDTH);
			set => Cmd(Sci.SCI_SETTABWIDTH, value);
		}

		/// <summary></summary>
		public bool UseTabs
		{
			get => Cmd(Sci.SCI_GETUSETABS) != 0;
			set => Cmd(Sci.SCI_SETUSETABS, value ? 1 : 0);
		}

		/// <summary>
		/// Gets/Sets the size of indentation in terms of the width of a space in STYLE_DEFAULT.
		/// If you set a width of 0, the indent size is the same as the tab size. There are no limits
		/// on indent sizes, but values less than 0 or large values may have undesirable effects.</summary>
		public int Indent
		{
			get => Cmd(Sci.SCI_GETINDENT);
			set => Cmd(Sci.SCI_SETINDENT, value);
		}

		/// <summary>Inside indentation white space, gets/sets whether the tab key indents rather than inserts a tab character.</summary>
		public bool TabIndents
		{
			get => Cmd(Sci.SCI_GETTABINDENTS) != 0;
			set => Cmd(Sci.SCI_SETTABINDENTS, value ? 1 : 0);
		}

		/// <summary>Inside indentation white space, gets/sets whether the delete key un-indents rather than deletes a tab character.</summary>
		public bool BackSpaceUnIndents
		{
			get => Cmd(Sci.SCI_GETBACKSPACEUNINDENTS) != 0;
			set => Cmd(Sci.SCI_SETBACKSPACEUNINDENTS, value ? 1 : 0);
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
			get => Cmd(Sci.SCI_GETINDENTATIONGUIDES) != 0;
			set => Cmd(Sci.SCI_SETINDENTATIONGUIDES, value ? 1 : 0);
		}

		/// <summary></summary>
		public int HighlightGuide
		{
			get => Cmd(Sci.SCI_GETHIGHLIGHTGUIDE);
			set => Cmd(Sci.SCI_SETHIGHLIGHTGUIDE, value);
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
			return Cmd(Sci.SCI_MARKERPREVIOUS, lineStart, markerMask);
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

		#region Context Menu

		/// <summary>Set whether the built-in context menu is used</summary>
		public bool UsePopUp
		{
			set { Cmd(Sci.SCI_USEPOPUP, value ? 1 : 0); }
		}

		#endregion

		#region Macro Recording

		/// <summary></summary>
		public void StartRecord()
		{
			Cmd(Sci.SCI_STARTRECORD);
		}

		/// <summary></summary>
		public void StopRecord()
		{
			Cmd(Sci.SCI_STOPRECORD);
		}

		#endregion

		#region Printing

		/// <summary></summary>
		public int FormatRange(bool draw, out Sci.RangeToFormat fr)
		{
			throw new NotImplementedException();
			//return Cmd(Sci.SCI_FORMATRANGE, draw, &fr);
		}

		/// <summary></summary>
		public int GetPrintMagnification()
		{
			return Cmd(Sci.SCI_GETPRINTMAGNIFICATION);
		}

		/// <summary></summary>
		public void SetPrintMagnification(int magnification)
		{
			Cmd(Sci.SCI_SETPRINTMAGNIFICATION, magnification);
		}

		/// <summary></summary>
		public int GetPrintColourMode()
		{
			return Cmd(Sci.SCI_GETPRINTCOLOURMODE);
		}

		/// <summary></summary>
		public void SetPrintColourMode(int mode)
		{
			Cmd(Sci.SCI_SETPRINTCOLOURMODE, mode);
		}

		/// <summary></summary>
		public int PrintWrapMode
		{
			get => Cmd(Sci.SCI_GETPRINTWRAPMODE);
			set => Cmd(Sci.SCI_SETPRINTWRAPMODE, value);
		}

		#endregion

		#region Multiple Views

		/// <summary></summary>
		public IntPtr GetDocPointer()
		{
			return (IntPtr)Cmd(Sci.SCI_GETDOCPOINTER, 0, 0);
		}

		/// <summary></summary>
		public void SetDocPointer(IntPtr pointer)
		{
			Cmd(Sci.SCI_SETDOCPOINTER, 0, pointer);
		}

		/// <summary></summary>
		public IntPtr CreateDocument()
		{
			return (IntPtr)Cmd(Sci.SCI_CREATEDOCUMENT, 0, 0);
		}

		/// <summary></summary>
		public void AddRefDocument(IntPtr doc)
		{
			Cmd(Sci.SCI_ADDREFDOCUMENT, 0, doc);
		}

		/// <summary></summary>
		public void ReleaseDocument(IntPtr doc)
		{
			Cmd(Sci.SCI_RELEASEDOCUMENT, 0, doc);
		}

		#endregion

		#region Folding

		/// <summary></summary>
		public int VisibleFromDocLine(int line)
		{
			return Cmd(Sci.SCI_VISIBLEFROMDOCLINE, line);
		}

		/// <summary></summary>
		public int DocLineFromVisible(int lineDisplay)
		{
			return Cmd(Sci.SCI_DOCLINEFROMVISIBLE, lineDisplay);
		}

		/// <summary></summary>
		public void ShowLines(int lineStart, int lineEnd)
		{
			Cmd(Sci.SCI_SHOWLINES, lineStart, lineEnd);
		}

		/// <summary></summary>
		public void HideLines(int lineStart, int lineEnd)
		{
			Cmd(Sci.SCI_HIDELINES, lineStart, lineEnd);
		}

		/// <summary></summary>
		public bool GetLineVisible(int line)
		{
			return Cmd(Sci.SCI_GETLINEVISIBLE, line) != 0;
		}

		/// <summary></summary>
		public int FoldLevel(int line)
		{
			return Cmd(Sci.SCI_GETFOLDLEVEL, line);
		}

		/// <summary></summary>
		public void FoldLevel(int line, int level)
		{
			Cmd(Sci.SCI_SETFOLDLEVEL, line, level);
		}

		/// <summary></summary>
		public void FoldFlags(int flags)
		{
			Cmd(Sci.SCI_SETFOLDFLAGS, flags);
		}

		/// <summary></summary>
		public int GetLastChild(int line, int level)
		{
			return Cmd(Sci.SCI_GETLASTCHILD, line, level);
		}

		/// <summary></summary>
		public int GetFoldParent(int line)
		{
			return Cmd(Sci.SCI_GETFOLDPARENT, line);
		}

		/// <summary></summary>
		public bool FoldExpanded(int line)
		{
			return Cmd(Sci.SCI_GETFOLDEXPANDED, line) != 0;
		}

		/// <summary></summary>
		public void FoldExpanded(int line, bool expanded)
		{
			Cmd(Sci.SCI_SETFOLDEXPANDED, line, expanded ? 1 : 0);
		}

		/// <summary></summary>
		public void ToggleFold(int line)
		{
			Cmd(Sci.SCI_TOGGLEFOLD, line);
		}

		/// <summary></summary>
		public void EnsureVisible(int line)
		{
			Cmd(Sci.SCI_ENSUREVISIBLE, line);
		}

		/// <summary></summary>
		public void EnsureVisibleEnforcePolicy(int line)
		{
			Cmd(Sci.SCI_ENSUREVISIBLEENFORCEPOLICY, line);
		}

		/// <summary></summary>
		public void SetFoldMarginColour(bool useSetting, Colour32 back)
		{
			Cmd(Sci.SCI_SETFOLDMARGINCOLOUR, useSetting ? 1 : 0, (int)(back.ARGB & 0x00FFFFFF));
		}

		/// <summary></summary>
		public void SetFoldMarginHiColour(bool useSetting, Colour32 fore)
		{
			Cmd(Sci.SCI_SETFOLDMARGINHICOLOUR, useSetting ? 1 : 0, (int)(fore.ARGB & 0x00FFFFFF));
		}

		#endregion

		#region Line Wrapping

		/// <summary></summary>
		public int WrapMode
		{
			get => Cmd(Sci.SCI_GETWRAPMODE);
			set => Cmd(Sci.SCI_SETWRAPMODE, value);
		}

		/// <summary></summary>
		public int WrapVisualFlags
		{
			get => Cmd(Sci.SCI_GETWRAPVISUALFLAGS);
			set => Cmd(Sci.SCI_SETWRAPVISUALFLAGS, value);
		}

		/// <summary></summary>
		public int WrapVisualFlagsLocation
		{
			get => Cmd(Sci.SCI_GETWRAPVISUALFLAGSLOCATION);
			set => Cmd(Sci.SCI_SETWRAPVISUALFLAGSLOCATION, value);
		}

		/// <summary></summary>
		public int WrapStartIndent
		{
			get => Cmd(Sci.SCI_GETWRAPSTARTINDENT);
			set => Cmd(Sci.SCI_SETWRAPSTARTINDENT, value);
		}

		/// <summary></summary>
		public int LayoutCache
		{
			get => Cmd(Sci.SCI_GETLAYOUTCACHE);
			set => Cmd(Sci.SCI_SETLAYOUTCACHE, value);
		}

		/// <summary></summary>
		public void LinesSplit(int pixelWidth)
		{
			Cmd(Sci.SCI_LINESSPLIT, pixelWidth);
		}

		/// <summary></summary>
		public void LinesJoin()
		{
			Cmd(Sci.SCI_LINESJOIN);
		}

		/// <summary></summary>
		public int WrapCount(int line)
		{
			return Cmd(Sci.SCI_WRAPCOUNT, line);
		}

		#endregion

		#region Zooming

		/// <summary></summary>
		public void ZoomIn()
		{
			Cmd(Sci.SCI_ZOOMIN);
		}

		/// <summary></summary>
		public void ZoomOut()
		{
			Cmd(Sci.SCI_ZOOMOUT);
		}

		/// <summary></summary>
		public int Zoom
		{
			get => Cmd(Sci.SCI_GETZOOM);
			set => Cmd(Sci.SCI_SETZOOM, value);
		}

		#endregion

		#region Long Lines

		/// <summary></summary>
		public int EdgeMode
		{
			get => Cmd(Sci.SCI_GETEDGEMODE);
			set => Cmd(Sci.SCI_SETEDGEMODE, value);
		}

		/// <summary></summary>
		public int EdgeColumn
		{
			get => Cmd(Sci.SCI_GETEDGECOLUMN);
			set => Cmd(Sci.SCI_SETEDGECOLUMN, value);
		}

		/// <summary></summary>
		public Colour32 EdgeColour
		{
			get => new Colour32((uint)(0xFF000000 | Cmd(Sci.SCI_GETEDGECOLOUR)));
			set => Cmd(Sci.SCI_SETEDGECOLOUR, (int)(value.ARGB & 0x00FFFFFF));
		}

		#endregion

		#region Lexer

		/// <summary></summary>
		public int Lexer
		{
			get => Cmd(Sci.SCI_GETLEXER);
			set => Cmd(Sci.SCI_SETLEXER, value);
		}

		/// <summary></summary>
		public void LexerLanguage(string language)
		{
			var bytes = Encoding.UTF8.GetBytes(language);
			using var handle = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			Cmd(Sci.SCI_SETLEXERLANGUAGE, 0, handle.Handle.AddrOfPinnedObject());
		}

		/// <summary></summary>
		public void LoadLexerLibrary(string path)
		{
			var bytes = Encoding.UTF8.GetBytes(path);
			using var handle = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
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
			var key_bytes = Encoding.UTF8.GetBytes(key);
			using var k = GCHandle_.Alloc(key_bytes, GCHandleType.Pinned);
			var len = Cmd(Sci.SCI_GETPROPERTY, k.Handle.AddrOfPinnedObject(), IntPtr.Zero);
			var buf = new byte[len + 1];
			using var b = GCHandle_.Alloc(buf, GCHandleType.Pinned);
			len = Cmd(Sci.SCI_GETPROPERTY, k.Handle.AddrOfPinnedObject(), b.Handle.AddrOfPinnedObject());
			return Encoding.UTF8.GetString(buf, 0, len);
		}
		public void Property(string key, string value)
		{
			var key_bytes = Encoding.UTF8.GetBytes(key);
			var val_bytes = Encoding.UTF8.GetBytes(value);
			using var k = GCHandle_.Alloc(key_bytes, GCHandleType.Pinned);
			using var v = GCHandle_.Alloc(val_bytes, GCHandleType.Pinned);
			Cmd(Sci.SCI_SETPROPERTY, k.Handle.AddrOfPinnedObject(), v.Handle.AddrOfPinnedObject());
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

		#region Notifications

		/// <summary></summary>
		public int ModEventMask
		{
			get => Cmd(Sci.SCI_GETMODEVENTMASK);
			set => Cmd(Sci.SCI_SETMODEVENTMASK, value);
		}

		/// <summary>(in ms)</summary>
		public int MouseDwellTime
		{
			get => Cmd(Sci.SCI_GETMOUSEDWELLTIME);
			set => Cmd(Sci.SCI_SETMOUSEDWELLTIME, value);
		}

		#endregion

		#region Misc

		/// <summary></summary>
		private void Allocate(int bytes)
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
			get => Cmd(Sci.SCI_GETBUFFEREDDRAW) != 0;
			set => Cmd(Sci.SCI_SETBUFFEREDDRAW, value ? 1 : 0);
		}

		/// <summary></summary>
		public bool TwoPhaseDraw
		{
			get => Cmd(Sci.SCI_GETTWOPHASEDRAW) != 0;
			set => Cmd(Sci.SCI_SETTWOPHASEDRAW, value ? 1 : 0);
		}

		/// <summary></summary>
		public int CodePage
		{
			get => Cmd(Sci.SCI_GETCODEPAGE);
			set => Cmd(Sci.SCI_SETCODEPAGE, value);
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
			get => Cmd(Sci.SCI_GETFOCUS) != 0;
			set => Cmd(Sci.SCI_SETFOCUS, value ? 1 : 0);
		}

		/// <summary></summary>
		public bool ReadOnly
		{
			get => Cmd(Sci.SCI_GETREADONLY) != 0;
			set => Cmd(Sci.SCI_SETREADONLY, value ? 1 : 0);
		}

		#endregion

		#region Status/Errors

		/// <summary></summary>
		public int Status
		{
			get => Cmd(Sci.SCI_GETSTATUS);
			set => Cmd(Sci.SCI_SETSTATUS, value);
		}

		#endregion

		/// <summary>Host the native scintilla control in WPF</summary>
		private class NativeControl :HwndHost
		{
			private const int ScintillaId = 0x1;

			public NativeControl()
			{ }

			/// <summary>The window handle of the Scintilla control</summary>
			public IntPtr Hwnd { get; private set; }

			/// <summary>Call the direct function</summary>
			public IntPtr Call(int code, IntPtr wparam, IntPtr lparam)
			{
				if (m_func == null) return IntPtr.Zero;
				return m_func(m_ptr, code, wparam, lparam);
			}
			private Sci.DirectFunction? m_func;
			private IntPtr m_ptr;

			/// <summary>Create the native window handle</summary>
			protected override HandleRef BuildWindowCore(HandleRef hwnd_parent)
			{
				// Notes:
				//  - The examples suggest creating the scintilla control as a child of a static control
				//    in order to fix some issue with notification messages sent from the native control.
				//    I haven't seen any issue so I'm just creating the native control directly. Doing so
				//    means resizing and general interaction with FrameworkElements works properly.
				if (hwnd_parent.Handle == IntPtr.Zero)
					throw new Exception("Expected this control to be a child");

				// Create the native scintilla window to fit within 'hwnd_parent'
				Win32.GetClientRect(hwnd_parent.Handle, out var parent_rect);
				Hwnd = Win32.CreateWindowEx(0, "Scintilla", string.Empty, Win32.WS_CHILD | Win32.WS_VISIBLE, 0, 0, parent_rect.width, parent_rect.height, hwnd_parent.Handle, (IntPtr)ScintillaId, IntPtr.Zero, IntPtr.Zero);
				if (Hwnd == IntPtr.Zero)
					throw new Exception("Failed to create scintilla native control");

				// Get the function pointer for direct calling the WndProc (rather than windows messages)
				var func = Win32.SendMessage(Hwnd, Sci.SCI_GETDIRECTFUNCTION, IntPtr.Zero, IntPtr.Zero);
				m_ptr = Win32.SendMessage(Hwnd, Sci.SCI_GETDIRECTPOINTER, IntPtr.Zero, IntPtr.Zero);
				m_func = Marshal_.PtrToDelegate<Sci.DirectFunction>(func);

				// Return the host window handle
				return new HandleRef(this, Hwnd);
			}
			protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lParam, ref bool handled)
			{
				handled = false;
				return IntPtr.Zero;
			}
			protected override void DestroyWindowCore(HandleRef hwnd)
			{
				m_func = null;
				m_ptr = IntPtr.Zero;
				Win32.DestroyWindow(hwnd.Handle);
			}
		}
	}
}
