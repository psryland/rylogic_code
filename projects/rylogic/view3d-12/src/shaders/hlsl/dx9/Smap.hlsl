//***********************************************
// Renderer - Generated Shader
//  Copyright © Rylogic Ltd 2010
//***********************************************
#pragma warning (disable:3557)
#define TINY 0.0005f

// SMap variables
#define SMapTexSize 1024
#define SMapEps 0.01f
shared uniform float4x4 g_object_to_world :World;
shared uniform float4x4 g_world_to_smap;
shared uniform float4   g_ws_smap_plane;
shared uniform float4   g_smap_frust_dim;
shared uniform int      g_light_type[1];
shared uniform float4   g_ws_light_position[1]  :Position ;
shared uniform float4   g_ws_light_direction[1] :Direction;
shared uniform float4x4 g_smap_frust;
shared uniform float4x4 g_world_to_camera :View;
shared uniform int      g_cast_shadows[1];
sampler2D g_sampler_smap[1];

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
	float4   ws_pos   :TexCoord0;
	float2   ss_pos   :TexCoord1;
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
	Out.ws_pos   = 0;
	Out.ss_pos   = 0;
	float4 ms_pos  = float4(In.pos  ,1);
	float4 ms_norm = float4(In.norm ,0);

	// SMap
	Out.ws_pos = mul(ms_pos, g_object_to_world);
	Out.pos    = mul(Out.ws_pos, g_world_to_smap);
	Out.ss_pos = Out.pos.xy;

	return Out;
}

PSOut0 PS0(VSOut0 In,uniform bool fwd,uniform float sign0,uniform float sign1)
{
	PSOut0 Out;
	Out.diff     = 1;

	// SMap
	// find a world space ray starting from 'ws_pos' and away from the light source
	float4 ws_ray = ShadowRayWS(In.ws_pos, 0);

	// clip it to the frustum plane
	float t = ClipToPlane(g_ws_smap_plane, In.ws_pos, In.ws_pos + ws_ray);
	float dist = t * length(ws_ray) / g_smap_frust_dim.w;

	// clip pixels with a negative distance
	clip(dist);

	// clip to the wedge of the fwd texture we're rendering to
	if (fwd)
	{
		clip(sign0 * (In.ss_pos.y - In.ss_pos.x) + TINY);
		clip(sign1 * (In.ss_pos.y + In.ss_pos.x) + TINY);
	}

	// encode the distance into the output
	if (fwd) Out.diff.rg = EncodeFloat2(dist);
	else     Out.diff.ba = EncodeFloat2(dist);

	return Out;
}

// Techniques ********************************
technique t0
{
	pass p0 {
	VertexShader = compile vs_3_0 VS0();
	PixelShader  = compile ps_3_0 PS0(true,+1,-1);
	ColorWriteEnable=Red|Green; 
	CullMode=CCW;
	}
	pass p1 {
	VertexShader = compile vs_3_0 VS0();
	PixelShader  = compile ps_3_0 PS0(true,-1,+1);
	ColorWriteEnable=Red|Green; 
	CullMode=CCW;
	}
	pass p2 {
	VertexShader = compile vs_3_0 VS0();
	PixelShader  = compile ps_3_0 PS0(true,+1,+1);
	ColorWriteEnable=Red|Green; 
	CullMode=CCW;
	}
	pass p3 {
	VertexShader = compile vs_3_0 VS0();
	PixelShader  = compile ps_3_0 PS0(true,-1,-1);
	ColorWriteEnable=Red|Green; 
	CullMode=CCW;
	}
	pass p4 {
	VertexShader = compile vs_3_0 VS0();
	PixelShader  = compile ps_3_0 PS0(false,0,0);
	ColorWriteEnable=Blue|Alpha;
	CullMode=CW;
	}
}

