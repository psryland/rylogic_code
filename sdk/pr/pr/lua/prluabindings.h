//*****************************************
// PR Lua - Lua bindings for pr lib
//  Copyright © Rylogic Ltd 2007
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

#ifndef PR_LUA_BINDINGS_H
#define PR_LUA_BINDINGS_H

extern "C"
{
	#include "lua.h"
	#include "lualib.h"
	#include "lauxlib.h"

	// If this file is being built as part of the PRLua project
	#ifdef PRLUA_EXPORTS
	#	define PRLUA_API __declspec(dllexport)
	#else
	#	define PRLUA_API __declspec(dllimport)
	#endif

	#ifdef NDEBUG
	#	define PRLuaBindingFunction luaopen_PRLua
	#else
	#	define PRLuaBindingFunction luaopen_PRLuaD
	#endif

	// Dynamic binding function
	PRLUA_API int PRLuaBindingFunction(lua_State* lua_state);
	
	// PR library functions
	PRLUA_API int lua_pr_Template(lua_State* lua_state);
}

#endif//PR_LUA_BINDINGS_H

