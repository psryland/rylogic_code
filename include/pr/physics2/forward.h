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
#include "pr/common/event_handler.h"
#include "pr/str/to_string.h"
#include "pr/container/vector.h"
#include "pr/container/byte_data.h"
#include "pr/maths/maths.h"
#include "pr/maths/spatial.h"
#include "pr/collision/shapes.h"
#include "pr/collision/collision.h"
#include "pr/geometry/closest_point.h"
#include "pr/geometry/intersect.h"

namespace pr::physics
{
	// Import types into this namespace
	using namespace ::pr::maths::spatial;
	using namespace ::pr::collision;

	// Forwards
	struct Material;
	struct MaterialMap;
	struct RigidBody;
	
	// A container object that groups the parts of a physics system together
	template <typename TBroadphase, typename TMaterials>
	struct Engine;

	// Literals
	constexpr float operator "" _kg(long double mass)
	{
		return float(mass);
	}
	constexpr float operator "" _m(long double dist)
	{
		return float(dist);
	}

	// Traits
	template <typename T> struct is_rb :std::false_type {};
	template <> struct is_rb<RigidBody> :std::true_type {};

	template <typename T> using enable_if_rb = typename std::enable_if<is_rb<T>::value>::type;

	// Bring functions into the pr::physics namespace
	using pr::FEql;
}
