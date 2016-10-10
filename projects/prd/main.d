// An entry point for testing this library

import core.runtime;
import core.sys.windows.windows;
import std.string;
import prd.gui.wingui;

pragma(lib, "gdi32.lib");
pragma(lib, "comdlg32.lib");
pragma(lib, "winmm.lib");

// Windows entry point
extern (Windows) int WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
	int result;
	try
	{
		Runtime.initialize();
		result = DWinMain(hInstance, hPrevInstance, lpCmdLine, nCmdShow);
		Runtime.terminate();
	}
	catch (Throwable e) 
	{
		MessageBoxA(null, e.toString().toStringz(), null, MB_ICONEXCLAMATION);
		result = 0;
	}
	return result;
}

// D program win main
int DWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
	scope Form form;
	return 0;
}