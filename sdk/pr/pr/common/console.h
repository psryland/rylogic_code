//**************************************************************************************
// Console
//  Copyright © Rylogic Ltd 2010
//**************************************************************************************
#ifndef PR_CONSOLE_H
#define PR_CONSOLE_H

#if _WIN32_WINNT < 0x0500
#	error "Console requires _WIN32_WINNT >= 0x0500"
#endif//_WIN32_WINNT < 0x0500

#include <windows.h>
#include <stdio.h>
#include <fcntl.h>
#include <io.h>
#include <iostream>
#include <fstream>
#include <conio.h>
#include <crtdbg.h>
#include <string>
#include <sstream>
#include <stdarg.h>
#include <tchar.h>

namespace pr
{
	namespace console
	{
		namespace EEvent
		{
			enum Type
			{
				Key   = KEY_EVENT,                // Event contains key event record
				Mouse = MOUSE_EVENT,              // Event contains mouse event record
				Size  = WINDOW_BUFFER_SIZE_EVENT, // Event contains window change event record
				Menu  = MENU_EVENT,               // Event contains menu event record
				Focus = FOCUS_EVENT,              // event contains focus change
				Any   = Key|Mouse|Size|Menu|Focus,
			};
		}

		typedef INPUT_RECORD Event;
		typedef BOOL (__stdcall *HandlerFunction)(DWORD ctrl_type);

		class Console
		{
			HANDLE m_stdout;
			HANDLE m_stdin;
			HANDLE m_stderr;
			bool   m_opened;
			bool   m_console_created;
			int    m_base;
			int    m_digits;

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
			void Open(short columns, short lines)
			{
				Open();
				SetConsoleDimensions(columns, lines);
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
	
			// Return the dimensions of the console window
			COORD GetConsoleDimensions()
			{
				CONSOLE_SCREEN_BUFFER_INFO csbi;
				GetConsoleScreenBufferInfo(m_stdout, &csbi);
				return csbi.dwSize;
			}
			void GetConsoleDimensions(short& columns, short& lines)
			{
				COORD size = GetConsoleDimensions();
				columns = size.X;
				lines   = size.Y;
			}
	
			// Set the buffer size for the console
			void SetConsoleDimensions(short columns, short lines)
			{
				COORD size = {columns, lines};
				SetConsoleScreenBufferSize(m_stdout, size);
			}
	
			// Return the position of the cursor
			COORD GetCursor()
			{
				CONSOLE_SCREEN_BUFFER_INFO csbi;
				GetConsoleScreenBufferInfo(m_stdout, &csbi);
				return csbi.dwCursorPosition;
			}
	
			// Position the cursor in the window
			void SetCursor(COORD coord)
			{
				SetConsoleCursorPosition(m_stdout, coord);
			}
			void SetCursor(short cx, short cy)
			{
				COORD coord = {cx, cy};
				SetCursor(coord);
			}
	
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
				CONSOLE_SCREEN_BUFFER_INFO csbi;
				GetConsoleScreenBufferInfo(m_stdout, &csbi);
				unsigned int num_chars = csbi.dwSize.X * csbi.dwSize.Y;
				
				// Fill the area with blanks
				COORD topleft = {0, 0}; 
				DWORD chars_written;
				FillConsoleOutputCharacter(m_stdout, _T(' '), num_chars, topleft, &chars_written);

				// Get the current text attribute
				GetConsoleScreenBufferInfo(m_stdout, &csbi);

				// Set the buffer's attributes accordingly
				FillConsoleOutputAttribute(m_stdout, csbi.wAttributes, num_chars, topleft, &chars_written);
			    
				// Set the cursor back to cx,cy
				SetCursor(0, 0);
			}
	
			// Clear a rectangular area in the console
			// If size_x == 0 or size_y == 0 then the current console width/height is used
			void Clear(short x, short y, short size_x, short size_y)
			{
				// Use the whole screen clear
				if (x == 0 && y == 0 && size_x == 0 && size_y == 0) return Clear();

				// Get the number of character cells in the current buffer
				CONSOLE_SCREEN_BUFFER_INFO csbi;
				GetConsoleScreenBufferInfo(m_stdout, &csbi);
				x	   = (x < 0) ? 0 : (x >= csbi.dwSize.X) ? csbi.dwSize.X - 1 : x;
				y	   = (y < 0) ? 0 : (y >= csbi.dwSize.Y) ? csbi.dwSize.Y - 1 : y;
				size_x = (size_x == 0) ? csbi.dwSize.X : size_x;
				size_y = (size_y == 0) ? csbi.dwSize.Y : size_y;
				size_x = (size_x < 0) ? 0 : (x + size_x > csbi.dwSize.X) ? csbi.dwSize.X - x : size_x;
				size_y = (size_y < 0) ? 0 : (y + size_y > csbi.dwSize.Y) ? csbi.dwSize.Y - y : size_y;
				
				// Fill the area with blanks
				COORD topleft = {x, y}; 
				for( topleft.Y = y; topleft.Y != y + size_y; ++topleft.Y )
				{
					DWORD chars_written;
					FillConsoleOutputCharacter(m_stdout, _T(' '), size_x, topleft, &chars_written);

					// Get the current text attribute
					GetConsoleScreenBufferInfo(m_stdout, &csbi);

					// Set the buffer's attributes accordingly
					FillConsoleOutputAttribute(m_stdout, csbi.wAttributes, size_x, topleft, &chars_written);
				}
			    
				// Set the cursor back to x,y
				SetCursor(x, y);
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
			void Write(std::string const& str)
			{
				DWORD chars_written;
				WriteConsole(m_stdout, str.c_str(), (DWORD)str.size(), &chars_written, 0);
			}
	
			// Write text to the output window at a specified position
			void Write(short cx, short cy, std::string const& str)
			{
				SetCursor(cx, cy);
				Write(str);
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
		};
	
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
