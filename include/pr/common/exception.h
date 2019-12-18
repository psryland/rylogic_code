//**********************************
// Exception std::exception
//  Copyright (c) Rylogic Ltd 2007
//**********************************
#pragma once

#include <stdexcept>
#include <string>

namespace pr
{
	enum EResultGen
	{
		EResultGen_Success = 0,
		EResultGen_Failed = 0x80000000
	};

	template <typename CodeType = int>
	struct Exception :std::runtime_error
	{
		using base = std::runtime_error;
		CodeType m_code;

		Exception() = default;
		explicit Exception(std::string msg)
			:Exception(0, msg)
		{}
		explicit Exception(CodeType code)
			:Exception(code, std::string("Error code ").append(std::to_string(code)))
		{}
		explicit Exception(CodeType code, std::string msg)
			:base(msg)
			,m_code(code)
		{}
		virtual CodeType code() const
		{
			return m_code;
		}
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
