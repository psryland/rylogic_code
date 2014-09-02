//*************************************
// Stringise
// Copyright (c) Rylogic Ltd 2007
//*************************************
#ifndef PR_MACROS_STRINGISE_H
#define PR_MACROS_STRINGISE_H

#ifndef PR_STRINGISE
#  define PR_STRINGISE_IMPL(x) #x
#  define PR_STRINGISE(x) PR_STRINGISE_IMPL(x)
#endif

#endif
