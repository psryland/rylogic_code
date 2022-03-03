//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************
#pragma once
#include "pr/script/forward.h"
#include "pr/script/location.h"

namespace pr::script
{
	// Script exception
	struct ScriptException :std::runtime_error
	{
		EResult m_result;
		Loc m_loc;

		ScriptException(EResult result, Loc const& loc, std::string msg)
			:std::runtime_error(msg)
			,m_result(result)
			,m_loc(loc)
		{}
		ScriptException(EResult result, Loc const& loc, std::wstring msg)
			:ScriptException(result, loc, Narrow(msg))
		{}
		std::string Message() const
		{
			return std::string()
				.append(what())
				.append("\r\nError Code: ").append(Enum<EResult>::ToStringA(m_result))
				.append("\r\nLocation: ").append(Narrow(m_loc.ToString()));
		}
	};
}
