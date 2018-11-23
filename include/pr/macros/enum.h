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

#include <exception>
#include <string>
#include <type_traits>
#include <iostream>
#include <sstream>

	#pragma region Enum field expansions
	#define PR_ENUM_NULL(x)
	#define PR_ENUM_EXPAND(x) x

	#define PR_ENUM_DEFINE1(id)           id,
	#define PR_ENUM_DEFINE2(id, val)      id val,
	#define PR_ENUM_DEFINE3(id, str, val) id val,

	#define PR_ENUM_COUNT1(id)            + 1
	#define PR_ENUM_COUNT2(id, val)       + 1
	#define PR_ENUM_COUNT3(id, str, val)  + 1

	#define PR_ENUM_TOSTRING1(id)           case E::id: return #id;
	#define PR_ENUM_TOSTRING2(id, val)      case E::id: return #id;
	#define PR_ENUM_TOSTRING3(id, str, val) case E::id: return str;

	#define PR_ENUM_TOWSTRING1(id)           case E::id: return L#id;
	#define PR_ENUM_TOWSTRING2(id, val)      case E::id: return L#id;
	#define PR_ENUM_TOWSTRING3(id, str, val) case E::id: return L##str;

	#define PR_ENUM_STRCMP1(id)            if (::strcmp(name, #id) == 0) { e = E::id; return true; }
	#define PR_ENUM_STRCMP2(id, val)       if (::strcmp(name, #id) == 0) { e = E::id; return true; }
	#define PR_ENUM_STRCMP3(id, str, val)  if (::strcmp(name, str) == 0) { e = E::id; return true; }
	#define PR_ENUM_STRCMPI1(id)           if (::_stricmp(name, #id) == 0) { e = E::id; return true; }
	#define PR_ENUM_STRCMPI2(id, val)      if (::_stricmp(name, #id) == 0) { e = E::id; return true; }
	#define PR_ENUM_STRCMPI3(id, str, val) if (::_stricmp(name, str) == 0) { e = E::id; return true; }

	#define PR_ENUM_WSTRCMP1(id)            if (::wcscmp(name, L#id  ) == 0) { e = E::id; return true; }
	#define PR_ENUM_WSTRCMP2(id, val)       if (::wcscmp(name, L#id  ) == 0) { e = E::id; return true; }
	#define PR_ENUM_WSTRCMP3(id, str, val)  if (::wcscmp(name, L##str) == 0) { e = E::id; return true; }
	#define PR_ENUM_WSTRCMPI1(id)           if (::_wcsicmp(name, L#id  ) == 0) { e = E::id; return true; }
	#define PR_ENUM_WSTRCMPI2(id, val)      if (::_wcsicmp(name, L#id  ) == 0) { e = E::id; return true; }
	#define PR_ENUM_WSTRCMPI3(id, str, val) if (::_wcsicmp(name, L##str) == 0) { e = E::id; return true; }

	#define PR_ENUM_TOTRUE1(id)           case E::id: return true;
	#define PR_ENUM_TOTRUE2(id, val)      case E::id: return true;
	#define PR_ENUM_TOTRUE3(id, str, val) case E::id: return true;

	#define PR_ENUM_FIELDS1(id)           E::id,
	#define PR_ENUM_FIELDS2(id, val)      E::id,
	#define PR_ENUM_FIELDS3(id, str, val) E::id,
	#pragma endregion

	// Enum generator
	#pragma region Enum Generator
	#define PR_DEFINE_ENUM_IMPL(enum_name, enum_vals1, enum_vals2, enum_vals3, notflags, flags, base_type)\
	enum class enum_name base_type\
	{\
		enum_vals1(PR_ENUM_DEFINE1)\
		enum_vals2(PR_ENUM_DEFINE2)\
		enum_vals3(PR_ENUM_DEFINE3)\
	};\
	struct enum_name##_\
	{\
		/* The name of the enum as a string */\
		template <typename Char> static constexpr Char const* Name()\
		{\
			if constexpr (std::is_same_v<Char,char   >) return #enum_name;\
			if constexpr (std::is_same_v<Char,wchar_t>) return L#enum_name;\
		}\
		static constexpr char const*    NameA() { return Name<char>(); }\
		static constexpr wchar_t const* NameW() { return Name<wchar_t>(); }\
\
		/* The number of values in the enum */\
		static int const NumberOf = 0\
			enum_vals1(PR_ENUM_COUNT1)\
			enum_vals2(PR_ENUM_COUNT2)\
			enum_vals3(PR_ENUM_COUNT3);\
\
		/* Return an enum member as a string */\
		template <typename Char> static constexpr Char const* ToString(enum_name e)\
		{\
			using E = enum_name;\
			if constexpr (std::is_same_v<Char,char>)\
			{\
				switch (e) {\
				default: return "";\
				enum_vals1(PR_ENUM_TOSTRING1)\
				enum_vals2(PR_ENUM_TOSTRING2)\
				enum_vals3(PR_ENUM_TOSTRING3)\
				}\
			}\
			if constexpr (std::is_same_v<Char,wchar_t>)\
			{\
				switch (e) {\
				default: return L"";\
				enum_vals1(PR_ENUM_TOWSTRING1)\
				enum_vals2(PR_ENUM_TOWSTRING2)\
				enum_vals3(PR_ENUM_TOWSTRING3)\
				}\
			}\
		}\
		static constexpr char const*    ToStringA(enum_name e) { return ToString<char>(e); }\
		static constexpr wchar_t const* ToStringW(enum_name e) { return ToString<wchar_t>(e); }\
\
		/* Try to convert a string name into it's enum value (inverse of ToString)*/ \
		template <typename Char> static bool TryParse(enum_name& e, Char const* name, bool match_case = true)\
		{\
			using E = enum_name;\
			if constexpr (std::is_same_v<Char,char>)\
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
			if constexpr (std::is_same_v<Char,wchar_t>)\
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
		}\
\
		/* Convert a string name into it's enum value (inverse of ToString)*/ \
		template <typename Char> static enum_name Parse(Char const* name, bool match_case = true)\
		{\
			enum_name r;\
			if (TryParse(r, name, match_case)) return r;\
			throw std::exception("Parse failed, no matching value in enum "#enum_name);\
		}\
\
		/* Returns true if 'val' is convertible to one of the values in this enum */ \
		template <typename T> static constexpr bool IsValue(T val)\
		{\
			using E = enum_name;\
			switch (val) {\
			default: return false;\
			enum_vals1(PR_ENUM_TOTRUE1)\
			enum_vals2(PR_ENUM_TOTRUE2)\
			enum_vals3(PR_ENUM_TOTRUE3)\
			}\
		}\
\
		/* Convert an integral type to an enum value, throws if 'val' is not a valid value */ \
		template <typename T> static constexpr enum_name From(T val)\
		{\
			if (!IsValue(val)) throw std::exception("value is not a valid member of enum "#enum_name);\
			return static_cast<enum_name>(val);\
		}\
\
		/* Return an enum member by index. (const& so that address of can be used) */ \
		static enum_name const& Member(int index)\
		{\
			using E = enum_name;\
			static enum_name const map[] =\
			{\
				enum_vals1(PR_ENUM_FIELDS1)\
				enum_vals2(PR_ENUM_FIELDS2)\
				enum_vals3(PR_ENUM_FIELDS3)\
			};\
			if (index < 0 || index >= NumberOf) throw std::exception("index out of range for enum "#enum_name);\
			return map[index];\
		}\
\
		/* Return the name of an enum member by index */ \
		template <typename Char> static constexpr Char const* MemberName(int index)\
		{\
			return ToString<Char>(Member(index));\
		}\
		static constexpr char const*    MemberNameA(int index) { return MemberName<char   >(index); }\
		static constexpr wchar_t const* MemberNameW(int index) { return MemberName<wchar_t>(index); }\
\
		/* Returns an iterator range for iterating over each element in the enum*/\
		static std::initializer_list<enum_name> Members()\
		{\
			auto b = &Member(0);\
			return std::initializer_list<enum_name>(b, b + NumberOf);\
		}\
	};\
\
	/* Stream the enum value to/from basic streams */ \
	template <typename Char> std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& stream, enum_name e)\
	{\
		return stream << enum_name##_::ToString<Char>(e);\
	}\
	template <typename Char> std::basic_istream<Char>& operator >> (std::basic_istream<Char>& stream, enum_name& e)\
	{\
		std::string s; stream >> s;\
		e = enum_name##_::Parse(s.c_str());\
		return stream;\
	}\
\
	/* Try to convert a string name into it's enum value (inverse of ToString)*/ \
	template <typename Char> inline constexpr Char const* ToString(enum_name e)\
	{\
		return enum_name##_::ToString<Char>(e);\
	}\
	inline constexpr char const* ToStringA(enum_name e)\
	{\
		return enum_name##_::ToStringA(e);\
	}\
	inline constexpr wchar_t const* ToStringW(enum_name e)\
	{\
		return enum_name##_::ToStringW(e);\
	}\
\
	/* Try to convert a string name into it's enum value (inverse of ToString)*/ \
	template <typename Char> inline bool TryParse(enum_name& e, Char const* name, bool match_case = true)\
	{\
		return enum_name##_::TryParse(e, name, match_case);\
	}\
\
	/* reflected enum type trait functions*/\
	std::true_type pr_impl_is_reflected_enum(enum_name*);\
	enum_name##_   pr_impl_get_enum_metadata(enum_name*);\

	// Declares an enum where values are implicit, 'enum_vals' should be a macro with one parameter; id
	#define PR_DEFINE_ENUM1(enum_name, enum_vals)                 PR_DEFINE_ENUM_IMPL(enum_name, enum_vals, PR_ENUM_NULL, PR_ENUM_NULL, PR_ENUM_EXPAND, PR_ENUM_NULL, )
	#define PR_DEFINE_ENUM1_BASE(enum_name, enum_vals, base_type) PR_DEFINE_ENUM_IMPL(enum_name, enum_vals, PR_ENUM_NULL, PR_ENUM_NULL, PR_ENUM_EXPAND, PR_ENUM_NULL, :base_type)

	// Declares an enum where the values are assigned explicitly, 'enum_vals' should be a macro with two parameters; id and value
	#define PR_DEFINE_ENUM2(enum_name, enum_vals)                 PR_DEFINE_ENUM_IMPL(enum_name, PR_ENUM_NULL, enum_vals, PR_ENUM_NULL, PR_ENUM_EXPAND, PR_ENUM_NULL, )
	#define PR_DEFINE_ENUM2_BASE(enum_name, enum_vals, base_type) PR_DEFINE_ENUM_IMPL(enum_name, PR_ENUM_NULL, enum_vals, PR_ENUM_NULL, PR_ENUM_EXPAND, PR_ENUM_NULL, :base_type)

	// Declares an enum where the values are assigned explicitly and the string name of each member is explicit. 'enum_vals' should be a macro with three parameters; id, string, and value
	#define PR_DEFINE_ENUM3(enum_name, enum_vals)                 PR_DEFINE_ENUM_IMPL(enum_name, PR_ENUM_NULL, PR_ENUM_NULL, enum_vals, PR_ENUM_EXPAND, PR_ENUM_NULL, )
	#define PR_DEFINE_ENUM3_BASE(enum_name, enum_vals, base_type) PR_DEFINE_ENUM_IMPL(enum_name, PR_ENUM_NULL, PR_ENUM_NULL, enum_vals, PR_ENUM_EXPAND, PR_ENUM_NULL, :base_type)

	#pragma endregion

namespace pr
{
	std::false_type pr_impl_is_reflected_enum(void*);
	std::false_type pr_impl_get_enum_metadata(void*);

	// Enabler for reflected enums
	template <typename T> using is_reflected_enum = decltype(pr_impl_is_reflected_enum((T*)nullptr));
	template <typename T> using enable_if_reflected_enum = typename std::enable_if<is_reflected_enum<T>::value>::type;

	// Common interface for accessing meta data for an enum
	template <typename T> using Enum = decltype(pr_impl_get_enum_metadata((T*)nullptr));
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/flags_enum.h"
namespace pr::common
{
	namespace unittests::enum2
	{
		// Normal enum
		enum class TestEnum0
		{
			A,
			B,
			C,
		};
		static_assert(!is_reflected_enum<TestEnum0>::value, "");

		#define PR_ENUM(x) /*
			*/x(A)/*
			*/x(B)/* this is 'B'
			*/x(C)
		PR_DEFINE_ENUM1(TestEnum1, PR_ENUM);
		#undef PR_ENUM
		static_assert(is_reflected_enum<TestEnum1>::value, "");

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
			x(C, = 1 << 2)\
			x(_bitwise_operators_allowed, )
		PR_DEFINE_ENUM2(TestEnum4, PR_ENUM);
		#undef PR_ENUM

		#define PR_ENUM(x) \
			x(A, "a", = 1 << 0)\
			x(B, "b", = 1 << 1)\
			x(C, "c", = 1 << 2 | B)\
			x(_bitwise_operators_allowed,"",)
		PR_DEFINE_ENUM3(TestEnum5, PR_ENUM);
		#undef PR_ENUM
	}
	PRUnitTest(Enum2Tests)
	{
		using namespace unittests::enum2;

		PR_CHECK(Enum<TestEnum1>::NameA(), "TestEnum1");
		PR_CHECK(Enum<TestEnum2>::NameA(), "TestEnum2");
		PR_CHECK(Enum<TestEnum3>::NameA(), "TestEnum3");
		PR_CHECK(Enum<TestEnum4>::NameA(), "TestEnum4");
		PR_CHECK(Enum<TestEnum5>::NameA(), "TestEnum5");

		PR_CHECK(Enum<TestEnum1>::NumberOf, 3);
		PR_CHECK(Enum<TestEnum2>::NumberOf, 3);
		PR_CHECK(Enum<TestEnum3>::NumberOf, 3);
		PR_CHECK(Enum<TestEnum4>::NumberOf, 4);
		PR_CHECK(Enum<TestEnum5>::NumberOf, 4);

		PR_CHECK(Enum<TestEnum1>::ToStringA(TestEnum1::A), "A");
		PR_CHECK(Enum<TestEnum2>::ToStringA(TestEnum2::A), "A");
		PR_CHECK(Enum<TestEnum3>::ToStringA(TestEnum3::A), "a");
		PR_CHECK(Enum<TestEnum4>::ToStringA(TestEnum4::A), "A");
		PR_CHECK(Enum<TestEnum5>::ToStringA(TestEnum5::A), "a");

		PR_CHECK(Enum<TestEnum1>::Parse("A"), TestEnum1::A);
		PR_CHECK(Enum<TestEnum2>::Parse("A"), TestEnum2::A);
		PR_CHECK(Enum<TestEnum3>::Parse("a"), TestEnum3::A);
		PR_CHECK(Enum<TestEnum4>::Parse("A"), TestEnum4::A);
		PR_CHECK(Enum<TestEnum5>::Parse("a"), TestEnum5::A);

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
			PR_CHECK(out, TestEnum1::A);
		}
		PR_THROWS([&]()
		{
			volatile int i = 4;
			Enum<TestEnum3>::From(i); // invalid conversion, 4 is not an enum value
		}, std::exception);

		char const* names[] = {"A","B","C"};
		TestEnum1 values[] = {TestEnum1::A, TestEnum1::B, TestEnum1::C};
		for (int i = 0; i != Enum<TestEnum1>::NumberOf; ++i)
		{
			PR_CHECK(Enum<TestEnum1>::MemberNameA(i), names[i]); // Access names by index
			PR_CHECK(Enum<TestEnum1>::Member(i), values[i]);     // Access members by index
		}

		// Enumerate members
		{
			int idx = 0;
			for (auto e : Enum<TestEnum1>::Members())
			{
				PR_CHECK(e, Enum<TestEnum1>::Member(idx));
				PR_CHECK(ToStringA(e), Enum<TestEnum1>::MemberNameA(idx));
				++idx;
			}
		}
	}
}
#endif