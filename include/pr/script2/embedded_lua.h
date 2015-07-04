//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script2/forward.h"
#include "pr/script2/location.h"
#include "pr/script2/fail_policy.h"
#include "pr/lua/lua.h"
// Requires lua.lib

namespace pr
{
	namespace script2
	{
		// An embedded code handler that supports lua code
		// Serves as the default for Preprocessor and as an interface definition.
		template <typename FailPolicy = ThrowOnFailure>
		struct EmbeddedLua
		{
			pr::lua::Lua m_lua;
			
			// Execute the given 'code' from the language 'lang'
			// 'loc' is the location of the start of the code within the source
			// 'result' should return the result of executing the code
			void Execute(pr::string<wchar_t> lang, pr::string<wchar_t> const& code, Location const& loc, pr::string<wchar_t>& result)
			{
				// We only handle lua code
				if (lang != L"lua")
					return FailPolicy::Fail(EResult::EmbeddedCodeNotSupported, loc, pr::FmtS("Code language '%s' not supported. Only 'lua' code supported", lang.c_str()));

				// Record the number of items on the stack
				int base = lua_gettop(m_lua);

				// Convert the lua code to a compiled chunk
				pr::string<char> error_msg;
				if (pr::lua::PushLuaChunk(m_lua, code, error_msg) != pr::lua::EResult::Success)
					return FailPolicy::Fail(EResult::EmbeddedCodeSyntaxError, loc, error_msg.c_str());

				// Execute the chunk
				if (!pr::lua::CallLuaChunk(m_lua, 0, false))
					return FailPolicy::Fail(EResult::EmbeddedCodeExecutionFailed, loc, "Error while attempting to execute lua code");

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
			}
		};
	}
}
