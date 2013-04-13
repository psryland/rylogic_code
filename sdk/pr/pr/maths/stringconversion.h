//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#ifndef PR_MATHS_STRING_CONVERSION_H
#define PR_MATHS_STRING_CONVERSION_H

#include <string>
#include "pr/maths/maths.h"
#include "pr/str/tostring.h"

namespace pr
{
	// Convert to/from std::string/char const*
	template <typename ToType> inline ToType To(v3 const& v);
	template <typename ToType> inline ToType To(v4 const& v);
	template <typename ToType> inline ToType To(m3x3 const& m);
	template <typename ToType> inline ToType To(m4x4 const& m);
	template <>                inline std::string To<std::string>(v3 const& v)   { return To<std::string>(v.x)       + " " + To<std::string>(v.y)       + " " + To<std::string>(v.z); }
	template <>                inline std::string To<std::string>(v4 const& v)   { return To<std::string>(v.xyz())   + " " + To<std::string>(v.w); }
	template <>                inline std::string To<std::string>(m3x3 const& m) { return To<std::string>(m.x.xyz()) + " " + To<std::string>(m.y.xyz()) + " " + To<std::string>(m.z.xyz()); }
	template <>                inline std::string To<std::string>(m4x4 const& m) { return To<std::string>(m.x)       + " " + To<std::string>(m.y)       + " " + To<std::string>(m.z)        + " " + To<std::string>(m.w); }
	
	template <typename ToType> inline ToType      To             (char const* s, char const** end, int radix = 10);
	template <>                inline double      To<double>     (char const* s, char const** end, int)          { return                       strtod (s, (char**)end); }
	template <>                inline double      To<double>     (char const* s)                                 { return To<double>(s, 0); }
	template <>                inline float       To<float>      (char const* s, char const** end, int)          { return static_cast<float>   (strtod (s, (char**)end)); }
	template <>                inline float       To<float>      (char const* s)                                 { return To<float>(s, 0); }
	template <>                inline int         To<int>        (char const* s, char const** end, int radix)    { return static_cast<int>     (strtol (s, (char**)end, radix)); }
	template <>                inline pr::uint    To<pr::uint>   (char const* s, char const** end, int radix)    { return static_cast<pr::uint>(strtoul(s, (char**)end, radix)); }
	template <>                inline pr::v4      To<pr::v4>     (char const* s, char const** end, int)
	{
		pr::v4 v;  float* f = v.ToArray();  char const* str = s;
		for (int i = 0; i != 4; ++i) *f++ = To<float>(str, &str);
		if (end) *end = str;
		return v;
	}
	template <>                inline pr::m4x4    To<pr::m4x4>   (char const* s, char const** end, int)
	{
		pr::m4x4 m;  pr::v4* v = m.ToArray();  char const* str = s;
		for (int i = 0; i != 4; ++i) *v++ = To<pr::v4>(str, &str);
		if (end) *end = str;
		return m;
	}
	
	// Convert an integer to a string of binary
	template <typename T> inline std::string ToBinary(T n)
	{
		int const bits = sizeof(T) * 8;
		std::string str; str.reserve(bits);
		for (int i = bits; i-- != 0;)
			str.push_back((n & Bit64(i)) ? '1' : '0');
		return str;
	}
	
	// Write a vector to a stream
	template <typename Stream> inline Stream& operator << (Stream& out, pr::v4 const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z;
	}
}

#endif
