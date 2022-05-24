//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
// Careful!, these are global variables and therefore
// do not have a defined construction order. If you're using
// them in other global objects they mightn't be initialised
#pragma once
#include "pr/maths/forward.h"

namespace pr
{
	// Alias the numeric_limits to a shorter name
	template <typename Type> using limits = std::numeric_limits<Type>;

	// Mathematical constants.
	template <typename Type> struct constants {};
	template <> struct constants<double>
	{
		static constexpr double tiny = 1.0000000000000002e-12;
		static constexpr double tiny_sq = 1.0000000000000003e-24;
		static constexpr double tiny_sqrt = 1.0000000000000002e-06;

		static constexpr double tau = 6.283185307179586476925286766559; // circle constant
		static constexpr double inv_tau = 1.0 / tau;
		static constexpr double tau_by_2 = tau / 2.0;
		static constexpr double tau_by_4 = tau / 4.0;
		static constexpr double tau_by_6 = tau / 6.0;
		static constexpr double tau_by_8 = tau / 8.0;
		static constexpr double tau_by_360 = tau / 360.0;
		static constexpr double E60_by_tau = 360.0 / tau;
		static constexpr double root2 = 1.4142135623730950488;
		static constexpr double root3 = 1.7320508075688772935274463415059;
		static constexpr double root5 = 2.23606797749978969641;
		static constexpr double root3_by_2 = root3 / 2.0; // 0.86602540378443864676
		static constexpr double inv_root2 = 1.0 / root2;
		static constexpr double inv_root3 = 1.0 / root3;
		static constexpr double golden_ratio = 1.618033988749894848204586834;    // "Golden Ratio" = (1 + root5) / 2
		static constexpr double golden_angle = 2.39996322972865332223;  // (rad) "Golden Angle" = pi*(3-root(5)) = the angle the divides the circumference of a circle with ratio equal to the golden ratio

		// Trig
		static constexpr double cos_30 = root3_by_2;
		static constexpr double cos_45 = inv_root2;
		static constexpr double cos_60 = 0.5;
		static constexpr double sin_30 = cos_60;
		static constexpr double sin_45 = cos_45;
		static constexpr double sin_60 = cos_30;

		// The maximum integer value that can be exactly represented by a float,double
		static constexpr int64_t max_representable_int = 9007199254740994LL; // 2^53
	};
	template <> struct constants<float>
	{
		static constexpr float tiny = 1.00000007e-05f;
		static constexpr float tiny_sq = 1.00000015e-10f;
		static constexpr float tiny_sqrt = 3.16227786e-03f;

		static constexpr float tau = 6.283185307179586476925286766559f;    // circle constant
		static constexpr float inv_tau = 1.0f / tau;
		static constexpr float tau_by_2 = tau / 2.0f;
		static constexpr float tau_by_4 = tau / 4.0f;
		static constexpr float tau_by_6 = tau / 6.0f;
		static constexpr float tau_by_8 = tau / 8.0f;
		static constexpr float tau_by_360 = tau / 360.0f;
		static constexpr float E60_by_tau = 360.0f / tau;
		static constexpr float root2 = 1.4142135623730950488f;
		static constexpr float root3 = 1.7320508075688772935274463415059f;
		static constexpr float root5 = 2.23606797749978969641f;
		static constexpr float root3_by_2 = root3 / 2.0f;
		static constexpr float inv_root2 = 1.0f / root2;
		static constexpr float inv_root3 = 1.0f / root3;
		static constexpr float golden_ratio = 1.618033988749894848204586834f;  // "Golden Ratio"
		static constexpr float golden_angle = 2.39996322972865332223f; //(rad) "Golden Angle" = pi*(3-root(5)) = the angle the divides the circumference of a circle with ratio equal to the golden ratio

		// Trig
		static constexpr float cos_30 = root3_by_2;
		static constexpr float cos_45 = inv_root2;
		static constexpr float cos_60 = 0.5f;
		static constexpr float sin_30 = cos_60;
		static constexpr float sin_45 = cos_45;
		static constexpr float sin_60 = cos_30;

		// The maximum integer value that can be exactly represented by a float,double
		static constexpr int32_t max_representable_int = 16777216; // 2^24
	};
}
namespace pr::maths
{
	template <typename T> constexpr std::decay_t<T> tiny;
	template <> constexpr double  tiny<double const>  = constants<double>::tiny;
	template <> constexpr float   tiny<float const>   = constants<float>::tiny;
	template <> constexpr int64_t tiny<int64_t const> = 0LL;
	template <> constexpr int32_t tiny<int32_t const> = 0;
	template <> constexpr double  tiny<double>        = constants<double>::tiny;
	template <> constexpr float   tiny<float>         = constants<float>::tiny;
	template <> constexpr int64_t tiny<int64_t>       = 0LL;
	template <> constexpr int32_t tiny<int32_t>       = 0;

	constexpr double tinyd = constants<double>::tiny;
	constexpr double tiny_sqd = constants<double>::tiny_sq;
	constexpr double tiny_sqrtd = constants<double>::tiny_sqrt;

	constexpr float tinyf = constants<float>::tiny;
	constexpr float tiny_sqf = constants<float>::tiny_sq;
	constexpr float tiny_sqrtf = constants<float>::tiny_sqrt;

	constexpr double tau          = constants<double>::tau;
	constexpr double inv_tau      = constants<double>::inv_tau;
	constexpr double tau_by_2     = constants<double>::tau_by_2;
	constexpr double tau_by_4     = constants<double>::tau_by_4;
	constexpr double tau_by_6     = constants<double>::tau_by_6;
	constexpr double tau_by_8     = constants<double>::tau_by_8;
	constexpr double tau_by_360   = constants<double>::tau_by_360;
	constexpr double E60_by_tau   = constants<double>::E60_by_tau;
	constexpr double root2        = constants<double>::root2;
	constexpr double root3        = constants<double>::root3;
	constexpr double root5        = constants<double>::root5;
	constexpr double root3_by_2   = constants<double>::root3_by_2;
	constexpr double inv_root2    = constants<double>::inv_root2;
	constexpr double inv_root3    = constants<double>::inv_root3;
	constexpr double golden_ratio = constants<double>::golden_ratio;
	constexpr double golden_angle = constants<double>::golden_angle;

	constexpr float tauf          = constants<float>::tau;
	constexpr float inv_tauf      = constants<float>::inv_tau;
	constexpr float tau_by_2f     = constants<float>::tau_by_2;
	constexpr float tau_by_4f     = constants<float>::tau_by_4;
	constexpr float tau_by_6f     = constants<float>::tau_by_6;
	constexpr float tau_by_8f     = constants<float>::tau_by_8;
	constexpr float tau_by_360f   = constants<float>::tau_by_360;
	constexpr float E60_by_tauf   = constants<float>::E60_by_tau;
	constexpr float root2f        = constants<float>::root2;
	constexpr float root3f        = constants<float>::root3;
	constexpr float root5f        = constants<float>::root5;
	constexpr float root3_by_2f   = constants<float>::root3_by_2;
	constexpr float inv_root2f    = constants<float>::inv_root2;
	constexpr float inv_root3f    = constants<float>::inv_root3;
	constexpr float golden_ratiof = constants<float>::golden_ratio;
	constexpr float golden_anglef = constants<float>::golden_angle;

	constexpr double cos_30 = constants<double>::cos_30;
	constexpr double cos_45 = constants<double>::cos_45;
	constexpr double cos_60 = constants<double>::cos_60;
	constexpr double sin_30 = constants<double>::sin_30;
	constexpr double sin_45 = constants<double>::sin_45;
	constexpr double sin_60 = constants<double>::sin_60;

	constexpr float cos_30f = constants<float>::cos_30;
	constexpr float cos_45f = constants<float>::cos_45;
	constexpr float cos_60f = constants<float>::cos_60;
	constexpr float sin_30f = constants<float>::sin_30;
	constexpr float sin_45f = constants<float>::sin_45;
	constexpr float sin_60f = constants<float>::sin_60;

	// Convenient constants
	constexpr int8_t   int8_min     = limits<int8_t>::min();
	constexpr int8_t   int8_max     = limits<int8_t>::max();
	constexpr uint8_t  uint8_min    = limits<uint8_t>::min();
	constexpr uint8_t  uint8_max    = limits<uint8_t>::max();
	constexpr int16_t  int16_min    = limits<int16_t>::min();
	constexpr int16_t  int16_max    = limits<int16_t>::max();
	constexpr uint16_t uint16_min   = limits<uint16_t>::min();
	constexpr uint16_t uint16_max   = limits<uint16_t>::max();
	constexpr int32_t  int32_min    = limits<int32_t>::min();
	constexpr int32_t  int32_max    = limits<int32_t>::max();
	constexpr uint32_t uint32_min   = limits<uint32_t>::min();
	constexpr uint32_t uint32_max   = limits<uint32_t>::max();
	constexpr int64_t  int64_min    = limits<int64_t>::min();
	constexpr int64_t  int64_max    = limits<int64_t>::max();
	constexpr uint64_t uint64_min   = limits<uint64_t>::min();
	constexpr uint64_t uint64_max   = limits<uint64_t>::max();
	constexpr float    float_lowest = limits<float>::lowest();
	constexpr float    float_min    = limits<float>::min();
	constexpr float    float_max    = limits<float>::max();
	constexpr float    float_eps    = limits<float>::epsilon();
	constexpr double   double_min   = limits<double>::min();
	constexpr double   double_max   = limits<double>::max();
	constexpr double   double_eps   = limits<double>::epsilon();
	constexpr float    float_inf    = limits<float>::infinity();
	constexpr double   double_inf   = limits<double>::infinity();
	constexpr float    float_nan    = limits<float>::quiet_NaN();
	constexpr double   double_nan   = limits<double>::quiet_NaN();

	// The maximum integer value that can be exactly represented by a float,double
	constexpr int32_t max_int_in_float  = 16777216;           // 2^24
	constexpr int64_t max_int_in_double = 9007199254740994LL; // 2^53
	static_assert(static_cast<int32_t>(static_cast<float >(max_int_in_float     )) == max_int_in_float    );
	static_assert(static_cast<int32_t>(static_cast<float >(max_int_in_float  + 1)) != max_int_in_float + 1);
	static_assert(static_cast<int64_t>(static_cast<double>(max_int_in_double    )) == max_int_in_double    );
	static_assert(static_cast<int64_t>(static_cast<double>(max_int_in_double + 1)) != max_int_in_double + 1);
}
