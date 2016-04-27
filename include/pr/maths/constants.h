//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"

namespace pr
{
	template <typename Type> using limits = std::numeric_limits<Type>;

	namespace maths
	{
		// Careful!, these are global variables and therefore
		// do not have a defined construction order. If you're using
		// them in other global objects they mightn't be initialised
		constexpr float const tiny       = 1.00000002e-4F; // Can't go lower than this cos DX uses less precision
		constexpr float const tiny_sq    = 1.00000008e-8F; // Can't go lower than this cos DX uses less precision
		constexpr float const phi        = 1.618034e+0F; // "Golden Ratio"
		constexpr float const tau        = 6.283185e+0F; // circle constant
		constexpr float const inv_tau    = 1.591549e-1F;
		constexpr float const tau_by_2   = 3.141593e+0F;
		constexpr float const tau_by_4   = 1.570796e+0F;
		constexpr float const tau_by_8   = 7.853982e-1F;
		constexpr float const tau_by_360 = 1.745329e-2F;
		constexpr float const E60_by_tau = 5.729578e+1F;
		constexpr float const root2      = 1.414214e+0F;
		constexpr float const inv_root2  = 7.071069e-1F;
		constexpr double const dbl_tiny = 1.000000e-12;

		char const    char_min     = limits<char>::min();
		char const    char_max     = limits<char>::max();
		uint8 const   uint8_min    = limits<uint8>::min();
		uint8 const   uint8_max    = limits<uint8>::max();
		short const   short_min    = limits<short>::min();
		short const   short_max    = limits<short>::max();
		uint16 const  uint16_min   = limits<uint16>::min();
		uint16 const  uint16_max   = limits<uint16>::max();
		int const     int_min      = limits<int>::min();
		int const     int_max      = limits<int>::max();
		uint const    uint_min     = limits<uint>::min();
		uint const    uint_max     = limits<uint>::max();
		int64 const   int64_min    = limits<int64>::min();
		int64 const   int64_max    = limits<int64>::max();
		uint64 const  uint64_min   = limits<uint64>::min();
		uint64 const  uint64_max   = limits<uint64>::max();
		float const   float_min    = limits<float>::min();
		float const   float_max    = limits<float>::max();
		float const   float_eps    = limits<float>::epsilon();
		double const  double_min   = limits<double>::min();
		double const  double_max   = limits<double>::max();
		double const  double_eps   = limits<double>::epsilon();
	}
}
