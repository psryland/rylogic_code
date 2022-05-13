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
#include "pr/maths/matrix2x2.h"
#include "pr/maths/matrix3x4.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/matrix6x8.h"
#include "pr/maths/quaternion.h"

namespace pr
{
	// Deprecated - Use VecN::Zero() ...

	constexpr v2 v2Zero    = v2{0.0f, 0.0f};
	constexpr v2 v2Half    = v2{0.5f, 0.5f};
	constexpr v2 v2One     = v2{1.0f, 1.0f};
	constexpr v2 v2TinyF   = v2{maths::tinyf, maths::tinyf};
	constexpr v2 v2Min     = v2{+maths::float_min, +maths::float_min};
	constexpr v2 v2Max     = v2{+maths::float_max, +maths::float_max};
	constexpr v2 v2Lowest  = v2{-maths::float_max, -maths::float_max};
	constexpr v2 v2Epsilon = v2{+maths::float_eps, +maths::float_eps};
	constexpr v2 v2XAxis   = v2{1.0f, 0.0f};
	constexpr v2 v2YAxis   = v2{0.0f, 1.0f};

	constexpr v3 v3Zero    = v3{0.0f, 0.0f, 0.0f};
	constexpr v3 v3Half    = v3{0.5f, 0.5f, 0.5f};
	constexpr v3 v3One     = v3{1.0f, 1.0f, 1.0f};
	constexpr v3 v3TinyF   = v3{maths::tinyf, maths::tinyf, maths::tinyf};
	constexpr v3 v3Min     = v3{+maths::float_min, +maths::float_min, +maths::float_min};
	constexpr v3 v3Max     = v3{+maths::float_max, +maths::float_max, +maths::float_max};
	constexpr v3 v3Lowest  = v3{-maths::float_max, -maths::float_max, -maths::float_max};
	constexpr v3 v3Epsilon = v3{+maths::float_eps, +maths::float_eps, +maths::float_eps};
	constexpr v3 v3XAxis   = v3{1.0f, 0.0f, 0.0f};
	constexpr v3 v3YAxis   = v3{0.0f, 1.0f, 0.0f};
	constexpr v3 v3ZAxis   = v3{0.0f, 0.0f, 1.0f};

	constexpr v4 v4Zero    = v4{0.0f, 0.0f, 0.0f, 0.0f};
	constexpr v4 v4Half    = v4{0.5f, 0.5f, 0.5f, 0.5f};
	constexpr v4 v4One     = v4{1.0f, 1.0f, 1.0f, 1.0f};
	constexpr v4 v4TinyF   = v4{maths::tinyf, maths::tinyf, maths::tinyf, maths::tinyf};
	constexpr v4 v4Min     = v4{+maths::float_min, +maths::float_min, +maths::float_min, +maths::float_min};
	constexpr v4 v4Max     = v4{+maths::float_max, +maths::float_max, +maths::float_max, +maths::float_max};
	constexpr v4 v4Lowest  = v4{-maths::float_max, -maths::float_max, -maths::float_max, -maths::float_max};
	constexpr v4 v4Epsilon = v4{+maths::float_eps, +maths::float_eps, +maths::float_eps, +maths::float_eps};
	constexpr v4 v4XAxis   = v4{1.0f, 0.0f, 0.0f, 0.0f};
	constexpr v4 v4YAxis   = v4{0.0f, 1.0f, 0.0f, 0.0f};
	constexpr v4 v4ZAxis   = v4{0.0f, 0.0f, 1.0f, 0.0f};
	constexpr v4 v4Origin  = v4{0.0f, 0.0f, 0.0f, 1.0f};

	constexpr v8 v8Zero = v8{0.0f, 0.0f, 0.0f, 0.0f,  0.0f, 0.0f, 0.0f, 0.0f};

	constexpr quat QuatZero     = quat{0.0f, 0.0f, 0.0f, 0.0f};
	constexpr quat QuatIdentity = quat{0.0f, 0.0f, 0.0f, 1.0f};

	constexpr m2x2 m2x2Zero     = m2x2{v2Zero, v2Zero};
	constexpr m2x2 m2x2Identity = m2x2{v2XAxis, v2YAxis};
	constexpr m2x2 m2x2One      = m2x2{v2One, v2One};
	constexpr m2x2 m2x2Min      = m2x2{+v2Min, +v2Min};
	constexpr m2x2 m2x2Max      = m2x2{+v2Max, +v2Max};
	constexpr m2x2 m2x2Lowest   = m2x2{-v2Max, -v2Max};
	constexpr m2x2 m2x2Epsilon  = m2x2{+v2Epsilon, +v2Epsilon};

	constexpr m3x4 m3x4Zero     = m3x4{v4Zero, v4Zero, v4Zero};
	constexpr m3x4 m3x4Identity = m3x4{v4XAxis, v4YAxis, v4ZAxis};
	constexpr m3x4 m3x4One      = m3x4{v4One, v4One, v4One};
	constexpr m3x4 m3x4Min      = m3x4{+v4Min, +v4Min, +v4Min};
	constexpr m3x4 m3x4Max      = m3x4{+v4Max, +v4Max, +v4Max};
	constexpr m3x4 m3x4Lowest   = m3x4{-v4Max, -v4Max, -v4Max};
	constexpr m3x4 m3x4Epsilon  = m3x4{+v4Epsilon, +v4Epsilon, +v4Epsilon};

	constexpr m4x4 m4x4Zero     = m4x4{v4Zero, v4Zero, v4Zero, v4Zero};
	constexpr m4x4 m4x4Identity = m4x4{v4XAxis, v4YAxis, v4ZAxis, v4Origin};
	constexpr m4x4 m4x4One      = m4x4{v4One, v4One, v4One, v4One};
	constexpr m4x4 m4x4Min      = m4x4{+v4Min, +v4Min, +v4Min, +v4Min};
	constexpr m4x4 m4x4Max      = m4x4{+v4Max, +v4Max, +v4Max, +v4Max};
	constexpr m4x4 m4x4Lowest   = m4x4{-v4Max, -v4Max, -v4Max, -v4Max};
	constexpr m4x4 m4x4Epsilon  = m4x4{+v4Epsilon, +v4Epsilon, +v4Epsilon, +v4Epsilon};

	constexpr m6x8 m6x8Zero     = m6x8{ m3x4Zero, m3x4Zero, m3x4Zero, m3x4Zero };
	constexpr m6x8 m6x8Identity = m6x8{ m3x4Identity, m3x4Zero, m3x4Zero, m3x4Identity };

	constexpr iv2 iv2Zero    = iv2{0, 0};
	constexpr iv2 iv2One     = iv2{1, 1};
	constexpr iv2 iv2Min     = iv2{+maths::int32_min, +maths::int32_min};
	constexpr iv2 iv2Max     = iv2{+maths::int32_max, +maths::int32_max};
	constexpr iv2 iv2Lowest  = iv2{-maths::int32_max, -maths::int32_max};
	constexpr iv2 iv2XAxis   = iv2{1, 0};
	constexpr iv2 iv2YAxis   = iv2{0, 1};

	constexpr iv3 iv3Zero    = iv3{0, 0, 0};
	constexpr iv3 iv3One     = iv3{1, 1, 1};
	constexpr iv3 iv3Min     = iv3{+maths::int32_min, +maths::int32_min, +maths::int32_min};
	constexpr iv3 iv3Max     = iv3{+maths::int32_max, +maths::int32_max, +maths::int32_max};
	constexpr iv3 iv3Lowest  = iv3{-maths::int32_max, -maths::int32_max, -maths::int32_max};
	constexpr iv3 iv3XAxis   = iv3{1, 0, 0};
	constexpr iv3 iv3YAxis   = iv3{0, 1, 0};
	constexpr iv3 iv3ZAxis   = iv3{0, 0, 1};

	constexpr iv4 iv4Zero   = iv4{0, 0, 0, 0};
	constexpr iv4 iv4One    = iv4{1, 1, 1, 1};
	constexpr iv4 iv4Min    = iv4{+maths::int32_min, +maths::int32_min, +maths::int32_min, +maths::int32_min};
	constexpr iv4 iv4Max    = iv4{+maths::int32_max, +maths::int32_max, +maths::int32_max, +maths::int32_max};
	constexpr iv4 iv4Lowest = iv4{-maths::int32_max, -maths::int32_max, -maths::int32_max, -maths::int32_max};
	constexpr iv4 iv4XAxis  = iv4{1, 0, 0, 0};
	constexpr iv4 iv4YAxis  = iv4{0, 1, 0, 0};
	constexpr iv4 iv4ZAxis  = iv4{0, 0, 1, 0};
	constexpr iv4 iv4Origin = iv4{0, 0, 0, 1};
}

