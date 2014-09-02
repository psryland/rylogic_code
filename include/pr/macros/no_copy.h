//*************************************
// No copy
// Copyright (c) Rylogic Ltd 2007
//*************************************
#ifndef PR_MACROS_NO_COPY_H
#define PR_MACROS_NO_COPY_H

// Use:
//  struct MyType
//  {
//     int& m_ref;
//     MyType(int& i) :m_ref(i) {}
//     PR_NO_COPY(MyType);
//  };
#if _MSC_VER <= 1700

#define PR_NO_COPY(type)\
	private:\
		type(type const&);\
		type& operator=(type const&)

#else // C++0X

#define PR_NO_COPY(type)\
	type(type const&) = delete;\
	type& operator=(type const&) = delete

#endif

#endif
