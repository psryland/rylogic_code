//***************************************
// Pauls Lua Interpreter
//  Copyright © Rylogic Ltd 2007
//***************************************

#include <iostream>
#include "pr/str/prstring.h"
#include "pr/common/fmt.h"
#include "pr/common/command_line.h"
#include "pr/lua/lua.h"

using namespace pr;
using namespace pr::cmdline;

class Main : public IOptionReceiver
{
public:
	Main();
	int  Run(int argc, char* argv[]);
	bool CmdLineOption(std::string const& option, TArgIter& arg , TArgIter arg_end);
	bool CmdLineData  (                           TArgIter& data, TArgIter data_end);
	void ShowHelp();
	static std::string ReadConsole();

private:
	pr::lua::Lua m_lua;
	std::string  m_file;
};

Main::Main()
{}

// Read text from the console
std::string Main::ReadConsole()
{
	std::string input; input.reserve(1024);
	int ch;
	do
	{
		ch = getc(stdin);
		input.push_back(static_cast<char>(ch));
	}
	while (ch != '\n');
	return input;
}

// Main program run
int Main::Run(int argc, char* argv[])
{
	if( !EnumCommandLine(argc, argv, *this) )	{ ShowHelp(); return -1; }

	if (!m_file.empty())
	{
		lua::DoFile(m_lua, m_file.c_str());
	}
	else
	{
		std::string welcome = Fmt(	"Rylogic Lua Interpreter\n"
									"%s\n"
									,lua::VersionString()
									);
		lua::Call(m_lua, "print", "s", welcome.c_str());

		// Console behaviour
		for(;;)
		{
			lua::EResult::Type result = lua::EResult::Success;
			std::string input, err_msg;
			do
			{
				if (result != lua::EResult::Incomplete) lua::Call(m_lua, "print", "s", ">");
				else                                    lua::Call(m_lua, "print", "s", "-");
				input += ReadConsole();
				result = lua::StepConsole(m_lua, input, err_msg);
				if (result == lua::EResult::SyntaxError) printf("%s", err_msg.c_str());
			}
			while (result == lua::EResult::Incomplete);
			if      (result == lua::EResult::Exit   ) { break; }
			else if (result != lua::EResult::Success) { lua::LuaPrint(m_lua); } // Print the error message
		}
	}
	return 0;
}

// Usage message
void Main::ShowHelp()
{
	printf(	"\n"
			"****************************************\n"
			" --- Lua Interpreter - Rylogic 2007 --- \n"
			"****************************************\n"
			"%s\n"
			"\n"
			"  Syntax: LuaInterpreter [filename.lua]\n"
			"    filename : A lua file to execute\n"
			,lua::VersionString()
			);
}

//*****
bool Main::CmdLineOption(std::string const& option, TArgIter&, TArgIter)
{
	printf("Error: Unknown option '%s'\n", option.c_str());
	ShowHelp();
	return false;
}
bool Main::CmdLineData(TArgIter& data, TArgIter)
{
	m_file = *data++;
	return true;
}

int main(int argc, char* argv[])
{
	Main m;
	return m.Run(argc, argv);
}


