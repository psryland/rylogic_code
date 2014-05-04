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
#include "pr/renderer11/render/render_step.h"
#include "pr/renderer11/util/lock.h"
#include "pr/renderer11/util/stock_resources.h"
#include "pr/renderer11/util/util.h"
#include "renderer11/util/internal_resources.h"
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

		// Convert a geom into an iv4 for flags passed to a shader
		inline pr::iv4 GeomToIV4(EGeom geom)
		{
			 return pr::iv4::make(
				 pr::AllSet(geom, EGeom::Colr),
				 pr::AllSet(geom, EGeom::Norm),
				 pr::AllSet(geom, EGeom::Tex0),
				 0);
		}

		// FwdShader **********************************************************
		struct FwdShader :BaseShader
		{
			D3DPtr<ID3D11Buffer> m_cbuf_model; // Constant buffer used by the shader

			FwdShader(ShaderManager* mgr)
				:BaseShader(mgr)
			{
				// Create a per-model constants buffer
				CBufferDesc cbdesc(sizeof(ForwardRender::CBufModel));
				pr::Throw(mgr->m_device->CreateBuffer(&cbdesc, 0, &m_cbuf_model.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_cbuf_model, "ForwardRender::CBufModel"));
			}
		};

		// TxTint *************************************************************
		struct TxTint :FwdShader
		{
			explicit TxTint(ShaderManager* mgr) :FwdShader(mgr) {}
			static void Create(ShaderManager& sm)
			{
				VShaderDesc vsdesc(txfm_tint_vs, VertP());
				PShaderDesc psdesc(txfm_tint_ps);
				sm.CreateShader<TxTint>(EStockShader::TxTint, &vsdesc, &psdesc, "txfm_tint");
			}
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep) override
			{
				BaseShader::Setup(dc, dle, rstep);

				// Fill out the model constants buffer and bind it to the VS stage
				ForwardRender::CBufModel cb = {};
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				WriteConstants(dc, m_cbuf_model, cb);
			}
		};

		// TxTintPvc **********************************************************
		struct TxTintPvc :FwdShader
		{
			explicit TxTintPvc(ShaderManager* mgr) :FwdShader(mgr) {}
			static void Create(ShaderManager& sm)
			{
				VShaderDesc vsdesc(txfm_tint_pvc_vs, VertPC());
				PShaderDesc psdesc(txfm_tint_pvc_ps);
				sm.CreateShader<TxTintPvc>(EStockShader::TxTintPvc, &vsdesc, &psdesc, "txfm_tint_pvc");
			}
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep) override
			{
				BaseShader::Setup(dc, dle, rstep);

				// Fill out the model constants buffer
				ForwardRender::CBufModel cb = {};
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				WriteConstants(dc, m_cbuf_model, cb);
			}
		};

		// TxTintTex **********************************************************
		struct TxTintTex :FwdShader
		{
			explicit TxTintTex(ShaderManager* mgr) :FwdShader(mgr) {}
			static void Create(ShaderManager& sm)
			{
				// Create the shader
				VShaderDesc vsdesc(txfm_tint_tex_vs, VertPT());
				PShaderDesc psdesc(txfm_tint_tex_ps);
				sm.CreateShader<TxTintTex>(EStockShader::TxTintTex, &vsdesc, &psdesc, "txfm_tint_tex");
			}
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep) override
			{
				BaseShader::Setup(dc, dle, rstep);

				// Fill out the model constants buffer and bind it to the VS stage
				ForwardRender::CBufModel cb = {};
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				Tex0(*dle.m_nugget, cb);
				WriteConstants(dc, m_cbuf_model, cb);

				// Set constants for the pixel shader
				BindTextureAndSampler(dc, dle.m_nugget->m_tex_diffuse);
			}
			void Cleanup(D3DPtr<ID3D11DeviceContext>& dc) override
			{
				BindTextureAndSampler(dc, nullptr);
			}
		};

		// TxTintPvcLit *******************************************************
		struct TxTintPvcLit :FwdShader
		{
			explicit TxTintPvcLit(ShaderManager* mgr) :FwdShader(mgr) {}
			static void Create(ShaderManager& sm)
			{
				// Create the shader
				VShaderDesc vsdesc(txfm_tint_pvc_lit_vs, VertPCNT(), EGeom::Vert|EGeom::Colr|EGeom::Norm);
				PShaderDesc psdesc(txfm_tint_pvc_lit_ps);
				sm.CreateShader<TxTintPvcLit>(EStockShader::TxTintPvcLit, &vsdesc, &psdesc, "txfm_tint_pvc_lit");
			}
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep) override
			{
				BaseShader::Setup(dc, dle, rstep);

				// Fill out the model constants buffer and bind it to the VS stage
				ForwardRender::CBufModel cb = {};
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				WriteConstants(dc, m_cbuf_model, cb);
			}
		};

		// TxTintPvcLitTex ****************************************************
		struct TxTintPvcLitTex :FwdShader
		{
			explicit TxTintPvcLitTex(ShaderManager* mgr) :FwdShader(mgr) {}
			static void Create(ShaderManager& sm)
			{
				// Create the shader
				VShaderDesc vsdesc(txfm_tint_pvc_lit_tex_vs, VertPCNT());
				PShaderDesc psdesc(txfm_tint_pvc_lit_tex_ps);
				sm.CreateShader<TxTintPvcLitTex>(EStockShader::TxTintPvcLitTex, &vsdesc, &psdesc, "txfm_tint_pvc_lit_tex");
			}
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep) override
			{
				BaseShader::Setup(dc, dle, rstep);

				// Fill out the model constants buffer and bind it to the VS stage
				ForwardRender::CBufModel cb = {};
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				Tex0(*dle.m_nugget, cb);
				WriteConstants(dc, m_cbuf_model, cb);

				// Set constants for the pixel shader
				BindTextureAndSampler(dc, dle.m_nugget->m_tex_diffuse);
			}
			void Cleanup(D3DPtr<ID3D11DeviceContext>& dc) override
			{
				BindTextureAndSampler(dc, nullptr);
			}
		};

		// GBuffer ************************************************************
		struct DSGBuffer :BaseShader
		{
			D3DPtr<ID3D11Buffer> m_cbuf_model; // Constant buffer used by the shader

			explicit DSGBuffer(ShaderManager* mgr)
				:BaseShader(mgr)
			{
				// Create a per-model constants buffer
				CBufferDesc cbdesc(sizeof(GBuffer::CBufModel));
				pr::Throw(mgr->m_device->CreateBuffer(&cbdesc, 0, &m_cbuf_model.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_cbuf_model, "GBuffer::CBufModel"));
			}
			static void Create(ShaderManager& sm)
			{
				// Create the shader
				VShaderDesc vsdesc(gbuffer_vs, VertPCNT());
				PShaderDesc psdesc(gbuffer_ps);
				sm.CreateShader<DSGBuffer>(ERdrShader::GBuffer, &vsdesc, &psdesc, "gbuffer");
			}
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep) override
			{
				BaseShader::Setup(dc, dle, rstep);

				// Set the constants for the shader
				GBuffer::CBufModel cb = {};
				cb.m_geom = GeomToIV4(dle.m_nugget->m_geom);
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				Tex0(*dle.m_nugget, cb);
				WriteConstants(dc, m_cbuf_model, cb);

				// Set constants for the pixel shader
				BindTextureAndSampler(dc, dle.m_nugget->m_tex_diffuse);
			}
			void Cleanup(D3DPtr<ID3D11DeviceContext>& dc) override
			{
				BindTextureAndSampler(dc, nullptr);
			}
		};

		// DSLighting *********************************************************
		struct DSLighting :BaseShader
		{
			D3DPtr<ID3D11SamplerState> m_point_sampler; // A point sampler used to sample the gbuffer

			explicit DSLighting(ShaderManager* mgr)
				:BaseShader(mgr)
			{
				// Create a gbuffer sampler
				auto sdesc = SamplerDesc::PointClamp();
				pr::Throw(mgr->m_device->CreateSamplerState(&sdesc, &m_point_sampler.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_point_sampler, "dslighting point sampler"));
			}
			static void Create(ShaderManager& sm)
			{
				// Create the shader
				VShaderDesc vsdesc(dslighting_vs, VertPCNT());
				PShaderDesc psdesc(dslighting_ps);
				sm.CreateShader<DSLighting>(ERdrShader::DSLighting, &vsdesc, &psdesc, "dslighting");
			}
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep) override
			{
				BaseShader::Setup(dc, dle, rstep);

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
				dc->PSSetSamplers(0, 1, &m_point_sampler.m_ptr);
				dc->PSSetShaderResources(0, GBufferCreate::RTCount, (ID3D11ShaderResourceView* const*)gbuffer.m_srv);
			}
			void Cleanup(D3DPtr<ID3D11DeviceContext>& dc) override
			{
				ID3D11ShaderResourceView* null_srv[GBufferCreate::RTCount] = {};
				dc->PSSetShaderResources(0, GBufferCreate::RTCount, null_srv);

				ID3D11SamplerState* null_samp[1] = {};
				dc->PSSetSamplers(0, 1, null_samp);
			}
		};

		// Create the built-in shaders
		void ShaderManager::CreateStockShaders()
		{
			// Forward shaders
			TxTint         ::Create(*this);
			TxTintPvc      ::Create(*this);
			TxTintTex      ::Create(*this);
			TxTintPvcLit   ::Create(*this);
			TxTintPvcLitTex::Create(*this);

			// GBuffer shaders
			DSGBuffer ::Create(*this);
			DSLighting::Create(*this);
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
