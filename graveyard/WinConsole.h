//*****************************************************************************//
//                                                                             //
//				A starting point for creating Win32 applications               //
//                                                                             //
//*****************************************************************************//
#ifndef WINCONSOLE_H
#define WINCONSOLE_H

// Defines
// ;NOMINMAX ;VC_EXTRALEAN

#include <windows.h>

class WinConsole;

//*****
// External globals. These parameters are extern'ed so that
// they can be used by the user program plus other object files
extern WinConsole*	g_Application;

// The user application must define this
void OnStartUp();

// WinConsole friend functions
int WINAPI WinMain(HINSTANCE hinstance, HINSTANCE, LPSTR command_line, int show);
bool CreateApplicationWindow();

//*****
// A base class to derive win32 apps from
class WinConsole
{
public:
	WinConsole();
	virtual ~WinConsole() {}
	
	// Overridables
	virtual bool OnStartup()	{ return true;	}	// Use to set global parameters
	virtual bool Initialise()	{ return true;	}	// Called once a window has been created
	virtual void Main()			{				}	// Called as often as possible
	virtual int  OnShutdown()	{ return 0;		}	// Called on exit
	virtual LRESULT CALLBACK WindowProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam);	// Handles messages
	virtual void Idle(DWORD time_ms);				// Calls Sleep() for 'time_ms' milliseconds

	// Event Overridables
	virtual LRESULT OnActivate()		{ return 0;	}	// Called when the window becomes active
	virtual LRESULT OnPaint()			{ return 0; }	// Called whenever a WM_PAINT message is recieved
	virtual LRESULT OnExitSizeMove()	{ return 0; }	// Called after a window has been moved/resized
	virtual LRESULT OnClose()			{ SendMessage(m_main_window_handle, WM_DESTROY, 0, 0); return 1; }
	virtual LRESULT OnDestroy()			{ PostQuitMessage(0); return 1; } // kill the application

protected:
	bool GetCommandLineParameter(int which, char*& parameter, int& length);	// Returns a parameter from the command line

protected:
	enum { MAX_ARGVS = 256 };
	friend int WINAPI WinMain(HINSTANCE hinstance, HINSTANCE, LPSTR command_line, int show);
	friend bool CreateApplicationWindow();

	HWND		m_main_window_handle;
	HINSTANCE	m_main_app_instance;
	TCHAR*		m_argv[MAX_ARGVS];			// C-runtime style arguments
	int			m_argc;						// C-runtime style arguments
	bool		m_application_active;		// True when the app is visible
	int			m_show_window;				// Window show mode
	DWORD		m_screen_width;				// Window size or screen resolution
	DWORD		m_screen_height;			// Window size or screen resolution
	int			m_screen_x;					// Window position
	int			m_screen_y;					// Window position
	RECT		m_window_bounds;			// Size of the window
	RECT		m_client_area;				// Size of the client area within the window
	const TCHAR* m_window_title;
	HICON		m_icon;
	HICON		m_icon_small;
	HCURSOR		m_cursor;
	HMENU		m_menu;
	DWORD		m_window_style;

private:
	friend void TokeniseCommandLine();
	TCHAR		m_command_line[MAX_PATH];	// A buffer for the tokenised command line
};

#endif//WINCONSOLE_H
