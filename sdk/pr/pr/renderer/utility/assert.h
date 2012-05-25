//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_ASSERT_H
#define PR_RDR_ASSERT_H

#include "pr/common/assert.h"

#ifndef PR_DBG_RDR
#define PR_DBG_RDR PR_DBG
#endif

#if PR_DBG_RDR == 1 && !defined(D3D_DEBUG_INFO)
//#define D3D_DEBUG_INFO;
#endif

#define PR_DBG_RDR_SHADERS 0

enum EDbgRdrFlags
{
	EDbgRdrFlags_WarnedNoRenderNuggets = 1 << 0,
};


#endif
