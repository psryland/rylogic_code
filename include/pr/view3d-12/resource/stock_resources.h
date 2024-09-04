//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	#define PR_ENUM(x) \
		x(Invalid     ,= InvalidId)\
		x(Basis       ,)\
		x(UnitQuad    ,)\
		x(BBoxModel   ,)\
		x(SelectionBox,)
	PR_DEFINE_ENUM2_BASE(EStockModel, PR_ENUM, RdrId);
	#undef PR_ENUM

	#define PR_ENUM(x) \
		x(Invalid  ,= InvalidId)\
		x(Black           ,)\
		x(White           ,)\
		x(Gray            ,)\
		x(Checker         ,)\
		x(Checker2        ,)\
		x(Checker3        ,)\
		x(WhiteDot        ,)\
		x(WhiteSpot       ,)\
		x(WhiteSpike      ,)\
		x(WhiteSphere     ,)\
		x(WhiteTriangle   ,)\
		x(EnvMapProjection,)
	PR_DEFINE_ENUM2_BASE(EStockTexture, PR_ENUM, RdrId);
	#undef PR_ENUM

	#define PR_ENUM(x) \
		x(Invalid    ,= InvalidId)\
		x(PointClamp       ,)\
		x(PointWrap        ,)\
		x(LinearClamp      ,)\
		x(LinearWrap       ,)\
		x(AnisotropicClamp ,)\
		x(AnisotropicWrap  ,)
	PR_DEFINE_ENUM2_BASE(EStockSampler, PR_ENUM, RdrId);
	#undef PR_ENUM
}