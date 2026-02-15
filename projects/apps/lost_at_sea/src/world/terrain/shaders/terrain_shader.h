//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Custom terrain shader override: VS for Perlin noise displacement,
// PS for height-based terrain colouring with basic lighting.
#pragma once
#include "src/forward.h"

namespace las
{
	struct TerrainShader : rdr12::Shader
	{
		// Compiled shader bytecodes (populated at construction from runtime compilation).
		// The ByteCode wrappers in m_code point into these vectors, so they must outlive the shader.
		std::vector<uint8_t> m_vs_bytecode;
		std::vector<uint8_t> m_ps_bytecode;

		// Terrain constant buffer data, updated each frame
		alignas(16) std::byte m_cbuf[256];

		explicit TerrainShader(Renderer& rdr);

		// Called per-nugget during forward rendering to bind the terrain constant buffer
		void SetupElement(ID3D12GraphicsCommandList* cmd_list, rdr12::GpuUploadBuffer& upload, rdr12::Scene const& scene, rdr12::DrawListElement const* dle) override;

		// Update the constant buffer data for this frame
		void SetupFrame(v4 camera_world_pos, float inner_radius, float outer_radius, int num_rings, int num_segments);
	};
}
