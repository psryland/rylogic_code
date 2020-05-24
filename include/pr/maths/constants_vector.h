//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector2.h"
#include "pr/maths/vector3.h"
#include "pr/maths/vector4.h"
#include "pr/maths/vector8.h"
#include "pr/maths/ivector2.h"
#include "pr/maths/ivector4.h"
#include "pr/maths/matrix2x2.h"
#include "pr/maths/matrix3x4.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/matrix6x8.h"
#include "pr/maths/quaternion.h"

namespace pr
{
	constexpr v2 v2Zero    = v2{0.0f, 0.0f};
	constexpr v2 v2Half    = v2{0.5f, 0.5f};
	constexpr v2 v2One     = v2{1.0f, 1.0f};
	constexpr v2 v2Min     = v2{+maths::float_min, +maths::float_min};
	constexpr v2 v2Max     = v2{+maths::float_max, +maths::float_max};
	constexpr v2 v2Lowest  = v2{-maths::float_max, -maths::float_max};
	constexpr v2 v2Epsilon = v2{+maths::float_eps, +maths::float_eps};
	constexpr v2 v2XAxis   = v2{1.0f, 0.0f};
	constexpr v2 v2YAxis   = v2{0.0f, 1.0f};

	static v3 const v3Zero    = v3{0.0f, 0.0f, 0.0f};
	static v3 const v3Half    = v3{0.5f, 0.5f, 0.5f};
	static v3 const v3One     = v3{1.0f, 1.0f, 1.0f};
	static v3 const v3Min     = v3{+maths::float_min, +maths::float_min, +maths::float_min};
	static v3 const v3Max     = v3{+maths::float_max, +maths::float_max, +maths::float_max};
	static v3 const v3Lowest  = v3{-maths::float_max, -maths::float_max, -maths::float_max};
	static v3 const v3Epsilon = v3{+maths::float_eps, +maths::float_eps, +maths::float_eps};
	static v3 const v3XAxis   = v3{1.0f, 0.0f, 0.0f};
	static v3 const v3YAxis   = v3{0.0f, 1.0f, 0.0f};
	static v3 const v3ZAxis   = v3{0.0f, 0.0f, 1.0f};

	constexpr v4 v4Zero    = v4{0.0f, 0.0f, 0.0f, 0.0f};
	constexpr v4 v4Half    = v4{0.5f, 0.5f, 0.5f, 0.5f};
	constexpr v4 v4One     = v4{1.0f, 1.0f, 1.0f, 1.0f};
	constexpr v4 v4Min     = v4{+maths::float_min, +maths::float_min, +maths::float_min, +maths::float_min};
	constexpr v4 v4Max     = v4{+maths::float_max, +maths::float_max, +maths::float_max, +maths::float_max};
	constexpr v4 v4Lowest  = v4{-maths::float_max, -maths::float_max, -maths::float_max, -maths::float_max};
	constexpr v4 v4Epsilon = v4{+maths::float_eps, +maths::float_eps, +maths::float_eps, +maths::float_eps};
	constexpr v4 v4XAxis   = v4{1.0f, 0.0f, 0.0f, 0.0f};
	constexpr v4 v4YAxis   = v4{0.0f, 1.0f, 0.0f, 0.0f};
	constexpr v4 v4ZAxis   = v4{0.0f, 0.0f, 1.0f, 0.0f};
	constexpr v4 v4Origin  = v4{0.0f, 0.0f, 0.0f, 1.0f};

	static v8 const v8Zero = v8{0.0f, 0.0f, 0.0f, 0.0f,  0.0f, 0.0f, 0.0f, 0.0f};

	static quat const QuatZero     = quat{0.0f, 0.0f, 0.0f, 0.0f};
	static quat const QuatIdentity = quat{0.0f, 0.0f, 0.0f, 1.0f};

	static m2x2 const m2x2Zero     = m2x2{v2Zero, v2Zero};
	static m2x2 const m2x2Identity = m2x2{v2XAxis, v2YAxis};
	static m2x2 const m2x2One      = m2x2{v2One, v2One};
	static m2x2 const m2x2Min      = m2x2{+v2Min, +v2Min};
	static m2x2 const m2x2Max      = m2x2{+v2Max, +v2Max};
	static m2x2 const m2x2Lowest   = m2x2{-v2Max, -v2Max};
	static m2x2 const m2x2Epsilon  = m2x2{+v2Epsilon, +v2Epsilon};

	static m3x4 const m3x4Zero     = m3x4{v4Zero, v4Zero, v4Zero};
	static m3x4 const m3x4Identity = m3x4{v4XAxis, v4YAxis, v4ZAxis};
	static m3x4 const m3x4One      = m3x4{v4One, v4One, v4One};
	static m3x4 const m3x4Min      = m3x4{+v4Min, +v4Min, +v4Min};
	static m3x4 const m3x4Max      = m3x4{+v4Max, +v4Max, +v4Max};
	static m3x4 const m3x4Lowest   = m3x4{-v4Max, -v4Max, -v4Max};
	static m3x4 const m3x4Epsilon  = m3x4{+v4Epsilon, +v4Epsilon, +v4Epsilon};

	static m4x4 const m4x4Zero     = m4x4{v4Zero, v4Zero, v4Zero, v4Zero};
	static m4x4 const m4x4Identity = m4x4{v4XAxis, v4YAxis, v4ZAxis, v4Origin};
	static m4x4 const m4x4One      = m4x4{v4One, v4One, v4One, v4One};
	static m4x4 const m4x4Min      = m4x4{+v4Min, +v4Min, +v4Min, +v4Min};
	static m4x4 const m4x4Max      = m4x4{+v4Max, +v4Max, +v4Max, +v4Max};
	static m4x4 const m4x4Lowest   = m4x4{-v4Max, -v4Max, -v4Max, -v4Max};
	static m4x4 const m4x4Epsilon  = m4x4{+v4Epsilon, +v4Epsilon, +v4Epsilon, +v4Epsilon};

	static m6x8 const m6x8Zero     = m6x8{ m3x4Zero, m3x4Zero, m3x4Zero, m3x4Zero };
	static m6x8 const m6x8Identity = m6x8{ m3x4Identity, m3x4Zero, m3x4Zero, m3x4Identity };

	static iv2 const iv2Zero    = iv2{0, 0};
	static iv2 const iv2One     = iv2{1, 1};
	static iv2 const iv2Min     = iv2{+maths::int_min, +maths::int_min};
	static iv2 const iv2Max     = iv2{+maths::int_max, +maths::int_max};
	static iv2 const iv2Lowest  = iv2{-maths::int_max, -maths::int_max};
	static iv2 const iv2XAxis   = iv2{1, 0};
	static iv2 const iv2YAxis   = iv2{0, 1};

	static iv4 const iv4Zero   = iv4{0, 0, 0, 0};
	static iv4 const iv4One    = iv4{1, 1, 1, 1};
	static iv4 const iv4Min    = iv4{+maths::int_min, +maths::int_min, +maths::int_min, +maths::int_min};
	static iv4 const iv4Max    = iv4{+maths::int_max, +maths::int_max, +maths::int_max, +maths::int_max};
	static iv4 const iv4Lowest = iv4{-maths::int_max, -maths::int_max, -maths::int_max, -maths::int_max};
	static iv4 const iv4XAxis  = iv4{1, 0, 0, 0};
	static iv4 const iv4YAxis  = iv4{0, 1, 0, 0};
	static iv4 const iv4ZAxis  = iv4{0, 0, 1, 0};
	static iv4 const iv4Origin = iv4{0, 0, 0, 1};
}

