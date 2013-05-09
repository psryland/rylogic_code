//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/textures/texture_manager.h"
#include "pr/renderer11/textures/texture2d.h"
#include "pr/renderer11/textures/image.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/stock_resources.h"
#include "pr/renderer11/util/util.h"
#include "renderer11/textures/dds_texture_loader.h"
#include "renderer11/textures/wic_texture_loader.h"

using namespace pr::rdr;

// Create a DX texture from a dds,jpg,png,tga,gif,bmp file
void LoadTextureFromFile(D3DPtr<ID3D11Device>& device, wchar_t const* filepath, D3DPtr<ID3D11Texture2D>& tex, D3DPtr<ID3D11ShaderResourceView>& srv)
{
	using namespace DirectX;
	auto extn = pr::filesys::GetExtensionInPlace(filepath);

	// If the file is a dds file, use the faster dds loader
	if (_wcsicmp(extn, L"dds") == 0)
	{
		// This doesn't support some dds formats tho, so might be worth trying the directxtex dds loader
		D3DPtr<ID3D11Resource> res;
		pr::Throw(DirectX::CreateDDSTextureFromFile(device.m_ptr, filepath, &res.m_ptr, &srv.m_ptr));
		pr::Throw(res->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&tex.m_ptr));
	}
	else
	{
		D3DPtr<ID3D11DeviceContext> dc;
		device->GetImmediateContext(&dc.m_ptr);

		// Otherwise, use the WIC loader
		D3DPtr<ID3D11Resource> res;
		pr::Throw(DirectX::CreateWICTextureFromFile(device.m_ptr, dc.m_ptr, filepath, &res.m_ptr, &srv.m_ptr));
	}
}

pr::rdr::TextureManager::TextureManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device)
	:m_alex_tex2d(Allocator<Texture2D>(mem))
	,m_device(device)
	,m_lookup_tex(mem)
	,m_lookup_fname(mem)
	,m_stock_textures()
{
	CreateStockTextures();
}
pr::rdr::TextureManager::~TextureManager()
{
	// Drop references to the stock textures
	m_stock_textures.resize(0);

	// Release the ref added in CreateTexture()
	// When the textures delete themselves, they'll be removed from the lookup
	while (!m_lookup_tex.empty())
	{
		auto iter = begin(m_lookup_tex);
		PR_INFO_EXP(PR_DBG_RDR, pr::PtrRefCount(iter->second) == 1, pr::FmtS("External references to texture %d - %s still exist", iter->second->m_id, iter->second->m_name.c_str()));
		iter->second->Release();
	}
}

// Create a new texture instance.
// 'id' is the id to assign to the created texture instance. Use 'AutoId' to auto generate an id
// 'src' is the initialisation data
// 'tdesc' is a description of the texture to be created
// 'sdesc' is a description of the sampler to use
Texture2DPtr pr::rdr::TextureManager::CreateTexture2D(RdrId id, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc)
{
	// Check whether 'id' already exists, if so, throw.
	if (id != AutoId && m_lookup_tex.find(id) != end(m_lookup_tex))
		throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

	// Allocate a new texture instance and d3d texture resource
	SortKeyId sort_id = m_lookup_tex.size() % sortkey::MaxTextureId;
	Texture2DPtr inst = m_alex_tex2d.New(this, src, tdesc, sdesc, sort_id);
	inst->m_id = AutoId ? MakeId(inst.m_ptr) : id;

	// Add the texture instance to the lookup table
	AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
	return inst;
}

// Create a texture instance from a dds file.
// 'filepath' can be a special string identifying a stock texture (e.g.  #black, #white, #checker, etc)
// Throws if creation fails. On success returns a pointer to the created texture.
Texture2DPtr pr::rdr::TextureManager::CreateTexture2D(RdrId id, SamplerDesc const& sam_desc, wchar_t const* filepath)
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
		LoadTextureFromFile(m_device, filepath, tex, srv);
		AddLookup(m_lookup_fname, texfile_id, tex.m_ptr);
	}

	// Allocate the texture instance
	SortKeyId sort_id = m_lookup_tex.size() % sortkey::MaxTextureId;
	Texture2DPtr inst = m_alex_tex2d.New(this, tex, srv, sam_desc, sort_id);
	inst->m_id     = id == AutoId ? MakeId(inst.m_ptr) : id;
	inst->m_src_id = texfile_id;
	inst->m_name   = pr::To<string32>(pr::filesys::GetFilename<wstring256>(filepath));
	AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
	return inst;
}
Texture2DPtr pr::rdr::TextureManager::CreateTexture2D(RdrId id, SamplerDesc const& sam_desc, char const* filepath)
{
	return CreateTexture2D(id, sam_desc, pr::To<wstring256>(filepath).c_str());
}

// Create a new texture instance that uses the same d3d texture as an existing texture.
// 'id' is the id to assign to this new texture instance. Use 'AutoId' to auto generate an id
// 'existing' is an existing texture instance to clone
// 'sam_desc' is an optional sampler state description to set on the clone.
Texture2DPtr pr::rdr::TextureManager::CloneTexture2D(RdrId id, Texture2DPtr const& existing, SamplerDesc const* sam_desc)
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

// Generates the stock textures
void pr::rdr::TextureManager::CreateStockTextures()
{
	// Calling AddRef so that the textures are not destroyed immediately.
	// This means the only reference to these textures is in the texture lookup map
	{
		pr::uint const data[] = {0};
		Image src = Image::make(1, 1, data, DXGI_FORMAT_R8G8B8A8_UNORM);
		TextureDesc tdesc(src, 1, D3D11_USAGE_IMMUTABLE);
		m_stock_textures.push_back(CreateTexture2D(EStockTexture::Black, src, tdesc, SamplerDesc::ClampSampler()));
	}{
		pr::uint const data[] = {0xFFFFFFFF};
		Image src = Image::make(1, 1, data, DXGI_FORMAT_R8G8B8A8_UNORM);
		TextureDesc tdesc(src, 1, D3D11_USAGE_IMMUTABLE);
		m_stock_textures.push_back(CreateTexture2D(EStockTexture::White, src, tdesc, SamplerDesc::ClampSampler()));
	}{
		pr::uint const data[] =
		{
			0xFFFFFFFF, 0, 0xFFFFFFFF, 0,
			0, 0xFFFFFFFF, 0, 0xFFFFFFFF,
			0xFFFFFFFF, 0, 0xFFFFFFFF, 0,
			0, 0xFFFFFFFF, 0, 0xFFFFFFFF
		};
		Image src = Image::make(4, 4, data, DXGI_FORMAT_R8G8B8A8_UNORM);
		TextureDesc tdesc(src, 0, D3D11_USAGE_IMMUTABLE);
		m_stock_textures.push_back(CreateTexture2D(EStockTexture::Checker, src, tdesc, SamplerDesc::WrapSampler()));
	}
}

