//*************************************
// Repeat
//  Copyright © Rylogic Ltd 2012
//*************************************
// Example:
//  // Overloads taking various numbers of parameters
//  #define PR_TN(n) typename U##n
//  #define PR_PARM1(n) U##n const& parm##n
//  #define PR_PARM2(n) parm##n
//  #define PR_FUNC(n)\
//  template <PR_REPEAT(n,PR_TN,PR_COMMA)> T* New(PR_REPEAT(n,PR_PARM1,PR_COMMA))\
//  {\
//     pointer p = allocate(1);\
//     construct(p, PR_REPEAT(n,PR_PARM2,PR_COMMA));\
//     return p;\
//  }
//  PR_FUNC(1)
//  PR_FUNC(2)
//  PR_FUNC(3)
//  PR_FUNC(4)
//  PR_FUNC(5)
//  PR_FUNC(6)
//  PR_FUNC(7)
//  #undef PR_TN
//  #undef PR_PARM1
//  #undef PR_PARM2
//  #undef PR_FUNC
// Note: nested PR_REPEATs don't work because of the macro recursion rules
#pragma once
#ifndef PR_MACRO_REPEAT_H
#define PR_MACRO_REPEAT_H

#include "pr/macros/join.h"

// Generates 'N' copies of 'mac', separated by 'sep'
// 'mac' should be a macro taking a single parameter 'x' which is the repeat number
#define PR_REPEAT(N,mac,sep) PR_JOIN(PR_REP,N)(mac,sep)

#define PR_COMMA ,
#define PR_NEWLINE\
// leave this line blank

// Repeater macros
#define PR_REP0(m,s) 
#define PR_REP1(m,s) m(1)
#define PR_REP2(m,s) PR_REP1(m,s)##s##m(2)
#define PR_REP3(m,s) PR_REP2(m,s)##s##m(3)
#define PR_REP4(m,s) PR_REP3(m,s)##s##m(4)
#define PR_REP5(m,s) PR_REP4(m,s)##s##m(5)
#define PR_REP6(m,s) PR_REP5(m,s)##s##m(6)
#define PR_REP7(m,s) PR_REP6(m,s)##s##m(7)
#define PR_REP8(m,s) PR_REP7(m,s)##s##m(8)
#define PR_REP9(m,s) PR_REP8(m,s)##s##m(9)

#define PR_REP10(m,s) PR_REP9(m,s)##s##m(10)
#define PR_REP11(m,s) PR_REP10(m,s)##s##m(11)
#define PR_REP12(m,s) PR_REP11(m,s)##s##m(12)
#define PR_REP13(m,s) PR_REP12(m,s)##s##m(13)
#define PR_REP14(m,s) PR_REP13(m,s)##s##m(14)
#define PR_REP15(m,s) PR_REP14(m,s)##s##m(15)
#define PR_REP16(m,s) PR_REP15(m,s)##s##m(16)
#define PR_REP17(m,s) PR_REP16(m,s)##s##m(17)
#define PR_REP18(m,s) PR_REP17(m,s)##s##m(18)
#define PR_REP19(m,s) PR_REP18(m,s)##s##m(19)

#define PR_REP20(m,s) PR_REP19(m,s)##s##m(20)
#define PR_REP21(m,s) PR_REP20(m,s)##s##m(21)
#define PR_REP22(m,s) PR_REP21(m,s)##s##m(22)
#define PR_REP23(m,s) PR_REP22(m,s)##s##m(23)
#define PR_REP24(m,s) PR_REP23(m,s)##s##m(24)
#define PR_REP25(m,s) PR_REP24(m,s)##s##m(25)
#define PR_REP26(m,s) PR_REP25(m,s)##s##m(26)
#define PR_REP27(m,s) PR_REP26(m,s)##s##m(27)
#define PR_REP28(m,s) PR_REP27(m,s)##s##m(28)
#define PR_REP29(m,s) PR_REP28(m,s)##s##m(29)

#define PR_REP30(m,s) PR_REP29(m,s)##s##m(30)
#define PR_REP31(m,s) PR_REP30(m,s)##s##m(31)
#define PR_REP32(m,s) PR_REP31(m,s)##s##m(32)
#define PR_REP33(m,s) PR_REP32(m,s)##s##m(33)
#define PR_REP34(m,s) PR_REP33(m,s)##s##m(34)
#define PR_REP35(m,s) PR_REP34(m,s)##s##m(35)
#define PR_REP36(m,s) PR_REP35(m,s)##s##m(36)
#define PR_REP37(m,s) PR_REP36(m,s)##s##m(37)
#define PR_REP38(m,s) PR_REP37(m,s)##s##m(38)
#define PR_REP39(m,s) PR_REP38(m,s)##s##m(39)

#endif

