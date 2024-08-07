//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_ASSERT_H
#define PR_PHYSICS_ASSERT_H

#ifndef PR_OUTPUT_MSG
#define PR_OUTPUT_MSG(str) OutputDebugStringA(str)
#endif

#include "pr/common/assert.h"

#ifndef PR_DBG_PHYSICS
#define PR_DBG_PHYSICS  PR_DBG
#endif

#endif
