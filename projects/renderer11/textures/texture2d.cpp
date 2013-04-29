//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/textures/texture2d.h"
#include "pr/renderer11/textures/texture_manager.h"

using namespace pr::rdr;

pr::rdr::Texture2D::Texture2D(TextureManager* mgr, D3DPtr<ID3D11Texture2D>& tex, D3DPtr<ID3D11ShaderResourceView>& srv, SamplerDesc const& sam_desc, SortKeyId sort_id)
	:m_t2s(pr::m4x4Identity)
	,m_tex(tex)
	,m_srv(srv)
	,m_samp()
	,m_id()
	,m_src_id()
	,m_sort_id(sort_id)
	,m_has_alpha(false)
	,m_mgr(mgr)
	,m_name()
{
	SamDesc(sam_desc);
}
pr::rdr::Texture2D::Texture2D(TextureManager* mgr, TextureDesc const& tex_desc, SamplerDesc const& sam_desc, void const* data, SortKeyId sort_id)
	:m_t2s(pr::m4x4Identity)
	,m_tex()
	,m_srv()
	,m_samp()
	,m_id()
	,m_src_id()
	,m_sort_id(sort_id)
	,m_has_alpha(false)
	,m_mgr(mgr)
	,m_name()
{
	TexDesc(tex_desc, data);
	SamDesc(sam_desc);
}
pr::rdr::Texture2D::Texture2D(TextureManager* mgr, Texture2D const& existing, SortKeyId sort_id)
	:m_t2s(existing.m_t2s)
	,m_tex(existing.m_tex)
	,m_srv(existing.m_srv)
	,m_samp(existing.m_samp)
	,m_id()
	,m_src_id(existing.m_src_id)
	,m_sort_id(sort_id)
	,m_has_alpha(false)
	,m_mgr(mgr)
	,m_name()
{}

// Get/Set the description of the current texture pointed to by 'm_tex'
// Setting a new texture description, re-creates the texture and the srv
TextureDesc pr::rdr::Texture2D::TexDesc() const
{
	TextureDesc desc;
	if (m_tex != nullptr) m_tex->GetDesc(&desc);
	return desc;
}
void pr::rdr::Texture2D::TexDesc(TextureDesc const& desc, void const* data)
{
	// Create the d3d resource
	D3DPtr<ID3D11Texture2D> tex;
	SubResourceData init_data(data, desc.Width, 0);
	pr::Throw(m_mgr->m_device->CreateTexture2D(&desc,  &init_data, &tex.m_ptr));

	// Create a shader resource view of the texture
	D3DPtr<ID3D11ShaderResourceView> srv;
	ShaderResViewDesc srv_desc(desc.Format, D3D11_SRV_DIMENSION_TEXTURE2D);
	srv_desc.Texture2D.MipLevels = desc.MipLevels;
	pr::Throw(m_mgr->m_device->CreateShaderResourceView(tex.m_ptr, &srv_desc, &srv.m_ptr));

	// Exception-safely save the pointers
	m_tex = tex;
	m_srv = srv;
}

// Returns a description of the current sampler state pointed to by 'm_samp'
SamplerDesc pr::rdr::Texture2D::SamDesc() const
{
	SamplerDesc desc;
	if (m_samp != nullptr) m_samp->GetDesc(&desc);
	return desc;
}
void pr::rdr::Texture2D::SamDesc(SamplerDesc const& desc)
{
	D3DPtr<ID3D11SamplerState> samp_state;
	pr::Throw(m_mgr->m_device->CreateSamplerState(&desc, &samp_state.m_ptr));
	m_samp = samp_state;
}

// Refcounting cleanup function
void pr::rdr::Texture2D::RefCountZero(pr::RefCount<Texture2D>* doomed)
{
	pr::rdr::Texture2D* tex = static_cast<pr::rdr::Texture2D*>(doomed);
	tex->m_mgr->Delete(tex);
}