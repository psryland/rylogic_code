//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
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
namespace pr
{
	namespace rdr
	{
		// Create a DX texture from a 'dds,jpg,png,tga,gif,bmp' file
		void LoadTextureFromFile(D3DPtr<ID3D11Device>& device, wchar_t const* filepath, D3DPtr<ID3D11Texture2D>& tex, D3DPtr<ID3D11ShaderResourceView>& srv)
		{
			using namespace DirectX;
			auto extn = pr::filesys::GetExtensionInPlace(filepath);

			// If the file is a DDS file, use the faster DDS loader
			if (_wcsicmp(extn, L"dds") == 0)
			{
				// This does not support some DDS formats tho, so might be worth trying the 'directxtex' DDS loader
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

		TextureManager::TextureManager(MemFuncs& mem, D3DPtr<ID3D11Device>& device, D3DPtr<ID2D1Factory>& d2dfactory)
			:m_alex_tex2d(Allocator<Texture2D>(mem))
			,m_alex_texgdi(Allocator<TextureGdi>(mem))
			,m_device(device)
			,m_d2dfactory(d2dfactory)
			,m_lookup_tex(mem)
			,m_lookup_fname(mem)
			,m_stock_textures()
			,m_gdiplus()
		{
			CreateStockTextures();
		}

		// Create a new texture instance.
		// 'id' is the id to assign to the created texture instance. Use 'AutoId' to auto generate an id
		// 'src' is the initialisation data
		// 'tdesc' is a description of the texture to be created
		// 'sdesc' is a description of the sampler to use
		Texture2DPtr TextureManager::CreateTexture2D(RdrId id, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc, char const* name)
		{
			// Check whether 'id' already exists, if so, throw.
			if (id != AutoId && m_lookup_tex.find(id) != end(m_lookup_tex))
				throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

			// Allocate a new texture instance and dx texture resource
			SortKeyId sort_id = m_lookup_tex.size() % SortKey::MaxTextureId;
			Texture2DPtr inst = m_alex_tex2d.New(this, src, tdesc, sdesc, sort_id);
			inst->m_id = id == AutoId ? MakeId(inst.m_ptr) : id;
			inst->m_name = name ? name : "";

			// Add the texture instance to the lookup table
			AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
			return inst;
		}

		// Create a texture instance from a DDS file.
		// 'filepath' can be a special string identifying a stock texture (e.g.  #black, #white, #checker, etc)
		// Throws if creation fails. On success returns a pointer to the created texture.
		Texture2DPtr TextureManager::CreateTexture2D(RdrId id, SamplerDesc const& sam_desc, wchar_t const* filepath, char const* name)
		{
			if (filepath == nullptr)
				throw std::exception("Filepath must be given");

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
				return CloneTexture2D(id, stock_tex, &sam_desc, name);
			}

			// Check whether 'id' already exists, if so, throw.
			if (id != AutoId && m_lookup_tex.find(id) == end(m_lookup_tex))
				throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

			D3DPtr<ID3D11Texture2D> tex;
			D3DPtr<ID3D11ShaderResourceView> srv;

			// Look for an existing dx texture corresponding to 'filepath'
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
			SortKeyId sort_id = m_lookup_tex.size() % SortKey::MaxTextureId;
			Texture2DPtr inst = m_alex_tex2d.New(this, tex, srv, sam_desc, sort_id);
			inst->m_id     = id == AutoId ? MakeId(inst.m_ptr) : id;
			inst->m_src_id = texfile_id;
			inst->m_name   = name ? name : pr::To<string32>(pr::filesys::GetFilename<wstring256>(filepath));
			AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
			return inst;
		}
		Texture2DPtr TextureManager::CreateTexture2D(RdrId id, SamplerDesc const& sam_desc, char const* filepath, char const* name)
		{
			return CreateTexture2D(id, sam_desc, pr::To<wstring256>(filepath).c_str(), name);
		}

		// Create a GDI texture instance
		// 'id' is the id to assign to the created texture instance. Use 'AutoId' to auto generate an id
		// 'src' is the initialisation data
		// 'tdesc' is a description of the texture to be created
		// 'sdesc' is a description of the sampler to use
		TextureGdiPtr TextureManager::CreateTextureGdi(RdrId id, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc, char const* name)
		{
			// Check whether 'id' already exists, if so, throw.
			if (id != AutoId && m_lookup_tex.find(id) != end(m_lookup_tex))
				throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

			// Validate
			if (tdesc.Format != DXGI_FORMAT_B8G8R8A8_UNORM &&
				tdesc.Format != DXGI_FORMAT_B8G8R8A8_UNORM_SRGB)
				throw pr::Exception<HRESULT>(E_FAIL, "GDI textures must use the B8G8R8A8 format");
			if (!pr::AllSet(tdesc.MiscFlags, D3D11_RESOURCE_MISC_GDI_COMPATIBLE))
				throw pr::Exception<HRESULT>(E_FAIL, "GDI textures require the GDI compatible flag");
			if (!tdesc.MipLevels == 1)
				throw pr::Exception<HRESULT>(E_FAIL, "GDI textures require the MipLevels == 1");

			// Allocate a new texture instance and dx texture resource
			SortKeyId sort_id = m_lookup_tex.size() % SortKey::MaxTextureId;
			TextureGdiPtr inst = m_alex_texgdi.New(this, src, tdesc, sdesc, sort_id);
			inst->m_id = id == AutoId ? MakeId(inst.m_ptr) : id;
			inst->m_name = name ? name : "";

			// Add the texture instance to the lookup table
			AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
			return inst;
		}
		TextureGdiPtr TextureManager::CreateTextureGdi(RdrId id, Image const& src, char const* name)
		{
			TextureDesc tdesc(src.m_dim.x, src.m_dim.y, 1, DXGI_FORMAT_B8G8R8A8_UNORM);
			tdesc.BindFlags = D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_RENDER_TARGET;
			tdesc.MiscFlags = D3D11_RESOURCE_MISC_GDI_COMPATIBLE;
			return CreateTextureGdi(id, src, tdesc, SamplerDesc::LinearClamp(), name);
		}
		TextureGdiPtr TextureManager::CreateTextureGdi(RdrId id, int w, int h, char const* name)
		{
			return CreateTextureGdi(id, Image(w,h), name);
		}

		// Create a new texture instance that uses the same dx texture as an existing texture.
		// 'id' is the id to assign to this new texture instance. Use 'AutoId' to auto generate an id
		// 'existing' is an existing texture instance to clone
		// 'sam_desc' is an optional sampler state description to set on the clone.
		Texture2DPtr TextureManager::CloneTexture2D(RdrId id, Texture2DPtr const& existing, SamplerDesc const* sam_desc, char const* name)
		{
			// Check whether 'id' already exists, if so, throw.
			if (id != AutoId && m_lookup_tex.find(id) == end(m_lookup_tex))
				throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

			// Allocate a new texture instance that reuses the dx texture resource
			SortKeyId sort_id = m_lookup_tex.size() % SortKey::MaxTextureId;
			Texture2DPtr inst = m_alex_tex2d.New(this, *existing.m_ptr, sort_id);
			inst->m_id = id == AutoId ? MakeId(inst.m_ptr) : id;
			inst->m_name = name ? name : "";

			// If a sampler state is given, recreate the sampler state
			if (sam_desc) inst->SamDesc(*sam_desc);

			// Add the texture instance to the lookup table
			AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
			return inst;
		}

		// Create a new texture instance that wraps an existing dx texture.
		// 'id' is the id to assign to this new texture instance. Use 'AutoId' to auto generate an id
		// 'existing' is an existing dx texture to wrap
		// 'sam_desc' is an optional sampler state description to set on the clone.
		Texture2DPtr TextureManager::CreateTexture2D(RdrId id, D3DPtr<ID3D11Texture2D> existing_tex, D3DPtr<ID3D11ShaderResourceView> existing_srv, SamplerDesc const& sam_desc, char const* name)
		{
			// Check whether 'id' already exists, if so, throw.
			if (id != AutoId && m_lookup_tex.find(id) != end(m_lookup_tex))
				throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

			// Allocate a new texture instance that reuses the dx texture resource
			SortKeyId sort_id = m_lookup_tex.size() % SortKey::MaxTextureId;
			Texture2DPtr inst = m_alex_tex2d.New(this, existing_tex, existing_srv, sam_desc, sort_id);
			inst->m_id = id == AutoId ? MakeId(inst.m_ptr) : id;
			inst->m_name = name ? name : "";

			// Add the texture instance to the lookup table
			AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
			return inst;
		}

		// Delete a texture instance.
		void TextureManager::Delete(Texture2D* tex)
		{
			if (tex == nullptr)
				return;

			// Find 'tex' in the map of RdrIds to texture instances
			// We'll remove this, but first use it as a non-const reference
			TextureLookup::iterator iter = m_lookup_tex.find(tex->m_id);
			PR_ASSERT(PR_DBG_RDR, iter != m_lookup_tex.end(), "Texture not found");

			// If the dx texture will be released when we clean up this texture
			// then check whether it is in the 'fname' lookup table and remove it if it is.
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
		void TextureManager::CreateStockTextures()
		{
			{
				pr::uint const data[] = {0};
				Image src(1, 1, data, DXGI_FORMAT_R8G8B8A8_UNORM);
				TextureDesc tdesc(src, 1, D3D11_USAGE_IMMUTABLE);
				m_stock_textures.push_back(CreateTexture2D(EStockTexture::Black, src, tdesc, SamplerDesc::LinearClamp(), "#black"));
			}{
				pr::uint const data[] = {0xFFFFFFFF};
				Image src(1, 1, data, DXGI_FORMAT_R8G8B8A8_UNORM);
				TextureDesc tdesc(src, 1, D3D11_USAGE_IMMUTABLE);
				m_stock_textures.push_back(CreateTexture2D(EStockTexture::White, src, tdesc, SamplerDesc::LinearClamp(), "#white"));
			}{
				pr::uint const data[] = {0xFF808080};
				Image src(1, 1, data, DXGI_FORMAT_R8G8B8A8_UNORM);
				TextureDesc tdesc(src, 1, D3D11_USAGE_IMMUTABLE);
				m_stock_textures.push_back(CreateTexture2D(EStockTexture::Gray, src, tdesc, SamplerDesc::LinearClamp(), "#gray"));
			}{
				pr::uint const data[] =
				{
					#define X 0xFFFFFFFF
					#define O 0x00000000
					X,X,O,O,X,X,O,O,
					X,X,O,O,X,X,O,O,
					O,O,X,X,O,O,X,X,
					O,O,X,X,O,O,X,X,
					X,X,O,O,X,X,O,O,
					X,X,O,O,X,X,O,O,
					O,O,X,X,O,O,X,X,
					O,O,X,X,O,O,X,X,
					#undef X
					#undef O
				};
				Image src(8, 8, data, DXGI_FORMAT_R8G8B8A8_UNORM);
				TextureDesc tdesc(src, 0, D3D11_USAGE_IMMUTABLE);
				auto sam = SamplerDesc::LinearWrap();
				sam.Filter = D3D11_FILTER_MIN_MAG_MIP_POINT;
				m_stock_textures.push_back(CreateTexture2D(EStockTexture::Checker, src, tdesc, sam, "#checker"));
			}
		}

		// Updates the texture and 'srv' pointers in 'existing' to those provided.
		// If 'all_instances' is true, 'm_lookup_tex' is searched for Texture instances that point to the same
		// dx resource as 'existing'. All are updated to point to the given 'tex' and 'srv' and the RdrId remains unchanged.
		// If 'all_instances' is false, only 'existing' has its dx pointers changed.
		void TextureManager::ReplaceTexture(Texture2D& existing, D3DPtr<ID3D11Texture2D> tex, D3DPtr<ID3D11ShaderResourceView> srv, bool all_instances)
		{
			if (all_instances && existing.m_tex != nullptr)
			{
				// Replace the dx texture in all other texture instances that share it with 'existing'
				for (auto& rhs : m_lookup_tex)
				{
					auto& other = *rhs.second;
					if (other.m_tex.m_ptr != existing.m_tex.m_ptr) continue;
					other.m_tex = tex;
					other.m_srv = srv;
				}
			}
			else
			{
				existing.m_tex = tex;
				existing.m_srv = srv;
			}
		}

		void TextureManager::OnEvent(Evt_RendererDestroy const&)
		{
			// Drop references to the stock textures
			m_stock_textures.resize(0);

			// Release the ref added in CreateTexture()
			// When the textures delete themselves, they'll be removed from the lookup
			while (!m_lookup_tex.empty())
			{
				auto iter = begin(m_lookup_tex);
				PR_INFO_IF(PR_DBG_RDR, pr::PtrRefCount(iter->second) != 1, pr::FmtS("External references to texture %d - %s still exist", iter->second->m_id, iter->second->m_name.c_str()));
				iter->second->Release();
			}
		}
	}
}