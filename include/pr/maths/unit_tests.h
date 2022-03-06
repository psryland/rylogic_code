//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#if PR_UNITTESTS
#include <random>
#include "pr/common/unittests.h"
#include "pr/maths/maths.h"
namespace pr::maths
{
	PRUnitTest(MathsCoreTests)
	{
		{// Permutations
			auto eql = [](int const* arr, int a, int b, int c, int d)
			{
				return arr[0] == a && arr[1] == b && arr[2] == c && arr[3] == d;
			};
			{// 4-sequential
				int arr1[] = {1, 2, 3, 4};
				PR_CHECK(PermutationFirst(arr1, _countof(arr1)) && eql(arr1, 1, 2, 3, 4), true);//0
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 1, 2, 4, 3), true);//1
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 1, 3, 2, 4), true);//2
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 1, 3, 4, 2), true);//3
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 1, 4, 2, 3), true);//4
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 1, 4, 3, 2), true);//5
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 2, 1, 3, 4), true);//6
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 2, 1, 4, 3), true);//7
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 2, 3, 1, 4), true);//8
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 2, 3, 4, 1), true);//9
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 2, 4, 1, 3), true);//10
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 2, 4, 3, 1), true);//11
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 3, 1, 2, 4), true);//12
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 3, 1, 4, 2), true);//13
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 3, 2, 1, 4), true);//14
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 3, 2, 4, 1), true);//15
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 3, 4, 1, 2), true);//16
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 3, 4, 2, 1), true);//17
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 4, 1, 2, 3), true);//18
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 4, 1, 3, 2), true);//19
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 4, 2, 1, 3), true);//20
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 4, 2, 3, 1), true);//21
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 4, 3, 1, 2), true);//22
				PR_CHECK(PermutationNext(arr1, _countof(arr1)) && eql(arr1, 4, 3, 2, 1), true);//23
				PR_CHECK(PermutationNext(arr1, _countof(arr1)), false);//24
			}
			{// non-sequential
				int i, arr2[] = {-1, 4, 11, 20};
				for (i = 1; i != 24; ++i)//== 4!
				{
					PR_CHECK(PermutationNext(arr2, _countof(arr2)), true);
					if (i == 6) PR_CHECK(eql(arr2, 4, -1, 11, 20), true);
					if (i == 13) PR_CHECK(eql(arr2, 11, -1, 20, 4), true);
				}
				PR_CHECK(PermutationNext(arr2, _countof(arr2)), false);
			}
			{// large number of permutations
				int i, arr3[] = {-10, -9, -8, -1, 0, +1, +3, +6, +9};
				for (i = 1; PermutationNext(arr3, _countof(arr3)); ++i) {}
				PR_CHECK(i == 362880, true); // == 9!
			}
		}
		{// Floating point compare
			float const _6dp = 1.000000111e-6f;

			// Regular large numbers - generally not problematic
			PR_CHECK(FEqlRelative(1000000.0f, 1000001.0f, _6dp), true);
			PR_CHECK(FEqlRelative(1000001.0f, 1000000.0f, _6dp), true);
			PR_CHECK(FEqlRelative(1000000.0f, 1000010.0f, _6dp), false);
			PR_CHECK(FEqlRelative(1000010.0f, 1000000.0f, _6dp), false);

			// Negative large numbers
			PR_CHECK(FEqlRelative(-1000000.0f, -1000001.0f, _6dp), true);
			PR_CHECK(FEqlRelative(-1000001.0f, -1000000.0f, _6dp), true);
			PR_CHECK(FEqlRelative(-1000000.0f, -1000010.0f, _6dp), false);
			PR_CHECK(FEqlRelative(-1000010.0f, -1000000.0f, _6dp), false);

			// Numbers around 1
			PR_CHECK(FEqlRelative(1.0000001f, 1.0000002f, _6dp), true);
			PR_CHECK(FEqlRelative(1.0000002f, 1.0000001f, _6dp), true);
			PR_CHECK(FEqlRelative(1.0000020f, 1.0000010f, _6dp), false);
			PR_CHECK(FEqlRelative(1.0000010f, 1.0000020f, _6dp), false);

			// Numbers around -1
			PR_CHECK(FEqlRelative(-1.0000001f, -1.0000002f, _6dp), true);
			PR_CHECK(FEqlRelative(-1.0000002f, -1.0000001f, _6dp), true);
			PR_CHECK(FEqlRelative(-1.0000010f, -1.0000020f, _6dp), false);
			PR_CHECK(FEqlRelative(-1.0000020f, -1.0000010f, _6dp), false);

			// Numbers between 1 and 0
			PR_CHECK(FEqlRelative(0.000000001000001f, 0.000000001000002f, _6dp), true);
			PR_CHECK(FEqlRelative(0.000000001000002f, 0.000000001000001f, _6dp), true);
			PR_CHECK(FEqlRelative(0.000000000100002f, 0.000000000100001f, _6dp), false);
			PR_CHECK(FEqlRelative(0.000000000100001f, 0.000000000100002f, _6dp), false);

			// Numbers between -1 and 0
			PR_CHECK(FEqlRelative(-0.0000000010000001f, -0.0000000010000002f, _6dp), true);
			PR_CHECK(FEqlRelative(-0.0000000010000002f, -0.0000000010000001f, _6dp), true);
			PR_CHECK(FEqlRelative(-0.0000000001000002f, -0.0000000001000001f, _6dp), false);
			PR_CHECK(FEqlRelative(-0.0000000001000001f, -0.0000000001000002f, _6dp), false);

			// Comparisons involving zero
			PR_CHECK(FEqlRelative(+0.0f, +0.0f, _6dp), true);
			PR_CHECK(FEqlRelative(+0.0f, -0.0f, _6dp), true);
			PR_CHECK(FEqlRelative(-0.0f, -0.0f, _6dp), true);
			PR_CHECK(FEqlRelative(+0.000001f, +0.0f, _6dp), true);
			PR_CHECK(FEqlRelative(+0.0f, +0.000001f, _6dp), true);
			PR_CHECK(FEqlRelative(-0.000001f, +0.0f, _6dp), true);
			PR_CHECK(FEqlRelative(+0.0f, -0.000001f, _6dp), true);
			PR_CHECK(FEqlRelative(+0.00001f, +0.0f, _6dp), false);
			PR_CHECK(FEqlRelative(+0.0f, +0.00001f, _6dp), false);
			PR_CHECK(FEqlRelative(-0.00001f, +0.0f, _6dp), false);
			PR_CHECK(FEqlRelative(+0.0f, -0.00001f, _6dp), false);

			// Comparisons involving extreme values (overflow potential)
			auto float_hi = maths::float_max;
			auto float_lo = maths::float_lowest;
			PR_CHECK(FEqlRelative(float_hi, float_hi, _6dp), true);
			PR_CHECK(FEqlRelative(float_hi, float_lo, _6dp), false);
			PR_CHECK(FEqlRelative(float_lo, float_hi, _6dp), false);
			PR_CHECK(FEqlRelative(float_lo, float_lo, _6dp), true);
			PR_CHECK(FEqlRelative(float_hi, float_hi / 2, _6dp), false);
			PR_CHECK(FEqlRelative(float_hi, float_lo / 2, _6dp), false);
			PR_CHECK(FEqlRelative(float_lo, float_hi / 2, _6dp), false);
			PR_CHECK(FEqlRelative(float_lo, float_lo / 2, _6dp), false);

			// Comparisons involving infinities
			PR_CHECK(FEqlRelative(+maths::float_inf, +maths::float_inf, _6dp), true);
			PR_CHECK(FEqlRelative(-maths::float_inf, -maths::float_inf, _6dp), true);
			PR_CHECK(FEqlRelative(-maths::float_inf, +maths::float_inf, _6dp), false);
			PR_CHECK(FEqlRelative(+maths::float_inf, +maths::float_max, _6dp), false);
			PR_CHECK(FEqlRelative(-maths::float_inf, -maths::float_max, _6dp), false);

			// Comparisons involving NaN values
			PR_CHECK(FEqlRelative(maths::float_nan, maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative(maths::float_nan, +0.0f, _6dp), false);
			PR_CHECK(FEqlRelative(-0.0f, maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative(maths::float_nan, -0.0f, _6dp), false);
			PR_CHECK(FEqlRelative(+0.0f, maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative( maths::float_nan, +maths::float_inf, _6dp), false);
			PR_CHECK(FEqlRelative(+maths::float_inf,  maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative( maths::float_nan, -maths::float_inf, _6dp), false);
			PR_CHECK(FEqlRelative(-maths::float_inf,  maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative( maths::float_nan, +maths::float_max, _6dp), false);
			PR_CHECK(FEqlRelative(+maths::float_max,  maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative( maths::float_nan, -maths::float_max, _6dp), false);
			PR_CHECK(FEqlRelative(-maths::float_max,  maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative( maths::float_nan, +maths::float_min, _6dp), false);
			PR_CHECK(FEqlRelative(+maths::float_min,  maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative( maths::float_nan, -maths::float_min, _6dp), false);
			PR_CHECK(FEqlRelative(-maths::float_min,  maths::float_nan, _6dp), false);

			// Comparisons of numbers on opposite sides of 0
			PR_CHECK(FEqlRelative(+1.0f, -1.0f, _6dp), false);
			PR_CHECK(FEqlRelative(-1.0f, +1.0f, _6dp), false);
			PR_CHECK(FEqlRelative(+1.000000001f, -1.0f, _6dp), false);
			PR_CHECK(FEqlRelative(-1.0f, +1.000000001f, _6dp), false);
			PR_CHECK(FEqlRelative(-1.000000001f, +1.0f, _6dp), false);
			PR_CHECK(FEqlRelative(+1.0f, -1.000000001f, _6dp), false);
			PR_CHECK(FEqlRelative(2 * maths::float_min, 0, _6dp), true);
			PR_CHECK(FEqlRelative(maths::float_min, -maths::float_min, _6dp), false);

			// The really tricky part - comparisons of numbers very close to zero.
			PR_CHECK(FEqlRelative(+maths::float_min, +maths::float_min, _6dp), true);
			PR_CHECK(FEqlRelative(+maths::float_min, -maths::float_min, _6dp), false);
			PR_CHECK(FEqlRelative(-maths::float_min, +maths::float_min, _6dp), false);
			PR_CHECK(FEqlRelative(+maths::float_min, 0, _6dp), true);
			PR_CHECK(FEqlRelative(0, +maths::float_min, _6dp), true);
			PR_CHECK(FEqlRelative(-maths::float_min, 0, _6dp), true);
			PR_CHECK(FEqlRelative(0, -maths::float_min, _6dp), true);

			PR_CHECK(FEqlRelative(0.000000001f, -maths::float_min, _6dp), false);
			PR_CHECK(FEqlRelative(0.000000001f, +maths::float_min, _6dp), false);
			PR_CHECK(FEqlRelative(+maths::float_min, 0.000000001f, _6dp), false);
			PR_CHECK(FEqlRelative(-maths::float_min, 0.000000001f, _6dp), false);
		}
		{// Floating point vector compare
			float arr0[] = {1,2,3,4};
			float arr1[] = {1,2,3,5};
			static_assert(maths::is_vec<decltype(arr0)>::value, "");
			static_assert(maths::is_vec<decltype(arr1)>::value, "");

			PR_CHECK(!Equal(arr0, arr1), true);
		}
		{// FEql arrays
			auto t0 = 0.0f;
			auto t1 = maths::tinyf * 0.5f;
			auto t2 = maths::tinyf * 1.5f;
			float arr0[] = {t0, 0, maths::tinyf, -1};
			float arr1[] = {t1, 0, maths::tinyf, -1};
			float arr2[] = {t2, 0, maths::tinyf, -1};

			PR_CHECK(FEql(arr0, arr1), true ); // Different by 1.000005%
			PR_CHECK(FEql(arr0, arr2), false); // Different by 1.000015%
		}
		{// Finite
			volatile auto f0 = 0.0f;
			volatile auto d0 = 0.0;
			PR_CHECK( IsFinite(1.0f), true);
			PR_CHECK( IsFinite(limits<int>::max()), true);
			PR_CHECK(!IsFinite(1.0f/f0), true);
			PR_CHECK(!IsFinite(0.0/d0), true);
			PR_CHECK(!IsFinite(11, 10), true);

			v4 arr0(0.0f, 1.0f, 10.0f, 1.0f);
			v4 arr1(0.0f, 1.0f, 1.0f/f0, 0.0f/f0);
			PR_CHECK( IsFinite(arr0), true);
			PR_CHECK(!IsFinite(arr1), true);
			PR_CHECK(!All(arr0, [](float x){ return x < 5.0f; }), true);
			PR_CHECK( Any(arr0, [](float x){ return x < 5.0f; }), true);

			m4x4 arr2(arr0,arr0,arr0,arr0);
			m4x4 arr3(arr1,arr1,arr1,arr1);
			PR_CHECK( IsFinite(arr2), true);
			PR_CHECK(!IsFinite(arr3), true);
			PR_CHECK(!All(arr2, [](float x){ return x < 5.0f; }), true);
			PR_CHECK( Any(arr2, [](float x){ return x < 5.0f; }), true);

			iv2 arr4(10,1);
			PR_CHECK( IsFinite(arr4), true);
			PR_CHECK(!All(arr4, [](int x){ return x < 5; }), true);
			PR_CHECK( Any(arr4, [](int x){ return x < 5; }), true);
		}
		{// Abs
			PR_CHECK(Abs(-1.0f) == Abs(-1.0f), true);
			PR_CHECK(Abs(-1.0f) == Abs(+1.0f), true);
			PR_CHECK(Abs(+1.0f) == Abs(+1.0f), true);

			v4 arr0 = {+1,-2,+3,-4};
			v4 arr1 = {-1,+2,-3,+4};
			v4 arr2 = {+1,+2,+3,+4};
			PR_CHECK(Abs(arr0) == Abs(arr1), true);
			PR_CHECK(Abs(arr0) == Abs(arr2), true);
			PR_CHECK(Abs(arr1) == Abs(arr2), true);

			float arr3[] = {+1,-2,+3,-4};
			float arr4[] = {+1,+2,+3,+4};
			auto arr5 = Abs(arr3);
			std::span<float const> span0(arr5);
			std::span<float const> span1(arr4);
			PR_CHECK(FEql(span0, span1), true);

			std::array<float, 5> const arr6 = { 1, 2, 3, 4, 5 };
			std::span<float const> span5(arr6);
		}
		{// Truncate
			v4 arr0 = {+1.1f, -1.2f, +2.8f, -2.9f};
			v4 arr1 = {+1.0f, -1.0f, +2.0f, -2.0f};
			v4 arr2 = {+1.0f, -1.0f, +3.0f, -3.0f};
			v4 arr3 = {+0.1f, -0.2f, +0.8f, -0.9f};

			PR_CHECK(Trunc(1.9f) == 1.0f, true);
			PR_CHECK(Trunc(10000000000000.9) == 10000000000000.0, true);
			PR_CHECK(Trunc(arr0, ETruncType::TowardZero) == arr1, true);
			PR_CHECK(Trunc(arr0, ETruncType::ToNearest ) == arr2, true);
			PR_CHECK(FEql(Frac(arr0), arr3), true);
		}
		{// Any/All
			float arr0[] = {1.0f, 2.0f, 0.0f, -4.0f};
			auto are_zero = [](float x) { return x == 0.0f; };
			auto not_zero = [](float x) { return x != 0.0f; };

			PR_CHECK(!All(arr0, are_zero), true);
			PR_CHECK(!All(arr0, not_zero), true);
			PR_CHECK( Any(arr0, not_zero), true);
			PR_CHECK( Any(arr0, are_zero), true);
		}
		{// Lengths
			PR_CHECK(LenSq(3,4) == 25, true);
			PR_CHECK(LenSq(3,4,5) == 50, true);
			PR_CHECK(LenSq(3,4,5,6) == 86, true);
			PR_CHECK(FEql(Len(3,4), 5.0f), true);
			PR_CHECK(FEql(Len(3,4,5), 7.0710678f), true);
			PR_CHECK(FEql(Len(3,4,5,6), 9.2736185f), true);

			auto arr0 = v4(3,4,5,6);
			PR_CHECK(FEql(Length(arr0.xy), 5.0f), true);
			PR_CHECK(FEql(Length(arr0.xyz), 7.0710678f), true);
			PR_CHECK(FEql(Length(arr0), 9.2736185f), true);
		}
		{// Min/Max/Clamp
			PR_CHECK(Min(1,2,-3,4,-5) == -5, true);
			PR_CHECK(Max(1,2,-3,4,-5) == 4, true);
			PR_CHECK(Clamp(-1,0,10) == 0, true);
			PR_CHECK(Clamp(3,0,10) == 3, true);
			PR_CHECK(Clamp(12,0,10) == 10, true);

			auto arr0 = v4(+1,-2,+3,-4);
			auto arr1 = v4(-1,+2,-3,+4);
			auto arr2 = v4(+0,+0,+0,+0);
			auto arr3 = v4(+2,+2,+2,+2);
			PR_CHECK(Min(arr0, arr1, arr2, arr3) == v4(-1,-2,-3,-4), true);
			PR_CHECK(Max(arr0, arr1, arr2, arr3) == v4(+2,+2,+3,+4), true);
			PR_CHECK(Clamp(arr0, arr2, arr3) == v4(+1,+0,+2,+0), true);
		}
		{// Operators
			auto arr0 = v4(+1,-2,+3,-4);
			auto arr1 = v4(-1,+2,-3,+4);
			PR_CHECK((arr0 == arr1) == !(arr0 != arr1), true);
			PR_CHECK((arr0 != arr1) == !(arr0 == arr1), true);
			PR_CHECK((arr0 <  arr1) == !(arr0 >= arr1), true);
			PR_CHECK((arr0 >  arr1) == !(arr0 <= arr1), true);
			PR_CHECK((arr0 <= arr1) == !(arr0 >  arr1), true);
			PR_CHECK((arr0 >= arr1) == !(arr0 <  arr1), true);

			auto arr2 = v4(+3,+4,+5,+6);
			auto arr3 = v4(+1,+2,+3,+4);
			PR_CHECK(FEql(arr2 + arr3, v4(4,6,8,10)), true);
			PR_CHECK(FEql(arr2 - arr3, v4(2,2,2,2)), true);
			PR_CHECK(FEql(arr2 * 2.0f, v4(6,8,10,12)), true);
			PR_CHECK(FEql(2.0f * arr2, v4(6,8,10,12)), true);
			PR_CHECK(FEql(arr2 / 2.0f, v4(1.5f,2,2.5f,3)), true);
			PR_CHECK(FEql(arr2 % 3.0f, v4(0,1,2,0)), true);
		}
		{// Normalise
			auto arr0 = v4(1,2,3,4);
			PR_CHECK(FEql(Normalise(v4Zero, arr0), arr0), true);
			PR_CHECK(FEql(Normalise(arr0), v4(0.1825742f, 0.3651484f, 0.5477226f, 0.7302967f)), true);

			auto arr1 = v2(1,2);
			PR_CHECK(FEql(Normalise(v2Zero, arr1), arr1), true);
			PR_CHECK(FEql(Normalise(arr1), v2(0.4472136f, 0.8944272f)), true);

			PR_CHECK(IsNormal(Normalise(arr0)), true);
		}
		{// Smallest/Largest element
			int arr0[] = {1,2,3,4,5};
			int arr1[] = {2,1,3,4,5};
			int arr2[] = {2,3,1,4,5};
			int arr3[] = {2,3,4,1,5};
			int arr4[] = {2,3,4,5,1};
			static_assert(std::is_same<maths::is_vec<int[5]>::elem_type, int>::value, "");

			PR_CHECK(MinElement(arr0 ) == 1, true);
			PR_CHECK(MinElement(arr1 ) == 1, true);
			PR_CHECK(MinElement(arr2 ) == 1, true);
			PR_CHECK(MinElement(arr3 ) == 1, true);
			PR_CHECK(MinElement(arr4 ) == 1, true);

			float arr5[] = {1,2,3,4,5};
			float arr6[] = {1,2,3,5,4};
			float arr7[] = {2,3,5,1,4};
			float arr8[] = {2,5,3,4,1};
			float arr9[] = {5,2,3,4,1};
			PR_CHECK(MaxElement(arr5) == 5, true);
			PR_CHECK(MaxElement(arr6) == 5, true);
			PR_CHECK(MaxElement(arr7) == 5, true);
			PR_CHECK(MaxElement(arr8) == 5, true);
			PR_CHECK(MaxElement(arr9) == 5, true);
		}
		{// Smallest/Largest element index
			int arr0[] = {1,2,3,4,5};
			int arr1[] = {2,1,3,4,5};
			int arr2[] = {2,3,1,4,5};
			int arr3[] = {2,3,4,1,5};
			int arr4[] = {2,3,4,5,1};

			PR_CHECK(MinElementIndex(arr0) == 0, true);
			PR_CHECK(MinElementIndex(arr1) == 1, true);
			PR_CHECK(MinElementIndex(arr2) == 2, true);
			PR_CHECK(MinElementIndex(arr3) == 3, true);
			PR_CHECK(MinElementIndex(arr4) == 4, true);

			float arr5[] = {1,2,3,4,5};
			float arr6[] = {1,2,3,5,4};
			float arr7[] = {2,3,5,1,4};
			float arr8[] = {2,5,3,4,1};
			float arr9[] = {5,2,3,4,1};
			PR_CHECK(MaxElementIndex(arr5) == 4, true);
			PR_CHECK(MaxElementIndex(arr6) == 3, true);
			PR_CHECK(MaxElementIndex(arr7) == 2, true);
			PR_CHECK(MaxElementIndex(arr8) == 1, true);
			PR_CHECK(MaxElementIndex(arr9) == 0, true);
		}
		{// Dot
			v3 arr0(1,2,3);
			v3 arr1(2,3,4);
			iv2 arr2(1,2);
			iv2 arr3(3,4);
			quat arr4(4,3,2,1);
			quat arr5(1,2,3,4);
			PR_CHECK(FEql(Dot(arr0, arr1), 20), true);
			PR_CHECK(Dot(arr2, arr3) == 11, true);
			PR_CHECK(Dot(arr4, arr5) == 20, true);
		}
		{// Fraction
			PR_CHECK(FEql(Frac(-5, 2, 5), 7.0f/10.0f), true);
		}
		{// Linear interpolate
			v4 arr0(1,10,100,1000);
			v4 arr1(2,20,200,2000);
			PR_CHECK(FEql(Lerp(arr0, arr1, 0.7f), v4(1.7f, 17, 170, 1700)), true);
		}
		{// Spherical linear interpolate
			PR_CHECK(FEql(Slerp(v4XAxis, 2.0f*v4YAxis, 0.5f), 1.5f*v4::Normal(0.5f,0.5f,0,0)), true);
		}
		{// Quantise
			v4 arr0(1.0f/3.0f, 0.0f, 2.0f, float(maths::tau));
			PR_CHECK(FEql(Quantise(arr0, 1024), v4(0.333f, 0.0f, 2.0f, 6.28222f)), true);
		}
		{// CosAngle
			v2 arr0(1,0);
			v2 arr1(0,1);
			PR_CHECK(FEql(CosAngle(1.0,1.0,maths::root2) - Cos(DegreesToRadians(90.0)), 0), true);
			PR_CHECK(FEql(CosAngle(arr0, arr1)           - Cos(DegreesToRadians(90.0f)), 0), true);
			PR_CHECK(FEql(Angle(1.0,1.0,maths::root2), DegreesToRadians(90.0)), true);
			PR_CHECK(FEql(Angle(arr0, arr1),           DegreesToRadians(90.0f)), true);
			PR_CHECK(FEql(Length(1.0f, 1.0f, DegreesToRadians(90.0f)), float(maths::root2)), true);
		}
		{// Cube Root (32bit)
			auto a = 1.23456789123456789f;
			auto b = Cubert(a * a * a);
			PR_CHECK(FEqlRelative(a,b,0.000001f), true);
		}
		{// Cube Root (64bit)
			auto a = 1.23456789123456789;
			auto b = Cubert(a * a * a);
			PR_CHECK(FEqlRelative(a,b,0.000000000001), true);
		}
		{// Arithmetic sequence
			ArithmeticSequence a(2, 5);
			PR_CHECK(a(), 2);
			PR_CHECK(a(), 7);
			PR_CHECK(a(), 12);
			PR_CHECK(a(), 17);

			PR_CHECK(ArithmeticSum(0, 2, 4), 20);
			PR_CHECK(ArithmeticSum(4, 2, 2), 18);
			PR_CHECK(ArithmeticSum(1, 2, 0), 1);
			PR_CHECK(ArithmeticSum(1, 2, 5), 36);
		}
		{// Geometric sequence
			GeometricSequence g(2, 5);
			PR_CHECK(g(), 2);
			PR_CHECK(g(), 10);
			PR_CHECK(g(), 50);
			PR_CHECK(g(), 250);

			PR_CHECK(GeometricSum(1, 2, 4), 31);
			PR_CHECK(GeometricSum(4, 2, 2), 28);
			PR_CHECK(GeometricSum(1, 3, 0), 1);
			PR_CHECK(GeometricSum(1, 3, 5), 364);
		}
	}
	PRUnitTest(Vector2Tests)
	{
		{// Create
			auto V0 = v2(1, 1);
			PR_CHECK(V0.x == 1.0f, true);
			PR_CHECK(V0.y == 1.0f, true);

			auto V1 = v2(1, 2);
			PR_CHECK(V1.x == 1.0f, true);
			PR_CHECK(V1.y == 2.0f, true);

			auto V2 = v2({3, 4});
			PR_CHECK(V2.x == 3.0f, true);
			PR_CHECK(V2.y == 4.0f, true);

			v2 V3 = {4, 5};
			PR_CHECK(V3.x == 4.0f, true);
			PR_CHECK(V3.y == 5.0f, true);

			auto V4 = v2::Normal(3, 4);
			PR_CHECK(FEql(V4, v2(0.6f, 0.8f)), true);
			PR_CHECK(FEql(V4[0], 0.6f), true);
			PR_CHECK(FEql(V4[1], 0.8f), true);
		}
		{// Operators
			auto V0 = v2(1, 2);
			auto V1 = v2(2, 3);

			PR_CHECK(FEql(V0 + V1, v2(3, 5)), true);
			PR_CHECK(FEql(V0 - V1, v2(-1, -1)), true);
			PR_CHECK(FEql(V0 * V1, v2(2, 6)), true);
			PR_CHECK(FEql(V0 / V1, v2(1.0f / 2.0f, 2.0f / 3.0f)), true);
			PR_CHECK(FEql(V0 % V1, v2(1, 2)), true);

			PR_CHECK(FEql(V0 * 3.0f, v2(3, 6)), true);
			PR_CHECK(FEql(V0 / 2.0f, v2(0.5f, 1.0f)), true);
			PR_CHECK(FEql(V0 % 2.0f, v2(1, 0)), true);

			PR_CHECK(FEql(3.0f * V0, v2(3, 6)), true);

			PR_CHECK(FEql(+V0, v2(1, 2)), true);
			PR_CHECK(FEql(-V0, v2(-1, -2)), true);

			PR_CHECK(V0 == v2(1, 2), true);
			PR_CHECK(V0 != v2(2, 1), true);
		}
		{// Min/Max/Clamp
			auto V0 = v2(1, 2);
			auto V1 = v2(-1, -2);
			auto V2 = v2(2, 4);

			PR_CHECK(FEql(Min(V0, V1, V2), v2(-1, -2)), true);
			PR_CHECK(FEql(Max(V0, V1, V2), v2(2, 4)), true);
			PR_CHECK(FEql(Clamp(V0, V1, V2), v2(1, 2)), true);
			PR_CHECK(FEql(Clamp(V0, 0.0f, 1.0f), v2(1, 1)), true);
		}
	}
	PRUnitTest(Vector3Tests)
	{
	}
	PRUnitTest(Vector4Tests)
	{
		#if PR_MATHS_USE_DIRECTMATH
		{
			v4 V0 = v4(1,2,3,4);
			DirectX::XMVECTORF32 VX0;
			VX0.v = V0.vec;
			PR_CHECK(V0.x, VX0.f[0]);
			PR_CHECK(V0.y, VX0.f[1]);
			PR_CHECK(V0.z, VX0.f[2]);
			PR_CHECK(V0.w, VX0.f[3]);
		}
		#endif
		{// Operators
			auto a = v4{1, 2, 3, 4};
			auto b = v4{-4, -3, -2, -1};

			PR_CHECK(a + b, v4{-3, -1, +1, +3});
			PR_CHECK(a - b, v4{+5, +5, +5, +5});
			PR_CHECK(3 * a, v4{+3, +6, +9, +12});
			PR_CHECK(a % 2, v4{+1, +0, +1, +0});
			PR_CHECK(a/2.0f, v4{1.0f/2.0f, 2.0f/2.0f, 3.0f/2.0f, 4.0f/2.0f});
			PR_CHECK(1.0f/a, v4{1.0f/1.0f, 1.0f/2.0f, 1.0f/3.0f, 1.0f/4.0f});
		}
		{// Largest/Smallest element
			auto v1 = v4{1,-2,-3,4};
			PR_CHECK(MinElement(v1) == -3, true);
			PR_CHECK(MaxElement(v1) == +4, true);
			PR_CHECK(MinElementIndex(v1) == 2, true);
			PR_CHECK(MaxElementIndex(v1) == 3, true);
		}
		{// FEql
			auto a = v4{0, 0, -1, 0.5f};
			auto b = v4{0, 0, -1, 0.5f};
			
			// Equal if the relative difference is less than tiny compared to the maximum element in the matrix.
			a.x = a.y = 1.0e-5f;
			b.x = b.y = 1.1e-5f;
			PR_CHECK(FEql(MinElement(a), -1.0f), true);
			PR_CHECK(FEql(MinElement(b), -1.0f), true);
			PR_CHECK(FEql(MaxElement(a), +0.5f), true);
			PR_CHECK(FEql(MaxElement(b), +0.5f), true);
			PR_CHECK(FEql(a,b), true);
			
			a.z = a.w = 1.0e-5f;
			b.z = b.w = 1.1e-5f;
			PR_CHECK(FEql(MaxElement(a), 1.0e-5f), true);
			PR_CHECK(FEql(MaxElement(b), 1.1e-5f), true);
			PR_CHECK(FEql(a,b), false);
		}
		{// FEql
			v4 a(1,1,-1,-1);
			auto t2 = maths::tinyf * 2.0f;
			PR_CHECK(FEql(a, v4(1   ,1,-1,-1)), true);
			PR_CHECK(FEql(a, v4(1+t2,1,-1,-1)), false);
			PR_CHECK(FEql(v4(1e-20f,0,0,1).xyz, v3::Zero()), true);
			PR_CHECK(FEql(v4(1e-20f,0,0,1e-19f), v4::Zero()), true);
		}
		{
			v4 a(3,-1,2,-4);
			v4 b = {-2,-1,4,2};
			PR_CHECK(Max(a,b), v4(3,-1,4,2));
			PR_CHECK(Min(a,b), v4(-2,-1,2,-4));
		}
		{
			v4 a(3,-1,2,-4);
			PR_CHECK(MinElement(a), -4.0f);
			PR_CHECK(MaxElement(a), 3.0f);
		}
		{
			v4 a(3,-1,2,-4);
			PR_CHECK(LengthSq(a), a.x*a.x + a.y*a.y + a.z*a.z + a.w*a.w);
			PR_CHECK(Length(a), sqrt(LengthSq(a)));
		}
		{
			v4 a(3,-1,2,-4);
			v4 b = Normalise(a.w0());
			v4 c = Normalise(a);
			PR_CHECK(Length(b), 1.0f);
			PR_CHECK(Length(c), 1.0f);
			PR_CHECK(IsNormal(a), false);
			PR_CHECK(IsNormal(b), true);
			PR_CHECK(IsNormal(c), true);
		}
		{
			v4 a = {-2,  4,  2,  6};
			v4 b = { 3, -5,  2, -4};
			auto a2b = CPM(a, v4::Origin());

			v4 c = Cross3(a,b);
			v4 d = a2b * b;
			PR_CHECK(FEql(c.xyz, d.xyz), true);
		}
		{
			v4 a = {-2,  4,  2,  6};
			v4 b = { 3, -5,  2, -4};
			PR_CHECK(Dot4(a,b), -46);
			PR_CHECK(Dot3(a,b), -22);
		}
		{ // ComponentSum
			v4 a = {1, 2, 3, 4};
			PR_CHECK(ComponentSum(a), 1+2+3+4);
		}
		{
			char c0;
			v4 const pt0[] =
			{
				v4(1,2,3,4),
				v4(5,6,7,8),
			};
			char c1;
			v4 const pt1[] =
			{
				v4(1,2,3,4),
				v4(5,6,7,8),
			};
			(void)c0,c1;
			PR_CHECK(maths::is_aligned(&pt0[0]), true);
			PR_CHECK(maths::is_aligned(&pt1[0]), true);
		}
	}
	PRUnitTest(Vector8Tests)
	{
		std::default_random_engine rng;
		{// LinAt, AngAt
			auto v = v8{Random3(rng, v4{}, 10.0f, 0.0f), Random3(rng, v4{}, 10.0f, 0.0f)};
			auto lin = v.LinAt(v4Origin);
			auto ang = v.AngAt(v4Origin);
			auto V = v8{ang, lin};
			PR_CHECK(FEql(v, V), true);
		}
		{// LinAt, AngAt
			auto v = v8{0, 0, 1, 0, 1, 0};

			auto lin0 = v.LinAt(v4{-1,0,0,0});
			auto ang0 = v.AngAt(v4{-1,0,0,0});
			PR_CHECK(FEql(lin0, v4{0,0,0,0}), true);
			PR_CHECK(FEql(ang0, v4{0,0,2,0}), true);

			auto lin1 = v.LinAt(v4{0,0,0,0});
			auto ang1 = v.AngAt(v4{0,0,0,0});
			PR_CHECK(FEql(lin1, v4{0,1,0,0}), true);
			PR_CHECK(FEql(ang1, v4{0,0,1,0}), true);

			auto lin2 = v.LinAt(v4{+1,0,0,0});
			auto ang2 = v.AngAt(v4{+1,0,0,0});
			PR_CHECK(FEql(lin2, v4{0,2,0,0}), true);
			PR_CHECK(FEql(ang2, v4{0,0,0,0}), true);

			auto lin3 = v.LinAt(v4{+2,0,0,0});
			auto ang3 = v.AngAt(v4{+2,0,0,0});
			PR_CHECK(FEql(lin3, v4{0,3,0,0}), true);
			PR_CHECK(FEql(ang3, v4{0,0,-1,0}), true);

			auto lin4 = v.LinAt(v4{+3,0,0,0});
			auto ang4 = v.AngAt(v4{+3,0,0,0});
			PR_CHECK(FEql(lin4, v4{0,4,0,0}), true);
			PR_CHECK(FEql(ang4, v4{0,0,-2,0}), true);
		}
		{// Projection
			auto v = v8{1,-2,3,-3,2,-1};
			auto vn = Proj(v, v4ZAxis);
			auto vt = v - vn;
			auto r = vn + vt;
			PR_CHECK(FEql(vn, v8{0,0,3,0,0,-1}), true);
			PR_CHECK(FEql(vt, v8{1,-2,0,-3,2,0}), true);
			PR_CHECK(FEql(r, v), true);
		}
		{// Projection/Reflect
			auto v = v8{0, 0, 1, 0, 1, 0};
			auto n = v4::Normal(-1,-1,-1,0);
			auto r = v8{-0.6666666f, -0.6666666f, 0.3333333f, -0.6666666f, 0.3333333f, -0.6666666f};
			auto R = Reflect(v, n);
			PR_CHECK(FEql(r, R), true);
		}
	}
	PRUnitTest(QuaternionTests)
	{
		std::default_random_engine rng(1U);

		{ // Create
			#if PR_MATHS_USE_INTRINSICS && PR_MATHS_USE_DIRECTMATH
			auto p = DegreesToRadians(  43.0f);
			auto y = DegreesToRadians(  10.0f);
			auto r = DegreesToRadians(-245.0f);

			auto q0 = quat(p,y,r);
			quat q1(DirectX::XMQuaternionRotationRollPitchYaw(p,y,r));
			PR_CHECK(FEql(q0, q1), true);
			#endif
		}
		{ // Create from m3x4
			std::uniform_real_distribution<float> rng_angle(-maths::tauf, +maths::tauf);
			for (int i = 0; i != 100; ++i)
			{
				auto ang = rng_angle(rng);
				auto axis = Random3N(rng, 0);
				auto mat = m3x4::Rotation(axis, ang);
				auto q = quat(mat);
				auto v0 = Random3N(rng, 0);
				auto r0 = mat * v0;
				auto r1 = Rotate(q, v0);
				PR_CHECK(FEql(r0, r1), true);
			}
		}
		{ // Average
			auto ideal_mean = quat(Normalise(v4(1,1,1,0)), 0.5f);

			std::uniform_int_distribution<int> rng_bool(0, 1);
			std::uniform_real_distribution<float> rng_angle(ideal_mean.Angle() - 0.2f, ideal_mean.Angle() + 0.2f);

			Avr<quat, float> avr;
			for (int i = 0; i != 1000; ++i)
			{
				auto axis = Normalise(ideal_mean.Axis() + Random3(rng, 0.0f, 0.2f, 0.0f));
				auto angle = rng_angle(rng);
				quat q(axis, angle);
				avr.Add(rng_bool(rng) ? q : -q);
			}
				
			auto actual_mean = avr.Mean();
			PR_CHECK(FEqlRelative(ideal_mean, actual_mean, 0.01f), true);
		}
	}
	PRUnitTest(Matrix2x2Tests)
	{
		{// Create
			auto V0 = m2x2(1,2,3,4);
			PR_CHECK(V0.x == v2(1,2), true);
			PR_CHECK(V0.y == v2(3,4), true);

			auto V1 = m2x2(v2(1,2), v2(3,4));
			PR_CHECK(V1.x == v2(1,2), true);
			PR_CHECK(V1.y == v2(3,4), true);

			auto V2 = m2x2({1,2,3,4});
			PR_CHECK(V2.x == v2(1,2), true);
			PR_CHECK(V2.y == v2(3,4), true);

			auto V3 = m2x2{4,5,6,7};
			PR_CHECK(V3.x == v2(4,5), true);
			PR_CHECK(V3.y == v2(6,7), true);
		}
		{// Operators
			auto V0 = m2x2(1,2,3,4);
			auto V1 = m2x2(2,3,4,5);

			PR_CHECK(FEql(V0 + V1, m2x2(3,5,7,9)), true);
			PR_CHECK(FEql(V0 - V1, m2x2(-1,-1,-1,-1)), true);

			// 1 3     2 4     2+9  4+15     11 19
			// 2 4  x  3 5  =  4+12 8+20  =  16 28
			PR_CHECK(FEql(V0 * V1, m2x2(11,16,19,28)), true);

			PR_CHECK(FEql(V0 / 2.0f, m2x2(1.0f/2.0f, 2.0f/2.0f, 3.0f/2.0f, 4.0f/2.0f)), true);
			PR_CHECK(FEql(V0 % 2.0f, m2x2(1,0,1,0)), true);

			PR_CHECK(FEql(V0 * 3.0f, m2x2(3,6,9,12)), true);
			PR_CHECK(FEql(V0 / 2.0f, m2x2(0.5f, 1.0f, 1.5f, 2.0f)), true);
			PR_CHECK(FEql(V0 % 2.0f, m2x2(1,0,1,0)), true);

			PR_CHECK(FEql(3.0f * V0, m2x2(3,6,9,12)), true);

			PR_CHECK(FEql(+V0, m2x2(1,2,3,4)), true);
			PR_CHECK(FEql(-V0, m2x2(-1,-2,-3,-4)), true);

			PR_CHECK(V0 == m2x2(1,2,3,4), true);
			PR_CHECK(V0 != m2x2(4,3,2,1), true);
		}
		{// Min/Max/Clamp
			auto V0 = m2x2(1,2,3,4);
			auto V1 = m2x2(-1,-2,-3,-4);
			auto V2 = m2x2(2,4,6,8);

			PR_CHECK(FEql(Min(V0,V1,V2), m2x2(-1,-2,-3,-4)), true);
			PR_CHECK(FEql(Max(V0,V1,V2), m2x2(2,4,6,8)), true);
			PR_CHECK(FEql(Clamp(V0,V1,V2), m2x2(1,2,3,4)), true);
			PR_CHECK(FEql(Clamp(V0,0.0f,1.0f), m2x2(1,1,1,1)), true);
		}
	}
	PRUnitTest(Matrix3x3Tests)
	{
		std::default_random_engine rng;
		{// Multiply scalar
			auto m1 = m3x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{4,3,2,1}};
			auto m2 = 2.0f;
			auto m3 = m3x4{v4{2,4,6,8}, v4{2,2,2,2}, v4{8,6,4,2}};
			auto r = m1 * m2;
			PR_CHECK(FEql(r, m3), true);
		}
		{// Multiply vector4
			auto m = m3x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{4,3,2,1}};
			auto v = v4{-3,4,2,-2};
			auto R = v4{9,4,-1,-2};
			auto r = m * v;
			PR_CHECK(FEql(r, R), true);
		}
		{// Multiply vector3
			auto m = m3x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{4,3,2,1}};
			auto v = v3{-3,4,2};
			auto R = v3{9,4,-1};
			auto r = m * v;
			PR_CHECK(FEql(r, R), true);
		}
		{// Multiply matrix
			auto m1 = m3x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{4,3,2,1}};
			auto m2 = m3x4{v4{1,1,1,1}, v4{2,2,2,2}, v4{-2,-2,-2,-2}};
			auto m3 = m3x4{v4{6,6,6,0}, v4{12,12,12,0}, v4{-12,-12,-12,0}};
			auto r = m1 * m2;
			PR_CHECK(FEql(r, m3), true);
		}
		{//OriFromDir
			using namespace pr;
			v4 dir(0,1,0,0);
			{
				auto ori = OriFromDir(dir, AxisId::PosZ, v4::ZAxis());
				PR_CHECK(dir == +ori.z, true);
				PR_CHECK(IsOrthonormal(ori), true);
			}
			{
				auto ori = OriFromDir(dir, AxisId::NegX);
				PR_CHECK(dir == -ori.x, true);
				PR_CHECK(IsOrthonormal(ori), true);
			}
			{
				auto scale = 0.125f;
				auto sdir = scale * dir;
				auto ori = ScaledOriFromDir(sdir, AxisId::PosY);
				PR_CHECK(sdir == +ori.y, true);
				PR_CHECK(IsOrthonormal((1/scale) * ori), true);
			}
		}
		{// Inverse
			{
				auto m = Random3x4(rng, Random3N(rng, 0), -float(maths::tau), +float(maths::tau));
				auto inv_m0 = InvertFast(m);
				auto inv_m1 = Invert(m);
				PR_CHECK(FEql(inv_m0, inv_m1), true);
			}{
				auto m = Random3x4(rng, -5.0f, +5.0f);
				auto inv_m = Invert(m);
				auto I0 = inv_m * m;
				auto I1 = m * inv_m;

				PR_CHECK(FEql(I0, m3x4::Identity()), true);
				PR_CHECK(FEql(I1, m3x4::Identity()), true);
			}{
				auto m = m3x4(
					v4(0.25f, 0.5f, 1.0f, 0.0f),
					v4(0.49f, 0.7f, 1.0f, 0.0f),
					v4(1.0f, 1.0f, 1.0f, 0.0f));
				auto INV_M = m3x4(
					v4(10.0f, -16.666667f, 6.66667f, 0.0f),
					v4(-17.0f, 25.0f, -8.0f, 0.0f),
					v4(7.0f, -8.333333f, 2.333333f, 0.0f));

				auto inv_m = Invert(m);
				PR_CHECK(FEqlRelative(inv_m, INV_M, 0.0001f), true);
			}
		}
		{// CPM
			{
				auto v = v4(2.0f, -1.0f, 4.0f, 0.0f);
				auto m = CPM(v);

				auto a0 = Random3(rng, v4::Origin(), 5.0f, 0.0f);
				auto A0 = m * a0;
				auto A1 = Cross(v, a0);

				PR_CHECK(FEql(A0, A1), true);
			}
		}
	}
	PRUnitTest(Matrix4x4Tests)
	{
		using namespace pr;
		std::default_random_engine rng;
		{
			auto m1 = m4x4::Identity();
			auto m2 = m4x4::Identity();
			auto m3 = m1 * m2;
			PR_CHECK(FEql(m3, m4x4::Identity()), true);
		}
		{// Largest/Smallest element
			auto m1 = m4x4{v4{1,2,3,4}, v4{-2,-3,-4,-5}, v4{1,1,-1,9}, v4{-8, 5, 0, 0}};
			PR_CHECK(MinElement(m1) == -8, true);
			PR_CHECK(MaxElement(m1) == +9, true);
		}
		{// FEql
			// Equal if the relative difference is less than tiny compared to the maximum element in the matrix.
			auto m1 = m4x4::Identity();
			auto m2 = m4x4::Identity();
			
			m1.x.x = m1.y.y = 1.0e-5f;
			m2.x.x = m2.y.y = 1.1e-5f;
			PR_CHECK(FEql(MaxElement(m1), 1), true);
			PR_CHECK(FEql(MaxElement(m2), 1), true);
			PR_CHECK(FEql(m1,m2), true);
			
			m1.z.z = m1.w.w = 1.0e-5f;
			m2.z.z = m2.w.w = 1.1e-5f;
			PR_CHECK(FEql(MaxElement(m1), 1.0e-5f), true);
			PR_CHECK(FEql(MaxElement(m2), 1.1e-5f), true);
			PR_CHECK(FEql(m1,m2), false);
		}
		{// Multiply scalar
			auto m1 = m4x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{-2,-2,-2,-2}, v4{4,3,2,1}};
			auto m2 = 2.0f;
			auto m3 = m4x4{v4{2,4,6,8}, v4{2,2,2,2}, v4{-4,-4,-4,-4}, v4{8,6,4,2}};
			auto r = m1 * m2;
			PR_CHECK(FEql(r, m3), true);
		}
		{// Multiply vector
			auto m = m4x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{-2,-2,-2,-2}, v4{4,3,2,1}};
			auto v = v4{-3,4,2,-1};
			auto R = v4{-7,-9,-11,-13};
			auto r = m * v;
			PR_CHECK(FEql(r, R), true);
		}
		{// Multiply matrix
			auto m1 = m4x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{-2,-2,-2,-2}, v4{4,3,2,1}};
			auto m2 = m4x4{v4{1,1,1,1}, v4{2,2,2,2}, v4{-1,-1,-1,-1}, v4{-2,-2,-2,-2}};
			auto m3 = m4x4{v4{4,4,4,4}, v4{8,8,8,8}, v4{-4,-4,-4,-4}, v4{-8,-8,-8,-8}};
			auto r = m1 * m2;
			PR_CHECK(FEql(r, m3), true);
		}
		{// Component multiply
			auto m1 = m4x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{-2,-2,-2,-2}, v4{4,3,2,1}};
			auto m2 = v4(2,1,-2,-1);
			auto m3 = m4x4{v4{2,4,6,8}, v4{1,1,1,1}, v4{+4,+4,+4,+4}, v4{-4,-3,-2,-1}};
			auto r = CompMul(m1, m2);
			PR_CHECK(FEql(r, m3), true);
		}
		{//m4x4Translation
			auto m1 = m4x4(v4::XAxis(), v4::YAxis(), v4::ZAxis(), v4(1.0f, 2.0f, 3.0f, 1.0f));
			auto m2 = m4x4::Translation(v4(1.0f, 2.0f, 3.0f, 1.0f));
			PR_CHECK(FEql(m1, m2), true);
		}
		{//m4x4CreateFrom
			auto V1 = Random3(rng, 0.0f, 10.0f, 1.0f);
			auto a2b = m4x4::Transform(v4::Normal(+3,-2,-1,0), +1.23f, v4(+4.4f, -3.3f, +2.2f, 1.0f));
			auto b2c = m4x4::Transform(v4::Normal(-1,+2,-3,0), -3.21f, v4(-1.1f, +2.2f, -3.3f, 1.0f));
			PR_CHECK(IsOrthonormal(a2b), true);
			PR_CHECK(IsOrthonormal(b2c), true);
			v4 V2 = a2b * V1;
			v4 V3 = b2c * V2; V3;
			m4x4 a2c = b2c * a2b;
			v4 V4 = a2c * V1; V4;
			PR_CHECK(FEql(V3, V4), true);
		}
		{//m4x4CreateFrom2
			auto q = quat(1.0f, 0.5f, 0.7f);
			m4x4 m1 = m4x4::Transform(1.0f, 0.5f, 0.7f, v4Origin);
			m4x4 m2 = m4x4::Transform(q, v4Origin);
			PR_CHECK(IsOrthonormal(m1), true);
			PR_CHECK(IsOrthonormal(m2), true);
			PR_CHECK(FEql(m1, m2), true);

			std::uniform_real_distribution<float> dist(-1.0f,+1.0f);
			float ang = dist(rng);
			v4 axis = Random3N(rng, 0.0f);
			m1 = m4x4::Transform(axis, ang, v4Origin);
			m2 = m4x4::Transform(quat(axis, ang), v4Origin);
			PR_CHECK(IsOrthonormal(m1), true);
			PR_CHECK(IsOrthonormal(m2), true);
			PR_CHECK(FEql(m1, m2), true);
		}
		{// Invert
			m4x4 a2b = m4x4::Transform(v4::Normal(-4,-3,+2,0), -2.15f, v4(-5,+3,+1,1));
			m4x4 b2a = Invert(a2b);
			m4x4 a2a = b2a * a2b;
			PR_CHECK(FEql(m4x4Identity, a2a), true);
			{
				#if PR_MATHS_USE_DIRECTMATH
				auto dx_b2a = m4x4(DirectX::XMMatrixInverse(nullptr, a2b));
				PR_CHECK(FEql(b2a, dx_b2a), true);
				#endif
			}

			m4x4 b2a_fast = InvertFast(a2b);
			PR_CHECK(FEql(b2a_fast, b2a), true);
		}
		{//m4x4Orthonormalise
			m4x4 a2b;
			a2b.x = v4(-2.0f, 3.0f, 1.0f, 0.0f);
			a2b.y = v4(4.0f,-1.0f, 2.0f, 0.0f);
			a2b.z = v4(1.0f,-2.0f, 4.0f, 0.0f);
			a2b.w = v4(1.0f, 2.0f, 3.0f, 1.0f);
			PR_CHECK(IsOrthonormal(Orthonorm(a2b)), true);
		}
	}
	PRUnitTest(Matrix6x8Tests)
	{
		{// Memory order tests
			auto m1 = Matrix<float>{6, 8,  // = [{1} {2} {3} {4} {5} {6}]
			{
				1, 1, 1, 1, 1, 1, 1, 1,
				2, 2, 2, 2, 2, 2, 2, 2,
				3, 3, 3, 3, 3, 3, 3, 3,
				4, 4, 4, 4, 4, 4, 4, 4,
				5, 5, 5, 5, 5, 5, 5, 5,
				6, 6, 6, 6, 6, 6, 6, 6,
			}};
			auto m2 = Matrix<float>{6, 8,
			{
				1, 1, 1, 1, 2, 2, 2, 2, // = [[1] [3]]
				1, 1, 1, 1, 2, 2, 2, 2, //   [[2] [4]]
				1, 1, 1, 1, 2, 2, 2, 2,
				3, 3, 3, 3, 4, 4, 4, 4,
				3, 3, 3, 3, 4, 4, 4, 4,
				3, 3, 3, 3, 4, 4, 4, 4,
			}};
			auto M1 = m6x8 // = [{1} {2} {3} {4} {5} {6}]
			{
				m3x4{v4{1},v4{2},v4{3}}, m3x4{v4{4},v4{5},v4{6}},
				m3x4{v4{1},v4{2},v4{3}}, m3x4{v4{4},v4{5},v4{6}}
			};
			auto M2 = m6x8
			{
				m3x4{1}, m3x4{3}, // = [[1] [3]]
				m3x4{2}, m3x4{4}  //   [[2] [4]]
			};

			for (int r = 0; r != 6; ++r)
			for (int c = 0; c != 8; ++c)
			{
				PR_CHECK(FEql(m1(r, c), M1[r][c]), true);
				PR_CHECK(FEql(m2(r, c), M2[r][c]), true);
			}

			auto M3 = m6x8 // = [{1} {2} {3} {4} {5} {6}]
			{
				v8{1,1,1,1, 1,1,1,1},
				v8{2,2,2,2, 2,2,2,2},
				v8{3,3,3,3, 3,3,3,3},
				v8{4,4,4,4, 4,4,4,4},
				v8{5,5,5,5, 5,5,5,5},
				v8{6,6,6,6, 6,6,6,6},
			};
			PR_CHECK(FEql(M3, M1), true);
		}
		{// Array access
			auto m1 = m6x8
			{
				m3x4{v4{1},v4{2},v4{3}}, m3x4{v4{4},v4{5},v4{6}}, // = [{1} {2} {3} {4} {5} {6}]
				m3x4{v4{1},v4{2},v4{3}}, m3x4{v4{4},v4{5},v4{6}}
			};
			PR_CHECK(FEql(m1[0], v8{1,1,1,1, 1,1,1,1}), true);
			PR_CHECK(FEql(m1[1], v8{2,2,2,2, 2,2,2,2}), true);
			PR_CHECK(FEql(m1[2], v8{3,3,3,3, 3,3,3,3}), true);
			PR_CHECK(FEql(m1[3], v8{4,4,4,4, 4,4,4,4}), true);
			PR_CHECK(FEql(m1[4], v8{5,5,5,5, 5,5,5,5}), true);
			PR_CHECK(FEql(m1[5], v8{6,6,6,6, 6,6,6,6}), true);

			auto tmp = m1.col(0);
			m1.col(0, m1[5]);
			m1.col(5, tmp);
			PR_CHECK(FEql(m1[0], v8{6,6,6,6, 6,6,6,6}), true);
			PR_CHECK(FEql(m1[5], v8{1,1,1,1, 1,1,1,1}), true);
		}
		{// Multiply vector
			auto m = Matrix<float>{6, 6,
			{
				1, 1, 1, 1, 1, 1, // = [{1} {2} {3} {4} {5} {6}]
				2, 2, 2, 2, 2, 2,
				3, 3, 3, 3, 3, 3,
				4, 4, 4, 4, 4, 4,
				5, 5, 5, 5, 5, 5,
				6, 6, 6, 6, 6, 6,
			}};
			auto v = Matrix<float>{1, 6,
			{
				1, 2, 3, 4, 5, 6,
			}};
			auto e = Matrix<float>{1, 6,
			{
				91, 91, 91, 91, 91, 91,
			}};
			auto r = m * v;
			PR_CHECK(FEql(r, e), true);

			auto M = m6x8
			{
				m3x4{v3{1},v3{2},v3{3}}, m3x4{v3{4},v3{5},v3{6}},
				m3x4{v3{1},v3{2},v3{3}}, m3x4{v3{4},v3{5},v3{6}}, // = [{1} {2} {3} {4} {5} {6}]
			};
			auto V = v8
			{
				v4{1, 2, 3, 0},
				v4{4, 5, 6, 0},
			};
			auto E = v8
			{
				v4{91, 91, 91, 0},
				v4{91, 91, 91, 0},
			};
			auto R = M * V;
			PR_CHECK(FEql(R, E), true);
		}
		{// Multiply matrix
			auto m1 = Matrix<float>{6, 6,
			{
				1, 1, 1, 1, 1, 1, // = [{1} {2} {3} {4} {5} {6}]
				2, 2, 2, 2, 2, 2,
				3, 3, 3, 3, 3, 3,
				4, 4, 4, 4, 4, 4,
				5, 5, 5, 5, 5, 5,
				6, 6, 6, 6, 6, 6,
			}};
			auto m2 = Matrix<float>{6, 6,
			{
				1, 1, 1, 2, 2, 2, // = [[1] [3]]
				1, 1, 1, 2, 2, 2, //   [[2] [4]]
				1, 1, 1, 2, 2, 2,
				3, 3, 3, 4, 4, 4,
				3, 3, 3, 4, 4, 4,
				3, 3, 3, 4, 4, 4,
			}};
			auto m3 = Matrix<float>{6, 6,
			{
				36, 36, 36, 36, 36, 36, 
				36, 36, 36, 36, 36, 36, 
				36, 36, 36, 36, 36, 36, 
				78, 78, 78, 78, 78, 78, 
				78, 78, 78, 78, 78, 78, 
				78, 78, 78, 78, 78, 78, 
			}};
			auto m4 = m1 * m2;
			PR_CHECK(FEql(m3, m4), true);

			auto M1 = m6x8
			{
				m3x4{v3{1},v3{2},v3{3}}, m3x4{v3{4},v3{5},v3{6}},
				m3x4{v3{1},v3{2},v3{3}}, m3x4{v3{4},v3{5},v3{6}}, // = [{1} {2} {3} {4} {5} {6}]
			};
			auto M2 = m6x8
			{
				m3x4{v3{1},v3{1},v3{1}}, m3x4{v3{3},v3{3},v3{3}}, // = [[1] [3]]
				m3x4{v3{2},v3{2},v3{2}}, m3x4{v3{4},v3{4},v3{4}}  //   [[2] [4]]
			};
			auto M3 = m6x8
			{
				m3x4{v3{36},v3{36},v3{36}}, m3x4{v3{78},v3{78},v3{78}},
				m3x4{v3{36},v3{36},v3{36}}, m3x4{v3{78},v3{78},v3{78}} 
			};
			auto M4 = M1 * M2;
			PR_CHECK(FEql(M3, M4), true);
		}
		{// Transpose
			auto m1 = Matrix<float>{6, 6,
			{
				1, 1, 1, 1, 1, 1, // = [{1} {2} {3} {4} {5} {6}]
				2, 2, 2, 2, 2, 2,
				3, 3, 3, 3, 3, 3,
				4, 4, 4, 4, 4, 4,
				5, 5, 5, 5, 5, 5,
				6, 6, 6, 6, 6, 6,
			}};
			auto M1 = m6x8
			{
				m3x4{v3{1},v3{2},v3{3}}, m3x4{v3{4},v3{5},v3{6}}, // = [{1} {2} {3} {4} {5} {6}]
				m3x4{v3{1},v3{2},v3{3}}, m3x4{v3{4},v3{5},v3{6}}
			};
			auto m2 = Transpose(m1);
			auto M2 = Transpose(M1);

			for (int i = 0; i != 6; ++i)
			{
				PR_CHECK(FEql(M2[i], v8{v3{1, 2, 3}, v3{4, 5, 6}}), true);

				PR_CHECK(FEql(m2(i, 0), M2[i].ang.x), true);
				PR_CHECK(FEql(m2(i, 1), M2[i].ang.y), true);
				PR_CHECK(FEql(m2(i, 2), M2[i].ang.z), true);
				PR_CHECK(FEql(m2(i, 3), M2[i].lin.x), true);
				PR_CHECK(FEql(m2(i, 4), M2[i].lin.y), true);
				PR_CHECK(FEql(m2(i, 5), M2[i].lin.z), true);
			}
		}
		{// Inverse
			auto M = m6x8
			{
				v8{+1, +1, +2, -1, +6, +2},
				v8{-2, +2, +4, -3, +5, -4},
				v8{+1, +3, -2, -5, +4, +6},
				v8{+1, +4, +3, -7, +3, -5},
				v8{+1, +2, +3, -2, +2, +3},
				v8{+1, -1, -2, -3, +6, -1}
			};
			auto M_ = m6x8
			{
				v8{+227.0f/794.0f, -135.0f/397.0f, -101.0f/794.0f,  +84.0f/397.0f,  -16.0f/397.0f,  -4.0f/397.0f},
				v8{+219.0f/397.0f,  -75.0f/794.0f, +382.0f/1985.f, +179.0f/794.0f, -2647.f/3970.f,-976.0f/1985.f},
				v8{-129.0f/794.0f,  +26.0f/397.0f, -107.0f/794.0f,  -25.0f/397.0f, +156.0f/397.0f, +39.0f/397.0f},
				v8{+367.0f/794.0f,  -71.0f/794.0f,  +51.0f/3970.f,  +53.0f/794.0f, -1733.f/3970.f,-564.0f/1985.f},
				v8{+159.0f/794.0f,  +19.0f/794.0f,  +87.0f/3970.f,   -3.0f/794.0f, -621.0f/3970.f, -28.0f/1985.f},
				v8{ -50.0f/397.0f,  +14.0f/397.0f,  +17.0f/397.0f,  -44.0f/397.0f,  +84.0f/397.0f, +21.0f/397.0f}
			};

			auto m_ = Invert(M);
			PR_CHECK(FEql(m_, M_), true);

			auto I = M * M_;
			PR_CHECK(FEql(I, m6x8Identity), true);
			
			auto i = M * m_;
			PR_CHECK(FEql(i, m6x8Identity), true);
		}
	}
	PRUnitTest(MatrixTests)
	{
		using namespace pr;
		std::default_random_engine rng(1);

		{// LU decomposition
			auto m = MatrixLU<double>(4, 4, 
			{
				  1.0, +2.0,  3.0, +1.0,
				  4.0, -5.0,  6.0, +5.0,
				  7.0, +8.0,  9.0, -9.0,
				-10.0, 11.0, 12.0, +0.0,
			});
			auto res = Matrix<double>(4, 4,
			{
				3.0, 0.66666666666667, 0.33333333333333, 0.33333333333333,
				6.0, -9.0, -0.33333333333333, -0.22222222222222,
				9.0, 2.0, -11.333333333333, -0.3921568627451,
				12.0, 3.0, -3.0, -14.509803921569,
			});
			PR_CHECK(FEql(m.lu, res), true);
		}
		{// Invert
			auto m = Matrix<double>(4, 4, { 1, 2, 3, 1, 4, -5, 6, 5, 7, 8, 9, -9, -10, 11, 12, 0 });
			auto inv = Invert(m);
			auto INV = Matrix<double>(4, 4,
			{
				+0.258783783783783810, -0.018918918918918920, +0.018243243243243241, -0.068918918918918923,
				+0.414864864864864790, -0.124324324324324320, -0.022972972972972971, -0.024324324324324322,
				-0.164639639639639650, +0.098198198198198194, +0.036261261261261266, +0.048198198198198199,
				+0.405405405405405430, -0.027027027027027029, -0.081081081081081086, -0.027027027027027025,
			});
			PR_CHECK(FEql(inv, INV), true);
		}
		{// Invert
			auto M = m4x4(
				v4(1.0f, +2.0f, 3.0f, +1.0f),
				v4(4.0f, -5.0f, 6.0f, +5.0f),
				v4(7.0f, +8.0f, 9.0f, -9.0f),
				v4(-10.0f, 11.0f, 12.0f, +0.0f)
			);
			auto INV = Invert(M);
			auto m = Matrix<double>(4, 4,
			{
				1.0, +2.0, 3.0, +1.0,
				4.0, -5.0, 6.0, +5.0,
				7.0, +8.0, 9.0, -9.0,
				-10.0, 11.0, 12.0, +0.0,
			});
			auto inv = Invert(m);

			PR_CHECK(FEql(m, M), true);
			PR_CHECK(FEql(inv, INV), true);
		}
		{// Invert transposed
			auto M = Transpose4x4(m4x4(
				v4(1.0f, +2.0f, 3.0f, +1.0f),
				v4(4.0f, -5.0f, 6.0f, +5.0f),
				v4(7.0f, +8.0f, 9.0f, -9.0f),
				v4(-10.0f, 11.0f, 12.0f, +0.0f)
			));
			auto INV = Invert(M);
			auto m = Matrix<double>(4, 4,
			{
				1.0, +2.0, 3.0, +1.0,
				4.0, -5.0, 6.0, +5.0,
				7.0, +8.0, 9.0, -9.0,
				-10.0, 11.0, 12.0, +0.0,
			}, true);
			auto inv = Invert(m);

			PR_CHECK(FEql(m, M), true);
			PR_CHECK(FEql(inv, INV), true);
		}
		{// Compare with m4x4
			auto M = Random4x4(rng, -5.0f, +5.0f, v4Origin);
			Matrix<float> m(M);

			PR_CHECK(FEql(m, M), true);
			PR_CHECK(FEql(m(0, 3), M.x.w), true);
			PR_CHECK(FEql(m(3, 0), M.w.x), true);
			PR_CHECK(FEql(m(2, 2), M.z.z), true);

			PR_CHECK(IsInvertible(m) == IsInvertible(M), true);

			auto m1 = Invert(m);
			auto M1 = Invert(M);
			PR_CHECK(FEql(m1, M1), true);

			auto m2 = Transpose(m);
			auto M2 = Transpose4x4(M);
			PR_CHECK(FEql(m2, M2), true);
		}
		{// Multiply
			double data0[] = {1, 2, 3, 4, 0.1, 0.2, 0.3, 0.4, -4, -3, -2, -1};
			double data1[] = {1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4};
			double rdata[] = {30, 30, 30, 30, 30, 3, 3, 3, 3, 3, -20, -20, -20, -20, -20};
			auto a2b = Matrix<double>(3, 4, data0);
			auto b2c = Matrix<double>(4, 5, data1);
			auto A2C = Matrix<double>(3, 5, rdata);
			auto a2c = b2c * a2b;
			PR_CHECK(FEql(a2c, A2C), true);
		}
		{// Multipy
			std::uniform_real_distribution<float> dist(-5.0f, +5.0f);
			
			auto V0 = Random4(rng, -5, +5);
			auto M0 = Random4x4(rng, -5, +5);
			auto M1 = Random4x4(rng, -5, +5);

			auto v0 = Matrix<float>(V0);
			auto m0 = Matrix<float>(M0);
			auto m1 = Matrix<float>(M1);

			PR_CHECK(FEql(v0, V0), true);
			PR_CHECK(FEql(m0, M0), true);
			PR_CHECK(FEql(m1, M1), true);

			auto V2 = M0 * V0;
			auto v2 = m0 * v0;
			PR_CHECK(FEql(v2, V2), true);

			auto M2 = M0 * M1;
			auto m2 = m0 * m1;
			PR_CHECK(FEql(m2, M2), true);
		}
		{// Multiply round trip
			std::uniform_real_distribution<float> dist(-5.0f, +5.0f);
			const int SZ = 100;
			Matrix<float> m(SZ, SZ);
			for (int k = 0; k != 10; ++k)
			{
				for (int r = 0; r != m.vecs(); ++r)
					for (int c = 0; c != m.cmps(); ++c)
						m(r, c) = dist(rng);

				if (IsInvertible(m))
				{
					auto m_inv = Invert(m);

					auto i0 = Matrix<float>::Identity(SZ, SZ);
					auto i1 = m * m_inv;
					auto i2 = m_inv * m;

					PR_CHECK(FEqlRelative(i0, i1, 0.0001f), true);
					PR_CHECK(FEqlRelative(i0, i2, 0.0001f), true);

					break;
				}
			}
		}
		{// Transpose
			const int vecs = 4, cmps = 3;
			auto m = Matrix<double>::Random(rng, vecs, cmps, -5.0, 5.0);
			auto t = Transpose(m);

			PR_CHECK(m.vecs(), vecs);
			PR_CHECK(m.cmps(), cmps);
			PR_CHECK(t.vecs(), cmps);
			PR_CHECK(t.cmps(), vecs);

			for (int r = 0; r != vecs; ++r)
				for (int c = 0; c != cmps; ++c)
					PR_CHECK(m(r, c) == t(c, r), true);
		}
		{// Resizing
			auto M = Matrix<double>::Random(rng, 4, 3, -5.0, 5.0);
			auto m = M;
			auto t = Transpose(M);

			// Resizing a normal matrix adds more vectors, and preserves data
			PR_CHECK(m.vecs(), 4);
			PR_CHECK(m.cmps(), 3);
			m.resize(5);
			PR_CHECK(m.vecs(), 5);
			PR_CHECK(m.cmps(), 3);
			for (int r = 0; r != m.vecs(); ++r)
			{
				for (int c = 0; c != m.cmps(); ++c)
				{
					if (r < 4 && c < 3)
						PR_CHECK(m(r, c) == M(r, c), true);
					else
						PR_CHECK(m(r, c) == 0, true);
				}
			}

			// Resizing a transposed matrix adds more transposed vectors, and preserves data 
			PR_CHECK(t.vecs(), 3);
			PR_CHECK(t.cmps(), 4);
			t.resize(5);
			PR_CHECK(t.vecs(), 5);
			PR_CHECK(t.cmps(), 4);
			for (int r = 0; r != t.vecs(); ++r)
			{
				for (int c = 0; c != t.cmps(); ++c)
				{
					if (r < 3 && c < 4)
						PR_CHECK(t(r, c) == M(c, r), true);
					else
						PR_CHECK(t(r, c) == 0, true);
				}
			}
		}
		{// Dot Product
			auto a = Matrix<float>(1, 3, {1.0, 2.0, 3.0});
			auto b = Matrix<float>(1, 3, {3.0, 2.0, 1.0});
			auto r = Dot(a, b);
			PR_CHECK(FEql(r, 10.0f), true);
		}
	}
	PRUnitTest(IVector2Tests)
	{
	}
	PRUnitTest(IVector4Tests)
	{
	}
}
#endif
