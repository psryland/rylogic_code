using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace pr.gui_old
{
	/// <summary>
	/// VT terminal emulation control
	/// see: http://ascii-table.com/ansi-escape-sequences.php
	/// Usage:
	///   Add the control to a form.
	///   Use the 'GetUserInput()' method to retrieve user input (since last called)
	///   Send the user input to the remote device/system
	///   Receive data from the remote device/system
	///   Use 'Output()' to display the terminal output in the control</summary>
	public class VT100 :pr.gui.RichTextBox ,ISupportInitialize
	{
		/// <summary>The state of the terminal</summary>
		private class State
		{
			/// <summary>The output cursor location</summary>
			public Point m_cursor_location = new Point(0,0);

			/// <summary>The text attributes when last saved</summary>
			public Font m_saved_font = new Font(FontFamily.GenericMonospace, 10);

			/// <summary>The cursor position when last saved</summary>
			public Point m_saved_cursor_location = new Point(0,0);
		}

		/// <summary>Terminal settings</summary>
		public VT100Settings m_settings = new VT100Settings();

		/// <summary>The current control sequence</summary>
		public readonly StringBuilder m_seq = new StringBuilder();

		/// <summary>User input buffer</summary>
		private readonly StringBuilder m_input = new StringBuilder(8192);

		/// <summary>The state of the terminal</summary>
		private readonly State m_state = new State();

		/// <summary>Occurs when vt100 settings are changed</summary>
		public event Action<VT100> SettingsChanged;

		public VT100()
		{
			InitializeComponent();
			DoubleBuffered = true;
			Multiline = true;
			HideSelection = false;
			WordWrap = false;

			LocalEcho = true;
			TerminalWidth = 100;
			Font = m_state.m_saved_font;
			SelectionFont = m_state.m_saved_font;
			MouseUp += (s,e)=> { if (e.Button == MouseButtons.Right) {ShowContextMenu(e.Location);} };
		}

		/// <summary>Return the buffered user input</summary>
		public string GetUserInput() { return GetUserInput(true); }

		/// <summary>Return the buffered user input. 'clear' resets the buffer</summary>
		public string GetUserInput(bool clear)
		{
			string user_input = m_input.ToString();
			if (clear) { m_input.Length = 0; }
			return user_input;
		}

		/// <summary>A context menu for the vt100 terminal</summary>
		private void ShowContextMenu(Point location)
		{
			ContextMenuStrip menu = new ContextMenuStrip();
			{// Clear
				ToolStripMenuItem item = new ToolStripMenuItem{Text="Clear"};
				item.Click += (s,e)=> {Clear();};
				menu.Items.Add(item);
			}
			menu.Items.Add(new ToolStripSeparator());
			{// Copy
				ToolStripMenuItem item = new ToolStripMenuItem{Text="Copy"};
				item.Click += (s,e)=> {Copy();};
				menu.Items.Add(item);
			}
			{// Paste
				ToolStripMenuItem item = new ToolStripMenuItem{Text="Paste"};
				item.Click += (s,e)=> {Paste();};
				menu.Items.Add(item);
			}
			menu.Items.Add(new ToolStripSeparator());
			{// Terminal Options
				ToolStripMenuItem options = new ToolStripMenuItem{Text="Terminal Options"};
				{// Local echo
					ToolStripMenuItem item = new ToolStripMenuItem{Text="Local Echo", Checked=LocalEcho, CheckOnClick=true};
					item.Click += (s,e)=> { LocalEcho = item.Checked; };
					options.DropDownItems.Add(item);
				}
				{// Terminal width
					ToolStripMenuItem item = new ToolStripMenuItem{Text="Terminal Width"};
					ToolStripTextBox  edit = new ToolStripTextBox{Text=TerminalWidth.ToString()};
					edit.KeyDown   += (s,e)=> { if (e.KeyCode == Keys.Return) menu.Close(); };
					edit.LostFocus += (s,e)=> { int w; if (int.TryParse(edit.Text, out w)) {TerminalWidth = w;} };
					item.DropDownItems.Add(edit);
					options.DropDownItems.Add(item);
				}
				{// Tab size
					ToolStripMenuItem item = new ToolStripMenuItem{Text="Tab Size"};
					ToolStripTextBox  edit = new ToolStripTextBox{Text=TabSize.ToString()};
					edit.KeyDown   += (s,e)=> { if (e.KeyCode == Keys.Return) menu.Close(); };
					edit.LostFocus += (s,e)=> { int size; if (int.TryParse(edit.Text, out size)) {TabSize = size;} };
					item.DropDownItems.Add(edit);
					options.DropDownItems.Add(item);
				}
				{// newline receive
					ToolStripMenuItem item = new ToolStripMenuItem{Text="Newline Recv"};
					ToolStripComboBox edit = new ToolStripComboBox();
					edit.Items.AddRange(Enum.GetNames(typeof(VT100Settings.ENewLineMode)));
					edit.SelectedIndex = (int)NewlineRecv;
					edit.SelectedIndexChanged += (s,e)=> { NewlineRecv = (VT100Settings.ENewLineMode)edit.SelectedIndex; };
					item.DropDownItems.Add(edit);
					options.DropDownItems.Add(item);
				}
				{// newline send
					ToolStripMenuItem item = new ToolStripMenuItem{Text="Newline Send"};
					ToolStripComboBox edit = new ToolStripComboBox();
					edit.Items.AddRange(Enum.GetNames(typeof(VT100Settings.ENewLineMode)));
					edit.SelectedIndex = (int)NewlineSend;
					edit.SelectedIndexChanged += (s,e)=> { NewlineSend = (VT100Settings.ENewLineMode)edit.SelectedIndex; };
					item.DropDownItems.Add(edit);
					options.DropDownItems.Add(item);
				}
				{// Background colour
					ToolStripMenuItem item = new ToolStripMenuItem{Text="Background Colour"};
					ToolStripButton btn = new ToolStripButton{Text="   ", BackColor=BackColor, AutoToolTip=false};
					btn.Click += (s,e)=> { ColorDialog cd = new ColorDialog(); if (cd.ShowDialog() == DialogResult.OK) {BackColor = cd.Color;} menu.Close(); };
					item.DropDownItems.Add(btn);
					options.DropDownItems.Add(item);
				}
				{// Text colour
					ToolStripMenuItem item = new ToolStripMenuItem{Text="Text Colour"};
					ToolStripButton btn = new ToolStripButton{Text="   ", BackColor=ForeColor, AutoToolTip=false};
					btn.Click += (s,e)=> { ColorDialog cd = new ColorDialog(); if (cd.ShowDialog() == DialogResult.OK) {ForeColor = cd.Color;} menu.Close(); };
					item.DropDownItems.Add(btn);
					options.DropDownItems.Add(item);
				}
				{
					ToolStripMenuItem item = new ToolStripMenuItem{Text="Hex Output", Checked=HexOutput, CheckOnClick=true};
					item.Click += (s,e)=> { HexOutput = item.Checked; };
					options.DropDownItems.Add(item);
				}
				menu.Items.Add(options);
			}

			// Show the context menu
			menu.Show(this, location);
		}

		/// <summary>Get/Set the control background colour </summary>
		public override Color BackColor
		{
			get { return base.BackColor; }
			set { base.BackColor = value; SettingsChanged.Raise(this); }
		}

		/// <summary>Get/Set the control foreground colour </summary>
		public override Color ForeColor
		{
			get { return base.ForeColor; }
			set { base.ForeColor = value; SettingsChanged.Raise(this); }
		}

		/// <summary>True if user input is displayed in the terminal</summary>
		public bool LocalEcho
		{
			get { return m_settings.LocalEcho; }
			set { m_settings.LocalEcho = value; SettingsChanged.Raise(this); }
		}

		/// <summary>Get/Set the width of the terminal in columns</summary>
		public int TerminalWidth
		{
			get { return m_settings.TerminalWidth; }
			set { m_settings.TerminalWidth = Math.Max(value,1); SettingsChanged.Raise(this); }
		}

		/// <summary>Get/Set the tab size in characters</summary>
		public int TabSize
		{
			get { return m_settings.TabSize; }
			set { m_settings.TabSize = Math.Max(value,1); SettingsChanged.Raise(this); }
		}

		/// <summary>Get/Set the newline mode for received newlines</summary>
		public VT100Settings.ENewLineMode NewlineRecv
		{
			get { return m_settings.NewlineRecv; }
			set { m_settings.NewlineRecv = value; SettingsChanged.Raise(this); }
		}

		/// <summary>Get/Set the newline mode for sent newlines</summary>
		public VT100Settings.ENewLineMode NewlineSend
		{
			get { return m_settings.NewlineSend; }
			set { m_settings.NewlineSend = value; SettingsChanged.Raise(this); }
		}

		/// <summary>Output received data as hex</summary>
		public bool HexOutput
		{
			get { return m_settings.HexOutput; }
			set { m_settings.HexOutput = value; SettingsChanged.Raise(this); }
		}

		/// <summary>The cursor position for output text (not the same as the user selection cursor)</summary>
		public Point OutputCursorLocation
		{
			get { return m_state.m_cursor_location; }
			set { m_state.m_cursor_location = value; }
		}

		/// <summary>Paste clipboard contents into the terminal window</summary>
		public new void Paste()
		{
			string text = Clipboard.GetText();
			Input(text);
			if (LocalEcho) base.Paste();
		}

		/// <summary>Handle key down events</summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Control)
			{
				switch (e.KeyCode)
				{
				default:break;
				case Keys.V: Paste(); e.Handled = true; break;
				case Keys.X: Copy(); e.Handled = true; break;
				}
			}
			base.OnKeyDown(e);
		}

		/// <summary>Handle user input characters</summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			e.Handled = Input(e.KeyChar.ToString()) == 1 && !LocalEcho;
			base.OnKeyPress(e);
		}

		/// <summary>
		/// Add a string to the user input buffer
		/// Returns the number of characters added to the input buffer</summary>
		public int Input(string text)
		{
			// If the control is readonly, ignore all input
			if (ReadOnly)
			{
				m_input.Length = 0;
				return 0;
			}

			int count = 0;
			foreach (char c in text)
			{
				// Block input when the input buffer is full
				if (m_input.Length >= m_input.Capacity - 2) return count;

				// Add the user key to the input buffer
				switch ((Keys)c)
				{
				default: m_input.Append(c); break;
				case Keys.Return:
					switch (m_settings.NewlineSend){
					default: throw new ArgumentOutOfRangeException();
					case VT100Settings.ENewLineMode.CR:    m_input.Append('\r'); break;
					case VT100Settings.ENewLineMode.LF:    m_input.Append('\n'); break;
					case VT100Settings.ENewLineMode.CR_LF: m_input.Append("\r\n"); break;
					}break;
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
			if (!IsHandleCreated) return;
			if (InvokeRequired) { BeginInvoke(new Action<string>(Output), text); return; }

			using (this.SuspendRedraw(true))
			{
				CaretLocation = OutputCursorLocation;

				if (m_settings.HexOutput)
					OutputHex(text);
				else
					ParseOutput(text);

				OutputCursorLocation = CaretLocation;
			}
			ClearUndo();
		}

		/// <summary>Helper for writing a string to the control</summary>
		private void Write(string str)
		{
			Point loc = CaretLocation;
			SelectionLength = Math.Min(str.Length, LineLength(loc.Y, str.EndsWith("\n") || str.EndsWith("\r")) - loc.X);
			SelectedText = str;
		}

		/// <summary>Output the string 'text' as hex</summary>
		private void OutputHex(string text)
		{
			byte[] buf = Encoding.UTF8.GetBytes(text);
			var hex = new StringBuilder(3 * 16 + 2);
			var str = new StringBuilder(16 + 2);
			int i = 0;
			foreach (byte b in buf)
			{
				char c = char.IsControl((char)b) ? '.' : (char)b;
				hex.AppendFormat("{0:x2} ", b);
				str.Append(c);
				if (++i == 16)  { Write(hex.Append(" | ").Append(str).Append('\n').ToString()); hex.Length = 0; str.Length = 0; i = 0; }
			}
			if (i != 0) { Write(hex.Append(' ', 3*(16-i)).Append(" | ").Append(str).Append('\n').ToString()); }
		}

		/// <summary>Parse the vt100 console text in 'text'</summary>
		private void ParseOutput(string text)
		{
			int first = 0, last = 0, text_end = text.Length;
			for (;last != text_end; ++last)
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
					switch (m_settings.NewlineRecv){
						default:throw new ArgumentOutOfRangeException();
						case VT100Settings.ENewLineMode.CR:    str += "\r"; break;
						case VT100Settings.ENewLineMode.LF:    str += "\n"; break;
						case VT100Settings.ENewLineMode.CR_LF: str += "\r\n"; break;
					}
					Write(str);
					if (last+1 != text_end && ((c == '\n' && text[last+1] == '\r') || (c == '\r' && text[last+1] == '\n'))) ++last;
					first = last + 1;
				}
				else if (char.IsControl(c)) // ignore non printable text
				{
					if (first != last) Write(text.Substring(first, last - first));
					if (c == '\b') CaretLocation = MoveCursor(CaretLocation, -1, 0);
					if (c == '\r') CaretLocation = MoveCursor(CaretLocation, -CaretLocation.X, 0);
					if (c == '\t') Write("".PadRight(TabSize - (CaretLocation.X % TabSize), ' '));
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
				CaretLocation = MoveCursor(CaretLocation, 0,-1);
				break;
			case 'B': //EscB Move cursor down one line cursordn
				CaretLocation = MoveCursor(CaretLocation, 0,+1);
				break;
			case 'C': //EscC Move cursor right one char cursorrt
				CaretLocation = MoveCursor(CaretLocation, +1,0);
				break;
			case 'D': //EscD Move cursor left one char cursorlf
				CaretLocation = MoveCursor(CaretLocation, -1,0);
				break;

			//EscD	Move/scroll window up one line	IND
			//EscM	Move/scroll window down one line	RI
			//EscE	Move to next line	NEL

			case '7': //Esc7 Save cursor position and attributes DECSC
				m_state.m_saved_cursor_location = CaretLocation;
				m_state.m_saved_font = (Font)SelectionFont.Clone();
				break;
			case '8': //Esc8 Restore cursor position and attributes DECSC
				CaretLocation = m_state.m_saved_cursor_location;
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
				{ int[] n = Params(1, m_seq.ToString(2,m_seq.Length-3)); CaretLocation = MoveCursor(CaretLocation, 0,-Math.Max(n[0],1)); }
				break;
			case 'B': // Esc[ValueB Move cursor down n lines CUD
				{ int[] n = Params(1, m_seq.ToString(2,m_seq.Length-3)); CaretLocation = MoveCursor(CaretLocation, 0,+Math.Max(n[0],1)); }
				break;
			case 'C': // Esc[ValueC Move cursor right n lines CUF
				{ int[] n = Params(1, m_seq.ToString(2,m_seq.Length-3)); CaretLocation = MoveCursor(CaretLocation, +Math.Max(n[0],1),0); }
				break;
			case 'D': // Esc[ValueD Move cursor left n lines CUB
				{ int[] n = Params(1, m_seq.ToString(2,m_seq.Length-3)); CaretLocation = MoveCursor(CaretLocation, -Math.Max(n[0],1),0); }
				break;

			case 'f':
				//Esc[f             Move cursor to upper left corner hvhome
				//Esc[;f            Move cursor to upper left corner hvhome
				//Esc[Line;Columnf  Move cursor to screen location v,h CUP
				{ int[] n = Params(2, m_seq.ToString(2, m_seq.Length-3)); CaretLocation = MoveCursor(n[1], n[0]); }
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
				{ int[] n = Params(2, m_seq.ToString(2, m_seq.Length-3)); CaretLocation = MoveCursor(n[1], n[0]); }
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
					Range rg = IndexRangeFromLine(CurrentLineIndex, false);
					SelectionLength = (int)rg.End - SelectionStart;
					SelectedText = "";
					}break;
				case "\u001b[1K":{//Esc[1K Clear line from cursor left EL1
					Range rg = IndexRangeFromLine(CurrentLineIndex, false);
					rg.End = SelectionStart;
					SelectionStart = (int)rg.Begin;
					SelectionLength = (int)rg.Size;
					SelectedText = "";
					}break;
				case "\u001b[2K":{//Esc[2K Clear entire line EL2
					Range rg = IndexRangeFromLine(CurrentLineIndex, false);
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

		/// <summary>Get/Set the caret location</summary>
		public override Point CaretLocation
		{
			get { return base.CaretLocation; }
			set { base.CaretLocation = MoveCursor(value.X, value.Y); }
		}

		/// <summary>Move the cursor to an absolute position</summary>
		private Point MoveCursor(int x, int y)
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

		/// <summary>Get/Set the terminal settings</summary>
		[Browsable(false)] public VT100Settings Settings
		{
			get { return m_settings; }
			set
			{
				m_settings = value;
				base.BackColor = m_settings.BackColour;
				base.ForeColor = m_settings.ForeColour;
			}
		}

		#region C# designer code
		/// <summary>Required designer variable.</summary>
		private IContainer components;

		/// <summary>Clean up any resources being used.</summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null)) components.Dispose();
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			components = new Container();
		}

		public void BeginInit(){}
		public void EndInit(){}
		#endregion
	}

	/// <summary>Settings for the VT100 terminal</summary>
	[TypeConverter(typeof(VT100Settings.TyConv))]
	public class VT100Settings :SettingsSet<VT100Settings>
	{
		public enum ENewLineMode { CR, LF, CR_LF, }

		/// <summary>True if local characters are written into the control</summary>
		public bool LocalEcho
		{
			get { return get(x => x.LocalEcho); }
			set { set(x => x.LocalEcho, value); }
		}

		/// <summary>The tab width</summary>
		public int TabSize
		{
			get { return get(x => x.TabSize); }
			set { set(x => x.TabSize, value); }
		}

		/// <summary>The width of the terminal in characters</summary>
		public int TerminalWidth
		{
			get { return get(x => x.TerminalWidth); }
			set { set(x => x.TerminalWidth, value); }
		}

		/// <summary>Describes the format of received new lines</summary>
		public ENewLineMode NewlineRecv
		{
			get { return get(x => x.NewlineRecv); }
			set { set(x => x.NewlineRecv, value); }
		}

		/// <summary>Describes the format of sent new lines</summary>
		public ENewLineMode NewlineSend
		{
			get { return get(x => x.NewlineSend); }
			set { set(x => x.NewlineSend, value); }
		}

		/// <summary>The back colour for the control</summary>
		public Color BackColour
		{
			get { return get(x => x.BackColour); }
			set { set(x => x.BackColour, value); }
		}

		/// <summary>The fore colour for the control</summary>
		public Color ForeColour
		{
			get { return get(x => x.ForeColour); }
			set { set(x => x.ForeColour, value); }
		}

		/// <summary>Output hex data rather than parsing terminal data</summary>
		public bool HexOutput
		{
			get { return get(x => x.HexOutput); }
			set { set(x => x.HexOutput, value); }
		}

		public VT100Settings()
		{
			LocalEcho     = true;
			TabSize       = 8;
			TerminalWidth = 100;
			NewlineRecv   = ENewLineMode.LF;
			NewlineSend   = ENewLineMode.CR;
			BackColour    = Color.White;
			ForeColour    = Color.Black;
			HexOutput     = false;
		}

		/// <summary>Type converter for displaying in a property grid</summary>
		private class TyConv :GenericTypeConverter<VT100Settings> {}
	}
}
