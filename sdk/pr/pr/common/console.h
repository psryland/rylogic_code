//**************************************************************************************
// Console
//  Copyright © Rylogic Ltd 2010
//**************************************************************************************
#ifndef PR_COMMON_CONSOLE_H
#define PR_COMMON_CONSOLE_H

#if _WIN32_WINNT < 0x0500
#  error "Console requires _WIN32_WINNT >= 0x0500"
#endif

#include <string>
#include <sstream>
#include <iostream>
#include <fstream>
#include <algorithm>
#include <vector>
#include <deque>
#include <windows.h>
#include <stdio.h>
#include <fcntl.h>
#include <io.h>
#include <conio.h>
#include <crtdbg.h>
#include <stdarg.h>
#include <tchar.h>
#include <new>
#include <cassert>
#include "pr/macros/enum.h"
#include "pr/macros/no_copy.h"
#include "pr/str/prstring.h"
#include "pr/common/events.h"
#include "pr/common/event.h"

namespace pr
{
	namespace console
	{
		class Console;
		struct Pad;
		typedef INPUT_RECORD Event;
		typedef BOOL (__stdcall *HandlerFunction)(DWORD ctrl_type);

		// Generic string operations
		template <typename Str> inline void append(Str& str, size_t count, typename Str::value_type ch) { str.append(count, ch); }
		template <typename Str> inline void resize(Str& str, size_t sz)                                 { str.resize(sz); }
		template <typename Str> inline size_t size(Str& str)                                            { return str.size(); }
		template <typename Str> inline bool empty(Str& str)                                             { return str.empty(); }
		inline long width(RECT const& r)                                                                { return r.right - r.left; }
		inline long height(RECT const& r)                                                               { return r.bottom - r.top; }
		inline long width(SMALL_RECT const& r)                                                          { return r.Right - r.Left; }
		inline long height(SMALL_RECT const& r)                                                         { return r.Bottom - r.Top; }

		#pragma region Enums

		#define PR_ENUM(x)\
			x(Key   , = KEY_EVENT                 )\
			x(Mouse , = MOUSE_EVENT               )\
			x(Size  , = WINDOW_BUFFER_SIZE_EVENT  )\
			x(Menu  , = MENU_EVENT                )\
			x(Focus , = FOCUS_EVENT               )\
			x(Any   , = Key|Mouse|Size|Menu|Focus )
		PR_DEFINE_ENUM2_FLAGS(EEvent, PR_ENUM);
		#undef PR_ENUM

		#define PR_ENUM(x)\
			x(Left         , = 1 << 0          )\
			x(HCentre      , = 1 << 1          )\
			x(Right        , = 1 << 2          )\
			x(Top          , = 1 << 3          )\
			x(VCentre      , = 1 << 4          )\
			x(Bottom       , = 1 << 5          )\
			x(TopLeft      , = Top|Left        )\
			x(TopCentre    , = Top|HCentre     )\
			x(TopRight     , = Top|Right       )\
			x(MiddleLeft   , = VCentre|Left    )\
			x(Centre       , = VCentre|HCentre )\
			x(MiddleRight  , = VCentre|Right   )\
			x(BottomLeft   , = Bottom|Left     )\
			x(BottomCentre , = Bottom|HCentre  )\
			x(BottomRight  , = Bottom|Right    )
		PR_DEFINE_ENUM2_FLAGS(EAnchor, PR_ENUM);
		#undef PR_ENUM

		#define PR_ENUM(x)\
			x(Black        , = 0                     )\
			x(Blue         , = 1 << 0                )\
			x(Green        , = 1 << 1                )\
			x(Red          , = 1 << 2                )\
			x(Cyan         , = Blue|Green            )\
			x(Purple       , = Blue|Red              )\
			x(Yellow       , = Green|Red             )\
			x(Grey         , = Blue|Green|Red        )\
			x(BrightBlue   , = (1<<3)|Blue           )\
			x(BrightGreen  , = (1<<3)|Green          )\
			x(BrightRed    , = (1<<3)|Red            )\
			x(BrightCyan   , = (1<<3)|Blue|Green     )\
			x(BrightPurple , = (1<<3)|Blue|Red       )\
			x(BrightYellow , = (1<<3)|Green|Red      )\
			x(White        , = (1<<3)|Red|Green|Blue )\
			x(Default      , = 1 << 16               )
		PR_DEFINE_ENUM2_FLAGS(EColour, PR_ENUM);
		#undef PR_ENUM

		#pragma endregion

		#pragma region Wrapper structs

		// Helper to correctly initialise CONSOLE_SCREEN_BUFFER_INFOEX
		struct ConsoleScreenBufferInfo :CONSOLE_SCREEN_BUFFER_INFOEX
		{
			ConsoleScreenBufferInfo() :CONSOLE_SCREEN_BUFFER_INFOEX() { cbSize = sizeof(CONSOLE_SCREEN_BUFFER_INFOEX); }
		};

		// Helper to wrap 'COORD'
		struct Coord :COORD
		{
			Coord() :COORD() {}
			Coord(COORD c) :COORD(c) {}
			Coord(int x, int y) { X = short(x); Y = short(y); }
		};


		// Helper for combining fore and back colours
		struct Colours
		{
			EColour m_fore;
			EColour m_back;
			Colours() :m_fore(EColour::Default) ,m_back(EColour::Default) {}
			Colours(EColour fore) :m_fore(fore) ,m_back(EColour::Default) {}
			Colours(EColour fore, EColour back) :m_fore(fore) ,m_back(back) {}
			Colours(EColour::Enum_ fore) :m_fore(fore) ,m_back(EColour::Default) {}
			Colours(EColour::Enum_ fore, EColour::Enum_ back) :m_fore(fore) ,m_back(back) {}
			static Colours From(WORD colours) { Colours c; c.m_fore = colours & 0xf; c.m_back = (colours >> 4) & 0xf; return c; }
			operator WORD() const { return WORD((m_back << 4) | m_fore); }
			bool operator == (Colours const& rhs) const { return m_fore == rhs.m_fore && m_back == rhs.m_back; }
			Colours Merge(Colours rhs) const
			{
				return Colours(
					rhs.m_fore != EColour::Default ? rhs.m_fore : m_fore,
					rhs.m_back != EColour::Default ? rhs.m_back : m_back);
			}
		};

		#pragma endregion

		#pragma region Events

		// An event raised when a key event is 'pumped'.
		// Note: sends both key down and key up events
		struct Evt_Key
		{
			KEY_EVENT_RECORD const& m_key;
			Evt_Key(KEY_EVENT_RECORD const& k) :m_key(k) {}
			PR_NO_COPY(Evt_Key);
		};

		// An event raised when a key event is 'pumped'. Down events only
		struct Evt_KeyDown
		{
			KEY_EVENT_RECORD const& m_key;
			Evt_KeyDown(KEY_EVENT_RECORD const& k) :m_key(k) {}
			PR_NO_COPY(Evt_KeyDown);
		};

		// An event raised when a line of user input is available
		template <typename Char> struct Evt_Line
		{
			std::basic_string<Char> const& m_input;
			Evt_Line(std::basic_string<Char> const& input) :m_input(input) {}
			PR_NO_COPY(Evt_Line);
		};

		// An event raised when escape is pressed (while there is no user input)
		struct Evt_Escape
		{};

		// An event raised when tab is pressed
		struct Evt_Tab
		{};

		// An event raised whenever a function key is pressed
		struct Evt_FunctionKey
		{
			int m_num; // 1 - 24 for F1 -> F24
			Evt_FunctionKey(int vk) :m_num(vk - VK_F1) {}
		};

		// An event raised whenever a pad receives focus
		struct Evt_FocusChanged
		{
			Pad const& m_pad;
			explicit Evt_FocusChanged(Pad const& pad) :m_pad(pad) {}
			PR_NO_COPY(Evt_FocusChanged);
		};

		#pragma endregion

		class Console
		{
			// Helpers for reading/writing char/wchar_t
			#pragma region Traits
			struct traits
			{
				static size_t length(char const* str)    { return strlen(str); }
				static size_t length(wchar_t const* str) { return wcslen(str); }
				static void read(KEY_EVENT_RECORD evt, char&    ch) { ch = evt.uChar.AsciiChar; }
				static void read(KEY_EVENT_RECORD evt, wchar_t& ch) { ch = evt.uChar.UnicodeChar; }
				static void write(HANDLE out, char    const* str, size_t ofs, size_t count) { DWORD chars_written; WriteConsoleA(out, str + ofs, DWORD(count), &chars_written, 0); }
				static void write(HANDLE out, wchar_t const* str, size_t ofs, size_t count) { DWORD chars_written; WriteConsoleW(out, str + ofs, DWORD(count), &chars_written, 0); }
				static void fill(HANDLE out, char    ch, size_t count, COORD loc)           { DWORD chars_written; FillConsoleOutputCharacterA(out, ch, count, loc, &chars_written); }
				static void fill(HANDLE out, wchar_t ch, size_t count, COORD loc)           { DWORD chars_written; FillConsoleOutputCharacterW(out, ch, count, loc, &chars_written); }
				static void fill(HANDLE out, WORD   col, size_t count, COORD loc)           { DWORD chars_written; FillConsoleOutputAttribute(out, col, count, loc, &chars_written); }
				static void fill(HANDLE out, char    ch, WORD col, size_t count, COORD loc) { fill(out, ch, count, loc); fill(out, col, count, loc); }
				static void fill(HANDLE out, wchar_t ch, WORD col, size_t count, COORD loc) { fill(out, ch, count, loc); fill(out, col, count, loc); }
			};
			#pragma endregion

			#pragma region LineInput

			// A helper object for managing line input
			template <typename Char> struct LineInput
			{
				std::basic_string<Char> m_text;  // The current line input
				size_t m_caret;                  // The cursor position with 'm_text'
				Console& m_cons;                 // The console to echo the line input to
				bool m_echo;                     // True if we should echo to the output buffer
				
				LineInput(Console& cons)
					:m_text()
					,m_caret()
					,m_cons(cons)
					,m_echo(true)
				{}
				bool empty() const
				{
					return m_text.empty();
				}
				size_t word_boundary(size_t caret, bool fwd) const
				{
					size_t end = fwd * m_text.size();
					if (fwd)
					{
						for (;caret != end && !isspace(m_text[caret]); ++caret) {} // skip over non-ws
						for (;caret != end &&  isspace(m_text[caret]); ++caret) {} // Find the next non-ws
					}
					else
					{
						for (;caret != end &&  isspace(m_text[caret]); --caret) {} // Find the next non-ws
						for (;caret != end && !isspace(m_text[caret]); --caret) {} // skip over non-ws
						caret += caret != 0;
					}
					return caret;
				}
				void left(bool word_skip)
				{
					if (m_caret == 0) return;
					size_t caret = m_caret - 1;
					if (word_skip) caret = word_boundary(caret, false);
					if (m_echo) m_cons.Cursor(m_cons.Cursor(), caret - m_caret, 0);
					m_caret = caret;
				}
				void right(bool word_skip)
				{
					if (m_caret == m_text.size()) return;
					size_t caret = m_caret + 1;
					if (word_skip) caret = word_boundary(caret, true);
					if (m_echo) m_cons.Cursor(m_cons.Cursor(), caret - m_caret, 0);
					m_caret = caret;
				}
				void home()
				{
					if (m_caret == 0) return;
					if (m_echo) m_cons.Cursor(m_cons.Cursor(), 0 - m_caret, 0);
					m_caret = 0;
				}
				void end()
				{
					if (m_caret == m_text.size()) return;
					if (m_echo) m_cons.Cursor(m_cons.Cursor(), m_text.size() - m_caret, 0);
					m_caret = m_text.size();
				}
				void reset()
				{
					if (m_echo)
					{
						m_cons.Cursor(m_cons.Cursor(), 0 - m_caret, 0);
						m_cons.Fill(' ', m_text.size());
					}
					m_text.resize(0);
					m_caret = 0;
				}
				void write(Char ch)
				{
					if (m_caret == m_text.size())
					{
						if (m_echo) m_cons.Write(&ch, 0, 1);
						m_text.push_back(ch);
					}
					else
					{
						if (m_echo)
						{
							auto cur = m_cons.Cursor();
							m_cons.Write(&ch, 0, 1);
							m_cons.Write(m_text.c_str(), m_caret, m_text.size() - m_caret);
							m_cons.Cursor(cur, 1, 0);
						}
						m_text.insert(m_text.begin() + m_caret, 1, ch);
					}
					++m_caret;
				}
				void delback(bool whole_word)
				{
					if (m_caret == 0) return;
					size_t caret = m_caret - 1;
					if (whole_word) caret = word_boundary(caret, false);
					if (m_echo)
					{
						int dx = caret - m_caret;
						auto cur = m_cons.Cursor();
						m_cons.Cursor(cur, dx, 0);
						m_cons.Write(m_text.c_str(), m_caret, m_text.size() - m_caret);
						m_cons.Write(std::string(size_t(-dx), ' '));
						m_cons.Cursor(cur, dx, 0);
					}
					m_text.erase(m_text.begin() + caret, m_text.begin() + m_caret);
					m_caret = caret;
				}
				void delfwd(bool whole_word)
				{
					if (m_caret == m_text.size()) return;
					size_t caret = m_caret + 1;
					if (whole_word) caret = word_boundary(caret, true);
					if (m_echo)
					{
						int dx = caret - m_caret;
						auto cur = m_cons.Cursor();
						m_cons.Write(m_text.c_str(), m_caret + dx, m_text.size() - caret);
						m_cons.Write(std::string(size_t(dx), ' '));
						m_cons.Cursor(cur);
					}
					m_text.erase(m_text.begin() + m_caret, m_text.begin() + caret);
				}
				PR_NO_COPY(LineInput);
			};

			#pragma endregion

			HANDLE  m_stdout;
			HANDLE  m_stdin;
			HANDLE  m_stderr;
			HANDLE  m_buf[2]; // Handles for screen buffers
			HANDLE* m_front;  // Front buffer for double buffered console
			HANDLE* m_back;   // Back buffer for double buffered console
			Colours m_colour;
			int     m_base;
			int     m_digits;
			bool    m_opened;
			bool    m_console_created;
			bool    m_double_buffered;
			bool    m_unicode_input;
			LineInput<char>    m_linea;    // Buffer of line input
			LineInput<wchar_t> m_linew;   // Buffer of line input

			friend struct CursorScope;
			friend struct ColourScope;

			void Throw(int result, char const* msg) const
			{
				if (result != 0) return;

				// Retrieve the system error message for the last-error code
				char lpMsgBuf[1024];
				DWORD dw = GetLastError(); 
				FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM|FORMAT_MESSAGE_IGNORE_INSERTS, NULL, dw, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), lpMsgBuf, sizeof(lpMsgBuf), NULL);
				std::string err; err.append(msg).append("\n").append(lpMsgBuf).append("\n");
				LocalFree(lpMsgBuf);

				throw std::exception(err.c_str());
			}

			// Convert a stream of key event records into user input, handling special keys
			template <typename Char> void TranslateKeyEvent(KEY_EVENT_RECORD const& k, LineInput<Char>& line)
			{
				// Notify of the key press
				pr::events::Send(Evt_Key(k));
				if (!k.bKeyDown) return;
				pr::events::Send(Evt_KeyDown(k));

				Char ch; traits::read(k, ch);
				for (int i = 0; i != k.wRepeatCount; ++i)
				{
					bool ctrl = (k.dwControlKeyState & (LEFT_CTRL_PRESSED|RIGHT_CTRL_PRESSED)) != 0;
					switch (k.wVirtualKeyCode)
					{
					default:
						if (k.wVirtualKeyCode >= VK_F1 && k.wVirtualKeyCode <= VK_F24)
							pr::events::Send(Evt_FunctionKey(k.wVirtualKeyCode));
						if (ch != 0)
							line.write(ch);
						break;
					case VK_TAB:
						pr::events::Send(Evt_Tab());
						break;
					case VK_RETURN:
						pr::events::Send(Evt_Line<Char>(line.m_text));
						line.reset();
						break;
					case VK_ESCAPE:
						if (!line.empty()) line.reset();
						else pr::events::Send(Evt_Escape());
						break;
					case VK_BACK:    line.delback(ctrl); break;
					case VK_DELETE:  line.delfwd(ctrl);  break;
					case VK_LEFT:    line.left(ctrl);    break;
					case VK_RIGHT:   line.right(ctrl);   break;
					case VK_HOME:    line.home();        break;
					case VK_END:     line.end();         break;
					}
				}
			}

			// Convert a stream of mouse event records into mouse events
			void TranslateMouseEvents()
			{}

		public:
			Console()
				:m_stdout()
				,m_stdin()
				,m_stderr()
				,m_buf()
				,m_front(&m_stdout)
				,m_back(&m_stdout)
				,m_base(10)
				,m_colour(EColour::Black, EColour::Grey)
				,m_digits(3)
				,m_opened(false)
				,m_console_created(false)
				,m_double_buffered(false)
				,m_unicode_input(false)
				,m_linea(*this)
				,m_linew(*this)
			{
				// I can't figure this console redirecting stuff out. It seems sometimes you need
				// to call 'RedirectIOToConsole()', other times that doesn't work but 'ReopenStdio()'
				// seems to. If creating the console in a dll it needs to be linked against the same CRT
				// as the main app apparently.
				if (!Attach())
					Open();
			}

			~Console()
			{
				Close();
			}

			// Attach to an existing console window
			bool Attach()
			{
				m_console_created = false;
				m_opened = AttachConsole(DWORD(-1)) == TRUE;
				return m_opened;
			}

			// Open a console window
			void Open()
			{
				if (m_opened) return;
				m_console_created = AllocConsole() == TRUE;
				Throw((m_stdout = GetStdHandle(STD_OUTPUT_HANDLE)) != INVALID_HANDLE_VALUE, "Duplicate stdout handle failed");
				Throw((m_stdin  = GetStdHandle(STD_INPUT_HANDLE )) != INVALID_HANDLE_VALUE, "Duplicate stdin handle failed" );
				Throw((m_stderr = GetStdHandle(STD_ERROR_HANDLE )) != INVALID_HANDLE_VALUE, "Duplicate stderr handle failed");
				m_buf[0] = m_buf[1] = INVALID_HANDLE_VALUE;
				m_opened = true;
			}
			void Open(int columns, int lines)
			{
				Open();
				auto info = Info();
				info.srWindow.Left   = 0;
				info.srWindow.Top    = 0;
				info.srWindow.Right  = info.dwSize.X = info.dwMaximumWindowSize.X = SHORT(columns);
				info.srWindow.Bottom = info.dwSize.Y = info.dwMaximumWindowSize.Y = SHORT(lines);
				Info(info);
			}

			// Redirect IO to console
			void RedirectIOToConsole()
			{
				FILE *fp;
				int h;

				// redirect unbuffered STDOUT to the console
				h = _open_osfhandle(reinterpret_cast<intptr_t>(m_stdout), _O_TEXT);
				fp = _fdopen(h, "w");
				*stdout = *fp;
				setvbuf(stdout, NULL, _IONBF, 0);

				// redirect unbuffered STDIN to the console
				h = _open_osfhandle(reinterpret_cast<intptr_t>(m_stdin), _O_TEXT);
				fp = _fdopen(h, "r");
				*stdin = *fp;
				setvbuf(stdin, NULL, _IONBF, 0);

				// redirect unbuffered STDERR to the console
				h = _open_osfhandle(reinterpret_cast<intptr_t>(m_stderr), _O_TEXT);
				fp = _fdopen(h, "w");
				*stderr = *fp;
				setvbuf(stderr, NULL, _IONBF, 0);

				// make cout, wcout, cin, wcin, wcerr, cerr, wclog and clog
				// point to console as well
				std::ios::sync_with_stdio();
			}

			// Closes the console window
			void Close()
			{
				if (!m_opened) return;
				CloseHandle(m_stdout);
				CloseHandle(m_stdin);
				CloseHandle(m_stderr);
				CloseHandle(m_buf[0]);
				CloseHandle(m_buf[1]);
				if (m_console_created) FreeConsole();
				m_console_created = false;
				m_opened = false;
			}
			void CloseHandle(HANDLE& handle)
			{
				if (handle == INVALID_HANDLE_VALUE) return;
				if (handle == GetStdHandle(STD_OUTPUT_HANDLE)) return;
				if (handle == GetStdHandle(STD_INPUT_HANDLE )) return;
				if (handle == GetStdHandle(STD_ERROR_HANDLE )) return;
				CloseHandle(handle);
				handle = INVALID_HANDLE_VALUE;
			}

			// Get/Set the dimensions of the console window and/or buffer
			// Note, the buffer must not be smaller than the window or the window larger than the buffer
			ConsoleScreenBufferInfo Info() const
			{
				ConsoleScreenBufferInfo info;
				Throw(GetConsoleScreenBufferInfoEx(*m_back, &info), "Failed to read console info");
				return info;
			}
			void Info(ConsoleScreenBufferInfo info)
			{
				info.dwSize.X = std::min(info.dwSize.X, info.dwMaximumWindowSize.X);
				info.dwSize.Y = std::min(info.dwSize.Y, info.dwMaximumWindowSize.Y);
				info.dwCursorPosition.X = std::max(std::min(info.dwCursorPosition.X, info.dwSize.X), SHORT(0));
				info.dwCursorPosition.Y = std::max(std::min(info.dwCursorPosition.Y, info.dwSize.Y), SHORT(0));
				Throw(SetConsoleScreenBufferInfoEx(*m_back , &info), "Failed to set console dimensions");
				Throw(SetConsoleScreenBufferInfoEx(*m_front, &info), "Failed to set console dimensions");
			}

			// Get/Set the highlevel console output mode
			DWORD OutMode() const
			{
				DWORD mode;
				Throw(GetConsoleMode(*m_back, &mode), "failed to read console output mode");
				return mode;
			}
			void OutMode(DWORD mode)
			{
				Throw(SetConsoleMode(*m_back,  mode), "failed to set console output mode");
				Throw(SetConsoleMode(*m_front, mode), "failed to set console output mode");
			}

			// Get/Set the highlevel console input mode
			DWORD InMode() const
			{
				DWORD mode;
				Throw(GetConsoleMode(m_stdin, &mode), "failed to read console input mode");
				return mode;
			}
			void InMode(DWORD mode)
			{
				Throw(SetConsoleMode(m_stdin,  mode), "failed to set console input mode");
			}

			// Return the hwnd for the console window
			HWND GetWindowHandle() const
			{
				return GetConsoleWindow();
			}

			// Return true if the console window has focus
			bool HasFocus() const
			{
				return GetForegroundWindow() == GetConsoleWindow();
			}

			// Get/Set auto scrolling for the console
			bool AutoScroll() const  { return (OutMode() & ENABLE_WRAP_AT_EOL_OUTPUT) != 0; }
			void AutoScroll(bool on) { if (on) OutMode(OutMode() | ENABLE_WRAP_AT_EOL_OUTPUT); else OutMode(OutMode() & ~ENABLE_WRAP_AT_EOL_OUTPUT); }

			// Get/Set echo mode
			bool Echo() const { return (InMode() & ENABLE_ECHO_INPUT) != 0; }
			void Echo(bool on) { if (on) InMode(InMode() | ENABLE_ECHO_INPUT); else InMode(InMode() & ~ENABLE_ECHO_INPUT); }

			// Get/Set double buffering for the console
			bool DoubleBuffered() const { return m_double_buffered; }
			void DoubleBuffered(bool on)
			{
				if (on == m_double_buffered) return;
				if (on)
				{
					auto info = Info();
					m_buf[0] = CreateConsoleScreenBuffer(GENERIC_READ|GENERIC_WRITE, FILE_SHARE_READ|FILE_SHARE_WRITE, nullptr, CONSOLE_TEXTMODE_BUFFER, nullptr);
					m_buf[1] = CreateConsoleScreenBuffer(GENERIC_READ|GENERIC_WRITE, FILE_SHARE_READ|FILE_SHARE_WRITE, nullptr, CONSOLE_TEXTMODE_BUFFER, nullptr);
					Throw(m_buf[0] != INVALID_HANDLE_VALUE, "Failed to create console screen buffer");
					Throw(m_buf[1] != INVALID_HANDLE_VALUE, "Failed to create console screen buffer");
					m_back  = &m_buf[0];
					m_front = &m_buf[1];
					Throw(SetConsoleScreenBufferInfoEx(*m_back , &info), "Failed to set console dimensions");
					Throw(SetConsoleScreenBufferInfoEx(*m_front, &info), "Failed to set console dimensions");
					FlipBuffer();
					m_double_buffered = true;
				}
				else
				{
					CloseHandle(m_buf[0]);
					CloseHandle(m_buf[1]);
					m_back = m_front = &m_stdout;
					m_double_buffered = false;
				}
			}

			// Flip the front/back buffer
			void FlipBuffer()
			{
				std::swap(m_back, m_front);
				Throw(SetConsoleActiveScreenBuffer(*m_front), "Set console active buffer failed");
			}

			// Get/Set unicode input
			bool UnicodeInput() const { return m_unicode_input; }
			void UnicodeInput(bool on) { m_unicode_input = on; }

			// Get the currently buffered line input
			std::string LineInputA() const { return m_linea.m_text; }
			std::wstring LineInputW() const { return m_linew.m_text; }

			// Return true if the console is open
			bool IsOpen() const { return m_opened; }

			// Get/Set the position of the window
			RECT WindowRect() const
			{
				RECT rect;
				::GetWindowRect(GetWindowHandle(), &rect);
				return rect;
			}
			void WindowPosition(int x, int y)
			{
				RECT rect = WindowRect();
				int w = rect.right - rect.left;
				int h = rect.bottom - rect.top;
				::MoveWindow(GetWindowHandle(), x, y, w, h, FALSE);
			}
			void WindowPosition(EAnchor anchor, int dx = 0, int dy = 0)
			{
				int sx = GetSystemMetrics(SM_CXSCREEN);
				int sy = GetSystemMetrics(SM_CYSCREEN);
				RECT rect = WindowRect();
				int w = rect.right - rect.left;
				int h = rect.bottom - rect.top;

				// Find the coordinate to write the string at
				int cx = 0, cy = 0;
				if (anchor & EAnchor::Left   ) cx = dx + 0;
				if (anchor & EAnchor::HCentre) cx = dx + (sx - w) / 2;
				if (anchor & EAnchor::Right  ) cx = dx + (sx - w);
				if (anchor & EAnchor::Top    ) cy = dy + 0;
				if (anchor & EAnchor::VCentre) cy = dy + (sy - h) / 2;
				if (anchor & EAnchor::Bottom ) cy = dy + (sy - h);

				WindowPosition(cx, cy);
			}

			// Get/Set the position of the cursor
			COORD Cursor() const { return Info().dwCursorPosition; }
			void Cursor(COORD coord, int dx = 0, int dy = 0)
			{
				// Limit the cursor position to within the console buffer
				auto info = Info();
				coord.X = SHORT(std::max(std::min(coord.X + dx, int(info.dwSize.X - 1)), 0));
				coord.Y = SHORT(std::max(std::min(coord.Y + dy, int(info.dwSize.Y - 1)), 0));
				Throw(SetConsoleCursorPosition(*m_back, coord), "Failed to set cursor position");
			}
			void Cursor(int x, int y) { Cursor(Coord(x,y)); }
			void Cursor(EAnchor anchor, int dx = 0, int dy = 0) { Cursor(CursorLocation(anchor, 1, 1, dx, dy)); }

			// Get/Set the colour to use
			Colours Colour() const { return Colours::From(Info().wAttributes); }
			void Colour(Colours c) { Throw(SetConsoleTextAttribute(*m_back, m_colour.Merge(c)), "Failed to set colour text attributes"); }
			void Colour(EColour fore, EColour back) { Colour(Colours(fore,back)); }

			// Set an event handler routine for the console, only affects this console, doesn't require removing.
			// 'ctrl_type' is one of:
			// CTRL_C_EVENT - Ctrl+C
			// CTRL_BREAK_EVENT - Ctrl+Break
			// CTRL_CLOSE_EVENT - Close button pressed
			// CTRL_LOGOFF_EVENT - User log off
			// CTRL_SHUTDOWN_EVENT - System shutdown
			// Handler routines form a stack, last added = first called. Return true if the handler handles the event
			// 'add' indicates whether the handler should be added or removed.
			// if 'HandlerRoutine' is null, 'add' == true means ignore Ctrl+C, 'add' == false means exit on Ctrl+C
			void SetHandler(HandlerFunction func, bool add)
			{
				SetConsoleCtrlHandler(func, add);
			}

			// Clear the console
			void Clear() { Clear(m_colour); }
			void Clear(Colours col)
			{
				// Get the number of character cells in the current buffer
				auto info = Info();
				unsigned int num_chars = info.dwSize.X * info.dwSize.Y;

				// Fill the area with blanks
				traits::fill(*m_back, L' ', m_colour.Merge(col), num_chars, Coord(0,0));
			}
			void Clear(int x, int y, int sx, int sy) { Clear(x,y,sx,sy,m_colour); }
			void Clear(int x, int y, int sx, int sy, Colours col) { Clear(x,y,sx,sy,true,col); }
			void Clear(int x, int y, int sx, int sy, bool clear_text, Colours col)
			{
				// Get the number of character cells in the current buffer
				auto info = Info();
				x  = (x  <  0) ? 0 : (x >= info.dwSize.X) ? info.dwSize.X - 1 : x;
				y  = (y  <  0) ? 0 : (y >= info.dwSize.Y) ? info.dwSize.Y - 1 : y;
				sx = (sx == 0) ? info.dwSize.X : sx;
				sy = (sy == 0) ? info.dwSize.Y : sy;
				sx = (sx <  0) ? 0 : (x + sx > info.dwSize.X) ? info.dwSize.X - x : sx;
				sy = (sy <  0) ? 0 : (y + sy > info.dwSize.Y) ? info.dwSize.Y - y : sy;

				// Fill the area with blanks
				auto c = m_colour.Merge(col);
				for (int yend = y + sy; y != yend; ++y)
				{
					if (clear_text) traits::fill(*m_back, L' ', c, sx, Coord(x,y));
					else            traits::fill(*m_back,       c, sx, Coord(x,y));
				}
			}

			// Flush all input events from the input buffer
			void Flush()
			{
				FlushConsoleInputBuffer(m_stdin);
			}

			// Call this method to consume input events on stdin for forward them to events
			void PumpInput()
			{
				for(;WaitForEvent(EEvent::Any, 0);)
				{
					Event in[128]; DWORD read;
					Throw(ReadConsoleInputW(m_stdin, in, 128, &read), "Failed to read a console input events");
					for (DWORD i = 0; i != read; ++i)
					{
						switch (in[i].EventType)
						{
						default:
							throw std::exception("Unknown input event type");
						case EEvent::Key:
							if (m_unicode_input)
								TranslateKeyEvent(in[i].Event.KeyEvent, m_linew);
							else
								TranslateKeyEvent(in[i].Event.KeyEvent, m_linea);
							break;
						case EEvent::Mouse:
							break;
						case EEvent::Size:
							break;
						case EEvent::Menu:
							break;
						case EEvent::Focus:
							break;
						}
					}
				}
			}

			// Return the number of events in the input buffer
			DWORD InputEventCount() const
			{
				DWORD count;
				Throw(GetNumberOfConsoleInputEvents(m_stdin, &count), "Failed to read input event count");
				return count;
			}

			// Return the next event from the input buffer without removing it
			Event PeekInputEvent() const
			{
				_ASSERT(InputEventCount() != 0);
				Event in; DWORD read;
				Throw(PeekConsoleInputW(m_stdin, &in, 1, &read), "Failed to peek a console input event");
				return in;
			}

			// Return the next event from the input buffer
			Event ReadInputEvent() const
			{
				_ASSERT(InputEventCount() != 0);
				Event in; DWORD read;
				Throw(ReadConsoleInputW(m_stdin, &in, 1, &read), "Failed to read a console input event");
				return in;
			}

			// Wait for a specific event to be next in the console input buffer
			// This function blocks for up to 'timeout_ms'
			// 'event_type' is a combination of 'EEvent'
			// Returns true if the correct event occurred, false if the wait timed out
			bool WaitForEvent(WORD event_type, DWORD timeout_ms) const
			{
				while (WaitForSingleObject(m_stdin, timeout_ms) == WAIT_OBJECT_0)
				{
					if (PeekInputEvent().EventType & event_type) return true;
					ReadInputEvent(); // Consume the event of the wrong type
				}
				return false;
			}

			// Wait for a key to be pressed
			void WaitKey() const
			{
				WaitForEvent(EEvent::Key, INFINITE);
			}

			// Returns true if input data is waiting, equivalent to _kbhit()
			bool KBHit() const
			{
				return WaitForEvent(EEvent::Key, 0);
			}

			// Consume input events upto and including the next 'key' event
			// Calls 'func' on the key event and if true is returned, returns true. Returns false on timeout
			// Note: these Read methods are basic and don't provide echoing etc. Use PumpInput() preferably
			template <typename Func> bool ReadKeyEvent(Func func, DWORD wait_ms = INFINITE) const
			{
				for(;WaitForEvent(EEvent::Key, wait_ms);)
				{
					auto evt = ReadInputEvent().Event.KeyEvent;
					if (func(evt))
						return true;
				}
				return false;
			}

			// Consume input events upto and including the next 'key' event
			// Returns the VK_KEY code for the input keyboard event
			// Returns true if a key was read, false on timeout
			bool ReadKey(int& vk, DWORD wait_ms = INFINITE) const
			{
				return ReadKeyEvent([&](KEY_EVENT_RECORD evt)
					{
						if (!evt.bKeyDown) return false;
						vk = evt.wVirtualKeyCode;
						return true;
					}, wait_ms);
			}

			// Consume input events upto and including the next 'key' event
			// Returns true if a char was read from the input buffer, false if timed out
			// 'wait_ms' is the length of time to wait for a key event
			template <typename Char> bool ReadChar(Char& ch, DWORD wait_ms = INFINITE) const
			{
				return ReadKeyEvent([&](KEY_EVENT_RECORD evt)
					{
						if (!evt.bKeyDown) return false;
						traits::read(ReadInputEvent().Event.KeyEvent, ch);
						if (ch == 0) return false;
						return true;
					}, wait_ms);
			}

			// Read charactors from the console that pass 'pred'
			template <typename Str, typename Pred> Str Read(Pred pred, DWORD wait_ms = INFINITE) const
			{
				typedef Str::value_type Char;
				Str str;
				for (;;)
				{
					Char ch;
					if (!ReadChar(ch, wait_ms))
						return str;
					if (!pred(ch))
						return str;
					push_back(str, ch);
				}
			}

			// Read up to a '\n' character. Blocks until a return character is read
			template <typename Str> Str ReadLine(DWORD wait_ms = INFINITE) const
			{
				typedef Str::value_type Char;
				return Read([](Char ch){ return ch != Char('\n'); }, wait_ms);
			}

			// Read number characters from the console. Blocks until the first non-digit character is read
			template <typename Str> Str ReadNumber(DWORD wait_ms = INFINITE) const
			{
				typedef Str::value_type Char;
				return Read([](Char ch){ return (ch >= Char('0') && ch <= Char('9')) || ch == Char('.') || ch == Char('e') || ch == Char('E') || ch == Char('-') || ch == Char('+'); }, wait_ms);
			}

			// Write N characters to the output window without changing the current cursor position
			// This method treats the console buffer as a 1D array and ignores auto scroll
			template <typename Char> void Fill(int x, int y, Char ch, size_t count = 1)
			{
				auto info = Info();
				traits::fill(*m_back, ch, info.wAttributes, count, Coord(x,y));
			}
			template <typename Char> void Fill(Char ch, size_t count = 1)
			{
				auto info = Info();
				traits::fill(*m_back, ch, info.wAttributes, count, info.dwCursorPosition);
			}

			// Write text to the output window
			template <typename Char> void Write(Char const* str, size_t ofs, size_t count)
			{
				traits::write(*m_back, str, ofs, count);
			}
			template <typename Char> void Write(Char const* str)
			{
				Write(str, 0, traits::length(str));
			}

			// Write text to the output window
			template <typename Char> void Write(std::basic_string<Char> const& str, size_t ofs, size_t count)
			{
				Write(str.c_str(), ofs, count);
			}
			template <typename Char> void Write(std::basic_string<Char> const& str)
			{
				Write(str.c_str(), 0, str.size());
			}

			// Write text to the output window at a specified position
			template <typename Char> void Write(int x, int y, Char const* str, size_t ofs, size_t count)
			{
				Cursor(x,y);
				Write(str, ofs, count);
			}
			template <typename Char> void Write(int x, int y, Char const* str)
			{
				Write(x, y, str, 0, traits::length(str));
			}
			template <typename Char> void Write(int x, int y, std::basic_string<Char> const& str, size_t ofs, size_t count)
			{
				Write(x, y, str.c_str(), ofs, count);
			}
			template <typename Char> void Write(int x, int y, std::basic_string<Char> const& str)
			{
				Write(x, y, str.c_str(), 0, str.size());
			}

			// Write text to the output window at a specified position
			template <typename Char> void Write(EAnchor anchor, Char const* str, int dx = 0, int dy = 0)
			{
				int sx,sy;
				MeasureString(str, sx, sy);

				// Get the console window dimensions
				auto wind = Info().srWindow;
				int wx = wind.Right - wind.Left + 1;
				int wy = wind.Bottom - wind.Top + 1;

				// Find the coordinate to write the string at
				int x = 0, y = 0;
				if (anchor & EAnchor::Left   ) x = dx + wind.Left + 0;
				if (anchor & EAnchor::HCentre) x = dx + wind.Left + (wx - sx) / 2;
				if (anchor & EAnchor::Right  ) x = dx + wind.Left + (wx - sx);
				if (anchor & EAnchor::Top    ) y = dy + wind.Top  + 0;
				if (anchor & EAnchor::VCentre) y = dy + wind.Top  + (wy - sy) / 2;
				if (anchor & EAnchor::Bottom ) y = dy + wind.Top  + (wy - sy);

				// Write the string, line by line
				for (Char const *p = str, *e = str; *p; p = ++e)
				{
					for (; *e && *e != Char('\n'); ++e) {}
					Write(x, y++, p, 0, e - p);
					if (*e == 0) break;
				}
			}
			template <typename Char> void Write(EAnchor anchor, std::basic_string<Char> const& str, int dx = 0, int dy = 0)
			{
				Write(anchor, str.c_str(), dx, dy);
			}

			// Draw a box with size sx,sy anchored to a screen point
			void WriteBox(EAnchor anchor, int w, int h, int dx = 0, int dy = 0)
			{
				Coord loc = CursorLocation(anchor, w, h, dx, dy);
				WriteBox(loc.X, loc.Y, w, h);
			}

			// Draw a box with size sx,sy at x,y
			void WriteBox(int x, int y, int w, int h)
			{
				auto c = Colour();
				DWORD chars_written;
				FillConsoleOutputAttribute(*m_back, c, w, Coord(x,y), &chars_written);
				FillConsoleOutputCharacterW(*m_back, L'╔', 1  , Coord(x    ,y), &chars_written);
				FillConsoleOutputCharacterW(*m_back, L'═', w-2, Coord(x+1  ,y), &chars_written);
				FillConsoleOutputCharacterW(*m_back, L'╗', 1  , Coord(x+w-1,y), &chars_written);
				for (int i = y+1, iend = i+h-2; i != iend; ++i)
				{
					FillConsoleOutputAttribute(*m_back, c, 1, Coord(x    ,i), &chars_written);
					FillConsoleOutputAttribute(*m_back, c, 1, Coord(x+w-1,i), &chars_written);
					FillConsoleOutputCharacterW(*m_back, L'║', 1, Coord(x    ,i), &chars_written);
					FillConsoleOutputCharacterW(*m_back, L'║', 1, Coord(x+w-1,i), &chars_written);
				}
				FillConsoleOutputAttribute(*m_back, c, w, Coord(x,y+h-1), &chars_written);
				FillConsoleOutputCharacterW(*m_back, L'╚', 1  , Coord(x    ,y+h-1), &chars_written);
				FillConsoleOutputCharacterW(*m_back, L'═', w-2, Coord(x+1  ,y+h-1), &chars_written);
				FillConsoleOutputCharacterW(*m_back, L'╝', 1  , Coord(x+w-1,y+h-1), &chars_written);
			}

			// Stream a type to/from the console
			template <typename Type> Console& operator << (Type type)
			{
				std::stringstream strm; strm << type;
				Write(strm.str());
				return *this;
			}
			template <typename Type> Console& operator >> (Type& type)
			{
				std::stringstream strm(ReadLine());
				strm >> type;
				return *this;
			}

			// Returns the top left corner for a rectangular region with dimensions 'width/height' anchored to 'anchor' offset by 'dx,dy'
			Coord CursorLocation(EAnchor anchor, int width, int height, int dx = 0, int dy = 0)
			{
				// Get the console dimensions
				auto info = Info();
				int wx = info.srWindow.Right - info.srWindow.Left;
				int wy = info.srWindow.Bottom - info.srWindow.Top;

				// Find the coordinate to write the string at
				Coord c;
				if (anchor & EAnchor::Left   ) c.X = SHORT(dx + 0);
				if (anchor & EAnchor::HCentre) c.X = SHORT(dx + (wx - width) / 2);
				if (anchor & EAnchor::Right  ) c.X = SHORT(dx + (wx - width) + 1);
				if (anchor & EAnchor::Top    ) c.Y = SHORT(dy + 0);
				if (anchor & EAnchor::VCentre) c.Y = SHORT(dy + (wy - height) / 2);
				if (anchor & EAnchor::Bottom ) c.Y = SHORT(dy + (wy - height) + 1);
				return c;
			}

			// Returns the rectangular area needed to contain 'str'
			template <typename Char> static void MeasureString(Char const* str, int& w, int& h)
			{
				int x = 0; w = 0; h = 1;
				for (Char const* p = str; *p; ++p)
				{
					if (*p == Char('\n')) { ++h; x = 0; }
					else if (++x > w)     { ++w; }
				}
			}
		};

		#pragma region Scope structs

		// RAII object for preserving the cursor position
		struct CursorScope
		{
			Console& m_cons;
			COORD m_pos;
			CursorScope(Console& cons) :m_cons(cons) ,m_pos(m_cons.Cursor()) {}
			~CursorScope() { m_cons.Cursor(m_pos); }
			PR_NO_COPY(CursorScope);
		};

		// RAII object for pushing console colours
		struct ColourScope
		{
			Console& m_cons;
			Colours m_colours;
			ColourScope(Console& cons) :m_cons(cons) ,m_colours(Colours::From(m_cons.Info().wAttributes)) {}
			~ColourScope() { SetConsoleTextAttribute(*m_cons.m_back, m_colours); }
			PR_NO_COPY(ColourScope);
		};

		// RAII object for pushing console colours and cursor position
		struct Scope
		{
			CursorScope m_cur;
			ColourScope m_col;
			Scope(Console& cons) :m_cur(cons) ,m_col(cons) {}
			PR_NO_COPY(Scope);
		};

		#pragma endregion

		// A helper for drawing rectangular blocks of text in the console
		#pragma region Pad
		struct Pad
			:private pr::events::IRecv<Evt_Key>
			,private pr::events::IRecv<Evt_KeyDown>
			,private pr::events::IRecv<Evt_Line<char>>
			,private pr::events::IRecv<Evt_Line<wchar_t>>
			,private pr::events::IRecv<Evt_Escape>
			,private pr::events::IRecv<Evt_Tab>
			,private pr::events::IRecv<Evt_FunctionKey>
			,private pr::events::IRecv<Evt_FocusChanged>
		{
			enum EItem { Unknown, NewLine, CurrentInput, AString, WString, SetColours, SetCursor };
		
		private:
			struct Item
			{
				EItem m_what;
				union
				{
					void* m_ptr;
					std::string* m_linea;
					std::wstring* m_linew;
					Colours* m_colour;
					Coord* m_cursor;
				};
				
				Item(EItem what)        :m_what(what)        ,m_ptr(0) {}
				Item(std::string line)  :m_what(AString)     ,m_linea(new std::string(line)) {}
				Item(std::wstring line) :m_what(WString)     ,m_linew(new std::wstring(line)) {}
				Item(Colours colour)    :m_what(SetColours)  ,m_colour(new Colours(colour)) {}
				Item(Coord coord)       :m_what(SetCursor)   ,m_cursor(new Coord(coord)) {}
				~Item() { delete m_ptr; }

				Item(Item const& rhs) :m_what(rhs.m_what) ,m_ptr()
				{
					switch (m_what)
					{
					default: throw std::exception("Unknown item type");
					case Unknown:      break;
					case NewLine:      break;
					case AString:      m_linea  = new std::string(*rhs.m_linea); break;
					case WString:      m_linew  = new std::wstring(*rhs.m_linew); break;
					case SetColours:   m_colour = new Colours(*rhs.m_colour); break;
					case SetCursor:    m_cursor = new Coord(*rhs.m_cursor); break;
					case CurrentInput: break;
					}
				}
				Item(Item&& rhs) :m_what(rhs.m_what) ,m_ptr(rhs.m_ptr)
				{
					rhs.m_what = Unknown;
					rhs.m_ptr = 0;
				}
				Item& operator = (Item const& rhs) { this->~Item(); new (this) Item(rhs); }
				Item& operator = (Item&& rhs)      { this->~Item(); new (this) Item((Item&&)rhs); }
			};

			Colours m_colours;
			Colours m_border;
			Colours m_title_colour;
			std::string m_title;
			EAnchor m_title_anchor;
			Colours m_selection_colour;
			size_t m_width;
			size_t m_height;
			size_t m_line_count;
			Coord m_display_offset; // The top/left corner of the view to display within the pad
			int m_selected;         // using sign values so that negatives are accepted (and then clamped to [0,..) otherwise they just roll over)
			bool m_focused;         // True if this pad has input focus
			std::vector<Item> m_items;

			// Forward console events to attached handlers
			void OnEvent(Evt_Key const& e)           { OnKey(*this, e);         }
			void OnEvent(Evt_KeyDown const& e)       { OnKeyDown(*this, e);     }
			void OnEvent(Evt_Line<char> const& e)    { OnLineA(*this, e);       }
			void OnEvent(Evt_Line<wchar_t> const& e) { OnLineW(*this, e);       }
			void OnEvent(Evt_Escape const& e)        { OnEscape(*this, e);      }
			void OnEvent(Evt_Tab const& e)           { OnTab(*this, e);         }
			void OnEvent(Evt_FunctionKey const& e)   { OnFunctionKey(*this, e); }
			void OnEvent(Evt_FocusChanged const& e)  { Focus(&e.m_pad == this); }

		public:
			Pad(EColour fore = EColour::Default, EColour back = EColour::Default)
				:pr::events::IRecv<Evt_Key>(0,false)
				,pr::events::IRecv<Evt_KeyDown>(0,false)
				,pr::events::IRecv<Evt_Line<char>>(0,false)
				,pr::events::IRecv<Evt_Line<wchar_t>>(0,false)
				,pr::events::IRecv<Evt_Escape>(0,false)
				,pr::events::IRecv<Evt_Tab>(0,false)
				,pr::events::IRecv<Evt_FunctionKey>(0,false)
				,m_colours(fore, back)
				,m_border()
				,m_title_colour()
				,m_title()
				,m_title_anchor()
				,m_selection_colour()
				,m_width()
				,m_height()
				,m_line_count()
				,m_display_offset()
				,m_selected()
				,m_focused()
				,m_items()
			{
				Clear();
			}

			// Raised when a key event occurs while this pad has focus
			pr::Event<void(Pad&,Evt_Key const&)> OnKey;

			// Raised when a key down event occurs while this pad has focus
			pr::Event<void(Pad&,Evt_KeyDown const&)> OnKeyDown;
			
			// Raised when a line of user input has been entered while this pad has focus
			pr::Event<void(Pad&,Evt_Line<char> const&)> OnLineA;
			pr::Event<void(Pad&,Evt_Line<wchar_t> const&)> OnLineW;

			// Raised when the escape key is pressed while this pad has focus
			pr::Event<void(Pad&,Evt_Escape const&)> OnEscape;
			
			// Raised when the tab key is pressed while this pad has focus
			pr::Event<void(Pad&,Evt_Tab const&)> OnTab;
			
			// Raised when a function key is pressed while this pad has focus
			pr::Event<void(Pad&,Evt_FunctionKey const&)> OnFunctionKey;

			// Raised when focus changes for this pad
			pr::Event<void(Pad&)> OnFocusChanged;

			// Clear the contents of the pad an optionally the pad properties
			void Clear(bool content = true, bool dimensions = true, bool title = true, bool title_colour = true, bool border = true, bool selection_colour = true)
			{
				if (content)
				{
					m_items.clear();
					m_line_count = 0;
					m_selected = -1;
				}
				if (dimensions)
				{
					m_width = 0;
					m_height = 0;
				}
				if (title)
				{
					m_title.clear();
				}
				if (title_colour)
				{
					m_title_colour = Colours();
					m_title_anchor = EAnchor::TopCentre;
				}
				if (border)
				{
					m_border = Colours();
				}
				if (selection_colour)
				{
					m_selection_colour = Colours(EColour::Green, EColour::Default);
				}
				m_display_offset = Coord();
			}

			// Get/Set whether this pad has input focus
			bool Focus() const { return m_focused; }
			void Focus(bool on)
			{
				using namespace pr::events;
				if (m_focused == on) return;
				IRecv<Evt_Key          >::subscribe(on);
				IRecv<Evt_KeyDown      >::subscribe(on);
				IRecv<Evt_Line<char>   >::subscribe(on);
				IRecv<Evt_Line<wchar_t>>::subscribe(on);
				IRecv<Evt_Escape       >::subscribe(on);
				IRecv<Evt_Tab          >::subscribe(on);
				IRecv<Evt_FunctionKey  >::subscribe(on);
				m_focused = on;
				if (on) pr::events::Send(Evt_FocusChanged(*this)); // notify the other pads
				OnFocusChanged(*this); // notify observers
			}

			// Get/Set the fore/back colours for the pad
			Colours Colour() const { return m_colours; }
			void Colour(EColour fore, EColour back = EColour::Default) { Colour(Colours(fore,back)); }
			void Colour(Colours c) { m_colours = c; }

			// Get/Set the fore/back colours for selected items in the pad
			Colours SelectionColour() const {  return m_selection_colour; }
			void SelectionColour(EColour fore, EColour back = EColour::Default) { Colour(Colours(fore,back)); }
			void SelectionColour(Colours c) { m_selection_colour = c; }

			// Set the title for the pad
			std::string Title() const { return m_title; }
			void Title(std::string title) { Title(title, m_colours.m_fore, EAnchor::TopCentre); }
			void Title(std::string title, Colours colour, EAnchor anchor)
			{
				m_title = title;
				m_title_colour = colour;
				m_title_anchor = anchor;
			}

			// Set the border colours (use Default,Default for no border)
			void Border(EColour fore)               { Border(fore, m_colours.m_back); }
			void Border(EColour fore, EColour back) { m_border.m_fore = fore; m_border.m_back = back; }

			// True if the pad has a border
			bool HasBorder() const { return !(m_border == Colours()); }

			// True if the pad has a title
			bool HasTitle() const { return !m_title.empty(); }

			// Returns the bounds of the pad including title and border (if present) (in screen space)
			RECT WindowRect(Coord loc) const
			{
				RECT wr;
				wr.left   = loc.X;
				wr.top    = loc.Y;
				wr.right  = loc.X + WindowWidth();
				wr.bottom = loc.Y + WindowHeight();
				return wr;
			}
			RECT WindowRect(Console& cons, EAnchor anchor, int dx = 0, int dy = 0) const
			{
				return WindowRect(cons.CursorLocation(anchor, WindowWidth(), WindowHeight(), dx, dy));
			}

			// Returns the bounds of the content of the pad in screen space
			RECT ClientRect(Coord loc) const
			{
				RECT wr = WindowRect(loc);
				if (HasBorder())
				{
					wr.top    += 1;
					wr.bottom -= 1;
					wr.left   += 1;
					wr.right  -= 1;
				}
				else if (HasTitle())
				{
					wr.top += 1;
				}
				return wr;
			}
			RECT ClientRect(Console& cons, EAnchor anchor, int dx = 0, int dy = 0) const
			{
				return ClientRect(cons.CursorLocation(anchor, WindowWidth(), WindowHeight(), dx, dy));
			}

			// Get/Set the client area width/height
			size_t WindowWidth() const      { return m_width  + (HasBorder() ? 2 : 0); }
			size_t WindowHeight() const      { return m_height + (HasBorder() ? 2 : HasTitle() ? 1 : 0); }
			SIZE   WindowSize() const       { SIZE sz = {long(WindowWidth()), long(WindowHeight())}; return sz; }
			size_t Width() const            { return m_width; }
			size_t Height() const           { return m_height; }
			SIZE   Size() const             { SIZE sz = {long(Width()), long(Height())}; return sz; }
			void   Width(size_t w)          { m_width = w; }
			void   Height(size_t h)         { m_height = h; }
			void   Size(size_t w, size_t h) { m_width = w; m_height = h; }

			// Get/Set the first column/line to display in the pad
			// Uses a signed value so that negative values are accepted but clamped to [0,line_count-height)
			Coord DisplayOffset() const { return m_display_offset; }
			void DisplayOffset(int dx, int dy) { DisplayOffset(Coord(dx,dy)); }
			void DisplayOffset(Coord ofs)
			{
				SIZE sz = PreferredSize();
				int x_max = std::max(0, int(sz.cx - m_width));
				int y_max = std::max(0, int(sz.cy - m_height));
				ofs.X = short(std::max(std::min(int(ofs.X), x_max), 0));
				ofs.Y = short(std::max(std::min(int(ofs.Y), y_max), 0));
				m_display_offset = ofs;
			}

			// Get/Set the selected line
			int Selected() const { return m_selected; }
			void Selected(int s) { m_selected = std::max(std::min(s, int(m_line_count)), -1); }

			// Get the number of lines contained in the pad
			int LineCount() const { return m_line_count; }

			// Stream anything to the pad
			template <typename T> Pad& operator << (T t)
			{
				std::stringstream out; out << t;
				return *this << out.str();
			}

			// Stream a string to the pad
			Pad& operator << (std::string const& s)
			{
				char const delim[] = "\n";
				if (m_line_count == 0 && !s.empty()) m_line_count = 1;
				pr::str::Split(s, delim, [&](std::string const& s, size_t i, size_t iend)
					{
						// Concat lines where possible
						if (!m_items.empty() && m_items.back().m_what == EItem::AString) m_items.back().m_linea->append(s);
						else m_items.push_back(s.substr(i, iend-i));
						if (iend != s.size() && s[iend] == '\n') { m_items.push_back(NewLine); ++m_line_count; }
					});
				return *this;
			}
			Pad& operator << (std::wstring const& s)
			{
				wchar_t const delim[] = L"\n";
				if (m_line_count == 0 && !s.empty()) m_line_count = 1;
				pr::str::Split(s, delim, [&](std::wstring const& s, size_t i, size_t iend)
					{
						if (!m_items.empty() && m_items.back().m_what == EItem::WString) m_items.back().m_linew->append(s);
						else m_items.push_back(s.substr(i, iend-i));
						if (iend != s.size() && s[iend] == L'\n') { m_items.push_back(NewLine); ++m_line_count; }
					});
				return *this;
			}

			// Stream a colour change
			Pad& operator << (Colours c)
			{
				m_items.push_back(c);
				return *this;
			}
			Pad& operator << (EColour::Enum_ c)
			{
				m_items.push_back(Colours(c));
				return *this;
			}

			// Stream a coordinate position
			Pad& operator << (Coord c)
			{
				m_items.push_back(c);
				return *this;
			}

			// A special type
			Pad& operator << (EItem item)
			{
				m_items.push_back(item);
				return *this;
			}

			// Draw the title and border to 'cons'
			void DrawFrame(Console& cons, RECT wr, Colours base_colour) const
			{
				int w = width(wr), h = height(wr);

				// Clear the pad background
				Colours pad_colour = base_colour.Merge(m_colours);
				cons.Colour(pad_colour);
				cons.Clear(wr.left, wr.top, w, h, pad_colour);

				// Draw the border
				if (HasBorder())
				{
					cons.Colour(base_colour.Merge(m_border));
					cons.WriteBox(wr.left, wr.top, w, h);
				}

				// Draw the title
				if (HasTitle())
				{
					cons.Colour(base_colour.Merge(m_title_colour));
					int xofs = 0;
					if (m_title_anchor & EAnchor::Left   ) xofs = 0 + HasBorder();
					if (m_title_anchor & EAnchor::HCentre) xofs = int((w - m_title.size()) / 2);
					if (m_title_anchor & EAnchor::Right  ) xofs = int( w - m_title.size() - HasBorder());
					cons.Write(wr.left + xofs, wr.top, m_title.c_str());
				}
			}

			// Draw a string from the pad into the console, clipped by the bounds of the pad
			// 'loc' is the pad-space location of the current cursor position
			template <typename Char> void DrawLine(Console& cons, std::basic_string<Char> const& line, Coord loc) const
			{
				int s = std::min<int>(std::max(m_display_offset.X - loc.X, 0), line.size());
				int c = std::min<int>(line.size() - s, m_width);
				cons.Write(line.c_str(), size_t(s), size_t(c));
			}

			// Draw the pad content to 'cons'
			void DrawContent(Console& cons, RECT cr, Colours base_colour) const
			{
				Colours pad_colour = base_colour.Merge(m_colours);
				Colours col = pad_colour;
				Coord cur(0,0);
				Coord ofs(0,0); // ofs from 'cur'
				bool set_cur = true;
				bool set_col = true;
				int line_index = 0;

				// Draw the pad content
				for (auto& item : m_items)
				{
					if (item.m_what == Pad::NewLine   ) { ++line_index; ofs.X = 0; ++ofs.Y; set_cur = true; continue; }
					if (item.m_what == Pad::SetCursor ) { cur = *item.m_cursor; ofs.X = ofs.Y = 0; set_cur = true; continue; }
					if (item.m_what == Pad::SetColours) { col = *item.m_colour; set_col = true; continue; }

					Coord loc(cur.X + ofs.X, cur.Y + ofs.Y);

					// Skip lines that are outside the bounds of the pad
					if (loc.Y < m_display_offset.Y || loc.Y >= int(m_display_offset.Y + m_height))
						continue;
					
					if (line_index == m_selected || line_index == m_selected + 1) set_col = true;
					if (set_cur && (set_cur = false) == false) cons.Cursor(cr.left + cur.X + ofs.X - m_display_offset.X, cr.top + cur.Y + ofs.Y - m_display_offset.Y);
					if (set_col && (set_col = false) == false) cons.Colour(line_index == m_selected ? pad_colour.Merge(m_selection_colour) : col);

					switch (item.m_what)
					{
					default:
						throw std::exception("Unknown pad item");
					case Pad::NewLine:
					case Pad::SetColours:
					case Pad::SetCursor:
						break;
					case Pad::AString:
						DrawLine(cons, *item.m_linea, loc);
						break;
					case Pad::WString:
						DrawLine(cons, *item.m_linew, loc);
						break;
					case Pad::CurrentInput:
						if (cons.UnicodeInput()) DrawLine(cons, cons.LineInputW(), loc);
						else                     DrawLine(cons, cons.LineInputA(), loc);
						break;
					}
				}
			}

			// Write the Pad to 'cons'. Cursor position and current colour are not changed by this method
			void Draw(Console& cons, Coord loc) const
			{
				Scope s(cons);
				assert(m_width != 0 && m_height != 0 && "pad has an invalid size");
				DrawFrame(cons, WindowRect(loc), s.m_col.m_colours);
				DrawContent(cons, ClientRect(loc), s.m_col.m_colours);
			}
			void Draw(Console& cons, int x, int y) const
			{
				Draw(cons, Coord(x,y));
			}
			void Draw(Console& cons, EAnchor anchor, int dx = 0, int dy = 0) const
			{
				Draw(cons, cons.CursorLocation(anchor, WindowWidth(), WindowHeight(), dx, dy));
			}

			// Returns the size the pad should have to display all content
			SIZE PreferredSize() const { return PreferredSize(std::string()); }
			template <typename Char> SIZE PreferredSize(std::basic_string<Char> const& current_input) const
			{
				Coord cur(0,0);
				SIZE sz = {0, 1};
				int w = 0, h = 1;
				for (auto& item : m_items)
				{
					switch (item.m_what)
					{
					default: throw std::exception("Unknown pad item");
					case Pad::SetColours:break;
					case Pad::NewLine: ++h; w = 0; break;
					case Pad::AString: w += (int)item.m_linea->size(); break;
					case Pad::WString: w += (int)item.m_linew->size(); break;
					case Pad::SetCursor: cur = *item.m_cursor; w = 0; h = 0; break;
					case Pad::CurrentInput: w += (int)current_input.size(); break;
					}
					sz.cx = std::max(sz.cx, long(cur.X + w));
					sz.cy = std::max(sz.cy, long(cur.Y + h));
				}
				return sz;
			}

			// Set the size of the client area to contain the content
			void AutoSize()
			{
				SIZE sz = PreferredSize();
				if (m_width  == 0) m_width  = size_t(sz.cx);
				if (m_height == 0) m_height = size_t(sz.cy);
			}
		};
		#pragma endregion

		#pragma region PadContainer
		struct PadContainer
		{
			std::list<Pad> m_pads;
		};
		#pragma endregion

		// Write output to the console using various methods. Handy way to test if it's working
		inline void OutputTest()
		{
			int iVar;

			// test stdio
			fprintf(stdout, "Test output to stdout\n");
			fprintf(stderr, "Test output to stderr\n");
			fprintf(stdout, "Enter an integer to test stdin: ");
			scanf_s("%d", &iVar);
			printf("You entered %d\n", iVar);

			//test iostreams
			std::cout << "Test output to cout" << std::endl;
			std::cerr << "Test output to cerr" << std::endl;
			std::clog << "Test output to clog" << std::endl;
			std::cout << "Enter an integer to test cin: ";
			std::cin >> iVar;
			std::cout << "You entered " << iVar << std::endl;

			// test wide iostreams
			std::wcout << L"Test output to wcout" << std::endl;
			std::wcerr << L"Test output to wcerr" << std::endl;
			std::wclog << L"Test output to wclog" << std::endl;
			std::wcout << L"Enter an integer to test wcin: ";
			std::wcin >> iVar;
			std::wcout << L"You entered " << iVar << std::endl;

			// test CrtDbg output
			_CrtSetReportMode( _CRT_ASSERT, _CRTDBG_MODE_FILE );
			_CrtSetReportFile( _CRT_ASSERT, _CRTDBG_FILE_STDERR );
			_CrtSetReportMode( _CRT_ERROR, _CRTDBG_MODE_FILE );
			_CrtSetReportFile( _CRT_ERROR, _CRTDBG_FILE_STDERR);
			_CrtSetReportMode( _CRT_WARN, _CRTDBG_MODE_FILE );
			_CrtSetReportFile( _CRT_WARN, _CRTDBG_FILE_STDERR);
			_RPT0(_CRT_WARN, "This is testing _CRT_WARN output\n");
			_RPT0(_CRT_ERROR, "This is testing _CRT_ERROR output\n");
			_ASSERT( 0 && "testing _ASSERT" );
			_ASSERTE( 0 && "testing _ASSERTE" );
		}
	}

	typedef console::Console Console;

	// Singleton access to the console
	inline Console& cons() { static Console s_cons; return s_cons; }
}

#endif
