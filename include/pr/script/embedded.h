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

			// Embedded code in a script has two main categories; support code, and execution code.
			// Execution code is code to be run immediately that returns a string result.
			// Support code is extra code needed to support the execution code (functions etc).
			// #embedded blocks that containing support code should be concatenated in the internal
			// state of the embedded code handler. #embedded blocks containing execution code should
			// not be preserved

			// When a new script is being parsed, 'Reset' is called to flush any buffered support code.
			virtual void Reset() = 0;

			// A handler function for executing embedded code
			// 'lang' is a string identifying the language of the embedded code.
			// 'code' is the code source
			// 'result' is the output of the code after execution, converted to a string.
			// Return true, if the code was executed successfully, false if not handled.
			// If the code can be handled but has errors, throw 'std::exception's.
			virtual bool Execute(string const& lang, string const& code, string& result) = 0;
		};

		// A container of embedded code handlers
		// Serves as the default for Preprocessor and as an interface definition.
		struct EmbeddedCode :IEmbeddedCode
		{
			std::vector<IEmbeddedCode*> Handler;

			EmbeddedCode()
				:Handler()
			{}
			EmbeddedCode(std::initializer_list<IEmbeddedCode*> handlers)
				:Handler(handlers)
			{}

			// Reset embedded code handlers
			void Reset()
			{
				// Reset each handler
				for (auto handler : Handler)
					handler->Reset();
			}

			// Execute the given code
			bool Execute(string const& lang, string const& code, string& result) override
			{
				// Forwards the code to all handlers, returning after the first that reports 'handled'.
				for (auto& handler : Handler)
					if (handler->Execute(lang, code, result))
						return true;

				return false;
			}
		};
	}
}
