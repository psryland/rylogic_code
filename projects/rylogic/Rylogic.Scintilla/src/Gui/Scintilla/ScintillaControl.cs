using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Interop.Win32;
using Rylogic.Scintilla;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public class ScintillaControl :HwndHost, INotifyPropertyChanged
	{
		// Notes:
		//  - See http://www.scintilla.org/ScintillaDoc.html for documentation
		//  - This is roughly a port of pr::gui::ScintillaCtrl
		//  - Function comments are mostly incomplete because it'd take too long.. Fill them in as needed
		//  - Copy/port #pragma regions from the native control on demand
		//  - Dispose is handled internally via the Closed event on the owning window.

		private static int ScintillaCtrlId = 1;
		private IntPtr CtrlId = new(++ScintillaCtrlId);

		static ScintillaControl()
		{
			Sci.LoadDll(throw_if_missing: false);
		}
		public ScintillaControl()
		{
			// Notes:
			//  - The examples suggest creating the scintilla control as a child of a static control
			//    in order to fix some issue with notification messages sent from the native control.
			//    I haven't seen any issue so I'm just creating the native control directly. Doing so
			//    means resizing and general interaction with FrameworkElements works properly.

			// The Scintilla dll must have been loaded already
			if (!Sci.ModuleLoaded)
				throw new Exception("Scintilla DLL must be loaded before attempting to create the scintilla control");

			// Create the native scintilla window to fit within 'hwnd_parent'
			var rect = Win32.RECT.FromLTRB(0, 0, 1, 1);
			Hwnd = Win32.CreateWindow(0, "Scintilla", string.Empty, Win32.WS_CHILD | Win32.WS_VISIBLE, 0, 0, rect.width, rect.height, Win32.ProxyParentHwnd, CtrlId, IntPtr.Zero, IntPtr.Zero);
			if (Hwnd == IntPtr.Zero)
				throw new Win32Exception(Win32.GetLastError(), $"Failed to create the scintilla native control. {Win32.GetLastErrorString()}");

			// Get the function pointer for direct calling the WndProc (rather than windows messages)
			var func = Win32.SendMessage(Hwnd, Sci.SCI_GETDIRECTFUNCTION, IntPtr.Zero, IntPtr.Zero);
			m_ptr = Win32.SendMessage(Hwnd, Sci.SCI_GETDIRECTPOINTER, IntPtr.Zero, IntPtr.Zero);
			m_func = Marshal_.PtrToDelegate<Sci.SciFnDirect>(func) ?? throw new Exception("Failed to retrieve the direct call function");

			// Reset the style
			InitStyle(this);
		}
		protected override void OnGotFocus(RoutedEventArgs e)
		{
			// Although there is only one WPF window handle,
			// this method is only called when this control gets focus.
			base.OnGotFocus(e);

			// When this 'ScintillaControl' receives focus, forward it on to the native control
			FocusHosted();
		}
		protected virtual IntPtr WndProcHost(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
		{
			// Host WndProc - This is a wnd proc for the WPF window that owns this 'HwndHost'.
			// Remember this function gets called for each ScintillaControl instance that exists
			// within the owning WPF window.
			//
			// Notes:
			//  - 'hwnd' is the window handle of the parent window of the native scintilla control.
			//    Scintilla sends notifications to its parent, which is what we're handling here.
			//  - WPF only has one HWND per window, so the Hook set up in 'BuildWindowCore' means
			//    notification messages from all scintilla control instances come through here.

			//var str = Win32.DebugMessage(hwnd, msg, wparam, lparam);
			//if (str.Length != 0) System.Diagnostics.Debug.WriteLine($"Host: {str}");
			switch (msg)
			{
				case Win32.WM_KILLFOCUS:
				{
					// This is a work around for a keyboard focus issue when hosting win32 api controls within a WPF app.
					// The problem is WPF windows only have one HWND for the whole window and WPF tracks the keyboard focus
					// within the app. When a win32 control is used, it has its own HWND, so when it gets focus the WPF app
					// thinks it has lost focus. As a work-around, suppress the WM_KILLFOCUS message if the hwnd receiving
					// focus is actually the hosted win32 control.
					if (wparam == Hwnd && Hwnd != IntPtr.Zero)
					{
						// If we're losing focus to the native control but we don't have keyboard focus,
						// call 'FocusHosted' to give this control keyboard focus.
						if (!IsKeyboardFocusWithin)
							FocusHosted();

						// Suppress this kill focus message so we keep IsKeyboardFocusWithin
						handled = true;
					}
					break;
				}
				case Win32.WM_ACTIVATEAPP:
				{
					if (wparam != IntPtr.Zero) // Activate
					{
						if (IsKeyboardFocusWithin)
							FocusHosted();
					}
					break;
				}
				case Win32.WM_COMMAND:
				{
					var id = Win32.LoWord(wparam.ToInt32());
					if (id != CtrlId.ToInt32())
						break; // Not for this instance.

					// Watch for edit notifications
					var notif = Win32.HiWord(wparam.ToInt32());
					if (notif == Sci.SCEN_CHANGE)
					{
						// This indicates the text buffer has changed via API functions.
						// 'SCN_CHARADDED' indicates a key press resulted in a character added
						TextChanged?.Invoke(this, EventArgs.Empty);
						NotifyPropertyChanged(nameof(Text));
						NotifyPropertyChanged(nameof(TextLength));
						handled = true;
					}
					break;
				}
				case Win32.WM_NOTIFY:
				{
					var notif = Marshal.PtrToStructure<Sci.SCNotification>(lparam);
					if (notif.nmhdr.idFrom != CtrlId)
						break; // Not for this instance.

					switch (notif.nmhdr.code)
					{
						case Sci.SCN_CHARADDED:
						{
							if (AutoIndent)
							{
								var lem = EOLMode;
								var lend =
									(lem == Sci.EEndOfLine.Cr && notif.ch == '\r') ||
									(lem == Sci.EEndOfLine.Lf && notif.ch == '\n') ||
									(lem == Sci.EEndOfLine.Crlf && notif.ch == '\n');
								if (lend)
								{
									var line = LineIndexFromPosition(CurrentPos);
									var indent = line > 0 ? LineIndentation(line - 1) : 0;
									LineIndentation(line, indent);
									GotoPos(FindColumn(line, indent));
								}
							}
							OnCharAdded((char)notif.ch);
							break;
						}
						case Sci.SCN_UPDATEUI:
						{
							// There is one of these messages every time the caret moves
							switch (notif.updated)
							{
								case Sci.SC_UPDATE_SELECTION:
								{
									OnSelectionChanged();
									NotifyPropertyChanged(nameof(Selection));
									NotifyPropertyChanged(nameof(CurrentPos));
									NotifyPropertyChanged(nameof(CurrentLineIndex));
									NotifyPropertyChanged(nameof(CurrentColumn));
									NotifyPropertyChanged(nameof(Anchor));
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
						case Sci.SCN_AUTOCSELECTION:
						{
							OnAutoCompleteSelection(notif.Position, notif.Text, (Sci.ECompletionMethods)notif.listCompletionMethod);
							break;
						}
						case Sci.SCN_AUTOCCOMPLETED:
						{
							OnAutoCompleteDone(notif.Position, notif.Text, (Sci.ECompletionMethods)notif.listCompletionMethod);
							break;
						}
					}
					handled = true;
					break;
				}
			}
			return IntPtr.Zero;
		}
		protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
		{
			// Native Control WndProc - This is a wnd proc for the native scintilla control
			// This is called before Scintilla's native WndProc
			//var str = Win32.DebugMessage(hwnd, msg, wparam, lparam);
			//if (str.Length != 0) System.Diagnostics.Debug.WriteLine($"Native: {str}");
			switch (msg)
			{
				case Win32.WM_SETFOCUS:
				{
					// When the hosted control receives focus, give focus to the
					// host momentarily so that 'IsKeyboardFocusWithin' gets updated.
					if (!IsKeyboardFocusWithin)
					{
						FocusHosted();
						handled = true;
					}
					break;
				}
				case Win32.WM_CONTEXTMENU:
				{
					// Handle context menu open requests from the native control
					if (ContextMenu is System.Windows.Controls.ContextMenu cmenu)
					{
						cmenu.IsOpen = true;
						handled = true;
					}
					break;
				}
			}
			return base.WndProc(hwnd, msg, wparam, lparam, ref handled);
		}
		protected override HandleRef BuildWindowCore(HandleRef parent_hwnd)
		{
			// 'parent_hwnd' is the HWND of the WPF main window.
			if (parent_hwnd.Handle == IntPtr.Zero)
				throw new Exception("Expected this control to be a child");

			// Re-parent the native control to 'parent_hwnd'
			ParentHwnd = parent_hwnd.Handle;
			if (Win32.SetParent(Hwnd, ParentHwnd) == IntPtr.Zero)
				throw new Win32Exception(Win32.GetLastError(), "Failed to re-parent the native scintilla control");

			// Resize to fit the parent
			var parent_rect = Win32.GetClientRect(ParentHwnd);
			Win32.MoveWindow(Hwnd, parent_rect.left, parent_rect.top, parent_rect.width, parent_rect.height, true);

			// Add a hook on the WndProc of the parent so we can intercept messages sent from the control
			var parent_src = HwndSource.FromHwnd(ParentHwnd);
			parent_src.AddHook(WndProcHost);
			Window.GetWindow(this).Closed += delegate { Dispose(); };// When the owning window is closed, call dispose on the native control

			// Return the host window handle
			return new HandleRef(this, Hwnd);
		}
		protected override void DestroyWindowCore(HandleRef hwnd)
		{
			var parent_src = HwndSource.FromHwnd(ParentHwnd);
			parent_src.RemoveHook(WndProcHost);

			m_func = null;
			m_ptr = IntPtr.Zero;
			Win32.DestroyWindow(hwnd.Handle);
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Tab:
				case Key.Up:
				case Key.Down:
				case Key.Left:
				case Key.Right:
				{
					// Forward navigation keys to the native control.
					// This cannot be done in WndProc because navigation keys never make
					// it to the native control.
					var vk = (int)e.Key.ToKeyCode();
					Win32.SendMessage(Hwnd, Win32.WM_KEYDOWN, vk, 1);
					e.Handled = true;
					break;
				}
				case Key.Space:
				{
					// Ctrl+Space shows the auto complete list
					if (AutoComplete != null && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
					{
						OnAutoComplete();
						e.Handled = true;
					}

					// Ctrl+Shift+Space shows the 'call tips'
					if (CallTip != null && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
					{
						DoCallTips();
						e.Handled = true;
					}

					break;
				}
			}
			base.OnKeyDown(e);
		}
		private void FocusHosted()
		{
			Keyboard.Focus(this);
			Win32.SetFocus(Hwnd);
		}

		/// <summary>
		/// The window handle of the Scintilla control.
		/// This is the same as HwndHost.Handle but has a value as soon as the scintilla control is
		/// actually created rather than after the 'BuildWindowCore' function has completed.</summary>
		private IntPtr Hwnd { get; set; }
		private IntPtr ParentHwnd { get; set; }

		/// <summary>Call the direct function</summary>
		public IntPtr Call(int code, IntPtr wparam, IntPtr lparam)
		{
			if (m_func == null) throw new Exception("The scintilla control has not been created yet");
			return m_func(m_ptr, code, wparam, lparam);
		}
		public long Cmd(int code, IntPtr wparam, IntPtr lparam)
		{
			return (int)Call(code, wparam, lparam);
		}
		public long Cmd(int code, long wparam, IntPtr lparam)
		{
			return (int)Call(code, (IntPtr)wparam, lparam);
		}
		public long Cmd(int code, long wparam, string lparam)
		{
			using var text = Marshal_.AllocUTF8String(EHeap.HGlobal, lparam);
			return (int)Call(code, (IntPtr)wparam, text.Value);
		}
		public long Cmd(int code, long wparam, long lparam)
		{
			return (int)Call(code, (IntPtr)wparam, (IntPtr)lparam);
		}
		public long Cmd(int code, long wparam)
		{
			return (int)Call(code, (IntPtr)wparam, IntPtr.Zero);
		}
		public long Cmd(int code)
		{
			return (int)Call(code, IntPtr.Zero, IntPtr.Zero);
		}
		private Sci.SciFnDirect? m_func;
		private IntPtr m_ptr;

		/// <summary>A callback function used to reset the style of the control</summary>
		public Action<ScintillaControl> InitStyle
		{
			get => m_init_style ?? DefaultInitStyle;
			set
			{
				if (m_init_style == value) return;
				m_init_style = value;
				if (m_func != null && m_init_style != null)
					m_init_style(this);
			}
		}
		private Action<ScintillaControl>? m_init_style;

		/// <summary>Default initial style function</summary>
		public static void DefaultInitStyle(ScintillaControl sc)
		{
			if (sc.m_func == null)
				throw new Exception($"Scintilla native control has not been created yet");

			// Reset to the default style
			sc.CodePage = Sci.SC_CP_UTF8;
			sc.Cursor = Cursors.IBeam;
			sc.ClearAll();
			sc.ClearDocumentStyle();
			sc.TabWidth = 4;
			sc.Indent = 4;
		}

		/// <summary>Property changed notification</summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		protected void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));

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

		/// <summary>Gets the length of the text *IN BYTES*.</summary>
		public long TextLength => Cmd(Sci.SCI_GETTEXTLENGTH);

		/// <summary>Get/Set the current text</summary>
		public string Text
		{
			get
			{
				var len = TextLength;
				if (len == 0)
					return string.Empty;

				var bytes = new byte[len];
				using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
				var num = Cmd(Sci.SCI_GETTEXT, len + 1, h.Handle.AddrOfPinnedObject());
				return Encoding.UTF8.GetString(bytes, 0, (int)num);
			}
			set
			{
				if (value.Length == 0)
				{
					ClearAll();
				}
				else
				{
					// Convert the string to UTF-8
					var bytes = Encoding.UTF8.GetBytes(value);
					using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
					Cmd(Sci.SCI_SETTEXT, 0, h.Handle.AddrOfPinnedObject());
				}

				TextChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		/// <summary>Raised when text in the control is changed</summary>
		public event EventHandler? TextChanged;

		/// <summary>Raised when a keypress results in a character added (Doesn't fire for delete)</summary>
		private void OnCharAdded(char ch)
		{
			CharAdded?.Invoke(this, new CharAddedEventArgs(ch));
		}
		public class CharAddedEventArgs :EventArgs
		{
			public CharAddedEventArgs(char ch)
			{
				Char = ch;
			}

			/// <summary>The character that was added</summary>
			public char Char { get; }
		}
		public event EventHandler<CharAddedEventArgs>? CharAdded;

		/// <summary></summary>
		public char GetCharAt(long pos)
		{
			return (char)(Cmd(Sci.SCI_GETCHARAT, pos) & 0xFF);
		}

		/// <summary>Return a line of text</summary>
		public string GetLine(long line_index)
		{
			var len = LineLength(line_index);
			var bytes = new byte[len];
			using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			var num = Cmd(Sci.SCI_GETLINE, line_index, h.Handle.AddrOfPinnedObject());
			return Encoding.UTF8.GetString(bytes, 0, (int)num);
		}

		/// <summary></summary>
		public long LineCount => Cmd(Sci.SCI_GETLINECOUNT);

		/// <summary>Returns the text in the given range</summary>
		public string TextRange(long beg, long end) => TextRange(new RangeI(beg, end));
		public string TextRange(RangeI range)
		{
			if (range.Beg < 0 || range.End > TextLength)
				throw new Exception($"Invalid text range: [{range.Beg}, {range.End}). Current text length is {TextLength}");

			// Create the 'TextRange' structure
			using var text = Marshal_.Alloc(EHeap.HGlobal, range.Sizei + 1);
			using var buff = Marshal_.Alloc(EHeap.HGlobal, new Sci.TextRange
			{
				chrg = new Sci.CharacterRange { cpMin = range.Begi, cpMax = range.Endi },
				lpstrText = text.Value.Ptr,
			});

			// Request the text in the given range
			Cmd(Sci.SCI_GETTEXTRANGE, 0, buff.Value.Ptr);

			// Return the returned string
			return Marshal.PtrToStringAnsi(text.Value.Ptr) ?? string.Empty;
		}

		/// <summary>Return the number of whole characters in the range [beg,end)</summary>
		public long CharacterCount(long beg, long end)
		{
			// This will need a message added to Scintilla
			throw new NotImplementedException();
		}

		/// <summary>Return the byte offset to the start of the 'character_index'th character.</summary>
		public long PositionFromCharacter(long character_index)
		{
			// This will need a message added to Scintilla
			throw new NotImplementedException();
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
		public void InsertText(long pos, string text)
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
		public void AddText(string text)
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
		public void InsertStyledText(long pos, Sci.CellBuf cells)
		{
			using var c = cells.Pin();
			Cmd(Sci.SCI_INSERTSTYLEDTEXT, pos, c.Pointer);
		}

		/// <summary>Delete a character range from the document</summary>
		public void DeleteRange(long start, long length)
		{
			Cmd(Sci.SCI_DELETERANGE, start, length);
		}

		/// <summary>Returns the style at 'pos' in the document, or 0 if pos is negative or past the end of the document.</summary>
		public int GetStyleAt(long pos)
		{
			return (int)Cmd(Sci.SCI_GETSTYLEAT, pos);
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

		/// <summary>Raised whenever the selection changes</summary>
		public event EventHandler? SelectionChanged;
		private void OnSelectionChanged()
		{
			SelectionChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Get/Set the current caret position.
		/// 'Set' sets the current position and creates a selection between the anchor and the current position.
		/// The caret is not scrolled into view.</summary>
		public long CurrentPos
		{
			get => Cmd(Sci.SCI_GETCURRENTPOS);
			set => Cmd(Sci.SCI_SETCURRENTPOS, value);
		}

		/// <summary>
		/// Get/Set the current anchor position.
		/// 'Set' sets the anchor position and creates a selection between the anchor position and the current position.
		/// The caret is not scrolled into view.</summary>
		public long Anchor
		{
			get => Cmd(Sci.SCI_GETANCHOR);
			set => Cmd(Sci.SCI_SETANCHOR, value);
		}

		/// <summary>Get the current line at the caret position</summary>
		public long CurrentLineIndex
		{
			get => LineIndexFromPosition(CurrentPos);
		}

		/// <summary>Get the current column at the caret position</summary>
		public long CurrentColumn
		{
			get => ColumnFromPosition(CurrentPos);
		}

		/// <summary>The *unnormalised* range of selected text. Note: *not* [SelectionStart,SelectionEnd)</summary>
		public RangeI Selection
		{
			get => new(Anchor, CurrentPos);
			set => SetSel(value.Beg, value.End);
		}

		/// <summary>
		/// Get/Set the selection start position.
		/// 'Get' returns the start of the selection without regard to which end is the current position and which is the anchor.
		/// SCI_GETSELECTIONSTART returns the smaller of the current position or the anchor position.
		/// SCI_GETSELECTIONEND returns the larger of the two values.
		/// 'Set' sets the selection based on the assumption that the anchor position is less than the current position.
		/// It does not make the caret visible. After set, the anchor position is unchanged, and the caret position is Max(anchor, current)</summary>
		public long SelectionStart
		{
			get => Cmd(Sci.SCI_GETSELECTIONSTART);
			set => Cmd(Sci.SCI_SETSELECTIONSTART, value);
		}

		/// <summary>
		/// Get/Set the selection end position.
		/// 'Get' returns the end of the selection without regard to which end is the current position and which is the anchor.
		/// SCI_GETSELECTIONSTART returns the smaller of the current position or the anchor position.
		/// SCI_GETSELECTIONEND returns the larger of the two values.
		/// 'Set' sets the selection based on the assumption that the anchor position is less than the current position.
		/// It does not make the caret visible. After set, the anchor position is Min(anchor, caret), and the caret position is unchanged.</summary>
		public long SelectionEnd
		{
			get => Cmd(Sci.SCI_GETSELECTIONEND);
			set => Cmd(Sci.SCI_SETSELECTIONEND, value);
		}

		/// <summary>
		/// Get/Set the selection mode, which can be stream (SC_SEL_STREAM=0) or rectangular (SC_SEL_RECTANGLE=1) or by lines (SC_SEL_LINES=2) or thin rectangular (SC_SEL_THIN=3).
		/// When set in these modes, regular caret moves will extend or reduce the selection, until the mode is cancelled by a call with same value or with SCI_CANCEL.
		/// The get function returns the current mode even if the selection was made by mouse or with regular extended moves.
		/// SC_SEL_THIN is the mode after a rectangular selection has been typed into and ensures that no characters are selected.</summary>
		public Sci.ESelectionMode SelectionMode
		{
			get => (Sci.ESelectionMode)Cmd(Sci.SCI_GETSELECTIONMODE);
			set => Cmd(Sci.SCI_SETSELECTIONMODE, (long)value);
		}

		/// <summary>Set the range of selected text. 'caret' can be less than 'anchor'</summary>
		public void SetSel(long anchor, long caret)
		{
			Cmd(Sci.SCI_SETSEL, anchor, caret);
		}

		/// <summary>This selects all the text in the document. The current position is not scrolled into view.</summary>
		public void SelectAll()
		{
			Cmd(Sci.SCI_SELECTALL);
		}

		/// <summary>This removes any selection and sets the caret at 'position'. The caret is not scrolled into view.</summary>
		public void ClearSelection(long position)
		{
			Cmd(Sci.SCI_SETEMPTYSELECTION, position);
		}

		/// <summary>RAII scope for a selection</summary>
		public Scope<RangeI> PreserveSelection()
		{
			return Scope.Create(() => Selection, sel => Selection = sel);
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
			return Encoding.UTF8.GetString(buf, 0, (int)len);
		}

		/// <summary>Retrieves the text of the line containing the caret and returns the position within the line of the caret.</summary>
		public string GetCurLine(out int caret_offset)
		{
			// Get the length of text on the current line
			var len = Cmd(Sci.SCI_GETCURLINE);
			var buf = new byte[len];

			// Allocate a global heap buffer and read the line into it
			using var t = GCHandle_.Alloc(buf, GCHandleType.Pinned);
			caret_offset = (int)Cmd(Sci.SCI_GETCURLINE, buf.Length, t.Handle.AddrOfPinnedObject());

			// Trim null terminators
			for (; len != 0 && buf[len - 1] == 0; --len) { }
			return Encoding.UTF8.GetString(buf, 0, (int)len);
		}
		public string GetCurLine()
		{
			return GetCurLine(out _);
		}

		/// <summary>Get the position of the start and end of the selection at the given line with INVALID_POSITION returned if no selection on this line.</summary>
		public RangeI GetLineSelectionPosition(long line)
		{
			return new RangeI(
				Cmd(Sci.SCI_GETLINESELSTARTPOSITION, line),
				Cmd(Sci.SCI_GETLINESELENDPOSITION, line));
		}

		/// <summary>Get/Set the index of the first visible line</summary>
		public long FirstVisibleLine
		{
			get => Cmd(Sci.SCI_GETFIRSTVISIBLELINE);
			set => Cmd(Sci.SCI_SETFIRSTVISIBLELINE, value);
		}

		/// <summary>Get the number of lines visible on screen</summary>
		public long LinesOnScreen => Cmd(Sci.SCI_LINESONSCREEN);

		/// <summary></summary>
		public bool GetModify()
		{
			return Cmd(Sci.SCI_GETMODIFY) != 0;
		}

		/// <summary>
		/// Removes any selection, sets the caret at caret and scrolls the view to make the caret visible, if necessary.
		/// It is equivalent to SCI_SETSEL(caret, caret). The anchor position is set the same as the current position.</summary>
		public void GotoPos(long pos)
		{
			Cmd(Sci.SCI_GOTOPOS, pos);
		}

		/// <summary>
		/// Removes any selection and sets the caret at the start of line number line and scrolls the view (if needed)
		/// to make it visible. The anchor position is set the same as the current position. If line is outside the lines
		/// in the document (first line is 0), the line set is the first or last.</summary>
		public void GotoLine(long line_index)
		{
			Cmd(Sci.SCI_GOTOLINE, line_index);
		}

		/// <summary>Return the character index position for the start of 'line_index'</summary>
		public long PositionFromLineIndex(long line_index)
		{
			return Cmd(Sci.SCI_POSITIONFROMLINE, line_index);
		}

		/// <summary>Get the line index from a character index</summary>
		public long LineIndexFromPosition(long pos)
		{
			return Cmd(Sci.SCI_LINEFROMPOSITION, pos);
		}

		/// <summary>Get the text of the line that contains 'pos'</summary>
		public string LineFromPosition(long pos)
		{
			var line_index = LineIndexFromPosition(pos);
			return GetLine(line_index);
		}
		public string LineFromPosition(long pos, out RangeI range)
		{
			var line_index = LineIndexFromPosition(pos);
			range = LineRange(line_index);
			return GetLine(line_index);
		}

		/// <summary>
		/// Returns the position at the end of the line, before any line end characters. If line is the last line in the document
		/// (which does not have any end of line characters) or greater, the result is the size of the document. If line is
		/// negative the result is undefined.</summary>
		public long LineEndPosition(long line_index)
		{
			return Cmd(Sci.SCI_GETLINEENDPOSITION, line_index);
		}

		/// <summary>Gets the character range for a line (including new line characters)</summary>
		public RangeI LineRange(long line_index)
		{
			return new RangeI(
				PositionFromLineIndex(line_index),
				PositionFromLineIndex(line_index + 1));
		}

		/// <summary>Return the length of line 'line'</summary>
		public long LineLength(long line)
		{
			return Cmd(Sci.SCI_LINELENGTH, line);
		}

		/// <summary>
		/// Returns the column number of a position 'pos' within the document taking the width of tabs into account.
		/// This returns the column number of the last tab on the line before pos, plus the number of characters between
		/// the last tab and pos. If there are no tab characters on the line, the return value is the number of characters
		/// up to the position on the line. In both cases, double byte characters count as a single character.
		/// This is probably only useful with mono-spaced fonts.</summary>
		public int ColumnFromPosition(long pos)
		{
			return (int)Cmd(Sci.SCI_GETCOLUMN, pos);
		}

		/// <summary>
		/// Returns the character position of a column on a line taking the width of tabs into account.
		/// It treats a multi-byte character as a single column. Column numbers, like lines start at 0.</summary>
		public long GetPos(long line, int column)
		{
			return FindColumn(line, column);
		}
		public long FindColumn(long line, int column)
		{
			return Cmd(Sci.SCI_FINDCOLUMN, line, column);
		}

		/// <summary>Gets the closest character position to a point.</summary>
		public long PositionFromPoint(int x, int y)
		{
			return Cmd(Sci.SCI_POSITIONFROMPOINT, x, y);
		}

		/// <summary>Similar to PositionFromPoint, but returns -1 if the point is outside the window or not close to any characters.</summary>
		public long PositionFromPointClose(int x, int y)
		{
			return Cmd(Sci.SCI_POSITIONFROMPOINTCLOSE, x, y);
		}

		/// <summary></summary>
		public long PointXFromPosition(long pos)
		{
			return Cmd(Sci.SCI_POINTXFROMPOSITION, 0, pos);
		}

		/// <summary></summary>
		public long PointYFromPosition(long pos)
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
		public long WordStartPosition(long pos, bool onlyWordCharacters)
		{
			return Cmd(Sci.SCI_WORDSTARTPOSITION, pos, onlyWordCharacters ? 1 : 0);
		}

		/// <summary></summary>
		public long WordEndPosition(long pos, bool onlyWordCharacters)
		{
			return Cmd(Sci.SCI_WORDENDPOSITION, pos, onlyWordCharacters ? 1 : 0);
		}

		/// <summary></summary>
		public long PositionBefore(long pos)
		{
			return Cmd(Sci.SCI_POSITIONBEFORE, pos);
		}

		/// <summary></summary>
		public long PositionAfter(long pos)
		{
			return Cmd(Sci.SCI_POSITIONAFTER, pos);
		}

		/// <summary>
		/// Returns the width (in pixels) of a string drawn in the given style.
		/// Can be used, for example, to decide how wide to make the line number margin in order to display a given number of numerals.</summary>
		public double TextWidth(int style, string text)
		{
			var text_bytes = Encoding.UTF8.GetBytes(text);
			using var t = GCHandle_.Alloc(text_bytes, GCHandleType.Pinned);
			return Cmd(Sci.SCI_TEXTWIDTH, style, t.Handle.AddrOfPinnedObject());
		}

		/// <summary>Returns the height (in pixels) of a particular line. Currently all lines are the same height.</summary>
		public double TextHeight(long line)
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
		public Sci.EMultiPaste MutliPaste
		{
			get => (Sci.EMultiPaste)Cmd(Sci.SCI_GETMULTIPASTE);
			set => Cmd(Sci.SCI_SETMULTIPASTE, (long)value);
		}

		/// <summary>
		/// Virtual space can be enabled or disabled for rectangular selections or in other circumstances or in both.
		/// There are two bit flags SCVS_RECTANGULARSELECTION=1 and SCVS_USERACCESSIBLE=2 which can be set independently.
		/// SCVS_NONE=0, the default, disables all use of virtual space.</summary>
		public Sci.EVirtualSpace VirtualSpace
		{
			get => (Sci.EVirtualSpace)Cmd(Sci.SCI_GETVIRTUALSPACEOPTIONS);
			set => Cmd(Sci.SCI_SETVIRTUALSPACEOPTIONS, (long)value);
		}

		/// <summary>Insert/Overwrite</summary>
		public bool Overtype
		{
			get => Cmd(Sci.SCI_GETOVERTYPE) != 0;
			set => Cmd(Sci.SCI_SETOVERTYPE, value ? 1 : 0);
		}

		#endregion
		#region Cut, Copy And Paste

		/// <summary></summary>
		public virtual void Cut()
		{
			Cmd(Sci.SCI_CUT);
		}

		/// <summary></summary>
		public virtual void Copy()
		{
			Cmd(Sci.SCI_COPY);
		}

		/// <summary></summary>
		public virtual void Paste()
		{
			Cmd(Sci.SCI_PASTE);
		}

		/// <summary></summary>
		public virtual bool CanPaste()
		{
			return Cmd(Sci.SCI_CANPASTE) != 0;
		}

		/// <summary>Clear the *selected* text</summary>
		public virtual void Clear()
		{
			Cmd(Sci.SCI_CLEAR);
		}

		/// <summary></summary>
		public void CopyRange(int first, int last)
		{
			Cmd(Sci.SCI_COPYRANGE, first, last);
		}

		/// <summary></summary>
		public void CopyText(StringBuilder text, long length)
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
		#region Auto Complete

		// Typical Usage:
		//  - Handle 'AutoComplete' to provide the list of possible completions
		//  - Handle 'AutoCompleteSelection' if you don't want default behaviour which is to replace the current word with the completion text.
		//  - Handle 'AutoCompleteDone' if you just want to know when an auto complete has happened.
		// Notes:
		//  - Autocompletion displays a list box showing likely identifiers based upon the user's typing. The user chooses the currently selected item by pressing
		//    the tab character or another character that is a member of the fillup character set defined with SCI_AUTOCSETFILLUPS. Autocompletion is triggered by
		//    your application. For example, in C if you detect that the user has just typed "fred", you could look up fred, and if it has a known list of members,
		//    you could offer them in an autocompletion list. Alternatively, you could monitor the user's typing and offer a list of likely items once their typing
		//    has narrowed down the choice to a reasonable list. As yet another alternative, you could define a key code to activate the list.
		//  - When the user makes a selection from the list the container is sent a SCN_AUTOCSELECTION notification message. On return from the notification Scintilla
		//    will insert the selected text and the container is sent a SCN_AUTOCCOMPLETED notification message unless the autocompletion list has been cancelled, for
		//    example by the container sending SCI_AUTOCCANCEL.
		//  - To make use of autocompletion you must monitor each character added to the document.See SciTEBase::CharAdded() in SciTEBase.cxx for an example of autocompletion.

		/// <summary>Provider of auto completion lists based on a partial text match</summary>
		public event EventHandler<AutoCompleteEventArgs>? AutoComplete;
		public class AutoCompleteEventArgs :EventArgs
		{
			// Notes:
			//  - 'PartialWord' is set to a default value based on standard word characters,
			//    callers can change it based on whatever criteria is needed.
			//  - 'Position' is the current caret position. The start position for auto completion
			//    equals 'Position - PartialWord.Length'

			public AutoCompleteEventArgs(long position, string partial_word)
			{
				Position = position;
				PartialWord = partial_word;
				Completions = new List<string>();
				Handled = false;
			}

			/// <summary>The current caret position</summary>
			public long Position { get; }

			/// <summary>Get/Set the partial text already typed</summary>
			public string PartialWord { get; set; }

			/// <summary>The list of possible completions</summary>
			public List<string> Completions { get; }

			/// <summary>True if auto completion data has been provided</summary>
			public bool Handled { get; set; }
		}
		private void OnAutoComplete()
		{
			// Leave the determination of the partial word to the event handler.
			// I could use 'WordChars' but it doesn't really work for UTF-8, also
			// users might want specific behaviour based on the nearby text.

			// Determine a default for the partial word
			var line = GetCurLine(out var caret_offset);
			var word_chars = WordChars;
			var i = caret_offset;
			for (; i-- != 0 && word_chars.Contains(line[i]);) { }
			var partial_word = line.Substring(i + 1, caret_offset - i - 1);

			// Request the completion list. If unhandled, auto complete is ignored.
			var args = new AutoCompleteEventArgs(CurrentPos, partial_word);
			AutoComplete?.Invoke(this, args);
			if (args.Handled && args.Completions.Count != 0)
			{
				// Create an unmanaged list of words and display them in the auto complete popup
				var word_list = string.Join(new string(AutoCompleteSeparator, 1), args.Completions);
				using var ptr = Marshal_.AllocUTF8String(EHeap.HGlobal, word_list);
				Cmd(Sci.SCI_AUTOCSHOW, args.PartialWord.Length, ptr.Value);
			}
		}

		/// <summary>Notifies that a selection has been made from the auto complete list (before text is inserted). Callers can cancel text insertion and use their own text</summary>
		public event EventHandler<AutoCompleteSelectionEventArgs>? AutoCompleteSelection;
		public class AutoCompleteSelectionEventArgs :EventArgs
		{
			// Notes:
			//  - Users should set 'ReplaceRange', 'Completion', and 'CaretPosition' appropriately.
			//  - If 'Cancel' is set, no auto completion is done.

			public AutoCompleteSelectionEventArgs(long position, RangeI replace_range, string completion, Sci.ECompletionMethods method)
			{
				Position = position;
				Method = method;
				ReplaceRange = replace_range;
				Completion = completion;
				CaretPosition = ReplaceRange.Beg + completion.Length;
				Handled = false;
				Cancel = false;
			}

			/// <summary>The start position of the text being completed</summary>
			public long Position { get; }

			/// <summary>How the auto complete list was closed</summary>
			public Sci.ECompletionMethods Method { get; }

			/// <summary>The range of text to replace with the completion text. Defaults to the word containing the caret position</summary>
			public RangeI ReplaceRange { get; set; }

			/// <summary>The replacement text</summary>
			public string Completion { get; set; }

			/// <summary>Where to position the caret after completion is done</summary>
			public long CaretPosition { get; set; }

			/// <summary>True to use the completion text provided in these args</summary>
			public bool Handled { get; set; }

			/// <summary>True to close the auto complete box without inserting text</summary>
			public bool Cancel { get; set; }
		}
		private void OnAutoCompleteSelection(long position, string completion, Sci.ECompletionMethods method)
		{
			var line_index = LineIndexFromPosition(position);
			var line_start = LineRange(line_index).Beg;
			var line = GetLine(line_index);

			// Determine a default for the range of characters to replace
			var word_chars = WordChars;
			long beg = (int)(position - line_start), end = beg;
			for (; end < line.Length && word_chars.Contains(line[(int)end]); ++end) { }
			var replace_range = new RangeI(line_start + beg, line_start + end);

			// Request the completion text to use. If not handled, fall back to the default handling
			var args = new AutoCompleteSelectionEventArgs(position, replace_range, completion, method);
			AutoCompleteSelection?.Invoke(this, args);
			if (args.Cancel)
			{
				// If canceled, don't do any auto completion
				Cmd(Sci.SCI_AUTOCCANCEL);
			}
			else if (args.Handled)
			{
				// Replace the text
				Target = args.ReplaceRange;
				ReplaceTarget(args.Completion);

				// Set the caret position
				Anchor = args.CaretPosition;
				CurrentPos = args.CaretPosition;

				// Cancel default auto complete and notify that completion is done
				Cmd(Sci.SCI_AUTOCCANCEL);
				OnAutoCompleteDone(args.Position, args.Completion, args.Method);
			}
		}

		/// <summary>Notification of when auto complete has finished</summary>
		public event EventHandler<AutoCompleteDoneEventArgs>? AutoCompleteDone;
		public class AutoCompleteDoneEventArgs :EventArgs
		{
			public AutoCompleteDoneEventArgs(long position, string completion, Sci.ECompletionMethods method)
			{
				Position = position;
				Completion = completion;
				Method = method;
			}

			/// <summary>The start position of the text being completed</summary>
			public long Position { get; }

			/// <summary>The text of the selected completion</summary>
			public string Completion { get; }

			/// <summary>How the auto complete list was closed</summary>
			public Sci.ECompletionMethods Method { get; }
		}
		private void OnAutoCompleteDone(long position, string completion, Sci.ECompletionMethods method)
		{
			AutoCompleteDone?.Invoke(this, new AutoCompleteDoneEventArgs(position, completion, method));
		}

		/// <summary>The configured auto completion list separator character (defaults to ' ')</summary>
		public char AutoCompleteSeparator
		{
			get => (char)Cmd(Sci.SCI_AUTOCGETSEPARATOR);
			set => Cmd(Sci.SCI_AUTOCSETSEPARATOR, value);
		}

		/// <summary>
		/// Sets the characters that will automatically cancel the autocompletion list.
		/// When you start the editor, this list is empty.</summary>
		public string AutoCompleteStopCharacters
		{
			set => Cmd(Sci.SCI_AUTOCSTOPS, 0, value);
		}

		/// <summary>
		/// By default, the list is cancelled if there are no viable matches (the user has typed characters that no longer match a list entry).
		/// If you want to keep displaying the original list, set AutoHide to false. This also effects SCI_AUTOCSELECT.</summary>
		public bool AutoCompleteAutoHide
		{
			get => Cmd(Sci.SCI_AUTOCGETAUTOHIDE) != 0;
			set => Cmd(Sci.SCI_AUTOCSETAUTOHIDE, value ? 1 : 0);
		}

		/// <summary>
		/// Get/Set whether autocomplete text matching is case sensitive.
		/// By default, matching of characters to list members is case sensitive.</summary>
		public bool AutoCompleteIgnoreCase
		{
			get => Cmd(Sci.SCI_AUTOCGETIGNORECASE) != 0;
			set => Cmd(Sci.SCI_AUTOCSETIGNORECASE, value ? 1 : 0);
		}

		/// <summary>
		/// Get or set the maximum width of an autocompletion list expressed as the number of characters in the longest
		/// item that will be totally visible. If zero (the default) then the list's width is calculated to fit the item
		/// with the most characters. Any items that cannot be fully displayed within the available width are indicated
		/// by the presence of ellipsis.</summary>
		public long AutoCompleteMaxWidth
		{
			get => Cmd(Sci.SCI_AUTOCSETMAXWIDTH);
			set => Cmd(Sci.SCI_AUTOCSETMAXWIDTH, value);
		}

		/// <summary>Get /Sset the maximum number of rows that will be visible in an autocompletion list.If there are more rows in the list, then a vertical scrollbar is shown.The default is 5.</summary>
		public int AutoCompleteMaxHeight
		{
			get => (int)Cmd(Sci.SCI_AUTOCSETMAXHEIGHT);
			set => Cmd(Sci.SCI_AUTOCSETMAXHEIGHT, value);
		}

		/// <summary>
		/// Get/Set
		/// When autocompletion is set to ignore case (SCI_AUTOCSETIGNORECASE), by default it will nonetheless select the first list member
		/// that matches in a case sensitive way to entered characters. This corresponds to a behaviour property of SC_CASEINSENSITIVEBEHAVIOUR_RESPECTCASE(0).
		/// If you want autocompletion to ignore case at all, choose SC_CASEINSENSITIVEBEHAVIOUR_IGNORECASE(1).</summary>
		public Sci.ECaseInsensitiveBehaviour AutoCompleteCaseSensitiveBehaviour
		{
			get => (Sci.ECaseInsensitiveBehaviour)Cmd(Sci.SCI_AUTOCGETCASEINSENSITIVEBEHAVIOUR);
			set => Cmd(Sci.SCI_AUTOCSETCASEINSENSITIVEBEHAVIOUR, (int)value);
		}

		#endregion
		#region Call Tips

		// Notes:
		//  - Call tips are small windows displaying the arguments to a function and are displayed after the user has typed the name of the function.
		//    They normally display characters using the font facename, size and character set defined by STYLE_DEFAULT. You can choose to use STYLE_CALLTIP
		//    to define the facename, size, foreground and background colours and character set with SCI_CALLTIPUSESTYLE. This also enables support for Tab
		//    characters. There is some interaction between call tips and autocompletion lists in that showing a call tip cancels any active autocompletion
		//    list, and vice versa.
		//  - Call tips can highlight part of the text within them. You could use this to highlight the current argument to a function by counting the number
		//    of commas (or whatever separator your language uses). See SciTEBase::CharAdded() in SciTEBase.cxx for an example of call tip use.
		//  - The mouse may be clicked on call tips and this causes a SCN_CALLTIPCLICK notification to be sent to the container. Small up and down arrows may
		//    be displayed within a call tip by, respectively, including the characters '\001', or '\002'. This is useful for showing that there are overloaded
		//    variants of one function name and that the user can click on the arrows to cycle through the overloads.
		//  - Alternatively, call tips can be displayed when you leave the mouse pointer for a while over a word in response to the SCN_DWELLSTART notification 
		//    and cancelled in response to SCN_DWELLEND. This method could be used in a debugger to give the value of a variable, or during editing to give
		//    information about the word under the pointer.

		/// <summary>Provider of call tip functionality</summary>
		public event EventHandler<CallTipEventArgs>? CallTip;
		public class CallTipEventArgs :EventArgs
		{
			public CallTipEventArgs(long position)
			{
				Position = position;
				Definition = string.Empty;
				Handled = false;
			}

			/// <summary>The caret position for where the call tip is required</summary>
			public long Position { get; }

			/// <summary>The definition to display for the call tip</summary>
			public string Definition { get; set; }

			/// <summary></summary>
			public bool Handled { get; set; }
		}
		private void DoCallTips()
		{
			var args = new CallTipEventArgs(CurrentPos);
			CallTip?.Invoke(this, args);
			if (args.Handled && args.Definition.Length != 0)
			{
				ShowCallTip(CurrentPos, args.Definition);
			}
		}

		/// <summary>
		/// Starts the process by displaying the call tip window. If a call tip is already active, this has no effect.
		/// 'pos' is the position in the document at which to align the call tip. The call tip text is aligned to start 1 line below this character unless you
		/// have included up and/or down arrows in the call tip text in which case the tip is aligned to the right-hand edge of the rightmost arrow.
		/// The assumption is that you will start the text with something like "\001 1 of 3 \002".
		/// 'definition' is the call tip text. This can contain multiple lines separated by '\n' (Line Feed, ASCII code 10) characters.
		/// Do not include '\r' (Carriage Return, ASCII code 13), as this will most likely print as an empty box. '\t' (Tab, ASCII code 9) is supported if you
		/// set a tabsize with SCI_CALLTIPUSESTYLE.
		/// The position of the caret is remembered here so that the call tip can be cancelled automatically if subsequent deletion moves the caret before this position.
		public void ShowCallTip(long pos, string definition)
		{
			Cmd(Sci.SCI_CALLTIPSHOW, pos, definition);
		}

		/// <summary>
		/// Cancels any displayed call tip. Scintilla will also cancel call tips for you if you use any keyboard commands that are not compatible with editing the
		/// argument list of a function. Call tips are cancelled if you delete back past the position where the caret was when the tip was triggered.</summary>
		public void CancelCallTip()
		{
			Cmd(Sci.SCI_CALLTIPCANCEL);
		}

		/// <summary>True if a call tip is active.</summary>
		public bool CallTipActive
		{
			get => Cmd(Sci.SCI_CALLTIPACTIVE) != 0;
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

		/// <summary>
		/// This message searches for text in the document. It does not use or move the current selection. The searchFlags argument controls the search type,
		/// which includes regular expression searches. You can search backwards to find the previous occurrence of a search string by setting the end of the
		/// search range before the start. The Sci_TextToFind structure is defined in Scintilla.h; set chrg.cpMin and chrg.cpMax with the range of positions
		/// in the document to search. You can search backwards by setting chrg.cpMax less than chrg.cpMin. Set the lpstrText member of Sci_TextToFind to point
		/// at a zero terminated text string holding the search pattern. If your language makes the use of Sci_TextToFind difficult, you should consider using
		/// SCI_SEARCHINTARGET instead.
		/// The return value is -1 if the search fails or the position of the start of the found text if it succeeds. The chrgText.cpMin and chrgText.cpMax
		/// members of Sci_TextToFind are filled in with the start and end positions of the found text.
		/// See also: SCI_SEARCHINTARGET
		/// Returns an empty range if no match is found</summary>
		public RangeI Find(Sci.EFindOption flags, string pattern, RangeI search_range)
		{
			// Create the 'TextToFind' structure
			using var text = Marshal_.AllocUTF8String(EHeap.HGlobal, pattern);
			using var buffer = Marshal_.Alloc(EHeap.HGlobal, new Sci.TextToFind
			{
				chrg = new Sci.CharacterRange { cpMin = search_range.Begi, cpMax = search_range.Endi },
				lpstrText = text.Value,
				chrgText = new Sci.CharacterRange(),
			});

			// Do the find
			var pos = Cmd(Sci.SCI_FINDTEXT, (long)flags, buffer.Value.Ptr);
			if (pos == -1)
				return new RangeI();

			var text_to_find = buffer.Value.As<Sci.TextToFind>();
			return new RangeI(text_to_find.chrgText.cpMin, text_to_find.chrgText.cpMax);
		}

		/// <summary>
		/// Sets the search start point used by SCI_SEARCHNEXT and SCI_SEARCHPREV to the start of the current selection, that is, the end of the selection
		/// that is nearer to the start of the document. You should always call this before calling either of SCI_SEARCHNEXT or SCI_SEARCHPREV.</summary>
		public void SearchAnchor()
		{
			Cmd(Sci.SCI_SEARCHANCHOR);
		}

		/// <summary>
		/// SCI_SEARCHNEXT and SCI_SEARCHPREV search for the next and previous occurrence of the zero terminated search string pointed at by text.
		/// The search is modified by the searchFlags. The return value is -1 if nothing is found, otherwise the return value is the start position
		/// of the matching text.The selection is updated to show the matched text, but is not scrolled into view.</summary>
		public long Search(Sci.EFindOption flags, string text, bool forward)
		{
			var cmd = forward ? Sci.SCI_SEARCHNEXT : Sci.SCI_SEARCHPREV;

			// Convert the search string to UTF-8
			var bytes = Encoding.UTF8.GetBytes(text);
			using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			return Cmd(cmd, (long)flags, h.Handle.AddrOfPinnedObject());
		}

		/// <summary>
		/// Get/Set the start and end of the target. When searching you can set start greater than end to find the last matching
		/// text in the target rather than the first matching text. The target is also set by a successful SCI_SEARCHINTARGET.</summary>
		public RangeI Target
		{
			get => new(TargetBeg, TargetEnd);
			set
			{
				TargetBeg = value.Beg;
				TargetEnd = value.End;
			}
		}
		public long TargetBeg
		{
			get => Cmd(Sci.SCI_GETTARGETSTART);
			set => Cmd(Sci.SCI_SETTARGETSTART, value);
		}
		public long TargetEnd
		{
			get => Cmd(Sci.SCI_GETTARGETEND);
			set => Cmd(Sci.SCI_SETTARGETEND, value);
		}

		/// <summary>Set the target start and end to the start and end positions of the selection.</summary>
		public void TargetFromSelection()
		{
			Cmd(Sci.SCI_TARGETFROMSELECTION);
		}

		/// <summary></summary>
		public Sci.EFindOption SearchFlags
		{
			get => (Sci.EFindOption)Cmd(Sci.SCI_GETSEARCHFLAGS);
			set => Cmd(Sci.SCI_SETSEARCHFLAGS, (long)value);
		}

		/// <summary>
		/// This searches for the first occurrence of a text string in the target defined by SCI_SETTARGETSTART and SCI_SETTARGETEND.
		/// The text string is not zero terminated; the size is set by length. The search is modified by the search flags set by SCI_SETSEARCHFLAGS.
		/// If the search succeeds, the target is set to the found text and the return value is the position of the start of
		/// the matching text. If the search fails, the result is -1.</summary>
		public long SearchInTarget(string text, long length = -1)
		{
			var bytes = Encoding.UTF8.GetBytes(text);
			length = length != -1 ? length : text.Length;
			using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			return Cmd(Sci.SCI_SEARCHINTARGET, length, h.Handle.AddrOfPinnedObject());
		}

		/// <summary>
		/// If length is -1, text is a zero terminated string, otherwise length sets the number of character to replace
		/// the target with. After replacement, the target range refers to the replacement text. The return value is the
		/// length of the replacement string. Note, the recommended way to delete text in the document is to set the
		/// target to the text to be removed, and to perform a replace target with an empty string.</summary>
		public long ReplaceTarget(string text, long length = -1)
		{
			var bytes = Encoding.UTF8.GetBytes(text);
			length = length != -1 ? length : text.Length;
			using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			return Cmd(Sci.SCI_REPLACETARGET, length, h.Handle.AddrOfPinnedObject());
		}

		/// <summary>
		/// This replaces the target using regular expressions. If length is -1, text is a zero terminated string,
		/// otherwise length is the number of characters to use. The replacement string is formed from the text string with any
		/// sequences of \1 through \9 replaced by tagged matches from the most recent regular expression search.
		/// \0 is replaced with all the matched text from the most recent search. After replacement, the target range refers
		/// to the replacement text. The return value is the length of the replacement string.</summary>
		public long ReplaceTargetRE(string text, long length = -1)
		{
			var bytes = Encoding.UTF8.GetBytes(text);
			length = length != -1 ? length : text.Length;
			using var h = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			return Cmd(Sci.SCI_REPLACETARGETRE, length, h.Handle.AddrOfPinnedObject());
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
		public Scope<long> ScrollScope()
		{
			return Scope.Create(
				() => FirstVisibleLine,
				fv => FirstVisibleLine = fv);
		}

		/// <summary>Gets the range of visible lines</summary>
		public RangeI VisibleLineIndexRange
		{
			get
			{
				var s = Cmd(Sci.SCI_GETFIRSTVISIBLELINE);
				var c = Cmd(Sci.SCI_LINESONSCREEN);
				return new RangeI(s, s + c);
			}
		}

		/// <summary></summary>
		public void ScrollToLine(long line)
		{
			LineScroll(0, line - LineIndexFromPosition(CurrentPos));
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
		public void LineScroll(int columns, long lines)
		{
			Cmd(Sci.SCI_LINESCROLL, columns, lines);
		}

		/// <summary>If the current position (this is the caret if there is no selection) is not visible, the view is scrolled to make it visible according to the current caret policy.</summary>
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

		/// <summary>
		/// Get the horizontal scroll position in pixels of the start of the text view.
		/// A value of 0 is the normal position with the first text column visible at the left of the view.</summary>
		public double XOffset
		{
			get => Cmd(Sci.SCI_GETXOFFSET);
			set => Cmd(Sci.SCI_SETXOFFSET, (long)value);
		}

		/// <summary>Scroll width (in pixels)</summary>
		public double ScrollWidth
		{
			get => Cmd(Sci.SCI_GETSCROLLWIDTH);
			set => Cmd(Sci.SCI_SETSCROLLWIDTH, (long)value);
		}

		/// <summary>Allow/Disallow scrolling up to one page past the last line</summary>
		public bool EndAtLastLine
		{
			get => Cmd(Sci.SCI_GETENDATLASTLINE) != 0;
			set => Cmd(Sci.SCI_SETENDATLASTLINE, value ? 1 : 0);
		}

		#endregion
		#region DragDrop

		//public new bool AllowDrop
		//{
		//	get => (Win32.GetWindowLong(Hwnd, Win32.GWL_EXSTYLE) & Win32.WS_EX_ACCEPTFILES) != 0;
		//	set => Win32.DragAcceptFiles(Hwnd, value);
		//}

		#endregion
		#region End of Line

		/// <summary>Get/Set the characters that are added into the document when the user presses the Enter key</summary>
		public Sci.EEndOfLine EOLMode
		{
			get => (Sci.EEndOfLine)Cmd(Sci.SCI_GETEOLMODE);
			set => Cmd(Sci.SCI_SETEOLMODE, (int)value);
		}

		/// <summary>Changes all the end of line characters in the document to match 'mode'</summary>
		public void ConvertEOLs(Sci.EEndOfLine mode)
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

		/// <summary>Set the text colour for style index 'idx'</summary>
		public void StyleSetFore(Sci.EStyleId id, Colour32 fore)
		{
			Cmd(Sci.SCI_STYLESETFORE, (int)id, (int)(fore.ARGB & 0x00FFFFFF));
		}

		/// <summary>Set the background colour for style index 'idx'</summary>
		public void StyleSetBack(Sci.EStyleId id, Colour32 back)
		{
			Cmd(Sci.SCI_STYLESETBACK, (int)id, (int)(back.ARGB & 0x00FFFFFF));
		}

		/// <summary>Set the font for style index 'idx'</summary>
		public void StyleSetFont(Sci.EStyleId id, string font_name)
		{
			var bytes = Encoding.UTF8.GetBytes(font_name);
			using var handle = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			Cmd(Sci.SCI_STYLESETFONT, (int)id, handle.Handle.AddrOfPinnedObject());
		}

		/// <summary>Set the font size for style index 'idx'</summary>
		public void StyleSetSize(Sci.EStyleId id, int sizePoints)
		{
			Cmd(Sci.SCI_STYLESETSIZE, (int)id, sizePoints);
		}

		/// <summary>Set bold or regular for style index 'idx'</summary>
		public void StyleSetBold(Sci.EStyleId id, bool bold)
		{
			Cmd(Sci.SCI_STYLESETBOLD, (int)id, bold ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetItalic(Sci.EStyleId id, bool italic)
		{
			Cmd(Sci.SCI_STYLESETITALIC, (int)id, italic ? 1 : 0);
		}

		/// <summary>Set underlined for style index 'idx'</summary>
		public void StyleSetUnderline(Sci.EStyleId id, bool underline)
		{
			Cmd(Sci.SCI_STYLESETUNDERLINE, (int)id, underline ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetEOLFilled(Sci.EStyleId id, bool filled)
		{
			Cmd(Sci.SCI_STYLESETEOLFILLED, (int)id, filled ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetCharacterSet(Sci.EStyleId id, int characterSet)
		{
			Cmd(Sci.SCI_STYLESETCHARACTERSET, (int)id, characterSet);
		}

		/// <summary></summary>
		public void StyleSetCase(Sci.EStyleId id, Sci.ECaseVisible @case)
		{
			Cmd(Sci.SCI_STYLESETCASE, (int)id, (int)@case);
		}

		/// <summary></summary>
		public void StyleSetVisible(Sci.EStyleId id, bool visible)
		{
			Cmd(Sci.SCI_STYLESETVISIBLE, (int)id, visible ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetChangeable(Sci.EStyleId id, bool changeable)
		{
			Cmd(Sci.SCI_STYLESETCHANGEABLE, (int)id, changeable ? 1 : 0);
		}

		/// <summary></summary>
		public void StyleSetHotSpot(Sci.EStyleId id, bool hotspot)
		{
			Cmd(Sci.SCI_STYLESETHOTSPOT, (int)id, hotspot ? 1 : 0);
		}

		/// <summary>Apply the give styles</summary>
		public void ApplyStyles(IEnumerable<Sci.StyleDesc> styles)
		{
			foreach (var style in styles)
			{
				if (style.Fore is Colour32 fore) StyleSetFore(style.Id, fore);
				if (style.Back is Colour32 back) StyleSetBack(style.Id, back);
				if (style.Font is string font) StyleSetFont(style.Id, font);
				if (style.Size is int size) StyleSetSize(style.Id, size);
				if (style.Bold is bool bold) StyleSetBold(style.Id, bold);
				if (style.Italic is bool italic) StyleSetItalic(style.Id, italic);
				if (style.Underline is bool underline) StyleSetUnderline(style.Id, underline);
				if (style.EOLFilled is bool eol_filled) StyleSetEOLFilled(style.Id, eol_filled);
				if (style.CharSet is int char_set) StyleSetCharacterSet(style.Id, char_set);
				if (style.Case is Sci.ECaseVisible @case) StyleSetCase(style.Id, @case);
				if (style.Visible is bool visible) StyleSetVisible(style.Id, visible);
				if (style.Changeable is bool changeable) StyleSetChangeable(style.Id, changeable);
				if (style.HotSpot is bool hot_spot) StyleSetHotSpot(style.Id, hot_spot);
			}
		}

		/// <summary>
		/// Scintilla keeps a record of the last character that is likely to be styled correctly. This is moved forwards when characters after
		/// it are styled and moved backwards if changes are made to the text of the document before it. Before drawing text, this position is
		/// checked to see if any styling is needed and, if so, a SCN_STYLENEEDED notification message is sent to the container. The container
		/// can send SCI_GETENDSTYLED to work out where it needs to start styling. Scintilla will always ask to style whole lines.</summary>
		public long GetEndStyled()
		{
			return Cmd(Sci.SCI_GETENDSTYLED);
		}

		/// <summary></summary>
		public void StartStyling(long pos, int mask)
		{
			Cmd(Sci.SCI_STARTSTYLING, pos, mask);
		}

		/// <summary></summary>
		public void SetStyling(long length, int style)
		{
			Cmd(Sci.SCI_SETSTYLING, length, style);
		}

		/// <summary></summary>
		public void SetStylingEx(long length, string styles)
		{
			throw new NotImplementedException();
			//Cmd(Sci.SCI_SETSTYLINGEX, length, styles);
		}

		/// <summary>
		/// As well as the 8 bits of lexical state stored for each character there is also an integer stored for each line. This can be used for longer
		/// lived parse states such as what the current scripting language is in an ASP page. Use SCI_SETLINESTATE to set the integer value and
		/// SCI_GETLINESTATE to get the value. Changing the value produces a SC_MOD_CHANGELINESTATE notification.</summary>
		public int LineState(long line)
		{
			return (int)Cmd(Sci.SCI_GETLINESTATE, line);
		}
		public void LineState(long line, int state)
		{
			Cmd(Sci.SCI_SETLINESTATE, line, state);
		}

		/// <summary>Gets the last line that has any line state.</summary>
		public long GetMaxLineState()
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

		/// <summary>
		/// The mnemonics may be replaced by a nominated symbol with an ASCII code in the range 32 to 255. If you set a symbol value less than 32, all control
		/// characters are displayed as mnemonics. The symbol you set is rendered in the font of the style set for the character. You can read back the current
		/// symbol with the SCI_GETCONTROLCHARSYMBOL message. The default symbol value is 0.</summary>
		public char ControlCharSymbol
		{
			get => (char)Cmd(Sci.SCI_GETCONTROLCHARSYMBOL);
			set => Cmd(Sci.SCI_SETCONTROLCHARSYMBOL, value & 0xFF);
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
			get => unchecked((uint)Cmd(Sci.SCI_GETCARETFORE));
			set => Cmd(Sci.SCI_SETCARETFORE, (long)value.ARGB);
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
			get => (int)Cmd(Sci.SCI_GETCARETPERIOD);
			set => Cmd(Sci.SCI_SETCARETPERIOD, value);
		}

		/// <summary>
		/// The width of the line caret can be set with SCI_SETCARETWIDTH to a value of 0, 1, 2 or 3 pixels. The default width is 1 pixel.
		/// You can read back the current width with SCI_GETCARETWIDTH. A width of 0 makes the caret invisible (added at version 1.50),
		/// similar to setting the caret style to CARETSTYLE_INVISIBLE (though not interchangeable). This setting only affects the width
		/// of the cursor when the cursor style is set to line caret mode, it does not affect the width for a block caret.</summary>
		public double CaretWidth
		{
			get => Cmd(Sci.SCI_GETCARETWIDTH);
			set => Cmd(Sci.SCI_SETCARETWIDTH, (long)value);
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

		/// <summary>
		/// Get/Set the type of a margin. The margin argument should be 0, 1, 2, 3 or 4. You can use the predefined constants SC_MARGIN_SYMBOL (0) and
		/// SC_MARGIN_NUMBER (1) to set a margin as either a line number or a symbol margin. A margin with application defined text may use SC_MARGIN_TEXT (4)
		/// or SC_MARGIN_RTEXT (5) to right justify the text. By convention, margin 0 is used for line numbers and the next two are used for symbols.
		/// You can also use the constants SC_MARGIN_BACK (2), SC_MARGIN_FORE (3), and SC_MARGIN_COLOUR (6) for symbol margins that set their background
		/// colour to match the STYLE_DEFAULT background and foreground colours or a specified colour.</summary>
		public Sci.EMarginType MarginTypeN(int margin)
		{
			return (Sci.EMarginType)Cmd(Sci.SCI_GETMARGINTYPEN, margin);
		}
		public void MarginTypeN(int margin, Sci.EMarginType marginType)
		{
			Cmd(Sci.SCI_SETMARGINTYPEN, margin, (long)marginType);
		}

		/// <summary>
		/// Get/Set the width of a margin in pixels. A margin with zero width is invisible. By default, Scintilla sets margin 1 for symbols with a width
		/// of 16 pixels, so this is a reasonable guess if you are not sure what would be appropriate. Line number margins widths should take into account
		/// the number of lines in the document and the line number style. You could use something like SCI_TEXTWIDTH(STYLE_LINENUMBER, "_99999") to get a suitable width.</summary>
		public double MarginWidthN(int margin)
		{
			return Cmd(Sci.SCI_GETMARGINWIDTHN, margin);
		}
		public void MarginWidthN(int margin, double pixelWidth)
		{
			Cmd(Sci.SCI_SETMARGINWIDTHN, margin, (long)pixelWidth);
		}

		/// <summary>
		/// The mask is a 32-bit value. Each bit corresponds to one of 32 logical symbols that can be displayed in a margin that is enabled for symbols.
		/// There is a useful constant, SC_MASK_FOLDERS (0xFE000000 or -33554432), that is a mask for the 7 logical symbols used to denote folding.
		/// You can assign a wide range of symbols and colours to each of the 32 logical symbols, see Markers for more information. If (mask & SC_MASK_FOLDERS)==0,
		/// the margin background colour is controlled by style 33 (STYLE_LINENUMBER).
		/// You add logical markers to a line with SCI_MARKERADD.If a line has an associated marker that does not appear in the mask of any margin with a non-zero width,
		/// the marker changes the background colour of the line.For example, suppose you decide to use logical marker 10 to mark lines with a syntax error and you want
		/// to show such lines by changing the background colour.The mask for this marker is 1 shifted left 10 times (1<<10) which is 0x400. If you make sure that no
		/// symbol margin includes 0x400 in its mask, any line with the marker gets the background colour changed.
		/// To set a non-folding margin 1 use SCI_SETMARGINMASKN(1, ~SC_MASK_FOLDERS) which is the default set by Scintilla. To set a folding margin 2 use
		/// SCI_SETMARGINMASKN(2, SC_MASK_FOLDERS). ~SC_MASK_FOLDERS is 0x1FFFFFF in hexadecimal or 33554431 decimal. Of course, you may need to display all 32 symbols in
		/// a margin, in which case use SCI_SETMARGINMASKN(margin, -1).</summary>
		public uint MarginMaskN(int margin)
		{
			return (uint)Cmd(Sci.SCI_GETMARGINMASKN, margin);
		}
		public void MarginMaskN(int margin, uint mask)
		{
			Cmd(Sci.SCI_SETMARGINMASKN, margin, mask);
		}

		/// <summary>
		/// Each of the five margins can be set sensitive or insensitive to mouse clicks. A click in a sensitive margin sends a SCN_MARGINCLICK or SCN_MARGINRIGHTCLICK
		/// notification to the container. Margins that are not sensitive act as selection margins which make it easy to select ranges of lines. By default, all margins are insensitive.</summary>
		public bool MarginSensitiveN(int margin)
		{
			return Cmd(Sci.SCI_GETMARGINSENSITIVEN, margin) != 0;
		}
		public void MarginSensitiveN(int margin, bool sensitive)
		{
			Cmd(Sci.SCI_SETMARGINSENSITIVEN, margin, sensitive ? 1 : 0);
		}

		/// <summary>Get/Set the width of the blank margin on the left side of the text in pixels. The default is to one pixel on each side.</summary>
		public double MarginLeft
		{
			get => Cmd(Sci.SCI_GETMARGINLEFT);
			set => Cmd(Sci.SCI_SETMARGINLEFT, 0, (long)value);
		}

		/// <summary>Get/Set the width of the blank margin on the right side of the text in pixels. The default is to one pixel on each side.</summary>
		public double MarginRight
		{
			get => Cmd(Sci.SCI_GETMARGINRIGHT);
			set => Cmd(Sci.SCI_SETMARGINRIGHT, 0, (long)value);
		}

		#endregion
		#region Brace Highlighting

		/// <summary></summary>
		public void BraceHighlight(long pos1, long pos2)
		{
			Cmd(Sci.SCI_BRACEHIGHLIGHT, pos1, pos2);
		}

		/// <summary></summary>
		public void BraceBadLight(long pos)
		{
			Cmd(Sci.SCI_BRACEBADLIGHT, pos);
		}

		/// <summary>
		/// The SCI_BRACEMATCH message finds a corresponding matching brace given pos, the position of one brace.
		/// The brace characters handled are '(', ')', '[', ']', '{', '}', '<', and '>'. The search is forwards from an opening
		/// brace and backwards from a closing brace. If the character at position is not a brace character, or a matching brace
		/// cannot be found, the return value is -1. Otherwise, the return value is the position of the matching brace.
		/// A match only occurs if the style of the matching brace is the same as the starting brace or the matching brace is beyond
		/// the end of styling. Nested braces are handled correctly. The maxReStyle parameter must currently be 0 - it may be used in
		/// the future to limit the length of brace searches.</summary>
		public long BraceMatch(long pos, int max_re_style = 0)
		{
			return Cmd(Sci.SCI_BRACEMATCH, pos, max_re_style);
		}

		#endregion
		#region Tabs / Indenting

		/// <summary>Enable/Disable auto indent mode</summary>
		public bool AutoIndent { get; set; }

		/// <summary>
		/// Gets/Sets the size of a tab as a multiple of the size of a space character in STYLE_DEFAULT.
		/// The default tab width is 8 characters. There are no limits on tab sizes, but values less than
		/// 1 or large values may have undesirable effects.</summary>
		public int TabWidth
		{
			get => (int)Cmd(Sci.SCI_GETTABWIDTH);
			set => Cmd(Sci.SCI_SETTABWIDTH, value);
		}

		/// <summary>
		/// Get/Set whether indentation should be created out of a mixture of tabs and spaces or be based purely on spaces.
		/// Set UseTabs to false (0) to create all tabs and indents out of spaces. The default is true. You can use SCI_GETCOLUMN
		/// to get the column of a position taking the width of a tab into account.</summary>
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
			get => (int)Cmd(Sci.SCI_GETINDENT);
			set => Cmd(Sci.SCI_SETINDENT, value);
		}

		/// <summary>Returns a string representing one unit of indentation</summary>
		public string IndentString => UseTabs ? "\t" : new string(' ', Indent);

		/// <summary>Gets the indentation level in units of the indent string</summary>
		public int LineIndentationLevel(long line_index)
		{
			return LineIndentation(line_index) / Indent;
		}

		/// <summary>Get/Set the amount of indentation on a line. The indentation is measured in character columns, which correspond to the width of space characters.</summary>
		public int LineIndentation(long line_index)
		{
			return (int)Cmd(Sci.SCI_GETLINEINDENTATION, line_index);
		}
		public void LineIndentation(long line_index, int indentSize)
		{
			Cmd(Sci.SCI_SETLINEINDENTATION, line_index, indentSize);
		}

		/// <summary>Returns the position at the end of indentation of a line</summary>
		public long LineIndentPosition(long line)
		{
			return Cmd(Sci.SCI_GETLINEINDENTPOSITION, line);
		}

		/// <summary>
		/// Indentation guides are dotted vertical lines that appear within indentation white space every indent size columns.
		/// They make it easy to see which constructs line up especially when they extend over multiple pages.
		/// Style STYLE_INDENTGUIDE (37) is used to specify the foreground and background colour of the indentation guides.
		/// There are 4 indentation guide views. SC_IV_NONE turns the feature off but the other 3 states determine how far the guides appear on empty lines.
		///   SC_IV_NONE - No indentation guides are shown.
		///   SC_IV_REAL - Indentation guides are shown inside real indentation white space.
		///   SC_IV_LOOKFORWARD - Indentation guides are shown beyond the actual indentation up to the level of the next non-empty line.
		///      If the previous non-empty line was a fold header then indentation guides are shown for one more level of indent than that line.
		///      This setting is good for Python.
		///   SC_IV_LOOKBOTH - Indentation guides are shown beyond the actual indentation up to the level of the next non-empty line or
		///      previous non-empty line whichever is the greater. This setting is good for most languages.</summary>
		public Sci.EIndentView IndentationGuides
		{
			get => (Sci.EIndentView)Cmd(Sci.SCI_GETINDENTATIONGUIDES);
			set => Cmd(Sci.SCI_SETINDENTATIONGUIDES, (long)value);
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

		/// <summary>
		/// When brace highlighting occurs, the indentation guide corresponding to the braces may be highlighted with the brace highlighting style,
		/// STYLE_BRACELIGHT (34). Set column to 0 to cancel this highlight.</summary>
		public int HighlightGuide
		{
			get => (int)Cmd(Sci.SCI_GETHIGHLIGHTGUIDE);
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

		/// <summary>
		/// This message adds marker number markerNumber to a line. The message returns -1 if this fails (illegal line number, out of memory)
		/// or it returns a marker handle number that identifies the added marker. You can use this returned handle with SCI_MARKERLINEFROMHANDLE
		/// to find where a marker is after moving or combining lines and with SCI_MARKERDELETEHANDLE to delete the marker based on its handle.
		/// The message does not check the value of markerNumber, nor does it check if the line already contains the marker.</summary>
		public long MarkerAdd(long line, int markerNumber)
		{
			return Cmd(Sci.SCI_MARKERADD, line, markerNumber);
		}

		/// <summary></summary>
		public long MarkerAddSet(long line, int markerNumber)
		{
			return Cmd(Sci.SCI_MARKERADDSET, line, markerNumber);
		}

		/// <summary></summary>
		public void MarkerDelete(long line, int markerNumber)
		{
			Cmd(Sci.SCI_MARKERDELETE, line, markerNumber);
		}

		/// <summary></summary>
		public void MarkerDeleteAll(int markerNumber)
		{
			Cmd(Sci.SCI_MARKERDELETEALL, markerNumber);
		}

		/// <summary></summary>
		public long MarkerGet(long line)
		{
			return Cmd(Sci.SCI_MARKERGET, line);
		}

		/// <summary></summary>
		public long MarkerNext(long lineStart, int markerMask)
		{
			return Cmd(Sci.SCI_MARKERNEXT, lineStart, markerMask);
		}

		/// <summary></summary>
		public long MarkerPrevious(long lineStart, int markerMask)
		{
			return Cmd(Sci.SCI_MARKERPREVIOUS, lineStart, markerMask);
		}

		/// <summary></summary>
		public long MarkerLineFromHandle(long handle)
		{
			return Cmd(Sci.SCI_MARKERLINEFROMHANDLE, handle);
		}

		/// <summary></summary>
		public void MarkerDeleteHandle(long handle)
		{
			Cmd(Sci.SCI_MARKERDELETEHANDLE, handle);
		}

		#endregion
		#region Context Menu

		/// <summary>Set whether the built-in context menu is used</summary>
		public bool UsePopUp
		{
			set => Cmd(Sci.SCI_USEPOPUP, value ? 1 : 0);
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

		/// <summary>Get/Set a different size to print at instead of the screen font. magnification is the number of points to add to the size of each screen font.
		/// A value of -3 or -4 gives reasonably small print. You can get this value with SCI_GETPRINTMAGNIFICATION.</summary>
		public int PrintMagnification
		{
			get => (int)Cmd(Sci.SCI_GETPRINTMAGNIFICATION);
			set => Cmd(Sci.SCI_SETPRINTMAGNIFICATION, value);
		}

		/// <summary></summary>
		public Sci.EPrintOption PrintColourMode
		{
			get => (Sci.EPrintOption)Cmd(Sci.SCI_GETPRINTCOLOURMODE);
			set => Cmd(Sci.SCI_SETPRINTCOLOURMODE, (long)value);
		}

		/// <summary></summary>
		public Sci.EWrap PrintWrapMode
		{
			get => (Sci.EWrap)Cmd(Sci.SCI_GETPRINTWRAPMODE);
			set => Cmd(Sci.SCI_SETPRINTWRAPMODE, (long)value);
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

		/// <summary>
		/// When some lines are hidden and/or annotations are displayed, then a particular line in the document may be displayed at a different position
		/// to its document position. If no lines are hidden and there are no annotations, this message returns docLine. Otherwise, this returns the display
		/// line (counting the very first visible line as 0). The display line of an invisible line is the same as the previous visible line. The display line
		/// number of the first line in the document is 0. If lines are hidden and docLine is outside the range of lines in the document, the return value is -1.
		/// Lines can occupy more than one display line if they wrap.</summary>
		public long VisibleFromDocLine(long line)
		{
			return Cmd(Sci.SCI_VISIBLEFROMDOCLINE, line);
		}

		/// <summary>
		/// When some lines are hidden and/or annotations are displayed, then a particular line in the document may be displayed at a different position to its
		/// document position. This message returns the document line number that corresponds to a display line (counting the display line of the first line in
		/// the document as 0). If displayLine is less than or equal to 0, the result is 0. If displayLine is greater than or equal to the number of displayed
		/// lines, the result is the number of lines in the document.</summary>
		public long DocLineFromVisible(long lineDisplay)
		{
			return Cmd(Sci.SCI_DOCLINEFROMVISIBLE, lineDisplay);
		}

		/// <summary></summary>
		public void ShowLines(long lineStart, long lineEnd)
		{
			Cmd(Sci.SCI_SHOWLINES, lineStart, lineEnd);
		}

		/// <summary></summary>
		public void HideLines(long lineStart, long lineEnd)
		{
			Cmd(Sci.SCI_HIDELINES, lineStart, lineEnd);
		}

		/// <summary></summary>
		public bool GetLineVisible(long line)
		{
			return Cmd(Sci.SCI_GETLINEVISIBLE, line) != 0;
		}

		/// <summary></summary>
		public int FoldLevel(long line)
		{
			return (int)Cmd(Sci.SCI_GETFOLDLEVEL, line);
		}

		/// <summary></summary>
		public void FoldLevel(long line, int level)
		{
			Cmd(Sci.SCI_SETFOLDLEVEL, line, level);
		}

		/// <summary></summary>
		public void FoldFlags(int flags)
		{
			Cmd(Sci.SCI_SETFOLDFLAGS, flags);
		}

		/// <summary>
		/// This message searches for the next line after line, that has a folding level that is less than or equal to level and then returns the previous line number.
		/// If you set level to -1, level is set to the folding level of line line. If from is a fold point, SCI_GETLASTCHILD(from, -1) returns the last line that would
		/// be in made visible or hidden by toggling the fold state.</summary>
		public long GetLastChild(long line, int level)
		{
			return Cmd(Sci.SCI_GETLASTCHILD, line, level);
		}

		/// <summary></summary>
		public long GetFoldParent(long line)
		{
			return Cmd(Sci.SCI_GETFOLDPARENT, line);
		}

		/// <summary></summary>
		public bool FoldExpanded(long line)
		{
			return Cmd(Sci.SCI_GETFOLDEXPANDED, line) != 0;
		}

		/// <summary></summary>
		public void FoldExpanded(long line, bool expanded)
		{
			Cmd(Sci.SCI_SETFOLDEXPANDED, line, expanded ? 1 : 0);
		}

		/// <summary></summary>
		public void ToggleFold(long line)
		{
			Cmd(Sci.SCI_TOGGLEFOLD, line);
		}

		/// <summary></summary>
		public void EnsureVisible(long line)
		{
			Cmd(Sci.SCI_ENSUREVISIBLE, line);
		}

		/// <summary></summary>
		public void EnsureVisibleEnforcePolicy(long line)
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

		/// <summary>
		/// Set wrapMode to SC_WRAP_WORD (1) to enable wrapping on word or style boundaries, SC_WRAP_CHAR (2) to enable wrapping between any characters,
		/// SC_WRAP_WHITESPACE (3) to enable wrapping on whitespace, and SC_WRAP_NONE (0) to disable line wrapping. SC_WRAP_CHAR is preferred for Asian
		/// languages where there is no white space between words.</summary>
		public Sci.EWrap WrapMode
		{
			get => (Sci.EWrap)Cmd(Sci.SCI_GETWRAPMODE);
			set => Cmd(Sci.SCI_SETWRAPMODE, (long)value);
		}

		/// <summary>Enable the drawing of visual flags to indicate a line is wrapped. Bits set in wrapVisualFlags determine which visual flags are drawn.</summary>
		public Sci.EWrapVisualFlag WrapVisualFlags
		{
			get => (Sci.EWrapVisualFlag)Cmd(Sci.SCI_GETWRAPVISUALFLAGS);
			set => Cmd(Sci.SCI_SETWRAPVISUALFLAGS, (long)value);
		}

		/// <summary></summary>
		public Sci.EWrapVisualLocation WrapVisualFlagsLocation
		{
			get => (Sci.EWrapVisualLocation)Cmd(Sci.SCI_GETWRAPVISUALFLAGSLOCATION);
			set => Cmd(Sci.SCI_SETWRAPVISUALFLAGSLOCATION, (long)value);
		}

		/// <summary></summary>
		public Sci.EWrapIndentMode WrapStartIndent
		{
			get => (Sci.EWrapIndentMode)Cmd(Sci.SCI_GETWRAPSTARTINDENT);
			set => Cmd(Sci.SCI_SETWRAPSTARTINDENT, (long)value);
		}

		/// <summary></summary>
		public Sci.ELineCache LayoutCache
		{
			get => (Sci.ELineCache)Cmd(Sci.SCI_GETLAYOUTCACHE);
			set => Cmd(Sci.SCI_SETLAYOUTCACHE, (long)value);
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

		/// <summary>Document lines can occupy more than one display line if they wrap and this returns the number of display lines needed to wrap a document line.</summary>
		public int WrapCount(long line)
		{
			return (int)Cmd(Sci.SCI_WRAPCOUNT, line);
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

		/// <summary>
		/// These messages let you set and get the zoom factor directly. There is no limit set on the factors you can set, so limiting yourself to
		/// -10 to +20 to match the incremental zoom functions is a good idea.</summary>
		public int Zoom
		{
			get => (int)Cmd(Sci.SCI_GETZOOM);
			set => Cmd(Sci.SCI_SETZOOM, value);
		}

		#endregion
		#region Long Lines

		/// <summary>Get/Set the mode used to display long lines</summary>
		public Sci.EEdgeVisualStyle EdgeMode
		{
			get => (Sci.EEdgeVisualStyle)Cmd(Sci.SCI_GETEDGEMODE);
			set => Cmd(Sci.SCI_SETEDGEMODE, (long)value);
		}

		/// <summary>
		/// These messages set and get the column number at which to display the long line marker. When drawing lines, the column sets a position
		/// in units of the width of a space character in STYLE_DEFAULT. When setting the background colour, the column is a character count
		/// (allowing for tabs) into the line.</summary>
		public int EdgeColumn
		{
			get => (int)Cmd(Sci.SCI_GETEDGECOLUMN);
			set => Cmd(Sci.SCI_SETEDGECOLUMN, value);
		}

		/// <summary></summary>
		public Colour32 EdgeColour
		{
			get => new((uint)(0xFF000000 | Cmd(Sci.SCI_GETEDGECOLOUR)));
			set => Cmd(Sci.SCI_SETEDGECOLOUR, (int)(value.ARGB & 0x00FFFFFF));
		}

		#endregion
		#region Lexer

		/// <summary></summary>
		public Sci.ELexer Lexer
		{
			get => (Sci.ELexer)Cmd(Sci.SCI_GETLEXER);
			set => Cmd(Sci.SCI_SETLEXER, (long)value);
		}

		/// <summary></summary>
		public void LexerLanguage(string language)
		{
			var bytes = Encoding.UTF8.GetBytes(language);
			using var handle = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
			Cmd(Sci.SCI_SETLEXERLANGUAGE, 0, handle.Handle.AddrOfPinnedObject());
		}

		///// <summary></summary>
		//public void LoadLexerLibrary(string path)
		//{
		//	var bytes = Encoding.UTF8.GetBytes(path);
		//	using var handle = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
		//	Cmd(Sci.SCI_LOADLEXERLIBRARY, 0, handle.Handle.AddrOfPinnedObject());
		//}

		/// <summary></summary>
		public void Colourise(long start, long end)
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
			return Encoding.UTF8.GetString(buf, 0, (int)len);
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

		#endregion
		#region Notifications

		/// <summary>
		/// Get/Set the event mask that determines which document change events are notified to the container with SCN_MODIFIED and SCEN_CHANGE.
		/// For example, a container may decide to see only notifications about changes to text and not styling changes by calling SCI_SETMODEVENTMASK(SC_MOD_INSERTTEXT|SC_MOD_DELETETEXT).
		/// The possible notification types are the same as the modificationType bit flags used by SCN_MODIFIED: SC_MOD_INSERTTEXT, SC_MOD_DELETETEXT, SC_MOD_CHANGESTYLE, SC_MOD_CHANGEFOLD,
		/// SC_PERFORMED_USER, SC_PERFORMED_UNDO, SC_PERFORMED_REDO, SC_MULTISTEPUNDOREDO, SC_LASTSTEPINUNDOREDO, SC_MOD_CHANGEMARKER, SC_MOD_BEFOREINSERT, SC_MOD_BEFOREDELETE,
		/// SC_MULTILINEUNDOREDO, and SC_MODEVENTMASKALL.</summary>
		public long ModEventMask
		{
			get => Cmd(Sci.SCI_GETMODEVENTMASK);
			set => Cmd(Sci.SCI_SETMODEVENTMASK, value);
		}

		/// <summary>
		/// Get/Set the time the mouse must sit still, in milliseconds, to generate a SCN_DWELLSTART notification.
		/// If set to SC_TIME_FOREVER, the default, no dwell events are generated.</summary>
		public int MouseDwellTime
		{
			get => (int)Cmd(Sci.SCI_GETMOUSEDWELLTIME);
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

		/// <summary>
		/// Scintilla supports UTF-8, Japanese, Chinese and Korean DBCS along with single byte encodings like Latin-1. UTF-8 (SC_CP_UTF8) is the default.
		/// Use this message with codePage set to the code page number to set Scintilla to use code page information to ensure multiple byte characters
		/// are treated as one character rather than multiple. This also stops the caret from moving between the bytes in a multi-byte character.
		/// Do not use this message to choose between different single byte character sets - use SCI_STYLESETCHARACTERSET. Call with codePage set
		/// to zero to disable multi-byte support.
		/// Code page SC_CP_UTF8(65001) sets Scintilla into Unicode mode with the document treated as a sequence of characters expressed in UTF-8.
		/// The text is converted to the platform's normal Unicode encoding before being drawn by the OS and thus can display Hebrew, Arabic, Cyrillic,
		/// and Han characters. Languages which can use two characters stacked vertically in one horizontal space, such as Thai, will mostly work but there
		/// are some issues where the characters are drawn separately leading to visual glitches. Bi-directional text is not supported.
		/// Code page can be set to 65001 (UTF-8), 932 (Japanese Shift-JIS), 936 (Simplified Chinese GBK), 949 (Korean Unified Hangul Code),
		/// 950 (Traditional Chinese Big5), or 1361 (Korean Johab).</summary>
		public int CodePage
		{
			get => (int)Cmd(Sci.SCI_GETCODEPAGE);
			set => Cmd(Sci.SCI_SETCODEPAGE, value);
		}

		/// <summary>
		/// Get/Set the characters that are members of the word category. The character categories are set to default values before processing this function.
		/// For example, if you don't allow '_' in your set of characters use: SCI_SETWORDCHARS(0, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
		/// NOTE: For multi-byte encodings, this API will not return meaningful values for 0x80 and above.</summary>
		public string WordChars => Encoding.UTF8.GetString(WordCharsBytes.Where(x => x < 0x80).ToArray());
		public byte[] WordCharsBytes
		{
			get
			{
				// 'Get' fills the characters parameter with all the characters included in words. The characters parameter must be large enough to hold all of the characters.
				// If the characters parameter is 0 then the length that should be allocated to store the entire set is returned.
				var len = Cmd(Sci.SCI_GETWORDCHARS);

				var bytes = new byte[len];
				using var b = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
				len = Cmd(Sci.SCI_GETWORDCHARS, 0, b.Handle.AddrOfPinnedObject());
				return bytes;
			}
			set
			{
				var bytes = value;
				using var b = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
				Cmd(Sci.SCI_SETWORDCHARS, 0, b.Handle.AddrOfPinnedObject());
			}
		}

		/// <summary>
		/// Similar to SCI_SETWORDCHARS, this message allows the user to define which chars Scintilla considers as whitespace. Setting the whitespace chars allows the user
		/// to fine-tune Scintilla's behaviour doing such things as moving the cursor to the start or end of a word; for example, by defining punctuation chars as whitespace,
		/// they will be skipped over when the user presses ctrl+left or ctrl+right. This function should be called after SCI_SETWORDCHARS as it will reset the whitespace
		/// characters to the default set. SCI_GETWHITESPACECHARS behaves similarly to SCI_GETWORDCHARS.</summary>
		public string WhitespaceChars => Encoding.UTF8.GetString(WhitespaceCharsBytes.Where(x => x < 0x80).ToArray());
		public byte[] WhitespaceCharsBytes
		{
			get
			{
				var len = Cmd(Sci.SCI_GETWHITESPACECHARS);

				var bytes = new byte[len];
				using var b = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
				len = Cmd(Sci.SCI_GETWHITESPACECHARS, 0, b.Handle.AddrOfPinnedObject());
				return bytes;
			}
			set
			{
				var bytes = value;
				using var b = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
				Cmd(Sci.SCI_SETWHITESPACECHARS, 0, b.Handle.AddrOfPinnedObject());
			}
		}

		/// <summary>
		/// Similar to SCI_SETWORDCHARS and SCI_SETWHITESPACECHARS, this message allows the user to define which chars Scintilla considers as punctuation. SCI_GETPUNCTUATIONCHARS
		/// behaves similarly to SCI_GETWORDCHARS.</summary>
		public string PunctuationChars => Encoding.UTF8.GetString(PunctuationCharsBytes.Where(x => x < 0x80).ToArray());
		public byte[] PunctuationCharsBytes
		{
			get
			{
				// 'Get' fills the characters parameter with all the characters included in words. The characters parameter must be large enough to hold all of the characters.
				// If the characters parameter is 0 then the length that should be allocated to store the entire set is returned.
				var len = Cmd(Sci.SCI_GETPUNCTUATIONCHARS);

				var bytes = new byte[len];
				using var b = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
				len = Cmd(Sci.SCI_GETPUNCTUATIONCHARS, 0, b.Handle.AddrOfPinnedObject());
				return bytes;
			}
			set
			{
				var bytes = value;
				using var b = GCHandle_.Alloc(bytes, GCHandleType.Pinned);
				Cmd(Sci.SCI_SETPUNCTUATIONCHARS, 0, b.Handle.AddrOfPinnedObject());
			}
		}

		/// <summary>
		/// Use the default sets of word and whitespace characters. This sets whitespace to space, tab and other characters with codes less than 0x20, with word characters
		/// set to alphanumeric and '_'.</summary>
		public void SetCharsDefault()
		{
			Cmd(Sci.SCI_SETCHARSDEFAULT);
		}

		/// <summary>
		/// Scintilla can be told to grab the focus with SCI_GRABFOCUS. This is needed more on GTK where focus handling is more complicated than on Windows.
		/// The internal focus flag can be set with SCI_SETFOCUS. This is used by clients that have complex focus requirements such as having their own window
		/// that gets the real focus but with the need to indicate that Scintilla has the logical focus.</summary>
		public void GrabFocus()
		{
			// Note: don't expose SCI_GETFOCUS/SCI_SETFOCUS, there'll not needed on windows
			Cmd(Sci.SCI_GRABFOCUS);
		}

		/// <summary></summary>
		public bool ReadOnly
		{
			get => Cmd(Sci.SCI_GETREADONLY) != 0;
			set => Cmd(Sci.SCI_SETREADONLY, value ? 1 : 0);
		}

		#endregion
		#region Status/Errors

		/// <summary>
		/// If an error occurs, Scintilla may set an internal error number that can be retrieved with SCI_GETSTATUS.
		/// To clear the error status call SCI_SETSTATUS(0). Status values from 1 to 999 are errors and status SC_STATUS_WARN_START (1000)
		/// and above are warnings. The currently defined statuses are:
		///    SC_STATUS_OK 0 No failures
		///    SC_STATUS_FAILURE 1 Generic failure
		///    SC_STATUS_BADALLOC 2 Memory is exhausted
		///    SC_STATUS_WARN_REGEX 1001 Regular expression is invalid</summary>
		public long Status
		{
			get => Cmd(Sci.SCI_GETSTATUS);
			set => Cmd(Sci.SCI_SETSTATUS, value);
		}

		#endregion
	}
}
