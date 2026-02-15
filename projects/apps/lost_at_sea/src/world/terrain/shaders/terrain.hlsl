//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Terrain vertex shader.
// Reconstructs world-space vertex positions from encoded ring/segment data,
// applies Perlin noise displacement for height, and computes terrain normals.
//
// Vertex encoding (same as ocean):
//   m_vert.xy = unit-circle direction (cos θ, sin θ) for the segment
//   m_vert.z  = normalised ring index t ∈ [0, 1]
//   m_vert.w  = 1
//   Centre vertex: xy = (0, 0), z = -1 (sentinel)
#include "view3d-12/src/shaders/hlsl/forward/forward_cbuf.hlsli"
#include "src/world/terrain/shaders/terrain_cbuf.hlsli"

struct PSOut
{
	float4 diff :SV_TARGET;
};

// ---- GPU Perlin noise ----
// Integer hash (good distribution, GPU-friendly)
int ihash(int n)
{
	n = (n << 13) ^ n;
	return (n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff;
}

// Hash two integers into a pseudo-random int
int hash2(int x, int y)
{
	return ihash(x + ihash(y));
}

// Smooth interpolation curve: 6t^5 - 15t^4 + 10t^3
float fade(float t)
{
	return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

// Gradient function: select from 8 gradient directions
float grad2d(int hash, float x, float y)
{
	int h = hash & 7;
	float u = h < 4 ? x : y;
	float v = h < 4 ? y : x;
	return ((h & 1) ? -u : u) + ((h & 2) ? -2.0 * v : 2.0 * v);
}

// Single-octave 2D Perlin noise, returns approximately [-1, 1]
float noise2d(float x, float y)
{
	int ix = (int)floor(x);
	int iy = (int)floor(y);
	float fx = x - floor(x);
	float fy = y - floor(y);

	float u = fade(fx);
	float v = fade(fy);

	// Hash the four corners
	int aa = hash2(ix,     iy);
	int ba = hash2(ix + 1, iy);
	int ab = hash2(ix,     iy + 1);
	int bb = hash2(ix + 1, iy + 1);

	// Gradient dot products at each corner, blended
	float x1 = lerp(grad2d(aa, fx,       fy),       grad2d(ba, fx - 1.0, fy),       u);
	float x2 = lerp(grad2d(ab, fx,       fy - 1.0), grad2d(bb, fx - 1.0, fy - 1.0), u);
	return lerp(x1, x2, v);
}

// Multi-octave (fractal) Perlin noise matching HeightField::HeightAt
float terrain_height(float world_x, float world_y)
{
	int octaves           = (int)m_noise_params.x;
	float base_frequency  = m_noise_params.y;
	float persistence     = m_noise_params.z;
	float amplitude       = m_noise_params.w;
	float sea_level_bias  = m_noise_bias.x;

	float value = 0.0;
	float freq = base_frequency;
	float amp = 1.0;
	float max_amp = 0.0;

	for (int i = 0; i < octaves; ++i)
	{
		value += noise2d(world_x * freq, world_y * freq) * amp;
		max_amp += amp;
		amp *= persistence;
		freq *= 2.0;
	}

	value = value / max_amp;
	return (value + sea_level_bias) * amplitude;
}

// Terrain normal from finite differences (matches HeightField::NormalAt)
float3 terrain_normal(float world_x, float world_y)
{
	float eps = 1.0; // 1m sample spacing
	float hL = terrain_height(world_x - eps, world_y);
	float hR = terrain_height(world_x + eps, world_y);
	float hD = terrain_height(world_x, world_y - eps);
	float hU = terrain_height(world_x, world_y + eps);
	return normalize(float3(hL - hR, hD - hU, 2.0 * eps));
}

// Compute ring radius with log-to-linear blend based on camera height
float RingRadius(float t, float camera_height, float inner, float outer)
{
	float log_ratio = log(outer / inner);
	float r_log = inner * exp(log_ratio * t);
	float r_lin = inner + (outer - inner) * t;

	float h = abs(camera_height);
	float blend = saturate(h / outer);
	return lerp(r_log, r_lin, blend);
}

// Terrain colour based on height and normal slope
float4 terrain_colour(float height, float3 normal)
{
	float flatness = normal.z; // z-component of normal = cosine of slope angle

	// Underwater: dark blue-grey
	if (height < 0.0)
		return float4(0.13, 0.25, 0.50, 1.0);

	// Beach: sandy yellow
	if (height < 2.0)
		return float4(0.82, 0.75, 0.37, 1.0);

	// Steep slope: rocky grey
	if (flatness < 0.7)
		return float4(0.38, 0.38, 0.38, 1.0);

	// High altitude: grey rock
	if (height > 40.0)
		return float4(0.50, 0.50, 0.50, 1.0);

	// Green vegetation, getting browner at higher elevations
	float t = saturate((height - 2.0) / 38.0);
	return float4(0.23 + t * 0.15, 0.50 - t * 0.20, 0.12 + t * 0.10, 1.0);
}

// Vertex shader: reconstruct terrain from encoded radial mesh
PSIn VSTerrain(VSIn In)
{
	PSIn Out = (PSIn)0;

	float inner = m_mesh_config.x;
	float outer = m_mesh_config.y;
	float cam_height = m_camera_pos.z;
	float2 cam_xy = m_camera_pos.xy;

	float2 world_xy;

	// Centre vertex sentinel: z == -1
	if (In.vert.z < 0)
	{
		world_xy = cam_xy;
	}
	else
	{
		// Ring vertex: decode direction and ring parameter
		float2 dir = In.vert.xy;
		float t = In.vert.z;
		float r = RingRadius(t, cam_height, inner, outer);
		world_xy = cam_xy + r * dir;
	}

	// Compute terrain height and normal
	float h = terrain_height(world_xy.x, world_xy.y);
	float3 n = terrain_normal(world_xy.x, world_xy.y);
	float3 ws_pos = float3(world_xy, h);

	// Terrain colour from height and slope
	Out.diff = terrain_colour(h, n);
	Out.ws_norm = float4(n, 0);

	// Camera-relative rendering: subtract camera XY to keep geometry near the origin.
	// The camera Z offset is handled by the view matrix (w2c).
	float3 cam_rel = ws_pos - float3(cam_xy, 0);
	float4 os_vert = float4(cam_rel, 1);

	Out.ws_vert = mul(os_vert, m_o2w);
	Out.ss_vert = mul(os_vert, m_o2s);
	Out.tex0 = In.tex0;
	Out.idx0 = In.idx0;

	return Out;
}

// Pixel shader: lit terrain with vertex colours
PSOut PSTerrain(PSIn In)
{
	PSOut Out = (PSOut)0;

	float3 N = normalize(In.ws_norm.xyz);
	float3 V = normalize(m_cam.m_c2w[3].xyz - In.ws_vert.xyz);
	float3 L = m_sun_direction.xyz;

	// Discard underwater fragments (ocean surface handles those)
	// Use ws_vert.z relative to camera-relative origin + camera height to get world z
	float world_z = In.ws_vert.z;
	if (world_z < -0.5)
		discard;

	// Diffuse lighting
	float NdotL = saturate(dot(N, L));
	float ambient = 0.25;
	float diffuse = NdotL * 0.65;

	// Soft sky light from above
	float sky_light = saturate(N.z * 0.5 + 0.5) * 0.15;

	float3 colour = In.diff.rgb * (ambient + diffuse + sky_light) * m_sun_colour.rgb;

	Out.diff = float4(colour, 1.0);
	return Out;
}
