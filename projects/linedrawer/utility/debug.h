//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once
#ifndef LDR_DEBUG_H
#define LDR_DEBUG_H

#include "pr/common/assert.h"

#ifndef NDEBUG
#	define PR_DBG_LDR 1
#else
#	define PR_DBG_LDR 0
#endif//NDEBUG

#endif//LDR_DEBUG_H