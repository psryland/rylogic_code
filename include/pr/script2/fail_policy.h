//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script2/forward.h"
#include "pr/script2/location.h"

namespace pr
{
	namespace script2
	{
		// Script exception
		struct Exception :pr::Exception<EResult>
		{
			Location const* m_loc;
			Exception(EResult result, Location const& loc, std::string msg)
				:pr::Exception<EResult>(result, msg)
				,m_loc(&loc)
			{}
		};

		// When a script error occurs, throws a pr::script::exception
		struct ThrowOnFailure
		{
			static void Fail(EResult result, Location const& loc, std::string msg)
			{
				throw Exception(result, loc, msg);
			}
		};
	}
}
