//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"

namespace pr
{
	namespace rdr
	{
		#define PR_ENUM(x)\
			x(Invalid       , = InvalidId)\
			x(ForwardRender ,)\
			x(GBuffer       ,)\
			x(DSLighting    ,)\
			x(ShadowMap     ,)\
			x(RayCast       ,)
		PR_DEFINE_ENUM2(ERenderStep, PR_ENUM);
		#undef PR_ENUM

		#define PR_ENUM(x) \
			x(Invalid  ,= InvalidId)\
			x(Black        ,)\
			x(White        ,)\
			x(Gray         ,)\
			x(Checker      ,)\
			x(Checker2     ,)\
			x(Checker3     ,)\
			x(WhiteSpot    ,)\
			x(WhiteTriangle,)
		PR_DEFINE_ENUM2_BASE(EStockTexture, PR_ENUM, RdrId);
		#undef PR_ENUM

		#define PR_ENUM(x) \
			x(Invalid         , = InvalidId)\
			x(FwdShaderVS     ,)\
			x(FwdShaderPS     ,)\
			x(FwdRadialFadePS ,)\
			x(GBufferVS       ,)\
			x(GBufferPS       ,)\
			x(DSLightingVS    ,)\
			x(DSLightingPS    ,)\
			x(ShadowMapVS     ,)\
			x(ShadowMapFaceGS ,)\
			x(ShadowMapLineGS ,)\
			x(ShadowMapPS     ,)\
			x(PointSpritesGS  ,)\
			x(ThickLineListGS ,)\
			x(ThickLineStripGS,)\
			x(ArrowHeadGS     ,)
		PR_DEFINE_ENUM2_BASE(EStockShader, PR_ENUM, RdrId);
		#undef PR_ENUM
	}
}