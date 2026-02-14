//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Custom ocean shader override: VS for Gerstner wave displacement,
// PS for PBR water rendering (Fresnel, reflection, refraction, SSS, foam).
#pragma once
#include "src/forward.h"

namespace las
{
	struct GerstnerWave;

	// C++ mirror of the HLSL CBufOcean struct.
	// Must match layout in ocean_common.hlsli exactly.
	struct alignas(16) CBufOcean
	{
		static constexpr int MaxWaves = 4;

		v4 m_wave_dirs[MaxWaves];     // xy = normalised direction per wave
		v4 m_wave_params[MaxWaves];   // x=amplitude, y=wavelength, z=speed, w=steepness
		v4 m_camera_pos_time;         // xyz = camera world pos, w = time
		v4 m_mesh_config;             // x=inner, y=outer, z=num_rings, w=num_segments
		int m_wave_count;
		float m_fresnel_f0;
		float m_specular_power;
		float m_sss_strength;
		v4 m_colour_shallow;
		v4 m_colour_deep;
		v4 m_colour_foam;
		v4 m_sun_direction;
		v4 m_sun_colour;
	};
	static_assert((sizeof(CBufOcean) % 16) == 0);

	struct OceanShader : pr::rdr12::ShaderOverride
	{
		// Compiled shader bytecodes (populated at construction from runtime compilation).
		// The ByteCode wrappers in m_code point into these vectors, so they must outlive the shader.
		std::vector<uint8_t> m_vs_bytecode;
		std::vector<uint8_t> m_ps_bytecode;

		// Ocean constant buffer data, updated each frame
		CBufOcean m_cbuf;

		explicit OceanShader(Renderer& rdr);

		// Called per-nugget during forward rendering to bind the ocean constant buffer
		void SetupOverride(
			ID3D12GraphicsCommandList* cmd_list,
			pr::rdr12::GpuUploadBuffer& upload,
			pr::rdr12::Scene const& scene,
			pr::rdr12::DrawListElement const* dle) override;

		// Update the constant buffer data for this frame
		void UpdateConstants(
			std::vector<GerstnerWave> const& waves,
			v4 camera_world_pos,
			float time,
			float inner_radius,
			float outer_radius,
			int num_rings,
			int num_segments);

	private:

		void CompileShaders(Renderer& rdr);
	};
}
