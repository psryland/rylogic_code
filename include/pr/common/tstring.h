//***********************************************
// tstring
//  Copyright (c) Rylogic Ltd 2009
//***********************************************

#ifndef PR_COMMON_TSTRING_H
#define PR_COMMON_TSTRING_H
#pragma once

#include <string>
#include <tchar.h>

namespace pr
{
	#ifdef _UNICODE
	typedef std::wstring tstring;
	#else
	typedef std::string  tstring;
	#endif
}

#endif
