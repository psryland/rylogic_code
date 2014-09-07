//***********************************************
// VT100 Teminal
//  Copyright (c) Rylogic Ltd 2014
//***********************************************
#pragma once

#include <deque>
#include <vector>
#include <string>
#include <algorithm>
#include <cassert>

namespace pr
{
	// Represents a screen buffer for a VT100 terminal.
	// see: http://ascii-table.com/ansi-escape-sequences.php
	// Render the buffer into a window by requesting a rectangular area from the buffer
	// The buffer is a virtual space of Settings.m_width/m_height area. The virtual space
	// becomes allocated space when characters are written or style set for the given character
	// position.
	// Line endings are not stored
	struct VT100
	{
		typedef unsigned char uint8_t;
		enum class ENewLineMode { CR, LF, CR_LF };
		enum class EColour { Black = 0, Red, Green, Yellow, Blue, Magenta, Cyan, White }; // 3bit colours

		struct Style
		{
		private:
			uint8_t m_col; // fore/back colour (highbright,blue,green,red)
			uint8_t m_sty; // bold, underline, etc..

			enum class bits
			{
				bold    = 1 << 0,
				uline   = 1 << 1,
				blink   = 1 << 2,
				revs    = 1 << 3,
				conceal = 1 << 4,
			};
			bool get(bits b) const    { return (m_sty & uint8_t(b)) != 0; }
			void set(bits b, bool on) { on ? m_sty |= uint8_t(b) : m_sty &= ~uint8_t(b); }

		public:
			// Get/Set 4-bit background colour
			uint8_t BackColour() const { return (m_col >> 0) & 0xF; }
			void BackColour(uint8_t c) { m_col &= 0xF0; m_col |= (c << 0); }

			// Get/Set 4-bit forecolour
			uint8_t ForeColour() const { return (m_col >> 4) & 0xF; }
			void ForeColour(uint8_t c) { m_col &= 0x0F; m_col |= (c << 4); }

			// Get/Set bold mode
			bool Bold() const  { return get(bits::bold); }
			void Bold(bool on) { set(bits::bold, on); }

			// Get/Set underline mode
			bool Underline() const  { return get(bits::uline); }
			void Underline(bool on) { set(bits::uline, on); }

			// Get/Set blink mode
			bool Blink() const  { return get(bits::blink); }
			void Blink(bool on) { set(bits::blink, on); }

			// Get/Set Reverse video mode
			bool ReverseVideo() const  { return get(bits::revs); }
			void ReverseVideo(bool on) { set(bits::revs, on); }

			// Get/Set Concealed mode
			bool Concealed() const  { return get(bits::conceal); }
			void Concealed(bool on) { set(bits::conceal, on); }

			Style() :m_col(0x8F) ,m_sty(0x00) {}
		};

		struct Settings
		{
			// The size of the terminal buffer
			size_t m_width;
			size_t m_height;

			// The tab size in characters
			size_t m_tab_size;

			// True if input characters should be echoed into the screen buffer
			bool m_local_echo;

			// Send/Recv newline modes
			ENewLineMode m_recv_newline;
			ENewLineMode m_send_newline;

			// Blocks user input if true
			bool m_readonly;

			// Input the size of the input buffer
			size_t m_input_buffer_size;

			Settings(size_t width = 100, size_t height = 50, size_t tab_size = 4, bool local_echo = false, ENewLineMode recv_newline = ENewLineMode::CR, ENewLineMode send_newline = ENewLineMode::CR, bool readonly = false)
				:m_width(width)
				,m_height(height)
				,m_tab_size(tab_size)
				,m_local_echo(local_echo)
				,m_recv_newline(recv_newline)
				,m_send_newline(send_newline)
				,m_readonly(readonly)
				,m_input_buffer_size(~0U)
			{}
		};

	private:

		struct CaretPosition
		{
			int X,Y;
			CaretPosition(int x = 0, int y = 0) :X(x) ,Y(y) {}
		};

		// The caret/style state
		struct State
		{
			CaretPosition pos;
			Style style;
			State() :pos() ,style() {}
		};

		// A row of characters making up the line
		struct Line
		{
			typedef std::string text_t;
			typedef std::basic_string<Style> styl_t;
			text_t m_line;
			styl_t m_styl;

			// Return a pointer to the null terminated line string
			char const* c_str(int ofs = 0) const
			{
				return m_line.c_str() + std::min(ofs, int(m_line.size()));
			}

			// The length of the line
			size_t size() const
			{
				return m_line.size();
			}

			// Set the line size
			void resize(size_t newsize, char fill, Style style)
			{
				m_line.resize(newsize, fill);
				m_styl.resize(newsize, style);
			}
			
			// Erase a range within the line
			void erase(size_t ofs, size_t count)
			{
				if (ofs >= m_line.size()) return;
				auto len = std::min(count, m_line.size() - ofs);
				m_line.erase(ofs, len);
				m_styl.erase(ofs, len);
			}

			// Write into/over this line from 'ofs'
			void write(int ofs, char const* str, size_t count, Style style)
			{
				if (m_line.size() <= ofs + count)
				{
					m_line.resize(ofs + count);
					m_styl.resize(ofs + count);
				}
				std::copy(str, str + count, &m_line[ofs]);
				std::fill(&m_styl[ofs], &m_styl[ofs] + count, style);
			}
		};

		// The terminal buffer
		typedef std::deque<Line> Buffer;
		Buffer m_lines;

		// Buffered user input to the terminal
		std::string m_input;

		// Terminal settings
		Settings m_settings;

		// The current output caret state
		State m_out;
		State m_saved;

		// The current control sequence
		std::string m_seq;

		// The line/char to return for constant access in virtual buffer space
		const Line m_nullline;

	public:

		VT100(Settings settings = Settings())
			:m_lines()
			,m_input()
			,m_settings(settings)
			,m_out()
			,m_saved()
			,m_seq()
			,m_nullline()
		{}

		// Access to the settings
		Settings& settings()
		{
			return m_settings;
		}

		// The number of lines
		size_t LineCount() const { return m_lines.size(); }

		// Tab size in characters
		size_t TabSize() const { return m_settings.m_tab_size; }
		
		// The width/height of the terminal buffer
		size_t Width() const { return m_settings.m_width; }
		size_t Height() const { return m_settings.m_height; }

		// Get/Set user input.
		// Get returns the buffer and optionally clears
		// Set returns the number of characters added to the input buffer
		std::string const& UserInput() const
		{
			return m_input;
		}
		std::string UserInput(bool clear)
		{
			return std::move(m_input);
		}
		size_t AddInput(char const* text)
		{
			// If the control is readonly, ignore all input
			if (m_settings.m_readonly)
				return 0;

			int count = 0;
			for (auto p = text; *p; ++p, ++count)
			{
				// Block input when the input buffer is full
				if (m_input.size() + 2 >= m_settings.m_input_buffer_size)
					return count;

				// Add the user key to the input buffer
				switch (*p)
				{
				default:
					m_input.push_back(*p);
					break;
				case '\r':
					break;
				case '\n':
					switch (m_settings.m_send_newline)
					{
					default: assert(false && "unknown send newline mode"); break;
					case ENewLineMode::CR:    m_input.push_back('\r'); break;
					case ENewLineMode::LF:    m_input.push_back('\n'); break;
					case ENewLineMode::CR_LF: m_input.append("\r\n"); break;
					}
					break;
				}
			}
			return count;
		}

		// Writes 'text' into the screen buffer at the current position.
		// Parses the text for vt100 control sequences.
		void Output(char const* text)
		{
			auto caret = m_out.pos;
			ParseOutput(text);
			m_out.pos = caret;
		}

		// Clear the entire buffer
		void Clear()
		{
			m_lines.resize(0);
			m_out.pos = MoveCaret(0,0);
		}

		// Call to read a rectangular area of text from the screen buffer
		// Note, width is not a parameter, each returned line is a null
		// terminated string. Callers can decide the width
		template <typename Out> void ReadTextArea(int x, int y, int height, Out out) const
		{
			for (int j = y, jend = y + height; j != jend; ++j)
			{
				auto const& line = LineAt(j);
				out(line.str(x));
			}
		}

	private:

		enum class Keys { Escape = 27 };

		// Returns true if 'c' is a control char
		inline bool IsControl(char c) const
		{
			return (c >= 0x00 && c <= 0x1f) || (c >= 0x80 && c <= 0x9F);
		}

		// Clamp x to [min,max]
		template <typename Int> Int Clamp(Int x, Int min, Int max)
		{
			return x < min ? min : x > max ? max : x;
		}

		// True if the current caret position is in virtual space
		bool IsVirtual(CaretPosition pos) const
		{
			return pos.Y >= m_lines.size() || pos.X >= LineAt(pos.Y).size();
		}

		// Converts a hbgr colour to 0xFFrrggbb (32 bit colour)
		static unsigned int HBGR(int hbgr)
		{
			auto i = (hbgr & 0x8) != 0 ? 0x80 : 0xFF;
			unsigned int c = 0;
			if (hbgr & 1) c |= i; i <<= 8;
			if (hbgr & 2) c |= i; i <<= 8;
			if (hbgr & 4) c |= i; i <<= 8;
			return c |= i;
		}

		// Return the line at 'y'
		Line const& LineAt(int y) const
		{
			return m_lines.size() > y ? m_lines[y] : m_nullline;
		}
		Line& LineAt(int y)
		{
			if (m_lines.size() <= y) m_lines.resize(y+1);
			return m_lines[y];
		}

		//Parse the vt100 console text in 'text'
		void ParseOutput(char const* text)
		{
			char const *s = text, *e = text;
			for (;*e;)
			{
				if (*e == (char)Keys::Escape || !m_seq.empty())
				{
					Write(s, e - s);
					m_seq.push_back(*e);
					ParseEscapeSeq();
					++e;
					s = e;
				}
				else if (
					(*e == '\n' && m_settings.m_recv_newline == ENewLineMode::LF) ||
					(*e == '\r' && m_settings.m_recv_newline == ENewLineMode::CR) ||
					(*e == '\r' && *(e+1) == '\n' && m_settings.m_recv_newline == ENewLineMode::CR_LF))
				{
					Write(s, e - s);
					m_out.pos = MoveCaret(m_out.pos, -m_out.pos.X, 1);
					e += m_settings.m_recv_newline == ENewLineMode::CR_LF ? 2 : 1;
					s = e;
				}
				else if (IsControl(*e)) // ignore non printable text
				{
					Write(s, e - s);
					if (*e == '\b') m_out.pos = MoveCaret(m_out.pos, -1, 0);
					if (*e == '\r') m_out.pos = MoveCaret(m_out.pos, -m_out.pos.X, 0);
					if (*e == '\n') m_out.pos = MoveCaret(m_out.pos, 0, 1);
					if (*e == '\t') for (auto pad = TabSize() - (m_out.pos.X % TabSize()); pad-- != 0;) Write(" ");
					++e;
					s = e;
				}
			}

			// Print any remaining printable text
			Write(s);
		}

		// Parses a stream of characters as a vt100 control sequence.
		void ParseEscapeSeq()
		{
			if (m_seq.size() <= 1)
				return;

			switch (m_seq[1])
			{
			default:
				// unknown escape sequence
				assert(false && "Unknown escape sequence found");
				break;
				
			case (char)Keys::Escape: // Double escape characters - reset to new escape sequence
				m_seq.resize(1);
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

			//EscD	Move/scroll window up one line	IND
			//EscM	Move/scroll window down one line	RI
			//EscE	Move to next line	NEL

			case '7': //Esc7 Save cursor position and attributes DECSC
				m_saved = m_out;
				break;
			case '8': //Esc8 Restore cursor position and attributes DECSC
				m_out = m_saved;
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
			m_seq.resize(0);
		}

		// Parse escape codes beginning with "Esc["
		void ParseEscapeSeq0()
		{
			auto code = m_seq.back(); m_seq.pop_back();
			auto field = m_seq.c_str() + 2; // skip the Esc[
			switch (code)
			{
			default: // Incomplete escape sequence
				return;

			case 'A': // Esc[ValueA Move cursor up n lines CUU
				{
					int n[1];
					Params(n, field);
					m_out.pos = MoveCaret(m_out.pos, 0, -std::max(n[0], 1));
				}
				break;
			case 'B': // Esc[ValueB Move cursor down n lines CUD
				{
					int n[1];
					Params(n, field);
					m_out.pos = MoveCaret(m_out.pos, 0, +std::max(n[0], 1));
				}
				break;
			case 'C': // Esc[ValueC Move cursor right n lines CUF
				{
					int n[1];
					Params(n, field);
					m_out.pos = MoveCaret(m_out.pos, +std::max(n[0], 1), 0);
				}
				break;
			case 'D': // Esc[ValueD Move cursor left n lines CUB
				{
					int n[1];
					Params(n, field);
					m_out.pos = MoveCaret(m_out.pos, -std::max(n[0], 1), 0);
				}
				break;
			case 'f':
				//Esc[f             Move cursor to upper left corner hvhome
				//Esc[;f            Move cursor to upper left corner hvhome
				//Esc[Line;Columnf  Move cursor to screen location v,h CUP
				{
					int n[2];
					Params(n, field);
					m_out.pos = MoveCaret(n[1], n[0]);
				}
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
				{
					int n[2];
					Params(n, field);
					m_out.pos = MoveCaret(n[1], n[0]);
				}
				break;
			case 'h':
				{
					int n[1];
					Params(n, field);
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
				}
				break;
			case 'J':
				{
					int n[1];
					Params(n, field);
					switch (n[0])
					{
					case 0:
						// Esc[J  Clear screen from cursor down ED0
						// Esc[0J Clear screen from cursor down ED0
						if (m_out.pos.Y < m_lines.size())
						{
							m_lines.resize(m_out.pos.Y + 1);
							LineAt(m_out.pos.Y).resize(m_out.pos.X, 0, m_out.style);
						}
						break;
					case 1:
						// Esc[1J Clear screen from cursor up ED1
						{
							auto end = std::begin(m_lines) + std::min(int(m_lines.size()), m_out.pos.Y);
							m_lines.erase(std::begin(m_lines), end);
							if (!m_lines.empty())
								LineAt(0).erase(0, m_out.pos.X);
						}
						break;
					case 2:
						// Esc[2J Clear entire screen ED2
						Clear();
						break;
					}
				}
				break;
			case 'K':
				{
					int n[1];
					Params(n, field);
					switch (n[0])
					{
					case 0:
						//Esc[K  Clear line from cursor right EL0
						//Esc[0K Clear line from cursor right EL0
						if (m_out.pos.Y < m_lines.size())
						{
							LineAt(m_out.pos.Y).resize(m_out.pos.X, 0, m_out.style);
						}
						break;
					case 1:
						//Esc[1K Clear line from cursor left EL1
						if (m_out.pos.Y < m_lines.size())
						{
							LineAt(m_out.pos.Y).erase(0, m_out.pos.X);
						}
						break;
					case 2:
						//Esc[2K Clear entire line EL2
						if (m_out.pos.Y < m_lines.size())
						{
							LineAt(m_out.pos.Y).resize(0, 0, m_out.style);
						}
						break;
					}
				}
				break;
			case 'l':
				{
					int n[1];
					Params(n, field);
					switch (n[0])
					{
					case 20:
						// Esc[20l Set line feed mode LMN
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
				}
				break;
			case 'm': // Esc[value;..;valuem 
				{
					int modes[5];
					Params(modes, field);
					for (auto n : modes)
					{
						switch (n)
						{
						case 0:
							// Esc[m Turn off character attributes SGR0
							// Esc[0m Turn off character attributes SGR0
							m_out.style = Style();
							break;
						case 1:
							// Esc[1m Turn bold mode on SGR1
							m_out.style.Bold(true);
							break;
						case 2:
							// Esc[2m Turn low intensity mode on SGR2
							m_out.style.ForeColour(m_out.style.ForeColour() & 0x7);
							break;
						case 4:
							//Esc[4m Turn underline mode on SGR4
							m_out.style.Underline(true);
							break;

						case 30: m_out.style.ForeColour(0x8 | uint8_t(EColour::Black  )); break; // forecolour
						case 31: m_out.style.ForeColour(0x8 | uint8_t(EColour::Red    )); break;
						case 32: m_out.style.ForeColour(0x8 | uint8_t(EColour::Green  )); break;
						case 33: m_out.style.ForeColour(0x8 | uint8_t(EColour::Yellow )); break;
						case 34: m_out.style.ForeColour(0x8 | uint8_t(EColour::Blue   )); break;
						case 35: m_out.style.ForeColour(0x8 | uint8_t(EColour::Magenta)); break;
						case 36: m_out.style.ForeColour(0x8 | uint8_t(EColour::Cyan   )); break;
						case 37: m_out.style.ForeColour(0x8 | uint8_t(EColour::White  )); break;

						case 40: m_out.style.BackColour(0x8 | uint8_t(EColour::Black  )); break; // background color
						case 41: m_out.style.BackColour(0x8 | uint8_t(EColour::Red    )); break;
						case 42: m_out.style.BackColour(0x8 | uint8_t(EColour::Green  )); break;
						case 43: m_out.style.BackColour(0x8 | uint8_t(EColour::Yellow )); break;
						case 44: m_out.style.BackColour(0x8 | uint8_t(EColour::Blue   )); break;
						case 45: m_out.style.BackColour(0x8 | uint8_t(EColour::Magenta)); break;
						case 46: m_out.style.BackColour(0x8 | uint8_t(EColour::Cyan   )); break;
						case 47: m_out.style.BackColour(0x8 | uint8_t(EColour::White  )); break;

						//Esc[5m	Turn blinking mode on	SGR5
						//Esc[7m	Turn reverse video on	SGR7
						//Esc[8m	Turn invisible text mode on	SGR8
						}
					}
				}
				break;

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
			m_seq.resize(0);
		}

		// Parse escape codes beginning with "Esc("
		void ParseEscapeSeq1()
		{
			switch (m_seq.back())
			{
			default: // Incomplete escape sequence
				break;
			//Esc(A	Set United Kingdom G0 character set	setukg0
			//Esc(B	Set United States G0 character set	setusg0
			//Esc(0	Set G0 special chars. & line set	setspecg0
			//Esc(1	Set G0 alternate character ROM	setaltg0
			//Esc(2	Set G0 alt char ROM and spec. graphics	setaltspecg0
			}

			// Escape sequence complete and processed
			m_seq.resize(0);
		}

		// Parse escape codes beginning with "Esc)"
		void ParseEscapeSeq2()
		{
			switch (m_seq.back())
			{
			default: // Incomplete escape sequence
				break;
			//Esc)A	Set United Kingdom G1 character set	setukg1
			//Esc)B	Set United States G1 character set	setusg1
			//Esc)0	Set G1 special chars. & line set	setspecg1
			//Esc)1	Set G1 alternate character ROM	setaltg1
			//Esc)2	Set G1 alt char ROM and spec. graphics	setaltspecg1
			}

			// Escape sequence complete and processed
			m_seq.resize(0);
		}

		// Parse escape codes beginning with "EscO"
		void ParseEscapeSeq3()
		{
			switch (m_seq.back())
			{
			default: // Incomplete escape sequence
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

			// Escape sequence complete and processed
			m_seq.resize(0);
		}

		// Parse escape codes beginning with "Esc#"
		void ParseEscapeSeq4()
		{
			switch (m_seq.back())
			{
			default: // Incomplete escape sequence
				break;
			//Esc#3	Double-height letters, top half	DECDHL
			//Esc#4	Double-height letters, bottom half	DECDHL
			//Esc#5	Single width, single height letters	DECSWL
			//Esc#6	Double width, single height letters	DECDWL
			}

			// Escape sequence complete and processed
			m_seq.resize(0);
		}

		// Converts a string of the form "param1;param2;param3;...;param4"
		// to an array of integers. Returns at least 'N' results.
		template <int N> void Params(int (&params)[N], char const* param_string)
		{
			char const *s = param_string, *e = s;
			for (int i = 0; i != N; ++i)
			{
				for (; *e && *e != ';'; ++e) {} // Find the separator
				try { params[i] = std::stoi(s, &e, 10); }
				catch (...) { params[i] = 0; } // ignore bad conversions
				s = *e ? e + 1 : e;
				e = s;
			}
		}

		// Move the cursor to an absolute position
		CaretPosition MoveCaret(int x, int y)
		{
			// Note, don't allocate memory until data is actually written
			auto loc = CaretPosition(Clamp<int>(x, 0, Width() - 1), Clamp<int>(y, 0, Height() - 1));
			return loc;
		}

		// Move the cursor by a relative offset
		CaretPosition MoveCaret(CaretPosition loc, int dx, int dy)
		{
			return MoveCaret(loc.X + dx, loc.Y + dy);
		}

		// Write 'str' into the screen buffer. Writes no more than 'max_count' characters
		// Stops at the null terminator of 'str'. 'str' should not contain any non-printable
		// characters (including \n,\r). These are removed by ParseOutput
		void Write(char const* str, size_t count)
		{
			assert(m_out.pos.X >= 0 && m_out.pos.X < m_settings.m_width && "Caret outside screen buffer");
			assert(m_out.pos.Y >= 0 && m_out.pos.Y < m_settings.m_height && "Caret outside screen buffer");

			// Get the line and ensure it's large enough
			auto& line = LineAt(m_out.pos.Y);
			if (int(line.size()) <= m_out.pos.X)
				line.resize(std::min(size_t(m_out.pos.X), m_settings.m_width), ' ', m_out.style);

			// write the string
			auto len = std::min(m_settings.m_width - m_out.pos.X, count);
			line.write(m_out.pos.X, str, len, m_out.style);
		}
		void Write(char const* str)
		{
			Write(str, strnlen(str, m_settings.m_width - m_out.pos.X));
		}
	};
}
