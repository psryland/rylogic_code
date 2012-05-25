//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_PROFILE_H
#define PR_PHYSICS_PROFILE_H

#include "pr/common/profile.h"
#include "pr/common/profilemanager.h"

// If you add to these remember to add a warning in Debug.cpp
#define PR_PROFILE_ENGINE_STEP         0
#define PR_PROFILE_BROADPHASE          0
#define PR_PROFILE_NARROW_PHASE        0
#define PR_PROFILE_RESOLVE_COLLISION   0
#define PR_PROFILE_COLLISION_DETECTION 0
#define PR_PROFILE_TERR_COLLISION      0
#define PR_PROFILE_MESH_COLLISION      0
#define PR_PROFILE_BOX_COLLISION       0
#define PR_PROFILE_CYL_COLLISION       0
#define PR_PROFILE_SUPPORT_VERTS       0
#define PR_PROFILE_SLEEPING            0

#endif
