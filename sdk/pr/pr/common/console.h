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
#include <windows.h>
#include <stdio.h>
#include <fcntl.h>
#include <io.h>
#include <conio.h>
#include <crtdbg.h>
#include <stdarg.h>
#include <tchar.h>
#include "pr/macros/enum.h"
#include "pr/str/prstring.h"

namespace pr
{
	namespace console
	{
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
			x(Black       , = 0                     )\
			x(Blue        , = 1 << 0                )\
			x(Green       , = 1 << 1                )\
			x(Red         , = 1 << 2                )\
			x(Cyan        , = Blue|Green            )\
			x(Purple      , = Blue|Red              )\
			x(Yellow      , = Green|Red             )\
			x(Grey        , = Blue|Green|Red        )\
			x(LightBlue   , = (1<<3)|Blue           )\
			x(LightGreen  , = (1<<3)|Green          )\
			x(LightRed    , = (1<<3)|Red            )\
			x(LightCyan   , = (1<<3)|Blue|Green     )\
			x(LightPurple , = (1<<3)|Blue|Red       )\
			x(LightYellow , = (1<<3)|Green|Red      )\
			x(White       , = (1<<3)|Red|Green|Blue )\
			x(Default     , = 1 << 16               )
		PR_DEFINE_ENUM2_FLAGS(EColour, PR_ENUM);
		#undef PR_ENUM

		class Console;
		typedef INPUT_RECORD Event;
		typedef BOOL (__stdcall *HandlerFunction)(DWORD ctrl_type);

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
			static Colours From(WORD colours) { return Colours(colours & 0xf, (colours >> 4) & 0xf); }
			operator WORD() const { return WORD((m_back << 4) | m_fore); }
			Colours Merge(Colours rhs) const
			{
				return Colours(
					rhs.m_fore != EColour::Default ? rhs.m_fore : m_fore,
					rhs.m_back != EColour::Default ? rhs.m_back : m_back);
			}
		};

		// RAII object for preserving the cursor position
		struct CursorScope
		{
			Console* m_cons;
			COORD m_cursor_pos;
			CursorScope(Console& cons);
			~CursorScope();
		};

		// RAII object for pushing console colours
		struct ColourScope
		{
			Console* m_cons;
			Colours m_colours;
			ColourScope(Console& cons);
			~ColourScope();
		};

		// A helper for drawing rectangular blocks of text in the console
		struct Pad
		{
			struct Item
			{
				enum EWhat { Unknown, NewLine, String, WString, Colour } m_what;
				union { void* m_ptr; std::string* m_line; std::wstring* m_wline; Colours* m_colour; };
				Item(EWhat what, void* ptr = 0) :m_what(what), m_ptr(ptr) {}
				Item(std::string* line)  :m_what(String ), m_line(line) {}
				Item(std::wstring* line) :m_what(WString), m_wline(line) {}
				Item(Colours* colour)    :m_what(Colour ), m_colour(colour) {}
			};

			Colours m_colours;
			Colours m_border;
			Colours m_title_colour;
			std::string m_title;
			EAnchor m_title_anchor;
			int m_width;
			int m_height;
			int m_w, m_h;
			std::vector<Item> m_items;

			Pad(EColour fore = EColour::Default, EColour back = EColour::Default)
				:m_colours(fore, back)
				,m_border()
				,m_title_colour()
				,m_title()
				,m_title_anchor(EAnchor::TopCentre)
				,m_width(0)
				,m_height(1)
				,m_w(0)
				,m_h(1)
				,m_items()
			{}
			~Pad()
			{
				for (auto& i : m_items)
					delete i.m_ptr;
			}

			// Set the title for the pad
			void Title(std::string title) { Title(title, m_colours.m_fore, EAnchor::TopCentre); }
			void Title(std::string title, Colours colour, EAnchor anchor) { m_title = title; m_title_colour = colour; m_title_anchor = anchor; }

			// Set the border colours (use Default,Default for no border)
			void Border(EColour fore)               { Border(fore, m_colours.m_back); }
			void Border(EColour fore, EColour back) { m_border.m_fore = fore; m_border.m_back = back; }

			// Stream anything to the pad
			template <typename T> Pad& operator << (T t)
			{
				std::stringstream out; out << t;
				return *this << out.str();
			}

			// Stream a string to the pad
			template <typename Char> Pad& operator << (std::basic_string<Char> const& s)
			{
				Char const delim[] = {Char('\n'), 0};
				pr::str::Split<std::basic_string<Char>, Char>(s, delim, [&](std::basic_string<Char> const& s, size_t i, size_t iend)
					{
						m_items.push_back(new std::basic_string<Char>(s.substr(i, iend-i)));
						m_width  = std::max(m_width, m_w += int(iend - i));
						if (iend != s.size() && s[iend] == Char('\n'))
						{
							m_items.push_back(Item(Item::NewLine));
							m_height = std::max(m_height, ++m_h);
							m_w = 0;
						}
					});
				return *this;
			}

			// Stream a colour change
			Pad& operator << (Colours c)
			{
				m_items.push_back(new Colours(c));
				return *this;
			}

		private:
			Pad(Pad const&);
			Pad& operator =(Pad const&);
		};

		template <typename Str> inline void append(Str& str, size_t count, typename Str::value_type ch) { str.append(count, ch); }
		template <typename Str> inline void resize(Str& str, size_t sz)                                 { str.resize(sz); }
		template <typename Str> inline size_t size(Str& str)                                            { return str.size(); }
		template <typename Str> inline bool empty(Str& str)                                             { return str.empty(); }

		class Console
		{
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

			struct traits
			{
				// Write to the back buffer, it will point to the active screen buffer when double buffering isn't enabled
				static void write(HANDLE out, char    const* str, size_t ofs, size_t count) { DWORD chars_written; WriteConsoleA(out, str + ofs, DWORD(count), &chars_written, 0); }
				static void write(HANDLE out, wchar_t const* str, size_t ofs, size_t count) { DWORD chars_written; WriteConsoleW(out, str + ofs, DWORD(count), &chars_written, 0); }
				static size_t length(char const* str)    { return strlen(str); }
				static size_t length(wchar_t const* str) { return wcslen(str); }
				static void read(KEY_EVENT_RECORD evt, char&    ch) { ch = evt.uChar.AsciiChar; }
				static void read(KEY_EVENT_RECORD evt, wchar_t& ch) { ch = evt.uChar.UnicodeChar; }
			};



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
			void Cursor(COORD coord) { SetConsoleCursorPosition(*m_back, coord); }
			void Cursor(int x, int y) { Cursor(Coord(x,y)); }
			void Cursor(EAnchor anchor, int dx = 0, int dy = 0) { Cursor(CursorLocation(anchor, 1, 1, dx, dy)); }

			// Get/Set the colour to use
			Colours Colour() const { return Info().wAttributes; }
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
				CursorScope s0(*this);
				auto c = m_colour.Merge(col);

				// Get the number of character cells in the current buffer
				auto info = Info();
				unsigned int num_chars = info.dwSize.X * info.dwSize.Y;

				// Fill the area with blanks
				Coord topleft(0,0);
				DWORD chars_written;
				FillConsoleOutputCharacterW(*m_back, L' ', num_chars, topleft, &chars_written);
				FillConsoleOutputAttribute (*m_back, c, num_chars, topleft, &chars_written);
			}
			void Clear(int x, int y, int sx, int sy) { Clear(x,y,sx,sy,m_colour); }
			void Clear(int x, int y, int sx, int sy, Colours col) { Clear(x,y,sx,sy,true,col); }
			void Clear(int x, int y, int sx, int sy, bool clear_text, Colours col)
			{
				CursorScope s0(*this);
				auto c = m_colour.Merge(col);

				// Get the number of character cells in the current buffer
				auto info = Info();
				x  = (x  <  0) ? 0 : (x >= info.dwSize.X) ? info.dwSize.X - 1 : x;
				y  = (y  <  0) ? 0 : (y >= info.dwSize.Y) ? info.dwSize.Y - 1 : y;
				sx = (sx == 0) ? info.dwSize.X : sx;
				sy = (sy == 0) ? info.dwSize.Y : sy;
				sx = (sx <  0) ? 0 : (x + sx > info.dwSize.X) ? info.dwSize.X - x : sx;
				sy = (sy <  0) ? 0 : (y + sy > info.dwSize.Y) ? info.dwSize.Y - y : sy;

				// Fill the area with blanks
				for (int yend = y + sy; y != yend; ++y)
				{
					DWORD chars_written;
					Coord pt(x,y);
					FillConsoleOutputAttribute (*m_back, c, sx, pt, &chars_written);
					if (clear_text)
						FillConsoleOutputCharacterW(*m_back, L' ', sx, pt, &chars_written);
				}
			}

			// Flush all input events from the input buffer
			void Flush()
			{
				FlushConsoleInputBuffer(m_stdin);
			}


			// Create separate FIFO buffers for the event types; key, mouse, etc..
			// Create a pump input function that reads from the input events adding to the appropriate buffer
			// Write methods for getting from a FIFO buffer that handle


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

			// Comsume input events until one that passes 'pred' is found.
			// Returns true if an event is found, false if the input buffer empties first
			template <typename Pred> bool SeekEvent(Pred pred) const
			{
				for (;InputEventCount() != 0; ReadInputEvent())
					if (pred(PeekInputEvent()))
						return true;
				return false;
			}
			bool SeekKeyEvent() const
			{
				return SeekEvent([](Event e){ return (e.EventType & EEvent::Key) != 0; });
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
			// Returns true if a char was read from the input buffer, false if timed out
			// 'wait_ms' is the length of time to wait for a key event
			template <typename Char> bool ReadChar(Char& ch, DWORD wait_ms = 0) const
			{
				for(;;)
				{
					if (SeekKeyEvent())
					{
						traits::read(ReadInputEvent().Event.KeyEvent, ch);
						return true;
					}
					if (!WaitForEvent(EEvent::Key, wait_ms))
					{
						return false;
					}
				}
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

			//// Accumulates keyboard input on repeated calls until a complete line has been
			//// entered or until escape is pressed while 'input' is empty.
			//// Returns true if 'input' contains a complete line or escape was pressed, false otherwise.
			//template <typename Str> bool AccumulateLine(Str& input, bool& escape)
			//{
			//	typedef Str::value_type Char;
			//	escape = false;
			//	for (;;)
			//	{
			//		if (!SeekKeyEvent()) return false;
			//		auto evt = ReadInputEvent();
			//		Coord pos = Cursor();

			//		// Enter terminates the line of user input
			//		if (evt.Event.KeyEvent.wVirtualKeyCode == VK_RETURN)
			//		{
			//			return true;
			//		}
			//		if (evt.Event.KeyEvent.wVirtualKeyCode == VK_ESCAPE)
			//		{
			//			// Escape, when 'input' is not empty, clears the user input
			//			if (!empty(input))
			//			{
			//				// Reset the input and erase the echoed characters
			//				WORD w = std::min(WORD(size(input)), WORD(pos.X));
			//				Clear(pos.X -= w, pos.Y, w, 1);
			//				Cursor(pos);
			//				resize(input, 0);
			//				return false;
			//			}
			//			escape = true;
			//			return true;
			//		}
			//		if (evt.Event.KeyEvent.wVirtualKeyCode == VK_DELETE)
			//		{
			//			WORD w = std::min(WORD(size(input)), evt.Event.KeyEvent.wRepeatCount);
			//			Clear(pos.X -= w, pos.Y, w, 1);
			//			Cursor(pos);
			//			resize(input, size(input) - w);
			//			return false;
			//		}
			//		if (evt.Event.KeyEvent.wVirtualKeyCode == VK_TAB)
			//		{
			//			append(input, evt.Event.KeyEvent.wRepeatCount, Char('\t'));
			//			return false;
			//		}
			//		
			//		Char ch;
			//		traits::read(evt.Event.KeyEvent, ch);
			//		if (ch != 0)
			//			append(input, evt.Event.KeyEvent.wRepeatCount, ch);
			//		return false;
			//	}
			//}

			// Write text to the output window
			template <typename Char> void Write(Char const* str, size_t ofs, size_t count)
			{
				traits::write(*m_back, str, ofs, count);
			}
			template <typename Char> void Write(Char const* str)
			{
				Write(str, 0, traits::len(str));
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

				// Get the console dimensions
				auto info = Info();
				int maxx = info.dwMaximumWindowSize.X;
				int maxy = info.dwMaximumWindowSize.Y;

				// Find the coordinate to write the string at
				int x = 0, y = 0;
				if (anchor & EAnchor::Left   ) x = dx + 0;
				if (anchor & EAnchor::HCentre) x = dx + (maxx - sx) / 2;
				if (anchor & EAnchor::Right  ) x = dx + (maxx - sx);
				if (anchor & EAnchor::Top    ) y = dy + 0;
				if (anchor & EAnchor::VCentre) y = dy + (maxy - sy) / 2;
				if (anchor & EAnchor::Bottom ) y = dy + (maxy - sy);

				// Write the string, line by line
				for (Char const *e, *p = str; *p; ++p)
				{
					for (e = p + 1; *e && *e != Char('\n'); ++e) {}
					Write(x, y++, p, 0, e - p - 1);
				}
			}

			// Write a Pad helper object to the screen
			void Write(EAnchor anchor, Pad const& pad, int dx = 0, int dy = 0)
			{
				CursorScope s0(*this);
				ColourScope s1(*this);
				auto base_colour = s1.m_colours;

				// Get the basic area of the pad
				int w = pad.m_width;
				int h = pad.m_height;
				bool has_border = pad.m_border != Colours();
				bool has_title  = pad.m_title.empty() == false;

				if (has_border)     { h += 2; w += 2; }
				else if (has_title) { h += 1; }
				Coord loc = CursorLocation(anchor, w, h, dx, dy);

				if (has_border)
				{
					Colour(base_colour.Merge(pad.m_border));
					WriteBox(anchor, w, h, dx, dy);
				}
				if (has_title)
				{
					Colour(base_colour.Merge(pad.m_title_colour));
					int xofs;
					if (pad.m_title_anchor & EAnchor::Left   ) xofs = 0;
					if (pad.m_title_anchor & EAnchor::HCentre) xofs = int((w - pad.m_title.size()) / 2);
					if (pad.m_title_anchor & EAnchor::Right  ) xofs = int(w - pad.m_title.size());
					Write(loc.X + xofs, loc.Y, pad.m_title.c_str());
				}

				if (has_border)     { h -= 2; w -= 2; loc.X += 1; loc.Y += 1; }
				else if (has_title) { h -= 1; loc.Y += 1; }

				// Clear the pad background
				Colours pad_colour = base_colour.Merge(pad.m_colours);
				Colour(pad_colour);
				Clear(loc.X, loc.Y, w, h, pad_colour);

				// Draw the pad content
				Cursor(loc);
				for (auto& item : pad.m_items)
				{
					switch (item.m_what)
					{
					case Pad::Item::NewLine:
						loc.Y++;
						Cursor(loc);
						break;
					case Pad::Item::String:
						Write(item.m_line->c_str(), 0, item.m_line->size());
						break;
					case Pad::Item::WString:
						Write(item.m_wline->c_str(), 0, item.m_wline->size());
						break;
					case Pad::Item::Colour:
						Colour(pad_colour.Merge(*item.m_colour));
						break;
					}
				}
			}

			// Draw a box with size sx,sy
			void WriteBox(EAnchor anchor, int w, int h, int dx = 0, int dy = 0)
			{
				DWORD chars_written;
				Coord loc = CursorLocation(anchor, w, h, dx, dy);
				FillConsoleOutputCharacterW(*m_back, L'╔', 1  , Coord(loc.X    ,loc.Y), &chars_written);
				FillConsoleOutputCharacterW(*m_back, L'═', w-2, Coord(loc.X+1  ,loc.Y), &chars_written);
				FillConsoleOutputCharacterW(*m_back, L'╗', 1  , Coord(loc.X+w-1,loc.Y), &chars_written);
				for (int y = loc.Y+1, yend = y+h-2; y != yend; ++y)
				{
					FillConsoleOutputCharacterW(*m_back, L'║', 1, Coord(loc.X    ,y), &chars_written);
					FillConsoleOutputCharacterW(*m_back, L'║', 1, Coord(loc.X+w-1,y), &chars_written);
				}
				FillConsoleOutputCharacterW(*m_back, L'╚', 1  , Coord(loc.X    ,loc.Y+h-1), &chars_written);
				FillConsoleOutputCharacterW(*m_back, L'═', w-2, Coord(loc.X+1  ,loc.Y+h-1), &chars_written);
				FillConsoleOutputCharacterW(*m_back, L'╝', 1  , Coord(loc.X+w-1,loc.Y+h-1), &chars_written);
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
			template <typename Char> void MeasureString(Char const* str, int& w, int& h)
			{
				int x = 0; w = 0; h = 0;
				for (Char const* p = str; *p; ++p)
				{
					if (*p == Char('\n')) { ++h; x = 0; }
					else if (++x > w)     { ++w; }
				}
			}
		};

		// RAII object for preserving the cursor position
		inline CursorScope::CursorScope(Console& cons) :m_cons(&cons) ,m_cursor_pos(m_cons->Cursor()) {}
		inline CursorScope::~CursorScope() { m_cons->Cursor(m_cursor_pos); }

		// RAII object for pushing console colours
		inline ColourScope::ColourScope(Console& cons) :m_cons(&cons) ,m_colours(Colours::From(m_cons->Info().wAttributes)) {}
		inline ColourScope::~ColourScope() { SetConsoleTextAttribute(*m_cons->m_back, m_colours); }

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
