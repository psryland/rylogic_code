//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/textures/texture_manager.h"
#include "pr/renderer11/textures/texture2d.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/dds_texture_loader.h"
#include "pr/renderer11/util/stock_resources.h"
#include "pr/renderer11/util/util.h"

using namespace pr::rdr;

//GUID const TexInfoGUID = {0x506e436e, 0x5a4f, 0x4190, 0x98, 0x43, 0x99, 0x7a, 0x19, 0xa8, 0xd8, 0x69}; // {506E436E-5A4F-4190-9843-997A19A8D869}

pr::rdr::TextureManager::TextureManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device)
:m_alex_tex2d(Allocator<Texture2D>(mem))
,m_device(device)
,m_lookup_tex(mem)
,m_lookup_fname(mem)
{
	// CreateStockTextures
}
pr::rdr::TextureManager::~TextureManager()
{
	// Release the ref added in CreateShader()
	while (!m_lookup_tex.empty())
	{
		auto iter = begin(m_lookup_tex);
		PR_INFO_EXP(PR_DBG_RDR, pr::PtrRefCount(iter->second) == 1, pr::FmtS("External references to texture %d - %s still exist", iter->second->m_id, iter->second->m_name.c_str()));
		iter->second->Release();
	}
}

// Create a new texture instance.
// 'id' is the id to assign to the created texture instance. Use 'AutoId' to auto generate an id
// 'tex_desc' is a description of the texture to be created
// 'sam_desc' is an optional sampler description used to initialise the sampler state to use with this texture
// 'data' is an optional pointer to the texture data with which to initialise the texture with
Texture2DPtr pr::rdr::TextureManager::CreateTexture2D(RdrId id, pr::rdr::TextureDesc tex_desc, pr::rdr::SamplerDesc const& sam_desc, void const* data)
{
	// Check whether 'id' already exists, if so, throw.
	if (id != AutoId && m_lookup_tex.find(id) == end(m_lookup_tex))
		throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

	// Allocate a new texture instance and d3d texture resource
	SortKeyId sort_id = m_lookup_tex.size() % pr::rdr::sortkey::MaxTextureId;
	Texture2DPtr inst = m_alex_tex2d.New(this, tex_desc, sam_desc, data, sort_id);
	inst->m_id = AutoId ? MakeId(inst.m_ptr) : id;

	// Add the texture instance to the lookup table
	AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
	return inst;
}

// Create a texture instance from a dds file.
// 'filepath' can be a special string identifying a stock texture (e.g.  #black, #white, #checker, etc)
// Throws if creation fails. On success returns a pointer to the created texture.
Texture2DPtr pr::rdr::TextureManager::CreateTexture2D(RdrId id, pr::rdr::SamplerDesc const& sam_desc, wchar_t const* filepath)
{
	PR_ASSERT(PR_DBG_RDR, filepath != nullptr, "Filepath must be given");

	// Accept stock texture strings: #black, #white, #checker, etc
	// This is handy for model files that contain string paths for textures.
	// The code that loads these models doesn't need to handle strings such as '#white' as a special case
	if (id == AutoId && filepath[0] == L'#')
	{
		EStockTexture stock;
		if (!EStockTexture::TryParse(stock, filepath + 1, false))
			throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Unknown stock texture name: %s", filepath + 1));
		
		auto stock_tex = FindTexture(stock);
		PR_ASSERT(PR_DBG_RDR, stock_tex != nullptr, "Stock texture not found");
		return CloneTexture2D(id, stock_tex, &sam_desc);
	}

	// Check whether 'id' already exists, if so, throw.
	if (id != AutoId && m_lookup_tex.find(id) == end(m_lookup_tex))
		throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

	D3DPtr<ID3D11Texture2D> tex;
	D3DPtr<ID3D11ShaderResourceView> srv;

	// Look for an existing d3d texture corresponding to 'filepath'
	RdrId texfile_id = MakeId(pr::filesys::StandardiseC<wstring256>(filepath).c_str());
	auto iter = m_lookup_fname.find(texfile_id);
	if (iter != m_lookup_fname.end())
	{
		tex = iter->second;
	}
	else // Otherwise, if not loaded already, load now
	{
		// If not, create the d3d texture and populate from the file
		D3DPtr<ID3D11Resource> res;
		pr::Throw(pr::rdr::CreateDDSTextureFromFile(m_device.m_ptr, filepath, &res.m_ptr, &srv.m_ptr, 0));
		pr::Throw(res->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&tex.m_ptr));
		AddLookup(m_lookup_fname, texfile_id, tex.m_ptr);
	}

	// Allocate the texture instance
	SortKeyId sort_id = m_lookup_tex.size() % pr::rdr::sortkey::MaxTextureId;
	Texture2DPtr inst = m_alex_tex2d.New(this, tex, srv, sam_desc, sort_id);
	inst->m_id     = id == AutoId ? MakeId(inst.m_ptr) : id;
	inst->m_src_id = texfile_id;
	inst->m_name   = pr::To<string32>(pr::filesys::GetFilename<wstring256>(filepath));
	AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
	return inst;
}
Texture2DPtr pr::rdr::TextureManager::CreateTexture2D(RdrId id, pr::rdr::SamplerDesc const& sam_desc, char const* filepath)
{
	return CreateTexture2D(id, sam_desc, pr::To<wstring256>(filepath).c_str());
}

// Create a new texture instance that uses the same d3d texture as an existing texture.
// 'id' is the id to assign to this new texture instance. Use 'AutoId' to auto generate an id
// 'existing' is an existing texture instance to clone
// 'sam_desc' is an optional sampler state description to set on the clone.
Texture2DPtr pr::rdr::TextureManager::CloneTexture2D(RdrId id, Texture2DPtr const& existing, pr::rdr::SamplerDesc const* sam_desc)
{
	// Check whether 'id' already exists, if so, throw.
	if (id != AutoId && m_lookup_tex.find(id) == end(m_lookup_tex))
		throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

	// Allocate a new texture instance that reuses the d3d texture resource
	SortKeyId sort_id = m_lookup_tex.size() % pr::rdr::sortkey::MaxTextureId;
	Texture2DPtr inst = m_alex_tex2d.New(this, *existing.m_ptr, sort_id);
	inst->m_id = id == AutoId ? MakeId(inst.m_ptr) : id;

	// If a sampler state is given, recreate the sampler state
	if (sam_desc) inst->SamDesc(*sam_desc);

	// Add the texture instance to the lookup table
	AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
	return inst;
}

// Delete a texture instance.
void pr::rdr::TextureManager::Delete(pr::rdr::Texture2D const* tex)
{
	if (tex == 0) return;
	
	// Find 'tex' in the map of RdrIds to texture instances
	// We'll remove this, but first use it as a non-const reference
	TextureLookup::iterator iter = m_lookup_tex.find(tex->m_id);
	PR_ASSERT(PR_DBG_RDR, iter != m_lookup_tex.end(), "Texture not found");
	
	// If the d3d texture will be released when we clean up this texture
	// then check whether it's in the fname lookup table and remove it if it is.
	if (tex->m_src_id != 0 && tex->m_tex.RefCount() == 1)
	{
		TexFileLookup::iterator jter = m_lookup_fname.find(tex->m_src_id);
		if (jter != m_lookup_fname.end()) m_lookup_fname.erase(jter);
	}
	
	// Delete the texture and remove the entry from the RdrId lookup map
	m_alex_tex2d.Delete(iter->second);
	m_lookup_tex.erase(iter);
}
