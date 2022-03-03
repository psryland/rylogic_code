//*****************************************
//*****************************************
#include "test.h"
#include "pr/str/prstring.h"
#include "pr/lua/lua.h"

namespace TestLua
{
	using namespace pr;
		
	// Read some text from the console
	std::string ReadConsole()
	{
		char string[512];
		fgets(string, 512, stdin);
		return string;
	}

	int TestLuaFunc(lua_State*)
	{
		printf("Called TestLuaFunc()\n");
		return 0;
	}

	void Run()
	{
		pr::lua::Lua lua;

		// Register some C functions
		lua::Register(lua, "TestLua", TestLuaFunc);
		lua::Register(lua, "pr.TestLua", TestLuaFunc);
		lua::Register(lua, "pr.bob.TestLua", TestLuaFunc);

		// Call a lua function
		lua::Call(lua, "print", "ss", lua::VersionString(), "\n");
		lua::Call(lua, "pr.TestLua", ">d");

		// Console behaviour
		for(;;)
		{
			lua::EResult::Type result = lua::EResult::Success;
			std::string  input, err_msg;
			do
			{
				if (result != lua::EResult::Incomplete) lua::Call(lua, "print", "s", ">");
				else                                    lua::Call(lua, "print", "s", "-");
				input += ReadConsole();
				result = lua::StepConsole(lua, input, err_msg);
				if (result == pr::lua::EResult::SyntaxError) printf("%s", err_msg.c_str());
			}
			while (result == lua::EResult::Incomplete);
			if      (result == lua::EResult::Exit   ) { break; }
			else if (result != lua::EResult::Success) { lua::LuaPrint(lua); } // Print the error message
		}
		printf("Done.\n");
		_getch();
	}
}//namespace TestLua
