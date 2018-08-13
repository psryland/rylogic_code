using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Maths;

namespace Rylogic.Common
{
	public class ConsoleEx
	{
		public ConsoleEx()
		{
			AutoNewLine = true;
			IndentString = "  ";
			IndentLevel = 0;
			History = new List<string>();
			HistoryMaxLength = 1000;
			IsWordChar = c => char.IsLetterOrDigit(c) || c == '_';
			AutoComplete = null;
		}

		/// <summary>Control-C pressed event</summary>
		public event ConsoleCancelEventHandler CancelKeyPress
		{
			add { Console.CancelKeyPress += value; }
			remove { Console.CancelKeyPress -= value; }
		}

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

		/// <summary>Auto complete handler</summary>
		public IAutoComplete AutoComplete { get; set; }

		/// <summary>Cursor location</summary>
		public Point Location
		{
			get { return new Point(Console.CursorLeft, Console.CursorTop); }
			set { Console.CursorLeft = value.X; Console.CursorTop = value.Y; }
		}

		/// <summary>The console buffer size</summary>
		public Size BufferSize
		{
			get { return new Size(Console.BufferWidth, Console.BufferHeight); }
			set { Console.SetBufferSize(value.Width, value.Height); }
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

		/// <summary>Given a buffer location plus 'dx' and 'dy', return the normalised buffer location</summary>
		public Point NormaliseLocation(Point pt, int dx, int dy)
		{
			pt.X += dx;
			pt.Y += dy;

			var size = BufferSize;
			if (size == Size.Empty)
				return Point.Empty;

			for (; pt.X < 0 && pt.Y > 0; pt.X += size.Width, --pt.Y) { }
			for (; pt.X >= size.Width && pt.Y < size.Height; pt.X -= size.Width, ++pt.Y) { }
			pt.Y = Math_.Clamp(pt.Y, 0, size.Height - 1);

			return pt;
		}

		/// <summary>Erase text between 'tl' and 'br'. If 'rectangular' is true, the clear area is a rectangle, otherwise rows are cleared</summary>
		public void Clear(Point tl, Point br, bool rectangular)
		{
			// Normalise the top left, bottom right
			var x0 = Math.Min(tl.X, br.X);
			var x1 = Math.Max(tl.X, br.X);
			var y0 = Math.Min(tl.Y, br.Y);
			var y1 = Math.Max(tl.Y, br.Y);

			using (PreserveLocation())
			{
				// If clearing a single line
				if (y0 == y1)
				{
					Location = new Point(x0, y0);
					Console.Write(new string(' ', x1 - x0));
				}
				else
				{
					// Clear a rectangular area in the buffer
					if (rectangular)
					{
						for (int y = y0; y != y1; ++y)
						{
							Location = new Point(x0, y);
							Console.Write(new string(' ', x1 - x0));
						}
					}
					// Clear lines in the buffer
					else
					{
						Location = new Point(x0, y0);
						Console.Write(new string(' ', Console.BufferWidth - x0));
						for (int y = y0 + 1; y < y1; ++y)
						{
							Location = new Point(0, y);
							Console.Write(new string(' ', Console.BufferWidth));
						}
						Location = new Point(0, y1);
						Console.Write(new string(' ', x1));
					}
				}
			}
		}

		/// <summary>Write a string to the console</summary>
		public void Write(string msg)
		{
			// Breaks 'msg' into lines (without allocating the whole string again, a.k.a Split)
			for (int s = 0, e = 0; s != msg.Length; s = e != msg.Length ? e + 1 : e)
			{
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
				if (e != msg.Length)
					Console.WriteLine(line);
				else
					Console.Write(line);
			}
		}
		public void WriteLine(string msg)
		{
			Write(msg);
			Write("\n");
		}
		public void WriteLine()
		{
			WriteLine(string.Empty);
		}

		/// <summary>Display a prompt</summary>
		public void Prompt(string prompt)
		{
			// Move to the next line if not at the start of a new line
			if (AutoNewLine && Location.X > IndentLevel * IndentString.Length)
				WriteLine();

			// Display the prompt
			Write(prompt);
		}

		/// <summary>The next character from the input stream, or negative one (-1) if there are currently no more characters to be read.</summary>
		public int ReadChar()
		{
			return Console.Read();
		}

		/// <summary>Obtains the next character or function key pressed by the user. The pressed key is displayed in the console window.</summary>
		public ConsoleKeyInfo ReadKey()
		{
			return Console.ReadKey();
		}

		/// <summary>An object that describes the System.ConsoleKey constant and Unicode character, if any, that correspond to the pressed console key. The System.ConsoleKeyInfo object also describes, in a bitwise combination of System.ConsoleModifiers values, whether one or more Shift, Alt, or Ctrl modifier keys was pressed simultaneously with the console key.</summary>
		public ConsoleKeyInfo ReadKey(bool intercept)
		{
			return Console.ReadKey(intercept);
		}

		/// <summary>Read a line from the console, with auto-complete and history support</summary>
		public string Read()
		{
			return ReadAsync(CancellationToken.None).Result;
		}

		/// <summary>Async read a line from the console, with auto-complete and history support</summary>
		public async Task<string> ReadAsync(CancellationToken cancel = default(CancellationToken), bool newline_on_enter = true)
		{
			// Read up to the enter key being pressed
			var input = (string)null;
			for (var buf = new Buffer(this); input == null;)
			{
				// Record the cursor location before waiting for a key.
				var loc0 = Location;

				// Read a key from the user
				var key = await Task.Run(() => ReadKey(true), cancel);

				// If the cursor moves while waiting, apply the offset to the user input.
				var loc1 = Location;
				if (loc0 != loc1)
				{
					Prompt(string.Empty);
					buf.Origin = Location;
				}

				// Do auto completion
				if (DoAutoComplete(buf, key))
					continue;

				switch (key.Key)
				{
				default:
					{
						// Add a character to the user input buffer
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
						_history_index = (_history_index + History.Count - 1) % History.Count;
						buf.Assign(0, History[_history_index], true);
						break;
					}
				case ConsoleKey.DownArrow:
					{
						// '_history_index' is the offset from the end of the 'History' collection
						if (History.Count == 0) break;
						_history_index = (_history_index + History.Count + 1) % History.Count;
						buf.Assign(0, History[_history_index], true);
						break;
					}
				}
			}
			return input;
		}

		/// <summary>Add 'input' to the history buffer</summary>
		private void AddToHistory(string input)
		{
			// Don't add duplicates to the last history entry
			if (History.Count == 0 || History[History.Count - 1] != input)
				History.Add(input);

			// Limit the history length
			for (; History.Count > HistoryMaxLength;)
				History.RemoveAt(0);

			// Reset the history index
			_history_index = 0;
		}
		private int _history_index;

		/// <summary>Handle auto complete behaviour</summary>
		private bool DoAutoComplete(Buffer buf, ConsoleKeyInfo key)
		{
			// No auto complete, or control is pressed meaning ignore auto completion
			bool key_handled = false;
			if (AutoComplete == null || (key.Key == ConsoleKey.Tab && key.Modifiers == ConsoleModifiers.Control))
				return key_handled;

			// If there are no suggestions yet, query them from the callback
			if (key.Key == ConsoleKey.Tab && _suggestions == null)
			{
				// Read the index for the start of the current word
				_word_bounding_index = buf.Insert + buf.WordBoundaryOffset(-1);

				// Get the suggestions and result the index
				_suggestions = AutoComplete.Suggestions(buf.ToString(), _word_bounding_index, buf.Insert);
				_auto_complete_index = 0;
				key_handled = true;
			}
			// If auto complete is active cycle to the next suggestion
			else if (key.Key == ConsoleKey.Tab && _suggestions?.Length > 1)
			{
				// Tab cycles forward, Shift+Tab cycles backward through the suggestions
				var cycle_direction = key.Modifiers != ConsoleModifiers.Shift ? +1 : -1;
				_auto_complete_index = (_auto_complete_index + _suggestions.Length + cycle_direction) % _suggestions.Length;
				key_handled = true;
			}

			// Update 'buf' with the suggestion
			if (_suggestions?.Length > 0)
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
							_suggestions = null;
						}
						// Cycle completions
						else if (key.Key == ConsoleKey.Tab)
						{
							// Replace the full user input text
							buf.Assign(0, _suggestions[_auto_complete_index], true);
							key_handled = true;
						}
						// Commit
						else
						{
							buf.Assign(0, _suggestions[_auto_complete_index], true);
							key_handled = key.Key == ConsoleKey.RightArrow;
							_suggestions = null;
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
							_suggestions = null;
						}
						// Cycle completions
						else if (key.Key == ConsoleKey.Tab)
						{
							// Replace only the current word
							buf.Assign(_word_bounding_index, _suggestions[_auto_complete_index], false);
							key_handled = true;
						}
						// Commit
						else if (key.Key == ConsoleKey.Spacebar || key.Key == ConsoleKey.RightArrow || key.Key == ConsoleKey.End || key.Key == ConsoleKey.Enter)
						{
							buf.Assign(_word_bounding_index, _suggestions[_auto_complete_index], true);
							key_handled = true;
							_suggestions = null;
						}
						// Continue to match
						else if (!char.IsControl(key.KeyChar))
						{
							// Save the current suggestion
							var suggestion = _suggestions[_auto_complete_index];

							// Add the character to the user input
							buf.Write(key.KeyChar);
							key_handled = true;

							// Look for new suggestions
							_suggestions = AutoComplete.Suggestions(buf.ToString(), _word_bounding_index, buf.Insert);

							// Look for the previous suggestion in the new list
							if (_suggestions?.Length > 0)
							{
								var idx = Array.IndexOf(_suggestions, suggestion);
								_auto_complete_index = idx != -1 ? idx : 0;
								buf.Assign(_word_bounding_index, _suggestions[_auto_complete_index], false);
							}
							else
							{
								// Revert the user input to just what they've typed
								buf.Assign(buf.Insert, string.Empty, false);
								_suggestions = null;
							}
						}
						break;
					}
				}
			}

			// Deactivation auto complete if there are no suggestions
			if (_suggestions?.Length == 0)
				_suggestions = null;

			return key_handled;
		}
		private string[] _suggestions;
		private int _word_bounding_index;
		private int _auto_complete_index;

		/// <summary>Helper for tracking the input string within the console buffer</summary>
		private class Buffer
		{
			private ConsoleEx _console;  // Associated console
			private StringBuilder _line; // The line of user input
			private Point _origin;       // The start point in the console buffer for this string
			private Point _end;          // The end point in the console buffer for this string

			public Buffer(ConsoleEx console)
			{
				_console = console;
				_line = new StringBuilder();
				_origin = _console.Location;
				_end = _origin;
				Insert = 0;
			}

			/// <summary>The current insert position</summary>
			public int Insert { get; private set; }

			/// <summary>The number of characters in the input buffer</summary>
			public int Length
			{
				get { return _line.Length; }
				set
				{
					_line.Length = value;
					Insert = Math_.Clamp(Insert, 0, _line.Length);
					Update();
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

			/// <summary>Append 'text' from 'insert', erasing the rest of the line</summary>
			public void Assign(int index, string text, bool move_insert)
			{
				_line.Length = index;
				_line.Append(text);
				Insert = Math_.Clamp(move_insert ? _line.Length : Insert, 0, _line.Length);
				Update();
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
				Update();
			}
			public void Write(string text)
			{
				_line.Insert(Insert, text);
				Insert += text.Length;
				Update();
			}

			/// <summary>Delete characters from the input buffer. Use negative numbers to delete backward, positive to delete forward</summary>
			public void Delete(int direction_and_count)
			{
				var insert = Math_.Clamp(Insert + direction_and_count, 0, _line.Length);
				_line.Remove(Math.Min(insert, Insert), Math.Abs(Insert - insert));
				Insert = Math.Min(insert, Insert);
				Update();
			}

			/// <summary>Move the insert position. Use negative numbers for backward, positive numbers for forward</summary>
			public void Move(int direction_and_count)
			{
				// Measure the amount to move the cursor along the user input.
				var insert = Math_.Clamp(Insert + direction_and_count, 0, _line.Length);
				var diff = insert - Insert;
				Insert = insert;

				// Shift the cursor location
				_console.Location = _console.NormaliseLocation(_console.Location, diff, 0);
			}

			/// <summary>Get/Set a new origin position</summary>
			internal Point Origin
			{
				get { return _origin; }
				set
				{
					_console.Clear(_origin, _end, false);
					_origin = value;
					_end = _origin;
					Update();
				}
			}

			/// <summary>Write the user input into the console and update the cursor location</summary>
			private void Update()
			{
				// Clear the buffer
				_console.Clear(_origin, _end, false);

				// Start at '_origin'
				_console.Location = _origin;

				// Write the user input up to '_insert', then save the cursor location
				_console.Write(_line.ToString(0, Insert));
				var loc = _console.Location;

				// Write the rest of the user input
				_console.Write(_line.ToString(Insert, _line.Length - Insert));

				// Record the new end point
				_end = _console.Location;

				// Set the cursor to the '_insert' position
				_console.Location = loc;
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
			/// <param name="word_start">The index of the start of the word to the left of the cursor position</param>
			/// <param name="cursor">The index of the current cursor position</param>
			string[] Suggestions(string text, int word_start, int cursor);
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
			public string[] Suggestions(string text, int word_start, int cursor)
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
							.Where(x => string.Compare(x, 0, text, 0, cursor, true) == 0)
							.ToArray();
					}
				case ECompletionMode.Word:
					{
						return List
							.Where(x => string.Compare(x, 0, text, word_start, cursor - word_start, true) == 0)
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