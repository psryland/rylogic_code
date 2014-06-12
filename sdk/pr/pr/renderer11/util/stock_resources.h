//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		#define PR_ENUM(x)\
			x(Invalid       , = InvalidId)\
			x(ForwardRender ,)\
			x(GBuffer       ,)\
			x(DSLighting    ,)\
			x(ShadowMap     ,)
		PR_DEFINE_ENUM2(ERenderStep, PR_ENUM);
		#undef PR_ENUM

		#define PR_ENUM(x) \
			x(Invalid ,= InvalidId)\
			x(Black   ,)\
			x(White   ,)\
			x(Checker ,)
		PR_DEFINE_ENUM2(EStockTexture, PR_ENUM);
		#undef PR_ENUM

		#define PR_ENUM(x) \
			x(Invalid         , = InvalidId)\
			x(FwdShaderVS     ,)\
			x(FwdShaderPS     ,)\
			x(GBufferVS       ,)\
			x(GBufferPS       ,)\
			x(DSLightingVS    ,)\
			x(DSLightingPS    ,)\
			x(ShadowMapVS     ,)\
			x(ShadowMapFaceGS ,)\
			x(ShadowMapLineGS ,)\
			x(ShadowMapPS     ,)\
			x(ThickLineListGS ,)\
			x(ArrowHeadGS     ,)
		PR_DEFINE_ENUM2(EStockShader, PR_ENUM);
		#undef PR_ENUM
	}
}
