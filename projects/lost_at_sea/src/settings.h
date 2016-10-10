//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************
#pragma once

#include "lost_at_sea/src/forward.h"

namespace las
{
	#define LAS_SETTING(x) \
		x(std::string ,Version     ,AppVersionA() ,"Application version number")\
		x(bool        ,FullScreen  ,false         ,"Full screen mode enabled"  )\
		x(pr::uint    ,XResolution ,1024          ,"Screen X resolution"       )\
		x(pr::uint    ,YResolution ,768           ,"Screen Y resolution"       )
	
	PR_DEFINE_SETTINGS(Settings, LAS_SETTING);
	#undef LAS_SETTING
}
