//************************************************************
//
//	A macro for creating a small struct that does something on destruction
//
//************************************************************
// Usage:
// PR_AUTO_DO(thing*, my_thing, if( my_thing ) my_thing->Do();)
//
#ifndef PR_AUTO_DO_H
#define PR_AUTO_DO_H

#include <pr/macros/join.h>

#define PR_AUTO_DO(type, variable, what_to_do) \
struct PR_JOIN(AutoDo,__LINE__) \
{ \
	type variable; \
	PR_JOIN(AutoDo,__LINE__)(const type& var) :variable(var) {} \
	~PR_JOIN(AutoDo,__LINE__)()                              { what_to_do } \
} PR_JOIN(auto_,__LINE__)(variable);

#endif