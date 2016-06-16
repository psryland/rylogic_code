//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#include "linedrawer/main/forward.h"
#include "linedrawer/main/lua_source.h"
#include "linedrawer/utility/debug.h"
#include "pr/linedrawer/ldr_object.h"
#include "pr/common/events.h"

namespace ldr
{
	LuaSource* m_this = 0;

	//namespace pr
	//{
	//	namespace lua
	//	{
	//		int ldrClear		(lua_State* lua_state)		{ return LuaInput::Get().ldrClear (lua_state); }
	//		int ldrScript		(lua_State* lua_state)		{ return LuaInput::Get().ldrScript(lua_state); }
	//		int ldrDelete		(lua_State* lua_state)		{ return LuaInput::Get().ldrDelete(lua_state); }
	//		int ldrGetNumObjects(lua_State* lua_state)		{ return LuaInput::Get().ldrGetNumObjects(lua_state); }
	//	}//namespace lua
	//}//namespace pr

	LuaSource::LuaSource()
		:m_lua()
	{
		m_this = this;
		m_lua.SetOutputFuncs(pr::lua::DebugPrint, pr::lua::DebugPrint, 0, 0);

		//// Register functions
		//pr::lua::Register(m_lua, "ldr.Clear"            ,lua::ldrClear);
		//pr::lua::Register(m_lua, "ldr.Script"           ,lua::ldrScript);
		//pr::lua::Register(m_lua, "ldr.Delete"           ,lua::ldrDelete);
		//pr::lua::Register(m_lua, "ldr.GetNumObjects"    ,lua::ldrGetNumObjects);
	}

	void LuaSource::Add(wchar_t const* filepath)
	{
		(void)filepath;
		//m_lua.DoFile(filepath);
	}

	// Execute a string containing lua code.
	// The code is expected to leave a string on the lua stack
	bool LuaSource::Execute(pr::script::string const& lang, pr::script::string const& code, pr::script::Location const& loc, pr::script::string& result)
	{
		// We only handle lua code
		if (!pr::str::Equal(lang, "lua"))
			return false;

		// Record the number of items on the stack
		int base = lua_gettop(m_lua);

		// Convert the lua code to a compiled chunk
		pr::string<> error_msg;
		if (pr::lua::PushLuaChunk<pr::string<>>(m_lua, pr::Narrow(code), error_msg) != pr::lua::EResult::Success)
			throw pr::script::Exception(pr::script::EResult::EmbeddedCodeSyntaxError, loc, error_msg.c_str());

		// Execute the chunk
		if (!pr::lua::CallLuaChunk(m_lua, 0, false))
			return false;

		// If there's something still on the stack, copy it to result
		if (lua_gettop(m_lua) != base && !lua_isnil(m_lua, -1))
		{
			auto r = pr::Widen(lua_tostring(m_lua, -1));
			result = r;
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

	// Return a string containing demo ldr lua script
	std::string LuaSource::CreateDemoLuaSource() const
	{
		std::stringstream out;
		out <<  "--********************************************\n"
				"-- Demo Ldr lua script\n"
				"--********************************************\n"
				"\n"
				"-- Set the rate to call the OnLdrStep() function\n"
				"LdrStepRate = 50 -- 50fps\n"
				"\n"
				"-- Called when the file is loaded by " << AppTitleA() << " \n"
				"function LdrLoad()\n"
				"    -- Create some ldr objects\n"
				"    ldrCreate('*Box point FF00FF00 {1}')\n"
				"end\n"
				"\n"
				"-- Called repeatedly by " << AppTitleA() << "\n"
				"function LdrStep()\n"
				"    -- Create some ldr objects\n"
				"    ldrCreate('*Box point FF00FF00 {1}')\n"
				"end\n"
				"\n"
				;
		return out.str();
	}
}