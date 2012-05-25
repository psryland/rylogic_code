#ifndef DECLARE
#define DECLARE(name)
#endif
DECLARE(name1)
DECLARE(name2)
DECLARE(name3)
DECLARE(name4)
#undef DECLARE

#ifndef ENUM_HEADER_H
#define ENUM_HEADER_H

namespace EEnum
{
	enum Type
	{
		#define DECLARE(name) name,
		#include "this_file.h"
		NumberOf
	};

	inline char const* EnumToName(Type id)
	{
		switch (id)
		{
		default:            return "";
		#define DECLARE(name) case name: return #name;
		#include "this_file.h"
		}
	}
	template <typename Enum> Enum NameToEnum(char const* name);
	template <> inline Type NameToEnum(char const* name)
	{
		int i;
		for (i = 0; i != NumberOf && _stricmp(name, EnumToName((Type)i)) != 0; ++i) {}
		return static_cast<Type>(i);
	}
}

#endif
