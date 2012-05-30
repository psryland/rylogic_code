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
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/render/scene_view.h"
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
		#include "renderer11/shaders/compiled/txfm_tint_pvc.vs.h"
		#include "renderer11/shaders/compiled/txfm_tint_pvc.ps.h"
	}
}

// Helper function for setting up the two standard constant buffers for a shader
void CreateCBufModel(D3DPtr<ID3D11Device>& device, pr::rdr::ShaderPtr& shdr)
{
	shdr->m_cbuf.resize(1);
	CBufferDesc cbdesc(sizeof(CBufModel));
	pr::Throw(device->CreateBuffer(&cbdesc, 0, &shdr->m_cbuf[0].m_ptr));
}

// Set the transform properties of CBufModel
void Txfm(BaseInstance const& inst, SceneView const& view, CBufModel& cb)
{
	pr::m4x4 o2w = GetO2W(inst);
	pr::m4x4 w2c = pr::GetInverseFast(view.m_c2w);
	pr::m4x4 c2s; if (!FindC2S(inst, c2s)) c2s = view.m_c2s;
	cb.m_o2s = c2s * w2c * o2w;
}

// Set the tint properties of CBufModel
void Tint(BaseInstance const& inst, CBufModel& cb)
{
	pr::Colour const* col = inst.find<pr::Colour>(EInstComp::TintColour32);
	cb.m_tint = col ? *col : pr::ColourWhite;
}

// Create the built-in shaders
void pr::rdr::ShaderManager::CreateStockShaders()
{
	{//TxTint
		BindShaderFunc map = [](D3DPtr<ID3D11DeviceContext>& dc, pr::rdr::DrawMethod const& meth, Nugget const&, BaseInstance const& inst, SceneView const& view)
		{
			CBufModel cb = {};
			Txfm(inst, view, cb);
			Tint(inst, cb);
			*Lock(dc, meth.m_shader->m_cbuf[0], 0, D3D11_MAP_WRITE_DISCARD, 0).ptr<CBufModel>() = cb;
			dc->VSSetConstantBuffers(EConstBuf::ModelConstants, 1, &meth.m_shader->m_cbuf[0].m_ptr);
		};
		
		// Create the shader
		VShaderDesc vsdesc(VertP(), txfm_tint_vs, sizeof(txfm_tint_vs));
		PShaderDesc psdesc(txfm_tint_ps, sizeof(txfm_tint_ps));
		pr::rdr::ShaderPtr shdr = CreateShader(shader::TxTint, map, &vsdesc, &psdesc);
		CreateCBufModel(m_device, shdr);
	}
	{//TxTintPvc
		BindShaderFunc map = [](D3DPtr<ID3D11DeviceContext>& dc, pr::rdr::DrawMethod const& meth, Nugget const&, BaseInstance const& inst, SceneView const& view)
		{
			CBufModel cb = {};
			Txfm(inst, view, cb);
			Tint(inst, cb);
			*Lock(dc, meth.m_shader->m_cbuf[0], 0, D3D11_MAP_WRITE_DISCARD, 0).ptr<CBufModel>() = cb;
			dc->VSSetConstantBuffers(EConstBuf::ModelConstants, 1, &meth.m_shader->m_cbuf[0].m_ptr);
		};

		// Create the shader
		VShaderDesc vsdesc(VertPC(), txfm_tint_pvc_vs, sizeof(txfm_tint_pvc_vs));
		PShaderDesc psdesc(txfm_tint_pvc_ps, sizeof(txfm_tint_pvc_ps));
		pr::rdr::ShaderPtr shdr = CreateShader(shader::TxTintPvc, map, &vsdesc, &psdesc);
		CreateCBufModel(m_device, shdr);
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

}
