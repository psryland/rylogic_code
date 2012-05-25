//*****************************************************************
//
//	Assert definitions
//
//*****************************************************************
// Usage:
//	Each module should use a define like "PR_DBG_???" to enable asserts
//	for that module. In the main headers for the module there should be
//	a definition:
//		#ifndef PR_DBG_???
//			#define PR_DBG_??? PR_DBG
//		#endif//PR_DBG_???
//
//	Sub parts of the module can also have there own debug define as in:
//		#ifndef PR_DBG_XYZ
//			#define PR_DBG_XYZ PR_DBG_???
//		#endif//PR_DBG_XYZ
//
//	To change the assert handler define PR_ASSERT_FUNC(exp, str) to your handler
//
//	Asserts can be enabled independently of debug/release using PR_ENABLE_ASSERTS
//	By default asserts are all enabled in debug and all disabled in release
//
//	Use these to remove dependencies in standalone files:
//
////"pr/common/assert.h" should be included prior to this for pr asserts
//#ifndef PR_ASSERT
//#   define PR_ASSERT_DEFINED
//#   define PR_ASSERT(grp, exp, str)
//#endif
//#ifdef PR_ASSERT_DEFINED
//#   undef PR_ASSERT_DEFINED
//#   undef PR_ASSERT
//#endif
//#ifndef PR_EXPAND
//#   define PR_EXPAND_DEFINED
//#   define PR_EXPAND(grp, exp)
//#endif
//#ifdef PR_EXPAND_DEFINED
//#   undef PR_EXPAND_DEFINED
//#   undef PR_EXPAND
//#endif
//#ifndef PR_INFO
//#   define PR_INFO_DEFINED
//#   define PR_INFO(grp, str)
//#endif
//#ifdef PR_INFO_DEFINED
//#   undef PR_INFO_DEFINED
//#   undef PR_INFO
//#endif
//#ifndef PR_INFO_EXP
//#   define PR_INFO_EXP_DEFINED
//#   define PR_INFO_EXP(grp, exp, str)
//#endif
//#ifdef PR_INFO_EXP_DEFINED
//#   undef PR_INFO_EXP_DEFINED
//#   undef PR_INFO_EXP
//#endif
//
#pragma once
#ifndef PR_ASSERT_H
#define PR_ASSERT_H

#include "pr/macros/stringise.h"
#include "pr/macros/join.h"

// If PR_ENABLE_ASSERTS is not defined then define it.
// Default is on, this switch can be used to turn all asserts off
#ifndef PR_ENABLE_ASSERTS
#   define PR_ENABLE_ASSERTS
#endif

// Assert enabler for common headers
// If not defined, set it based on whether it's a debug build
#ifndef PR_DBG
#   ifdef NDEBUG
#       define PR_DBG_COMMON <Use of PR_DBG_COMMON is deprecated>
#       define PR_DBG 0
#   else
#       define PR_DBG_COMMON <Use of PR_DBG_COMMON is deprecated>
#       define PR_DBG 1
#   endif
#endif

// Check whether the message output function has been
// defined, if not, define it as OutputDebugString or printf
#ifndef PR_OUTPUT_MSG
#   ifdef OutputDebugString
#       define PR_OUTPUT_MSG(str)   OutputDebugStringA(str)
#   else
#       include <cstdio>
#       define PR_OUTPUT_MSG(str)   printf("%s", (str))
#   endif
#endif

// Check whether the assert function has been
// defined, if not, define it as the crt assert
#ifndef PR_ASSERT_FUNC
#	if defined(_WIN32) && defined(_DEBUG)
#		include <crtdbg.h>
		inline bool AssertExpressionStink(bool b) {return b;}
#		ifdef _CPPUNWIND
#		define PR_ASSERT_ASBOOL(var, exp) bool var; try { var = AssertExpressionStink(!!(exp)); } catch (...) { var = false; }
#		else
#		define PR_ASSERT_ASBOOL(var, exp) bool var = AssertExpressionStink(!!(exp));
#		endif
#		define PR_ASSERT_FUNC(exp, str)\
			{\
				PR_ASSERT_ASBOOL(valid, exp);\
				valid ||\
				(0 != _CrtDbgReport(_CRT_WARN  , __FILE__, __LINE__, NULL, "ASSERT FAILURE: %s\nComment: %s\n", #exp, str)) ||\
				(1 != _CrtDbgReport(_CRT_ASSERT, __FILE__, __LINE__, NULL,                 "%s\nComment: %s\n", #exp, str)) ||\
				(_CrtDbgBreak(), 0);\
			}
#	else
#		include <cassert>
#		define PR_ASSERT_FUNC(exp, str) {assert((exp) && (str));}
#	endif
#endif

#define PR_EXPAND0(exp)
#define PR_EXPAND1(exp)         exp
#define PR_EXPAND(grp, exp)     PR_JOIN(PR_EXPAND, grp)(exp)

#define PR_INFO0(str)           {}
#define PR_INFO1(str)           {PR_OUTPUT_MSG(str); size_t len=strlen(str); if(len&&(str)[len-1]!='\n') {PR_OUTPUT_MSG("\n");}}

#define PR_ASSERT0(exp, str)    {}
#define PR_ASSERT1(exp, str)    PR_ASSERT_FUNC(exp, str)

// Define the assert macros
#ifdef PR_ENABLE_ASSERTS
#   define PR_ASSERT(grp, exp, str)   PR_JOIN(PR_ASSERT, grp)(exp, str)
#   define PR_INFO(grp, str)          PR_JOIN(PR_INFO, grp)(str);
#   define PR_INFO_EXP(grp, exp, str) if(!(exp)) {PR_JOIN(PR_INFO, grp)(str);}
#   define PR_STUB_FUNC()             PR_OUTPUT_MSG("Warning: Stub function called\n")
#else
#   define PR_ASSERT(grp, exp, str)
#   define PR_INFO(grp, str)
#   define PR_INFO_EXP(grp, exp, str)
#   define PR_STUB_FUNC()
#endif
	
// Support for C++0x static_assert
#if _MSC_VER < 1600
#	ifndef __cplusplus
#		define static_assert(exp,str) typedef int JOIN(static_assert_, __LINE__)[(exp) ? 1 : -1]
#	else
		namespace pr
		{
			template <int n>  struct StaticAssertDummy;
			template <bool x> struct StaticAssertFailure;
			template <>       struct StaticAssertFailure<true> { enum {StaticFailure}; };
		}
#		ifdef __GNUC__
#			define static_assert(exp,str) typedef pr::StaticAssertDummy<pr::StaticAssertFailure<!!(exp)>::StaticFailure> PR_JOIN(Static_Failure, __LINE__)
#		else
#			define static_assert(exp,str) typedef pr::StaticAssertDummy<pr::StaticAssertFailure<!!(exp)>::StaticFailure> Static_Failure
#		endif
#	endif
#endif

#ifndef PR_UNUSED
#   define PR_UNUSED(exp) sizeof(exp)
#endif

#endif
