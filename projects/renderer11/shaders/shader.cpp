//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/models/nugget.h"
#include "pr/renderer11/models/model_buffer.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/steps/render_step.h"

namespace pr
{
	namespace rdr
	{
		BaseShader::BaseShader(ShaderManager* mgr)
			:m_iplayout()
			,m_vs()
			,m_ps()
			,m_gs()
			,m_hs()
			,m_ds()
			,m_id()
			,m_geom_mask()
			,m_mgr(mgr)
			,m_sort_id()
			,m_bsb()
			,m_rsb()
			,m_dsb()
			,m_name()
		{}

		// Setup the shader ready to be used on 'dle'
		void BaseShader::Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const&, RenderStep const&)
		{
			// Look for a compiled shader object file by the name 'compiled_shader_filename'
			// If the file exists, check the last modified time, and if newer, replace the shader 'ptr' and update 'last_modified'
			#if PR_RDR_RUNTIME_SHADERS
			{
				struct ModCheck
				{
					DWORD  m_last_check;    // Tick value when the last check for a changed shader was made
					time_t m_last_modified; // Support for dynamically loading shaders at runtime (unused when PR_RDR_RUNTIME_SHADERS is not defined)
				};
				static std::unordered_map<RdrId, ModCheck> s_check;
				auto& check = s_check[m_id];

				// Only check every 5 seconds
				enum { check_frequency_ms = 1000 };
				if (GetTickCount() - check.m_last_check < check_frequency_ms) return;
				check.m_last_check = GetTickCount();

				wchar_t const* shader_directory = L"P:\\projects\\renderer11\\shaders\\compiled";
				time_t last_mod, newest = check.m_last_modified;
				wstring256 filepath;

				D3DPtr<ID3D11Device> device;
				dc->GetDevice(&device.m_ptr);

				// Check the VS
				filepath = pr::filesys::CombinePath<wstring256>(shader_directory, pr::To<wstring256>(m_name+".vs.cso"));
				if (pr::filesys::FileExists(filepath) && (last_mod = pr::filesys::GetFileTimeStats(filepath).m_last_modified) > check.m_last_modified)
				{
					std::vector<pr::uint8> buf;
					if (pr::FileToBuffer(filepath.c_str(), buf) && !buf.empty())
					{
						pr::Throw(device->CreateVertexShader(&buf[0], buf.size(), 0, &m_vs.m_ptr));
						if (last_mod > newest) newest = last_mod;
						PR_INFO(1, pr::FmtS("Vertex shader %s replaced", pr::To<std::string>(filepath).c_str()));
					}
				}

				// Check the PS
				filepath = pr::filesys::CombinePath<wstring256>(shader_directory, pr::To<wstring256>(m_name+".ps.cso"));
				if (pr::filesys::FileExists(filepath) && (last_mod = pr::filesys::FileTimeStats(filepath.c_str()).m_last_modified) > check.m_last_modified)
				{
					std::vector<pr::uint8> buf;
					if (pr::FileToBuffer(filepath.c_str(), buf) && !buf.empty())
					{
						pr::Throw(device->CreatePixelShader(&buf[0], buf.size(), 0, &m_ps.m_ptr));
						if (last_mod > newest) newest = last_mod;
						PR_INFO(1, pr::FmtS("Pixel shader %s replaced", pr::To<std::string>(filepath).c_str()));
					}
				}

				check.m_last_modified = newest;
			}
			#else
			(void)dc;
			#endif
		}

		// Undo any changes made by this shader on the dc
		void BaseShader::Cleanup(D3DPtr<ID3D11DeviceContext>& dc)
		{
			(void)dc;
		}

		// Helper for binding 'tex' to a texture slot, along with its sampler
		void BaseShader::BindTextureAndSampler(D3DPtr<ID3D11DeviceContext>& dc, Texture2DPtr tex, UINT slot)
		{
			if (tex != nullptr)
			{
				// Set the shader resource view of the texture and the texture sampler
				dc->PSSetShaderResources(slot, 1, &tex->m_srv.m_ptr);
				dc->PSSetSamplers(slot, 1, &tex->m_samp.m_ptr);
			}
			else
			{
				ID3D11ShaderResourceView* null_srv[1] = {};
				dc->PSSetShaderResources(slot, 1, null_srv);

				ID3D11SamplerState* null_samp[1] = {m_mgr->m_default_sampler_state.m_ptr};
				dc->PSSetSamplers(slot, 1, null_samp);
			}
		}

		// Ref counting cleanup function
		void BaseShader::RefCountZero(pr::RefCount<BaseShader>* doomed)
		{
			BaseShader* shdr = static_cast<BaseShader*>(doomed);
			shdr->m_mgr->Delete(shdr);
		}
	}
}