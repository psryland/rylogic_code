using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Rylogic.Extn;
using Rylogic.Str;
using Rylogic.Utility;

namespace Rylogic.Common
{
	// VT terminal emulation. see: http://ascii-table.com/ansi-escape-sequences.php
	public static class VT100
	{
		// Usage:
		//   - Add a 'Display' control to the window (Probably VT100Control).
		//   - Use the 'Buffer.UserInput.Get()' method to retrieve user input (since last called).
		//   - Send the user input to the remote device/system.
		//   - Receive data from the remote device/system.
		//   - Use 'Buffer.Output()' to update the in-memory screen buffer of characters. This will trigger 'BufferChanged' events.
		//   - Buffer keeps track of the invalidated screen buffer region.
		//   - Use 'Buffer.ReadTextArea()' to read regions of the in-memory screen buffer and update the display control</summary>

		/// <summary>Terminal behaviour setting</summary>
		[TypeConverter(typeof(Settings.TyConv))]
		public class Settings : SettingsSet<Settings>
		{
			public Settings()
			{
				LocalEcho = true;
				TabSize = 4;
				TerminalWidth = 120;
				TerminalHeight = 256;
				NewLineRecv = ENewLineMode.LF;
				NewLineSend = ENewLineMode.CR;
				HexOutput = false;
			}

			/// <summary>True if input characters should be echoed into the screen buffer</summary>
			public bool LocalEcho
			{
				get => get<bool>(nameof(LocalEcho));
				set => set(nameof(LocalEcho), value);
			}

			/// <summary>Get/Set the width of the terminal in columns</summary>
			public int TerminalWidth
			{
				get => get<int>(nameof(TerminalWidth));
				set => set(nameof(TerminalWidth), value);
			}

			/// <summary>Get/Set the height of the terminal in lines</summary>
			public int TerminalHeight
			{
				get => get<int>(nameof(TerminalHeight));
				set => set(nameof(TerminalHeight), value);
			}

			/// <summary>Get/Set the tab size in characters</summary>
			public int TabSize
			{
				get => get<int>(nameof(TabSize));
				set => set(nameof(TabSize), value);
			}

			/// <summary>Get/Set the newline mode for received newlines</summary>
			public ENewLineMode NewLineRecv
			{
				get => get<ENewLineMode>(nameof(NewLineRecv));
				set => set(nameof(NewLineRecv), value);
			}

			/// <summary>Get/Set the newline mode for sent newlines</summary>
			public ENewLineMode NewLineSend
			{
				get => get<ENewLineMode>(nameof(NewLineSend));
				set => set(nameof(NewLineSend), value);
			}

			/// <summary>Get/Set the received data being written has hex data into the buffer</summary>
			public bool HexOutput
			{
				get => get<bool>(nameof(HexOutput));
				set => set(nameof(HexOutput), value);
			}

			/// <summary>Type converter for displaying in a property grid</summary>
			private class TyConv : GenericTypeConverter<Settings> { }
		}

		/// <summary>New line modes</summary>
		public enum ENewLineMode { CR, LF, CR_LF, }

		/// <summary>Three bit colours with a bit for "high bright"</summary>
		public static class HBGR
		{
			public const byte Black = 0;
			public const byte Red = 1;
			public const byte Green = 2;
			public const byte Yellow = 3;
			public const byte Blue = 4;
			public const byte Magenta = 5;
			public const byte Cyan = 6;
			public const byte White = 7;

			/// <summary>Convert an HBGR colour to a Color</summary>
			public static Color ToColor(byte col)
			{
				switch (col)
				{
				default:
				case HBGR.Black: return Color.Black;
				case HBGR.Red: return Color.DarkRed;
				case HBGR.Green: return Color.DarkGreen;
				case HBGR.Yellow: return Color.Olive;
				case HBGR.Blue: return Color.DarkBlue;
				case HBGR.Magenta: return Color.DarkMagenta;
				case HBGR.Cyan: return Color.DarkCyan;
				case HBGR.White: return Color.Gray;
				case HBGR.Black | 0x8: return Color.Black;
				case HBGR.Red | 0x8: return Color.Red;
				case HBGR.Green | 0x8: return Color.Green;
				case HBGR.Yellow | 0x8: return Color.Yellow;
				case HBGR.Blue | 0x8: return Color.Blue;
				case HBGR.Magenta | 0x8: return Color.Magenta;
				case HBGR.Cyan | 0x8: return Color.Cyan;
				case HBGR.White | 0x8: return Color.White;
				}
			}
		}

		/// <summary>Per character style</summary>
		public struct Style
		{
			private struct bits
			{
				public const byte bold = 1 << 0;
				public const byte uline = 1 << 1;
				public const byte blink = 1 << 2;
				public const byte revs = 1 << 3;
				public const byte conceal = 1 << 4;
			}
			private byte m_col; // Fore/back colour (high-bright,blue,green,red)
			private byte m_sty; // bold, underline, etc...
			private bool get(byte b) { return (m_sty & b) != 0; }
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
				get { return new Style { m_col = 0x8F, m_sty = 0x00 }; }
			}

			public static bool operator ==(Style left, Style right)
			{
				return left.Equals(right);
			}
			public static bool operator !=(Style left, Style right)
			{
				return !(left == right);
			}
			public bool Equal(Style rhs)
			{
				return m_col == rhs.m_col && m_sty == rhs.m_sty;
			}
			public override bool Equals(object? obj)
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
			public char m_char;
			public Style m_styl;
			public Char(char ch, Style sty) { m_char = ch; m_styl = sty; }
		}

		/// <summary>A span of characters all with the same style</summary>
		public struct Span
		{
			public IString m_str;
			public Style m_sty;
			public Span(IString str, Style sty) { m_str = str; m_sty = sty; }
		}

		/// <summary>A row of characters making up the line</summary>
		public class Line
		{
			public StringBuilder m_line;
			public List<Style> m_styl;

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
					for (int s = 0, e = 0, end = Size; ;)
					{
						var sty = m_styl[s];
						for (e = s + 1; e != end && m_styl[e].Equal(sty); ++e) { }
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

		/// <summary>Buffer for user input.</summary>
		public class UserInput
		{
			// Note:
			//  - This class is used to buffer user input into batches that can then be send to the terminal server.
			//  - User input should never be rendered directly into the display, LocalEcho is handled at the UI level,
			//    otherwise the terminal server echos the user input if necessary.
			//  - This is really just a StringBuilder for bytes
			private byte[] m_buf;
			private int m_len;

			public UserInput(Settings settings, int capacity = 8192)
			{
				Settings = settings;
				m_buf = new byte[capacity != int.MaxValue ? capacity : 0];
				m_len = 0;
			}

			/// <summary>Buffer settings</summary>
			private Settings Settings { get; }

			/// <summary>The number of available user input bytes</summary>
			public int Length => m_len;

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

			/// <summary>Return access to the buffered user input</summary>
			public Span<byte> Peek => new Span<byte>(m_buf, 0, m_len);

			/// <summary>Return a whole line of buffered user input (including the 'NewLineSend' character(s)) or an empty span</summary>
			public Span<byte> PeekLine
			{
				get
				{
					switch (Settings.NewLineSend)
					{
						case ENewLineMode.LF:
						{
							var end = 0;
							for (; end != m_len && m_buf[end] != (byte)'\n'; ++end) { }
							return end != m_len ? new Span<byte>(m_buf, 0, end + 1) : Span<byte>.Empty;
						}
						case ENewLineMode.CR:
						{
							var end = 0;
							for (; end != m_len && m_buf[end] != (byte)'\r'; ++end) { }
							return end != m_len ? new Span<byte>(m_buf, 0, end + 1) : Span<byte>.Empty;
						}
						case ENewLineMode.CR_LF:
						{
							var end = 0;
							for (; end < m_len-1 && m_buf[end] != (byte)'\r' && m_buf[end+1] != (byte)'\n'; ++end) { }
							return end < m_len-1 ? new Span<byte>(m_buf, 0, end + 2) : Span<byte>.Empty;
						}
						default:
						{
							throw new Exception($"Unknown new line mode {Settings.NewLineSend}");
						}
					}
				}
			}

			/// <summary>Consume 'length' bytes from the user input</summary>
			public void Consume(int length)
			{
				if (length >= m_len)
					m_len = 0;
				else if (length > 0)
					Array.Copy(m_buf, length, m_buf, 0, m_len -= length);
			}

			/// <summary>Ensure 'm_buf' can hold 'size' bytes</summary>
			private void EnsureSpace(int size)
			{
				if (size <= m_buf.Length) return;
				Array.Resize(ref m_buf, m_buf.Length * 3 / 2);
			}
		}

		/// <summary>In-memory character/screen buffer</summary>
		public class Buffer
		{
			// Notes:
			//  - Render the buffer into a window by requesting a rectangular area from the buffer
			//  - The buffer is a virtual space of Settings.TerminalWidth/TerminalHeight area.
			//  - The virtual space becomes allocated space when characters are written or style
			//    set for the given character position. Line endings are not stored.

			/// <summary>The state of the terminal</summary>
			private struct State
			{
				public Point pos;
				public Style style;
				public static State Default
				{
					get { return new State { pos = Point.Empty, style = Style.Default }; }
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

			/// <summary>The current output caret state</summary>
			private State m_out;
			private State m_saved;

			public Buffer(Settings settings)
			{
				Settings = settings ?? new Settings();
				UserInput = new UserInput(Settings, 8192);
				m_seq = new StringBuilder();
				Lines = new List<Line>();
				m_state = State.Default;
				m_out = State.Default;
				ValidateRect();
			}

			/// <summary>Terminal settings</summary>
			public Settings Settings { get; }

			/// <summary>Return the number of lines of text in the control</summary>
			public int LineCount => Lines.Count;

			/// <summary>Access the lines in the buffer</summary>
			public List<Line> Lines { get; }

			/// <summary>The current position of the output caret</summary>
			public Point CaretPos => m_out.pos;

			/// <summary>The buffer region that has changed since ValidateRect() was last called</summary>
			public Rectangle InvalidRect { get; private set; }

			/// <summary>User input buffer</summary>
			public UserInput UserInput { get; }

			/// <summary>Add a single character to the user input buffer.</summary>
			public void AddInput(char c, bool notify = true)
			{
				if (c == '\n')
				{
					switch (Settings.NewLineSend)
					{
						case ENewLineMode.LF: UserInput.Add('\n'); break;
						case ENewLineMode.CR: UserInput.Add('\r'); break;
						case ENewLineMode.CR_LF: "\r\n".ForEach(UserInput.Add); break;
						default: throw new Exception($"Unknown new line mode {Settings.NewLineSend}");
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
			public bool AddInput(EKeyCodes vk)
			{
				switch (vk)
				{
					default:
						return false;

					case EKeyCodes.Left:
					case EKeyCodes.Right:
					case EKeyCodes.Up:
					case EKeyCodes.Down:
						return true; // ToDo: Send escape sequences for cursor control?

					case EKeyCodes.Back: AddInput('\b'); return true;
					case EKeyCodes.Delete: AddInput(" \b"); return true;
					case EKeyCodes.Return: AddInput('\n'); return true;
					case EKeyCodes.Tab: AddInput('\t'); return true;
				}
			}

			/// <summary>Raised when the user input buffer has data added</summary>
			public event EventHandler? InputAdded;
			protected virtual void OnInputAdded()
			{
				InputAdded?.Invoke(this, EventArgs.Empty);
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

				if (m_parsing) throw new Exception("Re-entrant call to 'Output'");
				using var scope = Scope.Create(() => m_parsing = true, () => m_parsing = false);

				if (Settings.HexOutput)
					OutputHex(text);
				else
					ParseOutput(text);
			}
			private int? m_output_thread;
			private bool m_parsing;

			/// <summary>Clear the entire buffer</summary>
			public void Clear()
			{
				// Invalidate the whole buffer
				InvalidateRect(new Rectangle(Point.Empty, new Size(Settings.TerminalWidth, LineCount)));

				Lines.Clear();
				m_out.pos = MoveCaret(0, 0);
				OnBufferChanged(new VT100BufferChangedEventArgs(Point.Empty, new Size(Settings.TerminalWidth, Settings.TerminalHeight)));
			}

			/// <summary>
			/// Call to read a rectangular area of text from the screen buffer
			/// Note, width is not a parameter, each returned line is a string
			/// up to TerminalWidth - x. Callers can decide the width.</summary>
			public IEnumerable<string> ReadTextArea(int x, int y, int height)
			{
				for (int j = y, jend = y + height; j != jend; ++j)
				{
					if (j == Lines.Count) yield break;
					var line = LineAt(j);
					yield return line.Substr(x);
				}
			}

			/// <summary>Raised whenever the buffer changes</summary>
			public event EventHandler<VT100BufferChangedEventArgs>? BufferChanged;
			protected virtual void OnBufferChanged(VT100BufferChangedEventArgs args)
			{
				BufferChanged?.Invoke(this, args);
			}

			/// <summary>Raised just before and just after buffer lines are dropped due to reaching the maximum Y-size</summary>
			public event EventHandler<VT100BufferOverflowEventArgs>? Overflow;
			protected virtual void OnOverflow(VT100BufferOverflowEventArgs args)
			{
				Overflow?.Invoke(this, args);
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

			/// <summary>True if capturing to file is currently enabled</summary>
			public bool CapturingToFile => m_capture_file != null;

			/// <summary>Start or stop capturing to a file</summary>
			public void CaptureToFile(string filepath, bool capture_all_data)
			{
				m_capture_file = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
				m_capture_all_data = capture_all_data;
			}
			public void CaptureToFileEnd()
			{
				Util.Dispose(ref m_capture_file);
			}
			private void Capture(string str, int ofs, int count, bool all_data)
			{
				if (m_capture_file == null) return;
				if (m_capture_all_data != all_data) return;

				var bytes = m_capture_all_data
					? str.ToBytes(ofs, count)
					: Encoding.UTF8.GetBytes(str.ToCharArray(ofs, Math.Min(count, str.Length - ofs)));

				m_capture_file.Write(bytes, 0, bytes.Length);
				m_capture_file.Flush();
			}
			private FileStream? m_capture_file;
			private bool m_capture_all_data;

			/// <summary>Send the contents of a file to the terminal</summary>
			public void SendFile(string filepath)
			{
				using var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				UserInput.Add(fs);
				OnInputAdded();
			}

			/// <summary>Return the line at 'y', allocating if needed</summary>
			private Line LineAt(int y)
			{
				if (Lines.Count <= y) Lines.Resize(y + 1, () => new Line());
				return Lines[y];
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
					Write(hex.Append(' ', 3 * (16 - i)).Append(" | ").Append(str).ToString());
					m_out.pos = MoveCaret(m_out.pos, -m_out.pos.X, 1);
				}
			}

			/// <summary>Parse the vt100 console text in 'text'</summary>
			private void ParseOutput(string text)
			{
				Capture(text, 0, text.Length, true);
				int s = 0, e = 0, eend = text.Length;
				for (; e != eend;)
				{
					char c = text[e];
					if (c == (char)EKeyCodes.Escape || m_seq.Length != 0)
					{
						Write(text, s, e - s);
						m_seq.Append(c);
						ParseEscapeSeq();
						++e;
						s = e;
					}
					else if (
						(c == '\n' && Settings.NewLineRecv == ENewLineMode.LF) ||
						(c == '\r' && Settings.NewLineRecv == ENewLineMode.CR) ||
						(c == '\r' && e + 1 != eend && text[e + 1] == '\n' && Settings.NewLineRecv == ENewLineMode.CR_LF))
					{
						Write(text, s, e - s);
						Capture("\n", 0, 1, false);
						m_out.pos = MoveCaret(m_out.pos, -m_out.pos.X, 1);
						e += Settings.NewLineRecv == ENewLineMode.CR_LF ? 2 : 1;
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

				case (char)EKeyCodes.Escape: // Double escape characters - reset to new escape sequence
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
								m_out.pos = MoveCaret(m_out.pos, +Math.Max(n[0], 1), 0);
								break;
							}
						#endregion

						case 'D':
							#region Esc[ValueD - Move cursor left n lines CUB
							{
								var n = Params(1, m_seq, 2, -1);
								m_out.pos = MoveCaret(m_out.pos, -Math.Max(n[0], 1), 0);
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
									if (m_out.pos.Y < Lines.Count)
									{
										Lines.Resize(m_out.pos.Y + 1, () => new Line());
										LineAt(m_out.pos.Y).Resize(m_out.pos.X);
									}
									break;
								case 1:
									// Esc[1J Clear screen from cursor up ED1
									{
										var num = Math.Min(Lines.Count, m_out.pos.Y);
										Lines.RemoveRange(0, num);
										if (Lines.Count != 0)
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
									if (m_out.pos.Y < Lines.Count)
									{
										LineAt(m_out.pos.Y).Resize(m_out.pos.X);
									}
									break;
								case 1:
									//Esc[1K Clear line from cursor left EL1
									if (m_out.pos.Y < Lines.Count)
									{
										LineAt(m_out.pos.Y).Erase(0, m_out.pos.X);
									}
									break;
								case 2:
									//Esc[2K Clear entire line EL2
									if (m_out.pos.Y < Lines.Count)
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

									case 30: m_out.style.ForeColour = (byte)(0x8 | HBGR.Black); break; // forecolour
									case 31: m_out.style.ForeColour = (byte)(0x8 | HBGR.Red); break;
									case 32: m_out.style.ForeColour = (byte)(0x8 | HBGR.Green); break;
									case 33: m_out.style.ForeColour = (byte)(0x8 | HBGR.Yellow); break;
									case 34: m_out.style.ForeColour = (byte)(0x8 | HBGR.Blue); break;
									case 35: m_out.style.ForeColour = (byte)(0x8 | HBGR.Magenta); break;
									case 36: m_out.style.ForeColour = (byte)(0x8 | HBGR.Cyan); break;
									case 37: m_out.style.ForeColour = (byte)(0x8 | HBGR.White); break;

									case 40: m_out.style.BackColour = (byte)(0x8 | HBGR.Black); break; // background color
									case 41: m_out.style.BackColour = (byte)(0x8 | HBGR.Red); break;
									case 42: m_out.style.BackColour = (byte)(0x8 | HBGR.Green); break;
									case 43: m_out.style.BackColour = (byte)(0x8 | HBGR.Yellow); break;
									case 44: m_out.style.BackColour = (byte)(0x8 | HBGR.Blue); break;
									case 45: m_out.style.BackColour = (byte)(0x8 | HBGR.Magenta); break;
									case 46: m_out.style.BackColour = (byte)(0x8 | HBGR.Cyan); break;
									case 47: m_out.style.BackColour = (byte)(0x8 | HBGR.White); break;
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
					m_out.pos = MoveCaret(m_out.pos, 0, -1);
					break;

				case 'B': //EscB Move cursor down one line CURSORDN
					m_out.pos = MoveCaret(m_out.pos, 0, +1);
					break;

				case 'C': //EscC Move cursor right one char CURSORRT
					m_out.pos = MoveCaret(m_out.pos, +1, 0);
					break;

				case 'D': //EscD Move cursor left one char CURSORLF
					m_out.pos = MoveCaret(m_out.pos, -1, 0);
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
				for (int e, s = beg; ;)
				{
					// Find the end of the next parameter
					for (e = s; e != end && param_string[e] != ';'; ++e) { }

					// Add the parameter
					int value;
					m_parms.Add(int.TryParse(param_string.ToString(s, e - s), out value) ? value : 0);

					if (e == end) break;
					s = e + 1;
				}
				for (; m_parms.Count < N;)
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
					OnOverflow(new VT100BufferOverflowEventArgs(true, new RangeI(0, 0 + count)));

					// Remove the lines and adjust anything that stores a line number
					Lines.RemoveRange(0, count);
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
						InvalidRect.X, Math.Max(0, InvalidRect.Y - count),
						InvalidRect.Width, InvalidRect.Height + Math.Min(0, InvalidRect.Y - count));

					OnOverflow(new VT100BufferOverflowEventArgs(false, new RangeI(0, 0 + count)));
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
					line.Write(Settings.TerminalWidth - 1, new string('~', 1), 0, 1, m_out.style);

				// Notify whenever the buffer is changed
				OnBufferChanged(new VT100BufferChangedEventArgs(m_out.pos, count));
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
				OnBufferChanged(new VT100BufferChangedEventArgs(new Point(x, y), line.Size - x));
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
			public static string TestTerminalText0
			{
				get =>
					"[2J[0m" +
					"[31m=========================\n" +
					"[30m     Terminal Test       \n" +
					"[31m=========================\n" +
					"[30m" +
					"\n\n> ";
			}
			public static string TestConsoleString0
			{
				get
				{
					// note: expects a tab size of 8
					return "[2J[0m"
						+ "[31m===============================\n"
						+ "[30m     terminal test string      \n"
						+ "[31m===============================\n"
						+ "[30m"
						+ "\n\n Live Actuator Details & Statistics\n"
						+ "------------------------------------\n\n"
						+ "[34m\t\t   #1\t   #2\t   #3\t   #4\t   #5\t   #6\t   #7\t   #8\t   #9\t   #10\n\n"
						+ "[30m[1m[30mActuator Senders\n"
						+ "[0mSender Active\n"
						+ "Extension (mm)\n"
						+ "AS Temperature\n"
						+ "AC Temperature\n"
						+ "Safe Settings\n"
						+ "Velocity\n"
						+ "\t\t7";
				}
			}
			public static string TestConsoleString1
			{
				get
				{
					return "[6A   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t"
						+ "8[5A  30.180  28.300  28.230  27.580   0.740   4.150  25.700  25.300  31.220  30.550"
						+ "8[4A   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t"
						+ "8[3A   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t   0°\t"
						+ "8[2A   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t   Y\t"
						+ "8[1A   0\t   0\t   0\t   0\t   0\t   0\t   0\t   0\t   0\t   0\t"
						+ "8";
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
	}

	#region EventArgs
	public class VT100BufferOverflowEventArgs : EventArgs
	{
		public VT100BufferOverflowEventArgs(bool before, RangeI dropped)
		{
			Before = before;
			Dropped = dropped;
		}

		/// <summary>True if the event is just before lines are dropped, false if just after</summary>
		public bool Before { get; private set; }

		/// <summary>The index range of lines that are dropped</summary>
		public RangeI Dropped { get; private set; }
	}
	public class VT100BufferChangedEventArgs : EventArgs
	{
		public VT100BufferChangedEventArgs(Rectangle area)
		{
			Area = area;
		}
		public VT100BufferChangedEventArgs(Point pt, Size sz)
			: this(new Rectangle(pt, sz))
		{ }
		public VT100BufferChangedEventArgs(int x, int y, int count)
			: this(new Rectangle(x, y, count, 1))
		{ }
		public VT100BufferChangedEventArgs(Point pt, int count)
			: this(pt.X, pt.Y, count)
		{ }

		/// <summary>The rectangular area that has changed</summary>
		public Rectangle Area { get; private set; }
	}
	#endregion
}