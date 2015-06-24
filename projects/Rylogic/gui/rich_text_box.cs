using System;
using System.Drawing;
using pr.common;
using pr.extn;
using pr.win32;

namespace pr.gui
{
	/// <summary>Subclass winforms RichTextBox to get RICHEDIT5.0 instead of 2.0!</summary>
	public class RichTextBox :System.Windows.Forms.RichTextBox
	{
		/// <summary>Return the number of lines of text in the control</summary>
		public int LineCount
		{
			get { return TextLength == 0 ? 0 : base.GetLineFromCharIndex(TextLength) + 1; }
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

		/// <summary>Return the index range for the given line</summary>
		public Range IndexRangeFromLine(int line, bool include_newline)
		{
			if (line <  0)         return new Range(0,0);
			if (line >= LineCount) return new Range(TextLength, TextLength);
			int idx0 = base.GetFirstCharIndexFromLine(line);
			int idx1 = base.GetFirstCharIndexFromLine(line+1);
			if (idx1 > idx0 && !include_newline) --idx1;
			return new Range(idx0, idx1 >= idx0 ? idx1 : TextLength);
		}

		/// <summary>Accessor proxy for the text data by line</summary>
		public LinesProxy Line { get { return new LinesProxy(this); } }
		public class LinesProxy
		{
			private readonly RichTextBox m_rtb;
			internal LinesProxy(RichTextBox rtb) { m_rtb = rtb; }
			public LineProxy this[int line_index] { get { return new LineProxy(m_rtb, line_index); } }
		}
		public class LineProxy
		{
			private readonly RichTextBox m_rtb;
			private readonly int m_line_index;
			internal LineProxy(RichTextBox rtb, int line_index) { m_rtb = rtb; m_line_index = line_index; }

			/// <summary>Get the character index for the first character on this line</summary>
			public long BegL { get { return m_rtb.GetFirstCharIndexFromLine(m_line_index); } }
			public int  Beg  { get { return (int)BegL; } }

			/// <summary>Get the character index for one-passed the last character on this line</summary>
			public long EndL { get { return m_line_index < m_rtb.LineCount ? m_rtb.GetFirstCharIndexFromLine(m_line_index + 1) : m_rtb.TextLength; } }
			public int  End  { get { return (int)EndL; } }

			/// <summary>Get the number of characters on this line</summary>
			public long CountL { get { return EndL - BegL; } }
			public int  Count  { get { return (int)CountL; } }

			/// <summary>Get/Set the text on this line</summary>
			public string Text
			{
				get { return m_rtb.Text.Substring(Beg, Count); }
				set
				{
					using (m_rtb.SelectionScope())
					{
						m_rtb.Select(Beg, Count);
						m_rtb.SelectedText = value;
					}
				}
			}
		}

		/// <summary>Get/Set the caret location</summary>
		public virtual Point CaretLocation
		{
			get
			{
				int idx = SelectionStart;
				int row = GetLineFromCharIndex(idx);
				int col = idx - GetFirstCharIndexFromLine(row);
				return new Point(col, row);
			}
			set
			{
				SelectionStart = GetFirstCharIndexFromLine(value.Y) + value.X;
				SelectionLength = 0;
			}
		}

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
	}
}
