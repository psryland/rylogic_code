//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/models/nugget.h"
#include "pr/renderer11/models/input_layout.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/util/lock.h"
#include "renderer11/shaders/constant_buffers.h"

using namespace pr::rdr;

namespace pr
{
	namespace rdr
	{
		// include generated header files
		#include "renderer11/shaders/shaders/vs_txfm_tint.h"
		#include "renderer11/shaders/shaders/ps_txfm_tint.h"
	}
}

// Create the built in shaders
void pr::rdr::CreateStockShaders(pr::rdr::ShaderManager& mgr)
{
	{//TxTint
		BindShaderFunc map = [](D3DPtr<ID3D11DeviceContext>& dc, pr::rdr::DrawMethod const& meth, Nugget const& nugget, BaseInstance const& inst, SceneView const& view)
		{
			(void)nugget;
			(void)view;
			pr::m4x4 o2w = GetO2W(inst);
			pr::m4x4 w2s = pr::GetInverse(view.m_c2w);
			pr::m4x4 o2s = w2s * o2w;
			(void)o2s;
			
			Lock lock;
			lock.Map(dc, meth.m_shader->m_cbuf[0], 0, D3D11_MAP_WRITE_DISCARD, 0);
			//shader::cb0* cb = lock.ptr<shader::cb0>();
			//cb->m_o2s = o2s;
		};
		
		VShaderDesc vsdesc(VertP(), vs_txfm_tint, sizeof(vs_txfm_tint));
		PShaderDesc psdesc(ps_txfm_tint, sizeof(ps_txfm_tint));
		mgr.CreateShader(shader::TxTint, map, &vsdesc, &psdesc);
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

// *******************************************************

pr::rdr::Shader::Shader()
:m_iplayout()
,m_cbuf()
,m_vs()
,m_ps()
,m_gs()
,m_hs()
,m_ds()
,m_blend_state()
,m_depth_state()
,m_rast_state()
,m_id()
,m_geom_mask()
,m_mgr()
,m_name()
,m_sort_id()
{}

// Set the shaders of the dc for the non-null shader pointers
void pr::rdr::Shader::Bind(D3DPtr<ID3D11DeviceContext>& dc) const
{
	// Bind the shaders (passing null disables the shader)
	dc->VSSetShader(m_vs.m_ptr, 0, 0);
	dc->PSSetShader(m_ps.m_ptr, 0, 0);
	dc->GSSetShader(m_gs.m_ptr, 0, 0);
	dc->HSSetShader(m_hs.m_ptr, 0, 0);
	dc->DSSetShader(m_ds.m_ptr, 0, 0);
}
    
// Refcounting cleanup function
void pr::rdr::Shader::RefCountZero(pr::RefCount<Shader>* doomed)
{
	pr::rdr::Shader* shdr = static_cast<pr::rdr::Shader*>(doomed);
	shdr->m_mgr->Delete(shdr);
}

