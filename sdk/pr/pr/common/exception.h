//**********************************
// Exception std::exception
//  Copyright © Rylogic Ltd 2007
//**********************************
#pragma once
#ifndef PR_EXCEPTION_H
#define PR_EXCEPTION_H

#include <exception>
#include <string>

namespace pr
{
	enum EResultGen
	{
		EResultGen_Success = 0,
		EResultGen_Failed = 0x80000000
	};
	
	template <typename CodeType = int> struct Exception :std::exception
	{
		CodeType    m_code;
		std::string m_msg;
		
		Exception()                                      :m_code()     ,m_msg()    {}
		Exception(char const* msg)                       :m_code()     ,m_msg(msg) {}
		Exception(std::string const& msg)                :m_code()     ,m_msg(msg) {}
		Exception(CodeType code)                         :m_code(code) ,m_msg()    {}
		Exception(CodeType code, char const* msg)        :m_code(code) ,m_msg(msg) {}
		Exception(CodeType code, std::string const& msg) :m_code(code) ,m_msg(msg) {}
		virtual char const* what() const { return m_msg.c_str(); }
		virtual CodeType    code() const { return m_code; }
	};
}

#endif
