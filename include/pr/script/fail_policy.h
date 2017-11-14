//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/location.h"

namespace pr
{
	namespace script
	{
		// Script exception
		struct Exception :pr::Exception<EResult>
		{
			Location m_loc;

			Exception(EResult result, Location const& loc, std::string msg)
				:pr::Exception<EResult>(result, msg.append("\nError Code: ").append(ToStringA(result)).append("\nLocation: ").append(Narrow(loc.ToString())))
				,m_loc(loc)
			{}
		};
	}
}
