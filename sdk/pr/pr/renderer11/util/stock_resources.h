//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once

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

		#define PR_ENUM(x)\
			x(TxTint         )\
			x(TxTintPvc      )\
			x(TxTintTex      )\
			x(TxTintPvcLit   )\
			x(TxTintPvcLitTex)\
			x(GBuffer)\
			x(DSLighting)
		PR_DEFINE_ENUM1(EStockShader, PR_ENUM);
		#undef PR_ENUM
	}
}
