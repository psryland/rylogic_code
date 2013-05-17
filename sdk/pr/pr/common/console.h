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
			x(MiddleCentre , = VCentre|HCentre )\
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
			x(White       , = (1<<3)|Red|Green|Blue )
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

		// Helper for combining fore and back colours
		struct Colours
		{
			WORD m_colours;
			Colours(WORD colours) :m_colours(colours) {}
			Colours(EColour fore = EColour::White, EColour back = EColour::Black) :m_colours((back << 4) | fore) {}
			operator WORD() const { return m_colours; }
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
			WORD m_colours;
			ColourScope(Console& cons);
			~ColourScope();
		};

		// A helper for drawing rectangular blocks of text in the console
		struct Pad
		{
			struct Item
			{
				enum EWhat { Unknown, NewLine, String, Colour } m_what;
				union { void* m_ptr; std::string* m_line; Colours* m_colour; };
				Item(EWhat what, void* ptr = 0) :m_what(what), m_ptr(ptr) {}
				Item(std::string* line) :m_what(String), m_line(line) {}
				Item(Colours* colour) :m_what(Colour), m_colour(colour) {}
			};

			Colours m_colours;
			int m_width, m_height, m_w;
			std::vector<Item> m_items;

			Pad(EColour fore = EColour::White, EColour back = EColour::Black) :m_colours(fore,back) ,m_width(0) ,m_height(0) ,m_w(0) ,m_items() {}
			~Pad() { for (auto& i : m_items) delete i.m_ptr; }
			
			// Stream anything to the pad
			template <typename T> Pad& operator << (T t)
			{
				std::stringstream out; out << t;
				return *this << out.str();
			}

			// Stream a string to the pad
			Pad& operator << (std::string const& s)
			{
				pr::str::Split(s,"\n", [&](std::string const& s, int i, int iend)
					{
						m_items.push_back(new std::string(s.c_str()+i, s.c_str()+iend));
						m_w += (iend - i);
						m_width = std::max(m_width, m_w);
						if (s[iend] == '\n')
						{
							m_items.push_back(Item(Item::NewLine));
							m_w = 0;
							m_height++;
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
		};

		class Console
		{
			HANDLE m_stdout;
			HANDLE m_stdin;
			HANDLE m_stderr;
			bool   m_opened;
			bool   m_console_created;
			int    m_base;
			int    m_digits;

			friend struct CursorScope;
			friend struct ColourScope;
		public:
			Console()
				:m_stdout(0)
				,m_stdin(0)
				,m_opened(false)
				,m_base(10)
				,m_digits(3)
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

			// Open a console window
			void Open()
			{
				if (m_opened) return;
				m_console_created = AllocConsole() == TRUE;
				m_stdout = GetStdHandle(STD_OUTPUT_HANDLE);
				m_stdin  = GetStdHandle(STD_INPUT_HANDLE);
				m_stderr = GetStdHandle(STD_ERROR_HANDLE);
				m_opened = true;
			}
			void Open(int columns, int lines)
			{
				Open();
				auto info = Info();
				info.srWindow.Left   = 0;
				info.srWindow.Top    = 0;
				info.srWindow.Right  = info.dwSize.X = info.dwMaximumWindowSize.X = columns;
				info.srWindow.Bottom = info.dwSize.Y = info.dwMaximumWindowSize.Y = lines;
				Info(info);
			}

			// Closes the console window
			void Close()
			{
				if (!m_opened) return;
				if (m_stdout != GetStdHandle(STD_OUTPUT_HANDLE)) CloseHandle(m_stdin);
				if (m_stdin  != GetStdHandle(STD_INPUT_HANDLE )) CloseHandle(m_stdout);
				if (m_stderr != GetStdHandle(STD_ERROR_HANDLE )) CloseHandle(m_stderr);
				if (m_console_created)	FreeConsole();
				m_opened = false;
			}

			//// Reopen the stdio file descriptors
			//void ReopenStdio()
			//{
			//	FILE arr[3]
			//	FILE* = &arr[0];
			//	&(FILE*)[0]
			//	FILE** f = &__iob_func()[0];
			//	freopen_s(stdin  ,"CONIN$"  ,"rb" ,stdin );
			//	freopen_s(stdout ,"CONOUT$" ,"wb" ,stdout);
			//	freopen_s(stderr ,"CONOUT$" ,"wb" ,stderr);
			//}

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

			// Attach to an existing console window
			bool Attach()
			{
				m_console_created = false;
				m_opened = AttachConsole(DWORD(-1)) == TRUE;
				return m_opened;
			}

			// Return true if the console is open
			bool IsOpen() const
			{
				return m_opened;
			}

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

			// Get/Set the dimensions of the console window and/or buffer
			// Note, the buffer must not be smaller than the window or the window larger than the buffer
			ConsoleScreenBufferInfo Info() const
			{
				ConsoleScreenBufferInfo info;
				GetConsoleScreenBufferInfoEx(m_stdout, &info);
				return info;
			}
			void Info(ConsoleScreenBufferInfo info)
			{
				info.dwSize.X = std::min(info.dwSize.X, info.dwMaximumWindowSize.X);
				info.dwSize.Y = std::min(info.dwSize.Y, info.dwMaximumWindowSize.Y);
				info.dwCursorPosition.X = std::max(std::min(info.dwCursorPosition.X, info.dwSize.X), SHORT(0));
				info.dwCursorPosition.Y = std::max(std::min(info.dwCursorPosition.Y, info.dwSize.Y), SHORT(0));
				if (!SetConsoleScreenBufferInfoEx(m_stdout, &info))
					throw std::exception("Failed to set console dimensions");
			}

			// Get/Set the position of the cursor
			COORD Cursor() const { return Info().dwCursorPosition; }
			void Cursor(COORD coord) { SetConsoleCursorPosition(m_stdout, coord); }
			void Cursor(int cx, int cy) { COORD coord = {short(cx), short(cy)}; Cursor(coord); }

			// Get/Set the colour to use
			Colours Colour() const { return Info().wAttributes; }
			void Colour(Colours c) { SetConsoleTextAttribute(m_stdout, c); }
			void Colour(EColour fore, EColour back) { Colour(Colours(fore,back)); }

			// Set an event handler routine for the console, only affects this console, doesn't require removing
			// 'ctrl_type' is one of:
			//	CTRL_C_EVENT - Ctrl+C
			//	CTRL_BREAK_EVENT - Ctrl+Break
			//	CTRL_CLOSE_EVENT - Close button pressed
			//	CTRL_LOGOFF_EVENT - User log off
			//	CTRL_SHUTDOWN_EVENT - System shutdown
			// Handler routines form a stack, last added = first called. Return true if the handler handles the event
			// 'add' indicates whether the handler should be added or removed.
			// if 'HandlerRoutine' is null, 'add' == true means ignore Ctrl+C, 'add' == false means exit on Ctrl+C
			void SetHandler(HandlerFunction func, bool add)
			{
				SetConsoleCtrlHandler(func, add);
			}

			// Clear the whole console
			void Clear()
			{
				// Get the number of character cells in the current buffer
				auto info = Info();
				unsigned int num_chars = info.dwSize.X * info.dwSize.Y;

				CursorScope scope(*this);

				// Fill the area with blanks
				COORD topleft = {0, 0};
				DWORD chars_written;
				FillConsoleOutputCharacterA(m_stdout, ' ', num_chars, topleft, &chars_written);
				FillConsoleOutputAttribute(m_stdout, info.wAttributes, num_chars, topleft, &chars_written);
			}

			// Clear a rectangular area in the console
			// If size_x == 0 or size_y == 0 then the current console width/height is used
			void Clear(short x, short y, short size_x, short size_y)
			{
				// Use the whole screen clear
				if (x == 0 && y == 0 && size_x == 0 && size_y == 0)
					return Clear();

				CursorScope scope(*this);

				// Get the number of character cells in the current buffer
				auto info = Info();
				x      = (x < 0) ? 0 : (x >= info.dwSize.X) ? info.dwSize.X - 1 : x;
				y      = (y < 0) ? 0 : (y >= info.dwSize.Y) ? info.dwSize.Y - 1 : y;
				size_x = (size_x == 0) ? info.dwSize.X : size_x;
				size_y = (size_y == 0) ? info.dwSize.Y : size_y;
				size_x = (size_x < 0) ? 0 : (x + size_x > info.dwSize.X) ? info.dwSize.X - x : size_x;
				size_y = (size_y < 0) ? 0 : (y + size_y > info.dwSize.Y) ? info.dwSize.Y - y : size_y;

				// Fill the area with blanks
				COORD topleft = {x, y}; 
				for (topleft.Y = y; topleft.Y != y + size_y; ++topleft.Y)
				{
					DWORD chars_written;
					FillConsoleOutputCharacterA(m_stdout, ' ', size_x, topleft, &chars_written);
					FillConsoleOutputAttribute(m_stdout, info.wAttributes, size_x, topleft, &chars_written);
				}
			}

			// Flush all input events from the input buffer
			void Flush()
			{
				FlushConsoleInputBuffer(m_stdin);
			}

			// Return the number of events in the input buffer
			DWORD GetInputEventCount()
			{
				DWORD count;
				return GetNumberOfConsoleInputEvents(m_stdin, &count) ? count : 0;
			}

			// Return the next event from the input buffer without removing it
			Event PeekInputEvent()
			{
				_ASSERT(GetInputEventCount() != 0);
				Event in; DWORD read;
				PeekConsoleInput(m_stdin, &in, 1, &read);
				return in;
			}

			// Return the next event from the input buffer
			Event ReadInputEvent()
			{
				_ASSERT(GetInputEventCount() != 0);
				Event in; DWORD read;
				ReadConsoleInput(m_stdin, &in, 1, &read);
				return in;
			}

			// Wait for a specific event to be next in the console input buffer
			// This function blocks for up to 'timeout_ms'
			// 'event_type' is a combination of 'EEvent'
			// Returns true if the correct event occurred, false if the wait timed out
			bool WaitForEvent(WORD event_type, DWORD timeout_ms)
			{
				while (WaitForSingleObject(m_stdin, timeout_ms) == WAIT_OBJECT_0)
				{
					if (PeekInputEvent().EventType & event_type) return true;
					ReadInputEvent(); // Consume the event of the wrong type
				}
				return false;
			}

			// Wait for a key to be pressed
			void WaitKey()
			{
				WaitForEvent(EEvent::Key, INFINITE);
			}

			// Returns true if input data is waiting, equivalent to _kbhit()
			bool KBHit()
			{
				return WaitForEvent(EEvent::Key, 0);
			}

			// Look for an ascii char in the input buffer.
			bool SeekChar()
			{
				for (;GetInputEventCount() != 0; ReadInputEvent())
				{
					Event in = PeekInputEvent();
					if ((in.EventType & EEvent::Key) == 0) continue;
					if (in.Event.KeyEvent.uChar.AsciiChar == 0) continue;
					return true;
				}
				return false;
			}

			// Consume input events upto and including the next 'key' event
			// Returns true if a char was read from the input buffer
			bool ReadChar(char& ch)
			{
				if (!SeekChar()) return false;
				Event in = ReadInputEvent();
				ch = in.Event.KeyEvent.uChar.AsciiChar;
				return true;
			}

			// Read a character, blocks until input is available
			char GetChar()
			{
				char ch;
				for (;!ReadChar(ch);) WaitForEvent(EEvent::Key, INFINITE);
				return ch;
			}

			// Read up to a '\n' character
			// Blocks until a return character is read
			std::string GetLine()
			{
				std::string str;
				for (char ch = GetChar(); ch != '\n' && ch != '\r'; ch = GetChar())
					str.push_back(ch);
				Flush();
				return str;
			}

			// Read number characters from the console.
			// Blocks until the first non-digit character is read
			std::string GetNumber()
			{
				std::string str;
				for (char ch = GetChar(); isdigit(ch) || ch == '.' || ch == 'e' || ch == 'E' || ch == '-' || ch == '+'; ch = GetChar())
					str.push_back(ch);
			}

			// Get a specific number of characters from the console
			// Blocks until 'max_length' chars are read
			void GetString(std::string& str, std::size_t max_length)
			{
				str.reserve(max_length);
				for (std::size_t i = 0; i != max_length; ++i)
					str.push_back(GetChar());
			}

			// Write text to the output window
			void Write(char const* str, size_t ofs, size_t count)
			{
				DWORD chars_written;
				WriteConsole(m_stdout, str + ofs, DWORD(count), &chars_written, 0);
			}
			void Write(char const* str)
			{
				Write(str, 0, strlen(str));
			}

			// Write text to the output window
			void Write(std::string const& str, size_t ofs = 0U, size_t count = ~0U)
			{
				Write(str.c_str(), ofs, std::min(str.size() - ofs, count));
			}

			// Write text to the output window at a specified position
			void Write(int cx, int cy, std::string const& str, size_t ofs = 0U, size_t count = ~0U)
			{
				Cursor(short(cx), short(cy));
				Write(str, ofs, count);
			}

			// Write text to the output window at a specified position
			void Write(EAnchor anchor, int dx, int dy, std::string const& str)
			{
				// Find the dimensions of the string
				int sx = 0, sy = 0, x = 0;
				for (auto i = std::begin(str), iend = std::end(str); i != iend; ++i)
				{
					if (*i == '\n')    { ++sy; x = 0; }
					else if (++x > sx) { ++sx; }
				}

				// Get the console dimensions
				auto info = Info();
				int maxx = info.dwMaximumWindowSize.X, maxy = info.dwMaximumWindowSize.Y;

				// Find the coordinate to write the string at
				int cx = 0, cy = 0;
				if (anchor & EAnchor::Left   ) cx = dx + 0;
				if (anchor & EAnchor::HCentre) cx = dx + (maxx - sx) / 2;
				if (anchor & EAnchor::Right  ) cx = dx + (maxx - sx);
				if (anchor & EAnchor::Top    ) cy = dy + 0;
				if (anchor & EAnchor::VCentre) cy = dy + (maxy - sy) / 2;
				if (anchor & EAnchor::Bottom ) cy = dy + (maxy - sy);

				// Write the string, line by line
				for (size_t i = 0, iend = str.find('\n', 0); i != std::string::npos; i = iend, iend = str.find('\n',i+1))
					Write(cx, cy++, str, i, iend - i);
			}
			void Write(EAnchor anchor, std::string const& str)
			{
				Write(anchor, 0, 0, str);
			}

			// Write a Pad helper object to the screen
			void Write(EAnchor anchor, Pad const& pad, int dx = 0, int dy = 0)
			{
				CursorScope scope0(*this);
				ColourScope scope1(*this);

				COORD loc = CursorLocation(anchor, dx, dy, pad.m_width, pad.m_height);

				Colour(pad.m_colours);
				Clear(loc.X, loc.Y, pad.m_width, pad.m_height);

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
					case Pad::Item::Colour:
						Colour(*item.m_colour);
						break;
					}
				}
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

			// Returns the top left corner for a rectangular region with dimensions 'width/height'
			// anchored to 'anchor' offset by 'dx,dy'
			COORD CursorLocation(EAnchor anchor, int dx, int dy, int width, int height)
			{
				// Get the console dimensions
				auto info = Info();
				int wx = info.srWindow.Right - info.srWindow.Left;
				int wy = info.srWindow.Bottom - info.srWindow.Top;

				// Find the coordinate to write the string at
				COORD c;
				if (anchor & EAnchor::Left   ) c.X = dx + 0;
				if (anchor & EAnchor::HCentre) c.X = dx + (wx - width) / 2;
				if (anchor & EAnchor::Right  ) c.X = dx + (wx - width) + 1;
				if (anchor & EAnchor::Top    ) c.Y = dy + 0;
				if (anchor & EAnchor::VCentre) c.Y = dy + (wy - height) / 2;
				if (anchor & EAnchor::Bottom ) c.Y = dy + (wy - height) + 1;
				return c;
			}
		};

		// RAII object for preserving the cursor position
		inline CursorScope::CursorScope(Console& cons) :m_cons(&cons) ,m_cursor_pos(m_cons->Cursor()) {}
		inline CursorScope::~CursorScope() { m_cons->Cursor(m_cursor_pos); }

		// RAII object for pushing console colours
		inline ColourScope::ColourScope(Console& cons) :m_cons(&cons) ,m_colours(m_cons->Info().wAttributes) {}
		inline ColourScope::~ColourScope() { SetConsoleTextAttribute(m_cons->m_stdout, m_colours); }

		// Write output to the console using various methods
		// Handy way to test if it's working
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
