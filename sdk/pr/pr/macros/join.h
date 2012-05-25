//******************************************************************************
// Join two things that may be macros together
//  Copyright © Rylogic Ltd 2002
//******************************************************************************
#ifndef PR_JOIN_H
#define PR_JOIN_H
	
#define PR_DO_JOIN(x,y) x##y
#define PR_JOIN(x,y)    PR_DO_JOIN(x,y)
	
#endif
