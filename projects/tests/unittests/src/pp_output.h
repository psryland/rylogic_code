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

#define PR_ENUM(x) \
			x(A, "a", = 0x0A)\
			x(B, "b", = 0x0B)\
			x(C, "c", = 0x0C)
		PR_DEFINE_ENUM3(TestEnum3, PR_ENUM);
		#undef PR_ENUM