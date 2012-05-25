module winmain;

import core.runtime;
import std.c.windows.windows;
import dgui.all;
import main_gui;

// Entry point
extern(Windows) int WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
	void ExceptionHandler(Throwable e) { throw e; }
	try
	{
		Runtime.initialize(&ExceptionHandler);
		int result = DWinMain(hInstance, hPrevInstance, lpCmdLine, nCmdShow);
		Runtime.terminate(&ExceptionHandler);
		return result;
	}
	catch (Throwable o) // catch any uncaught exceptions
	{
		//MessageBoxA(null, cast(char*)o.toString(), "Error", MB_OK | MB_ICONEXCLAMATION);
		return 0; // failed
	}
}

int DWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
	Application.run(new MainForm()); // Start the application
	return 0;
}
