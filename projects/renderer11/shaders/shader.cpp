//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/shaders/shader_manager.h"

using namespace pr::rdr;

pr::rdr::BaseShader::BaseShader()
:m_iplayout()
,m_cbuf()
,m_vs()
,m_ps()
,m_gs()
,m_hs()
,m_ds()
,m_id()
,m_geom_mask()
,m_mgr()
,m_name()
,m_sort_id()
{}

// Set the shaders of the dc for the non-null shader pointers
void pr::rdr::BaseShader::Bind(D3DPtr<ID3D11DeviceContext>& dc) const
{
	// Bind input format
	dc->IASetInputLayout(m_iplayout.m_ptr);
	
	// Bind the shaders (passing null disables the shader)
	dc->VSSetShader(m_vs.m_ptr, 0, 0);
	dc->PSSetShader(m_ps.m_ptr, 0, 0);
	dc->GSSetShader(m_gs.m_ptr, 0, 0);
	dc->HSSetShader(m_hs.m_ptr, 0, 0);
	dc->DSSetShader(m_ds.m_ptr, 0, 0);
}

// Refcounting cleanup function
void pr::rdr::BaseShader::RefCountZero(pr::RefCount<BaseShader>* doomed)
{
	pr::rdr::BaseShader* shdr = static_cast<pr::rdr::BaseShader*>(doomed);
	shdr->m_mgr->Delete(shdr);
}

