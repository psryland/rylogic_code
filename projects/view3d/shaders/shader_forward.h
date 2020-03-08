//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

namespace pr::rdr
{
	// Forward rendering shaders
	struct FwdShaderVS;
	struct FwdShaderPS;
	struct FwdRadialFadePS;

	// Deferred rendering shaders
	struct GBufferVS;
	struct GBufferPS;
	struct DSLightingVS;
	struct DSLightingPS;

	// Shadow mapping
	struct ShadowMapVS;
	struct ShadowMapFaceGS;
	struct ShadowMapLineGS;
	struct ShadowMapPS;

	// Screen space shaders
	struct PointSpritesGS;
	struct ThickLineListGS;
	struct ThickLineStripGS;
	struct ArrowHeadGS;

	// Diagnostic shaders
	struct ShowNormalsGS;
}