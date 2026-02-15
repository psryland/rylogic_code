//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"

namespace cex
{
	// To add a new command:
	//  1. Add a line to the HP_CMDS macro
	//  2. Add a *.cpp file and implement the command entry point function.
	//      e.g. int MyCommand(CmdLine const& args) {}
	//  3. Command functions should return 0 on success.

	#define CEX_CMD(x)\
	x("automate", "Execute a script of mouse/keyboard commands", Automate)\
	x("clip", "Add text to the windows clipboard", Clip)\
	x("dirpath", "Open a dialog window for finding a path", DirPath)\
	x("exec", "Exec: execute another process", Exec)\
	x("find_element", "Find a UI element by name", FindElement)\
	x("guid", "Generate a new GUID", Guid)\
	x("hash", "Hash the given stdin data", Hash)\
	x("hdata", "Convert a source file into a C/C++ compatible header file", HData)\
	x("list_windows", "List all windows of a process", ListWindows)\
	x("lwr", "Convert a string to lower case", Lower)\
	x("msgbox", "Display a message box", MsgBox)\
	x("newlines", "Add or remove new lines from a text file", NewLines)\
	x("read_text", "Read text from a window using UI Automation", ReadText)\
	x("screenshot", "Capture visible windows of a process to PNG", Screenshot)\
	x("send_keys", "Send key presses to a window", SendKeys)\
	x("send_mouse", "Send mouse events to a window", SendMouse)\
	x("shutdown_process", "Gracefully shut down a process", ShutdownProcess)\
	x("wait_window", "Wait for a window to appear", WaitWindow)\
	x("wait", "Wait for a specified length of time", Wait)

	// Forward declare command functions
	#define CEX_CMD_FUNCTION(option, description, func) int func(pr::CmdLine const& args);
	CEX_CMD(CEX_CMD_FUNCTION);
	#undef CEX_CMD_FUNCTION
}