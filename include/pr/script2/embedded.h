//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script2/forward.h"
#include "pr/script2/location.h"
#include "pr/script2/fail_policy.h"

namespace pr
{
	namespace script2
	{
		// An embedded code handler that doesn't handle any code.
		// Serves as the default for Preprocessor and as an interface definition.
		template <typename FailPolicy = ThrowOnFailure>
		struct NoEmbeddedCode
		{
			// Execute the given 'code' from the language 'lang'
			// 'loc' is the location of the start of the code within the source
			// 'result' should return the result of executing the code
			void Execute(pr::string<wchar_t> lang, pr::string<wchar_t> const& code, Location const& loc, pr::string<wchar_t>& result)
			{
				(void)lang, code, loc, result;
				FailPolicy::Fail(EResult::EmbeddedCodeNotSupported, loc, "No support for embedded code available");
			}
		};
	}
}
