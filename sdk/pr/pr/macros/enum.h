//******************************************************************************
// Reflective enum
//  Copyright © Rylogic Ltd 2013
//******************************************************************************
// Use:
//	#define PR_ENUM(x)\    //the name PR_ENUM is arbitrary, you can use whatever
//		x(A, = 0)  /* comment */ \
//		x(B, = 1)  /* comment */ \
//		x(C, = 2)  /* comment */ 
//	PR_DECLARE_ENUM(TestEnum1, PR_ENUM)
//	#undef PR_ENUM

#ifndef PR_MACROS_ENUM_H
#define PR_MACROS_ENUM_H

#include <exception>

//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT
#   define PR_ASSERT_DEFINED
#   define PR_ASSERT(grp, exp, str)
#   define PR_DBG 0
#endif

// Macro enum generation
#define PR_ENUM_DEFINE(id, val) id val,
#define PR_ENUM_COUNT(id, val) + 1
#define PR_ENUM_FIELDS(id, val) id,
#define PR_ENUM_TOSTRING(id, val) case id: return #id;
#define PR_ENUM_FROMSTR(id, val) if (::strcmp(str, #id) == 0) return id;
#define PR_ENUM_TOTRUE(id, val) case id: return true;
#define PR_ENUM_NULL(x)
#define PR_ENUM_EXPAND(x) x

// Enum generator
#define PR_DECLARE_ENUM(enum_name, enum_vals)       PR_DECLARE_ENUM_IMPL(enum_name, enum_vals, PR_ENUM_EXPAND, PR_ENUM_NULL)
#define PR_DECLARE_ENUM_FLAGS(enum_name, enum_vals) PR_DECLARE_ENUM_IMPL(enum_name, enum_vals, PR_ENUM_NULL, PR_ENUM_EXPAND)
#define PR_DECLARE_ENUM_IMPL(enum_name, enum_vals, notflags, flags)\
struct enum_name\
{\
	/* The name of the enum type */ \
	static char const* EnumName() { return #enum_name; }\
	/* The number of values in the enum*/\
	static int const NumberOf = 0 enum_vals(PR_ENUM_COUNT);\
	/* The members of the enum */ \
	enum Enum\
	{\
		enum_vals(PR_ENUM_DEFINE)\
	};\
	/* Storage for the enum value */ \
	Enum value;\
	/* Convert an enum value into its string name */ \
	static char const* ToString(Enum e)\
	{\
		switch (e) {\
		default: PR_ASSERT(PR_DBG, false, "Not a member of enum "#enum_name); return "";\
		enum_vals(PR_ENUM_TOSTRING)\
		}\
	}\
	/* Convert a string name into it's enum value */ \
	static Enum Parse(char const* str)\
	{\
		enum_vals(PR_ENUM_FROMSTR)\
		PR_ASSERT(PR_DBG, false, "Parse failed, no matching value in enum "#enum_name);\
		throw std::exception("Parse failed, no matching value in enum "#enum_name);\
	}\
	/* Returns true if 'val' is convertible to one of the values in this enum */ \
	template <typename T> static bool IsValue(T val)\
	{\
		switch (val) {\
		default: return false;\
		enum_vals(PR_ENUM_TOTRUE)\
		}\
	}\
	/* Convert an integral type to an enum value, throws if 'val' is not a valid value */ \
	template <typename T> static Enum From(T val)\
	{\
		if (!IsValue(val)) throw std::exception("value is not convertable to enum "#enum_name);\
		return static_cast<Enum>(val);\
	}\
	/* Returns the name of an enum member by index */ \
	static char const* MemberName(int index)\
	{\
		return ToString(Member(index));\
	}\
	/* Returns an enum member by index */ \
	static Enum Member(int index)\
	{\
		static Enum const map[] = {enum_vals(PR_ENUM_FIELDS)};\
		if (index < 0 || index >= NumberOf)\
			throw std::exception("index out of range for enum "#enum_name);\
		return map[index];\
	}\
	/* Converts this enum value to a string */ \
	char const* ToString() const\
	{\
		return enum_name::ToString(value);\
	}\
	enum_name() :value() {}\
	enum_name(Enum x) :value(x) {}\
	notflags(explicit) enum_name(int x) :value(static_cast<Enum>(x)) {}\
	enum_name& operator = (Enum x)                                                        { value = x; return *this; }\
	flags(enum_name& operator = (int x)                                                   { value = static_cast<Enum>(x); return *this; })\
	flags(Enum operator | (enum_name rhs) const                                           { return static_cast<Enum>(value | rhs.value); })\
	flags(Enum operator & (enum_name rhs) const                                           { return static_cast<Enum>(value & rhs.value); })\
	flags(Enum operator ^ (enum_name rhs) const                                           { return static_cast<Enum>(value ^ rhs.value); })\
	operator Enum const&() const                                                          { return value; }\
	operator Enum&()                                                                      { return value; }\
	friend std::ostream& operator << (std::ostream& stream, enum_name const& enum_)       { return stream << enum_name::ToString(enum_); }\
	friend std::istream& operator >> (std::istream& stream, enum_name&       enum_)       { std::string s; stream >> s; enum_ = enum_name::Parse(s.c_str()); return stream; }\
	friend std::ostream& operator << (std::ostream& stream, enum_name::Enum const& enum_) { return stream << enum_name::ToString(enum_); }\
	friend std::istream& operator >> (std::istream& stream, enum_name::Enum&       enum_) { std::string s; stream >> s; enum_ = enum_name::Parse(s.c_str()); return stream; }\
}

namespace pr
{
	// Used to check enums where the value of each member should be the hash of its string name
	// Use this method by declaring a static bool and assigning it to the result of this function
	template <typename TEnum> inline bool CheckHashEnum(int (*hash_func)(char const*))
	{
		(void)hash_func;
		bool result = true;

		std::string str;
		for (int i = 0; i != TEnum::NumberOf; ++i)
		{
			auto name = TEnum::MemberName(i);
			auto val  = TEnum::Member(i);
			auto hash = hash_func(name);
			if (val != hash)
			{
				str += FmtS("%s::%-16s hash value should be 0x%08X\n", TEnum::EnumName(), name, hash);
				result = false;
			}
		}
		
		if (!result) throw std::exception(str.c_str());
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
	#define PR_ENUM(x) \
		x(A, = 42)\
		x(B, = 43) /* this is 'B' */ \
		x(C, = 44)
	PR_DECLARE_ENUM(TestEnum1, PR_ENUM);
	#undef PR_ENUM

	#define PR_ENUM(x) \
		x(A, = 1 << 0)\
		x(B, = 1 << 1)\
		x(C, = 2 << 2)
	PR_DECLARE_ENUM_FLAGS(TestEnum2, PR_ENUM);
	#undef PR_ENUM

	PRUnitTest(EnumGeneration)
	{
		PR_CHECK(TestEnum1::NumberOf, 3);
		PR_CHECK(TestEnum2::NumberOf, 3);

		PR_CHECK(TestEnum1::ToString(TestEnum1::A), "A");
		PR_CHECK(TestEnum1::Parse("A"), TestEnum1::A);

		PR_CHECK(TestEnum2::ToString(TestEnum2::B), "B");
		PR_CHECK(TestEnum2::Parse("B"), TestEnum2::B);

		TestEnum1 a = TestEnum1::A;    // Initialisation
		TestEnum1 b; b = TestEnum1::B; // Assignment

		PR_CHECK(a.ToString(), "A");
		PR_CHECK(b.ToString(), "B");

		{
			std::stringstream s;
			s << a;
			PR_CHECK(s.str().c_str(), "A"); // Stream as a name
		}
		{
			std::stringstream s;
			s << TestEnum1::A;
			PR_CHECK(s.str().c_str(), "A"); // Stream as a name
		}
		{
			std::stringstream s;
			s << TestEnum1::A;
			s >> b;
			PR_CHECK(b.value, TestEnum1::A);
		}

		PR_CHECK(42 == a, true);                                  // Implicitly convertible from enum to int
		PR_CHECK(static_cast<TestEnum1>(43).value, TestEnum1::B); // Explicitly convertible from int to enum
		PR_THROWS(TestEnum1 c = TestEnum1::From(4), true);        // invalid conversion, 4 is not an enum value

		TestEnum2 x = (TestEnum2::A | TestEnum2::B) & ~TestEnum2::C; // Flag enums can be combined and assigned

		char const* names[] = {"A","B","C"};
		TestEnum1::Enum values[] = {TestEnum1::A, TestEnum1::B, TestEnum1::C};
		for (int i = 0; i != TestEnum1::NumberOf; ++i)
		{
			PR_CHECK(TestEnum1::MemberName(i), names[i]);     // Access names by index
			PR_CHECK(TestEnum1::Member(i), values[i]);        // Access members by index
		}
	}
}
#endif

#endif

