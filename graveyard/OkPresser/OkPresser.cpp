#include <stdio.h>
#include <conio.h>
#include <windows.h>
#include <windowsx.h>

const DWORD MAX_IDS = 10;
char* g_Parent_Substring = "";
bool  g_Use_Substring = false;
DWORD g_Parent_Process_ID = 0xFFFFFFFF;
bool  g_Use_Process_ID = false;
DWORD g_Sleeptime = 1000;
DWORD g_Num_IDs_To_Send = 0;
DWORD g_Id_To_Send[MAX_IDS];

BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM)
{
	#ifndef NDEBUG
	char window_name[MAX_PATH];
	GetWindowText(hwnd, window_name, MAX_PATH);
	#endif//NDEBUG

	HWND parent = hwnd;

	// Get the topmost parent
	while( GetParent(parent) )
		parent = GetParent(parent);
	
	if( g_Use_Substring )
	{
		// Get the name of the parent
		char name[MAX_PATH];
		GetWindowText(parent, name, MAX_PATH);
		strupr(name);
	
		// Look for the sub string in the parent's title
		if( g_Parent_Substring[0] == '\0' || strstr(name, g_Parent_Substring) )
		{
			// Post each of the IDs
			for( DWORD i = 0; i < g_Num_IDs_To_Send; ++i )
			{
				PostMessage(hwnd, WM_COMMAND, HIWORD(BN_CLICKED) | LOWORD(g_Id_To_Send[i]), NULL);
			}
		}
	}
	if( g_Use_Process_ID )
	{
		// Get the process ID
		DWORD process_id;
		GetWindowThreadProcessId(parent, &process_id);

		// If this id matches the id we want...
		if( process_id == g_Parent_Process_ID )
		{
			// Post each of the IDs
			for( DWORD i = 0; i < g_Num_IDs_To_Send; ++i )
			{
				PostMessage(hwnd, WM_COMMAND, HIWORD(BN_CLICKED) | LOWORD(g_Id_To_Send[i]), NULL);
			}
		}
	}
	return TRUE;
}

//*****
// Usage: OkPresser "Parent Window Sub String" SleepTime(1000ms) ID_to_send(IDOK, IDCANCEL, IDIGNORE)
int main(int argc, char* argv[])
{
	for( int arg = 1; arg < argc; ++arg )
	{
		strupr(argv[arg]);
		if( strcmp(argv[arg], "-S") == 0 )
		{
			++arg;
			strupr(argv[arg]);
			g_Parent_Substring	= argv[arg];
			g_Use_Substring = true;
		}
		else if( strcmp(argv[arg], "-P") == 0 )
		{
			++arg;
			g_Parent_Process_ID = strtoul(argv[arg], NULL, 10);
			g_Use_Process_ID = true;
		}
		else if( strcmp(argv[arg], "-T") == 0 )
		{
			++arg;
			g_Sleeptime = strtoul(argv[arg], NULL, 10);
		}
		else if( strcmp(argv[arg], "-I") == 0 )
		{
			++arg;
			g_Num_IDs_To_Send = argc - arg;
			if( g_Num_IDs_To_Send > MAX_IDS )
				g_Num_IDs_To_Send = MAX_IDS;

			for( DWORD i = 0; i < g_Num_IDs_To_Send; ++i )
			{
				g_Id_To_Send[i]	= strtoul(argv[i + arg], NULL, 10);
			}
			break;
		}
		else
		{
			argc = 1;
			break;
		}
	}
	if( argc == 1 )
	{
		printf("=================\n");
		printf("=== OkPresser ===\n");
		printf("=================\n");
		printf("\n");
		printf(" This program looks for a window whose topmost parent contains a\n");
		printf(" substring or matches a process ID. It then sends the provided IDs\n");
		printf(" to the found window.\n");
		printf("\n");
		printf("Usage:\n");
		printf("   OkPresser -S \"substring\" -P process_id -T SleepTime -I ID1 [ID2, ID3, ...]\n");
		printf("\n");
		printf("   -S Search for a substring in the parent window title bar\n");
		printf("   -P Search for a parent with a matching process id\n");
		printf("   -T Specify a sleep time between looking for ok's to press (default 1 sec)\n");
		printf("   -I IDs to send to matching dialog boxes (must be the last option)\n");
		printf("Note:\n");
		printf("   SleepTime is in milliseconds\n");
		printf("   ID is one of:\n");
		printf("      OK      - 1\n");
		printf("      CANCEL  - 2\n");
		printf("      ABORT   - 3\n");
		printf("      RETRY   - 4\n");
		printf("      IGNORE  - 5\n");
		printf("      YES     - 6\n");
		printf("      NO      - 7\n");
		printf("      CLOSE   - 8\n");
		printf("      HELP    - 9\n");
		printf("   Max number of IDs that can be sent is %d\n", MAX_IDS);
		printf("   IDs are sent in the order provided\n");
		printf("   To Kill this program from a script/batchfile create a file in the same directory\n");
		printf("   as OkPresser.exe is running called OkPresserTerminateFile. OkPresser.exe will delete\n");
		printf("   this file before it exits.\n");
		return 0;
	}

	FILE* fp = fopen("OkPresserTerminateFile", "r");
	while( !fp )
	{
		EnumWindows(EnumWindowsProc, 0);
		Sleep(g_Sleeptime);
		fp = fopen("OkPresserTerminateFile", "r");
	}
	fclose(fp);
	DeleteFile("OkPresserTerminateFile");
	return 0;
}