//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include <array>
#include <type_traits>
#include <algorithm>

#include "pr/common/to.h"
#include "pr/common/cast.h"
#include "pr/common/flags_enum.h"
#include "pr/common/scope.h"
#include "pr/str/to_string.h"
#include "pr/container/vector.h"
#include "pr/container/byte_data.h"
#include "pr/maths/maths.h"
#include "pr/maths/spatial.h"
#include "pr/collision/shapes.h"
#include "pr/geometry/closest_point.h"


namespace pr
{
	namespace physics
	{
		using metres_t   = float;
		using metres²_t  = float;
		using metres³_t  = float;
		using kg_p_m³_t  = float;
		using kg_t       = float;

		// Import collision into the physics namespace
		using namespace ::pr::collision;

		// Forwards
		struct Material;
		struct RigidBody;

		// Literals
		constexpr kg_t operator "" _kg(long double mass)
		{
			return kg_t(mass);
		}
		constexpr metres_t operator "" _m(long double dist)
		{
			return metres_t(dist);
		}

		// Traits
		template <typename T> struct is_rb :std::false_type {};
		template <> struct is_rb<RigidBody> :std::true_type {};

		template <typename T> using enable_if_rb = typename std::enable_if<is_rb<T>::value>::type;
	}
}
