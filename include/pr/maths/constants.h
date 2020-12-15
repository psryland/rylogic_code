//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"

namespace pr::maths
{
	// Careful!, these are global variables and therefore
	// do not have a defined construction order. If you're using
	// them in other global objects they mightn't be initialised
	constexpr double tinyd = double(1.0000000000000002e-12);
	constexpr double tiny_sqd = double(1.0000000000000003e-24);
	constexpr double tiny_sqrtd = double(1.0000000000000002e-06);

	constexpr float tinyf = float(1.00000007e-05);
	constexpr float tiny_sqf = float(1.00000015e-10);
	constexpr float tiny_sqrtf = float(3.16227786e-03);

	template <typename T> constexpr T tiny;
	template <> constexpr double tiny<double> = tinyd;
	template <> constexpr float tiny<float> = tinyf;

	constexpr double tau = double(6.283185307179586476925286766559); // circle constant
	constexpr double inv_tau = 1.0 / tau;
	constexpr double tau_by_2 = tau / 2.0;
	constexpr double tau_by_4 = tau / 4.0;
	constexpr double tau_by_6 = tau / 6.0;
	constexpr double tau_by_8 = tau / 8.0;
	constexpr double tau_by_360 = tau / 360.0;
	constexpr double E60_by_tau = 360.0 / tau;
	constexpr double root2 = double(1.4142135623730950488);
	constexpr double root3 = double(1.7320508075688772935274463415059);
	constexpr double root5 = double(2.23606797749978969641);
	constexpr double root3_by_2 = root3 / 2.0; // 0.86602540378443864676
	constexpr double inv_root2 = 1.0 / root2;
	constexpr double inv_root3 = 1.0 / root3;
	constexpr double golden_ratio = double(1.618033988749894848204586834);    // "Golden Ratio" = (1 + root5) / 2
	constexpr double golden_angle = double(2.39996322972865332223);  // (rad) "Golden Angle" = pi*(3-root(5)) = the angle the divides the circumference of a circle with ratio equal to the golden ratio

	constexpr float tauf = float(6.283185307179586476925286766559);    // circle constant
	constexpr float inv_tauf = 1.0f / tauf;
	constexpr float tau_by_2f = tauf / 2.0f;
	constexpr float tau_by_4f = tauf / 4.0f;
	constexpr float tau_by_6f = tauf / 6.0f;
	constexpr float tau_by_8f = tauf / 8.0f;
	constexpr float tau_by_360f = tauf / 360.0f;
	constexpr float E60_by_tauf = 360.0f / tauf;
	constexpr float root2f = float(1.4142135623730950488);
	constexpr float root3f = float(1.7320508075688772935274463415059);
	constexpr float root5f = float(2.23606797749978969641);
	constexpr float root3_by_2f = root3f / 2.0f;
	constexpr float inv_root2f = 1.0f / root2f;
	constexpr float inv_root3f = 1.0f / root3f;
	constexpr float golden_ratiof = float(1.618033988749894848204586834);  // "Golden Ratio"
	constexpr float golden_anglef = float(2.39996322972865332223); //(rad) "Golden Angle" = pi*(3-root(5)) = the angle the divides the circumference of a circle with ratio equal to the golden ratio

	constexpr double cos_30 = root3_by_2;
	constexpr double cos_45 = inv_root2;
	constexpr double cos_60 = 0.5;
	constexpr double sin_30 = cos_60;
	constexpr double sin_45 = cos_45;
	constexpr double sin_60 = cos_30;

	constexpr float cos_30f = root3_by_2f;
	constexpr float cos_45f = inv_root2f;
	constexpr float cos_60f = 0.5f;
	constexpr float sin_30f = cos_60f;
	constexpr float sin_45f = cos_45f;
	constexpr float sin_60f = cos_30f;
}
namespace pr
{
	template <typename Type> using limits = std::numeric_limits<Type>;
	namespace maths
	{
		constexpr char    char_min     = limits<char>::min();
		constexpr char    char_max     = limits<char>::max();
		constexpr uint8   uint8_min    = limits<uint8>::min();
		constexpr uint8   uint8_max    = limits<uint8>::max();
		constexpr short   short_min    = limits<short>::min();
		constexpr short   short_max    = limits<short>::max();
		constexpr uint16  uint16_min   = limits<uint16>::min();
		constexpr uint16  uint16_max   = limits<uint16>::max();
		constexpr int     int_min      = limits<int>::min();
		constexpr int     int_max      = limits<int>::max();
		constexpr uint    uint_min     = limits<uint>::min();
		constexpr uint    uint_max     = limits<uint>::max();
		constexpr int64   int64_min    = limits<int64>::min();
		constexpr int64   int64_max    = limits<int64>::max();
		constexpr uint64  uint64_min   = limits<uint64>::min();
		constexpr uint64  uint64_max   = limits<uint64>::max();
		constexpr float   float_lowest = limits<float>::lowest();
		constexpr float   float_min    = limits<float>::min();
		constexpr float   float_max    = limits<float>::max();
		constexpr float   float_eps    = limits<float>::epsilon();
		constexpr double  double_min   = limits<double>::min();
		constexpr double  double_max   = limits<double>::max();
		constexpr double  double_eps   = limits<double>::epsilon();
		constexpr float   float_inf    = limits<float>::infinity();
		constexpr double  double_inf   = limits<double>::infinity();
		constexpr float   float_nan    = limits<float>::quiet_NaN();
		constexpr double  double_nan   = limits<double>::quiet_NaN();

		// The maximum integer value that can be exactly represented by a float,double
		constexpr int max_int_in_float    = 16777216;           // 2^24
		constexpr int64 max_int_in_double = 9007199254740994LL; // 2^53
		static_assert(int(float(max_int_in_float    )) == max_int_in_float    , "");
		static_assert(int(float(max_int_in_float + 1)) != max_int_in_float + 1, "");
		static_assert(int64(double(max_int_in_double    )) == max_int_in_double    , "");
		static_assert(int64(double(max_int_in_double + 1)) != max_int_in_double + 1, "");
	}
}

