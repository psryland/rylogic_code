//*****************************************
//*****************************************
#include "test.h"

#include <windows.h>
#include <stdio.h>
#include <fcntl.h>
#include <io.h>
#include <iostream>
#include <fstream>
#include <conio.h>
#include <crtdbg.h>

#ifndef _USE_OLD_OSTREAMS
using namespace std;
#endif

namespace TestConsole
{
	// maximum mumber of lines the output console should have
	static const WORD MAX_CONSOLE_LINES = 500;

	void RedirectIOToConsole()
	{
		int hConHandle;
		long lStdHandle;

		CONSOLE_SCREEN_BUFFER_INFO coninfo;

		FILE *fp;

		// allocate a console for this app
		AllocConsole();

		// set the screen buffer to be big enough to let us scroll text
		GetConsoleScreenBufferInfo(GetStdHandle(STD_OUTPUT_HANDLE), &coninfo);
		coninfo.dwSize.Y = MAX_CONSOLE_LINES;
		SetConsoleScreenBufferSize(GetStdHandle(STD_OUTPUT_HANDLE), coninfo.dwSize);

		// redirect unbuffered STDOUT to the console
		lStdHandle = (long)GetStdHandle(STD_OUTPUT_HANDLE);
		hConHandle = _open_osfhandle(lStdHandle, _O_TEXT);
		fp = _fdopen( hConHandle, "w" );
		*stdout = *fp;
		setvbuf( stdout, NULL, _IONBF, 0 );

		// redirect unbuffered STDIN to the console
		lStdHandle = (long)GetStdHandle(STD_INPUT_HANDLE);
		hConHandle = _open_osfhandle(lStdHandle, _O_TEXT);
		fp = _fdopen( hConHandle, "r" );
		*stdin = *fp;
		setvbuf( stdin, NULL, _IONBF, 0 );

		// redirect unbuffered STDERR to the console
		lStdHandle = (long)GetStdHandle(STD_ERROR_HANDLE);
		hConHandle = _open_osfhandle(lStdHandle, _O_TEXT);
		fp = _fdopen( hConHandle, "w" );
		*stderr = *fp;
		setvbuf( stderr, NULL, _IONBF, 0 );

		// make cout, wcout, cin, wcin, wcerr, cerr, wclog and clog

		// point to console as well
		ios::sync_with_stdio();
	}	

	void ConsoleOutputTest()
	{
		int iVar;
	
		// test stdio
		fprintf(stdout, "Test output to stdout\n");
		fprintf(stderr, "Test output to stderr\n");
		fprintf(stdout, "Enter an integer to test stdin: ");
		scanf("%d", &iVar);
		printf("You entered %d\n", iVar);
	
		//test iostreams
		cout << "Test output to cout" << endl;
		cerr << "Test output to cerr" << endl;
		clog << "Test output to clog" << endl;
		cout << "Enter an integer to test cin: ";
		cin >> iVar;
		cout << "You entered " << iVar << endl;
	
	#ifndef _USE_OLD_IOSTREAMS
		// test wide iostreams
		wcout << L"Test output to wcout" << endl;
		wcerr << L"Test output to wcerr" << endl;
		wclog << L"Test output to wclog" << endl;
		wcout << L"Enter an integer to test wcin: ";
		wcin >> iVar;
		wcout << L"You entered " << iVar << endl;
	#endif
	
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
		Sleep(2000);
	}

	void Run()
	{
		//RedirectIOToConsole();
		ConsoleOutputTest();
		_getch();
	}
}
