//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/shaders/shader.h"
#include "pr/view3d/shaders/shader_manager.h"
#include "pr/view3d/models/nugget.h"
#include "pr/view3d/models/model_buffer.h"
#include "pr/view3d/instances/instance.h"
#include "pr/view3d/render/drawlist_element.h"
#include "pr/view3d/render/renderer.h"
#include "pr/view3d/render/scene.h"
#include "pr/view3d/steps/render_step.h"
#include "view3d/render/state_stack.h"

namespace pr::rdr
{
	// Look for a compiled shader object file by the name 'compiled_shader_filename'
	// If the file exists, check the last modified time, and if newer, replace the shader 'ptr' and update 'last_modified'
	#if PR_RDR_RUNTIME_SHADERS
	#pragma message(PR_LINK "WARNING: ************************************************** PR_RDR_RUNTIME_SHADERS enabled")

	std::unordered_map<RdrId, wstring256> shader_cso;
	void RegisterRuntimeShader(RdrId id, char const* cso_filepath)
	{
		#ifdef NDEBUG
		char const* compile_shader_dir = R"(P:\pr\projects\renderer11\shaders\hlsl\compiled\release)";
		#else
		char const* compile_shader_dir = R"(P:\pr\projects\renderer11\shaders\hlsl\compiled\debug)";
		#endif

		shader_cso[id] = pr::To<wstring256>(pr::FmtS("%s\\%s",compile_shader_dir,cso_filepath));
	}
	#endif
	
	void Shader::Setup(ID3D11DeviceContext*, DeviceState&)
	{
		#if PR_RDR_RUNTIME_SHADERS
		struct ModCheck
		{
			DWORD  m_last_check;    // Tick value when the last check for a changed shader was made
			time_t m_last_modified; // Support for dynamically loading shaders at runtime (unused when PR_RDR_RUNTIME_SHADERS is not defined)
		};
		static std::unordered_map<RdrId, ModCheck> s_check;
		auto& check = s_check[m_orig_id];

		// Only check every now and again
		enum { check_frequency_ms = 1000 };
		if (GetTickCount() - check.m_last_check < check_frequency_ms) return;
		check.m_last_check = GetTickCount();

		Renderer::Lock lock(m_mgr->m_rdr);
		auto device = lock.D3DDevice();
		auto iter = shader_cso.find(m_orig_id);
		if (iter == std::end(shader_cso))
			return;
			
		// Check for a new shader
		wstring256 cso_filepath = iter->second;
		time_t last_mod, newest = check.m_last_modified;
		if (pr::filesys::FileExists(cso_filepath) && (last_mod = pr::filesys::FileTimeStats(cso_filepath).m_last_modified) > check.m_last_modified)
		{
			std::vector<pr::uint8> buf;
			if (pr::FileToBuffer(cso_filepath.c_str(), buf) && !buf.empty())
			{
				D3DPtr<ID3D11DeviceChild> dxshdr;
				switch (m_shdr_type) {
				default: PR_ASSERT(PR_DBG_RDR, false, "Unknown shader type"); break;
				case EShaderType::VS: pr::Throw(device->CreateVertexShader   (&buf[0], buf.size(), 0, (ID3D11VertexShader  **)&dxshdr.m_ptr)); break;
				case EShaderType::PS: pr::Throw(device->CreatePixelShader    (&buf[0], buf.size(), 0, (ID3D11PixelShader   **)&dxshdr.m_ptr)); break;
				case EShaderType::GS: pr::Throw(device->CreateGeometryShader (&buf[0], buf.size(), 0, (ID3D11GeometryShader**)&dxshdr.m_ptr)); break;
				case EShaderType::CS: pr::Throw(device->CreateComputeShader  (&buf[0], buf.size(), 0, (ID3D11ComputeShader **)&dxshdr.m_ptr)); break;
				case EShaderType::HS: pr::Throw(device->CreateHullShader     (&buf[0], buf.size(), 0, (ID3D11HullShader    **)&dxshdr.m_ptr)); break;
				case EShaderType::DS: pr::Throw(device->CreateDomainShader   (&buf[0], buf.size(), 0, (ID3D11DomainShader  **)&dxshdr.m_ptr)); break;
				}
				m_dx_shdr = dxshdr;
				if (last_mod > newest) newest = last_mod;
				PR_INFO(1, pr::FmtS("Shader %s replaced", pr::To<std::string>(cso_filepath).c_str()));
			}
		}

		check.m_last_modified = newest;
		#endif
	}

	// Return the input layout associated with this shader.
	// Note, returns null for all shaders except vertex shaders
	D3DPtr<ID3D11InputLayout> Shader::IpLayout() const
	{
		if (m_shdr_type != EShaderType::VS) return nullptr;
		return m_mgr->GetIP(m_id);
	}
}