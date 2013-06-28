//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_UTIL_STOCK_RESOURCES_H
#define PR_RDR_UTIL_STOCK_RESOURCES_H

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		#define PR_ENUM(x)\
			x(Black   ,= 0x42001)\
			x(White   ,= 0x42002)\
			x(Checker ,= 0x42003)
		PR_DEFINE_ENUM2(EStockTexture, PR_ENUM);
		#undef PR_ENUM
	}
}

#endif
