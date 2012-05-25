//*****************************************************************************
//
// A starting point for creating Win32 applications
//
//*****************************************************************************
//	Usage:
//		Create a new cpp file.
//		Create a class derived from WinConsole
//		Create a function called OnStartUp():
//			e.g. void OnStartUp()	{ g_Application = new MyApp(); }
//		Override these methods:
//			virtual bool OnStartup()	{ return true;	}	// Use to set global parameters
//			virtual bool Initialise()	{ return true;	}	// Called once a window has been created
//			virtual void Main()			{				}	// Called as often as possible
//			virtual int  OnShutdown()	{ return 0;		}	// Called on exit
//			virtual LRESULT CALLBACK WindowProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam);	// Handles messages
//			virtual void Idle(DWORD until);					// Calls Sleep(until - now) until is in milliseconds
//		The console will delete the application on shutdown
//		
#include <tchar.h>
#include "pr/common/PRAssert.h"
#include "pr/common/HResult.h"
#include "pr/common/WinConsole.h"

//*****
// External application global variables. These parameters are extern'ed so
// that the user program can control the initial state of the program. They
// are all defaulted to sensible values in case the user program does not
// initialise them.
WinConsole*		g_Application		= 0;

//*****
// File globals
const TCHAR*	g_Window_Class_Name	= _T("WinConsole Window Class Name");

//*****
// Local functions
bool CreateApplicationWindow();
LRESULT CALLBACK WindowProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam);
void TokeniseCommandLine();

//*****
// WinMain: Entry point to the program. It all starts here
int WINAPI WinMain(HINSTANCE hinstance, HINSTANCE, LPSTR /*command_line*/, int show)
{
	// Call the global function to set the g_Application pointer
	OnStartUp();
	PR_ASSERT_STR(PR_DBG_COMMON, g_Application != 0, "User application must define 'g_Application'");
	if( !g_Application ) return -1;

	g_Application->m_main_app_instance	= hinstance;
	g_Application->m_icon				= LoadIcon(0, IDI_APPLICATION);
	g_Application->m_icon_small			= LoadIcon(0, IDI_APPLICATION);
	g_Application->m_cursor				= LoadCursor(0, IDC_ARROW);
	g_Application->m_show_window		= show;
	TokeniseCommandLine();

	// Allow the application to initialise things before a window is created
	if( !g_Application->OnStartup() ) return 0;

	// Create a window for this application
	if( !CreateApplicationWindow() ) return 0;

	// Display the window
	ShowWindow(g_Application->m_main_window_handle, g_Application->m_show_window);

	// Call post-window creation Initialise()
	if( !g_Application->Initialise() ) { g_Application->OnShutdown(); return 0; }

	// *** The main message pump ***
		
	// Now we're ready to recieve and process Windows messages.
    bool got_msg;
    MSG  msg; msg.message = WM_NULL;
    PeekMessage(&msg, 0, 0U, 0U, PM_NOREMOVE);

    while( msg.message != WM_QUIT )
    {
        // Use PeekMessage() if the app is active, so we can use idle time to
        // render the scene. Else, use GetMessage() to avoid eating CPU time.
        if( g_Application->m_application_active )
            got_msg = PeekMessage(&msg, 0, 0U, 0U, PM_REMOVE) != 0;
        else
            got_msg = GetMessage(&msg, 0, 0U, 0U) != 0;

		if( got_msg )
		{
			// Translate and dispatch the message
			TranslateMessage( &msg );
			DispatchMessage( &msg );
		}
        else
        {
	    	g_Application->Main();
        }
    }

	// Let the application shutdown
	int result = g_Application->OnShutdown();
	delete g_Application;
	return result;
}

//*****
// Create and register a window class,
bool CreateApplicationWindow()
{
	// Fill in the window class stucture
	WNDCLASSEX winclass;
	ZeroMemory(&winclass, sizeof(WNDCLASSEX));
	winclass.cbSize			= sizeof(WNDCLASSEX);
	winclass.style			= CS_HREDRAW | CS_VREDRAW;
	winclass.lpfnWndProc	= WindowProc;
	winclass.cbClsExtra		= 0;
	winclass.cbWndExtra		= 0;
	winclass.hInstance		= g_Application->m_main_app_instance;
	winclass.hIcon			= g_Application->m_icon;
	winclass.hIconSm		= g_Application->m_icon_small;
	winclass.hCursor		= g_Application->m_cursor;
	winclass.hbrBackground	= 0;
	winclass.lpszMenuName	= 0; 
	winclass.lpszClassName	= g_Window_Class_Name;
	
	// Register the window class
	if( !RegisterClassEx(&winclass) )
		return false;

	// Adjust the width and height of the window to allow for the window's border
	RECT rc;
	SetRect(&rc, 0, 0, g_Application->m_screen_width, g_Application->m_screen_height);
	AdjustWindowRect(&rc, g_Application->m_window_style, g_Application->m_menu != 0);

	// Create a window
	g_Application->m_main_window_handle = CreateWindowEx(
			0,									// Extra styles
			g_Window_Class_Name,				// Class name
			g_Application->m_window_title,		// Window caption
			WS_OVERLAPPEDWINDOW,				// Style, WS_POPUP | WS_VISIBLE
			g_Application->m_screen_x,			// X
			g_Application->m_screen_y,			// Y
			rc.right - rc.left,					// Width
			rc.bottom - rc.top,					// Height
			0,								// Handle to parent 
			g_Application->m_menu,				// Handle to menu
			g_Application->m_main_app_instance,	// Instance
			0								// Creation parms
			);

	if( !g_Application->m_main_window_handle )
		return false;

	// Save the window properties
	g_Application->m_window_style = GetWindowLong(g_Application->m_main_window_handle, GWL_STYLE);
    GetWindowRect(g_Application->m_main_window_handle, &g_Application->m_window_bounds);
    GetClientRect(g_Application->m_main_window_handle, &g_Application->m_client_area);
	return true;
}

//*****
// Call the application message handler if available
LRESULT CALLBACK WindowProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam)
{
	if( g_Application ) return g_Application->WindowProc(hwnd, msg, wparam, lparam);
	else				return DefWindowProc(hwnd, msg, wparam, lparam);
}

//*****
// Converts g_Application->m_command_line into m_argc, m_argv[]
void TokeniseCommandLine()
{
	_tcsncpy(g_Application->m_command_line, GetCommandLine(), MAX_PATH * sizeof(TCHAR));
	g_Application->m_command_line[MAX_PATH - 1] = '\0';
	g_Application->m_argc = 0;

	TCHAR* cmd = g_Application->m_command_line;
	size_t i = 0;
	size_t str_length = _tcsclen(g_Application->m_command_line);
	while( i < str_length )
	{
		if( cmd[i] == '"' )
		{
			if( ++i == str_length ) return;
			g_Application->m_argv[g_Application->m_argc] = &cmd[i];
			if( ++g_Application->m_argc == WinConsole::MAX_ARGVS ) return;

			while( cmd[i] != '"' ) if( ++i == str_length ) return;
			cmd[i] = '\0';
			if( ++i == str_length ) return;
		}
		else if( !isspace(cmd[i]) )
		{
			g_Application->m_argv[g_Application->m_argc] = &cmd[i];
			if( ++g_Application->m_argc == WinConsole::MAX_ARGVS ) return;

			while( !isspace(cmd[i]) ) if( ++i == str_length ) return;
			cmd[i] = '\0';
			if( ++i == str_length ) return;
		}

		while( isspace(cmd[i]) ) if( ++i == str_length ) return;
	}
}

//**************************************************************************************
// WinConsole methods
//*****
// Initialisation
WinConsole::WinConsole()
{
	m_main_window_handle	= 0;
	m_main_app_instance		= 0;
	m_application_active	= true;
	m_show_window			= TRUE;
	m_screen_width			= 640;
	m_screen_height			= 480;
	m_screen_x				= (GetSystemMetrics(SM_CXFULLSCREEN) - m_screen_width)  / 2;
	m_screen_y				= (GetSystemMetrics(SM_CYFULLSCREEN) - m_screen_height) / 2;
	m_window_title			= _T("Win32 Program");
	m_icon					= 0;
	m_icon_small			= 0;
	m_cursor				= 0;
	m_menu					= 0;
	m_window_style			= WS_POPUP|WS_CAPTION|WS_SYSMENU|WS_THICKFRAME|WS_MINIMIZEBOX|WS_VISIBLE;
	SetRect(&m_window_bounds, m_screen_x, m_screen_y, m_screen_x + m_screen_width, m_screen_y + m_screen_height);
	SetRect(&m_client_area,	  m_screen_x, m_screen_y, m_screen_x + m_screen_width, m_screen_y + m_screen_height);
}

//*****
// The default message handler
LRESULT CALLBACK WinConsole::WindowProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam)
{
	LRESULT r;
	switch( msg )
	{
    case WM_ACTIVATE:
		m_application_active = LOWORD(wparam) != 0;
		if( (r = OnActivate()) != 0 ) return r;
		break;

	case WM_PAINT:
		if( (r = OnPaint()) != 0 ) return r;
		break;

	case WM_EXITSIZEMOVE:
		if( m_application_active )
		{
			// Update window properties
			GetWindowRect(m_main_window_handle, &m_window_bounds);
			GetClientRect(m_main_window_handle, &m_client_area);
		}
		if( (r = OnExitSizeMove()) != 0 ) return r;
		break;

	case WM_CLOSE:
		if( (r = OnClose()) != 0 ) return r;
		break;

	case WM_DESTROY: 
		if( (r = OnDestroy()) != 0 ) return r;
		break;
    }

	// Process any unhandled messages
	return DefWindowProc(hwnd, msg, wparam, lparam);
}

//*****
// Sleep until 'time_ms' milliseconds have passed
void WinConsole::Idle(DWORD time_ms)
{
	Sleep(time_ms);
}
