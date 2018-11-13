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
		constexpr float const tiny       = float(1.00000007e-05);
		constexpr float const tiny_sq    = float(1.00000015e-10);
		constexpr float const tiny_sqrt  = float(3.16227786e-03);

		constexpr double const tinyd      = double(1.0000000000000002e-12);
		constexpr double const tinyd_sq   = double(1.0000000000000003e-24);
		constexpr double const tinyd_sqrt = double(1.0000000000000002e-06);

		constexpr double const phi        = double(1.618033988749894848204586834);    // "Golden Ratio"
		constexpr double const tau        = double(6.283185307179586476925286766559);    // circle constant
		constexpr double const inv_tau    = 1.0 / tau;
		constexpr double const tau_by_2   = tau / 2.0;
		constexpr double const tau_by_4   = tau / 4.0;
		constexpr double const tau_by_8   = tau / 8.0;
		constexpr double const tau_by_360 = tau / 360.0;
		constexpr double const E60_by_tau = 360.0 / tau;
		constexpr double const root2      = double(1.4142135623730950488);
		constexpr double const root3      = double(1.7320508075688772935274463415059);
		constexpr double const inv_root2  = 1.0 / root2;
		constexpr double const inv_root3  = 1.0 / root3;

		constexpr char const    char_min     = limits<char>::min();
		constexpr char const    char_max     = limits<char>::max();
		constexpr uint8 const   uint8_min    = limits<uint8>::min();
		constexpr uint8 const   uint8_max    = limits<uint8>::max();
		constexpr short const   short_min    = limits<short>::min();
		constexpr short const   short_max    = limits<short>::max();
		constexpr uint16 const  uint16_min   = limits<uint16>::min();
		constexpr uint16 const  uint16_max   = limits<uint16>::max();
		constexpr int const     int_min      = limits<int>::min();
		constexpr int const     int_max      = limits<int>::max();
		constexpr uint const    uint_min     = limits<uint>::min();
		constexpr uint const    uint_max     = limits<uint>::max();
		constexpr int64 const   int64_min    = limits<int64>::min();
		constexpr int64 const   int64_max    = limits<int64>::max();
		constexpr uint64 const  uint64_min   = limits<uint64>::min();
		constexpr uint64 const  uint64_max   = limits<uint64>::max();
		constexpr float const   float_lowest = limits<float>::lowest();
		constexpr float const   float_min    = limits<float>::min();
		constexpr float const   float_max    = limits<float>::max();
		constexpr float const   float_eps    = limits<float>::epsilon();
		constexpr double const  double_min   = limits<double>::min();
		constexpr double const  double_max   = limits<double>::max();
		constexpr double const  double_eps   = limits<double>::epsilon();

		constexpr float const   float_inf    = limits<float>::infinity();
		constexpr double const  double_inf   = limits<double>::infinity();

		constexpr float const   float_nan    = limits<float>::quiet_NaN();
		constexpr double const  double_nan   = limits<double>::quiet_NaN();

		// The maximum integer value that can be exactly represented by a float,double
		constexpr int const max_int_in_float    = 16777216;           // 2^24
		constexpr int64 const max_int_in_double = 9007199254740994LL; // 2^53
		static_assert(int(float(max_int_in_float    )) == max_int_in_float    , "");
		static_assert(int(float(max_int_in_float + 1)) != max_int_in_float + 1, "");
		static_assert(int64(double(max_int_in_double    )) == max_int_in_double    , "");
		static_assert(int64(double(max_int_in_double + 1)) != max_int_in_double + 1, "");
	}
}

