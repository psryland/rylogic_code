//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************
#pragma once
#ifndef LDR_LUA_SOURCE_H
#define LDR_LUA_SOURCE_H

#include "linedrawer/types/forward.h"
#include "pr/lua/lua.h"
#include "pr/filesys/filewatch.h"
#include "pr/script/embedded_code.h"

// This class processes lua code
class LuaSource :public pr::script::IEmbeddedCode
{
	pr::lua::Lua m_lua;
	bool IEmbeddedCode_Execute(char const* code_id, pr::script::string const& code, pr::script::Loc const& loc, pr::script::string& result);

public:
	LuaSource();

	// Add a lua source file
	void Add(char const* filepath);

	// Return a string containing demo ldr lua script
	std::string CreateDemoLuaSource() const;
};

#endif
