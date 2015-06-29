//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once
#include "pr/script2/script_core.h"

namespace pr
{
	namespace script2
	{
		// A union of the indivisible script elements
		struct Token
		{
			EToken m_tok; // The token type, implies which other members are valid
		};
	}
}

