//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// CDLOD terrain vertex shader.
// Transforms unit-grid patch vertices to world space using the instance i2w (m_o2w),
// samples Perlin noise height, applies geomorphing for seamless LOD transitions,
// and projects to screen via m_cam.m_w2s.
//
// Vertex layout:
//   m_vert.xy = normalised grid position (i/GridN, j/GridN) ∈ [0, 1]
//   m_vert.z  = 0 (unused — height comes from Perlin noise)
//   m_vert.w  = 1
//
// Instance i2w encoding:
//   m_o2w[0].x = patch_size (world units)
//   m_o2w[1].y = patch_size
//   m_o2w[3].xy = patch world origin
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

// Terrain colour based on height and normal slope
float4 terrain_colour(float height, float3 normal)
{
	float flatness = normal.z; // z-component of normal = cosine of slope angle

	// Underwater: dark blue-grey
	if (height < 0.0)
		return float4(0.13, 0.25, 0.50, 1.0);

	// Beach: sandy yellow (0–5m)
	if (height < 5.0)
		return float4(0.82, 0.75, 0.37, 1.0);

	// Steep slope: rocky grey
	if (flatness < 0.7)
		return float4(0.38, 0.38, 0.38, 1.0);

	// High altitude: grey rock (above 150m)
	if (height > 150.0)
		return float4(0.50, 0.50, 0.50, 1.0);

	// Green vegetation, getting browner at higher elevations (5–150m)
	float t = saturate((height - 5.0) / 145.0);
	return float4(0.23 + t * 0.15, 0.50 - t * 0.20, 0.12 + t * 0.10, 1.0);
}

// Vertex shader: CDLOD grid patch with Perlin noise height and geomorphing
PSIn VSTerrain(VSIn In)
{
	PSIn Out = (PSIn)0;

	float grid_n = m_patch_config.z;
	float morph_start = m_patch_config.x;
	float morph_end = m_patch_config.y;

	// Transform grid vertex [0,1] to world space via m_o2w (encodes patch origin + scale)
	float4 world_pos = mul(float4(In.vert.xy, 0, 1), m_o2w);
	float2 world_xy = world_pos.xy;

	// Extract cell size from the instance transform
	float patch_size = m_o2w[0][0];
	float cell_size = patch_size / grid_n;

	// Sample terrain height at this world position
	float h = terrain_height(world_xy.x, world_xy.y);

	// Geomorphing: odd-indexed vertices morph toward their coarser-LOD neighbours.
	// This eliminates popping when LOD levels change.
	int ix = (int)round(In.vert.x * grid_n);
	int iy = (int)round(In.vert.y * grid_n);
	bool odd_x = (ix & 1) != 0;
	bool odd_y = (iy & 1) != 0;

	if (odd_x || odd_y)
	{
		float dist = length(world_xy - m_camera_pos.xy);
		float morph = saturate((dist - morph_start) / max(morph_end - morph_start, 0.001));

		float morph_h = h;
		if (odd_x && !odd_y)
		{
			// Morph to average of X-axis neighbours (even indices)
			float hL = terrain_height(world_xy.x - cell_size, world_xy.y);
			float hR = terrain_height(world_xy.x + cell_size, world_xy.y);
			morph_h = (hL + hR) * 0.5;
		}
		else if (!odd_x && odd_y)
		{
			// Morph to average of Y-axis neighbours (even indices)
			float hD = terrain_height(world_xy.x, world_xy.y - cell_size);
			float hU = terrain_height(world_xy.x, world_xy.y + cell_size);
			morph_h = (hD + hU) * 0.5;
		}
		else
		{
			// Both odd: morph to average of 4 diagonal neighbours (all even)
			float h00 = terrain_height(world_xy.x - cell_size, world_xy.y - cell_size);
			float h10 = terrain_height(world_xy.x + cell_size, world_xy.y - cell_size);
			float h01 = terrain_height(world_xy.x - cell_size, world_xy.y + cell_size);
			float h11 = terrain_height(world_xy.x + cell_size, world_xy.y + cell_size);
			morph_h = (h00 + h10 + h01 + h11) * 0.25;
		}

		h = lerp(h, morph_h, morph);
	}

	// Final world position with terrain height
	world_pos.z = h;

	// Compute terrain normal from the actual Perlin surface
	float3 n = terrain_normal(world_xy.x, world_xy.y);

	// Output
	Out.ws_vert = world_pos;
	Out.ws_norm = float4(n, 0);
	Out.ss_vert = mul(world_pos, m_cam.m_w2s);
	Out.diff = float4(0, 0, 0, 1);
	Out.tex0 = In.tex0;
	Out.idx0 = In.idx0;

	return Out;
}

// Pixel shader: per-pixel terrain colouring and lighting
PSOut PSTerrain(PSIn In)
{
	PSOut Out = (PSOut)0;

	float3 N = normalize(In.ws_norm.xyz);
	float3 V = normalize(m_cam.m_c2w[3].xyz - In.ws_vert.xyz);
	float3 L = m_sun_direction.xyz;

	// ws_vert.z is world height
	float world_z = In.ws_vert.z;

	// Compute terrain colour per-pixel from height and slope
	float4 base_colour = terrain_colour(world_z, N);

	// Diffuse lighting
	float NdotL = saturate(dot(N, L));
	float ambient = 0.25;
	float diffuse = NdotL * 0.65;

	// Soft sky light from above
	float sky_light = saturate(N.z * 0.5 + 0.5) * 0.15;

	float3 colour = base_colour.rgb * (ambient + diffuse + sky_light) * m_sun_colour.rgb;

	Out.diff = float4(colour, 1.0);
	return Out;
}
