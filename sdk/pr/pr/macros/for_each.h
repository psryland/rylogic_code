//******************************************
// for_each macro
//  Copyright (c) Rylogic Ltd 2010
//******************************************

#ifndef PR_MACRO_FOR_EACH_H
#define PR_MACRO_FOR_EACH_H

#include "pr/meta/type_of.h"

// non-const forward for each
#define FOR_EACH(var, cont)\
	for (typeof(cont)::iterator var = cont.begin(), var##end = cont.end(); var != var##end; ++var)

// const forward for each
#define FOR_EACHC(var, cont)\
	for (typeof(cont)::const_iterator var = cont.begin(), var##end = cont.end(); var != var##end; ++var)

// non-const forward for each with index
#define FOR_EACHI(var, idx, cont)\
	for (typeof(cont)::iterator var##begin = cont.begin(), var##end = cont.end(), var = var##begin; idx = static_cast<typeof(idx)>(var - var##begin), var != var##end; ++var)

// const forward for each with index
#define FOR_EACHIC(var, idx, cont)\
	for (typeof(cont)::const_iterator var##begin = cont.begin(), var##end = cont.end(), var = var##begin; idx = static_cast<typeof(idx)>(var - var##begin), var != var##end; ++var)

#endif
