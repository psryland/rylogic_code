//*************************************
// Stringise
// Copyright (c) Rylogic Ltd 2007
//*************************************
#pragma once

#ifndef PR_STRINGISE
#  define PR_STRINGISE_IMPL(x) #x
#  define PR_STRINGISE(x) PR_STRINGISE_IMPL(x)
#endif
