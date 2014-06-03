//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2014
//***********************************************

#ifndef PR_RDR_SHADER_TYPES_HLSLI
#define PR_RDR_SHADER_TYPES_HLSLI

#include "cbuf.hlsli"

static const float TINY = 0.0001f;

// Models
#define HasNormals (m_geom.x == 1)
#define HasTex0    (m_geom.y == 1)

// Camera
struct Camera
{
	row_major float4x4 m_c2w; // camera to world
	row_major float4x4 m_c2s; // camera to screen
	row_major float4x4 m_w2c; // world to camera
	row_major float4x4 m_w2s; // world to screen
};

// Lights
struct Light
{
	// x = light type = 0 - ambient, 1 - directional, 2 - point, 3 - spot
	int4   m_info;         // Encoded info for global lighting
	float4 m_ws_direction; // The direction of the global light source
	float4 m_ws_position;  // The position of the global light source
	float4 m_ambient;      // The colour of the ambient light
	float4 m_colour;       // The colour of the directional light
	float4 m_specular;     // The colour of the specular light. alpha channel is specular power
	float4 m_spot;         // x = inner cos angle, y = outer cos angle, z = range, w = falloff
};

// Helpers for identifying light type
#define AmbientLight(light)     (light.m_info.x == 0)
#define DirectionalLight(light) (light.m_info.x == 1)
#define PointLight(light)       (light.m_info.x == 2)
#define SpotLight(light)        (light.m_info.x == 3)

// Shadows
struct Shadow
{
	int4 m_info;                // x = count of smaps
	float4 m_frust_dim;         // x = width at far plane, y = height at far plane, z = distance to far plane, w = smap max range (for normalising distances)
	row_major float4x4 m_frust; // Inward pointing frustum plane normals, transposed.
};

#define ShadowMapCount(shdw) (shdw.m_info.x)

#ifdef SHADER_BUILD

	// Vertex shader input format
	struct VSIn
	{
		float4 vert :Position;
		float4 diff :Color0;
		float4 norm :Normal;
		float2 tex0 :TexCoord0;
	};

	// Pixel shader input format
	struct PSIn
	{
		float4 ss_vert :SV_Position;
		float4 ws_vert :Position1;
		float4 ws_norm :Normal;
		float4 diff    :Color0;
		float2 tex0    :TexCoord0;
	};

#endif

#endif
