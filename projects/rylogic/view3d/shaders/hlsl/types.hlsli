//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2014
//***********************************************

#ifndef PR_RDR_SHADER_TYPES_HLSLI
#define PR_RDR_SHADER_TYPES_HLSLI

#include "cbuf.hlsli"

static const float TINY = 0.0001f;
static const int MaxShadowMaps = 1;
static const int MaxProjectedTextures = 1;

// Model flags
static const int ModelFlags_HasNormals           = (1 << 0);
static const int TextureFlags_HasDiffuse         = (1 << 0);
static const int TextureFlags_IsReflective       = (1 << 1);
static const int TextureFlags_ProjectFromEnvMap  = (1 << 2);
static const int AlphaFlags_HasAlpha             = (1 << 0);

// Models
#define HAS_NORMALS  (m_flags.x & ModelFlags_HasNormals)
#define HAS_TEX0     (m_flags.y & TextureFlags_HasDiffuse)
#define HAS_ENVMAP   (m_flags.y & TextureFlags_IsReflective)
#define ENV_MAP_PROJ (m_flags.y & TextureFlags_ProjectFromEnvMap)
#define HAS_ALPHA    (m_flags.z & AlphaFlags_HasAlpha)

// Light types
#define AMBIENT_LIGHT(light)     (light.m_info.x == 0)
#define DIRECTIONAL_LIGHT(light) (light.m_info.x == 1)
#define POINT_LIGHT(light)       (light.m_info.x == 2)
#define SPOT_LIGHT(light)        (light.m_info.x == 3)

// Shadows
#define ShadowMapCount(shdw) (shdw.m_info.x)

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
	float4 m_spot;         // x = inner angle, y = outer angle, z = range, w = falloff
};

// EnvMap
struct EnvMap
{
	row_major float4x4 m_w2env; // world to environment map to transform
};

// Shadows
struct Shadow
{
	int4 m_info;  // x = count of smaps, y = smap size
	row_major float4x4 m_w2l[MaxShadowMaps]; // World space to light space
	row_major float4x4 m_l2s[MaxShadowMaps]; // Light space to shadow map space
};

// Projected textures
struct ProjTexture
{
	int4 m_info;  // x = count of projected textures
	row_major float4x4 m_w2t[MaxProjectedTextures]; // World to texture space projection transform
};

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

	// Compute shader input
	struct CSIn
	{
		// Example:
		//  [numthreads(10,8,3)] = Number of threads in one thread group.
		//  Dispatch(5,3,2) = Run (5*3*2=30) thread groups.
		//  The threads in each group execute in parallel.

		// 'group_id' is the 3d address in units of groups.
		// 'group_id' is the highest level partitioning, with each value representing a block of threads.
		// e.g. group_id = (0,0,0) = first block of (10*8*3) threads, (1,0,0) is the next block of (10*8*3) threads
		//      Values in the range [0,0,0] -> [5,3,2]
		uint3 group_id :SV_GroupID;

		// 'thread_id' is the global address of the thread, equal to 'group_id'*[numthreads] + 'group_thread_id'.
		//  e.g. Values in the range [0,0,0] -> [5,3,2]*[10,8,3]
		uint3 thread_id :SV_DispatchThreadID;

		// 'group_thread_id' is the address of a thread within a group.
		// e.g. group_thread_id = (0,0,0) = first thread in the current block
		//      Values in the range [0,0,0] -> [10,8,3]
		uint3 group_thread_id :SV_GroupThreadID;

		// 'group_idx' is the 3d address within a group, converted to a 1d index: Z*width*height + Y*width + X
		// e.g. group_idx = group_thread_id.z*numthreads.x*numthreads.y + group_thread_id.y*numthreads.x + group_thread_id.x
		//      Values in the range [0] -> [3*10*8 + 8*10 + 10]
		uint group_idx :SV_GroupIndex;
	};

#endif

#endif
