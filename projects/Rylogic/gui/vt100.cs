using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace pr.gui

{
	/// <summary>
	/// VT terminal emulation screen buffer
	/// see: http://ascii-table.com/ansi-escape-sequences.php
	/// 
	/// Render the buffer into a window by requesting a rectangular area from the buffer
	/// The buffer is a virtual space of Settings.m_width/m_height area. The virtual space
	/// becomes allocated space when characters are written or style set for the given character
	/// position.
	/// Line endings are not stored
	/// Usage:
	///   Add the control to a form.
	///   Use the 'GetUserInput()' method to retrieve user input (since last called)
	///   Send the user input to the remote device/system
	///   Receive data from the remote device/system
	///   Use 'Output()' to display the terminal output in the control</summary>
	public class VT100_buf
	{
		public enum ENewLineMode { CR, LF, CR_LF, }

		/// <summary>Per character style</summary>
		private struct Style
		{
			public byte m_col; // Fore/back colour (highbright,blue,green,red)
			public byte m_sty; // bold, underline, etc...
		}

		/// <summary>A row of characters making up the line</summary>
		private class Line
		{
			public StringBuilder m_line;
			public List<Style>   m_styl;

			public Line()
			{
				m_line = new StringBuilder();
				m_styl = new List<Style>();
			}

			/// <summary>Length of the line</summary>
			public int size
			{
				get { return m_line.Length; }
			}

			/// <summary>Set the line size</summary>
			public void resize(int newsize, char fill, Style style)
			{
				m_line.Resize(newsize, fill);
				m_styl.Resize(newsize, () => style);
			}

			// Erase a range within the line
			public void erase(int ofs, int count)
			{
				if (ofs >= size) return;
				var len = Math.Min(count, size - ofs);
				m_line.Remove(ofs, len);
				m_styl.RemoveRange(ofs, len);
			}
			
			/// <summary>Write into/over the line from 'ofs'</summary>
			public void write(int ofs, string str, int count, Style style)
			{
				if (size <= ofs + count)
				{
					m_line.Resize(ofs + count);
					m_styl.Resize(ofs + count);
				}
				m_line.Remove(ofs, count);
 				m_line.Insert(ofs, str, 1);
				m_styl.RemoveRange(ofs, count);
				m_styl.InsertRange(ofs, Enumerable.Repeat(style, count));
			}
		}

		/// <summary>The state of the terminal</summary>
		private class State
		{
			public Point pos;
			public Style sty;
			public State()
			{
				pos = Point.Empty;
				sty = new Style();
			}
		}

		/// <summary>Terminal behaviour setting</summary>
		public class Settings
		{
			public Settings()
			{
				LocalEcho = true;
				TabSize = 8;
				TerminalWidth = 100;
				NewlineRecv = ENewLineMode.LF;
				NewlineSend = ENewLineMode.CR;
			}

			/// <summary>Occurs when vt100 settings are changed</summary>
			public event EventHandler SettingsChanged;

			/// <summary>Helper for setting a property and raising the settings changed event</summary>
			private void SetProp<T>(ref T prop, T value)
			{
				if (Equals(prop, value)) return;
				prop = value;
				SettingsChanged.Raise(this);
			}

			/// <summary>True if input characters should be echoed into the screen buffer</summary>
			public bool LocalEcho
			{
				get { return m_local_echo; }
				set { SetProp(ref m_local_echo, value); }
			}
			private bool m_local_echo;

			/// <summary>Get/Set the width of the terminal in columns</summary>
			public int TerminalWidth
			{
				get { return m_terminal_width; }
				set { SetProp(ref m_terminal_width, Math.Max(value,1)); }
			}
			private int m_terminal_width;

			/// <summary>Get/Set the height of the terminal in lines</summary>
			public int TerminalHeight
			{
				get { return m_terminal_height; }
				set { SetProp(ref m_terminal_height, Math.Max(value,1)); }
			}
			private int m_terminal_height;

			/// <summary>Get/Set the tab size in characters</summary>
			public int TabSize
			{
				get { return m_tab_size; }
				set { SetProp(ref m_tab_size, Math.Max(value,1)); }
			}
			private int m_tab_size;

			/// <summary>Get/Set the newline mode for received newlines</summary>
			public ENewLineMode NewlineRecv
			{
				get { return m_newline_recv; }
				set { SetProp(ref m_newline_recv, value); }
			}
			private ENewLineMode m_newline_recv;

			/// <summary>Get/Set the newline mode for sent newlines</summary>
			public ENewLineMode NewlineSend
			{
				get { return m_newline_send; }
				set { SetProp(ref m_newline_send, value); }
			}
			private ENewLineMode m_newline_send;

			public XElement ToXml(XElement node)
			{
				node.Add
				(
					LocalEcho     .ToXml("local_echo"    , false),
					TabSize       .ToXml("tab_size"      , false),
					TerminalWidth .ToXml("terminal_width", false),
					NewlineRecv   .ToXml("newline_recv"  , false),
					NewlineSend   .ToXml("newline_send"  , false)
				);
				return node;
			}
			public void FromXml(XElement node)
			{
				foreach (var n in node.Elements())
				{
					switch (n.Name.LocalName)
					{
					default: break;
					case "local_echo":     LocalEcho     = n.As<bool>(); break;
					case "tab_size":       TabSize       = n.As<int>(); break;
					case "terminal_width": TerminalWidth = n.As<int>(); break;
					case "newline_recv":   NewlineRecv   = n.As<ENewLineMode>(); break;
					case "newline_send":   NewlineSend   = n.As<ENewLineMode>(); break;
					}
				}
			}		
		}

		/// <summary>The terminal screen buffer</summary>
		private List<Line> m_lines;

		/// <summary>User input buffer</summary>
		private readonly StringBuilder m_input;

		/// <summary>The current output caret state</summary>
		private State m_out;
		private State m_saved;

		/// <summary>The current control sequence</summary>
		public readonly StringBuilder m_seq;

		/// <summary>The state of the terminal</summary>
		private readonly State m_state = new State();

		public VT100_buf()
		{
			Settings = new Settings();
			ReadOnly = false;
			m_lines  = new List<Line>();
			m_input  = new StringBuilder(8192);
			m_out    = new State();
			m_state  = null;
			m_seq    = new StringBuilder();
		}

		/// <summary>Terminal settings</summary>
		public Settings Settings { get; private set; }

		/// <summary>Return the number of lines of text in the control</summary>
		public int LineCount
		{
			get { return m_lines.Count; }
		}

		/// <summary>Get/Set whether user input is accepted</summary>
		public bool ReadOnly { get; set; }

		/// <summary>Return the buffered user input</summary>
		public string UserInput()
		{
			return UserInput(true);
		}

		/// <summary>Return the buffered user input. 'clear' resets the buffer</summary>
		public string UserInput(bool clear)
		{
			string user_input = m_input.ToString();
			if (clear) { m_input.Length = 0; }
			return user_input;
		}

		/// <summary>
		/// Add a string to the user input buffer
		/// Returns the number of characters added to the input buffer</summary>
		public int AddInput(string text)
		{
			// If the control is readonly, ignore all input
			if (ReadOnly)
				return 0;

			int count = 0;
			foreach (char c in text)
			{
				// Block input when the input buffer is full
				if (m_input.Length + 2 >= m_input.Capacity)
					return count;

				// Add the user key to the input buffer
				switch (c)
				{
				default:
					m_input.Append(c);
					break;
				case '\r':
					break;
				case '\n':
					switch (Settings.NewlineSend)
					{
					default: throw new ArgumentOutOfRangeException();
					case ENewLineMode.CR:    m_input.Append('\r'); break;
					case ENewLineMode.LF:    m_input.Append('\n'); break;
					case ENewLineMode.CR_LF: m_input.Append("\r\n"); break;
					}
					break;
				}

				++count;
			}
			return count;
		}

		/// <summary>
		/// Writes 'text' into the control at the current position.
		/// Parses the text for vt100 control sequences.</summary>
		public void Output(string text)
		{
			var caret = m_out.pos;
			using (Scope.Create(null, () => m_out.pos = caret))
				ParseOutput(text);
		}

		/// <summary>Clear the entire buffer</summary>
		public void Clear()
		{
			m_lines.Clear();
			m_out.pos = MoveCaret(0,0);
		}

		/// <summary>
		/// Call to read a rectangular area of text from the screen buffer
		/// Note, width is not a parameter, each returned line is a string
		/// up to TerminalWidth - x. Callers can decide the width.</summary>
		public IEnumerable<string> ReadTextArea(int x, int y, int height)
		{
			for (int j = y, jend = y + height; j != jend; ++j)
				yield return LineAt(x, j);
		}

		/// <summary>Parse the vt100 console text in 'text'</summary>
		private void ParseOutput(string text)
		{
			int first = 0, last = 0, text_end = text.Length;
			for (;last != text_end;)
			{
				char c = text[last];
				if (c == (char)Keys.Escape || m_seq.Length != 0)
				{
					if (first != last) Write(text.Substring(first, last - first));
					m_seq.Append(c);
					ParseEscapeSeq();
					first = last + 1;
				}
				else if (c == '\n' || c == '\r')
				{
					string str = text.Substring(first, last - first);
					switch (Settings.NewlineRecv){
						default:throw new ArgumentOutOfRangeException();
						case VT100_bufSettings.ENewLineMode.CR:    str += "\r"; break;
						case VT100_bufSettings.ENewLineMode.LF:    str += "\n"; break;
						case VT100_bufSettings.ENewLineMode.CR_LF: str += "\r\n"; break;
					}
					Write(str);
					if (last+1 != text_end && ((c == '\n' && text[last+1] == '\r') || (c == '\r' && text[last+1] == '\n'))) ++last;
					first = last + 1;
				}
				else if (char.IsControl(c)) // ignore non printable text
				{
					if (first != last) Write(text.Substring(first, last - first));
					if (c == '\b') CursorLocation = MoveCursor(CursorLocation, -1, 0);
					if (c == '\r') CursorLocation = MoveCursor(CursorLocation, -CursorLocation.X, 0);
					if (c == '\t') Write("".PadRight(TabSize - (CursorLocation.X % TabSize), ' '));
					first = last + 1;
				}
			}

			// Print any remaining printable text
			if (first != text_end) Write(text.Substring(first, text_end - first));
		}

		/// <summary>Parses a stream of characters as a vt100 control sequence.</summary>
		private void ParseEscapeSeq()
		{
			if (m_seq.Length <= 1) return;
			switch (m_seq[1])
			{
			default: // unknown escape sequence
				Debug.Assert(false, "Unknown escape sequence found");
				// ReSharper disable HeuristicUnreachableCode
				break;
				// ReSharper restore HeuristicUnreachableCode

			case (char)Keys.Escape: // Double escape characters - reset to new escape sequence
				m_seq.Length = 1;
				return;

			case '[': //Esc[... codes
				ParseEscapeSeq0();
				return;

			case '(': //Esc(... codes
				ParseEscapeSeq1();
				return;

			case ')': //Esc)... codes
				ParseEscapeSeq2();
				return;

			case 'O': //EscO... codes ... I think these are actually responce codes...
				ParseEscapeSeq3();
				return;

			case '#': //Esc#... codes
				ParseEscapeSeq4();
				return;

			case '=': //Esc= Set alternate keypad mode DECKPAM
				break;

			case '>': //Esc> Set numeric keypad mode DECKPNM
				break;

			//Escc	Reset terminal to initial state	RIS

			//EscN	Set single shift 2	SS2
			//EscO	Set single shift 3	SS3

			case 'A': //EscA Move cursor up one line cursorup
				CursorLocation = MoveCursor(CursorLocation, 0,-1);
				break;
			case 'B': //EscB Move cursor down one line cursordn
				CursorLocation = MoveCursor(CursorLocation, 0,+1);
				break;
			case 'C': //EscC Move cursor right one char cursorrt
				CursorLocation = MoveCursor(CursorLocation, +1,0);
				break;
			case 'D': //EscD Move cursor left one char cursorlf
				CursorLocation = MoveCursor(CursorLocation, -1,0);
				break;

			//EscD	Move/scroll window up one line	IND
			//EscM	Move/scroll window down one line	RI
			//EscE	Move to next line	NEL

			case '7': //Esc7 Save cursor position and attributes DECSC
				m_state.m_saved_cursor_location = CursorLocation;
				m_state.m_saved_font = (Font)SelectionFont.Clone();
				break;
			case '8': //Esc8 Restore cursor position and attributes DECSC
				CursorLocation = m_state.m_saved_cursor_location;
				SelectionFont = m_state.m_saved_font;
				break;

			//EscH	Set a tab at the current column	HTS

			//Codes for use in VT52 compatibility mode
			//Esc<	Enter/exit ANSI mode (VT52)	setansi

			//Esc=	Enter alternate keypad mode	altkeypad
			//Esc>	Exit alternate keypad mode	numkeypad

			//EscF	Use special graphics character set	setgr
			//EscG	Use normal US/UK character set	resetgr

			//EscH Move cursor to upper left corner	cursorhome
			//EscLineColumn	Move cursor to v,h location	cursorpos(v,h)
			//EscI	Generate a reverse line-feed	revindex

			//EscK	Erase to end of current line	cleareol
			//EscJ	Erase to end of screen	cleareos

			//EscZ	Identify what the terminal is	ident
			//Esc/Z	Correct response to ident	identresp
			}

			// Escape sequence complete and processed
			m_seq.Length = 0;
		}

		/// <summary>Parse escape codes beginning with "Esc["</summary>
		private void ParseEscapeSeq0()
		{
			switch (m_seq[m_seq.Length-1])
			{
			default: // Incomplete escape sequence
				return;

			case 'A': // Esc[ValueA Move cursor up n lines CUU
				{ int[] n = Params(1, m_seq.ToString(2,m_seq.Length-3)); CursorLocation = MoveCursor(CursorLocation, 0,-Math.Max(n[0],1)); }
				break;
			case 'B': // Esc[ValueB Move cursor down n lines CUD
				{ int[] n = Params(1, m_seq.ToString(2,m_seq.Length-3)); CursorLocation = MoveCursor(CursorLocation, 0,+Math.Max(n[0],1)); }
				break;
			case 'C': // Esc[ValueC Move cursor right n lines CUF
				{ int[] n = Params(1, m_seq.ToString(2,m_seq.Length-3)); CursorLocation = MoveCursor(CursorLocation, +Math.Max(n[0],1),0); }
				break;
			case 'D': // Esc[ValueD Move cursor left n lines CUB
				{ int[] n = Params(1, m_seq.ToString(2,m_seq.Length-3)); CursorLocation = MoveCursor(CursorLocation, -Math.Max(n[0],1),0); }
				break;

			case 'f':
				//Esc[f             Move cursor to upper left corner hvhome
				//Esc[;f            Move cursor to upper left corner hvhome
				//Esc[Line;Columnf  Move cursor to screen location v,h CUP
				{ int[] n = Params(2, m_seq.ToString(2, m_seq.Length-3)); CursorLocation = MoveCursor(n[1], n[0]); }
				break;

			case 'g':
				//Esc[g  Clear a tab at the current column TBC
				//Esc[0g Clear a tab at the current column TBC
				//Esc[3g Clear all tabs TBC
				break;

			case 'H':
				//Esc[H            Move cursor to upper left corner cursorhome
				//Esc[;H           Move cursor to upper left corner cursorhome
				//Esc[Line;ColumnH Move cursor to screen location v,h CUP
				{ int[] n = Params(2, m_seq.ToString(2, m_seq.Length-3)); CursorLocation = MoveCursor(n[1], n[0]); }
				break;

			case 'h':
				switch (m_seq.ToString()){
				case "\u001b[20h": // Esc[20h Set new line mode LMN
					//m_state.m_newline_recv = ENewLineMode.CR_LF;
					//m_state.m_newline_send = ENewLineMode.CR_LF; // ignoring, this control overwrites this
					break;
				//Esc[?1h	Set cursor key to application	DECCKM
				//none	Set ANSI (versus VT52)	DECANM
				//Esc[?3h	Set number of columns to 132	DECCOLM
				//Esc[?4h	Set smooth scrolling	DECSCLM
				//Esc[?5h	Set reverse video on screen	DECSCNM
				//Esc[?6h	Set origin to relative	DECOM
				//Esc[?7h	Set auto-wrap mode	DECAWM
				//Esc[?8h	Set auto-repeat mode	DECARM
				//Esc[?9h	Set interlacing mode	DECINLM
				}break;

			case 'J':
				switch (m_seq.ToString()){
				case "\u001b[J":  // Esc[J  Clear screen from cursor down ED0
				case "\u001b[0J": // Esc[0J Clear screen from cursor down ED0
					SelectionLength = Text.Length - SelectionStart;
					SelectedText = "";
					break;
				case "\u001b[1J":{// Esc[1J Clear screen from cursor up ED1
					int len = SelectionStart;
					SelectionStart = 0;
					SelectionLength = len;
					SelectedText = "";
				}	break;
				case "\u001b[2J": // Esc[2J Clear entire screen ED2
					Clear();
					break;
				}break;

			case 'K':
				switch (m_seq.ToString()){
				case "\u001b[K":  //Esc[K  Clear line from cursor right EL0
				case "\u001b[0K":{//Esc[0K Clear line from cursor right EL0
					Range rg = IndexRangeFromLine(CurrentLine, false);
					SelectionLength = (int)rg.End - SelectionStart;
					SelectedText = "";
					}break;
				case "\u001b[1K":{//Esc[1K Clear line from cursor left EL1
					Range rg = IndexRangeFromLine(CurrentLine, false);
					rg.End = SelectionStart;
					SelectionStart = (int)rg.Begin;
					SelectionLength = (int)rg.Size;
					SelectedText = "";
					}break;
				case "\u001b[2K":{//Esc[2K Clear entire line EL2
					Range rg = IndexRangeFromLine(CurrentLine, false);
					SelectionStart = (int)rg.Begin;
					SelectionLength = (int)rg.Size;
					SelectedText = "";
					}break;
				}break;

			case 'l':
				switch (m_seq.ToString()){
				case "\u001b[20l": // Esc[20l Set line feed mode LMN
					//m_state.m_newline_recv = ENewLineMode.LF;
					//m_state.m_newline_send = ENewLineMode.LF; // ignoring, this control overwrites this
					break;
				//Esc[?1l	Set cursor key to cursor	DECCKM
				//Esc[?2l	Set VT52 (versus ANSI)	DECANM
				//Esc[?3l	Set number of columns to 80	DECCOLM
				//Esc[?4l	Set jump scrolling	DECSCLM
				//Esc[?5l	Set normal video on screen	DECSCNM
				//Esc[?6l	Set origin to absolute	DECOM
				//Esc[?7l	Reset auto-wrap mode	DECAWM
				//Esc[?8l	Reset auto-repeat mode	DECARM
				//Esc[?9l	Reset interlacing mode	DECINLM
				}break;

			case 'm':
				switch (m_seq.ToString()){
				case "\u001b[m":  // Esc[m Turn off character attributes SGR0
				case "\u001b[0m": // Esc[0m Turn off character attributes SGR0
					SelectionFont = Font;
					break;
				case "\u001b[1m": // Esc[1m Turn bold mode on SGR1
					SelectionFont = new Font(SelectionFont, SelectionFont.Style|FontStyle.Bold);
					break;
				case "\u001b[2m": // Esc[2m Turn low intensity mode on SGR2
					break;
				case "\u001b[30m": SelectionColor = Color.Black;   break; // forecolour
				case "\u001b[31m": SelectionColor = Color.Red;     break;
				case "\u001b[32m": SelectionColor = Color.Green;   break;
				case "\u001b[33m": SelectionColor = Color.Yellow;  break;
				case "\u001b[34m": SelectionColor = Color.Blue;    break;
				case "\u001b[35m": SelectionColor = Color.Magenta; break;
				case "\u001b[36m": SelectionColor = Color.Cyan;    break;
				case "\u001b[37m": SelectionColor = Color.White;   break;
				case "\u001b[40m": SelectionBackColor = Color.Black;   break; // background color
				case "\u001b[41m": SelectionBackColor = Color.Red;     break;
				case "\u001b[42m": SelectionBackColor = Color.Green;   break;
				case "\u001b[43m": SelectionBackColor = Color.Yellow;  break;
				case "\u001b[44m": SelectionBackColor = Color.Blue;    break;
				case "\u001b[45m": SelectionBackColor = Color.Magenta; break;
				case "\u001b[46m": SelectionBackColor = Color.Cyan;    break;
				case "\u001b[47m": SelectionBackColor = Color.White;   break;
				case "\u001b[4m": //Esc[4m Turn underline mode on SGR4
					SelectionFont = new Font(SelectionFont, SelectionFont.Style|FontStyle.Underline);
					break;
				//Esc[5m	Turn blinking mode on	SGR5
				//Esc[7m	Turn reverse video on	SGR7
				//Esc[8m	Turn invisible text mode on	SGR8
				}break;

			//Esc[Line;Liner	Set top and bottom lines of a window	DECSTBM

			//Esc5n	Device status report	DSR
			//Esc0n	Response: terminal is OK	DSR
			//Esc3n	Response: terminal is not OK	DSR

			//Esc6n	Get cursor position	DSR
			//EscLine;ColumnR	Response: cursor is at v,h	CPR

			//Esc[c	Identify what terminal type	DA
			//Esc[0c	Identify what terminal type (another)	DA
			//Esc[?1;Value0c	Response: terminal type code n	DA

			//Esc#8	Screen alignment display	DECALN
			//Esc[2;1y	Confidence power up test	DECTST
			//Esc[2;2y	Confidence loopback test	DECTST
			//Esc[2;9y	Repeat power up test	DECTST
			//Esc[2;10y	Repeat loopback test	DECTST

			//Esc[0q	Turn off all four leds	DECLL0
			//Esc[1q	Turn on LED #1	DECLL1
			//Esc[2q	Turn on LED #2	DECLL2
			//Esc[3q	Turn on LED #3	DECLL3
			//Esc[4q	Turn on LED #4	DECLL4

			//Printing:
			//Esc[i	Print Screen	Print the current screen
			//Esc[1i	Print Line	Print the current line
			//Esc[4i	Stop Print Log	Disable log
			//Esc[5i	Start Print Log	Start log; all received text is echoed to a printer
			}

			// Escape sequence complete and processed
			m_seq.Length = 0;
		}

		/// <summary>Parse escape codes beginning with "Esc("</summary>
		private void ParseEscapeSeq1()
		{
			switch (m_seq[m_seq.Length - 1])
			{
			default: // Incomplete escape sequence
				break;//return;
			//Esc(A	Set United Kingdom G0 character set	setukg0
			//Esc(B	Set United States G0 character set	setusg0
			//Esc(0	Set G0 special chars. & line set	setspecg0
			//Esc(1	Set G0 alternate character ROM	setaltg0
			//Esc(2	Set G0 alt char ROM and spec. graphics	setaltspecg0
			}

			// Escape sequence complete and processed
			m_seq.Length = 0;
		}

		/// <summary>Parse escape codes beginning with "Esc)"</summary>
		private void ParseEscapeSeq2()
		{
			switch (m_seq[m_seq.Length - 1])
			{
			default: // Incomplete escape sequence
				break;//return;
			//Esc)A	Set United Kingdom G1 character set	setukg1
			//Esc)B	Set United States G1 character set	setusg1
			//Esc)0	Set G1 special chars. & line set	setspecg1
			//Esc)1	Set G1 alternate character ROM	setaltg1
			//Esc)2	Set G1 alt char ROM and spec. graphics	setaltspecg1
			}

			// Escape sequence complete and processed
			m_seq.Length = 0;
		}

		/// <summary>Parse escape codes beginning with "EscO"</summary>
		private void ParseEscapeSeq3()
		{
			switch (m_seq[m_seq.Length - 1])
			{
			default: // Incomplete escape sequence
				break;//return;

			//VT100 Special Key Codes
			//These are sent from the terminal back to the computer when the particular key is pressed.	Note that the numeric keypad keys send different codes in numeric mode than in alternate mode. See escape codes above to change keypad mode.

			//Function Keys:
			//EscOP	PF1
			//EscOQ	PF2
			//EscOR	PF3
			//EscOS	PF4

			//Arrow Keys:
			//    Reset	Set
			//up	EscA	EscOA
			//down	EscB	EscOB
			//right	EscC	EscOC
			//left	EscD	EscOD

			//Numeric Keypad Keys:
			//EscOp	0
			//EscOq	1
			//EscOr	2
			//EscOs	3
			//EscOt	4
			//EscOu	5
			//EscOv	6
			//EscOw	7
			//EscOx	8
			//EscOy	9
			//EscOm	-(minus)
			//EscOl	,(comma)
			//EscOn	.(period)
			//EscOM	^M
			}

			// Escape sequence complete and processed
			m_seq.Length = 0;
		}

		/// <summary>Parse escape codes beginning with "Esc#"</summary>
		private void ParseEscapeSeq4()
		{
			switch (m_seq[m_seq.Length - 1])
			{
			default: // Incomplete escape sequence
				break;//return;
			//Esc#3	Double-height letters, top half	DECDHL
			//Esc#4	Double-height letters, bottom half	DECDHL
			//Esc#5	Single width, single height letters	DECSWL
			//Esc#6	Double width, single height letters	DECDWL
			}

			// Escape sequence complete and processed
			m_seq.Length = 0;
		}

		/// <summary>Move the cursor to an absolute position</summary>
		private Point MoveCaret(int x, int y)
		{
			if (TextLength == 0) return Point.Empty;
			Point loc = new Point(Maths.Clamp(x, 0, TerminalWidth - 1), Maths.Clamp(y, 0, LineCount - 1));

			// Make sure the line 'loc.Y' is at least 'loc.X' wide
			Range range = IndexRangeFromLine(loc.Y, false);
			if (range.Size < loc.X)
			{
				int pad_count = loc.X - (int)range.Size;
				int restore = SelectionStart;
				if (restore > range.End) restore += pad_count;
				SelectionStart = (int)range.End;
				SelectionLength = 0;
				SelectedText = "".PadRight(pad_count, ' ');
				SelectionStart = restore;
			}
			return loc;
		}

		/// <summary>Move the cursor by a relative offset</summary>
		private Point MoveCursor(Point loc, int dx, int dy)
		{
			return MoveCursor(loc.X + dx, loc.Y + dy);
		}

		/// <summary>Converts a string of the form "param1;param2;param3;...;param4" to an array of integers
		/// Returns at least 'min_param_count' results</summary>
		private static int[] Params(int min_param_count, string param_string)
		{
			string[] str_parms = param_string.Split(';');
			int[] parms = new int[Math.Max(str_parms.Length, min_param_count)];
			for (int i = 0; i != str_parms.Length; ++i) int.TryParse(str_parms[i], out parms[i]);
			return parms;
		}

		/// <summary>A string to display in the console demonstrating/testing features</summary>
		public string TestConsoleString0
		{
			get
			{
				return "[0m"
					+"[31m===============================\n"
					+"[30m     terminal test string      \n"
					+"[31m===============================\n"
					+"[30m"
					+"\n\n Live Actuator Details & Statistics\n"
					+"------------------------------------\n\n"
					+"[34m\t\t   #1\t   #2\t   #3\t   #4\t   #5\t   #6\t   #7\t   #8\t   #9\t   #10\n\n"
					+"[30m[1m[30mActuator Senders\n"
					+"[0mSender Active\n"
					+"Extension (mm)\n"
					+"AS Temperature\n"
					+"AC Temperature\n"
					+"Safe Settings\n"
					+"Velocity\n"
					+"\t\t7";
			}
		}
		public string TestConsoleString1
		{
			get
			{
				return "[6A   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t"
					+"8[5A  30.180  28.300  28.230  27.580   0.740   4.150  25.700  25.300  31.220  30.550"
					+"8[4A   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t"
					+"8[3A   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t"
					+"8[2A   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t"
					+"8[1A   0\t   0\t   0\t   0\t   0\t   0\t   0\t   0\t   0\t   0\t"
					+"8";
			}
		}
	}
}
