//*******************************************************************************************
// LineDrawer
//	(c)opyright 2002 Rylogic Limited
//*******************************************************************************************
#ifndef LDR_EXCEPTION_H
#define LDR_EXCEPTION_H
#pragma once

#include <string>

struct LdrException
{
	std::string m_msg;
	explicit LdrException(std::string const& msg) :m_msg(msg) {}
};

#endif//LDR_EXCEPTION_H