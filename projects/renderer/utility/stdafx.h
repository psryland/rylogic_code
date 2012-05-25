//*********************************************
// PR Renderer
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_RDR_STDAFX_H
#define PR_RDR_STDAFX_H

#if !defined(_WIN32_WINNT)
#	error "_WIN32_WINNT not defined. Must be defined as 0x0400 or greater."
#endif
#if _WIN32_WINNT < 0x0400
	template <int> class S;	S<_WIN32_WINNT>  version_is_;
#	error "_WIN32_WINNT version 0x0400 or greater required."
#endif

#if _MSC_VER <= 1500
#	pragma warning (disable: 4351)
#endif

#include "pr/renderer/types/forward.h"

#endif

