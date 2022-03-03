//*****************************************
// PR Lua - Lua bindings for pr lib
//  Copyright (C) Rylogic Ltd 2007
//*****************************************
// This is a dll that can be used in a lua script file
// Usage:
//	#file.lua
//	--Add the search path for the PRLua.dll to 'cpath'
//	package.cpath = package.cpath..
//					";Q:/SDK/pr/Lib/?.dll"..
//					""
//	-- Load the dll
//	require "PRLua"
//	
//	pr.Template() -- Use the dll function
//
// To add a new binding search for "Template"
// (case, !whole word) and add code appropriately
#pragma once

extern "C"
{
	#include "lua/include/lua.h"
	#include "lua/include/lualib.h"
	#include "lua/include/lauxlib.h"

	// If this file is being built as part of the PRLua project
	#ifdef PRLUA_EXPORTS
	#	define PRLUA_API __declspec(dllexport)
	#else
	#	define PRLUA_API __declspec(dllimport)
	#endif

	// Dynamic binding function
	PRLUA_API int luaopen_PRLua(lua_State* lua_state);
	
	// PR library functions
	PRLUA_API int lua_pr_Template(lua_State* lua_state);
}
