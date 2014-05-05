//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/stock_resources.h"

namespace pr
{
	namespace rdr
	{
		namespace EDbgRdrFlags
		{
			enum Type { WarnedNoRenderNuggets = 1 << 0 };
		}

		#define PR_ENUM(x)\
			x(GBuffer   , = EStockShader::NumberOf)\
			x(DSLighting,)
		PR_DEFINE_ENUM2(ERdrShader, PR_ENUM);
		#undef PR_ENUM
	}
}
