using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;
using pr.win32;

namespace pr.gui
{
	using Timer = System.Windows.Forms.Timer;

	/// <summary>
	/// VT terminal emulation
	/// see: http://ascii-table.com/ansi-escape-sequences.php
	/// </summary>
	public static class VT100
	{
		/// <summary>Terminal behaviour setting</summary>
		[TypeConverter(typeof(Settings.TyConv))]
		public class Settings :SettingsSet<Settings>
		{
			// Some values are cached for performance

			/// <summary>True if input characters should be echoed into the screen buffer</summary>
			public bool LocalEcho
			{
				get { return get(x => x.LocalEcho); }
				set { set(x => x.LocalEcho, value); }
			}

			/// <summary>Get/Set the width of the terminal in columns</summary>
			public int TerminalWidth
			{
				get { return (m_cached_terminal_width ?? (m_cached_terminal_width = get(x => x.TerminalWidth))).Value; }
				set { set(x => x.TerminalWidth, m_cached_terminal_width = value); }
			}
			private int? m_cached_terminal_width;

			/// <summary>Get/Set the height of the terminal in lines</summary>
			public int TerminalHeight
			{
				get { return (m_cached_terminal_height ?? (m_cached_terminal_height = get(x => x.TerminalHeight))).Value; }
				set { set(x => x.TerminalHeight, m_cached_terminal_height = value); }
			}
			private int? m_cached_terminal_height;

			/// <summary>Get/Set the tab size in characters</summary>
			public int TabSize
			{
				get { return (m_cached_tab_size ?? (m_cached_tab_size = get(x => x.TabSize))).Value; }
				set { set(x => x.TabSize, m_cached_tab_size = value); }
			}
			private int? m_cached_tab_size;

			/// <summary>Get/Set the newline mode for received newlines</summary>
			public ENewLineMode NewlineRecv
			{
				get { return get(x => x.NewlineRecv); }
				set { set(x => x.NewlineRecv, value); }
			}

			/// <summary>Get/Set the newline mode for sent newlines</summary>
			public ENewLineMode NewlineSend
			{
				get { return get(x => x.NewlineSend); }
				set { set(x => x.NewlineSend, value); }
			}

			/// <summary>Get/Set the received data being written has hex data into the buffer</summary>
			public bool HexOutput
			{
				get { return get(x => x.HexOutput); }
				set { set(x => x.HexOutput, value); }
			}

			public Settings()
			{
				LocalEcho      = false;
				TabSize        = 4;
				TerminalWidth  = 120;
				TerminalHeight = 256;
				NewlineRecv    = ENewLineMode.LF;
				NewlineSend    = ENewLineMode.CR;
				HexOutput      = false;
			}

			public override void FromXml(XElement node)
			{
				base.FromXml(node);

				// Reset cached values after loading
				m_cached_tab_size = null;
				m_cached_terminal_width = null;
				m_cached_terminal_height = null;
			}

			/// <summary>Type converter for displaying in a property grid</summary>
			private class TyConv :GenericTypeConverter<Settings> {}
		}

		/// <summary>New line modes</summary>
		public enum ENewLineMode { CR, LF, CR_LF, }

		/// <summary>Three bit colours with a bit for "high bright"</summary>
		public static class HBGR
		{
			public const byte Black   = 0;
			public const byte Red     = 1;
			public const byte Green   = 2;
			public const byte Yellow  = 3;
			public const byte Blue    = 4;
			public const byte Magenta = 5;
			public const byte Cyan    = 6;
			public const byte White   = 7;

			/// <summary>Convert an HBGR colour to a Color</summary>
			public static Color ToColor(byte col)
			{
				switch (col)
				{
				default:
				case HBGR.Black      : return Color.Black      ;
				case HBGR.Red        : return Color.DarkRed    ;
				case HBGR.Green      : return Color.DarkGreen  ;
				case HBGR.Yellow     : return Color.Olive      ;
				case HBGR.Blue       : return Color.DarkBlue   ;
				case HBGR.Magenta    : return Color.DarkMagenta;
				case HBGR.Cyan       : return Color.DarkCyan   ;
				case HBGR.White      : return Color.Gray       ;
				case HBGR.Black  |0x8: return Color.Black      ;
				case HBGR.Red    |0x8: return Color.Red        ;
				case HBGR.Green  |0x8: return Color.Green      ;
				case HBGR.Yellow |0x8: return Color.Yellow     ;
				case HBGR.Blue   |0x8: return Color.Blue       ;
				case HBGR.Magenta|0x8: return Color.Magenta    ;
				case HBGR.Cyan   |0x8: return Color.Cyan       ;
				case HBGR.White  |0x8: return Color.White      ;
				}
			}
		}

		/// <summary>Per character style</summary>
		public struct Style
		{
			private struct bits
			{
				public const byte bold    = 1 << 0;
				public const byte uline   = 1 << 1;
				public const byte blink   = 1 << 2;
				public const byte revs    = 1 << 3;
				public const byte conceal = 1 << 4;
			}
			private byte m_col; // Fore/back colour (highbright,blue,green,red)
			private byte m_sty; // bold, underline, etc...
			private bool get(byte b)          { return (m_sty & b) != 0; }
			private void set(byte b, bool on) { if (on) m_sty |= b; else m_sty &= (byte)~b; }
			
			/// <summary>Get/Set 4-bit background colour</summary>
			public byte BackColour
			{
				get { return (byte)((m_col >> 0) & 0xf); }
				set { m_col &= 0xF0; m_col |= (byte)(value << 0); }
			}

			/// <summary>Get/Set 4-bit foreground colour</summary>
			public byte ForeColour
			{
				get { return (byte)((m_col >> 4) & 0xf); }
				set { m_col &= 0x0F; m_col |= (byte)(value << 4); }
			}

			/// <summary>Get/Set build mode</summary>
			public bool Bold
			{
				get { return get(bits.bold); }
				set { set(bits.bold, value); }
			}

			/// <summary>Get/Set underline mode</summary>
			public bool Underline
			{
				get { return get(bits.uline); }
				set { set(bits.uline, value); }
			}

			/// <summary>Get/Set blink mode</summary>
			public bool Blink
			{
				get { return get(bits.blink); }
				set { set(bits.blink, value); }
			}

			/// <summary>Get/Set Reverse video mode</summary>
			public bool RevserseVideo
			{
				get { return get(bits.revs); }
				set { set(bits.revs, value); }
			}

			/// <summary>Get/Set concealed mode</summary>
			public bool Concealed
			{
				get { return get(bits.conceal); }
				set { set(bits.conceal, value); }
			}

			/// <summary>Debug style</summary>
			public static Style Default
			{
				get { return new Style{m_col = 0x8F, m_sty = 0x00}; }
			}

			public bool Equal(Style rhs)
			{
				return m_col == rhs.m_col && m_sty == rhs.m_sty;
			}
			public override bool Equals(object obj)
			{
				return obj is Style && Equal((Style)obj);
			}
			public override int GetHashCode()
			{
				return m_col.GetHashCode() * 137 ^ m_sty.GetHashCode();
			}
			public override string ToString()
			{
				return "col: {0} sty: {1}".Fmt(m_col, m_sty);
			}
		}

		/// <summary>A single character in the terminal</summary>
		public struct Char
		{
			public char  m_char;
			public Style m_styl;
			public Char(char ch, Style sty) { m_char = ch; m_styl = sty; }
		}

		/// <summary>A span of characters all with the same style</summary>
		public struct Span
		{
			public IString m_str;
			public Style  m_sty;
			public Span(IString str, Style sty) { m_str = str; m_sty = sty; }
		}

		/// <summary>A row of characters making up the line</summary>
		public class Line
		{
			public StringBuilder m_line;
			public List<Style>   m_styl;

			public Line()
			{
				m_line = new StringBuilder();
				m_styl = new List<Style>();
			}

			/// <summary>Get/Set a single character + style for this line</summary>
			public Char this[int i]
			{
				get { return new Char(m_line[i], m_styl[i]); }
				set
				{
					if (i >= Size) Resize(i + 1, ' ', m_styl.LastOrDefault());
					Debug.Assert(value.m_char != 0);
					m_line[i] = value.m_char;
					m_styl[i] = value.m_styl;
				}
			}

			/// <summary>Get the contiguous spans of text with the same style</summary>
			public IEnumerable<Span> Spans
			{
				get
				{
					if (Size == 0) yield break;
					for (int s = 0, e = 0, end = Size;;)
					{
						var sty = m_styl[s];
						for (e = s+1; e != end && m_styl[e].Equal(sty); ++e) {}
						yield return new Span(IString.From(m_line, s, e - s), sty);
						if (e == end) yield break;
						s = e;
					}
				}
			}

			/// <summary>Length of the line</summary>
			public int Size
			{
				get { return m_line.Length; }
			}

			/// <summary>Set the line size</summary>
			public void Resize(int newsize, char fill, Style style)
			{
				Debug.Assert(fill != 0);
				m_line.Resize(newsize, fill);
				m_styl.Resize(newsize, () => style);
			}

			// Erase a range within the line
			public void Erase(int ofs, int count)
			{
				if (ofs >= Size) return;
				var len = Math.Min(count, Size - ofs);
				m_line.Remove(ofs, len);
				m_styl.RemoveRange(ofs, len);
			}

			/// <summary>Write 'str[ofs->ofs+count]' into/over the line from 'col'</summary>
			public void Write(int col, string str, int ofs, int count, Style style)
			{
				if (Size < col + count)
					Resize(col + count, ' ', style);

				for (; count-- != 0; ++ofs, ++col)
				{
					Debug.Assert(str[ofs] != 0 && str[ofs] != '\n'); // don't write nulls or newlines into buffer lines
					m_line[col] = str[ofs];
					m_styl[col] = style;
				}
			}

			/// <summary>Return a subsection of this line starting from 'ofs'</summary>
			public string Substr(int ofs)
			{
				return ofs < Size ? m_line.ToString(ofs, Size - ofs) : string.Empty;
			}

			public override string ToString()
			{
				return m_line.ToString();
			}
		}

		/// <summary>Buffer for user input. Separated into a class so Buffers user input and tracks the input caret location</summary>
		public interface IUserInputBuffer
		{
			// Note: all this needs to do is buffer key presses, those characters/keys
			// are sent to the server which will reply causing changes to the buffer.
			// User input should never be rendered directly into the display

			/// <summary>Add a character to the buffer</summary>
			void Add(char ch);

			/// <summary>Return the contents of the buffer, optionally resetting it</summary>
			string Get(bool reset = true);
		}
		private class UserInput :IUserInputBuffer
		{
			private readonly StringBuilder m_buf;

			public UserInput(int capacity = 8192)
			{
				m_buf = capacity != int.MaxValue ? new StringBuilder(capacity) : new StringBuilder();
			}
			public void Add(char ch)
			{
				m_buf.Append(ch);
			}
			public string Get(bool reset)
			{
				var ret = m_buf.ToString();
				if (reset) m_buf.Length = 0;
				return ret;
			}
		}

		/// <summary>
		/// In-memory screen buffer.
		/// Render the buffer into a window by requesting a rectangular area from the buffer
		/// The buffer is a virtual space of Settings.TerminalWidth/TerminalHeight area.
		/// The virtual space becomes allocated space when characters are written or style
		/// set for the given character position. Line endings are not stored.
		/// Usage:
		///   Add the 'Display' control to a form.
		///   Use the 'GetUserInput()' method to retrieve user input (since last called)
		///   Send the user input to the remote device/system
		///   Receive data from the remote device/system
		///   Use 'Output()' to display the terminal output in the control</summary>
		public class Buffer
		{
			/// <summary>The state of the terminal</summary>
			private struct State
			{
				public Point pos;
				public Style style;
				public static State Default
				{
					get { return new State{pos = Point.Empty, style = Style.Default}; }
				}
				public override string ToString()
				{
					return "{0} {1}".Fmt(pos.ToString(), style);
				}
			}

			/// <summary>User input buffer</summary>
			private IUserInputBuffer m_input;

			/// <summary>The current control sequence</summary>
			public readonly StringBuilder m_seq;

			/// <summary>The state of the terminal</summary>
			private readonly State m_state;

			/// <summary>The terminal screen buffer</summary>
			private List<Line> m_lines;

			/// <summary>The current output caret state</summary>
			private State m_out;
			private State m_saved;

			public Buffer(Settings settings)
			{
				Settings = settings ?? new Settings();
				m_input  = new UserInput(8192);
				m_seq    = new StringBuilder();
				m_lines  = new List<Line>();
				m_state  = State.Default;
				m_out    = State.Default;
			}

			/// <summary>Terminal settings</summary>
			public Settings Settings { get; private set; }

			/// <summary>Return the number of lines of text in the control</summary>
			public int LineCount { get { return m_lines.Count; } }

			/// <summary>Access the lines in the buffer</summary>
			public List<Line> Lines { get { return m_lines; } }

			/// <summary>The current position of the output caret</summary>
			public Point CaretPos { get { return m_out.pos; } }

			/// <summary>User input buffer</summary>
			public IUserInputBuffer UserInput
			{
				get { return m_input; }
				set
				{
					if (m_input == value) return;
					if (m_input != null) m_input.Get(true).ForEach(value.Add);
					m_input = value;
				}
			}

			/// <summary>Add a single character to the user input buffer.</summary>
			public void AddInput(char c, bool notify = true)
			{
				if (c == '\n')
				{
					switch (Settings.NewlineSend)
					{
					case ENewLineMode.LF:    m_input.Add('\n'); break;
					case ENewLineMode.CR:    m_input.Add('\r'); break;
					case ENewLineMode.CR_LF: "\r\n".ForEach(m_input.Add); break;
					}
				}
				else
				{
					m_input.Add(c);
				}

				// Notify that input data was added
				if (notify)
					OnInputAdded();
			}

			/// <summary>Add a string to the user input buffer.</summary>
			public void AddInput(string text, bool notify = true)
			{
				foreach (var c in text)
					AddInput(c, false);

				// Notify that input data was added
				if (notify)
					OnInputAdded();
			}

			/// <summary>Add a control character to the user input buffer. Returns true if the key was used</summary>
			public bool AddInput(Keys vk)
			{
				switch (vk)
				{
				default:
					return false;

				case Keys.Left:
				case Keys.Right:
				case Keys.Up:
				case Keys.Down:
					return true; // ToDo: Send escape sequences for cursor control?

				case Keys.Back:   AddInput('\b'); return true;
				case Keys.Delete: AddInput(" \b"); return true;
				case Keys.Return: AddInput('\n');  return true;
				case Keys.Tab:    AddInput('\t');  return true;
				}
			}

			/// <summary>Raised when the user input buffer has data added</summary>
			public event EventHandler InputAdded;
			protected virtual void OnInputAdded()
			{
				InputAdded.Raise(this);
			}

			/// <summary>
			/// Writes 'text' into the control at the current position.
			/// Parses the text for vt100 control sequences.</summary>
			public void Output(string text)
			{
				// Guard against multiple threads calling this method
				if (m_output_thread == null) m_output_thread = Thread.CurrentThread.ManagedThreadId;
				if (m_output_thread != null && m_output_thread.Value != Thread.CurrentThread.ManagedThreadId)
					throw new Exception("Cross thread call to VT100.Buffer.Output");

				if (m_parsing)
					throw new Exception("Re-entrant call to 'Output'");

				using (Scope.Create(() => m_parsing = true, () => m_parsing = false))
				{
					if (Settings.HexOutput)
						OutputHex(text);
					else
						ParseOutput(text);
				}
			}
			private int? m_output_thread;
			private bool m_parsing;

			/// <summary>Clear the entire buffer</summary>
			public void Clear()
			{
				m_lines.Clear();
				m_out.pos = MoveCaret(0,0);
				OnBufferChanged(new BufferChangedEventArgs(Point.Empty, new Size(Settings.TerminalWidth, Settings.TerminalHeight)));
			}

			/// <summary>Add the clipboard contents to the input buffer (if text)</summary>
			public void Paste()
			{
				if (Clipboard.ContainsText())
					AddInput(Clipboard.GetText());
			}

			/// <summary>
			/// Call to read a rectangular area of text from the screen buffer
			/// Note, width is not a parameter, each returned line is a string
			/// up to TerminalWidth - x. Callers can decide the width.</summary>
			public IEnumerable<string> ReadTextArea(int x, int y, int height)
			{
				for (int j = y, jend = y + height; j != jend; ++j)
				{
					if (j == m_lines.Count) yield break;
					var line = LineAt(j);
					yield return line.Substr(x);
				}
			}

			/// <summary>Raised whenever the buffer changes</summary>
			public event EventHandler<BufferChangedEventArgs> BufferChanged;
			public class BufferChangedEventArgs :EventArgs
			{
				public BufferChangedEventArgs(Rectangle area)
				{
					Area = area;
				}
				public BufferChangedEventArgs(Point pt, Size sz)
					:this(new Rectangle(pt, sz))
				{}
				public BufferChangedEventArgs(int x, int y, int count)
					:this(new Rectangle(x, y, count, 1))
				{}
				public BufferChangedEventArgs(Point pt, int count)
					:this(pt.X, pt.Y, count)
				{}

				/// <summary>The rectangular area that has changed</summary>
				public Rectangle Area { get; private set; }
			}
			protected virtual void OnBufferChanged(BufferChangedEventArgs args)
			{
				BufferChanged.Raise(this, args);
			}

			/// <summary>Raised just before and just after buffer lines are dropped due to reaching the maximum Y-size</summary>
			public event EventHandler<OverflowEventArgs> Overflow;
			public class OverflowEventArgs :EventArgs
			{
				public OverflowEventArgs(bool before, Range dropped)
				{
					Before = before;
					Dropped = dropped;
				}

				/// <summary>True if the event is just before lines are dropped, false if just after</summary>
				public bool Before { get; private set; }

				/// <summary>The index range of lines that are dropped</summary>
				public Range Dropped { get; private set; }
			}
			protected virtual void OnOverflow(OverflowEventArgs args)
			{
				Overflow.Raise(this, args);
			}

			/// <summary>Return the line at 'y', allocating if needed</summary>
			private Line LineAt(int y)
			{
				if (m_lines.Count <= y) m_lines.Resize(y + 1, () => new Line());
				return m_lines[y];
			}

			/// <summary>Output the string 'text' as hex</summary>
			private void OutputHex(string text)
			{
				var buf = Encoding.UTF8.GetBytes(text);
				var hex = new StringBuilder(3 * 16 + 2);
				var str = new StringBuilder(16 + 2);
				int i = 0;
				foreach (byte b in buf)
				{
					char c = char.IsControl((char)b) ? '.' : (char)b;
					hex.AppendFormat("{0:x2} ", b);
					str.Append(c);
					if (++i == 16) 
					{
						Write(hex.Append(" | ").Append(str).ToString());
						m_out.pos = MoveCaret(m_out.pos, -m_out.pos.X, 1);
						hex.Length = 0;
						str.Length = 0;
						i = 0;
					}
				}
				if (i != 0)
				{
					Write(hex.Append(' ', 3*(16-i)).Append(" | ").Append(str).ToString());
					m_out.pos = MoveCaret(m_out.pos, -m_out.pos.X, 1);
				}
			}

			/// <summary>Parse the vt100 console text in 'text'</summary>
			private void ParseOutput(string text)
			{
				int s = 0, e = 0, eend = text.Length;
				for (;e != eend;)
				{
					char c = text[e];
					if (c == (char)Keys.Escape || m_seq.Length != 0)
					{
						Write(text, s, e - s);
						m_seq.Append(c);
						ParseEscapeSeq();
						++e;
						s = e;
					}
					else if (
						(c == '\n' && Settings.NewlineRecv == ENewLineMode.LF) ||
						(c == '\r' && Settings.NewlineRecv == ENewLineMode.CR) ||
						(e+1 != eend && text[e] == '\r' && text[e+1] == '\n' && Settings.NewlineRecv == ENewLineMode.CR_LF))
					{
						Write(text, s, e - s);
						m_out.pos = MoveCaret(m_out.pos, -m_out.pos.X, 1);
						e += Settings.NewlineRecv == ENewLineMode.CR_LF ? 2 : 1;
						s = e;
					}
					else if (char.IsControl(c))
					{
						Write(text, s, e - s);
						if (c == '\b') BackSpace();
						if (c == '\r') m_out.pos = MoveCaret(m_out.pos, -m_out.pos.X, 0);
						if (c == '\n') m_out.pos = MoveCaret(m_out.pos, 0, 1);
						if (c == '\t') Write(new string(' ', Settings.TabSize - (m_out.pos.X % Settings.TabSize)));
						++e;
						s = e;
					}
					else
					{
						++e;
					}
				}

				// Print any remaining printable text
				if (s != eend)
					Write(text, s, eend - s);
			}

			/// <summary>Parses a stream of characters as a vt100 control sequence.</summary>
			private void ParseEscapeSeq()
			{
				if (m_seq.Length <= 1)
					return;

				// Return early if the escape sequence is incomplete
				switch (m_seq[1])
				{
				default:
					// unknown escape sequence
					Debug.WriteLine("Unsupported VT100 escape sequence: {0}".Fmt(m_seq.ToString()));
					break;

				case (char)Keys.Escape: // Double escape characters - reset to new escape sequence
					m_seq.Length = 1;
					return;

				case '[': //Esc[... codes
					#region Esc[...
					{
						if (!char.IsLetter(m_seq.Back())) return; // Incomplete sequence
						var code = m_seq.Back();
						switch (code)
						{
						default:
							Debug.WriteLine("Unsupported VT100 escape sequence: {0}".Fmt(m_seq.ToString()));
							break;

						case 'A': 
							#region Esc[ValueA - Move cursor up n lines CUU
							{
								var n = Params(1, m_seq, 2, -1);
								m_out.pos = MoveCaret(m_out.pos, 0, -Math.Max(n[0], 1));
								break;
							}
							#endregion

						case 'B':
							#region Esc[ValueB - Move cursor down n lines CUD
							{
								var n = Params(1, m_seq, 2, -1);
								m_out.pos = MoveCaret(m_out.pos, 0, +Math.Max(n[0], 1));
								break;
							}
							#endregion

						case 'C':
							#region Esc[ValueC - Move cursor right n lines CUF
							{
								var n = Params(1, m_seq, 2, -1);
								m_out.pos = MoveCaret(m_out.pos, +Math.Max(n[0],1), 0);
								break;
							}
							#endregion

						case 'D':
							#region Esc[ValueD - Move cursor left n lines CUB
							{
								var n = Params(1, m_seq, 2, -1);
								m_out.pos = MoveCaret(m_out.pos, -Math.Max(n[0],1),0);
								break;
							}
							#endregion

						case 'f':
							#region Esc[line;columnf - Move cursor to screen location
							{
								//Esc[f             Move cursor to upper left corner hvhome
								//Esc[;f            Move cursor to upper left corner hvhome
								//Esc[Line;Columnf  Move cursor to screen location v,h CUP
								var n = Params(2, m_seq, 2, -1);
								m_out.pos = MoveCaret(n[1], n[0]);
								break;
							}
							#endregion

						case 'g':
							#region Esc[#g - Clear tabs
							{
								//Esc[g  Clear a tab at the current column TBC
								//Esc[0g Clear a tab at the current column TBC
								//Esc[3g Clear all tabs TBC
								break;
							}
							#endregion

						case 'H':
							#region Esc[line;columnH - Move cursor to screen location
							{
								//Esc[H            Move cursor to upper left corner cursorhome
								//Esc[;H           Move cursor to upper left corner cursorhome
								//Esc[Line;ColumnH Move cursor to screen location v,h CUP
								var n = Params(2, m_seq, 2, -1);
								m_out.pos = MoveCaret(n[1], n[0]);
								break;
							}
							#endregion

						case 'h':
							#region Esc[?#h - Set screen modes
							{
								var n = Params(1, m_seq, 2, -1);
								switch (n[0])
								{
								case 20: // Esc[20h Set new line mode LMN
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
								}
								break;
							}
							#endregion

						case 'J':
							#region Esc[#J - Clear screen
							{
								var n = Params(1, m_seq, 2, -1);
								switch (n[0])
								{
								case 0: 
									// Esc[J  Clear screen from cursor down ED0
									// Esc[0J Clear screen from cursor down ED0
									if (m_out.pos.Y < m_lines.Count)
									{
										m_lines.Resize(m_out.pos.Y + 1, () => new Line());
										LineAt(m_out.pos.Y).Resize(m_out.pos.X, '\0', m_out.style);
									}
									break;
								case 1:
									// Esc[1J Clear screen from cursor up ED1
									{
										var num = Math.Min(m_lines.Count, m_out.pos.Y);
										m_lines.RemoveRange(0, num);
										if (m_lines.Count != 0)
											LineAt(0).Erase(0, m_out.pos.X);
									}
									break;
								case 2:
									// Esc[2J Clear entire screen ED2
									Clear();
									break;
								}
								break;
							}
							#endregion

						case 'K':
							#region Esc[#K - Clear line
							{
								var n = Params(1, m_seq, 2, -1);
								switch (n[0])
								{
								case 0:
									//Esc[K  Clear line from cursor right EL0
									//Esc[0K Clear line from cursor right EL0
									if (m_out.pos.Y < m_lines.Count)
									{
										LineAt(m_out.pos.Y).Resize(m_out.pos.X, '\0', m_out.style);
									}
									break;
								case 1:
									//Esc[1K Clear line from cursor left EL1
									if (m_out.pos.Y < m_lines.Count)
									{
										LineAt(m_out.pos.Y).Erase(0, m_out.pos.X);
									}
									break;
								case 2:
									//Esc[2K Clear entire line EL2
									if (m_out.pos.Y < m_lines.Count)
									{
										LineAt(m_out.pos.Y).Resize(0, '\0', m_out.style);
									}
									break;
								}
								break;
							}
							#endregion

						case 'l':
							#region Esc[?#l - Reset screen modes
							{
								var n = Params(1, m_seq, 2, -1);
								switch (n[0])
								{
								case 20: // Esc[20l Set line feed mode LMN
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
								}
								break;
							}
							#endregion

						case 'm':
							#region Esc[#m - Text mode
							{
								var modes = Params(1, m_seq, 2, -1);
								foreach (var m in modes)
								{
									switch (m)
									{
									case 0:
										// Esc[m Turn off character attributes SGR0
										// Esc[0m Turn off character attributes SGR0
										m_out.style = Style.Default;
										break;
									case 1:
										// Esc[1m Turn bold mode on SGR1
										m_out.style.Bold = true;
										break;
									case 2:
										// Esc[2m Turn low intensity mode on SGR2
										m_out.style.ForeColour = (byte)(m_out.style.ForeColour & 0x7);
										break;
									case 4: //Esc[4m Turn underline mode on SGR4
										m_out.style.Underline = true;
										break;
									case 5: //Esc[5m Turn blinking mode on SGR5
										m_out.style.Blink = true;
										break;
									case 7: //Esc[7m Turn reverse video on SGR7
										m_out.style.RevserseVideo = true;
										break;
									case 8: //Esc[8m Turn invisible text mode on SGR8
										m_out.style.Concealed = true;
										break;

									case 30: m_out.style.ForeColour = (byte)(0x8 | HBGR.Black  ); break; // forecolour
									case 31: m_out.style.ForeColour = (byte)(0x8 | HBGR.Red    ); break;
									case 32: m_out.style.ForeColour = (byte)(0x8 | HBGR.Green  ); break;
									case 33: m_out.style.ForeColour = (byte)(0x8 | HBGR.Yellow ); break;
									case 34: m_out.style.ForeColour = (byte)(0x8 | HBGR.Blue   ); break;
									case 35: m_out.style.ForeColour = (byte)(0x8 | HBGR.Magenta); break;
									case 36: m_out.style.ForeColour = (byte)(0x8 | HBGR.Cyan   ); break;
									case 37: m_out.style.ForeColour = (byte)(0x8 | HBGR.White  ); break;

									case 40: m_out.style.BackColour = (byte)(0x8 | HBGR.Black  ); break; // background color
									case 41: m_out.style.BackColour = (byte)(0x8 | HBGR.Red    ); break;
									case 42: m_out.style.BackColour = (byte)(0x8 | HBGR.Green  ); break;
									case 43: m_out.style.BackColour = (byte)(0x8 | HBGR.Yellow ); break;
									case 44: m_out.style.BackColour = (byte)(0x8 | HBGR.Blue   ); break;
									case 45: m_out.style.BackColour = (byte)(0x8 | HBGR.Magenta); break;
									case 46: m_out.style.BackColour = (byte)(0x8 | HBGR.Cyan   ); break;
									case 47: m_out.style.BackColour = (byte)(0x8 | HBGR.White  ); break;
									}
								}
								break;
							}
							#endregion

						#region Others
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
						#endregion
						}
						break;
					}
					#endregion

				case '(': //Esc(... codes
					#region Esc(
					{
						if (m_seq.Length < 3) return; // Incomplete sequence
						var code = m_seq.Back();
						switch (code)
						{
						default:
							Debug.WriteLine("Unsupported VT100 escape sequence: {0}".Fmt(m_seq.ToString()));
							break;
						//Esc(A	Set United Kingdom G0 character set	setukg0
						//Esc(B	Set United States G0 character set	setusg0
						//Esc(0	Set G0 special chars. & line set	setspecg0
						//Esc(1	Set G0 alternate character ROM	setaltg0
						//Esc(2	Set G0 alt char ROM and spec. graphics	setaltspecg0
						}
						break;
					}
					#endregion

				case ')': //Esc)... codes
					#region Esc)
					{
						if (m_seq.Length < 3) return; // Incomplete sequence
						var code = m_seq.Back();
						switch (code)
						{
						default:
							Debug.WriteLine("Unsupported VT100 escape sequence: {0}".Fmt(m_seq.ToString()));
							break;
						//Esc)A	Set United Kingdom G1 character set	setukg1
						//Esc)B	Set United States G1 character set	setusg1
						//Esc)0	Set G1 special chars. & line set	setspecg1
						//Esc)1	Set G1 alternate character ROM	setaltg1
						//Esc)2	Set G1 alt char ROM and spec. graphics	setaltspecg1
						}
						break;
					}
					#endregion

				case 'O': //EscO... codes ... I think these are actually responce codes...
					#region Esc0
					{
						if (m_seq.Length < 3) return; // Incomplete sequence
						var code = m_seq.Back();
						switch (code)
						{
						default:
							Debug.WriteLine("Unsupported VT100 escape sequence: {0}".Fmt(m_seq.ToString()));
							break;

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
						break;
					}
					#endregion

				case '#': //Esc#... codes
					#region Esc#
					{
						if (m_seq.Length < 3) return; // Incomplete sequence
						var code = m_seq.Back();
						switch (code)
						{
						default:
							Debug.WriteLine("Unsupported VT100 escape sequence: {0}".Fmt(m_seq.ToString()));
							break;

						//Esc#3	Double-height letters, top half	DECDHL
						//Esc#4	Double-height letters, bottom half	DECDHL
						//Esc#5	Single width, single height letters	DECSWL
						//Esc#6	Double width, single height letters	DECDWL
						}
						break;
					}
					#endregion

				case '=': //Esc= Set alternate keypad mode DECKPAM
					break;

				case '>': //Esc> Set numeric keypad mode DECKPNM
					break;

				case 'A': //EscA Move cursor up one line cursorup
					m_out.pos = MoveCaret(m_out.pos, 0,-1);
					break;

				case 'B': //EscB Move cursor down one line cursordn
					m_out.pos = MoveCaret(m_out.pos, 0,+1);
					break;

				case 'C': //EscC Move cursor right one char cursorrt
					m_out.pos = MoveCaret(m_out.pos, +1,0);
					break;

				case 'D': //EscD Move cursor left one char cursorlf
					m_out.pos = MoveCaret(m_out.pos, -1,0);
					break;

				case '7': //Esc7 Save cursor position and attributes DECSC
					m_saved = m_out;
					break;

				case '8': //Esc8 Restore cursor position and attributes DECSC
					m_out = m_saved;
					break;

				#region Others
				//Escc	Reset terminal to initial state	RIS
				//EscN	Set single shift 2	SS2
				//EscO	Set single shift 3	SS3

				//EscD	Move/scroll window up one line	IND
				//EscM	Move/scroll window down one line	RI
				//EscE	Move to next line	NEL

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
				#endregion
				}

				// Escape sequence complete and processed
				m_seq.Length = 0;
			}

			/// <summary>
			/// Converts a string of the form "param1;param2;param3;...;param4"
			/// to an array of integers. Returns at least 'N' results.
			/// If 'end' is negative, it's interpretted as 'param_string.Length - abs(end)'</summary>
			private static List<int> Params(int N, StringBuilder param_string, int beg, int end)
			{
				if (end < 0)
					end = param_string.Length + end;

				m_parms.Clear();
				for (int e, s = beg;;)
				{
					// Find the end of the next parameter
					for (e = s; e != end && param_string[e] != ';'; ++e) {}

					// Add the parameter
					int value;
					m_parms.Add(int.TryParse(param_string.ToString(s, e - s), out value) ? value : 0);

					if (e == end) break;
					s = e + 1;
				}
				for (;m_parms.Count < N;)
				{
					m_parms.Add(0);
				}
				return m_parms;
			}
			private static List<int> m_parms = new List<int>(); // Cached to reduce allocations
			
			/// <summary>Move the cursor to an absolute position</summary>
			private Point MoveCaret(int x, int y)
			{
				// Roll the buffer when y >= terminal height
				var half = Settings.TerminalHeight / 2;
				if (y >= Settings.TerminalHeight + half)
				{
					var count = half;
					OnOverflow(new OverflowEventArgs(true, new Range(0, 0 + count)));

					// Remove the lines and adjust anything that stores a line number
					m_lines.RemoveRange(0, count);
					y -= count;
					m_out.pos.Y -= count;
					m_saved.pos.Y -= count;

					OnOverflow(new OverflowEventArgs(false, new Range(0, 0 + count)));
				}

				var loc = new Point(Maths.Clamp(x, 0, Settings.TerminalWidth), Maths.Clamp(y, 0, Settings.TerminalHeight));
				return loc;
			}

			/// <summary>Move the cursor by a relative offset</summary>
			private Point MoveCaret(Point loc, int dx, int dy)
			{
				return MoveCaret(loc.X + dx, loc.Y + dy);
			}

			/// <summary>
			/// Write 'str' into the screen buffer at 'm_out.pos'.
			/// Writes up to 'TerminalWidth - m_out.pos.X' or 'count' characters, whichever is less.
			/// 'str' should not contain any non-printable characters (including \n,\r). These are removed by ParseOutput</summary>
			private void Write(string str, int ofs = 0, int count = int.MaxValue)
			{
				if (m_out.pos.X < 0 || m_out.pos.X > Settings.TerminalWidth) throw new Exception("Caret outside screen buffer");
				if (m_out.pos.Y < 0 || m_out.pos.Y > Settings.TerminalHeight) throw new Exception("Caret outside screen buffer");
				if (count == 0) return;

				// Limit 'count' to the size of the terminal and the maximum string length
				count = Math.Min(count, str.Length - ofs);
				var clipped = count > Settings.TerminalWidth - m_out.pos.X;
				count = Math.Min(count, Settings.TerminalWidth - m_out.pos.X);

				// Get the line and ensure it's large enough
				var line = LineAt(m_out.pos.Y);
				if (line.Size < m_out.pos.X + count)
					line.Resize(m_out.pos.X + count, ' ', m_out.style);

				// Write the string
				line.Write(m_out.pos.X, str, ofs, count, m_out.style);
				if (clipped)
					line.Write(Settings.TerminalWidth - 1, new string('~',1), 0, 1, m_out.style);

				// Notify whenever the buffer is changed
				OnBufferChanged(new BufferChangedEventArgs(m_out.pos, count));
				m_out.pos = MoveCaret(m_out.pos, count, 0);
			}

			/// <summary>Process a backspace character</summary>
			private void BackSpace()
			{
				if (m_out.pos.X < 0 || m_out.pos.X > Settings.TerminalWidth) throw new Exception("Caret outside screen buffer");
				if (m_out.pos.Y < 0 || m_out.pos.Y > Settings.TerminalHeight) throw new Exception("Caret outside screen buffer");
				if (m_out.pos.X == 0) return;

				m_out.pos = MoveCaret(m_out.pos, -1, 0);

				// Get the line
				var line = LineAt(m_out.pos.Y);
				line.Erase(m_out.pos.X, 1);

				// Notify whenever the buffer is changed
				OnBufferChanged(new BufferChangedEventArgs(m_out.pos, line.Size - m_out.pos.X));
			}

			/// <summary>Determines the terminal screen area used by 'str'</summary>
			public static Size AreaOf(IString str)
			{
				int x = 0, y = 0, mx = 0;
				foreach (var c in str)
				{
					if (c == '\n') { ++y; x = 0; }
					else mx = Math.Max(mx, ++x);
				}
				return new Size(mx, y);
			}

			/// <summary>A string to display in the console demonstrating/testing features</summary>
			public string TestConsoleString0
			{
				get
				{
					// note: expects a tab size of 8
					return "[2J[0m"
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

		/// <summary>A control that displays the VT100 buffer</summary>
		public class Display :ScintillaCtrl
		{
			/// <summary>Helper for sending text to scintilla</summary>
			private class CellBuf
			{
				private Sci.Cell[] m_text; // Buffer used to pass the text to scintilla
				private int m_len;     // The number of valid chars in 'm_text'

				public CellBuf()
				{
					m_text = new Sci.Cell[1024];
					m_len = 0;
				}
				public void Reset()
				{
					m_len = 0;
				}
				public int SizeInBytes
				{
					get { return m_len * R<Sci.Cell>.SizeOf; }
				}
				public void Add(byte ch, byte st)
				{
					if (m_len == m_text.Length)
						Array.Resize(ref m_text, m_text.Length * 3/2);

					m_text[m_len] = new Sci.Cell(ch, st);
					++m_len;
				}
				public void Pop(int n)
				{
					m_len -= Math.Min(n, m_len);
				}
				public GCHandleScope Pin()
				{
					return GCHandleEx.Alloc(m_text, GCHandleType.Pinned);
				}
			}

			private HoverScroll m_hs;
			private EventBatcher m_eb;
			private Dictionary<Style, byte> m_sty; // map from vt100 style to scintilla style index
			private CellBuf m_cells;
			private ContextMenuStrip m_cmenu;

			public Display()
			{
				m_hs = new HoverScroll(HostedCtrl.Handle);
				m_eb = new EventBatcher(UpdateText, TimeSpan.FromMilliseconds(10)){TriggerOnFirst = true};
				m_sty = new Dictionary<Style,byte>();
				m_cells = new CellBuf();
				m_cmenu = new CMenu(this);

				BlinkTimer = new Timer{Interval = 1000, Enabled = false};
				BlinkTimer.Tick += SignalRefresh;

				AutoScrollToBottom = true;

				// Use our own context menu
				Cmd(Sci.SCI_USEPOPUP, 0, 0);

				// Allow scrolling up to one page past the last line
				Cmd(Sci.SCI_SETENDATLASTLINE, 0, 0);
			}
			protected override void Dispose(bool disposing)
			{
				Util.Dispose(ref m_hs);
				Util.Dispose(ref m_eb);
				base.Dispose(disposing);
			}

			/// <summary>Return the vt100 settings</summary>
			[Browsable(false)]
			public Settings Settings
			{
				get { return Buffer != null ? Buffer.Settings : null; }
			}

			/// <summary>Get/Set the underlying VT100 buffer</summary>
			[Browsable(false)]
			public Buffer Buffer
			{
				get { return m_buffer; }
				set
				{
					if (m_buffer == value) return;
					if (m_buffer != null)
					{
						m_buffer.Overflow -= HandleBufferOverflow;
						m_buffer.BufferChanged -= SignalRefresh;
					}
					m_buffer = value;
					if (m_buffer != null)
					{
						m_buffer.Overflow += HandleBufferOverflow;
						m_buffer.BufferChanged += SignalRefresh;
						m_cmenu = new CMenu(this);
					}
					SignalRefresh();
				}
			}
			private Buffer m_buffer;

			/// <summary>Timer that causes refreshes once a seconds</summary>
			[Browsable(false)]
			public Timer BlinkTimer { get; private set; }

			/// <summary>True if the display automatically scrolls to the bottom</summary>
			[Browsable(false)]
			public bool AutoScrollToBottom
			{
				get { return m_auto_scroll_to_bottom; }
				set { m_auto_scroll_to_bottom = value; }
			}
			private bool m_auto_scroll_to_bottom;

			/// <summary>Request an update of the display</summary>
			public void SignalRefresh(object sender = null, EventArgs args = null)
			{
				m_eb.Signal();
			}

			/// <summary>Clear the buffer and the display</summary>
			public void Clear()
			{
				if (Buffer != null)
					Buffer.Clear();

				ClearAll();
			}

			#region Clipboard
			public override void Cut()
			{
				base.Copy(); // Cut isn't supported for terminals
			}
			public override void Copy()
			{
				base.Copy();
			}
			public override void Paste()
			{
				if (Buffer != null) Buffer.Paste();
				else base.Paste();
			}
			#endregion

			/// <summary>Refresh the control with text from the vt100 buffer</summary>
			private void UpdateText()
			{
				var buf = Buffer;

				// No buffer = empty display
				if (buf == null)
				{
					ClearAll();
					return;
				}

				using (HostedCtrl.SuspendRedraw(true))
				using (ScrollScope())
				using (SelectionScope())
				{
					ClearAll();

					// Add the buffered data to 'm_cells'
					foreach (var line in buf.Lines)
					{
						byte sty = 0;
						foreach (var span in line.Spans)
						{
							var str = Encoding.UTF8.GetBytes(span.m_str);
							sty = SciStyle(span.m_sty);
							foreach (var c in str)
								m_cells.Add(c, sty);
						}
						m_cells.Add(0x0a, sty);
					}
					m_cells.Pop(1);

					// Pass the buffer of cells to scintilla
					using (var cells = m_cells.Pin())
						Cmd(Sci.SCI_APPENDSTYLEDTEXT, m_cells.SizeInBytes, cells.Handle.AddrOfPinnedObject());

					// Reset, ready for next time
					m_cells.Reset();
				}

				// Auto scroll to the bottom if told to and the last line is off the bottom 
				if (AutoScrollToBottom)
				{
					// Move the caret to the end
					Cmd(Sci.SCI_SETEMPTYSELECTION, Cmd(Sci.SCI_GETTEXTLENGTH));

					// Only scroll if the last line isn't visible
					var last_line_index = Cmd(Sci.SCI_LINEFROMPOSITION, Cmd(Sci.SCI_GETTEXTLENGTH));
					var last_vis_line = Cmd(Sci.SCI_GETFIRSTVISIBLELINE) + Cmd(Sci.SCI_LINESONSCREEN);
					if (last_line_index >= last_vis_line)
						Cmd(Sci.SCI_SCROLLCARET);
				}
			}

			/// <summary>Return the scintilla style index for the given vt100 style</summary>
			private byte SciStyle(Style sty)
			{
				byte idx;
				if (!m_sty.TryGetValue(sty, out idx))
				{
					idx = (byte)Math.Min(m_sty.Count, 255);

					var forecol = HBGR.ToColor(!sty.RevserseVideo ? sty.ForeColour : sty.BackColour);
					var backcol = HBGR.ToColor(!sty.RevserseVideo ? sty.BackColour : sty.ForeColour);
					var fontname = Encoding.UTF8.GetBytes("consolas");
					using (var fonth = GCHandleEx.Alloc(fontname, GCHandleType.Pinned))
					{
						Cmd(Sci.SCI_STYLESETFONT, idx, fonth.Handle.AddrOfPinnedObject());
						Cmd(Sci.SCI_STYLESETFORE, idx, forecol.ToArgb() & 0x00FFFFFF);
						Cmd(Sci.SCI_STYLESETBACK, idx, backcol.ToArgb() & 0x00FFFFFF);
						Cmd(Sci.SCI_STYLESETBOLD, idx, sty.Bold ? 1 : 0);
						Cmd(Sci.SCI_STYLESETUNDERLINE, idx, sty.Underline ? 1 : 0);
					}

					m_sty[sty] = idx;
				}
				return idx;
			}

			/// <summary>Intercept the WndProc for this control</summary>
			protected override void CtrlWndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
			{
				//Win32.WndProcDebug(hwnd, msg, wparam, lparam, "vt100");
				switch (msg)
				{
				case Win32.WM_KEYDOWN:
					#region
					{
						//Win32.WndProcDebug(hwnd, msg, wparam, lparam, "vt100");
						var ks = new Win32.KeyState(lparam);
						if (!ks.Alt && Buffer != null)
						{
							// Let the key events through (by default) if local echo is on
							handled = Buffer.Settings.LocalEcho == false;

							var vk = (Keys)wparam;

							char ch;
							if (Win32.CharFromVKey(vk, out ch))
								Buffer.AddInput(ch);
							else
								handled &= Buffer.AddInput(vk);

							switch (vk)
							{
							// Intercept clipboard shortcuts
							case Keys.Control | Keys.X:
								Cut();
								break;
							case Keys.Control | Keys.C:
								Copy();
								break;
							case Keys.Control | Keys.V:
								Buffer.Paste();
								break;

							// Forward navigation keys to the control
							case Keys.Tab:
							case Keys.Up:
							case Keys.Down:
							case Keys.Left:
							case Keys.Right:
								Win32.SendMessage(HostedCtrl.Handle, (uint)msg, wparam, lparam);
								handled = true;
								break;
							}
						}
						break;
					}
					#endregion
				case Win32.WM_PARENTNOTIFY:
					#region
					{
						//Win32.WndProcDebug(hwnd, msg, wparam, lparam, "vt100");
						switch (Win32.LoWord(wparam))
						{
						case Win32.WM_RBUTTONDOWN:
							#region
							{
								var pt = Win32.LParamToPoint(lparam);
								ContextMenu.Show(this, pt);
								break;
							}
							#endregion
						}
						break;
					}
					#endregion
				}

				base.CtrlWndProc(hwnd, msg, wparam, lparam, ref handled);
			}

			/// <summary>Selection changes</summary>
			protected override void OnSelectionChanged()
			{
				base.OnSelectionChanged();
				var pos = Cmd(Sci.SCI_GETCURRENTPOS);
				var len = Cmd(Sci.SCI_GETTEXTLENGTH);
				AutoScrollToBottom = pos == len;
			}

			/// <summary>Handle buffer lines being dropped</summary>
			private void HandleBufferOverflow(object sender, Buffer.OverflowEventArgs args)
			{
				if (args.Before)
				{
					// Calculate the number of bytes dropped.
					var bytes = 0;
					for (int i = args.Dropped.Begini; i != args.Dropped.Endi; ++i)
						bytes += Encoding.UTF8.GetByteCount(Buffer.Lines[i].m_line.ToString());

					// Save the selection and adjust it by the number of characters to be dropped
					// Note: Buffer does not store line endings, but the saved selection does.
					bytes += args.Dropped.Sizei; // 1 '\n' per line dropped

					m_sel = Selection.Save(this);
					m_sel.m_current -= bytes;
					m_sel.m_anchor -= bytes;

					// If not auto scrolling, scroll up to move with the buffer text
					if (!AutoScrollToBottom)
					{
						var vis = Cmd(Sci.SCI_GETFIRSTVISIBLELINE);
						vis = Math.Max(0, vis - args.Dropped.Sizei);
						Cmd(Sci.SCI_SETFIRSTVISIBLELINE, vis);
					}
				}
				else
				{
					Selection.Restore(this, m_sel);
				}
			}
			private Selection m_sel;

			/// <summary>A context menu for the vt100 terminal</summary>
			public new ContextMenuStrip ContextMenu
			{
				get { return m_cmenu; }
			}

			/// <summary>The context menu for this vt100 display</summary>
			private class CMenu :ContextMenuStrip
			{
				private Display m_disp;
				public CMenu(Display disp)
				{
					m_disp = disp;
					using (this.SuspendLayout(false))
					{
						#region Clear
						{
							var item = Items.Add2(new ToolStripMenuItem("Clear", null));
							item.Click += (s,e) => m_disp.Clear();
						}
						#endregion
						Items.Add(new ToolStripSeparator());
						#region Copy
						{
							var item = Items.Add2(new ToolStripMenuItem("Copy", null));
							item.Click += (s,e) => m_disp.Copy();
						}
						#endregion
						#region Paste
						{
							var item = Items.Add2(new ToolStripMenuItem("Paste", null));
							item.Click += (s,e)=> m_disp.Paste();
						}
						#endregion
						if (m_disp.Settings != null)
						{
							Items.Add(new ToolStripSeparator());
							var options = Items.Add2(new ToolStripMenuItem("Terminal Options", null));

							#region Local Echo
							{
								var item = options.DropDownItems.Add2(new ToolStripMenuItem("Local Echo"){CheckOnClick = true});
								options.DropDown.Opening += (s,a) =>
									{
										item.Checked = m_disp.Settings.LocalEcho;
									};
								item.Click += (s,a) =>
									{
										m_disp.Settings.LocalEcho = item.Checked;
									};
							}
							#endregion
							#region Terminal Width
							{
								var item = options.DropDownItems.Add2(new ToolStripMenuItem("Terminal Width"));
								var edit = item.DropDownItems.Add2(new ToolStripTextBox());
								item.DropDown.Opening += (s,a) =>
									{
										edit.Text = m_disp.Settings.TerminalWidth.ToString();
									};
								item.DropDown.Closing += (s,a) =>
									{
										int w;
										if (!(a.Cancel = !int.TryParse(edit.Text, out w)))
											m_disp.Settings.TerminalWidth = w;
									};
								edit.KeyDown += (s,e) =>
									{
										if (e.KeyCode != Keys.Return) return;
										item.DropDown.Close();
									};
							}
							#endregion
							#region Terminal Height
							{
								var item = options.DropDownItems.Add2(new ToolStripMenuItem("Terminal Height"));
								var edit = item.DropDownItems.Add2(new ToolStripTextBox());
								item.DropDown.Opening += (s,a) =>
									{
										edit.Text = m_disp.Settings.TerminalHeight.ToString();
									};
								item.DropDown.Closing += (s,e) =>
									{
										int h;
										if (!(e.Cancel = !int.TryParse(edit.Text, out h)))
											m_disp.Settings.TerminalHeight = h;
									};
								edit.KeyDown += (s,e) =>
									{
										if (e.KeyCode != Keys.Return) return;
										item.DropDown.Close();
									};
							}
							#endregion
							#region Tab Size
							{
								var item = options.DropDownItems.Add2(new ToolStripMenuItem("Tab Size"));
								var edit = item.DropDownItems.Add2(new ToolStripTextBox());
								item.DropDown.Opening += (s,a) =>
									{
										edit.Text = m_disp.Settings.TabSize.ToString();
									};
								item.DropDown.Closing += (s,e) =>
									{
										int sz;
										if (!(e.Cancel = !int.TryParse(edit.Text, out sz)))
											m_disp.Settings.TabSize = sz;
									};
								edit.KeyDown += (s,e) =>
									{
										if (e.KeyCode != Keys.Return) return;
										Close();
									};
							}
							#endregion
							#region Newline Recv
							{
								var item = options.DropDownItems.Add2(new ToolStripMenuItem("Newline Recv"));
								var edit = item.DropDownItems.Add2(new ToolStripComboBox());
								edit.Items.AddRange(Enum<ENewLineMode>.Values.Cast<object>().ToArray());
								item.DropDown.Opening += (s,a) =>
									{
										edit.SelectedItem = m_disp.Settings.NewlineRecv;
									};
								edit.SelectedIndexChanged += (s,e) =>
									{
										m_disp.Settings.NewlineRecv = (ENewLineMode)edit.SelectedIndex;
									};
							}
							#endregion
							#region Newline Send
							{
								var item = options.DropDownItems.Add2(new ToolStripMenuItem("Newline Send"));
								var edit = item.DropDownItems.Add2(new ToolStripComboBox());
								edit.Items.AddRange(Enum<ENewLineMode>.Values.Cast<object>().ToArray());
								item.DropDown.Opening += (s,a) =>
									{
										edit.SelectedItem = m_disp.Settings.NewlineSend;
									};
								edit.SelectedIndexChanged += (s,e) =>
									{
										m_disp.Settings.NewlineSend = (ENewLineMode)edit.SelectedIndex;
									};
							}
							#endregion
							#region Hex Output
							{
								var item = options.DropDownItems.Add2(new ToolStripMenuItem("Hex Output"){CheckOnClick = true});
								options.DropDown.Opening += (s,a) =>
									{
										item.Checked = m_disp.Settings.HexOutput;
									};
								item.Click += (s,e) =>
									{
										m_disp.Settings.HexOutput = item.Checked;
									};
							}
							#endregion
						}
					}
				}
			}
		}
	}
}
