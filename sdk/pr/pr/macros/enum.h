//******************************************************************************
// Reflective enum
//  Copyright © Rylogic Ltd 2013
//******************************************************************************
// Use:
///*
//  #define PR_ENUM(x) /* the name PR_ENUM is arbitrary, you can use whatever
//     */x(A, "a", = 0)/* comment 
//     */x(B, "b", = 1)/* comment 
//     */x(C, "c", = 2)/* comment 
//  PR_DEFINE_ENUM(TestEnum1, PR_ENUM)
//  #undef PR_ENUM
//*/

#ifndef PR_MACROS_ENUM_H
#define PR_MACROS_ENUM_H

#include <exception>
#include <type_traits>

//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT
#   define PR_ASSERT_DEFINED
#   define PR_ASSERT(grp, exp, str)
#   define PR_DBG 0
#endif

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

#define PR_ENUM_FROMSTR1(id)           if (::strcmp(name, #id) == 0) return id;
#define PR_ENUM_FROMSTR2(id, val)      if (::strcmp(name, #id) == 0) return id;
#define PR_ENUM_FROMSTR3(id, str, val) if (::strcmp(name, str) == 0) return id;

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
	/* The name of the enum type */ \
	static char const* EnumName() { return #enum_name; }\
\
	/* The number of values in the enum */\
	static int const NumberOf = 0\
		enum_vals1(PR_ENUM_COUNT1)\
		enum_vals2(PR_ENUM_COUNT2)\
		enum_vals3(PR_ENUM_COUNT3);\
\
	/* The members of the enum */ \
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
		default: PR_ASSERT(PR_DBG, false, "Not a member of enum "#enum_name); return "";\
		enum_vals1(PR_ENUM_TOSTRING1)\
		enum_vals2(PR_ENUM_TOSTRING2)\
		enum_vals3(PR_ENUM_TOSTRING3)\
		}\
	}\
\
	/* Convert a string name into it's enum value (inverse of ToString)*/ \
	static Enum_ Parse(char const* name)\
	{\
		enum_vals1(PR_ENUM_FROMSTR1)\
		enum_vals2(PR_ENUM_FROMSTR2)\
		enum_vals3(PR_ENUM_FROMSTR3)\
		PR_ASSERT(PR_DBG, false, "Parse failed, no matching value in enum "#enum_name);\
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
	/* Returns an enum member by index */ \
	static Enum_ Member(int index)\
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
	/* Converts this enum value to a string */ \
	char const* ToString() const\
	{\
		return enum_name::ToString(value);\
	}\
\
	enum_name() :value() {}\
	enum_name(Enum_ x) :value(x) {}\
	notflags(explicit) enum_name(int x) :value(static_cast<Enum_>(x)) {}\
	enum_name& operator = (Enum_ x)                                                        { value = x; return *this; }\
	flags(enum_name& operator = (int x)                                                    { value = static_cast<Enum_>(x); return *this; })\
	flags(Enum_ operator | (enum_name rhs) const                                           { return static_cast<Enum_>(value | rhs.value); })\
	flags(Enum_ operator & (enum_name rhs) const                                           { return static_cast<Enum_>(value & rhs.value); })\
	flags(Enum_ operator ^ (enum_name rhs) const                                           { return static_cast<Enum_>(value ^ rhs.value); })\
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
	// Used to check enums where the value of each member should be the hash of its string name
	// Use this method by declaring a static bool and assigning it to the result of this function
	template <typename TEnum> inline bool CheckHashEnum(int (*hash_func)(char const*), void (*on_fail)(char const*) = nullptr)
	{
		bool result = true;
		if (on_fail == nullptr) on_fail = [](char const* msg){ throw std::exception(msg); };

		std::string str;
		for (int i = 0; i != TEnum::NumberOf; ++i)
		{
			auto name = TEnum::MemberName(i);
			auto val  = TEnum::Member(i);
			auto hash = hash_func(name);
			if (val != hash)
			{
				str += FmtS("\n%s::%-48s hash value should be 0x%08X", TEnum::EnumName(), name, hash);
				result = false;
			}
		}
		if (!result) on_fail(str.c_str());
		return result;
	}
}

#ifdef PR_ASSERT_DEFINED
#   undef PR_ASSERT_DEFINED
#   undef PR_ASSERT
#   undef PR_DBG
#endif

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

			PR_CHECK(a1.ToString(), "A");
			PR_CHECK(a2.ToString(), "A");
			PR_CHECK(a3.ToString(), "a");
			PR_CHECK(a4.ToString(), "A");
			PR_CHECK(a5.ToString(), "a");

			PR_CHECK(b1.ToString(), "B");
			PR_CHECK(b2.ToString(), "B");
			PR_CHECK(b3.ToString(), "b");
			PR_CHECK(b4.ToString(), "B");
			PR_CHECK(b5.ToString(), "b");

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

			PR_CHECK(42 == a2, true);                                 // Implicitly convertible from enum to int
			PR_CHECK(static_cast<TestEnum2>(43).value, TestEnum2::B); // Explicitly convertible from int to enum
			PR_THROWS(TestEnum3 c = TestEnum3::From(4), std::exception);        // invalid conversion, 4 is not an enum value

			TestEnum4 x = (TestEnum4::A | TestEnum4::B) & ~TestEnum4::C; // Flag enums can be combined and assigned

			char const* names[] = {"A","B","C"};
			TestEnum1::Enum_ values[] = {TestEnum1::A, TestEnum1::B, TestEnum1::C};
			for (int i = 0; i != TestEnum1::NumberOf; ++i)
			{
				PR_CHECK(TestEnum1::MemberName(i), names[i]);     // Access names by index
				PR_CHECK(TestEnum1::Member(i), values[i]);        // Access members by index
			}
		}
	}
}
#endif

#endif

