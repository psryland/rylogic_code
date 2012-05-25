//**********************************************************************************
//
// A macro for declaring unique types
//
//**********************************************************************************
//
// This macro allows several types to be created from the same basic data type
// Usage:
//		Typedef(DWORD, MyIndex);
//		Typedef(DWORD, YourIndex);
//	'YourIndex' cannot be used in place of 'MyIndex'
//
#ifndef PR_HARD_TYPEDEF_H
#define PR_HARD_TYPEDEF_H

#define HardTypedef(type, type_name)							\
struct type_name												\
{																\
	operator       type& ()       { return m_t; }				\
	operator const type& () const { return m_t; }				\
	type m_t;													\
}

#endif//PR_HARD_TYPEDEF_H
