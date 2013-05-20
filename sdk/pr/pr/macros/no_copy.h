//*************************************
// No copy
// Copyright © Rylogic Ltd 2007
//*************************************
#ifndef PR_MACROS_NO_COPY_H
#define PR_MACROS_NO_COPY_H

// Use:
//  struct MyType
//  {
//     int& m_ref;
//     MyType(int& i) :m_ref(i) {}
//
//  private: // this is optional
//     PR_NO_COPY(MyType);
//  };
#define PR_NO_COPY(type)\
	type(type const&);\
	type& operator=(type const&)

#endif
