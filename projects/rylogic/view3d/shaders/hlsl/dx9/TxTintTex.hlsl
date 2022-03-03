//***********************************************
// Renderer - Generated Shader
//  Copyright © Rylogic Ltd 2010
//***********************************************
#pragma warning (disable:3557)
#define TINY 0.0005f

// Txfm variables
shared uniform float4x4 g_object_to_world  :World;
shared uniform float4x4 g_norm_to_world    :World;
shared uniform float4x4 g_object_to_screen :WorldViewProjection;
shared uniform float4x4 g_camera_to_world  :ViewInverse;
shared uniform float4x4 g_world_to_camera  :View;
shared uniform float4x4 g_camera_to_screen :Projection;

// Tinting variables
shared uniform float4 g_tint_colour0 = float4(1,1,1,1);

// Texture2D variables
shared texture2D g_texture0 = NULL;
shared uniform float4x4 g_texture0_to_surf = float4x4(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1);
shared uniform DWORD g_texture0_mip_filter = 2;
shared uniform DWORD g_texture0_min_filter = 2;
shared uniform DWORD g_texture0_mag_filter = 2;
shared uniform DWORD g_texture0_addrU      = 3;
shared uniform DWORD g_texture0_addrV      = 3;
sampler2D g_sampler_texture0 = sampler_state { Texture=<g_texture0>; MipFilter=<g_texture0_mip_filter>; MinFilter=<g_texture0_min_filter>; MagFilter=<g_texture0_mag_filter>; AddressU=<g_texture0_addrU>; AddressV=<g_texture0_addrV>;};

// Txfm functions
float4 WSCameraPosition()                { return g_camera_to_world[3]; }
float4 ObjectToWorld(in float4 os_vec)   { return mul(os_vec, g_object_to_world); }
float4 NormToWorld(in float4 os_norm)    { return mul(os_norm, g_norm_to_world); }
float4 ObjectToScreen(in float4 os_vec)  { return mul(os_vec, g_object_to_screen); }
float4 ObjectToCamera(in float4 os_vec)  { return mul(os_vec, mul(g_object_to_world, g_world_to_camera)); }
float4 CameraToScreen(in float4 cs_vec)  { return mul(cs_vec, g_camera_to_screen); }

// Structs ********************************
struct VSIn
{
	float3   pos      :Position;
	float3   norm     :Normal;
	float4   diff     :Color0;
	float2   tex0     :TexCoord0;
};
struct VSOut0
{
	float4   pos      :Position;
	float4   diff     :Color0;
	float4   ws_pos   :TexCoord0;
	float4   ws_norm  :TexCoord1;
	float2   tex0     :TexCoord2;
};
struct PSOut0
{
	float4   diff     :Color0;
};

// Shaders ********************************
VSOut0 VS0(VSIn In)
{
	VSOut0 Out;
	Out.pos      = 0;
	Out.diff     = 1;
	Out.ws_pos   = 0;
	Out.ws_norm  = 0;
	Out.tex0     = 0;
	float4 ms_pos  = float4(In.pos  ,1);
	float4 ms_norm = float4(In.norm ,0);

	// Txfm
	Out.pos     = ObjectToScreen(ms_pos);
	Out.ws_pos  = ObjectToWorld(ms_pos);
	Out.ws_norm = NormToWorld(ms_norm);

	// Tinting
	Out.diff = g_tint_colour0;

	// Texture2D
	Out.tex0 = mul(float4(In.tex0,0,1), g_texture0_to_surf).xy;

	return Out;
}

PSOut0 PS0(VSOut0 In)
{
	PSOut0 Out;
	Out.diff     = 1;

	// Txfm
	In.ws_norm = normalize(In.ws_norm);

	// Tinting
	Out.diff = In.diff;

	// Texture2D
	Out.diff = tex2D(g_sampler_texture0, In.tex0) * Out.diff;

	return Out;
}

// Techniques ********************************
technique t0
{
	pass p0 {
	VertexShader = compile vs_3_0 VS0();
	PixelShader  = compile ps_3_0 PS0();
	}
}

