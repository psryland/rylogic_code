//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	enum class EStockModel : RdrId
	{
		#define PR_ENUM(x) \
		x(Invalid     ,= InvalidId)\
		x(Basis       ,)\
		x(UnitQuad    ,)\
		x(BBoxModel   ,)\
		x(SelectionBox,)
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(EStockModel, PR_ENUM);
	#undef PR_ENUM

	enum class EStockTexture : RdrId
	{
		#define PR_ENUM(x)\
		x(Invalid         ,= InvalidId)\
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
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(EStockTexture, PR_ENUM);
	#undef PR_ENUM

	enum class EStockSampler : RdrId
	{
		#define PR_ENUM(x)\
		x(Invalid    ,= InvalidId)\
		x(PointClamp       ,)\
		x(PointWrap        ,)\
		x(LinearClamp      ,)\
		x(LinearWrap       ,)\
		x(AnisotropicClamp ,)\
		x(AnisotropicWrap  ,)
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(EStockSampler, PR_ENUM);
	#undef PR_ENUM
}