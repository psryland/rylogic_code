﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Scintilla;
using Rylogic.Utility;
using Rylogic.Windows32;

namespace Rylogic.Gui
{
	using Timer = System.Windows.Forms.Timer;

	/// <summary>VT terminal emulation. see: http://ascii-table.com/ansi-escape-sequences.php </summary>
	public static class VT100
	{
		/// <summary>Terminal behaviour setting</summary>
		[TypeConverter(typeof(Settings.TyConv))]
		public class Settings :SettingsSet<Settings>
		{
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

			/// <summary>True if input characters should be echoed into the screen buffer</summary>
			public bool LocalEcho
			{
				get { return get<bool>(nameof(LocalEcho)); }
				set { set(nameof(LocalEcho), value); }
			}

			/// <summary>Get/Set the width of the terminal in columns</summary>
			public int TerminalWidth
			{
				get { return get<int>(nameof(TerminalWidth)); }
				set { set(nameof(TerminalWidth), value); }
			}

			/// <summary>Get/Set the height of the terminal in lines</summary>
			public int TerminalHeight
			{
				get { return get<int>(nameof(TerminalHeight)); }
				set { set(nameof(TerminalHeight), value); }
			}

			/// <summary>Get/Set the tab size in characters</summary>
			public int TabSize
			{
				get { return get<int>(nameof(TabSize)); }
				set { set(nameof(TabSize), value); }
			}

			/// <summary>Get/Set the newline mode for received newlines</summary>
			public ENewLineMode NewlineRecv
			{
				get { return get<ENewLineMode>(nameof(NewlineRecv)); }
				set { set(nameof(NewlineRecv), value); }
			}

			/// <summary>Get/Set the newline mode for sent newlines</summary>
			public ENewLineMode NewlineSend
			{
				get { return get<ENewLineMode>(nameof(NewlineSend)); }
				set { set(nameof(NewlineSend), value); }
			}

			/// <summary>Get/Set the received data being written has hex data into the buffer</summary>
			public bool HexOutput
			{
				get { return get<bool>(nameof(HexOutput)); }
				set { set(nameof(HexOutput), value); }
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
			private byte m_col; // Fore/back colour (high-bright,blue,green,red)
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
				return $"col: {m_col} sty: {m_sty}";
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
				Debug.Assert(fill != 0, "Don't fill with the null character because that creates a mismatch match between the line length and the string length");
				m_line.Resize(newsize, fill);
				m_styl.Resize(newsize, () => style);
			}
			public void Resize(int newsize)
			{
				Resize(newsize, ' ', Style.Default);
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
		public class UserInput
		{
			// Note: all this needs to do is buffer key presses, those characters/keys
			// are sent to the server which will reply causing changes to the buffer.
			// User input should never be rendered directly into the display
			private byte[] m_buf;
			private int m_len;

			public UserInput(int capacity = 8192)
			{
				m_buf = new byte[capacity != int.MaxValue ? capacity : 0];
				m_len = 0;
			}

			/// <summary>Append bytes to the user input</summary>
			public void Add(byte[] bytes, int ofs, int count)
			{
				EnsureSpace(m_len + count);
				Array.Copy(bytes, ofs, m_buf, m_len, count);
				m_len += count;
			}

			/// <summary>Add a character to the buffer</summary>
			public void Add(char ch)
			{
				var bytes = Encoding.UTF8.GetBytes(new string(ch, 1));
				Add(bytes, 0, bytes.Length);
			}

			/// <summary>Add another buffer to this</summary>
			public void Add(UserInput rhs)
			{
				Add(rhs.m_buf, 0, rhs.m_len);
			}

			/// <summary>Add a stream of bytes to the input</summary>
			public void Add(Stream s)
			{
				var buf = new byte[1024];
				for (int read; (read = s.Read(buf, 0, buf.Length)) != 0;)
					Add(buf, 0, read);
			}

			/// <summary>Return the contents of the buffer, optionally resetting it</summary>
			public byte[] Get(bool reset)
			{
				var ret = m_buf.Dup(0, m_len);
				if (reset) m_len = 0;
				return ret;
			}

			/// <summary>Ensure 'm_buf' can hold 'size' bytes</summary>
			private void EnsureSpace(int size)
			{
				if (size <= m_buf.Length) return;
				Array.Resize(ref m_buf, m_buf.Length * 3 / 2);
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
					return $"{pos} {style}";
				}
			}

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
				Settings  = settings ?? new Settings();
				UserInput = new UserInput(8192);
				m_seq     = new StringBuilder();
				m_lines   = new List<Line>();
				m_state   = State.Default;
				m_out     = State.Default;
				ValidateRect();
			}

			/// <summary>Terminal settings</summary>
			public Settings Settings { get; private set; }

			/// <summary>Return the number of lines of text in the control</summary>
			public int LineCount { get { return m_lines.Count; } }

			/// <summary>Access the lines in the buffer</summary>
			public List<Line> Lines { get { return m_lines; } }

			/// <summary>The current position of the output caret</summary>
			public Point CaretPos { get { return m_out.pos; } }

			/// <summary>The buffer region that has changed since ValidateRect() was last called</summary>
			public Rectangle InvalidRect { get; private set; }

			/// <summary>User input buffer</summary>
			public UserInput UserInput { get; private set; }

			/// <summary>Add a single character to the user input buffer.</summary>
			public void AddInput(char c, bool notify = true)
			{
				if (c == '\n')
				{
					switch (Settings.NewlineSend)
					{
					case ENewLineMode.LF:    UserInput.Add('\n'); break;
					case ENewLineMode.CR:    UserInput.Add('\r'); break;
					case ENewLineMode.CR_LF: "\r\n".ForEach(UserInput.Add); break;
					}
				}
				else
				{
					UserInput.Add(c);
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
				// Invalidate the whole buffer
				InvalidateRect(new Rectangle(Point.Empty, new Size(Settings.TerminalWidth, LineCount)));

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

			/// <summary>Reset the invalidation rectangle</summary>
			public void ValidateRect()
			{
				InvalidRect = Rectangle.Empty;
			}

			/// <summary>Add a rectangular region of the buffer to the invalidated area</summary>
			public void InvalidateRect(Rectangle rect)
			{
				if (rect.X < 0 || rect.Y < 0) throw new Exception("Invalidation area is outside the buffer");
				InvalidRect = InvalidRect.IsEmpty ? rect : Rectangle.Union(InvalidRect, rect);
			}

			/// <summary>Start or stop capturing to a file</summary>
			public void CaptureToFile(string filepath, bool capture_all_data)
			{
				m_capture_file = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite|FileShare.Delete);
				m_capture_all_data = capture_all_data;
			}
			public void CaptureToFileEnd()
			{
				Util.Dispose(ref m_capture_file);
			}
			private void Capture(string str, int ofs, int count, bool all_data)
			{
				if (!CapturingToFile) return;
				if (m_capture_all_data != all_data) return;

				var bytes = m_capture_all_data
					? str.ToBytes(ofs, count)
					: Encoding.UTF8.GetBytes(str.ToCharArray(ofs, Math.Min(count, str.Length - ofs)));

				m_capture_file.Write(bytes, 0, bytes.Length);
				m_capture_file.Flush();
			}
			private FileStream m_capture_file;
			private bool m_capture_all_data;

			/// <summary>True if capturing to file is currently enabled</summary>
			public bool CapturingToFile
			{
				get { return m_capture_file != null; }
			}

			/// <summary>Send the contents of a file to the terminal</summary>
			public void SendFile(string filepath, bool binary_mode)
			{
				using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					UserInput.Add(fs);
				OnInputAdded();
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
				Capture(text, 0, text.Length, true);
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
						(c == '\r' && e+1 != eend && text[e+1] == '\n' && Settings.NewlineRecv == ENewLineMode.CR_LF))
					{
						Write(text, s, e - s);
						Capture("\n", 0, 1, false);
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
					ReportUnsupportedEscapeSequence(m_seq);
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
							ReportUnsupportedEscapeSequence(m_seq);
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

						case 'G':
							#region Esc[#G - Move to column # 
							{
								// Esc[#G Move the cursor to column '#' (not standard vt100 protocol)
								var n = Params(1, m_seq, 2, -1);
								m_out.pos = MoveCaret(n[0], m_out.pos.Y);
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
										LineAt(m_out.pos.Y).Resize(m_out.pos.X);
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
										LineAt(m_out.pos.Y).Resize(m_out.pos.X);
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
										LineAt(m_out.pos.Y).Resize(0);
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
							ReportUnsupportedEscapeSequence(m_seq);
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
							ReportUnsupportedEscapeSequence(m_seq);
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

				case 'O': //EscO... codes ... I think these are actually response codes...
					#region Esc0
					{
						if (m_seq.Length < 3) return; // Incomplete sequence
						var code = m_seq.Back();
						switch (code)
						{
						default:
							ReportUnsupportedEscapeSequence(m_seq);
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
							ReportUnsupportedEscapeSequence(m_seq);
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

				case 'A': //EscA Move cursor up one line CURSORUP
					m_out.pos = MoveCaret(m_out.pos, 0,-1);
					break;

				case 'B': //EscB Move cursor down one line CURSORDN
					m_out.pos = MoveCaret(m_out.pos, 0,+1);
					break;

				case 'C': //EscC Move cursor right one char CURSORRT
					m_out.pos = MoveCaret(m_out.pos, +1,0);
					break;

				case 'D': //EscD Move cursor left one char CURSORLF
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
			
			/// <summary>Move the caret to an absolute position</summary>
			private Point MoveCaret(int x, int y)
			{
				// Allow the buffer to grow to %150 so that we're not rolling on every line
				var ymax = Settings.TerminalHeight * 3 / 2;

				// Roll the buffer when y >= terminal height
				if (y >= ymax)
				{
					var count = Math.Min(y - Settings.TerminalHeight + 1, LineCount);
					OnOverflow(new OverflowEventArgs(true, new Range(0, 0 + count)));

					// Remove the lines and adjust anything that stores a line number
					m_lines.RemoveRange(0, count);
					y -= count;
					m_out.pos.Y -= count;
					m_saved.pos.Y -= count;

					// Overflow does not invalidate the entire buffer.
					// This is handled by removing the first 'count' lines from the
					// control without any other text needing to change. This means
					// the last 'count' do not need invalidating because they should've
					// moved to the correct position by deleting the starting lines.
					// However, we need to move the invalidation rectangle up with the
					// rest of the text.
					InvalidRect = new Rectangle(
						InvalidRect.X    ,                      Math.Max(0, InvalidRect.Y - count),
						InvalidRect.Width, InvalidRect.Height + Math.Min(0, InvalidRect.Y - count));

					OnOverflow(new OverflowEventArgs(false, new Range(0, 0 + count)));
				}

				if (x >= 0 && x < Settings.TerminalWidth && y >= 0 && y < ymax)
					InvalidateRect(new Rectangle(x, y, 1, 1));

				// Don't clamp x, the vt100 doesn't know what our buffer size is
				// so we need to maintain a virtual space that might be outside our buffer
				//x = Math_.Clamp(x, 0, Settings.TerminalWidth);
				var loc = new Point(x, y);
				return loc;
			}

			/// <summary>Move the caret by a relative offset</summary>
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
				Debug.Assert(ofs >= 0 && ofs < str.Length && count >= 0);
				count = Math.Min(count, str.Length - ofs);

				// Add received text to the capture file, if capturing
				Capture(str, ofs, count, false);

				// Just ignore caret positions outside the buffer. This can happen because
				// the vt100 commands can move the caret but is unaware of the buffer size.
				if (m_out.pos.Y < 0) return;

				// If m_out.pos.X < 0, pretend Min(-m_out.pos.X, count) characters were written
				if (m_out.pos.X < 0)
				{
					var dx = Math.Min(-m_out.pos.X, count);
					Debug.Assert(dx <= count);
					m_out.pos.X += dx;
					count -= dx;
					ofs += dx;
				}

				// If m_out.pos.X >= Settings.TerminalWidth, pretend all characters were written
				if (m_out.pos.X >= Settings.TerminalWidth)
				{
					m_out.pos.X += count;
					count = 0;
				}

				if (count == 0)
					return;

				Debug.Assert(m_out.pos.X >= 0 && m_out.pos.X < Settings.TerminalWidth, "Output position out of range");

				// Limit 'count' to the size of the terminal and the maximum string length
				count = Math.Min(count, str.Length - ofs);
				var clipped = count > Settings.TerminalWidth - m_out.pos.X;
				count = Math.Min(count, Settings.TerminalWidth - m_out.pos.X);
				Debug.Assert(count >= 0 && count <= str.Length - ofs);

				// Get the line and ensure it's large enough
				var line = LineAt(m_out.pos.Y);
				if (line.Size < m_out.pos.X + count)
					line.Resize(m_out.pos.X + count, ' ', m_out.style);

				// Add to the invalid rect
				InvalidateRect(new Rectangle(m_out.pos, new Size(count, 1)));

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
				Capture("\b", 0, 1, false);
				m_out.pos = MoveCaret(m_out.pos, -1, 0);
				var x = Math.Max(m_out.pos.X, 0);
				var y = m_out.pos.Y;

				// Just ignore caret positions outside the buffer. This can happen because
				// the vt100 commands can move the caret but is unaware of the buffer size.
				if (y < 0) return;

				// Invalidate the rest of the line
				InvalidateRect(new Rectangle(0, y, Settings.TerminalWidth - x, 1));

				// Get the line
				var line = LineAt(y);
				line.Erase(x, 1);

				// Notify whenever the buffer is changed
				OnBufferChanged(new BufferChangedEventArgs(new Point(x,y), line.Size - x));
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
			public static string TestConsoleString0
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
			public static string TestConsoleString1
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

			/// <summary>Handle unsupported escape sequences</summary>
			public bool ReportUnsupportedEscapeSequences = true;
			protected virtual void ReportUnsupportedEscapeSequence(StringBuilder seq)
			{
				if (ReportUnsupportedEscapeSequences)
					Debug.WriteLine($"Unsupported VT100 escape sequence: {seq}");
			}
		}

		/// <summary>A control that displays the VT100 buffer</summary>
		public class Display :ScintillaCtrl
		{
			private static readonly string FileFilters = Util.FileDialogFilter("Text Files","*.txt","Log Files","*.log","All Files","*.*");

			private HoverScroll m_hs;
			private EventBatcher m_eb;
			private Dictionary<Style, byte> m_sty; // map from vt100 style to scintilla style index
			private Sci.CellBuf m_cells;

			public Display(Buffer buf)
			{
				m_hs = new HoverScroll(Handle);
				m_eb = new EventBatcher(UpdateText, TimeSpan.FromMilliseconds(1)){TriggerOnFirst = true};
				m_sty = new Dictionary<Style,byte>();
				m_cells = new Sci.CellBuf();
				ContextMenuStrip = new CMenu(this);
				
				BlinkTimer = new Timer{Interval = 1000, Enabled = false};
				BlinkTimer.Tick += SignalRefresh;

				AllowDrop = true;
				AutoScrollToBottom = true;
				ScrollToBottomOnInput = true;
				EndAtLastLine = true;

				// Use our own context menu
				UsePopUp = false;

				// Turn off undo history
				UndoCollection = false;

				Buffer = buf;
			}
			protected override void Dispose(bool disposing)
			{
				Buffer = null;
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
						m_buffer.CaptureToFileEnd();
						m_buffer.Overflow -= HandleBufferOverflow;
						m_buffer.BufferChanged -= UpdateText;
						ContextMenuStrip = null;
					}
					m_buffer = value;
					if (m_buffer != null)
					{
						m_buffer.Overflow += HandleBufferOverflow;
						m_buffer.BufferChanged += UpdateText;
						ContextMenuStrip = new CMenu(this);
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
			public bool AutoScrollToBottom { get; set; }

			/// <summary>True if adding input causes the display to automatically scroll to the bottom</summary>
			[Browsable(false)]
			public bool ScrollToBottomOnInput { get; set; }

			/// <summary>Request an update of the display</summary>
			public void SignalRefresh(object sender = null, EventArgs args = null)
			{
				m_eb.Signal();
			}

			/// <summary>Clear the buffer and the display</summary>
			public new void Clear()
			{
				if (Buffer != null)
					Buffer.Clear();

				ClearAll();
			}

			/// <summary>Copy the current selection</summary>
			public new void Copy()
			{
				base.Copy();
			}

			/// <summary>Paste the clipboard into the input buffer</summary>
			public new void Paste()
			{
				Buffer.Paste();
			}

			/// <summary>Adds text to the input buffer</summary>
			protected virtual void AddToBuffer(Keys vk)
			{
				// If input triggers auto scroll
				if (ScrollToBottomOnInput)
				{
					AutoScrollToBottom = true;
					ScrollToBottom();
				}

				// Try to add the raw key code first. If not handled,
				// try to convert it to a character and add it again.
				if (!Buffer.AddInput(vk))
				{
					char ch;
					if (Win32.CharFromVKey(vk, out ch))
						Buffer.AddInput(ch);
				}
			}

			/// <summary>Start or stop capturing to a file</summary>
			public void CaptureToFile(bool start)
			{
				if (Buffer == null) return;
				if (start)
				{
					using (var dlg = new ChooseCaptureFile())
					{
						if (dlg.ShowDialog(this) != DialogResult.OK) return;
						try
						{
							Buffer.CaptureToFile(dlg.Filepath, dlg.CaptureAllData);
						}
						catch (Exception ex)
						{
							MessageBox.Show(this, $"Capture to file could not start\r\n{ex.Message}", "Capture To File", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
					}
				}
				else
				{
					Buffer.CaptureToFileEnd();
				}
			}

			/// <summary>True if capturing to file is currently enabled</summary>
			public bool CapturingToFile
			{
				get { return Buffer != null && Buffer.CapturingToFile; }
			}

			/// <summary>Send a file to the terminal</summary>
			public void SendFile(bool binary_mode)
			{
				if (Buffer == null) return;
				using (var dlg = new OpenFileDialog { Title = "Choose the file to send", Filter = FileFilters })
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					Buffer.SendFile(dlg.FileName, binary_mode);
				}
			}

			/// <summary>Refresh the control with text from the vt100 buffer</summary>
			private void UpdateText(object sender = null, EventArgs args = null)
			{
				var buf = Buffer;

				// No buffer = empty display
				if (buf == null)
				{
					ClearAll();
					return;
				}

				// Get the buffer region that has changed
				var region = buf.InvalidRect;
				buf.ValidateRect();
				if (region.IsEmpty)
					return;

				using (this.SuspendRedraw(true))
				using (ScrollScope())
				{
					// Grow the text in the control to the required number of lines (if necessary)
					var line_count = LineCount;
					if (line_count < region.Bottom)
					{
						var pad = new byte[region.Bottom - line_count].Memset(0x0a);
						using (var p = GCHandle_.Alloc(pad, GCHandleType.Pinned))
							Cmd(Sci.SCI_APPENDTEXT, pad.Length, p.Handle.AddrOfPinnedObject());
					}

					// Update the text in the control from the invalid buffer region
					for (int i = region.Top, iend = region.Bottom; i != iend; ++i)
					{
						// Update whole lines, to hard to bother with x ranges
						// Note, the invalid region can be outside the buffer when the buffer gets cleared
						if (i >= buf.LineCount) break;
						var line = buf.Lines[i];

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

					// Remove the last newline if the last line updated is also the last line in the buffer,
					// and append a null terminator so that 'InsertStyledText' can determine the length.
					if (buf.LineCount == region.Bottom) m_cells.Pop(1);
					m_cells.Add(0, 0);

					// Determine the character range to be updated
					var beg = PositionFromLine(region.Top);
					var end = PositionFromLine(region.Bottom);
					if (beg < 0) beg = 0;
					if (end < 0) end = TextLength;

					// Overwrite the visible lines with the buffer of cells
					DeleteRange(beg, end - beg);
					InsertStyledText(beg, m_cells);

					// Reset, ready for next time
					m_cells.Length = 0;
				}

				// Auto scroll to the bottom if told to and the last line is off the bottom 
				if (AutoScrollToBottom)
					ScrollToBottom();
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
					StyleSetFont(idx, "consolas");
					StyleSetFore(idx, forecol);
					StyleSetBack(idx, backcol);
					StyleSetBold(idx, sty.Bold);
					StyleSetUnderline(idx, sty.Underline);
					m_sty[sty] = idx;
				}
				return idx;
			}

			/// <summary>Intercept the WndProc for this control</summary>
			protected override void WndProc(ref Message m)
			{
				//Win32.WndProcDebug(hwnd, msg, wparam, lparam, "vt100");
				switch (m.Msg)
				{
				case Win32.WM_KEYDOWN:
					#region
					{
						var ks = new Win32.KeyState(m.LParam);
						if (!ks.Alt && Buffer != null)
						{
							var vk = Win32.ToVKey(m.WParam);

							// Forward navigation keys to the control
							if (vk == Keys.Up || vk == Keys.Down || vk == Keys.Left || vk == Keys.Right)
							{
								Cmd(m.Msg, m.WParam, m.LParam);
								return;
							}

							// Handle clipboard shortcuts
							if (Win32.KeyDown(Keys.ControlKey))
							{
								if (vk == Keys.C) { Copy(); return; }
								if (vk == Keys.V) { Paste(); return; }
								return; // Disable all other shortcuts
							}

							// Add the keypress to the buffer input
							AddToBuffer(vk);

							// Let the key events through if local echo is on
							if (Buffer.Settings.LocalEcho)
								break;

							// Block key events from getting to the ctrl
							return;
						}
						break;
					}
					#endregion
				case Win32.WM_CHAR:
					#region
					{
						return;
					}
				#endregion
				case Win32.WM_DROPFILES:
					#region
					{
						var drop_info = m.WParam;
						var count = Win32.DragQueryFile(drop_info, 0xFFFFFFFFU, null, 0);
						var files = new List<string>();
						for (int i = 0; i != count; ++i)
						{
							var sb = new StringBuilder((int)Win32.DragQueryFile(drop_info, (uint)i, null, 0) + 1);
							if (Win32.DragQueryFile(drop_info, (uint)i, sb, (uint)sb.Capacity) == 0)
								throw new Exception("Failed to query file name from dropped files");
							files.Add(sb.ToString());
							sb.Clear();
						}
						HandleDropFiles(files);
						return;
					}
					#endregion
				}
				base.WndProc(ref m);
			}

			/// <summary>Scroll event</summary>
			protected override void OnScroll(ScrollEventArgs args)
			{
				base.OnScroll(args);

				// Turn on auto scroll if the last line is visible
				var vis = VisibleLineIndexRange;
				var last = LineCount - 1;
				AutoScrollToBottom = vis.Contains(last);
			}

			/// <summary>Drag drop event</summary>
			protected virtual void HandleDropFiles(IEnumerable<string> files)
			{
				if (Buffer == null) return;
				foreach (var file in files)
					Buffer.SendFile(file, true);
			}

			/// <summary>Handle buffer lines being dropped</summary>
			private void HandleBufferOverflow(object sender, Buffer.OverflowEventArgs args)
			{
				if (args.Before)
				{
					// Calculate the number of bytes dropped.
					// Note: Buffer does not store line endings, but the saved selection does.
					var bytes = 0;
					for (int i = args.Dropped.Begi; i != args.Dropped.Endi; ++i)
						bytes += Encoding.UTF8.GetByteCount(Buffer.Lines[i].m_line.ToString()) + 1; // +1 '\n' per line

					// Save the selection and adjust it by the number of characters to be dropped
					m_sel = Selection.Save(this);
					m_sel.m_current -= bytes;
					m_sel.m_anchor  -= bytes;

					// Remove the scrolled text from the control
					DeleteRange(0, bytes);

					// Scroll up to move with the buffer text
					var vis = FirstVisibleLine;
					vis = Math.Max(0, vis - args.Dropped.Sizei);
					FirstVisibleLine = vis;
				}
				else
				{
					Selection.Restore(this, m_sel);
				}
			}
			private Selection m_sel;

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
						Items.Add(new ToolStripSeparator());
						#region Capture To File
						{
							var item = Items.Add2(new ToolStripMenuItem("Capture To File", null));
							Opening += (s,a) =>
								{
									item.Enabled = m_disp.Buffer != null;
									item.Checked = m_disp.CapturingToFile;
								};
							item.Click += (s,e) =>
								{
									m_disp.CaptureToFile(!m_disp.CapturingToFile);
								};
						}
						#endregion
						#region Send File
						{
							var item = Items.Add2(new ToolStripMenuItem("Send File", null));
							Opening += (s,a) =>
								{
									item.Enabled = m_disp.Buffer != null;
								};
							item.Click += (s,e) =>
								{
									m_disp.SendFile(true);
								};
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
								var cb = new ComboBox(); cb.Items.AddRange(Enum<ENewLineMode>.Values.Cast<object>().ToArray());
								var edit = item.DropDownItems.Add2(new ToolStripControlHost(cb));
								item.DropDown.Opening += (s,a) =>
									{
										cb.SelectedItem = m_disp.Settings.NewlineRecv;
									};
								cb.SelectedIndexChanged += (s,e) =>
									{
										m_disp.Settings.NewlineRecv = (ENewLineMode)cb.SelectedItem;
									};
							}
							#endregion
							#region Newline Send
							{
								var item = options.DropDownItems.Add2(new ToolStripMenuItem("Newline Send"));
								var cb = new ComboBox(); cb.Items.AddRange(Enum<ENewLineMode>.Values.Cast<object>().ToArray());
								var edit = item.DropDownItems.Add2(new ToolStripControlHost(cb));
								item.DropDown.Opening += (s,a) =>
									{
										cb.SelectedItem = m_disp.Settings.NewlineSend;
									};
								cb.SelectedIndexChanged += (s,e) =>
									{
										m_disp.Settings.NewlineSend = (ENewLineMode)cb.SelectedItem;
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

			/// <summary>Dialog for selecting the capture file</summary>
			private class ChooseCaptureFile :Form
			{
				#region UI Elements
				private TextBox m_tb_filepath;
				private Button m_btn_browse;
				private CheckBox m_chk_capture_all;
				private Button m_btn_ok;
				private Button m_btn_cancel;
				#endregion

				public ChooseCaptureFile()
				{
					InitializeComponent();
					StartPosition = FormStartPosition.CenterParent;

					m_btn_browse.Click += (s,a) =>
						{
							using (var dlg = new SaveFileDialog{Title = "Capture file path", FileName = Filepath, Filter = FileFilters })
							{
								if (dlg.ShowDialog(this) != DialogResult.OK) return;
								DialogResult = DialogResult.None;
								Filepath = dlg.FileName;
							}
						};
				}
				protected override void Dispose(bool disposing)
				{
					Util.Dispose(ref components);
					base.Dispose(disposing);
				}

				/// <summary>The selected filepath</summary>
				public string Filepath
				{
					get { return m_tb_filepath.Text; }
					set { m_tb_filepath.Text = value; }
				}

				/// <summary>Get/Set whether all data is captured</summary>
				public bool CaptureAllData
				{
					get { return m_chk_capture_all.Checked; }
					set { m_chk_capture_all.Checked = value; }
				}

				/// <summary>
				/// Required method for Designer support - do not modify
				/// the contents of this method with the code editor.</summary>
				private void InitializeComponent()
				{
					this.m_tb_filepath = new System.Windows.Forms.TextBox();
					this.m_btn_browse = new System.Windows.Forms.Button();
					this.m_chk_capture_all = new System.Windows.Forms.CheckBox();
					this.m_btn_ok = new System.Windows.Forms.Button();
					this.m_btn_cancel = new System.Windows.Forms.Button();
					this.SuspendLayout();
					// 
					// m_tb_filepath
					// 
					this.m_tb_filepath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
					| System.Windows.Forms.AnchorStyles.Right)));
					this.m_tb_filepath.Location = new System.Drawing.Point(12, 12);
					this.m_tb_filepath.Name = "m_tb_filepath";
					this.m_tb_filepath.Size = new System.Drawing.Size(203, 20);
					this.m_tb_filepath.TabIndex = 0;
					// 
					// m_btn_browse
					// 
					this.m_btn_browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
					this.m_btn_browse.DialogResult = System.Windows.Forms.DialogResult.Cancel;
					this.m_btn_browse.Location = new System.Drawing.Point(221, 10);
					this.m_btn_browse.Name = "m_btn_browse";
					this.m_btn_browse.Size = new System.Drawing.Size(51, 23);
					this.m_btn_browse.TabIndex = 1;
					this.m_btn_browse.Text = ". . .";
					this.m_btn_browse.UseVisualStyleBackColor = true;
					// 
					// m_chk_capture_all
					// 
					this.m_chk_capture_all.AutoSize = true;
					this.m_chk_capture_all.Location = new System.Drawing.Point(24, 38);
					this.m_chk_capture_all.Name = "m_chk_capture_all";
					this.m_chk_capture_all.Size = new System.Drawing.Size(251, 17);
					this.m_chk_capture_all.TabIndex = 2;
					this.m_chk_capture_all.Text = "Capture all data (including vt100 escape codes)";
					this.m_chk_capture_all.UseVisualStyleBackColor = true;
					// 
					// m_btn_ok
					// 
					this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
					this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
					this.m_btn_ok.Location = new System.Drawing.Point(113, 63);
					this.m_btn_ok.Name = "m_btn_ok";
					this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
					this.m_btn_ok.TabIndex = 3;
					this.m_btn_ok.Text = "OK";
					this.m_btn_ok.UseVisualStyleBackColor = true;
					// 
					// m_btn_cancel
					// 
					this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
					this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
					this.m_btn_cancel.Location = new System.Drawing.Point(197, 63);
					this.m_btn_cancel.Name = "m_btn_cancel";
					this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
					this.m_btn_cancel.TabIndex = 4;
					this.m_btn_cancel.Text = "Cancel";
					this.m_btn_cancel.UseVisualStyleBackColor = true;
					// 
					// Form1
					// 
					this.AcceptButton = this.m_btn_ok;
					this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
					this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
					this.CancelButton = this.m_btn_cancel;
					this.ClientSize = new System.Drawing.Size(284, 93);
					this.Controls.Add(this.m_btn_cancel);
					this.Controls.Add(this.m_btn_ok);
					this.Controls.Add(this.m_chk_capture_all);
					this.Controls.Add(this.m_btn_browse);
					this.Controls.Add(this.m_tb_filepath);
					this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
					this.MaximumSize = new System.Drawing.Size(1000, 132);
					this.MinimumSize = new System.Drawing.Size(300, 132);
					this.Name = "Form1";
					this.Text = "Select a capture file";
					this.ResumeLayout(false);
					this.PerformLayout();

				}
				private IContainer components = null;
			}
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Gui;

	[TestFixture] public class TestVT100
	{
		[Test] public void Robustness()
		{
			// Blast the buffer with noise to make sure it can handle any input
			var settings = new VT100.Settings();
			var buf = new VT100.Buffer(settings);
			buf.ReportUnsupportedEscapeSequences = false;

			var rnd = new Random(0);
			for (int i = 0; i != 10000; ++i)
			{
				var noise = rnd.Bytes().Take(rnd.Next(1000)).Select(x => (char)x).ToArray();
				buf.Output(new string(noise));
			}
		}
	}
}
#endif