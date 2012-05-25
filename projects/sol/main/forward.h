//*****************************************************************************************
// Sol
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#ifndef PR_SOL_FORWARD_H
#define PR_SOL_FORWARD_H

#include "pr/app/forward.h"

namespace sol
{
	namespace EException
	{
		enum Type
		{
			Fail,
		};
	}
	typedef pr::Exception<EException::Type> Exception;

	struct MainGUI;
	struct Main;

	//char const* AppTitle();
	//char const* AppString();
	//char const* AppStringLine();
	typedef pr::string<char>    string;
	typedef pr::string<wchar_t> wstring;
	//typedef std::istream istream;
	//typedef std::ostream ostream;
	//typedef std::stringstream sstream;
}

#endif
