//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
#include "src/forward.h"
#include "src/shaders/ocean_shader.h"
#include "src/world/ocean.h"
#include "pr/view3d-12/shaders/shader_forward.h"

namespace las
{
	namespace fs = std::filesystem;

	// Helper to create a ByteCode from a vector of compiled bytecode
	static pr::rdr12::ByteCode MakeByteCode(std::vector<uint8_t> const& data)
	{
		pr::rdr12::ByteCode bc;
		static_cast<D3D12_SHADER_BYTECODE&>(bc) = { data.data(), data.size() };
		return bc;
	}

	OceanShader::OceanShader(Renderer& rdr)
		: Shader()
		, m_vs_bytecode()
		, m_ps_bytecode()
		, m_cbuf()
	{
		CompileShaders(rdr);

		// Set the shader code — replaces VS and PS in the forward pipeline
		m_code = pr::rdr12::ShaderCode{
			.VS = MakeByteCode(m_vs_bytecode),
			.PS = MakeByteCode(m_ps_bytecode),
			.DS = pr::rdr12::shader_code::none,
			.HS = pr::rdr12::shader_code::none,
			.GS = pr::rdr12::shader_code::none,
			.CS = pr::rdr12::shader_code::none,
		};

		// Initialise default PBR parameters
		m_cbuf.m_fresnel_f0 = 0.02f;       // Water at normal incidence
		m_cbuf.m_specular_power = 256.0f;   // Sharp sun glint
		m_cbuf.m_sss_strength = 0.5f;       // Moderate subsurface scattering

		m_cbuf.m_colour_shallow = v4(0.10f, 0.60f, 0.55f, 1.0f); // Turquoise
		m_cbuf.m_colour_deep    = v4(0.02f, 0.08f, 0.20f, 1.0f); // Dark ocean blue
		m_cbuf.m_colour_foam    = v4(0.95f, 0.97f, 1.00f, 1.0f); // Near-white foam

		m_cbuf.m_sun_direction  = Normalise(v4(0.5f, 0.3f, 0.8f, 0.0f)); // Elevated sun, slightly NE
		m_cbuf.m_sun_colour     = v4(1.0f, 0.95f, 0.85f, 1.0f);          // Warm sunlight
	}

	void OceanShader::CompileShaders(Renderer&)
	{
		using namespace pr::rdr12;

		// Find shader source files relative to the executable directory.
		// The vcxproj copies data/ to the output dir. The shader sources are
		// referenced relative to the project directory via include paths.
		// For runtime compilation, we need the actual repo paths.
		
		// Determine the repo root from the shader's expected location.
		// The exe runs from projects/apps/lost_at_sea/obj/x64/<Config>/
		// We need: projects/apps/lost_at_sea/src/shaders/ and projects/rylogic/
		auto exe_path = fs::path(pr::win32::ExePath());
		auto exe_dir = exe_path.parent_path();

		// Walk up to find the repo root (contains include/ and projects/)
		auto repo_root = exe_dir;
		for (int i = 0; i != 10; ++i)
		{
			if (fs::exists(repo_root / "include" / "pr") && fs::exists(repo_root / "projects"))
				break;
			repo_root = repo_root.parent_path();
		}

		auto rylogic_root = repo_root / "projects" / "rylogic";
		auto las_root = repo_root / "projects" / "apps" / "lost_at_sea";
		auto vs_path = las_root / "src" / "shaders" / "ocean_vs.hlsl";
		auto ps_path = las_root / "src" / "shaders" / "ocean_ps.hlsl";

		if (!fs::exists(vs_path))
			throw std::runtime_error("Ocean VS not found: " + vs_path.string());
		if (!fs::exists(ps_path))
			throw std::runtime_error("Ocean PS not found: " + ps_path.string());

		// Include paths for DXC: the HLSL files include paths like
		// "view3d-12/src/shaders/hlsl/types.hlsli" (relative to rylogic_root)
		// and "lost_at_sea/src/shaders/ocean_common.hlsli" (relative to projects/apps/)
		auto include_rylogic = std::format(L"-I{}", rylogic_root.wstring());
		auto include_las = std::format(L"-I{}", (repo_root / "projects" / "apps").wstring());
		auto include_repo = std::format(L"-I{}", repo_root.wstring());

		// Compile vertex shader
		{
			ShaderCompiler compiler;
			compiler.File(vs_path)
				.EntryPoint(L"main")
				.ShaderModel(L"vs_6_0")
				.Define(L"SHADER_BUILD")
				.Define(L"PR_RDR_VSHADER_ocean")
				.Optimise(true)
				.Arg(include_rylogic)
				.Arg(include_las)
				.Arg(include_repo);
			m_vs_bytecode = compiler.Compile();
		}

		// Compile pixel shader
		{
			ShaderCompiler compiler;
			compiler.File(ps_path)
				.EntryPoint(L"main")
				.ShaderModel(L"ps_6_0")
				.Define(L"SHADER_BUILD")
				.Define(L"PR_RDR_PSHADER_ocean")
				.Optimise(true)
				.Arg(include_rylogic)
				.Arg(include_las)
				.Arg(include_repo);
			m_ps_bytecode = compiler.Compile();
		}
	}

	void OceanShader::SetupElement(
		ID3D12GraphicsCommandList* cmd_list,
		pr::rdr12::GpuUploadBuffer& upload,
		pr::rdr12::Scene const&,
		pr::rdr12::DrawListElement const* dle)
	{
		if (dle == nullptr)
			return;

		// Upload the ocean constant buffer and bind to root parameter CBufScreenSpace (b3).
		// The ocean shader reuses this slot since it doesn't need screen-space geometry params.
		auto gpu_address = upload.Add(m_cbuf, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, true);
		cmd_list->SetGraphicsRootConstantBufferView(
			static_cast<UINT>(pr::rdr12::shaders::fwd::ERootParam::CBufScreenSpace),
			gpu_address);
	}

	void OceanShader::UpdateConstants(
		std::vector<GerstnerWave> const& waves,
		v4 camera_world_pos,
		float time,
		float inner_radius,
		float outer_radius,
		int num_rings,
		int num_segments)
	{
		auto count = std::min(static_cast<int>(waves.size()), CBufOcean::MaxWaves);
		m_cbuf.m_wave_count = count;

		for (int i = 0; i != count; ++i)
		{
			m_cbuf.m_wave_dirs[i] = waves[i].m_direction;
			m_cbuf.m_wave_params[i] = v4(
				waves[i].m_amplitude,
				waves[i].m_wavelength,
				waves[i].m_speed,
				waves[i].m_steepness);
		}

		// Zero remaining wave slots
		for (int i = count; i != CBufOcean::MaxWaves; ++i)
		{
			m_cbuf.m_wave_dirs[i] = v4::Zero();
			m_cbuf.m_wave_params[i] = v4::Zero();
		}

		m_cbuf.m_camera_pos_time = v4(camera_world_pos.x, camera_world_pos.y, camera_world_pos.z, time);
		m_cbuf.m_mesh_config = v4(inner_radius, outer_radius, static_cast<float>(num_rings), static_cast<float>(num_segments));
	}
}
