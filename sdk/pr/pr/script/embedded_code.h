//**********************************
// Script charactor source
//  Copyright (c) Rylogic Ltd 2007
//**********************************
#pragma once
#ifndef PR_SCRIPT_EMBEDDED_CODE_H
#define PR_SCRIPT_EMBEDDED_CODE_H

#include "pr/script/script_core.h"
#include "pr/script/keywords.h"
#include "pr/script/char_stream.h"

namespace pr
{
	namespace script
	{
		// Interface for handling embedded code
		struct IEmbeddedCode
		{
			virtual ~IEmbeddedCode() {}

			// A handler function for executing embedded code
			// 'code_id' is a string identifying the language of the embedded code.
			// 'code' is the code source
			// 'loc' is the file location of the embedded source
			// 'result' is the output of the code after execution, converted to a string
			// Return true, if the code was executed successfully, false if not handled.
			// If the code can be handled but has errors, throw 'Exception's.
			virtual bool IEmbeddedCode_Execute(char const* code_id, pr::script::string const& code, pr::script::Loc const& loc, pr::script::string& result) = 0;
		};

		// An embedded code handler that silently ignores everything between #embedded(lang)..#end
		struct IgnoreEmbeddedCode :IEmbeddedCode
		{
			bool IEmbeddedCode_Execute(char const*, pr::script::string const&, pr::script::Loc const&, pr::script::string& result)
			{
				result.clear();
				return true;
			}
		};
	}
}

#endif
