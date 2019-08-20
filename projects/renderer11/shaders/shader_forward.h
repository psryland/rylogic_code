//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

namespace pr::rdr
{
	// Shader Forward Declarations
	struct FwdShaderVS;
	struct FwdShaderPS;
	struct FwdRadialFadePS;
		
	struct GBufferVS;
	struct GBufferPS;
	struct DSLightingVS;
	struct DSLightingPS;
		
	struct ShadowMapVS;
	struct ShadowMapFaceGS;
	struct ShadowMapLineGS;
	struct ShadowMapPS;

	struct PointSpritesGS;
	struct ThickLineListGS;
	struct ThickLineStripGS;
	struct ArrowHeadGS;
}