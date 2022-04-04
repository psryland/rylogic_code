//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/resource/resource_manager.h"
#include "pr/view3d-12/resource/image.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/render/sortkey.h"
#include "pr/view3d-12/texture/texture_desc.h"
#include "pr/view3d-12/texture/texture_2d.h"
#include "pr/view3d-12/model/vertex_layout.h"
#include "pr/view3d-12/model/model_desc.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/view3d-12/utility/map_scope.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	ResourceManager::ResourceManager(Renderer& rdr)
		:m_mem_tracker()
		,m_rdr(rdr)
		,m_cmd_alloc()
		,m_cmd_list()
		,m_gpu_sync()
		,m_lookup_dxres()
		,m_lookup_tex()
		,m_gdiplus()
		,m_eh_resize()
		,m_gdi_dc_ref_count()
		,m_stock_models()
		,m_stock_textures()
	{
		Renderer::Lock lock(m_rdr);
		auto device = lock.D3DDevice();

		// Create a command allocator and list for use by the resource manager, since it is independent of any windows or scenes.
		Throw(device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, __uuidof(ID3D12CommandAllocator), (void**)&m_cmd_alloc.m_ptr));
		Throw(device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, m_cmd_alloc.get(), nullptr, __uuidof(ID3D12GraphicsCommandList), (void**)&m_cmd_list.m_ptr));
		Throw(m_cmd_list->Close());
		m_cmd_list->SetName(L"ResourceManager");
		m_gpu_sync.Init(device);

		// Create stock resources
		CreateStockTextures();
		CreateStockModels();

		// Close and execute the resource managers setup commands
		m_gpu_sync.AddSyncPoint(lock.CmdQueue());
		m_gpu_sync.Wait();

		#if 0 // todo
		// Create default samplers to use in shaders that expect a
		// sampler but the model has no texture/sampler bound.
		{
			Renderer::Lock lock(rdr);
			auto sdesc = SamDesc::LinearClamp();
			Throw(lock.D3DDevice()->CreateSamplerState(&sdesc, &m_def_sampler.m_ptr));
			sdesc.Filter = D3D11_FILTER_COMPARISON_MIN_MAG_LINEAR_MIP_POINT;
			Throw(lock.D3DDevice()->CreateSamplerState(&sdesc, &m_def_sampler_comp.m_ptr));
		}

		// Detect outstanding references to GDI device contexts
		m_eh_resize = rdr().BackBufferSizeChanged += [this](Window&, BackBufferSizeChangedEventArgs const&)
		{
			assert("Outstanding DC references during resize" && m_gdi_dc_ref_count == 0);
		};
		#endif
	}
	ResourceManager::~ResourceManager()
	{
		m_gpu_sync.Release();
		
	}

	// Renderer access
	Renderer& ResourceManager::rdr()
	{
		return m_rdr;
	}

	// Create a model.
	ModelPtr ResourceManager::CreateModel(ModelDesc const& mdesc)
	{
		if (mdesc.m_vb.ElemCount == 0)
			throw std::runtime_error("Attempt to create 0-length model vertex buffer");
		if (mdesc.m_ib.ElemCount == 0)
			throw std::runtime_error("Attempt to create 0-length model index buffer");
		if (mdesc.m_ib.Format != DXGI_FORMAT_R16_UINT && mdesc.m_ib.Format != DXGI_FORMAT_R32_UINT)
			throw std::runtime_error(Fmt("Index buffer format %d is not supported", mdesc.m_ib.Format));

		Renderer::Lock lock(m_rdr);
		auto device = lock.D3DDevice();

		// Create a vertex buffer
		D3DPtr<ID3D12Resource> vb;
		{
			Throw(device->CreateCommittedResource(
				&HeapProps::Default(),
				D3D12_HEAP_FLAG_NONE,
				&mdesc.m_vb,
				D3D12_RESOURCE_STATE_COPY_DEST,
				nullptr,
				__uuidof(ID3D12Resource),
				(void**)&vb.m_ptr));
			Throw(vb->SetName(FmtS(L"%s:VB:%d", mdesc.m_name.c_str(), mdesc.m_vb.ElemCount)));
		
			// Initialise the vertex data
			if (mdesc.m_vb.Data != nullptr)
			{
				// Get the size required for an upload heap
				UINT64 total_bytes;
				device->GetCopyableFootprints(&mdesc.m_vb, 0U, 1U, 0ULL, nullptr, nullptr, nullptr, &total_bytes);

				// Store the vertex data in the staging buffer (upload heap)
				auto staging = GetStagingBuffer(device, total_bytes);
				UpdateSubresource(m_cmd_list.get(), vb.get(), staging.buf.get(), mdesc.m_vb, 0);
			}

			// Transition the vertex buffer to a pixel shader resource
			auto barrier = ResourceBarrier::Transition(vb.get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);
			m_cmd_list->ResourceBarrier(1, &barrier);
		}

		// Create an index buffer
		D3DPtr<ID3D12Resource> ib;
		{
			Throw(device->CreateCommittedResource(
				&HeapProps::Default(),
				D3D12_HEAP_FLAG_NONE,
				&mdesc.m_ib,
				D3D12_RESOURCE_STATE_COPY_DEST,
				nullptr,
				__uuidof(ID3D12Resource),
				(void**)&ib.m_ptr));
			Throw(ib->SetName(FmtS(L"%s:IB:%d", mdesc.m_name.c_str(), mdesc.m_ib.ElemCount)));

			// Initialise the index data
			if (mdesc.m_ib.Data != nullptr)
			{
				// Get the size required for an upload heap
				UINT64 total_bytes;
				device->GetCopyableFootprints(&mdesc.m_vb, 0U, 1U, 0ULL, nullptr, nullptr, nullptr, &total_bytes);

				// Store the vertex data in the staging buffer (upload heap)
				auto staging = GetStagingBuffer(device, total_bytes);
				UpdateSubresource(m_cmd_list.get(), ib.get(), staging.buf.get(), mdesc.m_ib, 0);
			}

			// Transition the vertex buffer to a pixel shader resource
			auto barrier = ResourceBarrier::Transition(ib.get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);
			m_cmd_list->ResourceBarrier(1, &barrier);
		}

		// Execute the command list
		Throw(m_cmd_list->Close());
		lock.ExecuteCommands(m_cmd_list.get());

		// Create the model
		ModelPtr ptr(rdr12::New<Model>(*this, mdesc.m_vb.ElemCount, mdesc.m_ib.ElemCount, vb.get(), ib.get()), true);
		assert(m_mem_tracker.add(ptr.m_ptr));
		return ptr;
	}

	// Create a new texture instance.
	Texture2DPtr ResourceManager::CreateTexture2D(TextureDesc const& tdesc)
	{
		// Check whether 'id' already exists, if so, throw.
		if (tdesc.m_id != AutoId && m_lookup_tex.find(tdesc.m_id) != end(m_lookup_tex))
			throw std::runtime_error(FmtS("Texture Id '%d' is already in use", tdesc.m_id));

		// Texture 'id' doesn't exist, so create it
		Renderer::Lock lock(rdr());
		auto device = lock.D3DDevice();

		// Prepare the command list
		Throw(m_cmd_list->Reset(m_cmd_alloc.get(), nullptr));

		// Create the texture
		D3DPtr<ID3D12Resource> tex;
		{
			// Create a GPU visible resource that will hold the created texture.
			Throw(device->CreateCommittedResource(
				&HeapProps::Default(),          // a default heap
				D3D12_HEAP_FLAG_NONE,           // no flags
				&tdesc.m_tdesc,                 // the description of the texture
				D3D12_RESOURCE_STATE_COPY_DEST, // the texture will be copied from the upload heap to here, so it starts out as copy dest state
				&tdesc.m_clear_value,           // used for render targets and depth/stencil buffers
				__uuidof(ID3D12Resource),
				(void**)&tex.m_ptr));
			Throw(tex->SetName(tdesc.m_name.c_str()));

			// If initialisation data is provided
			if (tdesc.m_tdesc.Data != nullptr)
			{
				// Get the size required for an upload heap
				UINT64 total_bytes;
				device->GetCopyableFootprints(&tdesc.m_tdesc, 0U, 1U, 0ULL, nullptr, nullptr, nullptr, &total_bytes);

				// Store the image data in the staging texture (upload heap)
				auto staging = GetStagingBuffer(device, total_bytes);
				UpdateSubresource(m_cmd_list.get(), tex.get(), staging.buf.get(), tdesc.m_tdesc, 0);
			}

			// Transition the texture default heap to a pixel shader resource
			auto barrier = ResourceBarrier::Transition(tex.get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);
			m_cmd_list->ResourceBarrier(1, &barrier);
		}

		// Execute the command list
		Throw(m_cmd_list->Close());
		lock.ExecuteCommands(m_cmd_list.get());

		// todo
		(void)tdesc.m_sdesc;

		// Allocate a new texture instance and DX texture resource
		//SortKeyId sort_id = m_lookup_tex.size() % SortKey::MaxTextureId;
		Texture2DPtr inst(rdr12::New<Texture2D>(*this, tdesc.m_id, tex.get(), 0, tdesc.m_has_alpha), true);
		assert(m_mem_tracker.add(inst.get()));

		// Add a pointer (not a reference) to the texture instance to the lookup table.
		// The caller owns the texture, when released it will be removed from this lookup.
		AddLookup(m_lookup_tex, inst->m_id, inst.get());
		return inst;
	}

	// Create a new nugget
	Nugget* ResourceManager::CreateNugget(NuggetData const& ndata, Model* model)
	{
		Renderer::Lock lock(rdr());
		auto ptr = rdr12::New<Nugget>(ndata, model);
		assert(m_mem_tracker.add(ptr));
		return ptr;
	}

	// Return a stock texture
	Texture2DPtr ResourceManager::FindTexture(EStockTexture stock) const
	{
		return m_stock_textures[s_cast<int>(stock)];
	}

	// Create the basic textures that exist from startup
	void ResourceManager::CreateStockTextures()
	{
		{// EStockTexture::Black
			uint32_t const data[] = {0};
			Image src(1, 1, data, DXGI_FORMAT_B8G8R8A8_UNORM);
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::Black), TexDesc::Tex2D(src, 1), SamDesc::LinearClamp(), false, L"#black");
			m_stock_textures[s_cast<int>(EStockTexture::Black)] = CreateTexture2D(tdesc);
		}
		{// EStockTexture::White:
			uint32_t const data[] = {0xFFFFFFFF};
			Image src(1, 1, data, DXGI_FORMAT_B8G8R8A8_UNORM);
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::White), TexDesc::Tex2D(src, 1), SamDesc::LinearClamp(), false, L"#white");
			m_stock_textures[s_cast<int>(EStockTexture::White)] = CreateTexture2D(tdesc);
		}
		{// EStockTexture::Gray:
			uint32_t const data[] = {0xFF808080};
			Image src(1, 1, data, DXGI_FORMAT_B8G8R8A8_UNORM);
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::Gray), TexDesc::Tex2D(src, 1), SamDesc::LinearClamp(), false, L"#gray");
			m_stock_textures[s_cast<int>(EStockTexture::Gray)] = CreateTexture2D(tdesc);
		}
		{// EStockTexture::Checker:
			uint32_t const data[] =
			{
				#define X 0xFFFFFFFF
				#define O 0x00000000
				X, X, O, O, X, X, O, O,
				X, X, O, O, X, X, O, O,
				O, O, X, X, O, O, X, X,
				O, O, X, X, O, O, X, X,
				X, X, O, O, X, X, O, O,
				X, X, O, O, X, X, O, O,
				O, O, X, X, O, O, X, X,
				O, O, X, X, O, O, X, X,
				#undef X
				#undef O
			};
			Image src(8, 8, data, DXGI_FORMAT_B8G8R8A8_UNORM);
			auto sam = SamDesc::LinearWrap(); sam.Filter = D3D12_FILTER_MIN_MAG_MIP_POINT;
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::Checker), TexDesc::Tex2D(src, 0), sam, false, L"#checker");
			m_stock_textures[s_cast<int>(EStockTexture::Checker)] = CreateTexture2D(tdesc);
		}
		{// EStockTexture::Checker2:
			uint32_t const data[] =
			{
				#define X 0xFFFFFFFF
				#define O 0xFFAAAAAA
				X, X, O, O, X, X, O, O,
				X, X, O, O, X, X, O, O,
				O, O, X, X, O, O, X, X,
				O, O, X, X, O, O, X, X,
				X, X, O, O, X, X, O, O,
				X, X, O, O, X, X, O, O,
				O, O, X, X, O, O, X, X,
				O, O, X, X, O, O, X, X,
				#undef X
				#undef O
			};
			Image src(8, 8, data, DXGI_FORMAT_B8G8R8A8_UNORM);
			auto sam = SamDesc::LinearWrap(); sam.Filter = D3D12_FILTER_MIN_MAG_MIP_POINT;
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::Checker2), TexDesc::Tex2D(src, 0), sam, false, L"#checker2");
			m_stock_textures[s_cast<int>(EStockTexture::Checker2)] = CreateTexture2D(tdesc);
		}
		{// EStockTexture::Checker3:
			uint32_t const data[] =
			{
				#define O 0xFFFFFFFF
				#define X 0xFFEEEEEE
				X, X, O, O, X, X, O, O,
				X, X, O, O, X, X, O, O,
				O, O, X, X, O, O, X, X,
				O, O, X, X, O, O, X, X,
				X, X, O, O, X, X, O, O,
				X, X, O, O, X, X, O, O,
				O, O, X, X, O, O, X, X,
				O, O, X, X, O, O, X, X,
				#undef X
				#undef O
			};
			Image src(8, 8, data, DXGI_FORMAT_B8G8R8A8_UNORM);
			auto sam = SamDesc::LinearWrap(); sam.Filter = D3D12_FILTER_MIN_MAG_MIP_POINT;
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::Checker3), TexDesc::Tex2D(src, 0), sam, false, L"#checker3");
			m_stock_textures[s_cast<int>(EStockTexture::Checker3)] = CreateTexture2D(tdesc);
		}
		{// EStockTexture::WhiteSpot:
			constexpr int sz = 256;
			constexpr auto radius = sz / 2.0f;
			std::vector<uint32_t> data(sz * sz);
			for (int j = 0; j != sz; ++j)
			{
				for (int i = 0; i != sz; ++i)
				{
					auto c = Colour32White;
					auto t = Frac(0.0f, Len(i - radius, j - radius), radius);
					c.a = uint8_t(Lerp(0xFF, 0x00, SmoothStep(0.0f, 1.0f, t)));
					data[size_t(j * sz + i)] = c;
				}
			}

			Image src(sz, sz, data.data(), DXGI_FORMAT_B8G8R8A8_UNORM);
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::WhiteSpot), TexDesc::Tex2D(src, 0), SamDesc::LinearClamp(), true, L"#whitespot");
			m_stock_textures[s_cast<int>(EStockTexture::WhiteSpot)] = CreateTexture2D(tdesc);
		}
		{// EStockTexture::WhiteTriangle:
			// Equilateral triangle, 'pointing' up.
			// (-sqrt(3)/2,0.75)------(sqrt(3)/2,0.75)
			//               \         /
			//                \       /
			//                 \     / 
			//                   0,0
			constexpr int sz = 64, hsz = sz / 2;
			constexpr auto dx = maths::root3_by_2f / 2.0f;
			constexpr auto dy = 0.75f;
			constexpr auto s = 1.0f / sz;
			std::vector<uint> data(sz * sz);
			for (int j = 0; j * 4 <= sz * 3; ++j)
			{
				auto y = j * s; // [0, 0.75]

				// Do the positive half x range and mirror to -x
				for (int i = 0; i != hsz; ++i)
				{
					auto x0 = s * (i + 0);
					auto x1 = s * (i + 1);

					// x*dy == y*dx on the edge
					auto t =
						(x1 * dy < y* dx) ? 0.0f :  // inside the triangle
						(x0 * dy > y * dx) ? 1.0f :  // outside the triangle
						(Frac(x0 * dy, y * dx, x1 * 0.75f)); // Spanning the edge

					auto c = Colour32White;
					c.a = uint8_t(Lerp(0xFF, 0x00, SmoothStep(0.0f, 1.0f, t)));

					data[size_t(j * sz + hsz - i)] = c;
					data[size_t(j * sz + hsz + i)] = c;
				}
			}

			Image src(sz, sz, data.data(), DXGI_FORMAT_B8G8R8A8_UNORM);
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::WhiteTriangle), TexDesc::Tex2D(src, 0), SamDesc::LinearClamp(), true, L"#whitetriangle");
			m_stock_textures[s_cast<int>(EStockTexture::WhiteTriangle)] = CreateTexture2D(tdesc);
		}
		{// EStockTexture::EnvMapProjection:
			uint32_t const data[] = {0};
			Image src(1, 1, data, DXGI_FORMAT_B8G8R8A8_UNORM);
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::EnvMapProjection), TexDesc::Tex2D(src, 0), SamDesc::LinearClamp(), false, L"#envmapproj");
			m_stock_textures[s_cast<int>(EStockTexture::EnvMapProjection)] = CreateTexture2D(tdesc);
		}
	}

	// Create stock models
	void ResourceManager::CreateStockModels()
	{
		{// Basis/focus point model
			// Don't know why, but the optimiser buggers this up if I use initializer_list<>. Hence local arrays
			constexpr Vert verts[6] =
			{
				{v4( 0.0f,  0.0f,  0.0f, 1.0f), 0xFFFF0000, v4Zero, v2Zero},
				{v4( 1.0f,  0.0f,  0.0f, 1.0f), 0xFFFF0000, v4Zero, v2Zero},
				{v4( 0.0f,  0.0f,  0.0f, 1.0f), 0xFF00FF00, v4Zero, v2Zero},
				{v4( 0.0f,  1.0f,  0.0f, 1.0f), 0xFF00FF00, v4Zero, v2Zero},
				{v4( 0.0f,  0.0f,  0.0f, 1.0f), 0xFF0000FF, v4Zero, v2Zero},
				{v4( 0.0f,  0.0f,  1.0f, 1.0f), 0xFF0000FF, v4Zero, v2Zero},
			};
			uint16_t const idxs[] =
			{
				0, 1, 2, 3, 4, 5,
			};
			BBox const bbox(v4(0.5f, 0.5f, 0.5f, 1.0f), v4(1,1,1,0));

			ModelDesc mdesc(verts, idxs, bbox, L"basis");
			auto ptr = CreateModel(mdesc);

			NuggetData nug(ETopo::LineList, EGeom::Vert|EGeom::Colr);
			nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::ShadowCastExclude, true);
			ptr->CreateNugget(nug);

			m_stock_models[s_cast<int>(EStockModel::Basis)] = ptr;
		}
		#if 0 // todo
		{// Unit quad in Z = 0 plane
			Vert const verts[4] =
			{
				{v4(-0.5f,-0.5f, 0, 1), ColourWhite, v4ZAxis, v2(0.0000f,0.9999f)},
				{v4( 0.5f,-0.5f, 0, 1), ColourWhite, v4ZAxis, v2(0.9999f,0.9999f)},
				{v4( 0.5f, 0.5f, 0, 1), ColourWhite, v4ZAxis, v2(0.9999f,0.0000f)},
				{v4(-0.5f, 0.5f, 0, 1), ColourWhite, v4ZAxis, v2(0.0000f,0.0000f)},
			};
			uint16_t const idxs[] =
			{
				0, 1, 2, 0, 2, 3
			};
			BBox const bbox(v4Origin, v4(1,1,0,0));

			MdlSettings s(verts, idxs, bbox, "unit quad");
			m_unit_quad = CreateModel(s);

			NuggetProps n(ETopo::TriList, Vert::GeomMask);
			m_unit_quad->CreateNugget(n);
		}
		{// Bounding box cube
			Vert const verts[] =
			{
				{v4(-0.5f, -0.5f, -0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(+0.5f, -0.5f, -0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(+0.5f, +0.5f, -0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(-0.5f, +0.5f, -0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(-0.5f, -0.5f, +0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(+0.5f, -0.5f, +0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(+0.5f, +0.5f, +0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(-0.5f, +0.5f, +0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
			};
			uint16_t const idxs[] =
			{
				0, 1, 1, 2, 2, 3, 3, 0,
				4, 5, 5, 6, 6, 7, 7, 4,
				0, 4, 1, 5, 2, 6, 3, 7,
			};
			BBox const bbox(v4Origin, v4(1,1,1,0));

			MdlSettings s(verts, idxs, bbox, "bbox cube");
			m_bbox_model = CreateModel(s);

			NuggetProps n(ETopo::LineList, EGeom::Vert|EGeom::Colr);
			n.m_nflags = SetBits(n.m_nflags, ENuggetFlag::ShadowCastExclude, true);
			m_bbox_model->CreateNugget(n);
		}
		{// Selection box
			// Create the selection box model
			constexpr float sz = 1.0f;
			constexpr float dd = 0.8f;
			Vert const verts[] =
			{
				{v4(-sz, -sz, -sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(-dd, -sz, -sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(-sz, -dd, -sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(-sz, -sz, -dd, 1.0f), Colour32White, v4Zero, v2Zero},

				{v4(sz, -sz, -sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(sz, -dd, -sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(dd, -sz, -sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(sz, -sz, -dd, 1.0f), Colour32White, v4Zero, v2Zero},

				{v4(sz, sz, -sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(dd, sz, -sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(sz, dd, -sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(sz, sz, -dd, 1.0f), Colour32White, v4Zero, v2Zero},

				{v4(-sz, sz, -sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(-sz, dd, -sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(-dd, sz, -sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(-sz, sz, -dd, 1.0f), Colour32White, v4Zero, v2Zero},

				{v4(-sz, -sz, sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(-dd, -sz, sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(-sz, -dd, sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(-sz, -sz, dd, 1.0f), Colour32White, v4Zero, v2Zero},

				{v4(sz, -sz, sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(sz, -dd, sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(dd, -sz, sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(sz, -sz, dd, 1.0f), Colour32White, v4Zero, v2Zero},

				{v4(sz, sz, sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(dd, sz, sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(sz, dd, sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(sz, sz, dd, 1.0f), Colour32White, v4Zero, v2Zero},

				{v4(-sz, sz, sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(-sz, dd, sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(-dd, sz, sz, 1.0f), Colour32White, v4Zero, v2Zero},
				{v4(-sz, sz, dd, 1.0f), Colour32White, v4Zero, v2Zero},
			};
			uint16_t const idxs[] =
			{
				0,  1,  0,  2,  0,  3,
				4,  5,  4,  6,  4,  7,
				8,  9,  8, 10,  8, 11,
				12, 13, 12, 14, 12, 15,
				16, 17, 16, 18, 16, 19,
				20, 21, 20, 22, 20, 23,
				24, 25, 24, 26, 24, 27,
				28, 29, 28, 30, 28, 31,
			};
			BBox const bbox(v4Origin, v4(1,1,1,0));

			MdlSettings s(verts, idxs, bbox, "selection box");
			m_selection_box = CreateModel(s);

			NuggetProps n(ETopo::LineList, EGeom::Vert);
			n.m_nflags = SetBits(n.m_nflags, ENuggetFlag::ShadowCastExclude, true);
			m_selection_box->CreateNugget(n);
		}
		#endif
	}

	// Return a staging buffer that is at least 'size' big
	ResourceManager::DxStageBuffer& ResourceManager::GetStagingBuffer(ID3D12Device* device, uint64_t size)
	{
		auto iter = pr::lower_bound(m_staging_buffers, size, [](auto& lhs, auto& rhs) { return lhs.size < rhs; });
		if (iter == std::end(m_staging_buffers) || iter->size < size)
		{
			ResourceManager::DxStageBuffer staging = {};
			auto bdesc = BufferDesc::Buffer(size, nullptr);
			Throw(device->CreateCommittedResource(
				&HeapProps::Upload(),              // upload heap
				D3D12_HEAP_FLAG_NONE,              // no flags
				&bdesc,                            // resource description for a buffer (storing the image data in this heap just to copy to the default heap)
				D3D12_RESOURCE_STATE_GENERIC_READ, // we will copy the contents from this heap to the default heap above
				nullptr,                           // must be null for buffers
				__uuidof(ID3D12Resource),
				(void**)&staging.buf.m_ptr));

			staging.buf->SetName(FmtS(L"Staging-%llu", size));
			staging.size = size;

			iter = m_staging_buffers.insert(iter, staging);
		}
		return *iter;
	}
	
	// Return a model to the allocator
	void ResourceManager::Delete(Model* model)
	{
		if (model == nullptr)
			return;

		// Notify model deleted
		ModelDeleted(*model);

		Renderer::Lock lock(rdr());
		assert(m_mem_tracker.remove(model));
		rdr12::Delete<Model>(model);
	}
	
	// Return a render nugget to the allocator
	void ResourceManager::Delete(Nugget* nugget)
	{
		if (nugget == nullptr)
			return;

		Renderer::Lock lock(rdr());
		assert(m_mem_tracker.remove(nugget));
		rdr12::Delete<Nugget>(nugget);
	}

	// Return a texture to the allocator.
	void ResourceManager::Delete(TextureBase* tex)
	{
		if (tex == nullptr)
			return;

		Renderer::Lock lock(rdr());

		// Find 'tex' in the map of RdrIds to texture instances
		// We'll remove this, but first use it as a non-const reference
		auto iter = m_lookup_tex.find(tex->m_id);
		assert("Texture not found" && iter != end(m_lookup_tex));

		// If the DX texture will be released when we clean up this texture
		// then check whether it is in the 'fname' lookup table and remove it if it is.
		if (tex->m_uri != 0 && tex->m_res.RefCount() == 1)
		{
			auto jter = m_lookup_dxres.find(tex->m_uri);
			if (jter != end(m_lookup_dxres))
				m_lookup_dxres.erase(jter);
		}

		// Delete the texture and remove the entry from the RdrId lookup map
		assert(m_mem_tracker.remove(iter->second));
		rdr12::Delete<TextureBase>(iter->second);
		m_lookup_tex.erase(iter);
	}

}
