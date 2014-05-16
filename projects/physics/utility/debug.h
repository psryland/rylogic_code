//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_DEBUG_H
#define PR_PHYSICS_DEBUG_H

#include "pr/physics/types/forward.h"
#include "physics/utility/assert.h"

#if PR_DBG_PHYSICS
#define PR_DBG_COLLISION                0
#define PR_DBG_MESH_COLLISION           0
#define PR_DBG_TERR_COLLISION           0
#define PR_DBG_BOX_COLLISION            0
#define PR_DBG_CYL_COLLISION            0
#define PR_DBG_SPH_BOX_COLLISION        0
#define PR_DBG_SPH_CYL_COLLISION        0
#define PR_DBG_BOX_TRI_COLLISION        0
#define PR_DBG_BOX_CYL_COLLISION        0
#define PR_DBG_SLEEPING                 0
#define PR_LOG_RB                       0
#define PH_LDR_OUTPUT                   0
#define PR_LDR_SLEEPING                 0
#define PR_LDR_PUSHOUT                  0
#else
#define PR_DBG_COLLISION                0
#define PR_DBG_MESH_COLLISION           0
#define PR_DBG_TERR_COLLISION           0
#define PR_DBG_BOX_COLLISION            0
#define PR_DBG_CYL_COLLISION            0
#define PR_DBG_SPH_BOX_COLLISION        0
#define PR_DBG_SPH_CYL_COLLISION        0
#define PR_DBG_BOX_TRI_COLLISION        0
#define PR_DBG_BOX_CYL_COLLISION        0
#define PR_DBG_SLEEPING                 0
#define PR_LOG_RB                       0
#define PH_LDR_OUTPUT                   0
#define PR_LDR_SLEEPING                 0
#define PR_LDR_PUSHOUT                  0
#endif

#if PH_LDR_OUTPUT
#include "physics/utility/ldrhelper_private.h"
#endif

namespace pr
{
	bool IsFinite(const ph::Material& mat);
	namespace ph
	{
		const float OverflowValue = 1e10f;
		
		#if PR_DBG_PHYSICS
		void DebugOutput(const char* str);
		void DebugOutput(int cx, int cy, const char* str);
		#endif
		
		#if PR_LOG_RB
		void Log(Rigidbody& rb, char const* str);
		#endif
	}
}

#endif
