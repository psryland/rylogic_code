//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/embedded.h"
#include "pr/lua/lua.h"
// Requires lua.lib

namespace pr::script
{
	// An embedded code handler that supports lua code
	struct EmbeddedLua :IEmbeddedCode
	{
		pr::lua::Lua m_lua;

		// The language code that this handler is for
		wchar_t const* Lang() const override
		{
			return L"Lua";
		}

		// A handler function for executing embedded code
		// 'code' is the code source
		// 'support' is true when the code is support code and shouldn't return a result
		// 'loc' is the file location of the embedded source
		// 'result' is the output of the code after execution, converted to a string
		// Return true, if the code was executed successfully, false if not handled.
		// If the code can be handled but has errors, throw 'Exception's.
		bool Execute(string const& code, bool support, string& result) override
		{
			// Record the number of items on the stack
			int base = lua_gettop(m_lua);

			// Convert the lua code to a compiled chunk
			pr::string<> error_msg;
			if (pr::lua::PushLuaChunk(m_lua, Narrow(code), error_msg) != pr::lua::EResult::Success)
				throw std::exception(error_msg.c_str());

			// Execute the chunk
			if (!pr::lua::CallLuaChunk(m_lua, 0, false))
				throw std::exception("Error while attempting to execute lua code");

			// If there's something still on the stack, copy it to result
			if (lua_gettop(m_lua) != base && !lua_isnil(m_lua, -1))
			{
				result = !support ? Widen(lua_tostring(m_lua, -1)) : L"";
				lua_pop(m_lua, 1);
			}

			// Ensure the stack does not grow
			if (lua_gettop(m_lua) != base)
			{
				lua_settop(m_lua, base);
				throw std::exception("lua stack height not constant");
			}

			// Report 'handled'
			return true;
		}
	};
}
