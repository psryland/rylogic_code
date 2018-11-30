//*********************************************
// Collision
//  Copyright (c) Rylogic Ltd 2006
//*********************************************
#pragma once

#include "pr/collision/shapes.h"
#include "pr/collision/penetration.h"
#include "pr/collision/support.h"
#include "pr/collision/col_sphere_vs_sphere.h"
#include "pr/collision/col_sphere_vs_box.h"
#include "pr/collision/col_box_vs_box.h"

namespace pr::collision
{
	// Function type for collection detection
	using Detect = void (*)(Shape const& lhs, m4_cref<> l2w, Shape const& rhs, m4_cref<> r2w);
	inline Detect Collide()
	{
		//static Detect s_collision_functions[] = 
		//{
		//};
		return nullptr;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::collision
{
	PRUnitTest(CollisionDetectionTests)
	{
	}
}
#endif
