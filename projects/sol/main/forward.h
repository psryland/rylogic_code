//*****************************************************************************************
// Sol
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#ifndef PR_SOL_MAIN_FORWARD_H
#define PR_SOL_MAIN_FORWARD_H

#include "pr/common/new.h"
#include "pr/macros/enum.h"

#include "pr/app/forward.h"
#include "pr/app/main_gui.h"
#include "pr/app/main.h"
#include "pr/app/skybox.h"
#include "pr/app/gimble.h"
#include "pr/app/sim_message_loop.h"

namespace sol
{
	#define PR_ENUM(x)\
		x(Fail)
	PR_DEFINE_ENUM1(EException, PR_ENUM);
	#undef PR_ENUM
	typedef pr::Exception<EException> Exception;

	struct MainGUI;
	struct Main;

	struct AstronomicalBody;
	struct TestModel;

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
