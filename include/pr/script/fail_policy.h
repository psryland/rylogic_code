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
			Location const* m_loc;
			Exception(EResult result, Location const& loc, std::string msg)
				:pr::Exception<EResult>(result, msg)
				,m_loc(&loc)
			{}
		};

		// When a script error occurs, throws a pr::script::exception
		struct ThrowOnFailure
		{
			template <typename TResult>
			static TResult Fail(EResult result, Location const& loc, std::string msg, TResult = TResult())
			{
				throw Exception(result, loc, msg);
			}
			static void Fail(EResult result, Location const& loc, std::string msg)
			{
				throw Exception(result, loc, msg);
			}
		};
	}
}
