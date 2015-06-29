//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script2/script_core.h"
#include "pr/script2/location.h"

namespace pr
{
	namespace script2
	{
		// When a script error occurs, throws a pr::script::exception
		struct ThrowOnFailure
		{
			static bool Fail(EResult result, Loc const& loc, std::string msg)
			{
				throw Exception(result, loc, msg);
			}
		};
	}
}
