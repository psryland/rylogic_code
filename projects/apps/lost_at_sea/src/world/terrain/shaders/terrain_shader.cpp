//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
#include "src/forward.h"
#include "src/world/terrain/terrain.h"
#include "src/world/terrain/shaders/terrain_shader.h"

namespace las
{
	namespace shaders::terrain
	{
		enum class ERootParam : int
		{
			CBufScene = 0,   // Scene constant buffer (b0)
			CBufObject = 1,  // Object constant buffer (b1)
			CBufFrame = 2,   // Frame constant buffer (b2)
			CBufTerrain = 3, // Reused by terrain shader for terrain params (b3)
		};

		#include "src/world/terrain/shaders/terrain_cbuf.hlsli"
		static_assert((sizeof(CBufTerrain) % 16) == 0);
	}

	TerrainShader::TerrainShader(Renderer& rdr)
		: Shader(rdr)
		, m_vs_bytecode()
		, m_ps_bytecode()
		, m_cbuf()
	{
		static_assert(sizeof(shaders::terrain::CBufTerrain) <= sizeof(m_cbuf), "CBufTerrain exceeds m_cbuf storage");

		// Compile the shader
		auto compiler = ShaderCompiler{}
			.Source(resource::Read<char>(L"TERRAIN_HLSL", L"TEXT"))
			.Includes({ new rdr12::ResourceIncludeHandler, true })
			.Define(L"SHADER_BUILD")
			.Optimise(true);

		m_vs_bytecode = compiler.ShaderModel(L"vs_6_0").EntryPoint(L"VSTerrain").Compile();
		m_ps_bytecode = compiler.ShaderModel(L"ps_6_0").EntryPoint(L"PSTerrain").Compile();
		m_code.VS = { m_vs_bytecode };
		m_code.PS = { m_ps_bytecode };

		// Initialise default parameters (matching HeightField defaults)
		auto& cbuf = storage_cast<shaders::terrain::CBufTerrain>(m_cbuf);
		cbuf = shaders::terrain::CBufTerrain{
			.m_camera_pos = v4::Zero(),
			.m_mesh_config = v4::Zero(),
			.m_noise_params = v4(6.0f, 0.001f, 0.5f, 80.0f), // octaves, base_freq, persistence, amplitude
			.m_noise_bias = v4(-0.3f, 0, 0, 0),               // sea_level_bias
			.m_sun_direction = Normalise(v4(0.5f, 0.3f, 0.8f, 0.0f)),
			.m_sun_colour = v4(1.0f, 0.95f, 0.85f, 1.0f),
		};
	}

	// Called per-nugget during forward rendering to bind the terrain constant buffer
	void TerrainShader::SetupElement(ID3D12GraphicsCommandList* cmd_list, rdr12::GpuUploadBuffer& upload, rdr12::Scene const&, rdr12::DrawListElement const* dle)
	{
		if (dle == nullptr)
			return;

		auto& cbuf = storage_cast<shaders::terrain::CBufTerrain>(m_cbuf);
		auto gpu_address = upload.Add(cbuf, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, true);
		cmd_list->SetGraphicsRootConstantBufferView(static_cast<UINT>(shaders::terrain::ERootParam::CBufTerrain), gpu_address);
	}

	// Update the constant buffer data for this frame
	void TerrainShader::SetupFrame(v4 camera_world_pos, float inner_radius, float outer_radius, int num_rings, int num_segments)
	{
		auto& cbuf = storage_cast<shaders::terrain::CBufTerrain>(m_cbuf);
		cbuf.m_camera_pos = camera_world_pos;
		cbuf.m_mesh_config = v4(inner_radius, outer_radius, static_cast<float>(num_rings), static_cast<float>(num_segments));
	}
}
