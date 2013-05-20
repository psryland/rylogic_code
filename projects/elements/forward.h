#pragma once

// std
#include <string>
#include <vector>
#include <algorithm>
#include <iostream>

// pr
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/common/console.h"
#include "pr/common/datetime.h"
#include "pr/common/si_units.h"
#include "pr/macros/enum.h"
#include "pr/macros/no_copy.h"
#include "pr/app/sim_message_loop.h"
#include "pr/maths/maths.h"
#include "pr/maths/rand.h"

namespace ele
{
	struct GameInstance;
	struct GameConstants;
	struct Element;
	struct Material;
	struct Ship;

	#define PR_ENUM(x)\
		x(Home)\
		x(ShipDesign)\
		x(MatResearch)\
		x(Launch)
	PR_DEFINE_ENUM1(EView, PR_ENUM);
	#undef PR_ENUM

	struct ElementName
	{
		char m_name[16];   // E.g. Moronium
		char m_symbol[3];  // E.g  Mr
	};

	template <typename T> inline T sqr(T t)    { return static_cast<T>(t * t); }
	template <typename T> inline T sqrt(T t)   { return static_cast<T>(std::sqrt(t)); }
	template <typename T> inline T cubert(T t) { return static_cast<T>(std::pow(t, 1.0/3.0)); }
	template <typename T> inline T ln(T t)     { return static_cast<T>(std::log(t)); }
	template <typename T, size_t Sz> inline size_t length(T const (&)[Sz]) { return Sz; }
}

