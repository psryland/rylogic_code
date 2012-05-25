//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/materials/material_manager.h"
#include "pr/renderer11/materials/textures/texture2d.h"
#include "pr/renderer11/materials/shaders/shader.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/dds_texture_loader.h"
#include "pr/renderer11/util/stock_resources.h"
#include "pr/renderer11/util/util.h"

using namespace pr::rdr;

GUID const TexInfoGUID = {0x506e436e, 0x5a4f, 0x4190, 0x98, 0x43, 0x99, 0x7a, 0x19, 0xa8, 0xd8, 0x69}; // {506E436E-5A4F-4190-9843-997A19A8D869}

pr::rdr::MaterialManager::MaterialManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device)
:m_alex_tex2d(Allocator<Texture2D>(mem))
,m_alex_shader(Allocator<Shader>(mem))
,m_device(device)
,m_lookup_shader(mem)
,m_lookup_tex(mem)
,m_lookup_fname(mem)
{
	CreateStockShaders(*this);
	
	// CreateStockTextures
}
pr::rdr::MaterialManager::~MaterialManager()
{
	// Release the ref added in CreateShader()
	for (auto i = begin(m_lookup_shader), iend = end(m_lookup_shader); i != iend; ++i)
		i->second->Release();
}

// Create a shader.
// Pass nulls for unneeded shaders
pr::rdr::ShaderPtr pr::rdr::MaterialManager::CreateShader(RdrId id, shader::MapConstants map_consts, VShaderDesc const* vsdesc, PShaderDesc const* psdesc)
{
	// If the user has provided a specific id for the shader, look for an existing
	// shader instance with the same name and return it.
	if (id != AutoId)
	{
		// See if 'id' already exists, if not, then we'll carry on and create a new shader.
		ShaderLookup::const_iterator iter = m_lookup_shader.find(id);
		if (iter != m_lookup_shader.end())
		{
			PR_ASSERT(PR_DBG_RDR, vsdesc == 0 && psdesc == 0, "data provided for an existing shader");
			Shader& existing = *(iter->second);
			return &existing;
		}
	}
	
	// Allocate the shader instance
	pr::rdr::ShaderPtr inst = m_alex_shader.New();
	
	// If 'id' doesn't exist (or is Auto), allocate a new shader
	if (vsdesc != 0)
	{
		// Create the shader
		pr::Throw(m_device->CreateVertexShader(vsdesc->m_data, vsdesc->m_size, 0, &inst->m_vs.m_ptr));
		
		// Create the input layout
		pr::Throw(m_device->CreateInputLayout(vsdesc->m_iplayout, UINT(vsdesc->m_iplayout_count), vsdesc->m_data, vsdesc->m_size, &inst->m_iplayout.m_ptr));
		
		// Create a constants buffer for the shader
		pr::Throw(m_device->CreateBuffer(&vsdesc->m_cbuf_desc, 0, &inst->m_constants.m_ptr));
		
		// Set the minimum vertex format mask
		inst->m_geom_mask = vsdesc->m_geom_mask;
	}
	if (psdesc != 0)
	{
		// Create the pixel shader
		pr::Throw(m_device->CreatePixelShader(psdesc->m_data, psdesc->m_size, 0, &inst->m_ps.m_ptr));
	}
	
	// Populate the remaining shader instance variables
	inst->m_id      = id == AutoId ? MakeId(inst.m_ptr) : id;
	inst->m_mat_mgr = this;
	inst->m_map     = map_consts;
	inst->m_name    = L"";
	inst->m_sort_id = m_lookup_shader.size() % pr::rdr::sort_key::MaxShaderId;
	PR_ASSERT(PR_DBG_RDR, !m_lookup_shader.count(inst->m_id), "overwriting an existing shader id");
	m_lookup_shader[inst->m_id] = inst.m_ptr;
	
	// We need to prevent the shader from immediately being destroyed
	// This ref is removed in the destructor
	inst->AddRef();
	return inst;
}

// Create a texture instance.
// 'id' is the id to assign to this texture, use AutoId if you want a new instance regardless of whether there is an existing one or not.
// If 'id' already exists, create a new texture instance (with a new id) that points to the same d3d texture as the existing one.
// If 'id' does not exist, create a new d3d texture initialised with 'data','data_size' and a new texture instance that points to this d3d texture.
// 'pitch' is the size in bytes of the stride of 'data'.
// If 'data' or 'data_size' is 0, the texture is left uninitialised.
// Throws if creation fails. On success returns a pointer to the created texture.
pr::rdr::Texture2DPtr pr::rdr::MaterialManager::CreateTexture2D(RdrId id, pr::rdr::TextureDesc const& desc, void const* data)
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
			Texture2DPtr tex = m_alex_tex2d.New();
			tex->m_tex     = existing.m_tex;
			tex->m_info    = existing.m_info;
			tex->m_id      = MakeId(tex.m_ptr);
			tex->m_mat_mgr = this;
			tex->m_name    = existing.m_name;
			PR_ASSERT(PR_DBG_RDR, !m_lookup_tex.count(tex->m_id), "overwriting an existing texture id");
			m_lookup_tex[tex->m_id] = tex.m_ptr;
			return tex;
		}
	}
	
	// If 'id' doesn't exist (or is Auto), allocate a new d3d texture resource
	D3DPtr<ID3D11Texture2D> tex;
	SubResourceData init_data(data, desc.Pitch, desc.PitchPerSlice);
	pr::Throw(m_device->CreateTexture2D(&desc,  &init_data, &tex.m_ptr));
	
	// Save the texture creation info with the d3d texture, d3d cleans this up.
	TextureDesc info = desc;
	info.TexSrcId = 0; // This file was not derived from a file
	info.SortId   = m_lookup_tex.size() % pr::rdr::sort_key::MaxTextureId;
	pr::Throw(tex->SetPrivateData(TexInfoGUID, sizeof(info), &info));
	
	// Allocate the texture instance and save texture creation info
	pr::rdr::Texture2DPtr inst = m_alex_tex2d.New();
	inst->m_tex     = tex;
	inst->m_info    = info;
	inst->m_id      = id == AutoId ? MakeId(inst.m_ptr) : id;
	inst->m_mat_mgr = this;
	inst->m_name    = L"";
	PR_ASSERT(PR_DBG_RDR, !m_lookup_tex.count(inst->m_id), "overwriting an existing texture id");
	m_lookup_tex[inst->m_id] = inst.m_ptr;
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
pr::rdr::Texture2DPtr pr::rdr::MaterialManager::CreateTexture2D(RdrId id, pr::rdr::TextureDesc const& desc, wchar_t const* filepath)
{
	// Accept stock texture strings: #black, #white, #checker, etc and id's given via string
	if (filepath && filepath[0] == L'#')
	{
		if (::isdigit(filepath[1]))
			id = pr::str::as<RdrId>(filepath + 1, 0, 10);
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
			Texture2DPtr tex    = m_alex_tex2d.New();
			tex->m_tex          = existing.m_tex;
			tex->m_info         = existing.m_info;
			tex->m_id           = MakeId(tex.m_ptr);
			tex->m_mat_mgr      = this;
			tex->m_name         = filepath;
			PR_ASSERT(PR_DBG_RDR, !m_lookup_tex.count(tex->m_id), "overwriting an existing texture id");
			m_lookup_tex[tex->m_id] = tex.m_ptr;
			return tex;
		}
	}
	
	D3DPtr<ID3D11Texture2D> tex;
	TextureDesc info = desc;
	
	// Look for an existing d3d texture corresponding to 'filepath'
	RdrId texfile_id = MakeId(pr::filesys::StandardiseC<std::wstring>(filepath).c_str());
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
		pr::Throw(pr::rdr::CreateDDSTextureFromFile(m_device.m_ptr, filepath, &res.m_ptr, 0, 0));
		pr::Throw(res->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&tex.m_ptr));
		
		info.TexSrcId  = texfile_id;
		info.SortId    = m_lookup_tex.size() % pr::rdr::sort_key::MaxTextureId;
		pr::Throw(tex->SetPrivateData(TexInfoGUID, sizeof(info), &info)); // Attach info to the texture, d3d cleans this up
		m_lookup_fname[texfile_id] = tex.m_ptr;
	}
	
	// Allocate the texture instance
	pr::rdr::Texture2DPtr inst = m_alex_tex2d.New();
	inst->m_tex     = tex;
	inst->m_info    = info;
	inst->m_id      = id == AutoId ? MakeId(inst.m_ptr) : id;
	inst->m_mat_mgr = this;
	inst->m_name    = filepath;
	PR_ASSERT(PR_DBG_RDR, !m_lookup_tex.count(inst->m_id), "overwriting an existing texture id");
	m_lookup_tex[inst->m_id] = inst.m_ptr;
	return inst;
}

// Delete a shader instance
void pr::rdr::MaterialManager::Delete(pr::rdr::Shader const* shdr)
{
	if (shdr == 0) return;
	
	// Find 'shdr' in the map of RdrIds to shader instances
	// We'll remove this, but first use it as a non-const reference
	ShaderLookup::iterator iter = m_lookup_shader.find(shdr->m_id);
	PR_ASSERT(PR_DBG_RDR, iter != m_lookup_shader.end(), "Shader not found");
	
	// Delete the shader and remove the entry from the RdrId lookup map
	m_alex_shader.Delete(iter->second);
	m_lookup_shader.erase(iter);
}

// Delete a texture instance.
void pr::rdr::MaterialManager::Delete(pr::rdr::Texture2D const* tex)
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

// Return a pointer to a shader that is best suited for rendering geometry with the vertex structure described by 'geom_mask'
pr::rdr::ShaderPtr pr::rdr::MaterialManager::FindShaderFor(EGeom::Type geom_mask) const
{
	pr::rdr::Shader const* closest = 0;
	pr::uint matching_bit_count = 0;
	for (auto i = begin(m_lookup_shader), iend = end(m_lookup_shader); i != iend; ++i)
	{
		Shader const& shdr = *i->second;
		
		// Doesn't meet minimum requirements?
		if (!pr::AllSet(geom_mask, shdr.m_geom_mask))
			continue;
		
		// Quick out on an exact match
		if (shdr.m_geom_mask == geom_mask)
			closest = &shdr;
		
		// Otherwise find the shader that uses the most fields of geom_mask
		// 'geom_mask' has all of the 'shdr.m_geom_mask' bits set, plus some extra bits
		// so just finding the 'shdr.m_geom_mask' with the most bits will find the best match
		pr::uint match_count = pr::CountBits((pr::uint)shdr.m_geom_mask);
		if (match_count >= matching_bit_count)
		{
			// Typically, more complex shaders have higher valued geom masks, when the number
			// of matching bits is equal choose the highest mask value to (hopefully) get the better shader
			if (match_count == matching_bit_count && closest != 0 && shdr.m_geom_mask < closest->m_geom_mask)
				continue;
			
			matching_bit_count = match_count;
			closest = &shdr;
		}
	}
	
	// Throw if nothing suitable is found
	if (matching_bit_count == 0)
	{
		std::string msg = pr::Fmt("No suitable shader found that supports geometry mask: %X\nAvailable Shaders:\n" ,geom_mask);
		for (auto i = begin(m_lookup_shader), iend = end(m_lookup_shader); i != iend; ++i)
			msg += pr::Fmt("   %s - geometry mask: %X\n" ,i->second->m_name.c_str() ,i->second->m_geom_mask);
		
		PR_ASSERT(PR_DBG_RDR, false, msg.c_str());
		throw pr::Exception<HRESULT>(E_FAIL, msg);
	}
	return const_cast<Shader*>(closest);
}
