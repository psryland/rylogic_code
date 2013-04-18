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
	enum Enum\
	{\
		enum_vals1(PR_ENUM_DEFINE1)\
		enum_vals2(PR_ENUM_DEFINE2)\
		enum_vals3(PR_ENUM_DEFINE3)\
	};\
\
	/* Storage for the enum value */ \
	Enum value;\
\
	/* Convert an enum value into its string name */ \
	static char const* ToString(Enum e)\
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
	static Enum Parse(char const* name)\
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
	template <typename T> static Enum From(T val)\
	{\
		if (!IsValue(val)) throw std::exception("value is not a valid member of enum "#enum_name);\
		return static_cast<Enum>(val);\
	}\
\
	/* Returns the name of an enum member by index */ \
	static char const* MemberName(int index)\
	{\
		return ToString(Member(index));\
	}\
\
	/* Returns an enum member by index */ \
	static Enum Member(int index)\
	{\
		static Enum const map[] =\
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

		// C keywords
		#define PR_ENUM(x)\
			x(Invalid  ,""         ,= 0xffffffff)\
			x(Auto     ,"auto"     ,= 0x112746e9)\
			x(Double   ,"double"   ,= 0x1840d9ce)\
			x(Int      ,"int"      ,= 0x164a43dd)\
			x(Struct   ,"struct"   ,= 0x0f408d2a)\
			x(Break    ,"break"    ,= 0x1ac013ec)\
			x(Else     ,"else"     ,= 0x1d237859)\
			x(Long     ,"long"     ,= 0x14ef7164)\
			x(Switch   ,"switch"   ,= 0x13c0233f)\
			x(Case     ,"case"     ,= 0x18ea7f00)\
			x(Enum     ,"enum"     ,= 0x113f6121)\
			x(Register ,"register" ,= 0x1a14aae9)\
			x(Typedef  ,"typedef"  ,= 0x1b494818)\
			x(Char     ,"char"     ,= 0x1e5760f8)\
			x(Extern   ,"extern"   ,= 0x16497b3b)\
			x(Return   ,"return"   ,= 0x0a01f36e)\
			x(Union    ,"union"    ,= 0x1e57f369)\
			x(Const    ,"const"    ,= 0x036f03e1)\
			x(Float    ,"float"    ,= 0x176b5be3)\
			x(Short    ,"short"    ,= 0x1edc8c0f)\
			x(Unsigned ,"unsigned" ,= 0x186a2b87)\
			x(Continue ,"continue" ,= 0x1e46a876)\
			x(For      ,"for"      ,= 0x0e37a24a)\
			x(Signed   ,"signed"   ,= 0x00bf0c54)\
			x(Void     ,"void"     ,= 0x1a9b029d)\
			x(Default  ,"default"  ,= 0x1c8cdd40)\
			x(Goto     ,"goto"     ,= 0x04d53061)\
			x(Sizeof   ,"sizeof"   ,= 0x1429164b)\
			x(Volatile ,"volatile" ,= 0x18afc4c2)\
			x(Do       ,"do"       ,= 0x1d8b5fef)\
			x(If       ,"if"       ,= 0x1dfa87fc)\
			x(Static   ,"static"   ,= 0x16150ce7)\
			x(While    ,"while"    ,= 0x0b4669dc)
		PR_DEFINE_ENUM3(EKeyword, PR_ENUM);
		#undef PR_ENUM