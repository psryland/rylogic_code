//*****************************************
// PR Lua - Lua bindings for pr lib
//  Copyright (C) Rylogic Ltd 2007
//*****************************************
// To add a new binding search for "Template"
// (case, !whole word) and add code appropriately
#include "pr/lua/lua.h"
#include "pr/lua/lua_bindings.h"

using namespace pr;

extern "C"
{
	// Dynamic binding function
	PRLUA_API int luaopen_PRLua(lua_State* lua_state)
	{
		pr::lua::Lua lua(lua_state);
		lua::Register(lua_state, "pr.Template", lua_pr_Template);
		return 0;
	}

	// PR library functions
	PRLUA_API int lua_pr_Template(lua_State*)
	{
		printf("Template\b\n");
		return 0;
	}

}

