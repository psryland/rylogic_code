//**********************************
// Script charactor source
//  Copyright (c) Rylogic Ltd 2007
//**********************************
#ifndef PR_SCRIPT_EMBEDDED_LUA_H
#define PR_SCRIPT_EMBEDDED_LUA_H

#include "pr/script/embedded_code.h"
#include "pr/lua/lua.h"
// Requires lua.lib

namespace pr
{
	namespace script
	{
		// Interface for parsing lua code and converting it to ldr script
		struct EmbeddedLua :IEmbeddedCode
		{
			pr::lua::Lua m_lua;
			
			EmbeddedLua() {}
			bool IEmbeddedCode_Execute(char const* code_id, pr::script::string const& code, pr::script::Loc const& loc, pr::script::string& result)
			{
				// We only handle lua code
				if (!pr::str::Equal(code_id, "lua"))
					return false;
				
				// Record the number of items on the stack
				int base = lua_gettop(m_lua);
			
				// Convert the lua code to a compiled chunk
				string error_msg;
				if (pr::lua::PushLuaChunk(m_lua, code, error_msg) != pr::lua::EResult::Success)
					throw Exception(EResult::EmbeddedCodeSyntaxError, loc, error_msg.c_str());
				
				// Execute the chunk
				if (!pr::lua::CallLuaChunk(m_lua, 0, false))
					return false;
				
				// If there's something still on the stack, copy it to result
				if (lua_gettop(m_lua) != base && !lua_isnil(m_lua, -1))
				{
					result = lua_tostring(m_lua, -1);
					lua_pop(m_lua, 1);
				}
				
				// Ensure the stack does not grow
				if (lua_gettop(m_lua) != base)
				{
					PR_ASSERT(PR_DBG, false, "lua stack height not constant");
					lua_settop(m_lua, base);
				}
				
				return true;
			}
		};
	}
}

#endif
