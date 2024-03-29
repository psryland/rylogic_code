//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	#define PR_ENUM(x) \
		x(Invalid         , = InvalidId)\
		x(Forward         ,)\
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
	PR_DEFINE_ENUM2_BASE(EStockShader, PR_ENUM, RdrId);
	#undef PR_ENUM
}