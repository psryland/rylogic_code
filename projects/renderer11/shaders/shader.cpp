//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/shaders/shader_manager.h"

using namespace pr::rdr;

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

