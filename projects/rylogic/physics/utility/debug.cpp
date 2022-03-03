//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "physics/utility/debug.h"
#include "pr/physics/material/material.h"
#include "pr/physics/rigidbody/rigidbody.h"
#include "physics/utility/profile.h"

using namespace pr;
using namespace pr::ph;

// Return true if a material contains "finite" values
bool pr::IsFinite(ph::Material const& mat)
{
	return  IsFinite(mat.m_density               ,ph::OverflowValue) &&
			IsFinite(mat.m_static_friction       ,10.0f) &&
			IsFinite(mat.m_dynamic_friction      ,10.0f) &&
			IsFinite(mat.m_rolling_friction      ,10.0f) &&
			IsFinite(mat.m_elasticity            ,10.0f) &&
			IsFinite(mat.m_tangential_elasticity ,10.0f) &&
			IsFinite(mat.m_tortional_elasticity  ,10.0f);
}

#if PR_DBG_PHYSICS
#include "pr/common/console.h"
void pr::ph::DebugOutput(const char* str)                   { pr::cons().Write(str); }
void pr::ph::DebugOutput(int cx, int cy, const char* str)   { pr::cons().Write(pr::uint16(cx), pr::uint16(cy), str); }
struct InitConsole { InitConsole() { pr::ph::DebugOutput("Console Active\n"); } } g_init_cons;
#endif

#if PR_LOG_RB
void pr::ph::Log(Rigidbody& rb, char const* str)
{
	std::size_t len = strlen(str);
	if (len == 0) return;
	bool add_return = str[len - 1] != '\n';
	if (add_return) { len += 1; }
	PR_ASSERT(PR_DBG_PHYSICS, len < sizeof(rb.m_log_buf));
	if (std::size_t(rb.m_log - rb.m_log_buf) < len) rb.m_log = &rb.m_log_buf[sizeof(rb.m_log_buf) - 1];
	rb.m_log -= len;
	if (add_return) { memcpy(rb.m_log, str, len - 1); rb.m_log[len - 1] = '\n'; }
	else            { memcpy(rb.m_log, str, len); }
}
#endif

// Profile notification
#ifdef PR_PROFILE_ON
#pragma message("q:/SDK/pr/pr/common/Profile.h: PR_PROFILE_ON is defined")
#endif
#if PR_PROFILE_ENGINE_STEP
#pragma message("q:/SDK/physics/utility/Profile.h: PR_PROFILE_ENGINE_STEP is defined")
#endif
#if PR_PROFILE_BROADPHASE
#pragma message("q:/SDK/physics/utility/Profile.h: PR_PROFILE_BROADPHASE is defined")
#endif
#if PR_PROFILE_NARROW_PHASE
#pragma message("q:/SDK/physics/utility/Profile.h: PR_PROFILE_NARROW_PHASE is defined")
#endif
#if PR_PROFILE_RESOLVE_COLLISION
#pragma message("q:/SDK/physics/utility/Profile.h: PR_PROFILE_RESOLVE_COLLISION is defined")
#endif
#if PR_PROFILE_COLLISION_DETECTION
#pragma message("q:/SDK/physics/utility/Profile.h: PR_PROFILE_COLLISION_DETECTION is defined")
#endif
#if PR_PROFILE_TERR_COLLISION
#pragma message("q:/SDK/physics/utility/Profile.h: PR_PROFILE_TERR_COLLISION is defined")
#endif
#if PR_PROFILE_MESH_COLLISION
#pragma message("q:/SDK/physics/utility/Profile.h: PR_PROFILE_MESH_COLLISION is defined")
#endif
#if PR_PROFILE_BOX_COLLISION
#pragma message("q:/SDK/physics/utility/Profile.h: PR_PROFILE_BOX_COLLISION is defined")
#endif
#if PR_PROFILE_CYL_COLLISION
#pragma message("q:/SDK/physics/utility/Profile.h: PR_PROFILE_CYL_COLLISION is defined")
#endif
#if PR_PROFILE_SUPPORT_VERTS
#pragma message("q:/SDK/physics/utility/Profile.h: PR_PROFILE_SUPPORT_VERTS is defined")
#endif

// Debug notification
#if PH_LDR_OUTPUT
#pragma message("q:/SDK/physics/utility/Debug.h: PH_LDR_OUTPUT is defined")
#endif
#if PR_DBG_COLLISION
#pragma message("q:/SDK/physics/utility/Debug.h: PR_DBG_COLLISION  is defined")
#endif
#if PR_DBG_MESH_COLLISION
#pragma message("q:/SDK/physics/utility/Debug.h: PR_DBG_MESH_COLLISION  is defined")
#endif
#if PR_DBG_TERR_COLLISION
#pragma message("q:/SDK/physics/utility/Debug.h: PR_DBG_TERR_COLLISION is defined")
#endif
#if PR_DBG_BOX_COLLISION
#pragma message("q:/SDK/physics/utility/Debug.h: PR_DBG_BOX_COLLISION  is defined")
#endif
#if PR_DBG_CYL_COLLISION
#pragma message("q:/SDK/physics/utility/Debug.h: PR_DBG_CYL_COLLISION  is defined")
#endif
#if PR_DBG_SPH_BOX_COLLISION
#pragma message("q:/SDK/physics/utility/Debug.h: PR_DBG_SPH_BOX_COLLISION  is defined")
#endif
#if PR_DBG_SPH_CYL_COLLISION
#pragma message("q:/SDK/physics/utility/Debug.h: PR_DBG_SPH_CYL_COLLISION  is defined")
#endif
#if PR_DBG_BOX_TRI_COLLISION
#pragma message("q:/SDK/physics/utility/Debug.h: PR_DBG_BOX_TRI_COLLISION  is defined")
#endif
#if PR_LOG_RB
#pragma message("q:/SDK/physics/utility/Debug.h: PR_LOG_RB  is defined")
#endif
