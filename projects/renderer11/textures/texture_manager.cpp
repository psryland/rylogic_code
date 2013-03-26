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

GUID const TexInfoGUID = {0x506e436e, 0x5a4f, 0x4190, 0x98, 0x43, 0x99, 0x7a, 0x19, 0xa8, 0xd8, 0x69}; // {506E436E-5A4F-4190-9843-997A19A8D869}

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

// Create a texture instance.
// 'id' is the id to assign to this texture, use AutoId if you want a new instance regardless of whether there is an existing one or not.
// If 'id' already exists, create a new texture instance (with a new id) that points to the same d3d texture as the existing one.
// If 'id' does not exist, create a new d3d texture initialised with 'data','data_size' and a new texture instance that points to this d3d texture.
// 'pitch' is the size in bytes of the stride of 'data'.
// If 'data' or 'data_size' is 0, the texture is left uninitialised.
// Throws if creation fails. On success returns a pointer to the created texture.
pr::rdr::Texture2DPtr pr::rdr::TextureManager::CreateTexture2D(RdrId id, pr::rdr::TextureDesc const& desc, void const* data)
{
	// If the user has provided a specific id for the texture, look for an existing
	// texture instance with the same name and copy it (sharing the d3d texture).
	if (id != AutoId)
	{
		// See if 'id' already exists, if not, then we'll carry on and
		// create a new texture and d3d texture.
		TextureLookup::const_iterator iter = m_lookup_tex.find(id);
		if (iter != m_lookup_tex.end())
		{
			PR_ASSERT(PR_DBG_RDR, data == 0, "data provided for an existing texture");
			Texture2D& existing = *(iter->second);
			
			// Duplicate the texture instance, but reuse the underlying d3d texture
			Texture2DPtr tex = m_alex_tex2d.New(this);
			tex->m_tex  = existing.m_tex;
			tex->m_info = existing.m_info;
			tex->m_id   = MakeId(tex.m_ptr);
			tex->m_name = existing.m_name;
			AddLookup(m_lookup_tex, tex->m_id, tex.m_ptr);
			return tex;
		}
	}
	
	// If 'id' doesn't exist (or is Auto), allocate a new texture and d3d texture resource
	pr::rdr::Texture2DPtr inst = m_alex_tex2d.New(this);
	id = id == AutoId ? MakeId(inst.m_ptr) : id;
	
	// Create the d3d resource
	D3DPtr<ID3D11Texture2D> tex;
	SubResourceData init_data(data, desc.Pitch, desc.PitchPerSlice);
	pr::Throw(m_device->CreateTexture2D(&desc,  &init_data, &tex.m_ptr));
	PR_EXPAND(PR_DBG_RDR, pr::rdr::NameResource(tex, pr::FmtS("tex <RdrId:%d>", id)));
	
	// Create a shader resource view of the texture
	ShaderResViewDesc srv_desc(desc.Format, D3D11_SRV_DIMENSION_TEXTURE2D);
	srv_desc.Texture2D.MipLevels = desc.MipLevels;
	pr::Throw(m_device->CreateShaderResourceView(tex.m_ptr, &srv_desc, &inst->m_srv.m_ptr));
	
	// Save the texture creation info with the d3d texture, d3d cleans this up.
	TextureDesc info = desc;
	info.TexSrcId = 0; // This file was not derived from a file
	info.SortId   = m_lookup_tex.size() % pr::rdr::sortkey::MaxTextureId;
	pr::Throw(tex->SetPrivateData(TexInfoGUID, sizeof(info), &info));
	
	inst->m_tex  = tex;
	inst->m_info = info;
	inst->m_id   = id;
	inst->m_name = "";
	AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
	return inst;
}

// Create a texture instance from a dds file.
// 'filepath' can be a special string identifying a stock texture (e.g.  #black, #white, #checker, etc)
// If 'id' does not exist, creates a d3d texture corresponding to 'filepath' (loads if not already loaded)
// If 'id' already exists, create a new texture instance (with a new id) that points to the same d3d texture as the existing texture.
// If 'id' does not exist, get a d3d texture corresponding to 'filepath' (load if not already loaded) and 
//  a new texture instance that points to this d3d texture.
// If width/height are 0 the dimensions of the image file are used as the texture size.
// Throws if creation fails. On success returns a pointer to the created texture.
pr::rdr::Texture2DPtr pr::rdr::TextureManager::CreateTexture2D(RdrId id, pr::rdr::TextureDesc const& desc, wchar_t const* filepath)
{
	// Accept stock texture strings: #black, #white, #checker, etc and id's given via string
	if (filepath && filepath[0] == L'#')
	{
		if (::isdigit(filepath[1]))
			id = pr::str::as<RdrId>(filepath + 1, (wchar_t**)0, 10);
		else
		{
			id = pr::rdr::EStockTexture::Parse(filepath + 1);
			if (id == pr::rdr::EStockTexture::NumberOf)
				throw pr::Exception<HRESULT>(E_FAIL, "Failed to create stock texture");
		}
	}
	
	// See if 'id' already exists
	if (id != AutoId)
	{
		TextureLookup::const_iterator iter = m_lookup_tex.find(id);
		if (iter != m_lookup_tex.end())
		{
			// Duplicate the texture instance, but reuse the d3d texture
			Texture2D& existing = *(iter->second);
			Texture2DPtr tex = m_alex_tex2d.New(this);
			tex->m_tex  = existing.m_tex;
			tex->m_info = existing.m_info;
			tex->m_id   = MakeId(tex.m_ptr);
			tex->m_name = pr::str::ToAString<string32>(filepath);
			AddLookup(m_lookup_tex, tex->m_id, tex.m_ptr);
			return tex;
		}
	}

	D3DPtr<ID3D11Texture2D> tex;
	TextureDesc info = desc;
	Texture2D::SRVPtr srv;

	// Look for an existing d3d texture corresponding to 'filepath'
	RdrId texfile_id = MakeId(pr::filesys::StandardiseC<wstring256>(filepath).c_str());
	TexFileLookup::const_iterator iter = m_lookup_fname.find(texfile_id);
	if (iter != m_lookup_fname.end())
	{
		tex = iter->second;
		UINT info_size = sizeof(info);
		tex->GetPrivateData(TexInfoGUID, &info_size, &info);
	}
	
	// Otherwise, if not loaded already, load now
	else
	{
		// If not allocate the texture and populate from the file
		D3DPtr<ID3D11Resource> res;
		pr::Throw(pr::rdr::CreateDDSTextureFromFile(m_device.m_ptr, filepath, &res.m_ptr, &srv.m_ptr, 0));
		pr::Throw(res->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&tex.m_ptr));
		PR_EXPAND(PR_DBG_RDR, pr::rdr::NameResource(tex, pr::FmtS("tex <File: %s>", filepath)));
		
		info.TexSrcId  = texfile_id;
		info.SortId    = m_lookup_tex.size() % pr::rdr::sortkey::MaxTextureId;
		pr::Throw(tex->SetPrivateData(TexInfoGUID, sizeof(info), &info)); // Attach info to the texture, d3d cleans this up
		
		AddLookup(m_lookup_fname, texfile_id, tex.m_ptr);
	}
	
	// Allocate the texture instance
	pr::rdr::Texture2DPtr inst = m_alex_tex2d.New(this);
	inst->m_tex  = tex;
	inst->m_info = info;
	inst->m_id   = id == AutoId ? MakeId(inst.m_ptr) : id;
	inst->m_srv  = srv;
	inst->m_name = pr::str::ToAString<string32>(filepath);
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
	if (tex->m_info.TexSrcId != 0 && tex->m_tex.RefCount() == 1)
	{
		TexFileLookup::iterator jter = m_lookup_fname.find(tex->m_info.TexSrcId);
		if (jter != m_lookup_fname.end()) m_lookup_fname.erase(jter);
	}
	
	// Delete the texture and remove the entry from the RdrId lookup map
	m_alex_tex2d.Delete(iter->second);
	m_lookup_tex.erase(iter);
}
