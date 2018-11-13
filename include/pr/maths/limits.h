//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/vector2.h"
#include "pr/maths/vector3.h"
#include "pr/maths/vector4.h"
#include "pr/maths/vector8.h"
#include "pr/maths/ivector2.h"
#include "pr/maths/ivector4.h"
#include "pr/maths/matrix2x2.h"
#include "pr/maths/matrix3x4.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/constants_vector.h"

namespace std
{
	template <typename T>
	class numeric_limits<pr::Vec2<T>>
	{
	public:
		static pr::Vec2<T> min() throw()     { return pr::v2Min; }
		static pr::Vec2<T> max() throw()     { return pr::v2Max; }
		static pr::Vec2<T> lowest() throw()  { return pr::v2Lowest; }
		static pr::Vec2<T> epsilon() throw() { return pr::v2Epsilon; }

		static const bool is_specialized = true;
		static const bool is_signed = true;
		static const bool is_integer = false;
		static const bool is_exact = false;
		static const bool has_infinity = false;
		static const bool has_quiet_NaN = false;
		static const bool has_signaling_NaN = false;
		static const bool has_denorm_loss = true;
		static const float_denorm_style has_denorm = denorm_present;
		static const int radix = 10;
	};

	template <typename T>
	class numeric_limits<pr::Vec3<T>>
	{
	public:
		static pr::Vec3<T> min() throw()     { return pr::v3Min; }
		static pr::Vec3<T> max() throw()     { return pr::v3Max; }
		static pr::Vec3<T> lowest() throw()  { return pr::v3Lowest; }
		static pr::Vec3<T> epsilon() throw() { return pr::v3Epsilon; }

		static const bool is_specialized = true;
		static const bool is_signed = true;
		static const bool is_integer = false;
		static const bool is_exact = false;
		static const bool has_infinity = false;
		static const bool has_quiet_NaN = false;
		static const bool has_signaling_NaN = false;
		static const bool has_denorm_loss = true;
		static const float_denorm_style has_denorm = denorm_present;
		static const int radix = 10;
	};

	template <typename T>
	class numeric_limits<pr::Vec4<T>>
	{
	public:
		static pr::Vec4<T> min() throw()     { return pr::v4Min; }
		static pr::Vec4<T> max() throw()     { return pr::v4Max; }
		static pr::Vec4<T> lowest() throw()  { return pr::v4Lowest; }
		static pr::Vec4<T> epsilon() throw() { return pr::v4Epsilon; }

		static const bool is_specialized = true;
		static const bool is_signed = true;
		static const bool is_integer = false;
		static const bool is_exact = false;
		static const bool has_infinity = false;
		static const bool has_quiet_NaN = false;
		static const bool has_signaling_NaN = false;
		static const bool has_denorm_loss = true;
		static const float_denorm_style has_denorm = denorm_present;
		static const int radix = 10;
	};

	template <typename A, typename B> 
	class std::numeric_limits<pr::Mat2x2<A,B>>
	{
	public:
		static pr::Mat2x2<A,B> min() throw()     { return pr::m2x2Min; }
		static pr::Mat2x2<A,B> max() throw()     { return pr::m2x2Max; }
		static pr::Mat2x2<A,B> lowest() throw()  { return pr::m2x2Lowest; }
		static pr::Mat2x2<A,B> epsilon() throw() { return pr::m2x2Epsilon; }

		static const bool is_specialized = true;
		static const bool is_signed = true;
		static const bool is_integer = false;
		static const bool is_exact = false;
		static const bool has_infinity = false;
		static const bool has_quiet_NaN = false;
		static const bool has_signaling_NaN = false;
		static const bool has_denorm_loss = true;
		static const float_denorm_style has_denorm = denorm_present;
		static const int radix = 10;
	};


	template <typename A, typename B> 
	class std::numeric_limits<pr::Mat3x4<A,B>>
	{
	public:
		static pr::Mat3x4<A,B> min() throw()     { return pr::m3x4Min; }
		static pr::Mat3x4<A,B> max() throw()     { return pr::m3x4Max; }
		static pr::Mat3x4<A,B> lowest() throw()  { return pr::m3x4Lowest; }
		static pr::Mat3x4<A,B> epsilon() throw() { return pr::m3x4Epsilon; }

		static const bool is_specialized = true;
		static const bool is_signed = true;
		static const bool is_integer = false;
		static const bool is_exact = false;
		static const bool has_infinity = false;
		static const bool has_quiet_NaN = false;
		static const bool has_signaling_NaN = false;
		static const bool has_denorm_loss = true;
		static const float_denorm_style has_denorm = denorm_present;
		static const int radix = 10;
	};

	template <typename A, typename B> 
	class std::numeric_limits<pr::Mat4x4<A,B>>
	{
	public:
		static pr::Mat4x4<A,B> min() throw()     { return pr::m4x4Min; }
		static pr::Mat4x4<A,B> max() throw()     { return pr::m4x4Max; }
		static pr::Mat4x4<A,B> lowest() throw()  { return pr::m4x4Lowest; }
		static pr::Mat4x4<A,B> epsilon() throw() { return pr::m4x4Epsilon; }

		static const bool is_specialized = true;
		static const bool is_signed = true;
		static const bool is_integer = false;
		static const bool is_exact = false;
		static const bool has_infinity = false;
		static const bool has_quiet_NaN = false;
		static const bool has_signaling_NaN = false;
		static const bool has_denorm_loss = true;
		static const float_denorm_style has_denorm = denorm_present;
		static const int radix = 10;
	};

	template <typename T>
	class numeric_limits<pr::IVec2<T>>
	{
	public:
		static pr::IVec2<T> min() throw()     { return pr::iv2Min; }
		static pr::IVec2<T> max() throw()     { return pr::iv2Max; }
		static pr::IVec2<T> lowest() throw()  { return pr::iv2Lowest; }

		static const bool is_specialized = true;
		static const bool is_signed = true;
		static const bool is_integer = true;
		static const bool is_exact = true;
		static const bool has_infinity = false;
		static const bool has_quiet_NaN = false;
		static const bool has_signaling_NaN = false;
		static const bool has_denorm_loss = false;
		static const float_denorm_style has_denorm = denorm_absent;
		static const int radix = 10;
	};

	template <typename T>
	class numeric_limits<pr::IVec4<T>>
	{
	public:
		static pr::IVec4<T> min() throw()     { return pr::iv4Min; }
		static pr::IVec4<T> max() throw()     { return pr::iv4Max; }
		static pr::IVec4<T> lowest() throw()  { return pr::iv4Lowest; }

		static const bool is_specialized = true;
		static const bool is_signed = true;
		static const bool is_integer = true;
		static const bool is_exact = true;
		static const bool has_infinity = false;
		static const bool has_quiet_NaN = false;
		static const bool has_signaling_NaN = false;
		static const bool has_denorm_loss = false;
		static const float_denorm_style has_denorm = denorm_absent;
		static const int radix = 10;
	};
}
