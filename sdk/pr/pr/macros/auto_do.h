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

#define PR_AUTO_DO(type, variable, what_to_do) \
struct AutoDo##type##variable \
{ \
	AutoDo##type##variable(const type& var) : variable(var)	{} \
	~AutoDo##type##variable()								{ what_to_do } \
	type variable; \
} auto_##variable(variable);

#endif//PR_AUTO_DO_H