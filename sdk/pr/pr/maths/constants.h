//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#ifndef PR_MATHS_CONSTANTS_H
#define PR_MATHS_CONSTANTS_H

#ifndef NOMINMAX
#error "NOMINMAX must be defined before including maths library headers"
#endif

#include "pr/maths/forward.h"

namespace pr
{
	namespace maths
	{
		// Careful!, these are global variables and therefore
		// do not have a defined construction order. If you're using
		// them in other global objects they mightn't be initialised
		float const tiny       = 1.000000e-4F; // Can't go lower than this cos DX uses less precision
		float const phi        = 1.618034e+0F; // "Golden Ratio"
		float const tau        = 6.283185e+0F; // circle constant
		float const inv_tau    = 1.591549e-1F;
		float const tau_by_2   = 3.141593e+0F;
		float const tau_by_4   = 1.570796e+0F;
		float const tau_by_8   = 7.853982e-1F;
		float const tau_by_360 = 1.745329e-2F;
		float const E60_by_tau = 5.729578e+1F;
		double const dbl_tiny = 1.000000e-12;

		template <typename Type> struct limits {};
		template <> struct limits<char>
		{
			static char min() { return static_cast<char>(-128); }
			static char max() { return static_cast<char>( 127); }
		};
		template <> struct limits<uint8>
		{
			static uint8 min() { return static_cast<uint8>(0x00U); }
			static uint8 max() { return static_cast<uint8>(0xffU); }
		};
		template <> struct limits<short>
		{
			static short min() { return static_cast<short>(-32768); }
			static short max() { return static_cast<short>( 32767); }
		};
		template <> struct limits<uint16>
		{
			static uint16 min() { return static_cast<uint16>(0x0000U); }
			static uint16 max() { return static_cast<uint16>(0xffffU); }
		};
		template <> struct limits<int>
		{
			static int min() { return 0x80000000; }
			static int max() { return 0x7fffffff; }
		};
		template <> struct limits<uint>
		{
			static uint min() { return 0x00000000U; }
			static uint max() { return 0xffffffffU; }
		};
		template <> struct limits<int64>
		{
			static int64 min() { return 0x8000000000000000LL; }
			static int64 max() { return 0x7fffffffffffffffLL; }
		};
		template <> struct limits<uint64>
		{
			static uint64 min() { return 0x0000000000000000ULL; }
			static uint64 max() { return 0xffffffffffffffffULL; }
		};
		template <> struct limits<float>
		{
			static float min() { return 1.175494351e-38F; }
			static float max() { return 3.402823466e+38F; }
		};
		template <> struct limits<double>
		{
			static double min() { return 2.2250738585072014e-308; }
			static double max() { return 1.7976931348623158e+308; }
		};
		
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
		double const  double_min   = limits<double>::min();
		double const  double_max   = limits<double>::max();
		
		template <typename Type> inline Type min()  { return limits<Type>::min(); }
		template <typename Type> inline Type max()  { return limits<Type>::max(); }
		template <typename Type> inline Type zero() { return static_cast<const Type&>(0); }
		template <typename Type> inline Type unit() { return static_cast<const Type&>(1); }
	}
}

#endif
