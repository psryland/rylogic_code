//*****************************************************************************************
// min_max_fix.h
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************
// Templated inline functions to replace Windows defined
// min/max macros which interfere with the maths library.
// Based on the boost version of this fix
#pragma once
#ifndef PR_COMMON_MIN_MAX_MACRO_FIX_H
#define PR_COMMON_MIN_MAX_MACRO_FIX_H

// Technically, this fix should be made when we're using Microsoft headers.
// However, there's no (easy) way of detecting that, so we'll just disable it
// when compiling with VC.  We can always fix it on a case-by-case basis if
// this ever becomes an actual problem.
#ifdef _MSC_VER

	// disable min/max macros:
	#ifdef min
	#  undef min
	#endif
	#ifdef max
	#  undef max
	#endif
	#ifndef NOMINMAX
	#  define NOMINMAX
	#endif

	#ifdef _WIN32

		#include <algorithm> // for existing std::min and std::max
		namespace std
		{
			// Apparently, something in the Microsoft libraries requires the "long"
			// overload, because it calls the min/max functions with arguments of
			// slightly different type.
			inline long min(long a, long b) { return b < a ? b : a; }
			inline long max(long a, long b) { return a < b ? b : a; }

			// The "long double" overload is required, otherwise user code calling
			// min/max for floating-point numbers will use the "long" overload.
			// (SourceForge bug #495495)
			inline long double min(long double a, long double b) { return b < a ? b : a; }
			inline long double max(long double a, long double b) { return a < b ? b : a; }
		}
		using std::min;
		using std::max;

	#endif

#endif

#endif
