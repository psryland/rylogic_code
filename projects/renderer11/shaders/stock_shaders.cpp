//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/models/input_layout.h"
#include "pr/renderer11/models/nugget.h"
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/util/lock.h"
#include "pr/renderer11/util/stock_resources.h"
#include "renderer11/shaders/cbuffer.h"

namespace pr
{
	namespace rdr
	{
		// include generated header files
		#include "renderer11/shaders/compiled/txfm_tint.vs.h"
		#include "renderer11/shaders/compiled/txfm_tint.ps.h"
		#include "renderer11/shaders/compiled/txfm_tint_pvc_lit.vs.h"
		#include "renderer11/shaders/compiled/txfm_tint_pvc_lit.ps.h"
		#include "renderer11/shaders/compiled/txfm_tint_pvc_lit_tex.vs.h"
		#include "renderer11/shaders/compiled/txfm_tint_pvc_lit_tex.ps.h"
		#include "renderer11/shaders/compiled/txfm_tint_pvc.vs.h"
		#include "renderer11/shaders/compiled/txfm_tint_pvc.ps.h"
		#include "renderer11/shaders/compiled/txfm_tint_tex.vs.h"
		#include "renderer11/shaders/compiled/txfm_tint_tex.ps.h"
		#include "renderer11/shaders/compiled/gbuffer.vs.h"
		#include "renderer11/shaders/compiled/gbuffer.ps.h"
		#include "renderer11/shaders/compiled/dslighting.vs.h"
		#include "renderer11/shaders/compiled/dslighting.ps.h"

		// Set the transform properties of a constants buffer
		template <typename TCBuf> void Txfm(BaseInstance const& inst, SceneView const& view, TCBuf& cb)
		{
			pr::m4x4 o2w = GetO2W(inst);
			pr::m4x4 w2c = pr::GetInverseFast(view.m_c2w);
			pr::m4x4 c2s; if (!FindC2S(inst, c2s)) c2s = view.m_c2s;
			cb.m_o2s = c2s * w2c * o2w;
			cb.m_o2w = o2w;
		}

		// Set the tint properties of a constants buffer
		template <typename TCBuf> void Tint(BaseInstance const& inst, TCBuf& cb)
		{
			pr::Colour32 const* col = inst.find<pr::Colour32>(EInstComp::TintColour32);
			cb.m_tint = col ? *col : pr::ColourWhite;
		}

		// Set the texture properties of a constants buffer
		template <typename TCBuf> void Tex0(NuggetProps const& ddata, TCBuf& cb)
		{
			cb.m_tex2surf0 = ddata.m_tex_diffuse != nullptr
				? ddata.m_tex_diffuse->m_t2s
				: pr::m4x4Identity;
		}

		// TxTint *************************************************************
		struct TxTint :BaseShader
		{
			static void Create(ShaderManager& sm, D3DPtr<ID3D11Device>&)
			{
				VShaderDesc vsdesc(txfm_tint_vs, VertP());
				PShaderDesc psdesc(txfm_tint_ps);
				CBufferDesc cbdesc(sizeof(CBufModel_Forward));
				sm.CreateShader<TxTint>(EStockShader::TxTint, TxTint::Setup, &vsdesc, &psdesc, &cbdesc, "txfm_tint");
			}
			static void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep)
			{
				auto& me = Me<TxTint>(dle);

				// Fill out the model constants buffer and bind it to the VS stage
				CBufModel_Forward cb = {};
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				{
					LockT<CBufModel_Forward> lock(dc, me.m_cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0);
					*lock.ptr() = cb;
				}
				dc->VSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);
				dc->PSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);
			}
			explicit TxTint(ShaderManager* mgr) :BaseShader(mgr) {}
		};

		// TxTintPvc **********************************************************
		struct TxTintPvc :BaseShader
		{
			static void Create(ShaderManager& sm, D3DPtr<ID3D11Device>&)
			{
				VShaderDesc vsdesc(txfm_tint_pvc_vs, VertPC());
				PShaderDesc psdesc(txfm_tint_pvc_ps);
				CBufferDesc cbdesc(sizeof(CBufModel_Forward));
				sm.CreateShader<TxTintPvc>(EStockShader::TxTintPvc, TxTintPvc::Setup, &vsdesc, &psdesc, &cbdesc, "txfm_tint_pvc");
			}
			static void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep)
			{
				auto& me = Me<TxTintPvc>(dle);

				// Fill out the model constants buffer
				CBufModel_Forward cb = {};
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				{
					LockT<CBufModel_Forward> lock(dc, me.m_cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0);
					*lock.ptr() = cb;
				}
				dc->VSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);
				dc->PSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);
			};
			explicit TxTintPvc(ShaderManager* mgr) :BaseShader(mgr) {}
		};

		// TxTintTex **********************************************************
		struct TxTintTex :BaseShader
		{
			D3DPtr<ID3D11SamplerState> m_default_sampler_state;

			static void Create(ShaderManager& sm, D3DPtr<ID3D11Device>& device)
			{
				// Create the shader
				VShaderDesc vsdesc(txfm_tint_tex_vs, VertPT());
				PShaderDesc psdesc(txfm_tint_tex_ps);
				CBufferDesc cbdesc(sizeof(CBufModel_Forward));
				pr::RefPtr<TxTintTex> shdr = sm.CreateShader<TxTintTex>(EStockShader::TxTintTex, TxTintTex::Setup, &vsdesc, &psdesc, &cbdesc, "txfm_tint_tex");

				// Create a texture sampler
				SamplerDesc sdesc;
				pr::Throw(device->CreateSamplerState(&sdesc, &shdr->m_default_sampler_state.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(shdr->m_default_sampler_state, "tex0 sampler"));
			}
			static void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep)
			{
				auto& me = Me<TxTintTex>(dle);

				// Fill out the model constants buffer and bind it to the VS stage
				CBufModel_Forward cb = {};
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				Tex0(*dle.m_nugget, cb);
				{
					LockT<CBufModel_Forward> lock(dc, me.m_cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0);
					*lock.ptr() = cb;
				}

				// Set the constants
				dc->VSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);
				dc->PSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);

				// Set constants for the pixel shader
				auto& tex_diffuse = dle.m_nugget->m_tex_diffuse;
				if (tex_diffuse != nullptr)
				{
					// Set the shader resource view of the texture and the texture sampler
					dc->PSSetShaderResources(0, 1, &tex_diffuse->m_srv.m_ptr);
					dc->PSSetSamplers(0, 1, &tex_diffuse->m_samp.m_ptr);
				}
				else
				{
					dc->PSSetShaderResources(0, 0, 0);
					dc->PSSetSamplers(0, 1, &me.m_default_sampler_state.m_ptr);
				}
			};
			explicit TxTintTex(ShaderManager* mgr) :BaseShader(mgr) {}
		};

		// TxTintPvcLit *******************************************************
		struct TxTintPvcLit :BaseShader
		{
			static void Create(ShaderManager& sm, D3DPtr<ID3D11Device>&)
			{
				// Create the shader
				VShaderDesc vsdesc(txfm_tint_pvc_lit_vs, VertPCNT(), EGeom::Vert|EGeom::Colr|EGeom::Norm);
				PShaderDesc psdesc(txfm_tint_pvc_lit_ps);
				CBufferDesc cbdesc(sizeof(CBufModel_Forward));
				sm.CreateShader<TxTintPvcLit>(EStockShader::TxTintPvcLit, TxTintPvcLit::Setup, &vsdesc, &psdesc, &cbdesc, "txfm_tint_pvc_lit");
			}
			static void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep)
			{
				auto& me = Me<TxTintPvcLit>(dle);

				// Fill out the model constants buffer and bind it to the VS stage
				CBufModel_Forward cb = {};
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				{
					LockT<CBufModel_Forward> lock(dc, me.m_cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0);
					*lock.ptr() = cb;
				}

				// Set the constants
				dc->VSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);
				dc->PSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);
			}
			explicit TxTintPvcLit(ShaderManager* mgr) :BaseShader(mgr) {}
		};

		// TxTintPvcLitTex ****************************************************
		struct TxTintPvcLitTex :BaseShader
		{
			D3DPtr<ID3D11SamplerState> m_default_sampler_state;

			static void Create(ShaderManager& sm, D3DPtr<ID3D11Device>& device)
			{
				// Create the shader
				VShaderDesc vsdesc(txfm_tint_pvc_lit_tex_vs, VertPCNT());
				PShaderDesc psdesc(txfm_tint_pvc_lit_tex_ps);
				CBufferDesc cbdesc(sizeof(CBufModel_Forward));
				pr::RefPtr<TxTintPvcLitTex> shdr = sm.CreateShader<TxTintPvcLitTex>(EStockShader::TxTintPvcLitTex, TxTintPvcLitTex::Setup, &vsdesc, &psdesc, &cbdesc, "txfm_tint_pvc_lit_tex");

				// Create a texture sampler
				SamplerDesc sdesc;
				pr::Throw(device->CreateSamplerState(&sdesc, &shdr->m_default_sampler_state.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(shdr->m_default_sampler_state, "tex0 sampler"));
			}
			static void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep)
			{
				auto& me = Me<TxTintPvcLitTex>(dle);

				// Fill out the model constants buffer and bind it to the VS stage
				CBufModel_Forward cb = {};
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				Tex0(*dle.m_nugget, cb);
				{
					LockT<CBufModel_Forward> lock(dc, me.m_cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0);
					*lock.ptr() = cb;
				}

				// Set constants for the vertex shader
				dc->VSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);
				dc->PSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);

				// Set constants for the pixel shader
				auto& tex_diffuse = dle.m_nugget->m_tex_diffuse;
				if (tex_diffuse != nullptr)
				{
					// Set the shader resource view of the texture and the texture sampler
					dc->PSSetShaderResources(0, 1, &tex_diffuse->m_srv.m_ptr);
					dc->PSSetSamplers(0, 1, &tex_diffuse->m_samp.m_ptr);
				}
				else
				{
					dc->PSSetShaderResources(0, 0, 0);
					dc->PSSetSamplers(0, 1, &me.m_default_sampler_state.m_ptr);
				}
			}
			explicit TxTintPvcLitTex(ShaderManager* mgr) :BaseShader(mgr) {}
		};

		// GBuffer ************************************************************
		struct GBuffer :BaseShader
		{
			D3DPtr<ID3D11SamplerState> m_default_sampler_state;

			static void Create(ShaderManager& sm, D3DPtr<ID3D11Device>& device)
			{
				// Create the shader
				VShaderDesc vsdesc(gbuffer_vs, VertPCNT());
				PShaderDesc psdesc(gbuffer_ps);
				CBufferDesc cbdesc(sizeof(CBufModel_GBuffer));
				pr::RefPtr<GBuffer> shdr = sm.CreateShader<GBuffer>(EStockShader::GBuffer, GBuffer::Setup, &vsdesc, &psdesc, &cbdesc, "gbuffer");

				// Create a texture sampler
				SamplerDesc sdesc;
				pr::Throw(device->CreateSamplerState(&sdesc, &shdr->m_default_sampler_state.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(shdr->m_default_sampler_state, "gbuffer default sampler"));
			}
			static void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep)
			{
				auto& me = Me<GBuffer>(dle);

				// Set the constants for the shader
				CBufModel_GBuffer cb = {};
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				Tex0(*dle.m_nugget, cb);
				{
					LockT<CBufModel_GBuffer> lock(dc, me.m_cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0);
					*lock.ptr() = cb;
				}

				// Set constants for the shader
				dc->VSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);
				dc->PSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);

				// Set constants for the pixel shader
				auto& tex_diffuse = dle.m_nugget->m_tex_diffuse;
				if (tex_diffuse != nullptr)
				{
					// Set the shader resource view of the texture and the texture sampler
					dc->PSSetShaderResources(0, 1, &tex_diffuse->m_srv.m_ptr);
					dc->PSSetSamplers(0, 1, &tex_diffuse->m_samp.m_ptr);
				}
				else
				{
					dc->PSSetShaderResources(0, 0, 0);
					dc->PSSetSamplers(0, 1, &me.m_default_sampler_state.m_ptr);
				}
			}
			explicit GBuffer(ShaderManager* mgr) :BaseShader(mgr) {}
		};

		// DSLighting *********************************************************
		struct DSLighting :BaseShader
		{
			// A point sampler used to sample the gbuffer
			D3DPtr<ID3D11SamplerState> m_point_sampler;

			static void Create(ShaderManager& sm, D3DPtr<ID3D11Device>& device)
			{
				// Create the shader
				VShaderDesc vsdesc(dslighting_vs, VertPCNT());
				PShaderDesc psdesc(dslighting_ps);
				//CBufferDesc cbdesc(sizeof(CBufModel_DSLighting)); // no per-model constants as yet
				auto shdr = sm.CreateShader<DSLighting>(EStockShader::DSLighting, DSLighting::Setup, &vsdesc, &psdesc, nullptr, "dslighting");

				// Create a texture sampler
				auto sdesc = SamplerDesc::PointClamp();
				pr::Throw(device->CreateSamplerState(&sdesc, &shdr->m_point_sampler.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(shdr->m_point_sampler, "dslighting point sampler"));
			}
			static void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep)
			{
				auto& me = Me<DSLighting>(dle);
				auto& gbuffer = rstep.as<DSLightingPass>().m_gbuffer;

				// Set the constants for the shader
				//CBufModel_DSLighting cb = {};
				//Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				//Tint(*dle.m_instance, cb);
				//Tex0(*dle.m_nugget, cb);
				//{
				//	LockT<CBufModel_GBuffer> lock(dc, me.m_cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0);
				//	*lock.ptr() = cb;
				//}

				//// Set constants for the shader
				//dc->VSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);
				//dc->PSSetConstantBuffers(EConstBuf::ModelConstants, 1, &me.m_cbuf.m_ptr);

				// Set constants for the pixel shader
				dc->PSSetSamplers(0, 1, &me.m_point_sampler.m_ptr);
				dc->PSSetShaderResources(0, GBufferCreate::RTCount, (ID3D11ShaderResourceView* const*)gbuffer.m_srv);
			}
			explicit DSLighting(ShaderManager* mgr) :BaseShader(mgr) {}
		};

		// Create the built-in shaders
		void ShaderManager::CreateStockShaders()
		{
			TxTint         ::Create(*this, m_device);
			TxTintPvc      ::Create(*this, m_device);
			TxTintTex      ::Create(*this, m_device);
			TxTintPvcLit   ::Create(*this, m_device);
			TxTintPvcLitTex::Create(*this, m_device);

			GBuffer   ::Create(*this, m_device);
			DSLighting::Create(*this, m_device);
		}

		//// Setup this shader for rendering
		//void Shader::Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, SceneView const& view)
		//{
		//	(void)view;
		//	(void)dle; //todo textures
		//
		//	// Configure the constants buffer for this shader
		////todo	m_map(dc, m_constants, dle, view);
		//
		//	// Bind the constant buffer to the device
		////todo	dc->VSSetConstantBuffers(0, 1, &m_constants.m_ptr);
		//
		//	// Apply the blend state if present
		//	if (m_blend_state)
		//		dc->OMSetBlendState(m_blend_state.m_ptr, 0, 0xffffffff);
		//
		//	// Apply the rasterizer state if present
		//	if (m_rast_state)
		//		dc->RSSetState(m_rast_state.m_ptr);
		//
		//	// Apply the depth buffer state if present
		//	if (m_depth_state)
		//		dc->OMSetDepthStencilState(m_depth_state.m_ptr, 0);
		//}
	}
}
