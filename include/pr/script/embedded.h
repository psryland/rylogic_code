//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/location.h"
#include "pr/script/fail_policy.h"

namespace pr
{
	namespace script
	{
		// At interface for embedded code handlers
		struct IEmbeddedCode
		{
			virtual ~IEmbeddedCode() {}

			// A handler function for executing embedded code
			// 'lang' is a string identifying the language of the embedded code.
			// 'code' is the code source
			// 'loc' is the file location of the embedded source
			// 'result' is the output of the code after execution, converted to a string
			// Return true, if the code was executed successfully, false if not handled.
			// If the code can be handled but has errors, throw 'Exception's.
			virtual bool IEmbeddedCode_Execute(string const& lang, string const& code, Location const& loc, string& result) = 0;
		};

		// An embedded code handler that doesn't handle any code.
		// Serves as the default for Preprocessor and as an interface definition.
		template <typename FailPolicy = ThrowOnFailure>
		struct EmbeddedCode :IEmbeddedCode
		{
			std::vector<IEmbeddedCode*> Handler;

			// Forwards the code to all handlers, returning after the first that reports 'handled'.
			bool IEmbeddedCode_Execute(string const& lang, string const& code, Location const& loc, string& result) override
			{
				for (auto& handler : Handler)
					if (handler->IEmbeddedCode_Execute(lang, code, loc, result))
						return true;

				return false;
			}
		};
	}
}
