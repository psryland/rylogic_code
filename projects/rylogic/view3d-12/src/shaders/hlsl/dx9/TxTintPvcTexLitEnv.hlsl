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

// Lighting variables *********************
#define LightCount 1
shared uniform int    g_light_type         [LightCount];
shared uniform float4 g_ws_light_position  [LightCount] :Position ;
shared uniform float4 g_ws_light_direction [LightCount] :Direction;
shared uniform float4 g_light_ambient      [LightCount] :Ambient;
shared uniform float4 g_light_diffuse      [LightCount] :Diffuse;
shared uniform float  g_spot_inner_cosangle[LightCount];
shared uniform float  g_spot_outer_cosangle[LightCount];
shared uniform float  g_spot_range         [LightCount];
shared uniform float4 g_light_specular     [LightCount] :Specular;
shared uniform float  g_specular_power     [LightCount] :SpecularPower;
// ShadowMap variables *********************
#define SMapCasters 1
#define SMapTexSize 1024
#define SMapEps 0.01f
shared uniform int g_cast_shadows[LightCount];
shared uniform float4x4 g_smap_frust;
shared uniform float4   g_smap_frust_dim;
shared texture g_smap0 = NULL;
sampler2D g_sampler_smap[SMapCasters] =
{
	sampler_state {Texture=<g_smap0>; MinFilter=Point; MagFilter=Point; MipFilter=Point; AddressU=Clamp; AddressV = Clamp;},
};

// EnvMap variables
shared uniform float g_envmap_blend_fraction = 0;
shared textureCUBE g_envmap :Environment = NULL;
samplerCUBE g_sampler_envmap = sampler_state { Texture=<g_envmap>; MipFilter=Linear; MinFilter=Linear; MagFilter=Linear; };

// Txfm functions
float4 WSCameraPosition()                { return g_camera_to_world[3]; }
float4 ObjectToWorld(in float4 os_vec)   { return mul(os_vec, g_object_to_world); }
float4 NormToWorld(in float4 os_norm)    { return mul(os_norm, g_norm_to_world); }
float4 ObjectToScreen(in float4 os_vec)  { return mul(os_vec, g_object_to_screen); }
float4 ObjectToCamera(in float4 os_vec)  { return mul(os_vec, mul(g_object_to_world, g_world_to_camera)); }
float4 CameraToScreen(in float4 cs_vec)  { return mul(cs_vec, g_camera_to_screen); }

// SMap functions
float2 EncodeFloat2(float value)
{
	const float2 shift = float2(2.559999e2f, 9.999999e-1f);
	float2 packed = frac(value * shift);
	packed.y -= packed.x / 256.0f;
	return packed;
}
float DecodeFloat2(float2 value)
{
	const float2 shifts = float2(3.90625e-3f, 1.0f);
	return dot(value, shifts);
}
float ClipToPlane(uniform float4 plane, in float4 s, in float4 e)
{
	float d0 = dot(plane, s);
	float d1 = dot(plane, e);
	float d  = d1 - d0;
	return -d0/d;
}
float4 ShadowRayWS(in float4 ws_pos, in int light_index)
{
	return (g_light_type[light_index] == 1) ? (g_ws_light_direction[light_index]) : (ws_pos - g_ws_light_position[light_index]);
}
float IntersectFrustum(uniform float4x4 frust, in float4 s, in float4 e)
{
	// Intersect the line passing through 's' and 'e' with 'frust' return the parametric value 't'
	// Assumes 's' is within the frustum to start with
	const float4 T  = 100000;
	float4 d0 = mul(s, frust);
	float4 d1 = mul(e, frust);
	float4 t0 = step(d1,d0)   * min(T, -d0/(d1 - d0));        // Clip to the frustum sides
	float  t1 = step(e.z,s.z) * min(T.x, -s.z / (e.z - s.z)); // Clip to the far plane

	float t = T.x;
	if (t0.x != 0) t = min(t,t0.x);
	if (t0.y != 0) t = min(t,t0.y);
	if (t0.z != 0) t = min(t,t0.z);
	if (t0.w != 0) t = min(t,t0.w);
	if (t1   != 0) t = min(t,t1);
	return t;
}
float LightVisibility(int light_index, float4 ws_pos)
{
	// return a value between [0,1] where 0 means fully in shadow, 1 means not in shadow
	if (g_cast_shadows[light_index] == -1) return 1;

	// find the shadow ray in frustum space and its intersection with the frustum
	float4 ws_ray = ShadowRayWS(ws_pos, light_index);
	float4 fs_pos0 = mul(ws_pos          ,g_world_to_camera); fs_pos0.z += g_smap_frust_dim.z;
	float4 fs_pos1 = mul(ws_pos + ws_ray ,g_world_to_camera); fs_pos1.z += g_smap_frust_dim.z;
	float t = IntersectFrustum(g_smap_frust, fs_pos0, fs_pos1);

	// convert the intersection to texture space
	float4 intersect = lerp(fs_pos0, fs_pos1, t);
	float2 uv = float2(0.5 + 0.5*intersect.x/g_smap_frust_dim.x, 0.5 - 0.5*intersect.y/g_smap_frust_dim.y);

	// find the distance from the frustum to 'ws_pos'
	float dist = saturate(t * length(ws_ray) / g_smap_frust_dim.w) + SMapEps;

	const float d = 0.5 / SMapTexSize;
	float4 px0 = tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2(-d,-d));
	float4 px1 = tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2( d,-d));
	float4 px2 = tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2(-d, d));
	float4 px3 = tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2( d, d));
	if (intersect.z > TINY)
		return (step(DecodeFloat2(px0.rg), dist) +
				step(DecodeFloat2(px1.rg), dist) +
				step(DecodeFloat2(px2.rg), dist) +
				step(DecodeFloat2(px3.rg), dist)) / 4.0f;
	else
		return (step(DecodeFloat2(px0.ba), dist) +
				step(DecodeFloat2(px1.ba), dist) +
				step(DecodeFloat2(px2.ba), dist) +
				step(DecodeFloat2(px3.ba), dist)) / 4.0f;
}

// Lighting functions
float LightDirectional(in float4 ws_light_direction, in float4 ws_norm, in float alpha)
{
	float brightness = -dot(ws_light_direction, ws_norm);
	if (brightness < 0.0) brightness = (1.0 - alpha) * abs(brightness);
	return saturate(brightness);
}
float LightPoint(in float4 ws_light_position, in float4 ws_norm, in float4 ws_pos, in float alpha)
{
	float4 light_to_pos = ws_pos - ws_light_position;
	float dist = length(light_to_pos);
	float brightness = -dot(light_to_pos, ws_norm) / dist;
	if (brightness < 0.0) brightness = (1.0 - alpha) * abs(brightness);
	return saturate(brightness);
}
float LightSpot(in float4 ws_light_position, in float4 ws_light_direction, in float inner_cosangle, in float outer_cosangle, in float range, in float4 ws_norm, in float4 ws_pos, in float alpha)
{
	float brightness = LightPoint(ws_light_position, ws_norm, ws_pos, alpha);
	float4 light_to_pos = ws_pos - ws_light_position;
	float dist = length(light_to_pos);
	float cos_angle = saturate(dot(light_to_pos, ws_light_direction) / dist);
	brightness *= saturate((outer_cosangle - cos_angle) / (outer_cosangle - inner_cosangle));
	brightness *= saturate((range - dist) * 9 / range);
	return brightness;
}
float LightSpecular(in float4 ws_light_direction, in float specular_power, in float4 ws_norm, in float4 ws_toeye_norm, in float alpha)
{
	float4 ws_H = normalize(ws_toeye_norm - ws_light_direction);
	float brightness = dot(ws_norm, ws_H);
	if (brightness < 0.0) brightness = (1.0 - alpha) * abs(brightness);
	return pow(saturate(brightness), specular_power);
}
float4 Illuminate(float4 ws_pos, float4 ws_norm, float4 ws_cam, float4 unlit_diff)
{
	float4 ltdiff = 0;
	float4 ltspec = 0;
	float  ltvis = 1;
	float4 ws_toeye_norm = normalize(ws_cam - ws_pos);
	for (int i = 0; i != LightCount; ++i)
	{
		ltdiff += g_light_ambient[i];
		ltvis = LightVisibility(i, ws_pos);
		if (ltvis == 0) continue;
		float intensity = 0;
		if      (g_light_type[i] == 1) intensity = LightDirectional(g_ws_light_direction[i] ,ws_norm         ,unlit_diff.a);
		else if (g_light_type[i] == 2) intensity = LightPoint      (g_ws_light_position[i]  ,ws_norm ,ws_pos ,unlit_diff.a);
		else if (g_light_type[i] == 3) intensity = LightSpot       (g_ws_light_position[i]  ,g_ws_light_direction[i] ,g_spot_inner_cosangle[i] ,g_spot_outer_cosangle[i] ,g_spot_range[i] ,ws_norm ,ws_pos ,unlit_diff.a);
		ltdiff += ltvis * intensity * g_light_diffuse[i];
		float4 ws_light_dir = (g_light_type[i] == 1) ? g_ws_light_direction[i] : normalize(ws_pos - g_ws_light_position[i]);
		ltspec += ltvis * intensity * g_light_specular[i] * LightSpecular(ws_light_dir ,g_specular_power[i] ,ws_norm ,ws_toeye_norm ,unlit_diff.a);
	}
	return saturate(2.0*(ltdiff-0.5)*unlit_diff + ltspec + unlit_diff);
}

// EnvMap functions
float4 EnvMap(in float4 ws_pos, in float4 ws_norm, in float4 unenvmapped_diff)
{
	if (g_envmap_blend_fraction < 0.01) return unenvmapped_diff;
	float4 ws_toeye_norm = normalize(WSCameraPosition() - ws_pos);
	float4 ws_env        = reflect(-ws_toeye_norm, ws_norm);
	float4 env           = texCUBE(g_sampler_envmap, ws_env.xyz);
	return lerp(unenvmapped_diff, env, g_envmap_blend_fraction);
}

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

	// PVC
	Out.diff = In.diff * Out.diff;

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

	// PVC
	Out.diff = In.diff;

	// Texture2D
	Out.diff = tex2D(g_sampler_texture0, In.tex0) * Out.diff;

	// Lighting
	Out.diff = Illuminate(In.ws_pos, In.ws_norm, WSCameraPosition(), Out.diff);

	// EnvMap
	Out.diff = EnvMap(In.ws_pos, In.ws_norm, Out.diff);

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

