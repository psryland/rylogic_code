using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;
using pr.common;
using pr.extn;
using pr.util;
using pr.win32;

namespace pr.gui
{
	/// <summary>Subclass WinForms RichTextBox to get RICHEDIT5.0 instead of 2.0!</summary>
	public class RichTextBox :System.Windows.Forms.RichTextBox
	{
		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)] private static extern int SendMessage(IntPtr hwnd, uint msg, int wparam, ref Win32.TEXTRANGE lparam);
		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr SendMessage(IntPtr hWnd, Int32 wMsg, Int32 wParam, ref Point lParam);
		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr SendMessage(IntPtr hWnd, Int32 wMsg, Int32 wParam, IntPtr lParam);

		public RichTextBox()
		{
			Win32.SetStyleEx(Handle, Win32.WS_EX_STATICEDGE, false);
			Win32.SetStyleEx(Handle, Win32.WS_EX_CLIENTEDGE, false);
		}

		/// <summary>Try to load RichEdit5.0 if available</summary>
		protected override System.Windows.Forms.CreateParams CreateParams
		{
			get
			{
				var cparams = base.CreateParams; 
				if (Win32.LoadLibrary("msftedit.dll") != IntPtr.Zero)
				{
					cparams.ClassName = "RICHEDIT50W";
				}
				return cparams;
			 }
		}

		/// <summary>Remap border styles</summary>
		protected override void OnBorderStyleChanged(EventArgs e)
		{
			base.OnBorderStyleChanged(e);
			switch (BorderStyle)
			{
			case System.Windows.Forms.BorderStyle.None:
				Win32.SetStyleEx(Handle, Win32.WS_EX_STATICEDGE, false);
				Win32.SetStyleEx(Handle, Win32.WS_EX_CLIENTEDGE, false);
				break;
			case System.Windows.Forms.BorderStyle.FixedSingle:
				Win32.SetStyleEx(Handle, Win32.WS_EX_STATICEDGE, true);
				Win32.SetStyleEx(Handle, Win32.WS_EX_CLIENTEDGE, false);
				break;
			case System.Windows.Forms.BorderStyle.Fixed3D:
				Win32.SetStyleEx(Handle, Win32.WS_EX_STATICEDGE, false);
				Win32.SetStyleEx(Handle, Win32.WS_EX_CLIENTEDGE, true);
				break;
			}
			Invalidate();
		}

		/// <summary>Get text from the rich text box</summary>
		public string GetText(int first = 0, int last = -1)
		{
			var range = new Win32.TEXTRANGE(first, last);
			var len = SendMessage(Handle, Win32.EM_GETTEXTRANGE, 0, ref range);
			var err = Marshal.GetLastWin32Error();
			if (err != 0) Marshal.ThrowExceptionForHR(err);
			return range.text;
		}

		/// <summary>Suspend writing to the undo buffer</summary>
		public Scope SuspendUndo()
		{
			var ole = OleInterface;
			return Scope.Create(
				() => ole.TextDocument.Undo(TomConstants.Suspend, IntPtr.Zero),
				() => ole.TextDocument.Undo(TomConstants.Resume, IntPtr.Zero));
		}

		/// <summary>Get/Set the number of lines of text in the control</summary>
		public int LineCount
		{
			get { return TextLength == 0 ? 0 : base.GetLineFromCharIndex(TextLength) + 1; }
			set
			{
				// Careful with order here. If Text == "\n", then LineCount goes from 0 to 2
				if (value < LineCount)
				{
					var idx = GetFirstCharIndexFromLine(value);
					Select(idx > 0 ? idx - 1 : 0, TextLength);
					SelectedText = string.Empty;
				}
				if (value > LineCount)
				{
					Select(TextLength, 0);
					SelectedText = new string('\n', value - LineCount);
				}
			}
		}

		/// <summary>Return the length of a line in the control</summary>
		public int LineLength(int line, bool include_newline)
		{
			return (int)IndexRangeFromLine(line, include_newline).Size;
		}

		/// <summary>Get/Set the line that SelectionStart is on</summary>
		public int CurrentLineIndex
		{
			get { return base.GetLineFromCharIndex(base.SelectionStart); }
			set { SelectionStart = base.GetFirstCharIndexFromLine(value); }
		}

		/// <summary>Get/Set the caret location</summary>
		public virtual Point CaretLocation
		{
			get { return CaretLocationFromCharIndex(SelectionStart); }
			set
			{
				SelectionStart = CharIndexFromCaretLocation(value);
				SelectionLength = 0;
			}
		}

		/// <summary>Returns the index of the first visible line</summary>
		public int FirstVisibleLineIndex
		{
			get
			{
				var char_index = GetCharIndexFromPosition(Point.Empty);
				return GetLineFromCharIndex(char_index);
			}
			set
			{
				var delta = value - FirstVisibleLineIndex;
				Win32.SendMessage(Handle, Win32.EM_LINESCROLL, 0, delta);
			}
		}

		/// <summary>Returns the number of visible lines in the control. Note: LineCount may be less than this</summary>
		public int VisibleLineCount
		{
			get { return Height / FontHeight + 1; }
		}

		/// <summary>Return the character index range for the given line</summary>
		public Range IndexRangeFromLine(int line, bool include_newline)
		{
			var line_count = LineCount;
			var text_count = TextLength;
			if (line <  0)          return new Range(0,0);
			if (line >= line_count) return new Range(text_count, text_count);
			int idx0 = GetFirstCharIndexFromLine(line);
			int idx1 = line+1 != line_count ? GetFirstCharIndexFromLine(line+1) : text_count;
			if (idx1 > idx0 && line+1 < line_count && !include_newline) --idx1; // RTB always uses \n only for line endings, not \r\n
			return new Range(idx0, idx1);
		}

		/// <summary>Convert a character index into a character location in (columns,lines)</summary>
		public Point CaretLocationFromCharIndex(int idx)
		{
			int row = GetLineFromCharIndex(idx);
			int col = idx - GetFirstCharIndexFromLine(row);
			return new Point(col, row);
		}

		/// <summary>Convert a caret location (columns,lines) into a character index</summary>
		public int CharIndexFromCaretLocation(Point loc)
		{
			if (LineCount == 0) return 0;
			var line = Math.Min(loc.Y, LineCount - 1);
			var idx0 = GetFirstCharIndexFromLine(line);
			var idx1 = line+1 < LineCount ? GetFirstCharIndexFromLine(line+1) : TextLength;
			return idx0 + Math.Min(loc.X, idx1 - idx0);
		}

		/// <summary>Return the index of the line that contains (virtually) 'loc'</summary>
		public int LineIndexFromCaretLocation(Point loc)
		{
			return GetLineFromCharIndex(CharIndexFromCaretLocation(loc));
		}

		/// <summary>Convert a client-space point to a character location in (columns,lines)</summary>
		public Point CaretLocationFromPosition(Point position)
		{
			return CaretLocationFromCharIndex(GetCharIndexFromPosition(position));
		}

		/// <summary>Convert a caret position (columns, lines) into a client space point</summary>
		public Point PositionFromCaretLocation(Point loc)
		{
			return GetPositionFromCharIndex(CharIndexFromCaretLocation(loc));
		}

		/// <summary>Convert a client-space area into a virtual line range</summary>
		public Range ClientAreaToLineRange(Rectangle client_area)
		{
			var l0 = FirstVisibleLineIndex;
			var line0 = l0 + client_area.Top / Font.Height;
			var line1 = l0 + client_area.Bottom / Font.Height + 1;
			return new Range(line0, line1);
		}

		/// <summary>Convert a line range into a client-space area, clipped by ClientRectangle</summary>
		public Rectangle LineRangeToClientArea(Range line_range)
		{
			var l0 = FirstVisibleLineIndex;
			var l1 = l0 + VisibleLineCount;
			if (line_range.End < l0) return Rectangle.Empty;
			if (line_range.Beg > l1) return Rectangle.Empty;
			
			var t = (int)Math.Min(Height, Math.Max(0, (line_range.Beg - l0) * Font.Height));
			var b = (int)Math.Min(Height, Math.Max(0, (line_range.End - l0) * Font.Height));
			return Rectangle.FromLTRB(Left, t, Right, b);
		}

		/// <summary>Access proxy for the text data by line</summary>
		public LinesProxy Line { get { return new LinesProxy(this); } }
		public struct LinesProxy
		{
			private readonly RichTextBox m_rtb;
			private readonly int m_ref_line;
			internal LinesProxy(RichTextBox rtb, int ref_line = 0)
			{
				m_rtb = rtb;
				m_ref_line = ref_line;
			}
			public LineProxy this[int line_index]
			{
				get { return new LineProxy(m_rtb, m_ref_line + line_index); }
			}
		}
		public class LineProxy
		{
			private readonly RichTextBox m_rtb;
			private readonly int m_line_index;
			internal LineProxy(RichTextBox rtb, int line_index)
			{
				if (line_index < 0 || line_index >= rtb.LineCount) throw new IndexOutOfRangeException("Line index out of range");
				m_rtb = rtb;
				m_line_index = line_index;
			}

			/// <summary>Access a line relative to this line</summary>
			public LinesProxy LineRel { get { return new LinesProxy(m_rtb, m_line_index); } }

			/// <summary>Get the character index for the first character on this line</summary>
			public int Beg { get { return (m_beg ?? (m_beg = m_rtb.GetFirstCharIndexFromLine(m_line_index))).Value; } }
			private int? m_beg;

			/// <summary>Get the character index for one-passed the last character on this line. Note: includes the newline character (RTB only uses \n, not \r\n)</summary>
			public int End { get { return (m_end ?? (m_end = m_line_index+1 < m_rtb.LineCount ? m_rtb.GetFirstCharIndexFromLine(m_line_index+1) : m_rtb.TextLength)).Value; } }
			public int EndNoNL { get { return End - (HasNewLine ? 1 : 0); } }
			private int? m_end;

			/// <summary>Get the number of characters on this line</summary>
			public int Count { get { return End - Beg; } }
			public int CountNoNL { get { return Count - (HasNewLine ? 1 : 0); } }

			/// <summary>Get the character index range for this line</summary>
			public Range Range { get { return new Range(Beg, End); } }

			/// <summary>True if this line has a newline character (synonymous with 'is not last line')</summary>
			public bool HasNewLine { get { return m_line_index+1 < m_rtb.LineCount; } }
			public bool IsLastLine { get { return !HasNewLine; } }

			/// <summary>Get/Set the length of the string on this line excluding the newline character</summary>
			public int Length
			{
				get { return CountNoNL; }
				set
				{
					if (value < Length) Erase(value);
					if (value > Length) Insert(Length, new string(' ', value - Length));
				}
			}

			/// <summary>Select text within this line</summary>
			public void Select(int start, int length)
			{
				var count = CountNoNL;
				m_rtb.Select(Beg + Math.Min(start, count), Math.Min(length, Math.Max(0, count - start)));
			}

			/// <summary>Get/Set the selected text (Note: not limited to this line)</summary>
			public string SelectedText
			{
				get { return m_rtb.SelectedText; }
				set
				{
					System.Diagnostics.Debug.Assert(!value.Any(x => x == '\n'), "Don't add text with new line characters to single lines");
					var i = m_rtb.SelectionStart - Beg;
					m_rtb.SelectedText = new string(' ', Math.Max(0, i - CountNoNL)) + value;
					InvalidateCachedLine();
				}
			}

			/// <summary>Get/Set the text on this line</summary>
			public string Text
			{
				// This is a little bit dangerous, because we only invalidate the cached line
				// when we change this line of text. If it gets changed externally this object
				// won't noticed. I could sign up to TextChanged, but that triggers for all lines
				// which is overkill.
				get { return m_cached_text ?? (m_cached_text = m_rtb.GetText(Beg, EndNoNL)); }
				set
				{
					using (m_rtb.SelectionScope())
					{
						Select(0, CountNoNL);
						SelectedText = value;
					}
				}
			}
			public void InvalidateCachedLine(object sender = null, EventArgs args = null)
			{
				m_cached_text = null;
				m_beg = null;
				m_end = null;
			}
			private string m_cached_text;

			/// <summary>
			/// Access characters on this line by index. The line is treated as virtual space.
			/// Reading at i >= (Count - HasNewLine) returns '\0'.
			/// Writing at i >= (Count - HasNewLine) pads with ' '</summary>
			public char this[int i]
			{
				get
				{
					if (i < 0) throw new IndexOutOfRangeException("Character index {0} out of range".Fmt(i));
					if (i >= Count) return '\0';
					return Text[i];
				}
				set
				{
					if (i < 0) throw new IndexOutOfRangeException("Character index {0} out of range".Fmt(i));
					using (m_rtb.SelectionScope())
					{
						var count = CountNoNL;
						Select(i, 1);
						SelectedText = new string(value,1);
					}
				}
			}

			/// <summary>Insert 'text[ofs,ofs+length)' into this line at 'i'</summary>
			public void Insert(int i, IString text, int ofs = 0, int length = int.MaxValue)
			{
				if (i < 0) throw new IndexOutOfRangeException("Character index {0} out of range".Fmt(i));
				length = Math.Min(length, text.Length - ofs);
				using (m_rtb.SelectionScope())
				{
					Select(i, 0);
					SelectedText = text.Substring(ofs, length);
					InvalidateCachedLine();
				}
			}

			/// <summary>Erase [i, i+length) from this line</summary>
			public void Erase(int i, int length = int.MaxValue)
			{
				if (i < 0) throw new IndexOutOfRangeException("Character index {0} out of range".Fmt(i));
				using (m_rtb.SelectionScope())
				{
					Select(i, length);
					SelectedText = string.Empty;
					InvalidateCachedLine();
				}
			}

			/// <summary>Overwrite text in this line with 'text[ofs,ofs+length)' at 'i'</summary>
			public void Replace(int i, IString text, int ofs = 0, int length = int.MaxValue)
			{
				if (i < 0) throw new IndexOutOfRangeException("Character index {0} out of range".Fmt(i));
				length = Math.Min(length, text.Length - ofs);
				using (m_rtb.SelectionScope())
				{
					Select(i, length);
					SelectedText = text.Substring(ofs, length);
					InvalidateCachedLine();
				}
			}
		}

		/// <summary>Render the RichTextBox into an image</summary>
		public static Image Print(RichTextBox ctrl, int width, int height)
		{
			return Print(ctrl, new Bitmap(width, height));
		}
		public static Image Print(RichTextBox ctrl, Image img = null)
		{
			var width = img.Width;
			var height = img.Height;
			using (Graphics g = Graphics.FromImage(img))
			{
				// HorizontalResolution is measured in pix/inch
				float scale;
				scale = (float)(width * 100) / img.HorizontalResolution;
				width = (int)scale;
 
				// VerticalResolution is measured in pix/inch
				scale = (float)(height * 100) / img.VerticalResolution;
				height = (int)scale;
 
				var marginBounds = new Rectangle(0, 0, width, height);
				var pageBounds = new Rectangle(0, 0, width, height);
				var args = new PrintPageEventArgs(g, marginBounds, pageBounds, null);
				Print(ctrl.Handle, 0, ctrl.Text.Length, args);
			}
 
			return img;
		}

		/// <summary>
		/// Render the contents of the RichTextBox for printing.
		/// Return the last character printed + 1 (printing start from this point for next page).
		/// http://support.microsoft.com/default.aspx?scid=kb;en-us;812425
		/// The RichTextBox control does not provide any method to print the content of the RichTextBox.
		/// You can extend the RichTextBox class to use EM_FORMATRANGE message to send the content of
		/// a RichTextBox control to an output device such as printer.</summary>
		public static int Print(IntPtr rtb_handle, int charFrom, int charTo, PrintPageEventArgs e)
		{
			// Convert the unit used by the .NET framework (1/100 inch) 
			// and the unit used by Win32 API calls ('twips' 1/1440 inch)
			const double anInch = 14.4;
 
			// Calculate the area to render and print
			var print_rect = Win32.RECT.FromLTRB(
				(int)(e.MarginBounds.Left   * anInch),
				(int)(e.MarginBounds.Top    * anInch),
				(int)(e.MarginBounds.Right  * anInch),
				(int)(e.MarginBounds.Bottom * anInch));
 
			// Calculate the size of the page
			var page_rect = Win32.RECT.FromLTRB(
				(int)(e.PageBounds.Left   * anInch),
				(int)(e.PageBounds.Top    * anInch),
				(int)(e.PageBounds.Right  * anInch),
				(int)(e.PageBounds.Bottom * anInch));
 
			// Create a format range
			using (var hdc = e.Graphics.GetHdcScope())
			{
				var fmt_range = new Win32.FORMATRANGE
				{
					char_range = new Win32.CHARRANGE
					{
						max = charTo,      // Indicate character from to character to 
						min = charFrom,    // 
					},
					hdc        = hdc,        // Use the same DC for measuring and rendering
					hdcTarget  = hdc,        // Point at printer hDC
					rc         = print_rect, // Indicate the area on page to print
					rcPage     = page_rect,  // Indicate size of page
				};

				// Get the pointer to the FORMATRANGE structure in non-GC memory
				using (var lparam = Marshal_.StructureToPtr(fmt_range))
				{
					// Send the rendered data for printing, then release and cached info
					var res = Win32.SendMessage(rtb_handle, (uint)Win32.EM_FORMATRANGE,  new IntPtr(1), lparam);
					Win32.SendMessage(rtb_handle, (uint)Win32.EM_FORMATRANGE, (IntPtr)0, (IntPtr)0);
					return (int)res; // Return last + 1 character printed
				}
			}
		}

		/// <summary>Get a reference to an OLE interface for the RichEditControl</summary>
		private RichEditOleInterface OleInterface { get { return new RichEditOleInterface(this); } }

		#region IRichEditOle

		// Notes: keep this private so referencing projects don't need to reference 'tom'

		/// <summary>RAII object for the Rich Edit OLE interface</summary>
		private class RichEditOleInterface :IDisposable
		{
			protected IntPtr m_rich_edit_ole;
			protected IntPtr m_text_document;

			internal RichEditOleInterface(RichTextBox rtb)
			{
				m_rich_edit_ole = IntPtr.Zero;
				m_text_document = IntPtr.Zero;

				// Allocate the ptr that EM_GETOLEINTERFACE will fill in.
				using (var ptr = Marshal_.AllocCoTaskMem(typeof(IntPtr), 1))
				{
					//Marshal.WriteIntPtr(ptr, IntPtr.Zero);  // Clear it.
					if (Win32.SendMessage(rtb.Handle, Win32.EM_GETOLEINTERFACE, IntPtr.Zero, ptr) == IntPtr.Zero)
						throw new Exception("RichTextBox.OleInterface - EM_GETOLEINTERFACE failed.");

					// Read the returned pointer.
					using (var pRichEdit = Scope.Create(() => Marshal.ReadIntPtr(ptr), p => Marshal.Release(p)))
					{
						if (pRichEdit == IntPtr.Zero)
							throw new Exception("RichTextBox.OleInterface - failed to get the pointer.");

						{// Query for the IRichEditOle interface.
							var guid = new Guid("00020D00-0000-0000-c000-000000000046");
							Marshal.QueryInterface(pRichEdit, ref guid, out m_rich_edit_ole);
						}
						{// Query for the ITextDocument interface
							var guid = new Guid("8CC497C0-A1DF-11CE-8098-00AA0047BE5D");
							Marshal.QueryInterface(pRichEdit, ref guid, out m_text_document);
						}
					}
				}
			}
			public virtual void Dispose()
			{
				Marshal_.Release(ref m_rich_edit_ole);
				Marshal_.Release(ref m_text_document);
			}

			/// <summary>Get the IRichEditOle interface </summary>
			public IRichEditOle RichEditOle
			{
				get
				{
					var ole = (IRichEditOle)Marshal.GetTypedObjectForIUnknown(m_rich_edit_ole, typeof(IRichEditOle));
					if (ole == null) throw new Exception("RichTextBox.OleInterface.RichEditOle - Failed to get the object wrapper.");
					return ole;
				}
			}

			/// <summary>Get the ITextDocument interface</summary>
			public ITextDocument TextDocument
			{
				get
				{
					var ole = (ITextDocument)Marshal.GetTypedObjectForIUnknown(m_text_document, typeof(ITextDocument));
					if (ole == null) throw new Exception("RichTextBox.OleInterface.TextDocument - Failed to get the object wrapper.");
					return ole;
				}
			}
		}

		private class TomConstants
		{
			public const int Suspend  = -9999995;
			public const int Resume   = -9999994;
		}

		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("00020D00-0000-0000-c000-000000000046")]
		private interface IRichEditOle
		{
			int GetClientSite(IntPtr lplpolesite);
			int GetObjectCount();
			int GetLinkCount();
			int GetObject(int iob, REOBJECT lpreobject, [MarshalAs(UnmanagedType.U4)]GetObjectOptions flags);
			int InsertObject(REOBJECT lpreobject);
			int ConvertObject(int iob, CLSID rclsidNew, string lpstrUserTypeNew);
			int ActivateAs(CLSID rclsid, CLSID rclsidAs);
			int SetHostNames(string lpstrContainerApp, string lpstrContainerObj);
			int SetLinkAvailable(int iob, int fAvailable);
			int SetDvaspect(int iob, uint dvaspect);
			int HandsOffStorage(int iob);
			int SaveCompleted(int iob, IntPtr lpstg);
			int InPlaceDeactivate();
			int ContextSensitiveHelp(int fEnterMode);
		}

		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("8CC497C0-A1DF-11CE-8098-00AA0047BE5D")]
		public interface ITextDocument
		{
			// IDispath methods (We never use them)
			int GetIDsOfNames(Guid riid, IntPtr rgszNames, uint cNames, uint lcid, ref int rgDispId);
			int GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);
			int GetTypeInfoCount(ref uint pctinfo);
			int Invoke(uint dispIdMember, Guid riid, uint lcid, uint wFlags, IntPtr pDispParams, IntPtr pvarResult, IntPtr pExcepInfo, ref uint puArgErr);

			// ITextDocument methods
			int GetName( /* [retval][out] BSTR* */ [In, Out, MarshalAs(UnmanagedType.BStr)] ref string pName);
			int GetSelection( /* [retval][out] ITextSelection** */ IntPtr ppSel);
			int GetStoryCount( /* [retval][out] */ ref int pCount);
			int GetStoryRanges( /* [retval][out] ITextStoryRanges** */ IntPtr ppStories);
			int GetSaved( /* [retval][out] */ ref int pValue);
			int SetSaved( /* [in] */ int Value);
			int GetDefaultTabStop( /* [retval][out] */ ref float pValue);
			int SetDefaultTabStop( /* [in] */ float Value);
			int New();
			int Open( /* [in] VARIANT **/ IntPtr pVar, /* [in] */ int Flags, /* [in] */ int CodePage);
			int Save( /* [in] VARIANT * */ IntPtr pVar, /* [in] */ int Flags, /* [in] */ int CodePage);
			int Freeze( /* [retval][out] */ ref int pCount);
			int Unfreeze( /* [retval][out] */ ref int pCount);
			int BeginEditCollection();
			int EndEditCollection();
			int Undo( /* [in] */ int Count, /* [retval][out] */ ref IntPtr prop);
			int Redo( /* [in] */ int Count, /* [retval][out] */ ref IntPtr prop);
			int Range( /* [in] */ int cp1, /* [in] */ int cp2, /* [retval][out] ITextRange** */ IntPtr ppRange);
			int RangeFromPoint( /* [in] */ int x, /* [in] */ int y, /* [retval][out] ITextRange** */ IntPtr ppRange);
		}
#if false
		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsDual)]
		[Guid("8CC497C2-A1DF-11ce-8098-00AA0047BE5D")]
		public interface ITextRange
		{
	//	string Text { get; }
	//	void Placeholder_set_Text();
	//	void Placeholder_get_Char();
	//	void Placeholder_set_Char();
	//	//void GetDuplicate([MarshalAs(UnmanagedType.Interface)]out ITextRange ppRange);
	//	[return: MarshalAs(UnmanagedType.Interface)]
	//	ITextRange GetDuplicate();
	//	void Placeholder_get_FormattedText();
	//	void Placeholder_set_FormattedText();
	//	int Start { get; set; }
	//	int End { get; set; }
	//	ITextFont Font { get; set; } 
	//	ITextPara Para { get; set; } 
	//	int StoryLength { get; }
	//	TomStory StoryType { get; }
	//	void Collapse(TomStartEnd bStart);
	//	int Expand(TomUnit unit);
	//	void Placeholder_GetIndex();
	//	void Placeholder_SetIndex();
	//	void SetRange(int cp1, int cp2);
	//	TomBool InRange(ITextRange range);
	//	void Placeholder_InStory();
	//	TomBool IsEqual(ITextRange range);
	//	void Select();
	//	int StartOf(int type, int extend);
	//	int EndOf(TomUnit unit, TomExtend extend);
	//	int Move(TomUnit unit, int count);
	//	int MoveStart(TomUnit unit, int count);
	//	int MoveEnd(TomUnit unit, int count);
	//	void Placeholder_MoveWhile();
	//	void Placeholder_MoveStartWhile();
	//	void Placeholder_MoveEndWhile();
	//	void Placeholder_MoveUntil();
	//	void Placeholder_MoveStartUntil();
	//	void Placeholder_MoveEndUntil();
	//	int FindText(string bstr, int count, TomMatch flags);
	//	int FindTextStart(string bstr, int count, TomMatch flags);
	//	int FindTextEnd(string bstr, int count, TomMatch flags);
	//	void Placeholder_Delete();
	//	void Placeholder_Cut();
	//	void Placeholder_Copy();
	//	void Placeholder_Paste();
	//	void Placeholder_CanPaste();
	//	void Placeholder_CanEdit();
	//	void Placeholder_ChangeCase();
	//	[PreserveSig]int GetPoint(TomGetPoint type, out int px, out int py);
	//	void Placeholder_SetPoint();
	//	void ScrollIntoView(TomStartEnd scrollvalue);
	//	[PreserveSig]int GetEmbeddedObject([MarshalAs(UnmanagedType.IUnknown)]out object ppObj);
		}
#endif

		private enum GetObjectOptions
		{
			REO_GETOBJ_NO_INTERFACES    = 0x00000000,
			REO_GETOBJ_POLEOBJ          = 0x00000001,
			REO_GETOBJ_PSTG             = 0x00000002,
			REO_GETOBJ_POLESITE         = 0x00000004,
			REO_GETOBJ_ALL_INTERFACES   = 0x00000007,
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct CLSID
		{
			public int a;
			public short b;
			public short c;
			public byte d;
			public byte e;
			public byte f;
			public byte g;
			public byte h;
			public byte i;
			public byte j;
			public byte k;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct SIZEL
		{
			public int x;
			public int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		private class REOBJECT
		{
			public REOBJECT()
			{
			}

			public int cbStruct = Marshal.SizeOf(typeof(REOBJECT)); // Size of structure
			public int cp = 0;                                      // Character position of object
			public CLSID clsid = new CLSID();                       // Class ID of object
			public IntPtr poleobj = IntPtr.Zero;                    // OLE object interface
			public IntPtr pstg = IntPtr.Zero;                       // Associated storage interface
			public IntPtr polesite = IntPtr.Zero;                   // Associated client site interface
			public SIZEL sizel = new SIZEL();                       // Size of object (may be 0,0)
			public uint dvaspect = 0;                               // Display aspect to use
			public uint dwFlags = 0;                                // Object status flags
			public uint dwUser = 0;                                 // Dword for user's use
		}
		#endregion
	}
}
