//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"
#include "pr/lua/lua.h"
#include "pr/filesys/filewatch.h"
#include "pr/script/embedded.h"

namespace ldr
{
	// This class processes lua code
	class LuaSource :public pr::script::IEmbeddedCode
	{
		pr::lua::Lua m_lua;
		bool IEmbeddedCode_Execute(pr::script::string const& lang, pr::script::string const& code, pr::script::Location const& loc, pr::script::string& result) override;

	public:
		LuaSource();

		// Add a lua source file
		void Add(char const* filepath);

		// Return a string containing demo ldr lua script
		std::string CreateDemoLuaSource() const;
	};
}
