//***********************************************************************
// 'To' functions
//  Copyright © Rylogic Ltd 2008
//***********************************************************************

#include <string>

namespace pr
{
	// Template type conversions - Add overloads and specialisations as needed
	template <typename ToType> inline ToType To(bool x)                                   { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(char x)                                   { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(short x, int radix = 10)                  { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(int x, int radix = 10)                    { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(long x, int radix = 10)                   { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(long long x)                              { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(unsigned char x, int radix = 10)          { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(unsigned short x, int radix = 10)         { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(unsigned int x, int radix = 10)           { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(unsigned long x, int radix = 10)          { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(unsigned long long x)                     { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(float x)                                  { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(double x)                                 { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(long double x)                            { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(char const* from)                         { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(wchar_t const* from)                      { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(std::string const& from)                  { static_assert(false, "No conversion from to this type available"); }
	template <typename ToType> inline ToType To(std::wstring const& from)                 { static_assert(false, "No conversion from to this type available"); }
}
