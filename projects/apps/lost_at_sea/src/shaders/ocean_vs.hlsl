//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Ocean vertex shader.
// Reconstructs world-space vertex positions from encoded ring/segment data,
// applies Gerstner wave displacement, and computes analytical normals.
//
// Vertex encoding:
//   m_vert.xy = unit-circle direction (cos θ, sin θ) for the segment
//   m_vert.z  = normalised ring index t ∈ [0, 1]
//   m_vert.w  = 1
//   Centre vertex: xy = (0, 0), z = -1 (sentinel)
#include "view3d-12/src/shaders/hlsl/forward/forward_cbuf.hlsli"
#include "lost_at_sea/src/shaders/ocean_common.hlsli"

// Compute ring radius with log-to-linear blend based on camera height
float RingRadius(float t, float camera_height, float inner, float outer)
{
	// Logarithmic: r = inner * exp(log(outer/inner) * t)
	float log_ratio = log(outer / inner);
	float r_log = inner * exp(log_ratio * t);

	// Linear: r = inner + (outer - inner) * t
	float r_lin = inner + (outer - inner) * t;

	// Blend: 0 = fully logarithmic (near surface), 1 = fully linear (high altitude)
	float h = abs(camera_height);
	float blend = saturate(h / outer);

	return lerp(r_log, r_lin, blend);
}

// Apply Gerstner wave displacement to a world-space position
// Returns: xyz = displaced position, w = foam factor (Jacobian > 1 indicates foam)
float4 GerstnerDisplace(float2 world_xy, float time)
{
	float dx = 0, dy = 0, dz = 0;
	float jacobian = 1.0;

	for (int i = 0; i < m_wave_count; ++i)
	{
		float2 dir = m_wave_dirs[i].xy;
		float amplitude  = m_wave_params[i].x;
		float wavelength = m_wave_params[i].y;
		float speed      = m_wave_params[i].z;
		float steepness  = m_wave_params[i].w;

		float k = 6.283185307 / wavelength; // tau / wavelength
		float freq = k * speed;
		float phase = k * dot(dir, world_xy) - freq * time;
		float c = cos(phase);
		float s = sin(phase);

		dx -= steepness * amplitude * dir.x * c;
		dy -= steepness * amplitude * dir.y * c;
		dz += amplitude * s;

		// Jacobian term for foam detection
		jacobian -= steepness * k * amplitude * s;
	}

	return float4(dx, dy, dz, saturate(1.0 - jacobian));
}

// Compute analytical Gerstner wave normal
float3 GerstnerNormal(float2 world_xy, float time)
{
	float nx = 0, ny = 0, nz = 1;

	for (int i = 0; i < m_wave_count; ++i)
	{
		float2 dir = m_wave_dirs[i].xy;
		float amplitude  = m_wave_params[i].x;
		float wavelength = m_wave_params[i].y;
		float speed      = m_wave_params[i].z;
		float steepness  = m_wave_params[i].w;

		float k = 6.283185307 / wavelength;
		float freq = k * speed;
		float phase = k * dot(dir, world_xy) - freq * time;
		float c = cos(phase);
		float s = sin(phase);

		nx -= dir.x * k * amplitude * c;
		ny -= dir.y * k * amplitude * c;
		nz -= steepness * k * amplitude * s;
	}

	return normalize(float3(nx, ny, nz));
}

#ifdef PR_RDR_VSHADER_ocean

PSIn main(VSIn In)
{
	PSIn Out = (PSIn)0;

	float inner = m_mesh_config.x;
	float outer = m_mesh_config.y;
	float cam_height = m_camera_pos_time.z;
	float time = m_camera_pos_time.w;
	float2 cam_xy = m_camera_pos_time.xy;

	float3 ws_pos;

	// Centre vertex sentinel: z == -1
	if (In.vert.z < 0)
	{
		// Centre vertex is at the camera XY position, displaced by waves
		float4 disp = GerstnerDisplace(cam_xy, time);
		ws_pos = float3(cam_xy.x + disp.x, cam_xy.y + disp.y, disp.z);

		float3 n = GerstnerNormal(cam_xy, time);
		Out.ws_norm = float4(n, 0);
		Out.diff = float4(0, 0, 0, disp.w); // foam factor in alpha
	}
	else
	{
		// Ring vertex: decode direction and ring parameter
		float2 dir = In.vert.xy;    // unit-circle direction
		float t = In.vert.z;        // normalised ring index [0, 1]

		float r = RingRadius(t, cam_height, inner, outer);
		float2 local_xy = r * dir;
		float2 world_xy = cam_xy + local_xy;

		// Apply Gerstner displacement
		float4 disp = GerstnerDisplace(world_xy, time);
		ws_pos = float3(
			world_xy.x + disp.x,
			world_xy.y + disp.y,
			disp.z
		);

		float3 n = GerstnerNormal(world_xy, time);
		Out.ws_norm = float4(n, 0);
		Out.diff = float4(0, 0, 0, disp.w); // foam factor in alpha
	}

	// Camera-relative rendering: subtract camera XY to keep geometry near the origin.
	// The camera Z offset is handled by the view matrix (w2c) since
	// the camera is at (0, 0, cam_height) in render space.
	float3 cam_rel = ws_pos - float3(cam_xy, 0);
	float4 os_vert = float4(cam_rel, 1);

	// Transform using standard forward matrices
	Out.ws_vert = mul(os_vert, m_o2w);
	Out.ss_vert = mul(os_vert, m_o2s);

	// Pass through texture coordinates (unused for now, but available for detail maps)
	Out.tex0 = In.tex0;
	Out.idx0 = In.idx0;

	return Out;
}

#endif
