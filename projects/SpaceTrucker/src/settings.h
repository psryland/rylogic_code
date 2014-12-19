#pragma once

#include "SpaceTrucker/src/forward.h"

namespace st
{
	//(type, name, default_value, hashvalue, description)
	#define SETTINGS(x)\
		x(std::string ,AppVersion ,AppVersionString() ,0x16152F0E ,"Application version number")\
		x(std::string ,Adapter    ,""                 ,0x1        ,"Graphics adaptor")

	PR_DEFINE_SETTINGS(Settings, SETTINGS);
	#undef SETTINGS
}