//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script2/script_core.h"
#include "pr/script2/fail_policy.h"

namespace pr
{
	namespace script2
	{
		// A preprocessor macro definition
		template <typename FailPolicy = ThrowOnFailure>
		struct IncludeHandler
		{
			// Add a path to the include search paths
			void AddSearchPath(pr::string<wchar_t> path, size_t index = ~0U)
			{
				(void)path,index;
			}

			// Open an include file as a source
			std::unique_ptr<Src*> Open(pr::string<wchar_t> filepath, Location const& loc, bool search_paths_only)
			{
				(void)filepath,loc,search_paths_only;
				return nullptr;
			}
		};
	}
}
