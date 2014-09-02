//*************************************
// PRStringise
//  Copyright (c) Rylogic Ltd 2007
//*************************************
#pragma once
#ifndef PR_MACRO_LINK_H
#define PR_MACRO_LINK_H

#include "pr/macros/stringise.h"

// Usage:
//  #pragma message( PR_LINK "Hello World" )

#define PR_SHOW_LINK_PATH
#ifdef PR_SHOW_LINK_PATH
#	define PR_LINK __FILE__ "(" PR_STRINGISE(__LINE__) ") : "
#else
#	define PR_LINK
#endif

#endif
