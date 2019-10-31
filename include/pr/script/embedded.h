//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/location.h"
#include "pr/script/fail_policy.h"

namespace pr::script
{
	// At interface for embedded code handlers
	struct IEmbeddedCode
	{
		// Embedded code in a script has two main categories; support code, and execution code.
		// Execution code is code to be run immediately that returns a string result.
		// Support code is extra code needed to support the execution code (functions etc).
		// #embedded blocks that contain support code should be concatenated in the internal
		// state of the embedded code handler. #embedded blocks containing execution code should
		// not be preserved.
		// A new embedded code handler instance is created for each preprocessor to prevent
		// accidental reuse of the same handler by multiple threads.
		virtual ~IEmbeddedCode() {}

		// The language code that this handler is for
		virtual wchar_t const* Lang() const = 0;

		// A handler function for executing embedded code
		// 'code' is the code source
		// 'support' is true if the code is support code
		// 'result' is the output of the code after execution, converted to a string.
		// Return true, if the code was executed successfully, false if not handled.
		// If the code can be handled but has errors, throw 'std::exception's.
		virtual bool Execute(wchar_t const* code, bool support, string_t& result) = 0;
	};

	// A factory function for creating embedded code handler instances
	// Signature: std::unique_ptr<IEmbeddedCode> CreateHandler(wchar_t const* lang);
	using EmbeddedCodeFactory = std::function<std::unique_ptr<IEmbeddedCode>(wchar_t const*)>;
}
