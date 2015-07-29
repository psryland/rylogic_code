//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/location.h"
#include "pr/script/fail_policy.h"
#include "pr/lua/lua.h"
// Requires lua.lib

namespace pr
{
	namespace script
	{
		// An embedded code handler that supports lua code
		// Serves as the default for Preprocessor and as an interface definition.
		template <typename FailPolicy = ThrowOnFailure>
		struct EmbeddedLua :IEmbeddedCode
		{
			pr::lua::Lua m_lua;

			// A handler function for executing embedded code
			// 'lang' is a string identifying the language of the embedded code.
			// 'code' is the code source
			// 'loc' is the file location of the embedded source
			// 'result' is the output of the code after execution, converted to a string
			// Return true, if the code was executed successfully, false if not handled.
			// If the code can be handled but has errors, throw 'Exception's.
			bool Execute(string const& lang, string const& code, Location const& loc, string& result) override
			{
				// We only handle lua code
				if (lang != L"lua")
					return false;

				// Record the number of items on the stack
				int base = lua_gettop(m_lua);

				// Convert the lua code to a compiled chunk
				pr::string<> error_msg;
				if (pr::lua::PushLuaChunk(m_lua, Narrow(code), error_msg) != pr::lua::EResult::Success)
					return FailPolicy::Fail(EResult::EmbeddedCodeSyntaxError, loc, error_msg.c_str()), false;

				// Execute the chunk
				if (!pr::lua::CallLuaChunk(m_lua, 0, false))
					return FailPolicy::Fail(EResult::EmbeddedCodeExecutionFailed, loc, "Error while attempting to execute lua code"), false;

				// If there's something still on the stack, copy it to result
				if (lua_gettop(m_lua) != base && !lua_isnil(m_lua, -1))
				{
					result = Widen(lua_tostring(m_lua, -1));
					lua_pop(m_lua, 1);
				}

				// Ensure the stack does not grow
				if (lua_gettop(m_lua) != base)
				{
					assert(false && "lua stack height not constant");
					lua_settop(m_lua, base);
				}

				// Report 'handled'
				return true;
			}
		};
	}
}
