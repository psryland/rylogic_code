//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Custom terrain shader override: VS for CDLOD grid patches with Perlin
// noise height displacement and geomorphing. PS for height-based colouring.
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

		// Terrain constant buffer data. Shared parameters set in SetupFrame,
		// per-patch morph range overridden in SetupElement.
		alignas(16) std::byte m_cbuf[256];

		explicit TerrainShader(Renderer& rdr);

		// Called per-nugget during forward rendering. Copies the shared cbuf,
		// overrides per-patch morph data from the instance's i2w, then binds.
		void SetupElement(ID3D12GraphicsCommandList* cmd_list, rdr12::GpuUploadBuffer& upload, rdr12::Scene const& scene, rdr12::DrawListElement const* dle) override;

		// Update shared per-frame data (camera position). Called once per frame.
		void SetupFrame(v4 camera_world_pos);
	};
}
