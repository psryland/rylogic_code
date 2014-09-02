//******************************************************************************
// Join two things that may be macros together
//  Copyright (c) Rylogic Ltd 2002
//******************************************************************************
#pragma once
#ifndef PR_MACROS_JOIN_H
#define PR_MACROS_JOIN_H

#ifndef PR_JOIN
#  define PR_DO_JOIN(x,y) x##y
#  define PR_JOIN(x,y)    PR_DO_JOIN(x,y)
#endif

#endif
