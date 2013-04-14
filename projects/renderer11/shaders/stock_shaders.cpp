//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/models/input_layout.h"
//#include "pr/renderer11/models/model_buffer.h"
#include "pr/renderer11/models/nugget.h"
//#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/util/lock.h"
#include "renderer11/shaders/cbuffer.h"

using namespace pr::rdr;

namespace pr
{
	namespace rdr
	{
		// include generated header files
		#include "renderer11/shaders/compiled/txfm_tint.vs.h"
		#include "renderer11/shaders/compiled/txfm_tint.ps.h"
		#include "renderer11/shaders/compiled/txfm_tint_pvc_lit_tex.vs.h"
		#include "renderer11/shaders/compiled/txfm_tint_pvc_lit_tex.ps.h"
		#include "renderer11/shaders/compiled/txfm_tint_pvc.vs.h"
		#include "renderer11/shaders/compiled/txfm_tint_pvc.ps.h"
		#include "renderer11/shaders/compiled/txfm_tint_tex.vs.h"
		#include "renderer11/shaders/compiled/txfm_tint_tex.ps.h"
	}
}

// Helper function for setting up the two standard constant buffers for a shader
void CreateCBufModel(D3DPtr<ID3D11Device>& device, D3DPtr<ID3D11Buffer>& cbuf)
{
	CBufferDesc cbdesc(sizeof(CBufModel));
	pr::Throw(device->CreateBuffer(&cbdesc, 0, &cbuf.m_ptr));
	PR_EXPAND(PR_DBG_RDR, NameResource(cbuf, "CBufModel"));
}

// Set the transform properties of CBufModel
void Txfm(BaseInstance const& inst, SceneView const& view, CBufModel& cb)
{
	pr::m4x4 o2w = GetO2W(inst);
	pr::m4x4 w2c = pr::GetInverseFast(view.m_c2w);
	pr::m4x4 c2s; if (!FindC2S(inst, c2s)) c2s = view.m_c2s;
	cb.m_o2s = c2s * w2c * o2w;
	cb.m_o2w = o2w;
}

// Set the tint properties of CBufModel
void Tint(BaseInstance const& inst, CBufModel& cb)
{
	pr::Colour const* col = inst.find<pr::Colour>(EInstComp::TintColour32);
	cb.m_tint = col ? *col : pr::ColourWhite;
}

// Set the texture properties
void Tex0(DrawMethod const& meth, CBufModel& cb)
{
	cb.m_tex2surf0 = meth.m_tex_diffuse ? meth.m_tex_diffuse->m_t2s : pr::m4x4Identity;
}

// TxTint *************************************************************
struct TxTint :BaseShader
{
	static void Create(ShaderManager& sm, D3DPtr<ID3D11Device>& device)
	{
		VShaderDesc vsdesc(VertP(), txfm_tint_vs); 
		PShaderDesc psdesc(txfm_tint_ps);
		
		pr::rdr::ShaderPtr shdr = sm.CreateShader<TxTint>(EShader::TxTint, TxTint::Setup, &vsdesc, &psdesc, "txfm_tint");
		CreateCBufModel(device, shdr->m_cbuf);		
	}
	static void Setup(D3DPtr<ID3D11DeviceContext>& dc, Nugget const& nugget, BaseInstance const& inst, Scene const& scene)
	{
		//TxTint const* me = static_cast<TxTint const*>(nugget.m_draw.m_shader.m_ptr);
		
		// Fill out the model constants buffer and bind it to the VS stage
		CBufModel cb = {};
		Txfm(inst, scene.m_view, cb);
		Tint(inst, cb);
		*Lock(dc, nugget.m_draw.m_shader->m_cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0).ptr<CBufModel>() = cb;
		dc->VSSetConstantBuffers(EConstBuf::ModelConstants, 1, &nugget.m_draw.m_shader->m_cbuf.m_ptr);
	}
	explicit TxTint(ShaderManager* mgr) :BaseShader(mgr) {}
};

// TxTintPvc ***********************************************************
struct TxTintPvc :BaseShader
{
	static void Create(ShaderManager& sm, D3DPtr<ID3D11Device>& device)
	{
		VShaderDesc vsdesc(VertPC(), txfm_tint_pvc_vs);
		PShaderDesc psdesc(txfm_tint_pvc_ps);
		
		pr::rdr::ShaderPtr shdr = sm.CreateShader<TxTintPvc>(EShader::TxTintPvc, TxTintPvc::Setup, &vsdesc, &psdesc, "txfm_tint_pvc");
		CreateCBufModel(device, shdr->m_cbuf);
	}
	static void Setup(D3DPtr<ID3D11DeviceContext>& dc, Nugget const& nugget, BaseInstance const& inst, Scene const& scene)
	{
		//TxTintPvc const* me = static_cast<TxTintPvc const*>(nugget.m_draw.m_shader.m_ptr);
		
		// Fill out the model constants buffer and bind it to the VS stage
		CBufModel cb = {};
		Txfm(inst, scene.m_view, cb);
		Tint(inst, cb);
		*Lock(dc, nugget.m_draw.m_shader->m_cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0).ptr<CBufModel>() = cb;
		dc->VSSetConstantBuffers(EConstBuf::ModelConstants, 1, &nugget.m_draw.m_shader->m_cbuf.m_ptr);
	};
	explicit TxTintPvc(ShaderManager* mgr) :BaseShader(mgr) {}
};

// TxTintTex ***********************************************************
struct TxTintTex :BaseShader
{
	D3DPtr<ID3D11SamplerState> m_samp_state;
	
	static void Create(ShaderManager& sm, D3DPtr<ID3D11Device>& device)
	{
		// Create the shader
		VShaderDesc vsdesc(VertPT(), txfm_tint_tex_vs);
		PShaderDesc psdesc(txfm_tint_tex_ps);
		
		pr::RefPtr<TxTintTex> shdr = sm.CreateShader<TxTintTex>(EShader::TxTintTex, TxTintTex::Setup, &vsdesc, &psdesc, "txfm_tint_tex");
		CreateCBufModel(device, shdr->m_cbuf);
	
		// Create a texture sampler
		SamplerDesc sdesc;
		pr::Throw(device->CreateSamplerState(&sdesc, &shdr->m_samp_state.m_ptr));
		PR_EXPAND(PR_DBG_RDR, NameResource(shdr->m_samp_state, "tex0 sampler"));
	}
	static void Setup(D3DPtr<ID3D11DeviceContext>& dc, Nugget const& nugget, BaseInstance const& inst, Scene const& scene)
	{
		TxTintTex const* me = static_cast<TxTintTex const*>(nugget.m_draw.m_shader.m_ptr);
		
		// Fill out the model constants buffer and bind it to the VS stage
		CBufModel cb = {};
		Txfm(inst, scene.m_view, cb);
		Tint(inst, cb);
		Tex0(nugget.m_draw, cb);
		*Lock(dc, nugget.m_draw.m_shader->m_cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0).ptr<CBufModel>() = cb;
		dc->VSSetConstantBuffers(EConstBuf::ModelConstants, 1, &nugget.m_draw.m_shader->m_cbuf.m_ptr);
		
		// Set the texture and sampler
		if (nugget.m_draw.m_tex_diffuse != nullptr)
		{
			dc->PSSetShaderResources(0, 1, &nugget.m_draw.m_tex_diffuse->m_srv.m_ptr);
			dc->PSSetSamplers(0, 1, &me->m_samp_state.m_ptr);
		}
	};
	explicit TxTintTex(ShaderManager* mgr) :BaseShader(mgr) {}
};

// TxTintPvcLitTex ***********************************************************
struct TxTintPvcLitTex :BaseShader
{
	D3DPtr<ID3D11SamplerState> m_samp_state;
	
	static void Create(ShaderManager& sm, D3DPtr<ID3D11Device>& device)
	{
		// Create the shader
		VShaderDesc vsdesc(VertPCNT(), txfm_tint_pvc_lit_tex_vs);
		PShaderDesc psdesc(txfm_tint_pvc_lit_tex_ps);
		
		pr::RefPtr<TxTintTex> shdr = sm.CreateShader<TxTintPvcLitTex>(EShader::TxTintPvcLitTex, TxTintPvcLitTex::Setup, &vsdesc, &psdesc, "txfm_tint_pvc_lit_tex");
		CreateCBufModel(device, shdr->m_cbuf);
	
		// Create a texture sampler
		SamplerDesc sdesc;
		pr::Throw(device->CreateSamplerState(&sdesc, &shdr->m_samp_state.m_ptr));
		PR_EXPAND(PR_DBG_RDR, NameResource(shdr->m_samp_state, "tex0 sampler"));
	}
	static void Setup(D3DPtr<ID3D11DeviceContext>& dc, Nugget const& nugget, BaseInstance const& inst, Scene const& scene)
	{
		TxTintPvcLitTex const* me = static_cast<TxTintPvcLitTex const*>(nugget.m_draw.m_shader.m_ptr);
		
		// Fill out the model constants buffer and bind it to the VS stage
		CBufModel cb = {};
		Txfm(inst, scene.m_view, cb);
		Tint(inst, cb);
		Tex0(nugget.m_draw, cb);
		*Lock(dc, nugget.m_draw.m_shader->m_cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0).ptr<CBufModel>() = cb;
		dc->VSSetConstantBuffers(EConstBuf::ModelConstants, 1, &nugget.m_draw.m_shader->m_cbuf.m_ptr);
		
		// Set the texture and sampler
		if (nugget.m_draw.m_tex_diffuse != nullptr)
		{
			dc->PSSetShaderResources(0, 1, &nugget.m_draw.m_tex_diffuse->m_srv.m_ptr);
			dc->PSSetSamplers(0, 1, &me->m_samp_state.m_ptr);
		}
	};
	explicit TxTintPvcLitTex(ShaderManager* mgr) :BaseShader(mgr) {}
};

// Create the built-in shaders
void pr::rdr::ShaderManager::CreateStockShaders()
{
	TxTint         ::Create(*this, m_device);
	TxTintPvc      ::Create(*this, m_device);
	TxTintTex      ::Create(*this, m_device);
	TxTintPvcLitTex::Create(*this, m_device);
}



//// Setup this shader for rendering
//void pr::rdr::Shader::Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, SceneView const& view)
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
