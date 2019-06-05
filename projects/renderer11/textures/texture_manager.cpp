//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/textures/texture_manager.h"
#include "pr/renderer11/textures/texture_2d.h"
#include "pr/renderer11/textures/image.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/util.h"
#include "renderer11/textures/dds_texture_loader.h"
#include "renderer11/textures/wic_texture_loader.h"

namespace pr::rdr
{
	// Create a DX texture from a 'DDS,JPG,PNG,TGA,GIF,BMP' file or data in memory
	void LoadTextureFromFile(ID3D11Device* device, wchar_t const* filepath, ID3D11Texture2D*& tex, ID3D11ShaderResourceView*& srv)
	{
		auto extn = pr::filesys::GetExtensionInPlace(filepath);

		// If the file is a DDS file, use the faster DDS loader
		if (_wcsicmp(extn, L"DDS") == 0)
		{
			// This does not support some DDS formats tho, so might be worth trying the 'directxtex' DDS loader
			D3DPtr<ID3D11Resource> res;
			pr::Throw(DirectX::CreateDDSTextureFromFile(device, filepath, &res.m_ptr, &srv));
			pr::Throw(res->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&tex));
		}
		else
		{
			D3DPtr<ID3D11DeviceContext> dc;
			device->GetImmediateContext(&dc.m_ptr);

			// Otherwise, use the WIC loader
			D3DPtr<ID3D11Resource> res;
			pr::Throw(DirectX::CreateWICTextureFromFile(device, dc.m_ptr, filepath, &res.m_ptr, &srv));
			pr::Throw(res->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&tex));
		}
	}
	void LoadTextureFromMemory(ID3D11Device* device, void const* data, size_t size, bool dds, ID3D11Texture2D*& tex, ID3D11ShaderResourceView*& srv)
	{
		// If the data is a DDS file, use the faster DDS loader
		if (dds)
		{
			// This does not support some DDS formats tho, so might be worth trying the 'directxtex' DDS loader
			D3DPtr<ID3D11Resource> res;
			pr::Throw(DirectX::CreateDDSTextureFromMemory(device, data, size, &res.m_ptr, &srv));
			pr::Throw(res->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&tex));
		}
		else
		{
			D3DPtr<ID3D11DeviceContext> dc;
			device->GetImmediateContext(&dc.m_ptr);

			// Otherwise, use the WIC loader
			D3DPtr<ID3D11Resource> res;
			pr::Throw(DirectX::CreateWICTextureFromMemory(device, dc.m_ptr, static_cast<uint8_t const*>(data), size, &res.m_ptr, &srv));
			pr::Throw(res->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&tex));
		}
	}

	// Constructor
	TextureManager::TextureManager(MemFuncs mem, Renderer& rdr)
		:m_mem_funcs(mem)
		,m_mem_tracker()
		,m_rdr(rdr)
		,m_lookup_tex(mem)
		,m_lookup_fname(mem)
		,m_stock_textures()
		,m_gdiplus()
		,m_eh_resize()
		,m_gdi_dc_ref_count()
	{
		m_eh_resize = m_rdr.BackBufferSizeChanged += [this](Window&,BackBufferSizeChangedEventArgs const&)
		{
			assert("Outstanding DC references during resize" && m_gdi_dc_ref_count == 0);
		};

		// Create the basic textures that exist from startup
		CreateStockTextures();
	}

	// Create a new texture instance.
	// 'id' is the id to assign to the created texture instance. Use 'AutoId' to auto generate an id
	// 'src' is the initialisation data
	// 'tdesc' is a description of the texture to be created
	// 'sdesc' is a description of the sampler to use
	Texture2DPtr TextureManager::CreateTexture2D(RdrId id, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc, bool has_alpha, char const* name)
	{
		Renderer::Lock lock(m_rdr);

		// Check whether 'id' already exists, if so, throw.
		if (id != AutoId && m_lookup_tex.find(id) != std::end(m_lookup_tex))
			throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

		// Allocate a new texture instance and DX texture resource
		SortKeyId sort_id = m_lookup_tex.size() % SortKey::MaxTextureId;
		Texture2DPtr inst(Allocator<Texture2D>(m_mem_funcs).New(this, id, src, tdesc, sdesc, sort_id, has_alpha, name), true);
		assert(m_mem_tracker.add(inst.get()));

		// Add the texture instance to the lookup table
		AddLookup(m_lookup_tex, inst->m_id, inst.get());
		return inst;
	}

	// Create a texture instance from a filepath, embedded resource, or stock texture id.
	// 'resource_path' has the following formats:
	// '#<stock_texture_name>' - '#' indicates stock texture, the remaining text is the stock texture name  (e.g.  #black, #white, #checker, etc)
	// '@<module>:<resource_type>:<resource_name>' - '@' indicates embedded resource, <module> and <resource_type> are optional  (e.g. @::tex_resource, @00FE123:jpg:tex_resource)
	// '<filepath>' - All other strings are interpreted as filepaths.
	// Throws if creation fails. On success returns a pointer to the created texture.
	Texture2DPtr TextureManager::CreateTexture2D(RdrId id, wchar_t const* resource_name, SamplerDesc const& sam_desc, bool has_alpha, char const* name)
	{
		Renderer::Lock lock(m_rdr);

		// Check whether 'id' already exists, if so return it.
		if (id != AutoId)
		{
			auto tex = FindTexture<Texture2D>(id);
			if (tex != nullptr)
				return tex;
		}

		// The resource name is required
		if (resource_name == nullptr || *resource_name == 0)
			throw std::runtime_error("Filepath must be given");

		// Accept stock texture strings: #black, #white, #checker, etc
		// This is handy for model files that contain string paths for textures.
		// The code that loads these models doesn't need to handle strings such as '#white' as a special case
		if (resource_name[0] == '#')
		{
			EStockTexture stock;
			if (!TryParse(stock, resource_name + 1, false))
				throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Unknown stock texture name: %s", resource_name + 1));

			// Return a clone of the stock texture
			auto stock_tex = FindStockTexture(stock);
			assert(stock_tex != nullptr);
			return CloneTexture2D(id, stock_tex.get(), &sam_desc, name);
		}

		ID3D11Texture2D* tex;
		ID3D11ShaderResourceView* srv;
		string32 tex_name;
		RdrId src_id;

		// Create a texture from embedded resource
		if (resource_name[0] == '@')
		{
			src_id = MakeId(resource_name);
			tex_name = name ? name : To<string32>(str::FindLastOf(resource_name, L":"));

			// Look for an existing DX texture corresponding to the resource
			auto iter = m_lookup_fname.find(src_id);
			if (iter != end(m_lookup_fname))
			{
				tex = iter->second;
			}
			else
			{
				// Parse the embedded resource string: "@<module>:<res_type>:<res_name>"
				HMODULE hmodule = nullptr;
				std::wstring res_type, res_name;
				str::Split(resource_name + 1, ":", [&](auto const& s, size_t i, size_t j, int n)
				{
					switch (n)
					{
					case 0: hmodule = reinterpret_cast<HMODULE>(std::wcstoll(&s[i], nullptr, 16) + (char*)nullptr); break;
					case 1: res_type.assign(&s[i], j - i); break;
					case 2: res_name.assign(&s[i], j - i); break;
					}
				});

				// Get the embedded resource
				auto res = pr::resource::Read<char>(res_name.c_str(), res_type.c_str(), hmodule);
				LoadTextureFromMemory(lock.D3DDevice(), res.m_data, res.m_len, str::EqualI(res_type, "dds"), tex, srv);
				AddLookup(m_lookup_fname, src_id, tex);
			}
		}
		// Otherwise, create from file on disk
		else
		{
			// Generate an id from the filepath
			src_id = MakeId(pr::filesys::Standardise<wstring256>(resource_name).c_str());
			tex_name = name ? name : To<string32>(filesys::GetFilename<wstring256>(resource_name));

			// Look for an existing DX texture corresponding to the filepath
			auto iter = m_lookup_fname.find(src_id);
			if (iter != m_lookup_fname.end())
			{
				tex = iter->second;
			}
			// Otherwise, if not loaded already, load now
			else
			{
				LoadTextureFromFile(lock.D3DDevice(), resource_name, tex, srv);
				AddLookup(m_lookup_fname, src_id, tex);
			}
		}

		// Allocate the texture instance
		auto sort_id = static_cast<SortKeyId>(m_lookup_tex.size() % SortKey::MaxTextureId);
		Texture2DPtr inst(Allocator<Texture2D>(m_mem_funcs).New(this, id, tex, srv, sam_desc, sort_id, has_alpha, tex_name.c_str()), true);
		assert(m_mem_tracker.add(inst.get()));
		inst->m_src_id = src_id;
		AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
		return std::move(inst);
	}

	// Create a new texture instance that wraps an existing DX texture.
	// 'id' is the id to assign to this new texture instance. Use 'AutoId' to auto generate an id
	// 'existing' is an existing dx texture to wrap
	// 'sam_desc' is an optional sampler state description to set on the clone.
	Texture2DPtr TextureManager::CreateTexture2D(RdrId id, ID3D11Texture2D* existing_tex, ID3D11ShaderResourceView* existing_srv, SamplerDesc const& sam_desc, bool has_alpha, char const* name)
	{
		Renderer::Lock lock(m_rdr);

		// Check whether 'id' already exists, if so, throw.
		if (id != AutoId && m_lookup_tex.find(id) != std::end(m_lookup_tex))
			throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

		// Allocate a new texture instance that reuses the DX texture resource
		SortKeyId sort_id = m_lookup_tex.size() % SortKey::MaxTextureId;
		Texture2DPtr inst(Allocator<Texture2D>(m_mem_funcs).New(this, id, existing_tex, existing_srv, sam_desc, sort_id, has_alpha, name), true);
		assert(m_mem_tracker.add(inst.get()));

		// Add the texture instance to the lookup table
		AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
		return inst;
	}

	// Create a new texture instance that wraps a shared texture resource.
	// 'shared_resource' is a resource created on another d3d device (possible dx9,dx10,etc).
	Texture2DPtr TextureManager::CreateTexture2D(RdrId id, IUnknown* shared_resource, SamplerDesc const& sdesc, bool has_alpha, char const* name)
	{
		Renderer::Lock lock(m_rdr);

		// Check whether 'id' already exists, if so, throw.
		if (id != AutoId && m_lookup_tex.find(id) != std::end(m_lookup_tex))
			throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

		// Allocate a new texture instance and wrap the shared resource
		SortKeyId sort_id = m_lookup_tex.size() % SortKey::MaxTextureId;
		Texture2DPtr inst(Allocator<Texture2D>(m_mem_funcs).New(this, id, shared_resource, sdesc, sort_id, has_alpha, name), true);
		assert(m_mem_tracker.add(inst.get()));

		// Add the texture instance to the lookup table
		AddLookup(m_lookup_tex, inst->m_id, inst.get());
		return inst;
	}

	// Create a GDI texture instance
	// 'id' is the id to assign to the created texture instance. Use 'AutoId' to auto generate an id
	// 'src' is the initialisation data
	// 'tdesc' is a description of the texture to be created
	// 'sdesc' is a description of the sampler to use
	Texture2DPtr TextureManager::CreateTextureGdi(RdrId id, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc, bool has_alpha, char const* name)
	{
		// Here is a list of things you should know/do to make it all work:
		// Check the surface requirements for a GetDC method to work here: http://msdn.microsoft.com/en-us/library/windows/desktop/ff471345(v=vs.85).aspx
		// - You must create the surface by using the D3D11_RESOURCE_MISC_GDI_COMPATIBLE flag
		//   for a surface or by using the DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE flag for swap chains,
		//   otherwise this method fails.
		// - You must release the device and call the IDXGISurface1::ReleaseDC method before you
		//   issue any new Direct3D commands.
		// - This method fails if an outstanding DC has already been created by this method.
		// - The format for the surface or swap chain must be DXGI_FORMAT_B8G8R8A8_UNORM_SRGB
		//   or DXGI_FORMAT_B8G8R8A8_UNORM.
		// - On GetDC, the render target in the output merger of the Direct3D pipeline is
		//   unbound from the surface. You must call the ID3D11DeviceContext::OMSetRenderTargets
		//   method on the device prior to Direct3D rendering after GDI rendering.
		// - Prior to resizing buffers you must release all outstanding DCs.
		// - If you're going to use it in the back buffer, remember to re-bind render target
		//   after you've called ReleaseDC. It is not necessary to manually unbind RT before
		//   calling GetDC as this method does that for you.
		// - You can not use any Direct3D drawing between GetDC() and ReleaseDC() calls as the
		//   surface is exclusively locked out by DXGI for GDI. However you can mix GDI and D3D
		//   rendering provided that you call GetDC()/ReleaseDC() every time you need to use GDI,
		//   before moving on to D3D.
		//
		// This last bit may sounds easy, but you'd be surprised how many developers fall into
		// this issue
		// - when you draw with GDI on the back buffer, remember that this is the back buffer,
		//   not a frame buffer, so in order to actually see what you've drawn, you have to
		//   re-bind RT to OM and call swapChain->Present() method so the back buffer will
		//   become a frame buffer and its contents will be displayed on the screen.

		Renderer::Lock lock(m_rdr);

		// Check whether 'id' already exists, if so, throw.
		if (id != AutoId && m_lookup_tex.find(id) != std::end(m_lookup_tex))
			throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

		// Validate
		// Note: GDI compatible textures do not require the main swap chain to be GDI compatible.
		if (tdesc.Format != DXGI_FORMAT_B8G8R8A8_UNORM &&
			tdesc.Format != DXGI_FORMAT_B8G8R8A8_UNORM_SRGB)
			throw pr::Exception<HRESULT>(E_FAIL, "GDI textures must use the B8G8R8A8 format");
		if (!pr::AllSet(tdesc.MiscFlags, D3D11_RESOURCE_MISC_GDI_COMPATIBLE))
			throw pr::Exception<HRESULT>(E_FAIL, "GDI textures require the GDI compatible flag");
		if (tdesc.MipLevels != 1)
			throw pr::Exception<HRESULT>(E_FAIL, "GDI textures require the MipLevels == 1");

		// Allocate a new texture instance and DX texture resource
		SortKeyId sort_id = m_lookup_tex.size() % SortKey::MaxTextureId;
		Texture2DPtr inst(Allocator<Texture2D>(m_mem_funcs).New(this, id, src, tdesc, sdesc, sort_id, has_alpha, name), true);
		assert(m_mem_tracker.add(inst.get()));

		// Add the texture instance to the lookup table
		AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
		return inst;
	}
	Texture2DPtr TextureManager::CreateTextureGdi(RdrId id, Image const& src, bool has_alpha, char const* name)
	{
		TextureDesc tdesc(src.m_dim.x, src.m_dim.y, 1, DXGI_FORMAT_B8G8R8A8_UNORM);
		tdesc.BindFlags = D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_RENDER_TARGET;
		tdesc.MiscFlags = D3D11_RESOURCE_MISC_GDI_COMPATIBLE;
		return CreateTextureGdi(id, src, tdesc, SamplerDesc::LinearClamp(), has_alpha, name);
	}
	Texture2DPtr TextureManager::CreateTextureGdi(RdrId id, int w, int h, bool has_alpha, char const* name)
	{
		return CreateTextureGdi(id, Image(w,h), has_alpha, name);
	}

	// Create a new texture instance that uses the same DX texture as an existing texture.
	// 'id' is the id to assign to this new texture instance. Use 'AutoId' to auto generate an id
	// 'existing' is an existing texture instance to clone
	// 'sam_desc' is an optional sampler state description to set on the clone.
	Texture2DPtr TextureManager::CloneTexture2D(RdrId id, Texture2D const* existing, SamplerDesc const* sam_desc, char const* name)
	{
		Renderer::Lock lock(m_rdr);

		// Check whether 'id' already exists, if so, throw.
		if (id != AutoId && m_lookup_tex.find(id) == std::end(m_lookup_tex))
			throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

		// Allocate a new texture instance that reuses the DX texture resource
		SortKeyId sort_id = m_lookup_tex.size() % SortKey::MaxTextureId;
		Texture2DPtr inst(Allocator<Texture2D>(m_mem_funcs).New(this, id, *existing, sort_id, name), true);
		assert(m_mem_tracker.add(inst.get()));

		// If a sampler state is given, recreate the sampler state
		if (sam_desc)
			inst->SamDesc(*sam_desc);

		// Add the texture instance to the lookup table
		AddLookup(m_lookup_tex, inst->m_id, inst.m_ptr);
		return inst;
	}

	// Create a texture that references a shared resource
	Texture2DPtr TextureManager::OpenSharedTexture2D(RdrId id, HANDLE shared_handle, SamplerDesc const& sdesc, bool has_alpha, char const* name)
	{
		Renderer::Lock lock(m_rdr);

		// Check whether 'id' already exists, if so, throw.
		if (id != AutoId && m_lookup_tex.find(id) != std::end(m_lookup_tex))
			throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Texture Id '%d' is already in use", id));

		// Allocate a new texture instance and wrap the shared resource
		SortKeyId sort_id = m_lookup_tex.size() % SortKey::MaxTextureId;
		Texture2DPtr inst(Allocator<Texture2D>(m_mem_funcs).New(this, id, shared_handle, sdesc, sort_id, has_alpha, name), true);
		assert(m_mem_tracker.add(inst.get()));

		// Add the texture instance to the lookup table
		AddLookup(m_lookup_tex, inst->m_id, inst.get());
		return inst;
	}

	// Delete a texture instance.
	void TextureManager::Delete(TextureBase* tex)
	{
		if (tex == nullptr)
			return;

		Renderer::Lock lock(m_rdr);

		// Find 'tex' in the map of RdrIds to texture instances
		// We'll remove this, but first use it as a non-const reference
		auto iter = m_lookup_tex.find(tex->m_id);
		assert("Texture not found" && iter != end(m_lookup_tex));

		// If the DX texture will be released when we clean up this texture
		// then check whether it is in the 'fname' lookup table and remove it if it is.
		if (tex->m_src_id != 0 && tex->m_res.RefCount() == 1)
		{
			auto jter = m_lookup_fname.find(tex->m_src_id);
			if (jter != m_lookup_fname.end()) m_lookup_fname.erase(jter);
		}

		// Delete the texture and remove the entry from the RdrId lookup map
		assert(m_mem_tracker.remove(iter->second));
		Allocator<TextureBase>(m_mem_funcs).Delete(iter->second);
		m_lookup_tex.erase(iter);
	}

	// Return a stock texture
	Texture2DPtr TextureManager::FindStockTexture(EStockTexture stock)
	{
		// See if the texture already exists
		auto stock_tex = FindTexture<Texture2D>(RdrId(stock));
		if (stock_tex != nullptr)
			return std::move(stock_tex);

		// If not, generate it
		Texture2DPtr tex;
		switch (stock)
		{
		default:
			{
				throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Unknown stock texture: %s", ToStringA(stock)));
			}
		case EStockTexture::Black:
			{
				pr::uint const data[] = {0};
				Image src(1, 1, data, DXGI_FORMAT_R8G8B8A8_UNORM);
				TextureDesc tdesc(src, 1, EUsage::Immutable);
				tex = CreateTexture2D(RdrId(EStockTexture::Black), src, tdesc, SamplerDesc::LinearClamp(), false, "#black");
				break;
			}
		case EStockTexture::White:
			{
				pr::uint const data[] = {0xFFFFFFFF};
				Image src(1, 1, data, DXGI_FORMAT_R8G8B8A8_UNORM);
				TextureDesc tdesc(src, 1, EUsage::Immutable);
				tex = CreateTexture2D(RdrId(EStockTexture::White), src, tdesc, SamplerDesc::LinearClamp(), false, "#white");
				break;
			}
		case EStockTexture::Gray:
			{
				pr::uint const data[] = {0xFF808080};
				Image src(1, 1, data, DXGI_FORMAT_R8G8B8A8_UNORM);
				TextureDesc tdesc(src, 1, EUsage::Immutable);
				tex = CreateTexture2D(RdrId(EStockTexture::Gray), src, tdesc, SamplerDesc::LinearClamp(), false, "#gray");
				break;
			}
		case EStockTexture::Checker:
			{
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
				TextureDesc tdesc(src, 0, EUsage::Immutable);
				auto sam = SamplerDesc::LinearWrap();
				sam.Filter = D3D11_FILTER_MIN_MAG_MIP_POINT;
				tex = CreateTexture2D(RdrId(EStockTexture::Checker), src, tdesc, sam, false, "#checker");
				break;
			}
		case EStockTexture::Checker2:
			{
				pr::uint const data[] =
				{
					#define X 0xFFFFFFFF
					#define O 0xFFAAAAAA
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
				TextureDesc tdesc(src, 0, EUsage::Immutable);
				auto sam = SamplerDesc::LinearWrap();
				sam.Filter = D3D11_FILTER_MIN_MAG_MIP_POINT;
				tex = CreateTexture2D(RdrId(EStockTexture::Checker2), src, tdesc, sam, false, "#checker2");
				break;
			}
		case EStockTexture::Checker3:
			{
				pr::uint const data[] =
				{
					#define O 0xFFFFFFFF
					#define X 0xFFEEEEEE
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
				TextureDesc tdesc(src, 0, EUsage::Immutable);
				auto sam = SamplerDesc::LinearWrap();
				sam.Filter = D3D11_FILTER_MIN_MAG_MIP_POINT;
				tex = CreateTexture2D(RdrId(EStockTexture::Checker3), src, tdesc, sam, false, "#checker3");
				break;
			}
		case EStockTexture::WhiteSpot:
			{
				const int sz = 256;
				std::vector<uint> data;
				data.resize(sz*sz);
				auto radius = sz / 2.0f;
				for (int j = 0; j != sz; ++j)
				{
					for (int i = 0; i != sz; ++i)
					{
						auto t = Frac(0.0f, Len2(i - radius, j - radius), radius);
						data[j*sz + i] = Lerp(Colour32Zero, Colour32White, 1.0f - SmoothStep(0.0f, 1.0f, t));
					}
				}

				Image src(sz, sz, data.data(), DXGI_FORMAT_R8G8B8A8_UNORM);
				TextureDesc tdesc(src, 0, EUsage::Immutable);
				auto sam = SamplerDesc::LinearClamp();
				tex = CreateTexture2D(RdrId(EStockTexture::WhiteSpot), src, tdesc, sam, true, "#whitespot");
				break;
			}
		}

		// Add the stock texture to the collection
		assert(tex != nullptr);
		m_stock_textures.push_back(tex);
		return std::move(tex);
	}

	// Create the basic textures that exist from startup
	void TextureManager::CreateStockTextures()
	{
		FindStockTexture(EStockTexture::Black);
		FindStockTexture(EStockTexture::White);
		FindStockTexture(EStockTexture::Checker);
	}

	// Updates the texture and 'srv' pointers in 'existing' to those provided.
	// If 'all_instances' is true, 'm_lookup_tex' is searched for Texture instances that point to the same
	// DX resource as 'existing'. All are updated to point to the given 'tex' and 'srv' and the RdrId remains unchanged.
	// If 'all_instances' is false, only 'existing' has its dx pointers changed.
	void TextureManager::ReplaceTexture(Texture2D& existing, D3DPtr<ID3D11Texture2D> tex, D3DPtr<ID3D11ShaderResourceView> srv, bool all_instances)
	{
		if (all_instances && existing.m_res != nullptr)
		{
			// Replace the DX texture in all other texture instances that share it with 'existing'
			for (auto& rhs : m_lookup_tex)
			{
				auto& other = *rhs.second;
				if (other.m_res.m_ptr != existing.m_res.m_ptr) continue;
				other.m_res = tex;
				other.m_srv = srv;
			}
		}
		else
		{
			existing.m_res = tex;
			existing.m_srv = srv;
		}
	}
}
