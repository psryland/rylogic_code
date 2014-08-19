//******************************************************************************
// Reflective enum
//  Copyright (c) Rylogic Ltd 2013
//******************************************************************************
// Use:
///*
//  #define PR_ENUM(x) /* the name PR_ENUM is arbitrary, you can use whatever
//     */x(A, "a", = 0)/* comment
//     */x(B, "b", = 1)/* comment
//     */x(C, "c", = 2)/* comment
//  PR_DEFINE_ENUM3(TestEnum1, PR_ENUM)
//  #undef PR_ENUM
//*/

#pragma once
#ifndef PR_MACROS_ENUM_H
#define PR_MACROS_ENUM_H

#include <exception>
#include <functional>
#include <iostream>
#include <sstream>
#include <cassert>

// Note, enum flag operators are defined in <pr/meta/flags_enum.h>

// Macro enum generator functions
#define PR_ENUM_DEFINE1(id)           id,
#define PR_ENUM_DEFINE2(id, val)      id val,
#define PR_ENUM_DEFINE3(id, str, val) id val,

#define PR_ENUM_COUNT1(id)            + 1
#define PR_ENUM_COUNT2(id, val)       + 1
#define PR_ENUM_COUNT3(id, str, val)  + 1

#define PR_ENUM_TOSTRING1(id)           case id: return #id;
#define PR_ENUM_TOSTRING2(id, val)      case id: return #id;
#define PR_ENUM_TOSTRING3(id, str, val) case id: return str;

#define PR_ENUM_TOWSTRING1(id)           case id: return L#id;
#define PR_ENUM_TOWSTRING2(id, val)      case id: return L#id;
#define PR_ENUM_TOWSTRING3(id, str, val) case id: return L##str;

#define PR_ENUM_STRCMP1(id)            if (::strcmp(name, #id) == 0) { enum_ = id; return true; }
#define PR_ENUM_STRCMP2(id, val)       if (::strcmp(name, #id) == 0) { enum_ = id; return true; }
#define PR_ENUM_STRCMP3(id, str, val)  if (::strcmp(name, str) == 0) { enum_ = id; return true; }
#define PR_ENUM_STRCMPI1(id)           if (::_stricmp(name, #id) == 0) { enum_ = id; return true; }
#define PR_ENUM_STRCMPI2(id, val)      if (::_stricmp(name, #id) == 0) { enum_ = id; return true; }
#define PR_ENUM_STRCMPI3(id, str, val) if (::_stricmp(name, str) == 0) { enum_ = id; return true; }

#define PR_ENUM_WSTRCMP1(id)            if (::wcscmp(name, L#id) == 0) { enum_ = id; return true; }
#define PR_ENUM_WSTRCMP2(id, val)       if (::wcscmp(name, L#id) == 0) { enum_ = id; return true; }
#define PR_ENUM_WSTRCMP3(id, str, val)  if (::wcscmp(name, L##str) == 0) { enum_ = id; return true; }
#define PR_ENUM_WSTRCMPI1(id)           if (::_wcsicmp(name, L#id) == 0) { enum_ = id; return true; }
#define PR_ENUM_WSTRCMPI2(id, val)      if (::_wcsicmp(name, L#id) == 0) { enum_ = id; return true; }
#define PR_ENUM_WSTRCMPI3(id, str, val) if (::_wcsicmp(name, L##str) == 0) { enum_ = id; return true; }

#define PR_ENUM_TOTRUE1(id)           case id: return true;
#define PR_ENUM_TOTRUE2(id, val)      case id: return true;
#define PR_ENUM_TOTRUE3(id, str, val) case id: return true;

#define PR_ENUM_FIELDS1(id)           id,
#define PR_ENUM_FIELDS2(id, val)      id,
#define PR_ENUM_FIELDS3(id, str, val) id,

#define PR_ENUM_NULL(x)
#define PR_ENUM_EXPAND(x) x

// Enum generator
#define PR_DEFINE_ENUM_IMPL(enum_name, enum_vals1, enum_vals2, enum_vals3, notflags, flags)\
struct enum_name\
{\
	/* Type trait tag */\
	struct is_enum;\
\
	/* The name of the enum type */ \
	static char const* EnumName() { return #enum_name; }\
\
	/* The number of values in the enum */\
	static int const NumberOf = 0\
		enum_vals1(PR_ENUM_COUNT1)\
		enum_vals2(PR_ENUM_COUNT2)\
		enum_vals3(PR_ENUM_COUNT3);\
\
	/* The members of the enum. This can't be called 'Enum' or 'Type' because
	   it doesn't compile if the enum contains a member with the same name*/ \
	enum Enum_\
	{\
		enum_vals1(PR_ENUM_DEFINE1)\
		enum_vals2(PR_ENUM_DEFINE2)\
		enum_vals3(PR_ENUM_DEFINE3)\
	};\
\
	/* Storage for the enum value */ \
	Enum_ value;\
\
	/* Convert an enum value into its string name */ \
	static char const* ToString(Enum_ e)\
	{\
		switch (e) {\
		default: notflags(assert(false && "Not a member of enum "#enum_name);) return "";\
		enum_vals1(PR_ENUM_TOSTRING1)\
		enum_vals2(PR_ENUM_TOSTRING2)\
		enum_vals3(PR_ENUM_TOSTRING3)\
		}\
	}\
	static wchar_t const* ToWString(Enum_ e)\
	{\
		switch (e) {\
		default: notflags(assert(false && "Not a member of enum "#enum_name);) return L"";\
		enum_vals1(PR_ENUM_TOWSTRING1)\
		enum_vals2(PR_ENUM_TOWSTRING2)\
		enum_vals3(PR_ENUM_TOWSTRING3)\
		}\
	}\
\
	/* Try to convert a string name into it's enum value (inverse of ToString)*/ \
	static bool TryParse(Enum_& enum_, char const* name, bool match_case = true)\
	{\
		if (match_case)\
		{\
			enum_vals1(PR_ENUM_STRCMP1)\
			enum_vals2(PR_ENUM_STRCMP2)\
			enum_vals3(PR_ENUM_STRCMP3)\
		}\
		else\
		{\
			enum_vals1(PR_ENUM_STRCMPI1)\
			enum_vals2(PR_ENUM_STRCMPI2)\
			enum_vals3(PR_ENUM_STRCMPI3)\
		}\
		return false;\
	}\
\
	/* Convert a string name into it's enum value (inverse of ToString)*/ \
	static bool TryParse(Enum_& enum_, wchar_t const* name, bool match_case = true)\
	{\
		if (match_case)\
		{\
			enum_vals1(PR_ENUM_WSTRCMP1)\
			enum_vals2(PR_ENUM_WSTRCMP2)\
			enum_vals3(PR_ENUM_WSTRCMP3)\
		}\
		else\
		{\
			enum_vals1(PR_ENUM_WSTRCMPI1)\
			enum_vals2(PR_ENUM_WSTRCMPI2)\
			enum_vals3(PR_ENUM_WSTRCMPI3)\
		}\
		return false;\
	}\
\
	/* Convert a string name into it's enum value (inverse of ToString)*/ \
	static Enum_ Parse(char const* name, bool match_case = true)\
	{\
		Enum_ enum_;\
		if (TryParse(enum_, name, match_case)) return enum_;\
		throw std::exception("Parse failed, no matching value in enum "#enum_name);\
	}\
\
	/* Convert a string name into it's enum value (inverse of ToString)*/ \
	static Enum_ Parse(wchar_t const* name, bool match_case = true)\
	{\
		Enum_ enum_;\
		if (TryParse(enum_, name, match_case)) return enum_;\
		throw std::exception("Parse failed, no matching value in enum "#enum_name);\
	}\
\
	/* Returns true if 'val' is convertible to one of the values in this enum */ \
	template <typename T> static bool IsValue(T val)\
	{\
		switch (val) {\
		default: return false;\
		enum_vals1(PR_ENUM_TOTRUE1)\
		enum_vals2(PR_ENUM_TOTRUE2)\
		enum_vals3(PR_ENUM_TOTRUE3)\
		}\
	}\
\
	/* Convert an integral type to an enum value, throws if 'val' is not a valid value */ \
	template <typename T> static Enum_ From(T val)\
	{\
		if (!IsValue(val)) throw std::exception("value is not a valid member of enum "#enum_name);\
		return static_cast<Enum_>(val);\
	}\
\
	/* Returns the name of an enum member by index */ \
	static char const* MemberName(int index)\
	{\
		return ToString(Member(index));\
	}\
\
	/* Returns an enum member by index. (const& so that address of can be used) */ \
	static Enum_ const& Member(int index)\
	{\
		static Enum_ const map[] =\
		{\
			enum_vals1(PR_ENUM_FIELDS1)\
			enum_vals2(PR_ENUM_FIELDS2)\
			enum_vals3(PR_ENUM_FIELDS3)\
		};\
		if (index < 0 || index >= NumberOf)\
			throw std::exception("index out of range for enum "#enum_name);\
		return map[index];\
	}\
\
	/* Returns an iterator range for iterating over each element in the enum*/\
	static pr::EnumMemberEnumerator<enum_name> Members()\
	{\
		return pr::EnumMemberEnumerator<enum_name>();\
	}\
\
	/* Returns an iterator range for iterating over each element in the enum*/\
	static pr::EnumMemberNameEnumerator<enum_name> MemberNames()\
	{\
		return pr::EnumMemberNameEnumerator<enum_name>();\
	}\
\
	/* Converts this enum value to a string */ \
	char const* ToString() const\
	{\
		return enum_name::ToString(value);\
	}\
	wchar_t const* ToWString() const\
	{\
		return enum_name::ToWString(value);\
	}\
\
	enum_name() :value() {}\
	enum_name(Enum_ x) :value(x) {}\
	notflags(explicit) enum_name(int x) :value(static_cast<Enum_>(x)) {}\
	enum_name& operator = (Enum_ x)                                                        { value = x; return *this; }\
	flags(enum_name& operator = (int x)                                                    { value = static_cast<Enum_>(x); return *this; })\
	flags(enum_name& operator |= (Enum_ rhs)                                               { value = static_cast<Enum_>(value | rhs); return *this; })\
	flags(enum_name& operator &= (Enum_ rhs)                                               { value = static_cast<Enum_>(value & rhs); return *this; })\
	flags(enum_name& operator ^= (Enum_ rhs)                                               { value = static_cast<Enum_>(value ^ rhs); return *this; })\
	flags(Enum_ operator | (Enum_ rhs) const                                               { return static_cast<Enum_>(value | rhs); })\
	flags(Enum_ operator & (Enum_ rhs) const                                               { return static_cast<Enum_>(value & rhs); })\
	flags(Enum_ operator ^ (Enum_ rhs) const                                               { return static_cast<Enum_>(value ^ rhs); })\
	operator Enum_ const&() const                                                          { return value; }\
	operator Enum_&()                                                                      { return value; }\
	friend std::ostream& operator << (std::ostream& stream, enum_name const& enum_)        { return stream << enum_name::ToString(enum_); }\
	friend std::istream& operator >> (std::istream& stream, enum_name&       enum_)        { std::string s; stream >> s; enum_ = enum_name::Parse(s.c_str()); return stream; }\
	friend std::ostream& operator << (std::ostream& stream, enum_name::Enum_ const& enum_) { return stream << enum_name::ToString(enum_); }\
	friend std::istream& operator >> (std::istream& stream, enum_name::Enum_&       enum_) { std::string s; stream >> s; enum_ = enum_name::Parse(s.c_str()); return stream; }\
}

// Declares an enum where values are implicit, 'enum_vals' should be a macro with one parameter; id
#define PR_DEFINE_ENUM1(enum_name, enum_vals)            PR_DEFINE_ENUM_IMPL(enum_name, enum_vals, PR_ENUM_NULL, PR_ENUM_NULL, PR_ENUM_EXPAND, PR_ENUM_NULL)

// Declares an enum where the values are assigned explicitly, 'enum_vals' should be a macro with two paramters; id and value
#define PR_DEFINE_ENUM2(enum_name, enum_vals)            PR_DEFINE_ENUM_IMPL(enum_name, PR_ENUM_NULL, enum_vals, PR_ENUM_NULL, PR_ENUM_EXPAND, PR_ENUM_NULL)

// Declares an enum where the values are assigned explicitly and the string name of each member is explicit. 'enum_vals' should be a macro with three parameters; id, string, and value
#define PR_DEFINE_ENUM3(enum_name, enum_vals)            PR_DEFINE_ENUM_IMPL(enum_name, PR_ENUM_NULL, PR_ENUM_NULL, enum_vals, PR_ENUM_EXPAND, PR_ENUM_NULL)

// Declares a flags enum where the values are assigned explicitly, 'enum_vals' should be a macro with two paramters; id and value
#define PR_DEFINE_ENUM2_FLAGS(enum_name, enum_vals)      PR_DEFINE_ENUM_IMPL(enum_name, PR_ENUM_NULL, enum_vals, PR_ENUM_NULL, PR_ENUM_NULL, PR_ENUM_EXPAND)

// Declares a flags enum where the values are assigned explicitly and the string name of each member is explicit. 'enum_vals' should be a macro with three parameters; id, string, and value
#define PR_DEFINE_ENUM3_FLAGS(enum_name, enum_vals)      PR_DEFINE_ENUM_IMPL(enum_name, PR_ENUM_NULL, PR_ENUM_NULL, enum_vals, PR_ENUM_NULL, PR_ENUM_EXPAND)

namespace pr
{
	// A type trait for detecting PR_ENUM types
	// Use with std::enable_if, e.g.
	// template <typename TEnum> inline typename std::enable_if<pr::is_enum<TEnum>::value, char const*>::type Func(TEnum t) { return TEnum::ToString(t); }
	template <typename TEnum> struct is_enum
	{
		template <typename U> static char (&resolve(typename U::is_enum*))[2];
		template <typename U> static char resolve(...);
		enum { value = sizeof(resolve<TEnum>(0)) - 1 };
	};

	// Iterators for iterating over enum members/member names
	template <typename TEnum> struct EnumMemberIterator
	{
		int m_idx;
		explicit EnumMemberIterator(int idx) :m_idx(idx) {}
		TEnum operator *() const                        { return TEnum::Member(m_idx); }
		EnumMemberIterator& operator ++()               { ++m_idx; return *this; }
		EnumMemberIterator& operator --()               { --m_idx; return *this; }
		EnumMemberIterator operator ++(int)             { return EnumMemberIterator(m_idx++); }
		EnumMemberIterator operator --(int)             { return EnumMemberIterator(m_idx--); }
		bool operator == (EnumMemberIterator rhs) const { return m_idx == rhs.m_idx; }
		bool operator != (EnumMemberIterator rhs) const { return m_idx != rhs.m_idx; }
		bool operator <  (EnumMemberIterator rhs) const { return m_idx <  rhs.m_idx; }
	};
	template <typename TEnum> struct EnumMemberNameIterator
	{
		int m_idx;
		explicit EnumMemberNameIterator(int idx) :m_idx(idx) {}
		char const* operator *() const                      { return TEnum::MemberName(m_idx); }
		EnumMemberNameIterator& operator ++()               { ++m_idx; return *this; }
		EnumMemberNameIterator& operator --()               { --m_idx; return *this; }
		EnumMemberNameIterator operator ++(int)             { return EnumMemberNameIterator(m_idx++); }
		EnumMemberNameIterator operator --(int)             { return EnumMemberNameIterator(m_idx--); }
		bool operator == (EnumMemberNameIterator rhs) const { return m_idx == rhs.m_idx; }
		bool operator != (EnumMemberNameIterator rhs) const { return m_idx != rhs.m_idx; }
		bool operator <  (EnumMemberNameIterator rhs) const { return m_idx <  rhs.m_idx; }
	};

	// Proxy objects that allow for (TEnum e : TEnum::Members()) {}
	template <typename TEnum> struct EnumMemberEnumerator
	{
		EnumMemberIterator<TEnum> begin() const { return EnumMemberIterator<TEnum>(0); }
		EnumMemberIterator<TEnum> end() const   { return EnumMemberIterator<TEnum>(TEnum::NumberOf); }
	};
	template <typename TEnum> struct EnumMemberNameEnumerator
	{
		EnumMemberNameIterator<TEnum> begin() const { return EnumMemberNameIterator<TEnum>(0); }
		EnumMemberNameIterator<TEnum> end() const   { return EnumMemberNameIterator<TEnum>(TEnum::NumberOf); }
	};

	// Used to check enums where the value of each member should be the hash of its string name
	// Use this method by declaring a static bool and assigning it to the result of this function
	template <typename TEnum, typename THashFunc, typename TFailFunc> inline bool CheckHashEnum(THashFunc hash_func, TFailFunc on_fail)
	{
		bool result = true;
		std::stringstream ss;
		for (int i = 0; i != TEnum::NumberOf; ++i)
		{
			auto name = TEnum::MemberName(i);
			auto val  = TEnum::Member(i);
			auto hash = hash_func(name);
			if (hash != static_cast<int>(val))
			{
				ss << std::endl << TEnum::EnumName() << "::" << name << " hash value should be 0x" << std::hex << hash;
				result = false;
			}
		}
		if (!result) on_fail(ss.str().c_str());
		return result;
	}
	template <typename TEnum, typename THashFunc> inline bool CheckHashEnum(THashFunc hash_func)
	{
		return CheckHashEnum<TEnum>(hash_func, [](char const* msg)
			{
				std::cerr << msg;
				throw std::exception(msg);
			});
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		#define PR_ENUM(x) /*
			*/x(A)/*
			*/x(B)/* this is 'B'
			*/x(C)
		PR_DEFINE_ENUM1(TestEnum1, PR_ENUM);
		#undef PR_ENUM

		#define PR_ENUM(x) \
			x(A, = 42)\
			x(B, = 43) /* this is 'B' */ \
			x(C, = 44)
		PR_DEFINE_ENUM2(TestEnum2, PR_ENUM);
		#undef PR_ENUM

		#define PR_ENUM(x) \
			x(A, "a", = 0x0A)\
			x(B, "b", = 0x0B)\
			x(C, "c", = 0x0C)
		PR_DEFINE_ENUM3(TestEnum3, PR_ENUM);
		#undef PR_ENUM

		#define PR_ENUM(x) \
			x(A, = 1 << 0)\
			x(B, = 1 << 1)\
			x(C, = 1 << 2)
		PR_DEFINE_ENUM2_FLAGS(TestEnum4, PR_ENUM);
		#undef PR_ENUM

		#define PR_ENUM(x) \
			x(A, "a", = 1 << 0)\
			x(B, "b", = 1 << 1)\
			x(C, "c", = 1 << 2 | B)
		PR_DEFINE_ENUM3_FLAGS(TestEnum5, PR_ENUM);
		#undef PR_ENUM

		PRUnitTest(pr_macros_enum)
		{
			PR_CHECK(TestEnum1::NumberOf, 3);
			PR_CHECK(TestEnum2::NumberOf, 3);
			PR_CHECK(TestEnum3::NumberOf, 3);
			PR_CHECK(TestEnum4::NumberOf, 3);
			PR_CHECK(TestEnum5::NumberOf, 3);

			PR_CHECK(TestEnum1::ToString(TestEnum1::A), "A");
			PR_CHECK(TestEnum2::ToString(TestEnum2::A), "A");
			PR_CHECK(TestEnum3::ToString(TestEnum3::A), "a");
			PR_CHECK(TestEnum4::ToString(TestEnum4::A), "A");
			PR_CHECK(TestEnum5::ToString(TestEnum5::A), "a");

			PR_CHECK(TestEnum1::Parse("A"), TestEnum1::A);
			PR_CHECK(TestEnum2::Parse("A"), TestEnum2::A);
			PR_CHECK(TestEnum3::Parse("a"), TestEnum3::A);
			PR_CHECK(TestEnum4::Parse("A"), TestEnum4::A);
			PR_CHECK(TestEnum5::Parse("a"), TestEnum5::A);

			// Initialisation
			TestEnum1 a1 = TestEnum1::A;
			TestEnum2 a2 = TestEnum2::A;
			TestEnum3 a3 = TestEnum3::A;
			TestEnum4 a4 = TestEnum4::A;
			TestEnum5 a5 = TestEnum5::A;

			// Assignment
			TestEnum1 b1; b1 = TestEnum1::B;
			TestEnum2 b2; b2 = TestEnum2::B;
			TestEnum3 b3; b3 = TestEnum3::B;
			TestEnum4 b4; b4 = TestEnum4::B;
			TestEnum5 b5; b5 = TestEnum5::B;
			TestEnum4 b6; b6 = TestEnum4::B; b6 |= TestEnum4::C;
			TestEnum5 b7; b7 = TestEnum5::B; b7 |= TestEnum5::C;

			PR_CHECK(a1.ToString(), "A");
			PR_CHECK(a2.ToString(), "A");
			PR_CHECK(a3.ToString(), "a");
			PR_CHECK(a4.ToString(), "A");
			PR_CHECK(a5.ToString(), "a");

			PR_CHECK(a1.ToWString(), L"A");
			PR_CHECK(a2.ToWString(), L"A");
			PR_CHECK(a3.ToWString(), L"a");
			PR_CHECK(a4.ToWString(), L"A");
			PR_CHECK(a5.ToWString(), L"a");

			PR_CHECK(b1.ToString(), "B");
			PR_CHECK(b2.ToString(), "B");
			PR_CHECK(b3.ToString(), "b");
			PR_CHECK(b4.ToString(), "B");
			PR_CHECK(b5.ToString(), "b");
			PR_CHECK(b6.ToString(), "");
			PR_CHECK(b7.ToString(), "c");

			{
				std::stringstream s; s << a1 << a2 << a3 << a4 << a5;
				PR_CHECK(s.str().c_str(), "AAaAa"); // Stream as a name
			}
			{
				std::stringstream s; s << TestEnum1::A << TestEnum2::A << TestEnum3::A << TestEnum4::A << TestEnum5::A;
				PR_CHECK(s.str().c_str(), "AAaAa"); // Stream as a name
			}
			{
				std::stringstream s; s << TestEnum1::A;
				TestEnum1 out; s >> out;
				PR_CHECK(out.value, TestEnum1::A);
			}

			PR_CHECK(42 == a2, true);                                                    // Implicitly convertible from enum to int
			PR_CHECK(static_cast<TestEnum2>(43).value, TestEnum2::B);                    // Explicitly convertible from int to enum
			PR_THROWS([&](){ volatile int i = 4; TestEnum3::From(i); }, std::exception); // invalid conversion, 4 is not an enum value

			TestEnum4 x = (TestEnum4::A | TestEnum4::B) & ~TestEnum4::C; // Flag enums can be combined and assigned
			PR_CHECK(x == 42U, false); // Implicitly convertible to unsigned int (flags only)

			char const* names[] = {"A","B","C"};
			TestEnum1::Enum_ values[] = {TestEnum1::A, TestEnum1::B, TestEnum1::C};
			for (int i = 0; i != TestEnum1::NumberOf; ++i)
			{
				PR_CHECK(TestEnum1::MemberName(i), names[i]);     // Access names by index
				PR_CHECK(TestEnum1::Member(i), values[i]);        // Access members by index
			}

			{
				int idx = 0;
				for (TestEnum1 e : TestEnum1::Members())
					PR_CHECK(e, TestEnum1::Member(idx++));
			}
			{
				int idx = 0;
				for (auto e : TestEnum1::MemberNames())
					PR_CHECK(e, TestEnum1::MemberName(idx++));
			}
		}
	}
}
#endif

#endif
