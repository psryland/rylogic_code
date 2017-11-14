//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

namespace pr
{
	namespace rdr
	{
		// Shader Forward Declarations
		struct FwdShaderVS;
		struct FwdShaderPS;
		
		struct GBufferVS;
		struct GBufferPS;
		struct DSLightingVS;
		struct DSLightingPS;
		
		struct ShadowMapVS;
		struct ShadowMapFaceGS;
		struct ShadowMapLineGS;
		struct ShadowMapPS;

		struct RayCastVS;
		struct RayCastFaceGS;
		struct RayCastEdgeGS;
		struct RayCastVertGS;
		struct RayCastPS;
		struct RayCastCS;

		struct PointSpritesGS;
		struct ThickLineListGS;
		struct ArrowHeadGS;
	}
}