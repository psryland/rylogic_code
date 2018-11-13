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

namespace pr
{
	namespace collision
	{
		typedef void (*Detect)(Shape const& lhs, m4x4 const& l2w, Shape const& rhs, m4x4 const& r2w);
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
