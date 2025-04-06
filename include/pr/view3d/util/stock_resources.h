//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/view3d/forward.h"

namespace pr::rdr
{
	enum class ERenderStep
	{
		#define PR_ENUM(x)\
		x(Invalid       , = InvalidId)\
		x(ForwardRender ,)\
		x(GBuffer       ,)\
		x(DSLighting    ,)\
		x(ShadowMap     ,)\
		x(RayCast       ,)
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(ERenderStep, PR_ENUM);
	#undef PR_ENUM

	enum class EStockTexture : RdrId
	{
		#define PR_ENUM(x) \
		x(Invalid  ,= InvalidId)\
		x(Black        ,)\
		x(White        ,)\
		x(Gray         ,)\
		x(Checker      ,)\
		x(Checker2     ,)\
		x(Checker3     ,)\
		x(WhiteSpot    ,)\
		x(WhiteTriangle,)\
		x(EnvMapProjection,)
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(EStockTexture, PR_ENUM);
	#undef PR_ENUM

	enum class EStockShader : RdrId
	{
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
		x(ShadowMapPS     ,)\
		x(PointSpritesGS  ,)\
		x(ThickLineListGS ,)\
		x(ThickLineStripGS,)\
		x(ArrowHeadGS     ,)\
		x(ShowNormalsGS   ,)
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(EStockShader, PR_ENUM);
	#undef PR_ENUM
}
