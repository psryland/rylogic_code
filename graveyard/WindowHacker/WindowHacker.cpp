#include <stdio.h>
#include <windows.h>
#include "Common/StdVector.h"

const int MAX_WINDOW_NAME_LENGTH = 1024;

enum ECommands
{
	eNoCommand,
	eDumpWindows,
	eSetWindowPosition,
	eWindowsMessage,
	eNumCommands
};

struct Window
{
	Window() : m_hwnd(0) { ZeroMemory(m_name, sizeof(m_name)); }
	HWND m_hwnd;
	char m_name[MAX_WINDOW_NAME_LENGTH + 1];
};

struct Params
{
	const char* m_window_mask;
	HWND		m_window;
};

// Globals
std::vector<Window> g_Windows;
size_t				g_Current;
Params				g_Params;

//*****
// Prototypes
BOOL CALLBACK			EnumWindowsProc(HWND hwnd, LPARAM);
DWORD					ConvertWindowsMessage(const char* name);
DWORD					ConvertWindowsKeyword(const char* name);
int						ShowHelp(ECommands command = eNoCommand);
ECommands				GetCommand(const char* command_string);
Window*					GetWindowHandle(const char* window_name);
Window*					GetFirstWindowHandle(const char* window_mask);
Window*					GetNextWindowHandle(const char* window_mask);
std::vector<Window*>	GetWindowHandles(const char* window_mask);
int						DumpWindows(int num_params, char* params[]);
int						SetWindowPosition(int num_params, char* params[]);
int						SendWindowsMessage(int num_params, char* params[]);

//*****
// Main
int main(int argc, char* argv[])
{
	if( argc < 2 ) return ShowHelp();
	g_Windows.reserve(100);
	
	// Get all of the windows
	EnumWindows(EnumWindowsProc, 0);

	// Process the command
	ECommands command = GetCommand(argv[1]);
	switch( command )
	{
	case eDumpWindows:			return DumpWindows(argc - 2, &argv[2]);
	case eSetWindowPosition:	return SetWindowPosition(argc - 2, &argv[2]);
	case eWindowsMessage:		return SendWindowsMessage(argc - 2, &argv[2]);
	case eNoCommand:			return ShowHelp();
	default:					return ShowHelp();
	}
}

//*****
// Send a windows message to a window
int SendWindowsMessage(int num_params, char* params[])
{
	if( num_params != 4 )	return ShowHelp(eWindowsMessage);
	Window* window = GetWindowHandle(params[0]);
	if( window )
	{
		DWORD	msg		= ConvertWindowsMessage(params[1]);
		WPARAM	wparam	= ConvertWindowsKeyword(params[2]);
		LPARAM	lparam	= ConvertWindowsKeyword(params[3]);
		PostMessage(window->m_hwnd, msg, wparam, lparam);
	}
	else
	{
		printf("Window not found. Use DUMP_WINDOWS to get the window name\n");
		return ShowHelp();
	}
	return 0;
}

//*****
// Set the position of a window
int SetWindowPosition(int num_params, char* params[])
{
	if( num_params != 3 ) return ShowHelp(eSetWindowPosition);
	Window* window = GetWindowHandle(params[0]);
	if( window )
	{
		int x = strtoul(params[1], NULL, 10);
		int y = strtoul(params[2], NULL, 10);
		SetWindowPos(window->m_hwnd,0, x, y, 1, 1, SWP_NOZORDER | SWP_NOSIZE);
	}
	else
	{
		printf("Window not found. Use DUMP_WINDOWS to get the window name\n");
		return ShowHelp();
	}
	return 0;
}

//*****
// Display all of the windows on the desktop
int DumpWindows(int num_params, char* params[])
{
	if( num_params > 1 ) return ShowHelp(eDumpWindows);

	std::vector<Window*> window = GetWindowHandles((num_params == 0)?(NULL):(params[0]));
	size_t num_windows = window.size();
	
	printf(" Window names containing \"%s\":\n");
	for( size_t i = 0; i < num_windows; ++i )
	{
		printf("   \"%s\"\n", window[i]->m_name);
	}
	return 0;
}

//*****
// Return a window whose name is 'window_name'
Window* GetWindowHandle(const char* window_name)
{
	size_t num_windows = g_Windows.size();
	for( size_t i = 0; i < num_windows; ++i )
	{
		if( strcmp(g_Windows[i].m_name, window_name) == 0 )
			return &g_Windows[i];
	}
	return NULL;
}

//*****
// Returns a window whose name matches the mask
Window*	GetNextWindowHandle(const char* window_mask)
{
	size_t num_windows = g_Windows.size();
	for(++g_Current; g_Current < num_windows; ++g_Current )
	{
		if( strstr(g_Windows[g_Current].m_name, window_mask) != NULL )
			return &g_Windows[g_Current];
	}
	return NULL;
}
Window* GetFirstWindowHandle(const char* window_mask)
{
	g_Current = size_t(-1);
	return GetNextWindowHandle(window_mask);
}

//*****
// Returns all the windows whose name matches the mask
std::vector<Window*> GetWindowHandles(const char* window_mask)
{
	std::vector<Window*> window_list;
	Window* window = GetFirstWindowHandle(window_mask);
	while( window )
	{
		window_list.push_back(window);
		window = GetNextWindowHandle(window_mask);
	}
	return window_list;
}

//*****
// Convert a string into a command type
ECommands GetCommand(const char* command_string)
{
	if( stricmp(command_string, "DUMP_WINDOWS")			== 0 )	return eDumpWindows;
	if( stricmp(command_string, "SET_WINDOW_POSITION")	== 0 )	return eSetWindowPosition;
	if( stricmp(command_string, "WINDOWS_MESSAGE")		== 0 )	return eWindowsMessage;
	return eNoCommand;
}

//*****
// Show help
int ShowHelp(ECommands command)
{
	printf("Window Hacker:\n");
	switch( command )
	{
	case eDumpWindows:
		printf(" Dump Windows Syntax: WindowHacker DUMP_WINDOWS [window_mask]\n");
		break;

	case eSetWindowPosition:
		printf(" Set Window Position Syntax: WindowHacker SET_WINDOW_POSITION window_name x y\n");
		break;

	case eWindowsMessage:
		printf(" Windows Message Syntax: WindowHacker WINDOWS_MESSAGE wm_message wparam lparam\n");
		printf("     wm_message - is a windows message define e.g. WM_PAINT\n");
		printf("     wparam - is a windows keyword e.g. SIZE_MAXIZISED or a literal hex number\n");
		printf("     lparam - is a windows keyword e.g. SIZE_MAXIZISED or a literal hex number\n");
		break;

	case eNoCommand:
	default:
		printf("   Syntax: WindowHacker command [parameters]\n");
		printf("   Commands:\n");
		printf("       DUMP_WINDOWS [window title mask]\n");
		printf("       SET_WINDOW_POSITION window_handle x y\n");
		printf("       WINDOWS_MESSAGE wm_message wparam lparam\n");
		printf("\n");
	};
	return -1;
}

//*****
// Get all of the windows on the desktop
BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM)
{
	Window window;
	window.m_hwnd = hwnd;
	GetWindowText(hwnd, window.m_name, MAX_WINDOW_NAME_LENGTH);

	if( window.m_hwnd && window.m_name[0] != '\0' )
	{
		g_Windows.push_back(window);
	}
	return TRUE;
}

//*****
// Convert a keyword into its value
DWORD ConvertWindowsMessage(const char* name)
{
	if( isxdigit(name[0]) )								return strtoul(name, NULL, 16);
	if( strcmp(name, "WM_SIZE")					== 0)	return WM_SIZE;
	if( strcmp(name, "WM_PAINT")				== 0)	return WM_PAINT;
	if( strcmp(name, "WM_SHOWWINDOW")			== 0)	return WM_SHOWWINDOW;

	printf("Unknown windows message: \"%s\" interpreting as a literal base 16 number...\n", name);
	return strtoul(name, NULL, 16);
}

//*****
// Convert a windows key_word
DWORD ConvertWindowsKeyword(const char* name)
{
	if( isxdigit(name[0]) )								return strtoul(name, NULL, 16);
	if( strcmp(name, "TRUE")					== 0)	return TRUE;
	if( strcmp(name, "FALSE")					== 0)	return FALSE;
	if( strcmp(name, "SIZE_MAXIMIZED")			== 0)	return SIZE_MAXIMIZED;
	if( strcmp(name, "SIZE_RESTORED")			== 0)	return SIZE_RESTORED;
	if( strcmp(name, "SIZE_MINIMIZED")			== 0)	return SIZE_MINIMIZED;
	if( strcmp(name, "SIZE_MAXIMIZED")			== 0)	return SIZE_MAXIMIZED;
	if( strcmp(name, "SIZE_MAXSHOW")			== 0)	return SIZE_MAXSHOW;
	if( strcmp(name, "SIZE_MAXHIDE")			== 0)	return SIZE_MAXHIDE;

	// Show window defines
	if( strcmp(name, "SW_HIDE")					== 0)	return SW_HIDE;
	if( strcmp(name, "SW_SHOWNORMAL")			== 0)	return SW_SHOWNORMAL;
	if( strcmp(name, "SW_NORMAL")				== 0)	return SW_NORMAL;
	if( strcmp(name, "SW_SHOWMINIMIZED")		== 0)	return SW_SHOWMINIMIZED;
	if( strcmp(name, "SW_SHOWMAXIMIZED")		== 0)	return SW_SHOWMAXIMIZED;
	if( strcmp(name, "SW_MAXIMIZE")				== 0)	return SW_MAXIMIZE;
	if( strcmp(name, "SW_SHOWNOACTIVATE")		== 0)	return SW_SHOWNOACTIVATE;
	if( strcmp(name, "SW_SHOW")					== 0)	return SW_SHOW;
	if( strcmp(name, "SW_MINIMIZE")				== 0)	return SW_MINIMIZE;
	if( strcmp(name, "SW_SHOWMINNOACTIVE")		== 0)	return SW_SHOWMINNOACTIVE;
	if( strcmp(name, "SW_SHOWNA")				== 0)	return SW_SHOWNA;
	if( strcmp(name, "SW_RESTORE")				== 0)	return SW_RESTORE;
	if( strcmp(name, "SW_SHOWDEFAULT")			== 0)	return SW_SHOWDEFAULT;
	if( strcmp(name, "SW_FORCEMINIMIZE")		== 0)	return SW_FORCEMINIMIZE;
	if( strcmp(name, "SW_MAX")					== 0)	return SW_MAX;

	printf("Unknown windows keyword: \"%s\" interpreting as a literal base 16 number...\n", name);
	return strtoul(name, NULL, 16);
}

//PSR...	if( strcmp(name, "WM_NULL")					== 0) return WM_NULL;
//PSR...	if( strcmp(name, "WM_CREATE")				== 0) return WM_CREATE						
//PSR...	if( strcmp(name, "WM_DESTROY")				== 0) return WM_DESTROY						
//PSR...	if( strcmp(name, "WM_MOVE")					== 0) return WM_MOVE							
//PSR...	if( strcmp(name, "WM_ACTIVATE")				== 0) return WM_ACTIVATE						
//PSR...	if( strcmp(name, "WM_SETFOCUS")				== 0) return WM_SETFOCUS						
//PSR...	if( strcmp(name, "WM_KILLFOCUS")			== 0) return WM_KILLFOCUS					
//PSR...	if( strcmp(name, "WM_ENABLE")				== 0) return WM_ENABLE						
//PSR...	if( strcmp(name, "WM_SETREDRAW")			== 0) return WM_SETREDRAW					
//PSR...	if( strcmp(name, "WM_SETTEXT")				== 0) return WM_SETTEXT						
//PSR...	if( strcmp(name, "WM_GETTEXT")				== 0) return WM_GETTEXT						
//PSR...	if( strcmp(name, "WM_GETTEXTLENGTH")		== 0) return WM_GETTEXTLENGTH				
//PSR...	if( strcmp(name, "WM_CLOSE")				== 0) return WM_CLOSE						
//PSR...	if( strcmp(name, "WM_QUIT")					== 0) return WM_QUIT							
//PSR...	if( strcmp(name, "WM_ERASEBKGND")			== 0) return WM_ERASEBKGND					
//PSR...	if( strcmp(name, "WM_SYSCOLORCHANGE")		== 0) return WM_SYSCOLORCHANGE				
//PSR...	if( strcmp(name, "WM_WININICHANGE")			== 0) return WM_WININICHANGE					
//PSR...	if( strcmp(name, "WM_SETTINGCHANGE")		== 0) return WM_SETTINGCHANGE				
//PSR...	if( strcmp(name, "WM_DEVMODECHANGE")		== 0) return WM_DEVMODECHANGE				
//PSR...	if( strcmp(name, "WM_ACTIVATEAPP")			== 0) return WM_ACTIVATEAPP					
//PSR...	if( strcmp(name, "WM_FONTCHANGE")			== 0) return WM_FONTCHANGE					
//PSR...	if( strcmp(name, "WM_TIMECHANGE")			== 0) return WM_TIMECHANGE					
//PSR...	if( strcmp(name, "WM_CANCELMODE")			== 0) return WM_CANCELMODE					
//PSR...	if( strcmp(name, "WM_SETCURSOR")			== 0) return WM_SETCURSOR					
//PSR...	if( strcmp(name, "WM_MOUSEACTIVATE")		== 0) return WM_MOUSEACTIVATE				
//PSR...	if( strcmp(name, "WM_CHILDACTIVATE")		== 0) return WM_CHILDACTIVATE				
//PSR...	if( strcmp(name, "WM_QUEUESYNC")			== 0) return WM_QUEUESYNC					
//PSR...	if( strcmp(name, "WM_GETMINMAXINFO				WM_GETMINMAXINFO				
//PSR...	if( strcmp(name, "WM_PAINTICON					WM_PAINTICON					
//PSR...	if( strcmp(name, "WM_ICONERASEBKGND				WM_ICONERASEBKGND				
//PSR...	if( strcmp(name, "WM_NEXTDLGCTL					WM_NEXTDLGCTL					
//PSR...	if( strcmp(name, "WM_SPOOLERSTATUS				WM_SPOOLERSTATUS				
//PSR...	if( strcmp(name, "WM_DRAWITEM						WM_DRAWITEM						
//PSR...	if( strcmp(name, "WM_MEASUREITEM					WM_MEASUREITEM					
//PSR...	if( strcmp(name, "WM_DELETEITEM					WM_DELETEITEM					
//PSR...	if( strcmp(name, "WM_VKEYTOITEM					WM_VKEYTOITEM					
//PSR...	if( strcmp(name, "WM_CHARTOITEM					WM_CHARTOITEM					
//PSR...	if( strcmp(name, "WM_SETFONT						WM_SETFONT						
//PSR...	if( strcmp(name, "WM_GETFONT						WM_GETFONT						
//PSR...	if( strcmp(name, "WM_SETHOTKEY					WM_SETHOTKEY					
//PSR...	if( strcmp(name, "WM_GETHOTKEY					WM_GETHOTKEY					
//PSR...	if( strcmp(name, "WM_QUERYDRAGICON				WM_QUERYDRAGICON				
//PSR...	if( strcmp(name, "WM_COMPAREITEM					WM_COMPAREITEM					
//PSR...	if( strcmp(name, "WM_GETOBJECT					WM_GETOBJECT					
//PSR...	if( strcmp(name, "WM_COMPACTING					WM_COMPACTING					
//PSR...	if( strcmp(name, "WM_WINDOWPOSCHANGING			WM_WINDOWPOSCHANGING			
//PSR...	if( strcmp(name, "WM_WINDOWPOSCHANGED				WM_WINDOWPOSCHANGED				
//PSR...	if( strcmp(name, "WM_POWER						WM_POWER						
//PSR...	if( strcmp(name, "WM_COPYDATA						WM_COPYDATA						
//PSR...	if( strcmp(name, "WM_CANCELJOURNAL				WM_CANCELJOURNAL				
//PSR...	if( strcmp(name, "WM_NOTIFY						WM_NOTIFY						
//PSR...	if( strcmp(name, "WM_INPUTLANGCHANGEREQUEST		WM_INPUTLANGCHANGEREQUEST		
//PSR...	if( strcmp(name, "WM_INPUTLANGCHANGE				WM_INPUTLANGCHANGE				
//PSR...	if( strcmp(name, "WM_TCARD						WM_TCARD						
//PSR...	if( strcmp(name, "WM_HELP							WM_HELP							
//PSR...	if( strcmp(name, "WM_USERCHANGED					WM_USERCHANGED					
//PSR...	if( strcmp(name, "WM_NOTIFYFORMAT					WM_NOTIFYFORMAT					
//PSR...	if( strcmp(name, "WM_CONTEXTMENU					WM_CONTEXTMENU					
//PSR...	if( strcmp(name, "WM_STYLECHANGING				WM_STYLECHANGING				
//PSR...	if( strcmp(name, "WM_STYLECHANGED					WM_STYLECHANGED					
//PSR...	if( strcmp(name, "WM_DISPLAYCHANGE				WM_DISPLAYCHANGE				
//PSR...	if( strcmp(name, "WM_GETICON						WM_GETICON						
//PSR...	if( strcmp(name, "WM_SETICON						WM_SETICON						
//PSR...	if( strcmp(name, "WM_NCCREATE						WM_NCCREATE						
//PSR...	if( strcmp(name, "WM_NCDESTROY					WM_NCDESTROY					
//PSR...	if( strcmp(name, "WM_NCCALCSIZE					WM_NCCALCSIZE					
//PSR...	if( strcmp(name, "WM_NCHITTEST					WM_NCHITTEST					
//PSR...	if( strcmp(name, "WM_NCPAINT						WM_NCPAINT						
//PSR...	if( strcmp(name, "WM_NCACTIVATE					WM_NCACTIVATE					
//PSR...	if( strcmp(name, "WM_GETDLGCODE					WM_GETDLGCODE					
//PSR...	if( strcmp(name, "WM_SYNCPAINT					WM_SYNCPAINT					
//PSR...	if( strcmp(name, "WM_NCMOUSEMOVE					WM_NCMOUSEMOVE					
//PSR...	if( strcmp(name, "WM_NCLBUTTONDOWN				WM_NCLBUTTONDOWN				
//PSR...	if( strcmp(name, "WM_NCLBUTTONUP					WM_NCLBUTTONUP					
//PSR...	if( strcmp(name, "WM_NCLBUTTONDBLCLK				WM_NCLBUTTONDBLCLK				
//PSR...	if( strcmp(name, "WM_NCRBUTTONDOWN				WM_NCRBUTTONDOWN				
//PSR...	if( strcmp(name, "WM_NCRBUTTONUP					WM_NCRBUTTONUP					
//PSR...	if( strcmp(name, "WM_NCRBUTTONDBLCLK				WM_NCRBUTTONDBLCLK				
//PSR...	if( strcmp(name, "WM_NCMBUTTONDOWN				WM_NCMBUTTONDOWN				
//PSR...	if( strcmp(name, "WM_NCMBUTTONUP					WM_NCMBUTTONUP					
//PSR...	if( strcmp(name, "WM_NCMBUTTONDBLCLK				WM_NCMBUTTONDBLCLK				
//PSR...	if( strcmp(name, "WM_NCXBUTTONDOWN				WM_NCXBUTTONDOWN				
//PSR...	if( strcmp(name, "WM_NCXBUTTONUP					WM_NCXBUTTONUP					
//PSR...	if( strcmp(name, "WM_NCXBUTTONDBLCLK				WM_NCXBUTTONDBLCLK				
//PSR...	if( strcmp(name, "WM_INPUT						WM_INPUT						
//PSR...	if( strcmp(name, "WM_KEYFIRST						WM_KEYFIRST						
//PSR...	if( strcmp(name, "WM_KEYDOWN						WM_KEYDOWN						
//PSR...	if( strcmp(name, "WM_KEYUP						WM_KEYUP						
//PSR...	if( strcmp(name, "WM_CHAR							WM_CHAR							
//PSR...	if( strcmp(name, "WM_DEADCHAR						WM_DEADCHAR						
//PSR...	if( strcmp(name, "WM_SYSKEYDOWN					WM_SYSKEYDOWN					
//PSR...	if( strcmp(name, "WM_SYSKEYUP						WM_SYSKEYUP						
//PSR...	if( strcmp(name, "WM_SYSCHAR						WM_SYSCHAR						
//PSR...	if( strcmp(name, "WM_SYSDEADCHAR					WM_SYSDEADCHAR					
//PSR...	if( strcmp(name, "WM_UNICHAR						WM_UNICHAR						
//PSR...	if( strcmp(name, "WM_KEYLAST						WM_KEYLAST						
//PSR...	if( strcmp(name, "WM_IME_STARTCOMPOSITION			WM_IME_STARTCOMPOSITION			
//PSR...	if( strcmp(name, "WM_IME_ENDCOMPOSITION			WM_IME_ENDCOMPOSITION			
//PSR...	if( strcmp(name, "WM_IME_COMPOSITION				WM_IME_COMPOSITION				
//PSR...	if( strcmp(name, "WM_IME_KEYLAST					WM_IME_KEYLAST					
//PSR...	if( strcmp(name, "WM_INITDIALOG					WM_INITDIALOG					
//PSR...	if( strcmp(name, "WM_COMMAND						WM_COMMAND						
//PSR...	if( strcmp(name, "WM_SYSCOMMAND					WM_SYSCOMMAND					
//PSR...	if( strcmp(name, "WM_TIMER						WM_TIMER						
//PSR...	if( strcmp(name, "WM_HSCROLL						WM_HSCROLL						
//PSR...	if( strcmp(name, "WM_VSCROLL						WM_VSCROLL						
//PSR...	if( strcmp(name, "WM_INITMENU						WM_INITMENU						
//PSR...	if( strcmp(name, "WM_INITMENUPOPUP				WM_INITMENUPOPUP				
//PSR...	if( strcmp(name, "WM_MENUSELECT					WM_MENUSELECT					
//PSR...	if( strcmp(name, "WM_MENUCHAR						WM_MENUCHAR						
//PSR...	if( strcmp(name, "WM_ENTERIDLE					WM_ENTERIDLE					
//PSR...	if( strcmp(name, "WM_MENURBUTTONUP				WM_MENURBUTTONUP				
//PSR...	if( strcmp(name, "WM_MENUDRAG						WM_MENUDRAG						
//PSR...	if( strcmp(name, "WM_MENUGETOBJECT				WM_MENUGETOBJECT				
//PSR...	if( strcmp(name, "WM_UNINITMENUPOPUP				WM_UNINITMENUPOPUP				
//PSR...	if( strcmp(name, "WM_MENUCOMMAND					WM_MENUCOMMAND					
//PSR...	if( strcmp(name, "WM_CHANGEUISTATE				WM_CHANGEUISTATE				
//PSR...	if( strcmp(name, "WM_UPDATEUISTATE				WM_UPDATEUISTATE				
//PSR...	if( strcmp(name, "WM_QUERYUISTATE					WM_QUERYUISTATE					
//PSR...	if( strcmp(name, "WM_CTLCOLORMSGBOX				WM_CTLCOLORMSGBOX				
//PSR...	if( strcmp(name, "WM_CTLCOLOREDIT					WM_CTLCOLOREDIT					
//PSR...	if( strcmp(name, "WM_CTLCOLORLISTBOX				WM_CTLCOLORLISTBOX				
//PSR...	if( strcmp(name, "WM_CTLCOLORBTN					WM_CTLCOLORBTN					
//PSR...	if( strcmp(name, "WM_CTLCOLORDLG					WM_CTLCOLORDLG					
//PSR...	if( strcmp(name, "WM_CTLCOLORSCROLLBAR			WM_CTLCOLORSCROLLBAR			
//PSR...	if( strcmp(name, "WM_CTLCOLORSTATIC				WM_CTLCOLORSTATIC				
//PSR...	if( strcmp(name, "WM_MOUSEFIRST					WM_MOUSEFIRST					
//PSR...	if( strcmp(name, "WM_MOUSEMOVE					WM_MOUSEMOVE					
//PSR...	if( strcmp(name, "WM_LBUTTONDOWN					WM_LBUTTONDOWN					
//PSR...	if( strcmp(name, "WM_LBUTTONUP					WM_LBUTTONUP					
//PSR...	if( strcmp(name, "WM_LBUTTONDBLCLK				WM_LBUTTONDBLCLK				
//PSR...	if( strcmp(name, "WM_RBUTTONDOWN					WM_RBUTTONDOWN					
//PSR...	if( strcmp(name, "WM_RBUTTONUP					WM_RBUTTONUP					
//PSR...	if( strcmp(name, "WM_RBUTTONDBLCLK				WM_RBUTTONDBLCLK				
//PSR...	if( strcmp(name, "WM_MBUTTONDOWN					WM_MBUTTONDOWN					
//PSR...	if( strcmp(name, "WM_MBUTTONUP					WM_MBUTTONUP					
//PSR...	if( strcmp(name, "WM_MBUTTONDBLCLK				WM_MBUTTONDBLCLK				
//PSR...	if( strcmp(name, "WM_MOUSEWHEEL					WM_MOUSEWHEEL					
//PSR...	if( strcmp(name, "WM_XBUTTONDOWN					WM_XBUTTONDOWN					
//PSR...	if( strcmp(name, "WM_XBUTTONUP					WM_XBUTTONUP					
//PSR...	if( strcmp(name, "WM_XBUTTONDBLCLK				WM_XBUTTONDBLCLK				
//PSR...	if( strcmp(name, "WM_PARENTNOTIFY					WM_PARENTNOTIFY					
//PSR...	if( strcmp(name, "WM_ENTERMENULOOP				WM_ENTERMENULOOP				
//PSR...	if( strcmp(name, "WM_EXITMENULOOP					WM_EXITMENULOOP					
//PSR...	if( strcmp(name, "WM_NEXTMENU						WM_NEXTMENU						
//PSR...	if( strcmp(name, "WM_SIZING						WM_SIZING						
//PSR...	if( strcmp(name, "WM_CAPTURECHANGED				WM_CAPTURECHANGED				
//PSR...	if( strcmp(name, "WM_MOVING						WM_MOVING						
//PSR...	if( strcmp(name, "WM_POWERBROADCAST				WM_POWERBROADCAST				
//PSR...	if( strcmp(name, "WM_DEVICECHANGE					WM_DEVICECHANGE					
//PSR...	if( strcmp(name, "WM_MDICREATE					WM_MDICREATE					
//PSR...	if( strcmp(name, "WM_MDIDESTROY					WM_MDIDESTROY					
//PSR...	if( strcmp(name, "WM_MDIACTIVATE					WM_MDIACTIVATE					
//PSR...	if( strcmp(name, "WM_MDIRESTORE					WM_MDIRESTORE					
//PSR...	if( strcmp(name, "WM_MDINEXT						WM_MDINEXT						
//PSR...	if( strcmp(name, "WM_MDIMAXIMIZE					WM_MDIMAXIMIZE					
//PSR...	if( strcmp(name, "WM_MDITILE						WM_MDITILE						
//PSR...	if( strcmp(name, "WM_MDICASCADE					WM_MDICASCADE					
//PSR...	if( strcmp(name, "WM_MDIICONARRANGE				WM_MDIICONARRANGE				
//PSR...	if( strcmp(name, "WM_MDIGETACTIVE					WM_MDIGETACTIVE					
//PSR...	if( strcmp(name, "WM_MDISETMENU					WM_MDISETMENU					
//PSR...	if( strcmp(name, "WM_ENTERSIZEMOVE				WM_ENTERSIZEMOVE				
//PSR...	if( strcmp(name, "WM_EXITSIZEMOVE					WM_EXITSIZEMOVE					
//PSR...	if( strcmp(name, "WM_DROPFILES					WM_DROPFILES					
//PSR...	if( strcmp(name, "WM_MDIREFRESHMENU				WM_MDIREFRESHMENU				
//PSR...	if( strcmp(name, "WM_IME_SETCONTEXT				WM_IME_SETCONTEXT				
//PSR...	if( strcmp(name, "WM_IME_NOTIFY					WM_IME_NOTIFY					
//PSR...	if( strcmp(name, "WM_IME_CONTROL					WM_IME_CONTROL					
//PSR...	if( strcmp(name, "WM_IME_COMPOSITIONFULL			WM_IME_COMPOSITIONFULL			
//PSR...	if( strcmp(name, "WM_IME_SELECT					WM_IME_SELECT					
//PSR...	if( strcmp(name, "WM_IME_CHAR						WM_IME_CHAR						
//PSR...	if( strcmp(name, "WM_IME_REQUEST					WM_IME_REQUEST					
//PSR...	if( strcmp(name, "WM_IME_KEYDOWN					WM_IME_KEYDOWN					
//PSR...	if( strcmp(name, "WM_IME_KEYUP					WM_IME_KEYUP					
//PSR...	if( strcmp(name, "WM_MOUSEHOVER					WM_MOUSEHOVER					
//PSR...	if( strcmp(name, "WM_MOUSELEAVE					WM_MOUSELEAVE					
//PSR...	if( strcmp(name, "WM_NCMOUSEHOVER					WM_NCMOUSEHOVER					
//PSR...	if( strcmp(name, "WM_NCMOUSELEAVE					WM_NCMOUSELEAVE					
//PSR...	if( strcmp(name, "WM_WTSSESSION_CHANGE			WM_WTSSESSION_CHANGE			
//PSR...	if( strcmp(name, "WM_TABLET_FIRST					WM_TABLET_FIRST					
//PSR...	if( strcmp(name, "WM_TABLET_LAST					WM_TABLET_LAST					
//PSR...	if( strcmp(name, "WM_CUT							WM_CUT							
//PSR...	if( strcmp(name, "WM_COPY							WM_COPY							
//PSR...	if( strcmp(name, "WM_PASTE						WM_PASTE						
//PSR...	if( strcmp(name, "WM_CLEAR						WM_CLEAR						
//PSR...	if( strcmp(name, "WM_UNDO							WM_UNDO							
//PSR...	if( strcmp(name, "WM_RENDERFORMAT					WM_RENDERFORMAT					
//PSR...	if( strcmp(name, "WM_RENDERALLFORMATS				WM_RENDERALLFORMATS				
//PSR...	if( strcmp(name, "WM_DESTROYCLIPBOARD				WM_DESTROYCLIPBOARD				
//PSR...	if( strcmp(name, "WM_DRAWCLIPBOARD				WM_DRAWCLIPBOARD				
//PSR...	if( strcmp(name, "WM_PAINTCLIPBOARD				WM_PAINTCLIPBOARD				
//PSR...	if( strcmp(name, "WM_VSCROLLCLIPBOARD				WM_VSCROLLCLIPBOARD				
//PSR...	if( strcmp(name, "WM_SIZECLIPBOARD				WM_SIZECLIPBOARD				
//PSR...	if( strcmp(name, "WM_ASKCBFORMATNAME				WM_ASKCBFORMATNAME				
//PSR...	if( strcmp(name, "WM_CHANGECBCHAIN				WM_CHANGECBCHAIN				
//PSR...	if( strcmp(name, "WM_HSCROLLCLIPBOARD				WM_HSCROLLCLIPBOARD				
//PSR...	if( strcmp(name, "WM_QUERYNEWPALETTE				WM_QUERYNEWPALETTE				
//PSR...	if( strcmp(name, "WM_PALETTEISCHANGING			WM_PALETTEISCHANGING			
//PSR...	if( strcmp(name, "WM_PALETTECHANGED				WM_PALETTECHANGED				
//PSR...	if( strcmp(name, "WM_HOTKEY						WM_HOTKEY						
//PSR...	if( strcmp(name, "WM_PRINT						WM_PRINT						
//PSR...	if( strcmp(name, "WM_PRINTCLIENT					WM_PRINTCLIENT					
//PSR...	if( strcmp(name, "WM_APPCOMMAND					WM_APPCOMMAND					
//PSR...	if( strcmp(name, "WM_THEMECHANGED					WM_THEMECHANGED					
//PSR...	if( strcmp(name, "WM_HANDHELDFIRST				WM_HANDHELDFIRST				
//PSR...	if( strcmp(name, "WM_HANDHELDLAST					WM_HANDHELDLAST					
//PSR...	if( strcmp(name, "WM_AFXFIRST						WM_AFXFIRST						
//PSR...	if( strcmp(name, "WM_AFXLAST						WM_AFXLAST						
//PSR...	if( strcmp(name, "WM_PENWINFIRST					WM_PENWINFIRST					
//PSR...	if( strcmp(name, "WM_PENWINLAST					WM_PENWINLAST					
//PSR...	if( strcmp(name, "WM_APP							WM_APP							
