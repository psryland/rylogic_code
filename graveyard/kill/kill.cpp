#include <stdio.h>
#include <conio.h>
#include <windows.h>
#include <windowsx.h>

const DWORD MAX_IDS = 10;
char* g_Window_Name = "";
bool  g_Use_Name = false;
DWORD g_Process_ID = 0xFFFFFFFF;
bool  g_Use_Process_ID = false;

BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM)
{
	#ifdef _DEBUG
	char window_name[MAX_PATH];
	GetWindowText(hwnd, window_name, MAX_PATH);
	#endif//_DEBUG

	if( g_Use_Name )
	{
		// Get the name of the parent
		char name[MAX_PATH];
		GetWindowText(hwnd, name, MAX_PATH);
		_strupr(name);
	
		// Look for the sub string in the parent's title
		if( strcmp(name, g_Window_Name) == 0 )
		{
			// Kill
			PostMessage(hwnd, WM_DESTROY, 0, NULL);
		}
	}
	if( g_Use_Process_ID )
	{
		// Get the process ID
		DWORD process_id;
		GetWindowThreadProcessId(hwnd, &process_id);

		// If this id matches the id we want...
		if( process_id == g_Process_ID )
		{
			// Kill
			PostMessage(hwnd, WM_DESTROY, 0, NULL);
		}
	}
	return TRUE;
}

//*****
// Usage: Kill -P 1234 -S "Window Name"
int main(int argc, char* argv[])
{
	for( int arg = 1; arg < argc; ++arg )
	{
		_strupr(argv[arg]);
		if( strcmp(argv[arg], "-S") == 0 )
		{
			++arg;
			_strupr(argv[arg]);
			g_Window_Name = argv[arg];
			g_Use_Name = true;
		}
		else if( strcmp(argv[arg], "-P") == 0 )
		{
			++arg;
			g_Process_ID = strtoul(argv[arg], NULL, 10);
			g_Use_Process_ID = true;
		}
		else
		{
			argc = 1;
			break;
		}
	}
	if( argc == 1 )
	{
		printf("============\n");
		printf("=== Kill ===\n");
		printf("============\n");
		printf("\n");
		printf(" This program looks for a window based on name or process id and then kills it\n");
		printf("\n");
		printf("Usage:\n");
		printf("   Kill -S \"Window Name\" -P process_id\n");
		printf("\n");
		return 0;
	}

	EnumWindows(EnumWindowsProc, 0);
	return 0;
}