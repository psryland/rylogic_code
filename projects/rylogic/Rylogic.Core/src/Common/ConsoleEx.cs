using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Common
{
	/// <summary>A replacement for System.Console with extra features for intending and auto complete</summary>
	public class ConsoleEx: IDisposable
	{
		private IDisposable m_suppress_echo;
		public ConsoleEx()
		{
			SynchroniseWrites = false;
			AutoNewLine = true;
			IndentString = "  ";
			IndentLevel = 0;
			History = new List<string>();
			HistoryMaxLength = 1000;
			IsWordChar = IsWordCharDefault;
			AutoComplete = null;

			// Workaround for difference in behaviour of Linux terminals
			m_suppress_echo = SuppressEcho();
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool _)
		{
			m_suppress_echo.Dispose();
		}

		/// <summary>Control-C pressed event</summary>
		public event ConsoleCancelEventHandler CancelKeyPress
		{
			add { Console.CancelKeyPress += value; }
			remove { Console.CancelKeyPress -= value; }
		}

		/// <summary>Raised whenever the console buffer is rolled by 1 line</summary>
		public event EventHandler? BufferRolled;

		/// <summary>Synchronise multi-threaded access with locks</summary>
		public bool SynchroniseWrites { get; set; }
		private object m_write_sync = new();

		/// <summary>Ensure the prompt is always printed on a new line</summary>
		public bool AutoNewLine { get; set; }

		/// <summary>The string inserted as an indent</summary>
		public string IndentString { get; set; }

		/// <summary>The current indent level</summary>
		public int IndentLevel { get; set; }

		/// <summary>Command line history</summary>
		public IList<string> History { get; private set; }

		/// <summary>The maximum length of the history buffer</summary>
		public int HistoryMaxLength { get; set; }

		/// <summary>Defines characters that are considered word characters (e.g. for Ctrl+Backspace)</summary>
		public Func<char, bool> IsWordChar { get; set; }
		public static bool IsWordCharDefault(char c) => char.IsLetterOrDigit(c) || c == '_';

		/// <summary>Auto complete handler</summary>
		public IAutoComplete? AutoComplete { get; set; }

		/// <summary>Caret location</summary>
		public Point Location
		{
			get { return new Point(Console.CursorLeft, Console.CursorTop); }
			set
			{
				if (BufferSize.IsEmpty) return;
				using (SyncWrites())
				{
					try
					{
						Console.SetCursorPosition(
							Math_.Clamp(value.X, 0, Console.BufferWidth - 1),
							Math_.Clamp(value.Y, 0, Console.BufferHeight - 1));
					}
					// There is no way to synchronise setting the caret location
					// with resizing the buffer so we just have to swallow the exceptions.
					catch (IOException) { }
				}
			}
		}

		/// <summary>The console buffer size</summary>
		public Size BufferSize
		{
			get { return new Size(Console.BufferWidth, Console.BufferHeight); }
			set { Console.SetBufferSize(value.Width, value.Height); }
		}

		/// <summary>The number of characters in the console buffer</summary>
		public int BufferLength => Console.BufferWidth * Console.BufferHeight;

		/// <summary>The display area of the console</summary>
		public Rectangle DisplayRect
		{
			get { return new Rectangle(Console.WindowLeft, Console.WindowTop, Console.WindowWidth, Console.WindowHeight); }
			set
			{
				Console.WindowWidth = value.Width;
				Console.WindowHeight = value.Height;
				Console.WindowLeft = value.Left;
				Console.WindowTop = value.Top;
			}
		}

		/// <summary>RAII object for preserving the caret location</summary>
		public IDisposable PreserveLocation()
		{
			return new LocationSave(this);
		}

		/// <summary>RAII scope for indenting output</summary>
		public IDisposable PreserveIndent()
		{
			return new IndentSave(this);
		}

		/// <summary>RAII scope for acquiring the write lock</summary>
		public IDisposable SyncWrites()
		{
			if (!SynchroniseWrites) return new Scope();
			return new WriteSync(this);
		}

		/// <summary>Return the number of columns and rows needed to display 'msg'</summary>
		public Size MeasureText(string msg)
		{
			int x = 0, y = 0, maxx = 0;
			foreach (var ch in msg)
			{
				if (ch == '\n') { ++y; x = 0; }
				else if (ch == '\t') x += 4;
				else ++x;
				maxx = Math.Max(x, maxx);
			}
			return new Size(maxx, y);
		}

		/// <summary>Given a buffer location plus 'dx' and 'dy', return the normalised buffer location</summary>
		public Point NormaliseLocation(Point pt, int dx = 0, int dy = 0)
		{
			var size = BufferSize;
			if (size == Size.Empty)
				return Point.Empty;

			var c = (pt.X + dx) + size.Width * (pt.Y + dy);

			if (c < 0)
				return Point.Empty;

			if (c >= size.Width * size.Height)
				return new Point(size.Width - 1, size.Height - 1);

			return new Point(c % size.Width, c / size.Width);
		}

		/// <summary>Erase text between 'tl' and 'br'. If 'rectangular' is true, the clear area is a rectangle, otherwise rows are cleared</summary>
		public void Clear(Point tl, Point br, bool rectangular)
		{
			if (tl == br)
				return;

			using (SyncWrites())
			using (PreserveLocation())
			{
				// If clearing a single line
				if (tl.Y == br.Y)
				{
					Location = new Point(Math.Min(tl.X, br.X), tl.Y);
					Console.Write(new string(' ', Math.Abs(br.X - tl.X)));
				}
				else
				{
					// Clear a rectangular area in the buffer
					if (rectangular)
					{
						// Normalise the top left, bottom right
						var x0 = Math.Min(tl.X, br.X);
						var x1 = Math.Max(tl.X, br.X);
						var y0 = Math.Min(tl.Y, br.Y);
						var y1 = Math.Max(tl.Y, br.Y);
						for (int y = y0; y != y1; ++y)
						{
							Location = new Point(x0, y);
							Console.Write(new string(' ', x1 - x0));
						}
					}
					// Clear lines in the buffer
					else
					{
						Location = tl;
						Console.Write(new string(' ', Console.BufferWidth - tl.X));
						for (int y = tl.Y + 1; y < br.Y; ++y)
						{
							Location = new Point(0, y);
							Console.Write(new string(' ', Console.BufferWidth));
						}
						Location = new Point(0, br.Y);
						Console.Write(new string(' ', br.X));
					}
				}
			}
		}

		/// <summary>Write a string to the console</summary>
		public void Write(string msg)
		{
			using (SyncWrites())
			{
				try
				{
					// Breaks 'msg' into lines (without allocating the whole string again, a.k.a Split)
					for (int s = 0, e = 0; s != msg.Length; s = e != msg.Length ? e + 1 : e)
					{
						var y = Console.CursorTop;

						// Find the line end
						for (e = s; e != msg.Length && msg[e] != '\n'; ++e) { }
						var line = msg.Substring(s, e - s);

						// Add indent
						if (Console.CursorLeft <= IndentLevel * IndentString.Length)
						{
							Console.CursorLeft = 0;
							for (int k = 0; k != IndentLevel; ++k)
								Console.Write(IndentString);
						}

						// Output the line
						if (e == msg.Length)
							Console.Write(line);
						else
							Console.WriteLine(line);

						// Notify of buffer roll over
						if (Console.CursorLeft == 0 && y == Console.BufferHeight - 1)
							BufferRolled?.Invoke(this, EventArgs.Empty);
					}
				}
				// These occur when the console is resized to zero in the middle of a write
				catch (IOException) { }
			}
		}
		public void WriteLine(string msg)
		{
			Write(msg+"\n");
		}
		public void WriteLine()
		{
			WriteLine(string.Empty);
		}

		/// <summary>Write a string at a given position in the console</summary>
		public void WriteAt(Point loc, string msg, Size? clear = null)
		{
			var pt = loc;
			using (SyncWrites())
			using (PreserveLocation())
			{
				// Reset the area
				if (clear != null)
					Clear(loc, loc + clear.Value, true);

				// Breaks 'msg' into lines (without allocating the whole string again, a.k.a Split)
				for (int s = 0, e = 0; s != msg.Length; s = e != msg.Length ? e + 1 : e)
				{
					// Find the line end
					for (e = s; e != msg.Length && msg[e] != '\n'; ++e) { }
					var line = msg.Substring(s, e - s);

					Location = pt;
					Write(line);

					++pt.Y;
				}
			}
		}

		/// <summary>Display a prompt</summary>
		public void Prompt(string prompt)
		{
			// Move to the next line if not at the start of a new line
			var nl = (AutoNewLine && Location.X > IndentLevel * IndentString.Length) ? "\n" : string.Empty;

			// Display the prompt
			Write(nl + prompt);
		}

		/// <summary>The next character from the input stream, or negative one (-1) if there are currently no more characters to be read.</summary>
		public int ReadChar()
		{
			return Console.Read();
		}

		/// <summary>Obtains the next character or function key pressed by the user. If 'intercept' is true, the pressed key is not displayed in the console window.</summary>
		public ConsoleKeyInfo ReadKey(bool intercept = false)
		{
			lock (m_read_key_lock)
			{
				var key = Console.ReadKey(intercept);
				return key;
			}
		}
		public bool TryReadKey(out ConsoleKeyInfo key, bool intercept = false)
		{
			key = default(ConsoleKeyInfo);
			lock (m_read_key_lock)
			{
				if (Console.KeyAvailable == false) return false;
				key = ReadKey(intercept);
				return true;
			}
		}
		public object m_read_key_lock = new();

		/// <summary>Read a line from the console, with auto-complete and history support</summary>
		public string Read()
		{
			return ReadAsync(CancellationToken.None).Result;
		}

		/// <summary>Async read a line from the console, with auto-complete and history support</summary>
		public async Task<string> ReadAsync(CancellationToken cancel = default, bool newline_on_enter = true)
		{
			// Read up to the enter key being pressed
			var input = (string?)null;
			using (var buf = new Buffer(this))
			{
				for (; input == null;)
				{
					var loc0 = Idx(Location);

					// Read a key from the user.
					// This loop prevents output being blocked while waiting for a key (bug on .Net Core for Linux)
					var key = default(ConsoleKeyInfo);
					for (; !cancel.IsCancellationRequested && !TryReadKey(out key, true); await Task.Delay(50, cancel)) { }
					if (cancel.IsCancellationRequested) break;

					// If the current caret location moves while waiting for input then something
					// has been written to the console during collecting the user input. On Linux,
					// characters are echoed to the terminal in-spite of 'intercept = true' so we
					// can't use the exact position. Settle for changes in Y only.
					var loc1 = Idx(Location);
					buf.MoveToNextLine |= (loc0 != loc1 && key.Key != ConsoleKey.Enter);

					// Do auto completion
					if (DoAutoComplete(buf, key))
						continue;

					switch (key.Key)
					{
						default:
							{
								// Add a character to the user input buffer
								if (!char.IsControl(key.KeyChar))
									buf.Write(key.KeyChar);
								break;
							}
						case ConsoleKey.Enter:
							{
								// Insert '\n' into the input buffer if control is pressed
								if (key.Modifiers == ConsoleModifiers.Control)
								{
									buf.Write(key.KeyChar);
									break;
								}
								// Complete the user input
								else
								{
									input = buf.ToString();

									// Add the input to the history buffer
									AddToHistory(input);

									// Optionally include the newline character
									if (newline_on_enter)
										WriteLine();

									break;
								}
							}
						case ConsoleKey.Escape:
							{
								// Clear user input
								buf.Assign(0, string.Empty, true);
								break;
							}
						case ConsoleKey.Backspace:
							{
								var count = -1;
								if (key.Modifiers == ConsoleModifiers.Control)
									count = buf.WordBoundaryOffset(count);

								buf.Delete(count);
								break;
							}
						case ConsoleKey.Delete:
							{
								var count = +1;
								if (key.Modifiers == ConsoleModifiers.Control)
									count = buf.WordBoundaryOffset(count);

								buf.Delete(count);
								break;
							}
						case ConsoleKey.LeftArrow:
							{
								var count = -1;
								if (key.Modifiers == ConsoleModifiers.Control)
									count = buf.WordBoundaryOffset(count);

								buf.Move(count);
								break;
							}
						case ConsoleKey.RightArrow:
							{
								var count = +1;
								if (key.Modifiers == ConsoleModifiers.Control)
									count = buf.WordBoundaryOffset(count);

								buf.Move(count);
								break;
							}
						case ConsoleKey.Home:
							{
								buf.Move(-buf.Length);
								break;
							}
						case ConsoleKey.End:
							{
								buf.Move(+buf.Length);
								break;
							}
						case ConsoleKey.UpArrow:
							{
								if (History.Count == 0) break;
								m_history_index = (m_history_index + History.Count - 1) % History.Count;
								buf.Assign(0, History[m_history_index], true);
								break;
							}
						case ConsoleKey.DownArrow:
							{
								if (History.Count == 0) break;
								m_history_index = (m_history_index + History.Count + 1) % History.Count;
								buf.Assign(0, History[m_history_index], true);
								break;
							}
						case ConsoleKey.PageUp:
							{
								if (History.Count == 0) break;
								m_history_index = (m_history_index + History.Count - 1) % History.Count;
								if (buf.Length != 0)
								{
									// Search backward in the history for a partial match for 'buf'
									for (int i = 0; i != History.Count; ++i)
									{
										if (History[m_history_index].StartsWith(buf.ToString(), StringComparison.InvariantCultureIgnoreCase)) break;
										m_history_index = (m_history_index + History.Count - 1) % History.Count;
									}
								}
								buf.Assign(0, History[m_history_index], true);
								break;
							}
						case ConsoleKey.PageDown:
							{
								if (History.Count == 0) break;
								m_history_index = (m_history_index + History.Count + 1) % History.Count;
								if (buf.Length != 0 && key.Modifiers == ConsoleModifiers.Control)
								{
									// Search forward in the history for a partial match for 'buf'
									for (int i = 0; i != History.Count; ++i)
									{
										if (History[m_history_index].StartsWith(buf.ToString(), StringComparison.InvariantCultureIgnoreCase)) break;
										m_history_index = (m_history_index + History.Count + 1) % History.Count;
									}
								}
								buf.Assign(0, History[m_history_index], true);
								break;
							}
					}
				}
			}
			return input ?? string.Empty;
		}

		/// <summary>Returns true if the Escape key has been pressed. Flushes console key buffer as a side effect</summary>
		public bool EscapePressed()
		{
			return TryReadKey(out var k) && k.Key == ConsoleKey.Escape;
		}

		/// <summary>Add 'input' to the history buffer</summary>
		private void AddToHistory(string input)
		{
			// Don't add duplicates to the last history entry
			if (!string.IsNullOrEmpty(input) && (History.Count == 0 || History[History.Count - 1] != input))
				History.Add(input);

			// Limit the history length
			for (; History.Count > HistoryMaxLength;)
				History.RemoveAt(0);

			// Reset the history index
			m_history_index = 0;
		}
		private int m_history_index;

		/// <summary>Handle auto complete behaviour</summary>
		private bool DoAutoComplete(Buffer buf, ConsoleKeyInfo key)
		{
			// No auto complete, or control is pressed meaning ignore auto completion
			bool key_handled = false;
			if (AutoComplete == null || (key.Key == ConsoleKey.Tab && key.Modifiers == ConsoleModifiers.Control))
				return key_handled;

			// If there are no suggestions yet, query them from the callback
			if (key.Key == ConsoleKey.Tab && m_suggestions == null)
			{
				// Read the index for the start of the current word
				m_word_bounding_index = buf.Insert + buf.WordBoundaryOffset(-1);

				// Get the suggestions and result the index
				m_suggestions = AutoComplete.Suggestions(buf.ToString(), m_word_bounding_index, buf.Insert);
				m_auto_complete_index = 0;
				key_handled = true;
			}
			// If auto complete is active cycle to the next suggestion
			else if (key.Key == ConsoleKey.Tab && m_suggestions?.Length > 1)
			{
				// Tab cycles forward, Shift+Tab cycles backward through the suggestions
				var cycle_direction = key.Modifiers != ConsoleModifiers.Shift ? +1 : -1;
				m_auto_complete_index = (m_auto_complete_index + m_suggestions.Length + cycle_direction) % m_suggestions.Length;
				key_handled = true;
			}

			// Update 'buf' with the suggestion
			if (m_suggestions?.Length > 0)
			{
				switch (AutoComplete.CompletionMode)
				{
					default: throw new Exception($"Unknown completion mode: {AutoComplete.CompletionMode}");
					case ECompletionMode.FullText:
						{
							// Cancel
							if (key.Key == ConsoleKey.Escape)
							{
								buf.Assign(0, string.Empty, true);
								key_handled = true;
								m_suggestions = null;
							}
							// Cycle completions
							else if (key.Key == ConsoleKey.Tab)
							{
								// Replace the full user input text
								buf.Assign(0, m_suggestions[m_auto_complete_index], true);
								key_handled = true;
							}
							// Commit
							else
							{
								buf.Assign(0, m_suggestions[m_auto_complete_index], true);
								key_handled = key.Key == ConsoleKey.RightArrow;
								m_suggestions = null;
							}
							break;
						}
					case ECompletionMode.Word:
						{
							// Cancel
							if (key.Key == ConsoleKey.Escape)
							{
								buf.Assign(buf.Insert, string.Empty, true);
								key_handled = true;
								m_suggestions = null;
							}
							// Cycle completions
							else if (key.Key == ConsoleKey.Tab)
							{
								// Replace only the current word
								buf.Assign(m_word_bounding_index, m_suggestions[m_auto_complete_index], false);
								key_handled = true;
							}
							// Commit
							else if (key.Key == ConsoleKey.Spacebar || key.Key == ConsoleKey.RightArrow || key.Key == ConsoleKey.End || key.Key == ConsoleKey.Enter)
							{
								buf.Assign(m_word_bounding_index, m_suggestions[m_auto_complete_index], true);
								key_handled = true;
								m_suggestions = null;
							}
							// Continue to match
							else if (!char.IsControl(key.KeyChar))
							{
								// Save the current suggestion
								var suggestion = m_suggestions[m_auto_complete_index];

								// Add the character to the user input
								buf.Write(key.KeyChar);
								key_handled = true;

								// Look for new suggestions
								m_suggestions = AutoComplete.Suggestions(buf.ToString(), m_word_bounding_index, buf.Insert);

								// Look for the previous suggestion in the new list
								if (m_suggestions?.Length > 0)
								{
									var idx = Array.IndexOf(m_suggestions, suggestion);
									m_auto_complete_index = idx != -1 ? idx : 0;
									buf.Assign(m_word_bounding_index, m_suggestions[m_auto_complete_index], false);
								}
								else
								{
									// Revert the user input to just what they've typed
									buf.Assign(buf.Insert, string.Empty, false);
									m_suggestions = null;
								}
							}
							break;
						}
				}
			}

			// Deactivation auto complete if there are no suggestions
			if (m_suggestions?.Length == 0)
				m_suggestions = null;

			return key_handled;
		}
		private string[]? m_suggestions;
		private int m_word_bounding_index;
		private int m_auto_complete_index;

		/// <summary>Convert a point to a console buffer index (not normalised)</summary>
		private static int Idx(Point pt, int? width = null)
		{
			return pt.X + pt.Y * (width ?? Console.BufferWidth);
		}

		/// <summary>Convert a console buffer index to a console point (not normalised)</summary>
		private static Point Pt(int idx, int? width = null)
		{
			// Note: Pt(Idx(pt)) != pt
			// This is because there is not a unique mapping from 2D points to 1D points (e.g. [0,0], [Width,-1], [2*Width,-2], etc all have index 0)
			var W = width ?? Console.BufferWidth;
			return idx >= 0
				? new Point(idx % W, idx / W)
				: new Point(-1 + W + (1 + idx) % W, -1 + (1 + idx) / W);
		}

		/// <summary>Suppress character echo</summary>
		private Scope SuppressEcho()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				// Preserve the terminal state
				m_stty ??= Util.ShellCmd("stty -g");
				return Scope.Create(
					() => Util.ShellCmd("stty -echo"),
					() => Util.ShellCmd($"stty {m_stty}"));
			}
			return new Scope();
		}
		private string? m_stty;

		/// <summary>Helper for tracking the input string within the console buffer</summary>
		private class Buffer :IDisposable
		{
			private ConsoleEx _console;  // Associated console
			private StringBuilder _line; // The line of user input
			private Size _last_size;     // The size of the console buffer last update
			private int _last_length;    // The length of '_line' after the last update
			private int _caret;          // The caret position as a 1D index into the console buffer
			private int _anchor;         // The position in the user input that should be positioned at '_caret' (= the value of 'Insert' after the last update)
			
			public Buffer(ConsoleEx console)
			{
				_console = console;
				_line = new StringBuilder();
				_caret = Idx(_console.Location);
				_last_size = _console.BufferSize;
				_last_length = 0;
			 	_anchor = 0;
				Insert = 0;
				_console.BufferRolled += HandleBufferRolled;
			}
			public void Dispose()
			{
				_console.BufferRolled -= HandleBufferRolled;
			}

			/// <summary>A flag used to indicate that the user input should be displayed on the next line</summary>
			public bool MoveToNextLine { get; set; }

			/// <summary>The current insert position into the line buffer</summary>
			public int Insert { get; private set; }

			/// <summary>The number of characters in the input buffer</summary>
			public int Length
			{
				get { return _line.Length; }
				set
				{
					_line.Length = value;
					Insert = Math_.Clamp(Insert, 0, _line.Length);
					Update(false);
				}
			}

			/// <summary>Access the input buffer by index</summary>
			public char this[int i]
			{
				get
				{
					// Allow 'i' == '_line.Length' so that this accessor can be used with '_insert'
					return i == _line.Length ? default(char) : _line[i];
				}
			}

			/// <summary>Copy 'text' into the input buffer starting at 'index', erasing the rest of the line</summary>
			public void Assign(int index, string text, bool move_insert)
			{
				var redraw = index < Insert;
				_line.Length = index;
				_line.Append(text);
				Insert = Math_.Clamp(move_insert ? _line.Length : Insert, 0, _line.Length);
				Update(redraw);
			}

			/// <summary>Returns the offset to a word boundary in the given direction. Going left returns the first character in a word, going right returns the first character of the next word</summary>
			public int WordBoundaryOffset(int direction)
			{
				var offset = 0;
				if (direction < 0)
				{
					// Find the first word char to the left, then the start of the word
					offset = Insert - 1;
					for (; offset >= 0 && !_console.IsWordChar(_line[offset]); --offset) { }
					for (; offset >= 0 && _console.IsWordChar(_line[offset]); --offset) { }
					offset = Math.Max(offset + 1, 0);
				}
				if (direction > 0)
				{
					// Find the next non-word char to the right, then the start of the next word
					offset = Insert + 1;
					for (; offset < _line.Length && _console.IsWordChar(_line[offset]); ++offset) { }
					for (; offset < _line.Length && !_console.IsWordChar(_line[offset]); ++offset) { }
					offset = Math.Min(offset, _line.Length);
				}
				return offset - Insert;
			}

			/// <summary>Write a character to the buffer</summary>
			public void Write(char c)
			{
				_line.Insert(Insert++, c);
				Update(false);
			}
			public void Write(string text)
			{
				_line.Insert(Insert, text);
				Insert += text.Length;
				Update(false);
			}

			/// <summary>Move the insert position. Use negative numbers for backward, positive numbers for forward</summary>
			public void Move(int direction_and_count)
			{
				// Measure the amount to move the caret along the user input.
				// Don't shortcut out of here if 'diff' is 0, the console moves the caret, so we need to move it back
				var insert = Math_.Clamp(Insert + direction_and_count, 0, _line.Length);
				var diff = insert - Insert;

				// Move the insert position
				Insert = insert;

				// Move the caret position to the insert position
				using (_console.SyncWrites())
				{
					// Set the new insert position
					_caret += diff;
					_anchor += diff;

					// If the move went beyond the buffer extent, do a full update.
					// Otherwise, just set the caret to the new location
					var buf_length = _console.BufferLength;
					if (_caret < 0 || _caret >= buf_length)
					{
						_caret = Math_.Clamp(_caret, 0, _console.BufferLength - 1);
						Update(true);
					}
					else
					{
						_console.Location = _console.NormaliseLocation(Pt(_caret));
					}
				}
			}

			/// <summary>Delete characters from the input buffer. Use negative numbers to delete backward, positive to delete forward</summary>
			public void Delete(int direction_and_count)
			{
				// Measure the amount to move the caret along the user input
				// Don't shortcut out of here if 'diff' is 0, the console moves the caret, so we need to move it back
				var insert = Math_.Clamp(Insert + direction_and_count, 0, _line.Length);
				var diff = insert - Insert;

				// Delete the user input, and move the insert position
				_line.Remove(Math.Min(insert, Insert), Math.Abs(diff));
				Insert = Math.Min(insert, Insert);
				diff = Math.Min(0, diff);

				// Move the caret position
				using (_console.SyncWrites())
				{
					// Set the new insert position
					_caret += diff;
					_anchor += diff;

					// If the move went beyond the buffer extent, do a full update.
					// Otherwise, just set the caret to the new location and do a fast update.
					var buf_length = _console.BufferLength;
					if (_caret < 0 || _caret >= buf_length)
					{
						_caret = Math_.Clamp(_caret, 0, _console.BufferLength - 1);
						Update(true);
					}
					else
					{
						_console.Location = _console.NormaliseLocation(Pt(_caret));
						Update(false);
					}
				}
			}

			/// <summary>
			/// Write the user input into the console and update the caret location.
			/// Set 'redraw' true if everything up to '_caret' is unchanged.</summary>
			private void Update(bool redraw)
			{
				// Notes:
				//  - It's much easier to treat the console buffer as a 1D array.
				//  - '_caret' is the position in the console buffer that corresponds to '_anchor'.
				//    Callers can move the visible portion of the user input by adjusting '_caret' and '_anchor'.
				//  - The console buffer may get resized.
				//  - Writing to the console can cause it to roll over.
				//  - There seems to be a weird Linux-only bug where pasting text that extends past
				//    the end of the console buffer does not cause it to automatically roll over.
				//    I haven't found a work-around for this, but it only seems to be a problem for paste,
				//    and the full string pasted is added to the user input buffer.
				//
				// Think of it like this:
				//                                    _caret
				//   Console Buffer:    [---------------V------------] (1D array)
				//   User Input:  (=====================^===I==)       (1D array)
				//                                   _anchor

				using (_console.SyncWrites())
				{
					var buf_length = _console.BufferLength;
					var buf_size = _console.BufferSize;
					if (buf_size.IsEmpty)
						return;

					// Determine if the whole user input needs redrawing
					var moved = MoveToNextLine;
					var resized = buf_size != _last_size;
					redraw |= moved | resized;

					// Erase the previous buffer text.
					// If 'redraw', clear from the start of the user input.
					// Otherwise, just erase from the insert position onwards.
					_console.Clear(
						_console.NormaliseLocation(Pt(_caret - (redraw ? _anchor : 0))),
						_console.NormaliseLocation(Pt(_caret + (_last_length - _anchor))),
						false);

					// If the caret was moved by something else, set '_caret' to the next new line
					if (resized)
					{
						var pt = Pt(_caret, _last_size.Width);
						_caret = Idx(pt);
					}
					if (moved)
					{
						var pt0 = Pt(_caret);
						var pt1 = _console.Location;
						pt0.Y = Math_.Clamp(pt1.Y + (pt1.X != 0 ? 1 : 0), 0, buf_size.Height - 1);
						_caret = Idx(pt0);
					}

					// Write the user input to the console buffer.
					// Always write up to 'Insert', even if it causes a roll of the buffer.
					// For text after the insert position, only write up to the end of the buffer.
					int origin = 0, beg = 0, len = 0;
					if (redraw)
					{
						// Where in the console buffer to start writing to
						origin = Math_.Clamp(_caret - _anchor, 0, buf_length - 1);

						// Where in the user input to read from
						beg = Math_.Clamp(_anchor - _caret, 0, _line.Length);

						// The length of user input to write
						// Clamp to within the console buffer, unless the 'Insert' position extends beyond it.
						len = Math_.Clamp(_line.Length - beg, 0, buf_length - origin - 1);
						len = Math.Max(len, Insert - beg);
					}
					// Otherwise, just write forwards from '_anchor'
					else
					{
						// Where in the console buffer to start writing to
						origin = Math_.Clamp(_caret, 0, buf_length - 1);

						// Where in the user input to read from 
						beg = Math_.Clamp(_anchor, 0, _line.Length);

						// The length of user input to write
						// Clamp to within the console buffer, unless the 'Insert' position extends beyond it.
						len = Math_.Clamp(_line.Length - beg, 0, buf_length - origin - 1);
						len = Math.Max(len, Insert - beg);
					}

					// Write to the console (this could invalidate 'origin')
					_console.Location = _console.NormaliseLocation(Pt(origin));
					_console.Write(_line.ToString(beg, len));

					// Move the caret position to match the 'Insert' position.
					_caret = Math_.Clamp(_caret + (Insert - _anchor), 0, buf_length - 1);
					_console.Location = _console.NormaliseLocation(Pt(_caret));
					MoveToNextLine = false;

					// Save the values used in this update
					_last_length = _line.Length;
					_last_size = buf_size;
					_anchor = Insert;
				}
			}

			/// <summary>Handle the console buffer rolling down one line</summary>
			private void HandleBufferRolled(object? sender, EventArgs e)
			{
				_caret -= _console.BufferSize.Width;
			}

			/// <summary>RAII object for handing buffer roll over</summary>
			private Scope SubscribeBufferRolled(EventHandler handler)
			{
				return Scope.Create(
					() => _console.BufferRolled += handler,
					() => _console.BufferRolled -= handler);
			}

			/// <summary>Return the input buffer as a string</summary>
			public override string ToString()
			{
				return _line.ToString();
			}
			public string ToString(int start, int length)
			{
				return _line.ToString(start, length);
			}
		}

		/// <summary>RAII object for preserving the caret location</summary>
		private class LocationSave : IDisposable
		{
			private readonly ConsoleEx _console;
			private readonly Point _location;
			public LocationSave(ConsoleEx console)
			{
				_console = console;
				_location = console.Location;
			}
			public void Dispose()
			{
				_console.Location = _location;
			}
		}

		/// <summary>RAII object for preserving the indent level</summary>
		private class IndentSave : IDisposable
		{
			private readonly ConsoleEx _console;
			private readonly int _indent;
			public IndentSave(ConsoleEx console)
			{
				_console = console;
				_indent = console.IndentLevel;
			}
			public void Dispose()
			{
				_console.IndentLevel = _indent;
			}
		}

		/// <summary>RAII object for synchronising writes</summary>
		private class WriteSync :IDisposable
		{
			private readonly object _sync;
			private readonly bool _release;
			public WriteSync(object sync)
			{
				_release = false;
				Monitor.Enter(_sync = sync, ref _release);
			}
			public void Dispose()
			{
				if (!_release) return;
				Monitor.Exit(_sync);
			}
		}

		/// <summary>Completion behaviour</summary>
		public enum ECompletionMode
		{
			/// <summary>Suggestions should replace the entire user input text</summary>
			FullText,

			/// <summary>Suggestions should only replace the last word of the user input text</summary>
			Word,
		}

		/// <summary>Auto completion handler interface</summary>
		public interface IAutoComplete
		{
			/// <summary>How the results of 'Suggestions' should be used</summary>
			ECompletionMode CompletionMode { get; }

			/// <summary>Return the list of possible completions</summary>
			/// <param name="text">The full line of user input text</param>
			/// <param name="word_start">The index of the start of the word to the left of the caret position</param>
			/// <param name="caret">The index of the current caret position</param>
			string[] Suggestions(string text, int word_start, int caret);
		}

		/// <summary>A simple auto complete handler constructed from a list</summary>
		public class AutoCompleteList : IAutoComplete
		{
			public AutoCompleteList(ECompletionMode mode, IList<string> words)
			{
				CompletionMode = mode;
				List = words;
			}

			/// <summary>The completion mode to use</summary>
			public ECompletionMode CompletionMode { get; private set; }

			/// <summary>The set of completions</summary>
			public IList<string> List { get; private set; }

			/// <summary>Provide the suggestions from 'List'</summary>
			public string[] Suggestions(string text, int word_start, int caret)
			{
				switch (CompletionMode)
				{
					default:
						{
							throw new Exception($"Unknown completion mode: {CompletionMode}");
						}
					case ECompletionMode.FullText:
						{
							return List
								.Where(x => string.Compare(x, 0, text, 0, caret, true) == 0)
								.ToArray();
						}
					case ECompletionMode.Word:
						{
							return List
								.Where(x => string.Compare(x, 0, text, word_start, caret - word_start, true) == 0)
								.ToArray();
						}
				}
			}
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Common;

	[TestFixture]
	public class TestConsoleEx
	{
		//[Test]
		public async void Run()
		{
			var Console = new ConsoleEx
			{
				IndentString = ".",
				AutoComplete = new ConsoleEx.AutoCompleteList(ConsoleEx.ECompletionMode.Word, new List<string>
				{
					"Apple", "Approval", "A-Hole",
					"Boris", "Bottom", "Butt",
					"Cheese", "Cheers", "Chuck",
				}),
			};

			Console.IndentLevel = 0;
			Console.Write("Hello");
			Console.Write(" World");
			Console.WriteLine();
			Console.IndentLevel = 3;
			Console.WriteLine("Hello World");
			Console.IndentLevel = 1;
			Console.Write("Hello\nWorld\n");
			Console.WriteLine("Done");

			for (; ; )
			{
				Console.Prompt(">");
				var cmd_line = await Console.ReadAsync(CancellationToken.None);
				if (cmd_line.ToLower() == "exit")
					break;
			}
		}
	}
}
#endif