//**********************************
// Exception std::exception
//  Copyright (c) Rylogic Ltd 2007
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
		CodeType m_code;

		Exception()                               :std::exception()            ,m_code()     {}
		Exception(std::string msg)                :std::exception(msg.c_str()) ,m_code()     {}
		Exception(CodeType code)                  :std::exception()            ,m_code(code) {}
		Exception(CodeType code, std::string msg) :std::exception(msg.c_str()) ,m_code(code) {}
		virtual CodeType code() const { return m_code; }
	};

	// Note:
	//  To allow the exception message to be changed by intermediary catch handlers
	//  do this:
	//   catch (std::exception& ex)
	//   {
	//       // This is a slicing assignment, but that is actually what we want
	//       ex = std::exception(FmtS("%s\nMore info...", ex.what()));
	//       throw; // Preserves original exception type
	//   }
}

#endif
