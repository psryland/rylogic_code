//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/resource/resource_manager.h"
#include "pr/view3d-12/resource/image.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/texture/texture_desc.h"
#include "pr/view3d-12/texture/texture_2d.h"
#include "pr/view3d-12/texture/texture_cube.h"
#include "pr/view3d-12/texture/texture_loader.h"
#include "pr/view3d-12/sampler/sampler_desc.h"
#include "pr/view3d-12/sampler/sampler.h"
#include "pr/view3d-12/model/vertex_layout.h"
#include "pr/view3d-12/model/model_desc.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/view3d-12/utility/map_resource.h"
#include "pr/view3d-12/utility/utility.h"
#include "view3d-12/src/utility/barrier_batch.h"

namespace pr::rdr12
{
	constexpr int HeapCapacityView = 12;

	ResourceManager::ResourceManager(Renderer& rdr)
		:m_mem_tracker()
		,m_rdr(rdr)
		,m_gsync(rdr.D3DDevice())
		,m_keep_alive(m_gsync)
		,m_gfx_cmd_alloc_pool(m_gsync)
		,m_gfx_cmd_list(rdr.D3DDevice(), m_gfx_cmd_alloc_pool.Get(), nullptr, L"ResManCmdListGfx")
		,m_heap_view(HeapCapacityView, &m_gsync)
		,m_heap_sampler(HeapCapacityView, &m_gsync)
		,m_lookup_res()
		,m_lookup_tex()
		,m_lookup_sam()
		,m_upload_buffer(m_gsync, 1ULL * 1024 * 1024)
		,m_descriptor_store(rdr.D3DDevice())
		,m_mipmap_gen(rdr, m_gsync, m_gfx_cmd_list)
		,m_gdiplus()
		,m_eh_resize()
		,m_gdi_dc_ref_count()
		,m_flush_required()
	{
		// Setup notification of sync points
		m_rdr.AddPollCB({ &GpuSync::Poll, &m_gsync });

		// Wait till stock resources are created
		FlushToGpu(true);

		#if 0 // todo
		// Detect outstanding references to GDI device contexts
		m_eh_resize = rdr().BackBufferSizeChanged += [this](Window&, BackBufferSizeChangedEventArgs const&)
		{
			assert("Outstanding DC references during resize" && m_gdi_dc_ref_count == 0);
		};
		#endif
	}
	ResourceManager::~ResourceManager()
	{
		// Stop polling 'm_gsync'
		m_rdr.RemovePollCB({ &GpuSync::Poll, &m_gsync });
	}

	// Renderer access
	ID3D12Device4* ResourceManager::d3d() const
	{
		return rdr().d3d();
	}
	Renderer& ResourceManager::rdr() const
	{
		return m_rdr;
	}

	// Flush creation commands to the GPU. Returns the sync point for when they've been executed
	uint64_t ResourceManager::FlushToGpu(bool block)
	{
		if (!m_flush_required)
			return m_gsync.LastAddedSyncPoint();

		// Close the command list
		m_gfx_cmd_list.Close();

		// Execute the command list
		auto cmd_lists = {static_cast<ID3D12CommandList*>(m_gfx_cmd_list.get())};
		rdr().GfxQueue()->ExecuteCommandLists(s_cast<UINT>(cmd_lists.size()), cmd_lists.begin());
		m_flush_required = false;

		// Add a sync point
		auto sync_point = m_gsync.AddSyncPoint(rdr().GfxQueue());
		m_gfx_cmd_list.SyncPoint(sync_point);

		// Reset the command list
		m_gfx_cmd_list.Reset(m_gfx_cmd_alloc_pool.Get());

		// Wait till done?
		if (block)
			Wait(sync_point);

		return sync_point;
	}
	void ResourceManager::Wait(uint64_t sync_point) const
	{
		m_gsync.Wait(sync_point);
	}

	// Create and initialise a resource
	D3DPtr<ID3D12Resource> ResourceManager::CreateResource(ResDesc const& desc)
	{
		D3DPtr<ID3D12Resource> res;
		auto device = rdr().D3DDevice();
		auto has_init_data = !desc.Data.empty();

		// Buffer resources specify the Width as the size in bytes, even though for textures width is the pixel count.
		D3D12_RESOURCE_DESC rd = desc;
		if (desc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
			rd.Width *= desc.ElemStride;

		// Create a GPU visible resource that will hold the created texture/verts/indices/etc.
		// Create in the COMMON state to prevent a D3D12 warning "Buffers are effectively created in state D3D12_RESOURCE_STATE_COMMON"
		// COMMON state is implicitly promoted to the first state transition.
		Throw(device->CreateCommittedResource(
			&desc.HeapProps, desc.HeapFlags, &rd, D3D12_RESOURCE_STATE_COMMON,
			desc.ClearValue ? &*desc.ClearValue : nullptr,
			__uuidof(ID3D12Resource), (void**)&res.m_ptr));
		
		// We need to record that the initial state is 'desc.FinalState' then track, on a per-cmd-list
		// basis, what the state transitions are for this resource, so that at the end of a cmd list we know
		// what state the resource will be in, and can then transition it to the correct state for the next
		// cmd list.
		m_gfx_cmd_list.ResState(res.get()).Apply(D3D12_RESOURCE_STATE_COMMON);

		// If initialisation data is provided, initialise using an UploadBuffer
		if (has_init_data)
		{
			BarrierBatch barriers(m_gfx_cmd_list);
			barriers.Transition(res.get(), D3D12_RESOURCE_STATE_COPY_DEST);
			barriers.Commit();

			// Copy the initialisation data into the resource
			UpdateSubresource(res.get(), desc.Data, 0, desc.DataAlignment);
			
			// Generate mip maps for the texture (if needed)
			// 'm_mipmap_gen' should use the same cmd-list as the resource manager, so that mips are generated as
			// textures are created. Remember cmd-lists are executed serially.
			if (desc.MipLevels != 1)
				m_mipmap_gen.Generate(res.get());

			// Transition the resource to the final state
			barriers.Transition(res.get(), desc.FinalState);
			barriers.Commit();
			m_flush_required = true;
		}

		return res;
	}

	// Create a model.
	ModelPtr ResourceManager::CreateModel(ModelDesc const& mdesc)
	{
		if (mdesc.m_vb.Width == 0)
			throw std::runtime_error("Attempt to create 0-length model vertex buffer");
		if (mdesc.m_ib.Width == 0)
			throw std::runtime_error("Attempt to create 0-length model index buffer");

		// Create a V/I buffers
		D3DPtr<ID3D12Resource> vb = CreateResource(mdesc.m_vb);
		D3DPtr<ID3D12Resource> ib = CreateResource(mdesc.m_ib);

		// Create the model
		ModelPtr ptr(rdr12::New<Model>(*this, s_cast<size_t>(mdesc.m_vb.Width), s_cast<size_t>(mdesc.m_ib.Width), mdesc.m_vb.ElemStride, mdesc.m_ib.ElemStride, vb.get(), ib.get(), mdesc.m_bbox, mdesc.m_name.c_str()), true);
		assert(m_mem_tracker.add(ptr.m_ptr));
		return ptr;
	}
	ModelPtr ResourceManager::CreateModel(EStockModel id)
	{
		switch (id)
		{
			case EStockModel::Basis:
			{
				// Basis/focus point model
				constexpr Vert verts[] = {
					{v4(0.0f,  0.0f,  0.0f, 1.0f), Colour(0xFFFF0000), v4::Zero(), v2::Zero()},
					{v4(1.0f,  0.0f,  0.0f, 1.0f), Colour(0xFFFF0000), v4::Zero(), v2::Zero()},
					{v4(0.0f,  0.0f,  0.0f, 1.0f), Colour(0xFF00FF00), v4::Zero(), v2::Zero()},
					{v4(0.0f,  1.0f,  0.0f, 1.0f), Colour(0xFF00FF00), v4::Zero(), v2::Zero()},
					{v4(0.0f,  0.0f,  0.0f, 1.0f), Colour(0xFF0000FF), v4::Zero(), v2::Zero()},
					{v4(0.0f,  0.0f,  1.0f, 1.0f), Colour(0xFF0000FF), v4::Zero(), v2::Zero()},
				};
				constexpr uint16_t idxs[] = {
					0, 1, 2, 3, 4, 5,
				};
				constexpr BBox bbox = {
					v4(0.5f, 0.5f, 0.5f, 1.0f),
					v4(1, 1, 1, 0)
				};

				ModelDesc mdesc(verts, idxs, bbox, "basis");
				auto ptr = CreateModel(mdesc);

				NuggetData nug(ETopo::LineList, EGeom::Vert | EGeom::Colr);
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::ShadowCastExclude, true);
				ptr->CreateNugget(nug);

				return ptr;
			}
			case EStockModel::UnitQuad:
			{
				// Unit quad in Z = 0 plane
				constexpr Vert verts[] = {
					{v4(-0.5f,-0.5f, 0, 1), Colour(0xFFFFFFFF), v4::ZAxis(), v2(0.0000f,0.9999f)},
					{v4(0.5f,-0.5f, 0, 1), Colour(0xFFFFFFFF), v4::ZAxis(), v2(0.9999f,0.9999f)},
					{v4(0.5f, 0.5f, 0, 1), Colour(0xFFFFFFFF), v4::ZAxis(), v2(0.9999f,0.0000f)},
					{v4(-0.5f, 0.5f, 0, 1), Colour(0xFFFFFFFF), v4::ZAxis(), v2(0.0000f,0.0000f)},
				};
				constexpr uint16_t idxs[] = {
					0, 1, 2, 0, 2, 3
				};
				constexpr BBox bbox = {
					v4Origin,
					v4(1, 1, 0, 0)
				};

				ModelDesc mdesc(verts, idxs, bbox, "unit quad");
				auto ptr = CreateModel(mdesc);

				NuggetData nug(ETopo::TriList, EGeom::Vert | EGeom::Colr | EGeom::Norm | EGeom::Tex0);
				ptr->CreateNugget(nug);
				return ptr;
			}
			case EStockModel::BBoxModel:
			{
				// Bounding box cube
				constexpr Vert verts[] = {
					{v4(-0.5f, -0.5f, -0.5f, 1.0f), Colour(0xFF0000FF), v4::Zero(), v2::Zero()},
					{v4(+0.5f, -0.5f, -0.5f, 1.0f), Colour(0xFF0000FF), v4::Zero(), v2::Zero()},
					{v4(+0.5f, +0.5f, -0.5f, 1.0f), Colour(0xFF0000FF), v4::Zero(), v2::Zero()},
					{v4(-0.5f, +0.5f, -0.5f, 1.0f), Colour(0xFF0000FF), v4::Zero(), v2::Zero()},
					{v4(-0.5f, -0.5f, +0.5f, 1.0f), Colour(0xFF0000FF), v4::Zero(), v2::Zero()},
					{v4(+0.5f, -0.5f, +0.5f, 1.0f), Colour(0xFF0000FF), v4::Zero(), v2::Zero()},
					{v4(+0.5f, +0.5f, +0.5f, 1.0f), Colour(0xFF0000FF), v4::Zero(), v2::Zero()},
					{v4(-0.5f, +0.5f, +0.5f, 1.0f), Colour(0xFF0000FF), v4::Zero(), v2::Zero()},
				};
				constexpr uint16_t idxs[] = {
					0, 1, 1, 2, 2, 3, 3, 0,
					4, 5, 5, 6, 6, 7, 7, 4,
					0, 4, 1, 5, 2, 6, 3, 7,
				};
				constexpr BBox bbox = {
					v4Origin,
					v4(1, 1, 1, 0)
				};

				ModelDesc mdesc(verts, idxs, bbox, "bbox cube");
				auto ptr = CreateModel(mdesc);

				NuggetData nug(ETopo::LineList, EGeom::Vert | EGeom::Colr);
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::ShadowCastExclude, true);
				ptr->CreateNugget(nug);

				return ptr;
			}
			case EStockModel::SelectionBox:
			{
				// Selection box
				// Create the selection box model
				constexpr float sz = 1.0f;
				constexpr float dd = 0.8f;
				constexpr Vert verts[] = {
					{v4(-sz, -sz, -sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(-dd, -sz, -sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(-sz, -dd, -sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(-sz, -sz, -dd, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},

					{v4(sz, -sz, -sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(sz, -dd, -sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(dd, -sz, -sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(sz, -sz, -dd, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},

					{v4(sz, sz, -sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(dd, sz, -sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(sz, dd, -sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(sz, sz, -dd, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},

					{v4(-sz, sz, -sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(-sz, dd, -sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(-dd, sz, -sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(-sz, sz, -dd, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},

					{v4(-sz, -sz, sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(-dd, -sz, sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(-sz, -dd, sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(-sz, -sz, dd, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},

					{v4(sz, -sz, sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(sz, -dd, sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(dd, -sz, sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(sz, -sz, dd, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},

					{v4(sz, sz, sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(dd, sz, sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(sz, dd, sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(sz, sz, dd, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},

					{v4(-sz, sz, sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(-sz, dd, sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(-dd, sz, sz, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
					{v4(-sz, sz, dd, 1.0f), Colour(0xFFFFFFFF), v4::Zero(), v2::Zero()},
				};
				constexpr uint16_t idxs[] = {
					0,  1,  0,  2,  0,  3,
					4,  5,  4,  6,  4,  7,
					8,  9,  8, 10,  8, 11,
					12, 13, 12, 14, 12, 15,
					16, 17, 16, 18, 16, 19,
					20, 21, 20, 22, 20, 23,
					24, 25, 24, 26, 24, 27,
					28, 29, 28, 30, 28, 31,
				};
				constexpr BBox bbox = {
					v4Origin,
					v4(1, 1, 1, 0)
				};

				ModelDesc mdesc(verts, idxs, bbox, "selection box");
				auto ptr = CreateModel(mdesc);

				NuggetData nug(ETopo::LineList, EGeom::Vert);
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::ShadowCastExclude, true);
				ptr->CreateNugget(nug);

				return ptr;
			}
			default:
			{
				throw std::runtime_error("Unknown stock model type");
			}
		}
	}

	// Create a new texture instance.
	Texture2DPtr ResourceManager::CreateTexture2D(TextureDesc const& desc)
	{
		// Check whether 'id' already exists, if so, throw. Users should use FindTexture first.
		if (desc.m_id != AutoId && m_lookup_tex.find(desc.m_id) != end(m_lookup_tex))
			throw std::runtime_error(FmtS("Texture Id '%d' is already in use", desc.m_id));
		if (desc.m_tdesc.DepthOrArraySize != 1)
			throw std::runtime_error("Expected a 2D texture");

		D3DPtr<ID3D12Resource> res;

		// If a uri is given, see if the Dx resource already exists
		if (desc.m_uri != 0)
		{
			auto iter = m_lookup_res.find(desc.m_uri);
			if (iter == end(m_lookup_res))
			{
				// If not, create the resource and add it to the lookup
				res = CreateResource(desc.m_tdesc);

				// Record the uri for reuse
				AddLookup(m_lookup_res, desc.m_uri, res.get());
				iter = m_lookup_res.find(desc.m_uri);
			}
			res = D3DPtr<ID3D12Resource>(iter->second, true);
		}

		// Otherwise, just create the texture
		else
		{
			res = CreateResource(desc.m_tdesc);
		}

		// Allocate a new texture instance
		Texture2DPtr inst(rdr12::New<Texture2D>(*this, res.get(), desc), true);
		assert(m_mem_tracker.add(inst.get()));
		m_keep_alive.Add(inst, m_gsync.NextSyncPoint());

		// Add the texture instance pointer (not ref counted) to the lookup table.
		// The caller owns the texture, when released it will be removed from this lookup.
		AddLookup(m_lookup_tex, inst->m_id, inst.get());
		return std::move(inst);
	}
	Texture2DPtr ResourceManager::CreateTexture2D(std::filesystem::path const& resource_path, TextureDesc const& desc_)
	{
		// Check whether 'id' already exists, if so, throw.
		if (desc_.m_id != AutoId && m_lookup_tex.find(desc_.m_id) != end(m_lookup_tex))
			throw std::runtime_error(pr::FmtS("Texture Id '%d' already exists, use FindTexture", desc_.m_id));
		if (resource_path.empty())
			throw std::runtime_error("A resource path must be given");

		// Create the texture resource
		D3DPtr<ID3D12Resource> res;
		TextureDesc desc = desc_;

		// Accept stock texture strings: #black, #white, #checker, etc
		// This is handy for model files that contain string paths for textures.
		// The code that loads these models doesn't need to handle strings such as '#white' as a special case
		if (*resource_path.c_str() == '#')
		{
			auto stock = Enum<EStockTexture>::TryParse(resource_path.c_str() + 1, false);
			if (!stock)
				throw std::runtime_error(Fmt("Unknown stock texture name: %s", resource_path.string().c_str() + 1));

			// Create a stock texture
			return CreateTexture(*stock);
		}

		// Create a texture from embedded resource
		else if (*resource_path.c_str() == '@')
		{
			auto uri = resource_path.wstring();

			desc.m_uri = MakeId(uri.c_str());
			if (desc.m_name.empty())
				desc.m_name = To<string32>(str::FindLastOf(uri.c_str(), L":"));

			// Look for an existing Dx12 resource corresponding to the uri
			auto iter = m_lookup_res.find(desc.m_uri);
			if (iter == end(m_lookup_res))
			{
				// Parse the embedded resource string: "@<module>:<res_type>:<res_name>"
				HMODULE hmodule; wstring32 res_type, res_name;
				ParseEmbeddedResourceUri(uri, hmodule, res_type, res_name);

				// Get the embedded resource
				auto emb = resource::Read<uint8_t>(res_name.c_str(), res_type.c_str(), hmodule);
				auto data = std::span{ emb.m_data, emb.m_len };

				// Create the texture data
				auto [images, tdesc] = LoadImageData(data, 1, false, 0, &rdr().Features());
				desc.m_tdesc = tdesc;
				desc.m_tdesc.Data = images;

				// Create the texture
				res = CreateResource(desc.m_tdesc);

				// Record the uri for reuse
				AddLookup(m_lookup_res, desc.m_uri, res.get());
				iter = m_lookup_res.find(desc.m_uri);
			}
			res = D3DPtr<ID3D12Resource>(iter->second, true);
		}

		// Otherwise, create from a file on disk
		else
		{
			using namespace std::filesystem;
			auto filepath = resource_path.lexically_normal();

			// Generate an id from the filepath
			desc.m_uri = MakeId(filepath.c_str());
			if (desc.m_name.empty())
				desc.m_name = To<string32>(filepath.filename().c_str());

			// Look for an existing DX texture corresponding to the filepath
			auto iter = m_lookup_res.find(desc.m_uri);
			if (iter == end(m_lookup_res))
			{
				// If the texture filepath doesn't exist, use the resolve event
				if (!exists(filepath))
				{
					auto args = ResolvePathArgs{filepath, false};
					ResolveFilepath(*this, args);
					if (!args.handled || !exists(args.filepath))
						throw std::runtime_error(FmtS("Texture filepath '%s' does not exist", filepath.string().c_str()));

					filepath = std::move(args.filepath);
				}

				// Load the texture from disk
				auto [images, tdesc] = LoadImageData(filepath, 1, true, 0, &rdr().Features());
				desc.m_tdesc = tdesc;
				desc.m_tdesc.Data = images;

				// Create the texture
				res = CreateResource(desc.m_tdesc);

				// Record the uri for reuse
				AddLookup(m_lookup_res, desc.m_uri, res.get());
				iter = m_lookup_res.find(desc.m_uri);
			}
			res = D3DPtr<ID3D12Resource>(iter->second, true);
		}
	
		// Allocate a new texture instance
		Texture2DPtr inst(rdr12::New<Texture2D>(*this, res.get(), desc), true);
		assert(m_mem_tracker.add(inst.get()));
		m_keep_alive.Add(inst, m_gsync.NextSyncPoint());

		// Add a pointer (not ref counted) to the texture instance to the lookup table.
		// The caller owns the texture, when released it will be removed from this lookup.
		AddLookup(m_lookup_tex, inst->m_id, inst.get());
		return std::move(inst);
	}
	TextureCubePtr ResourceManager::CreateTextureCube(std::filesystem::path const& resource_path, TextureDesc const& desc_)
	{
		// Notes:
		//  - A cube map is an array of 6 2D textures.
		//  - DDS image files contain all six faces in the single file. Other image types need to be loaded separately.
		//  - 'resource_path' should contain '??' where the first '?' is the sign (+,-) and the second '?' is the axis (x,y,z)

		// Check whether 'id' already exists, if so, throw.
		if (desc_.m_id != AutoId && m_lookup_tex.find(desc_.m_id) != end(m_lookup_tex))
			throw std::runtime_error(pr::FmtS("Texture Id '%d' is already in use", desc_.m_id));
		if (resource_path.empty())
			throw std::runtime_error("Resource path must be given");
		
		// Create the texture resource
		D3DPtr<ID3D12Resource> res;
		TextureDesc desc = desc_;

		// Create a texture from embedded resources
		if (resource_path.c_str()[0] == '@')
		{
			desc.m_uri = MakeId(resource_path.c_str());
			if (desc.m_name.empty())
				desc.m_name = To<string32>(str::FindLastOf(resource_path.c_str(), ":"));

			// Look for an existing Dx12 resource corresponding to the uri
			auto iter = m_lookup_res.find(desc.m_uri);
			if (iter == end(m_lookup_res))
			{
				// Parse the embedded resource string: "@<module>:<res_type>:<res_name>"
				HMODULE hmodule; wstring32 res_type, res_name;
				ParseEmbeddedResourceUri(resource_path.wstring(), hmodule, res_type, res_name);

				// The faces of the cube
				pr::vector<std::span<uint8_t const>> source_images;

				// Read each face of the cube map
				auto idx = res_name.find(L"??");
				if (idx != std::wstring::npos)
				{
					// Get the data for each face of the cube map
					for (auto face : { L"px", L"nx", L"py", L"ny", L"pz", L"nz" })
					{
						res_name[idx + 0] = face[0];
						res_name[idx + 1] = face[1];
						auto emb = resource::Read<uint8_t>(res_name.c_str(), res_type.c_str(), hmodule);
						source_images.push_back(std::span{ emb.m_data, emb.m_len });
					}
				}
				else // Otherwise, the resource is a single file
				{
					auto emb = resource::Read<uint8_t>(res_name.c_str(), res_type.c_str(), hmodule);
					source_images.push_back(std::span{ emb.m_data, emb.m_len });
				}

				// Create the texture data
				auto [images, tdesc] = LoadImageData(source_images, 1, true, 0, &rdr().Features());
				desc.m_tdesc = tdesc;
				desc.m_tdesc.Data = images;

				// Create the texture
				res = CreateResource(desc.m_tdesc);

				// Record the uri for reuse
				AddLookup(m_lookup_res, desc.m_uri, res.get());
				iter = m_lookup_res.find(desc.m_uri);
			}
			res = D3DPtr<ID3D12Resource>(iter->second, true);
		}

		// Otherwise, create from a file (or files) on disk
		else
		{
			using namespace std::filesystem;
			auto filepath = resource_path.lexically_normal();

			// Generate an id from the filepath
			desc.m_uri = MakeId(filepath.c_str());
			if (desc.m_name.empty())
				desc.m_name = To<string32>(filepath.filename().c_str());

			// Look for an existing DX texture corresponding to the filepath
			auto iter = m_lookup_res.find(desc.m_uri);
			if (iter == end(m_lookup_res))
			{
				auto res_name = filepath.string();

				// The faces of the cube
				pr::vector<path> source_images;

				// If this is a filename pattern rather than a single file, load each face
				auto idx = res_name.find("??");
				if (idx != std::string::npos)
				{
					// Get the data for each face of the cube map
					for (auto face : { "px", "nx", "py", "ny", "pz", "nz" })
					{
						res_name[idx + 0] = face[0];
						res_name[idx + 1] = face[1];
						source_images.push_back(ResolvePath(res_name));
					}
				}
				else // Otherwise, the filename is a single file
				{
					source_images.push_back(ResolvePath(res_name));
				}

				// Load the texture from disk (supports '??' in the filepath)
				auto [images, tdesc] = LoadImageData(source_images, 1, true, 0, &rdr().Features());
				desc.m_tdesc = tdesc;
				desc.m_tdesc.Data = images;

				// Create the texture
				res = CreateResource(desc.m_tdesc);

				// Record the uri for reuse
				AddLookup(m_lookup_res, desc.m_uri, res.get());
				iter = m_lookup_res.find(desc.m_uri);
			}
			res = D3DPtr<ID3D12Resource>(iter->second, true);
		}

		// Allocate a new texture instance
		TextureCubePtr inst(rdr12::New<TextureCube>(*this, res.get(), desc), true);
		assert(m_mem_tracker.add(inst.get()));
		m_keep_alive.Add(inst, m_gsync.NextSyncPoint());

		// Add a pointer (not ref counted) to the texture instance to the lookup table.
		// The caller owns the texture, when released it will be removed from this lookup.
		AddLookup(m_lookup_tex, inst->m_id, inst.get());
		return std::move(inst);
	}
	Texture2DPtr ResourceManager::CreateTexture(EStockTexture id)
	{
		switch (id)
		{
			case EStockTexture::Black:
			{
				uint32_t const data[] = { 0xFF000000 };
				auto src = Image(1, 1, data, DXGI_FORMAT_B8G8R8A8_UNORM);
				auto tdesc = TextureDesc(AutoId, ResDesc::Tex2D(src, 1)).uri(EStockTexture::Black).name("#black");
				return CreateTexture2D(tdesc);
			}
			case EStockTexture::White:
			{
				uint32_t const data[] = { 0xFFFFFFFF };
				auto src = Image(1, 1, data, DXGI_FORMAT_B8G8R8A8_UNORM);
				auto tdesc = TextureDesc(AutoId, ResDesc::Tex2D(src, 1)).uri(EStockTexture::White).name("#white");
				return CreateTexture2D(tdesc);
			}
			case EStockTexture::Gray:
			{
				uint32_t const data[] = { 0xFF808080 };
				auto src = Image(1, 1, data, DXGI_FORMAT_B8G8R8A8_UNORM);
				auto tdesc = TextureDesc(AutoId, ResDesc::Tex2D(src, 1)).uri(EStockTexture::Gray).name("#gray");
				return CreateTexture2D(tdesc);
			}
			case EStockTexture::Checker:
			{
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
				auto src = Image(8, 8, data, DXGI_FORMAT_B8G8R8A8_UNORM);
				auto tdesc = TextureDesc(AutoId, ResDesc::Tex2D(src, 0)).uri(EStockTexture::Checker).name("#checker");
				return CreateTexture2D(tdesc);
			}
			case EStockTexture::Checker2:
			{
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
				auto src = Image(8, 8, data, DXGI_FORMAT_B8G8R8A8_UNORM);
				auto tdesc = TextureDesc(AutoId, ResDesc::Tex2D(src, 0)).uri(EStockTexture::Checker2).name("#checker2");
				return CreateTexture2D(tdesc);
			}
			case EStockTexture::Checker3:
			{
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
				auto src = Image(8, 8, data, DXGI_FORMAT_B8G8R8A8_UNORM);
				auto tdesc = TextureDesc(AutoId, ResDesc::Tex2D(src, 0)).uri(EStockTexture::Checker3).name("#checker3");
				return CreateTexture2D(tdesc);
			}
			case EStockTexture::WhiteSpot:
			{
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
						data[size_t(j * sz + i)] = c.argb;
					}
				}

				auto src = Image(sz, sz, data.data(), DXGI_FORMAT_B8G8R8A8_UNORM);
				auto tdesc = TextureDesc(AutoId, ResDesc::Tex2D(src, 0)).uri(EStockTexture::WhiteSpot).has_alpha().name("#whitespot");
				return CreateTexture2D(tdesc);
			}
			case EStockTexture::WhiteTriangle:
			{
				constexpr int sz = 64, hsz = sz / 2;
				constexpr auto dx = maths::root3_by_2f / 2.0f;
				constexpr auto dy = 0.75f;
				constexpr auto s = 1.0f / sz;

				// Equilateral triangle, 'pointing' up.
				// (-sqrt(3)/2,0.75)------(sqrt(3)/2,0.75)
				//               \         /
				//                \       /
				//                 \     / 
				//                   0,0
				std::vector<uint32_t> data(sz * sz);
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
							(x1 * dy < y * dx) ? 0.0f :  // inside the triangle
							(x0 * dy > y * dx) ? 1.0f :  // outside the triangle
							(Frac(x0 * dy, y * dx, x1 * 0.75f)); // Spanning the edge

						auto c = Colour32White;
						c.a = uint8_t(Lerp(0xFF, 0x00, SmoothStep(0.0f, 1.0f, t)));

						data[size_t(j * sz + hsz - i)] = c.argb;
						data[size_t(j * sz + hsz + i)] = c.argb;
					}
				}

				auto src = Image(sz, sz, data.data(), DXGI_FORMAT_B8G8R8A8_UNORM);
				auto tdesc = TextureDesc(AutoId, ResDesc::Tex2D(src, 0)).uri(EStockTexture::WhiteTriangle).has_alpha().name("#whitetriangle");
				return CreateTexture2D(tdesc);
			}
			case EStockTexture::EnvMapProjection:
			{
				uint32_t const data[] = { 0 };
				auto src = Image(1, 1, data, DXGI_FORMAT_B8G8R8A8_UNORM);
				auto tdesc = TextureDesc(AutoId, ResDesc::Tex2D(src, 0)).uri(EStockTexture::EnvMapProjection).name("#envmapproj");
				return CreateTexture2D(tdesc);
			}
			default:
			{
				throw std::runtime_error("Unknown stock texture");
			}
		}
	}

	// Create a new sampler instance.
	SamplerPtr ResourceManager::CreateSampler(SamplerDesc const& desc)
	{
		// Check whether 'id' already exists, if so, return it.
		// There is no per-instance data in samplers, so they can be shared.
		// So really 'CreateSampler' isn't quite right, it's more like 'GetOrCreateSampler'.
		if (desc.m_id != AutoId)
		{
			auto iter = m_lookup_sam.find(desc.m_id);
			if (iter != end(m_lookup_sam))
				return SamplerPtr(iter->second, true);
		}

		// Allocate a new sampler instance
		SamplerPtr inst(rdr12::New<Sampler>(*this, desc), true);
		assert(m_mem_tracker.add(inst.get()));
		m_keep_alive.Add(inst, m_gsync.NextSyncPoint());

		// Add the sampler instance pointer (not ref counted) to the lookup table.
		// The caller owns the sampler, when released it will be removed from this lookup.
		AddLookup(m_lookup_sam, inst->m_id, inst.get());
		return std::move(inst);
	}
	SamplerPtr ResourceManager::CreateSampler(EStockSampler id)
	{
		switch (id)
		{
			case EStockSampler::PointClamp:
			{
				auto sdesc = SamplerDesc(EStockSampler::PointClamp, SamDesc::PointClamp()).name("#pointclamp");
				return CreateSampler(sdesc);
			}
			case EStockSampler::PointWrap:
			{
				auto sdesc = SamplerDesc(EStockSampler::PointWrap, SamDesc::PointWrap()).name("#pointwrap");
				return CreateSampler(sdesc);
			}
			case EStockSampler::LinearClamp:
			{
				auto sdesc = SamplerDesc(EStockSampler::LinearClamp, SamDesc::LinearClamp()).name("#linearclamp");
				return CreateSampler(sdesc);
			}
			case EStockSampler::LinearWrap:
			{
				auto sdesc = SamplerDesc(EStockSampler::LinearWrap, SamDesc::LinearWrap()).name("#linearwrap");
				return CreateSampler(sdesc);
			}
			case EStockSampler::AnisotropicClamp:
			{
				auto sdesc = SamplerDesc(EStockSampler::AnisotropicClamp, SamDesc::AnisotropicClamp()).name("#anisotropicclamp");
				return CreateSampler(sdesc);
			}
			case EStockSampler::AnisotropicWrap:
			{
				auto sdesc = SamplerDesc(EStockSampler::AnisotropicWrap, SamDesc::AnisotropicWrap()).name("#anisotropicwrap");
				return CreateSampler(sdesc);
			}
			default:
			{
				throw std::runtime_error("Unknown stock sampler type");
			}
		}
	}
	SamplerPtr ResourceManager::FindSampler(RdrId id) const
	{
		auto sam = GetOrDefault(m_lookup_sam, id, (Sampler*)nullptr);
		return SamplerPtr(sam, true);
	}

	// Create a new nugget
	Nugget* ResourceManager::CreateNugget(NuggetData const& ndata, Model* model, RdrId id)
	{
		auto ptr = rdr12::New<Nugget>(ndata, model, id);
		assert(m_mem_tracker.add(ptr));
		return ptr;
	}

	// Update the data in 'dest' (sub resource range: [sub0,sub0+images.size())) using a staging buffer
	void ResourceManager::UpdateSubresource(ID3D12Resource* dest, std::span<Image const> images, int sub0, int alignment)
	{
		// Notes:
		//  - 'images' here is an array of any resource initialisation data (i.e. could be verts, indices, texture, etc)
		//  - 'sub0' is the first sub resource in 'dest' to update
		//  - Constant buffers must be aligned to D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT
		//  - Texture buffers must be aligned to D3D12_TEXTURE_DATA_PLACEMENT_ALIGNMENT
		//  - D3D12_TEXTURE_DATA_PITCH_ALIGNMENT(256) is the minimum row pitch for a texture
		//  - 'dest' must be in the 'copy dest' state
		if (images.empty())
			return;

		auto device = rdr().D3DDevice();
		int subN = s_cast<int>(images.size());

		// Check buffer types. Normal buffers don't have multiple subresources.
		auto ddesc = dest->GetDesc();
		if (ddesc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER && (sub0 != 0 || subN != 1))
			throw std::runtime_error("Destination resource is a buffer, but sub-resource range is given");

		// Get the size and footprints for copying 'subN' subresources.
		uint64_t total_size;
		auto row_count = PR_ALLOCA(row_count, UINT, subN);
		auto row_size  = PR_ALLOCA(row_size, UINT64, subN);
		auto layout    = PR_ALLOCA(layout, D3D12_PLACED_SUBRESOURCE_FOOTPRINT, subN);
		device->GetCopyableFootprints(&ddesc, sub0, subN, 0ULL, &layout[0], &row_count[0], &row_size[0], &total_size);

		// Get a staging buffer big enough for all of the subresources
		auto staging = m_upload_buffer.Alloc(total_size, alignment);

		// Copy data from 'images' into the staging buffer
		for (auto i = 0; i != subN; ++i)
		{
			auto const& image = images[i];               // The initialisation data
			auto const& footprint = layout[i].Footprint; // The dimension and row stride for 'dest'

			if (s_cast<int>(footprint.Depth) != image.m_dim.z)
				throw std::runtime_error("Image size mismatch (slice count)");
			if (s_cast<int>(row_count[i]) != image.m_dim.y)
				throw std::runtime_error("Image size mismatch (row count)");
			if (s_cast<int>(row_size[i]) != image.m_pitch.x)
				throw std::runtime_error("Image size mismatch (row size)");

			// 'GetCopyableFootprints' returns values relative to 0 for a staging resource, but 'staging' is a
			// sub-allocation within a staging resource, so we need to adjust the Offset values.
			layout[i].Offset += staging.m_ofs;

			// Copy each slice
			for (auto z = 0; z != image.m_dim.z; ++z)
			{
				auto src_slice = image.Slice(z);
				auto dst_slice = staging.m_mem + layout[i].Offset + footprint.RowPitch * row_count[i] * z;

				// Copy each row of the image
				for (auto y = 0; y != image.m_dim.y; ++y)
					memcpy(dst_slice + footprint.RowPitch * y, src_slice.bptr + image.m_pitch.x * y, image.m_pitch.x);
			}
		}

		// Add the command to copy from the staging resource to the destination resource
		if (ddesc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
		{
			m_gfx_cmd_list.CopyBufferRegion(dest, 0U, staging.m_buf, staging.m_ofs, layout[0].Footprint.Width);
			m_flush_required = true;
		}
		else
		{
			for (auto i = 0; i != subN; ++i)
			{
				D3D12_TEXTURE_COPY_LOCATION dst =
				{
					.pResource = dest,
					.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
					.SubresourceIndex = s_cast<UINT>(sub0 + i),
				};
				D3D12_TEXTURE_COPY_LOCATION src =
				{
					.pResource = staging.m_buf,
					.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
					.PlacedFootprint = layout[i],
				};
				m_gfx_cmd_list.CopyTextureRegion(&dst, 0, 0, 0, &src, nullptr);
				m_flush_required = true;
			}
		}
	}
	void ResourceManager::UpdateSubresource(ID3D12Resource* dest, Image const& image, int sub0, int alignment)
	{
		UpdateSubresource(dest, {&image, 1}, sub0, alignment);
	}
	
	// Use the 'ResolveFilepath' event to resolve a filepath
	std::filesystem::path ResourceManager::ResolvePath(std::string_view path) const
	{
		auto args = ResolvePathArgs{ .filepath = path, .handled = false };
		if (!std::filesystem::exists(args.filepath))
		{
			// If the texture filepath doesn't exist, use the resolve event
			ResolveFilepath(*this, args);
			if (!args.handled || !std::filesystem::exists(args.filepath))
				throw std::runtime_error(FmtS("Texture filepath '%s' does not exist", args.filepath.c_str()));
		}
		return std::move(args.filepath);
	}

	// Return a model to the allocator
	void ResourceManager::Delete(Model* model)
	{
		if (model == nullptr)
			return;

		// Notify model deleted
		ModelDeleted(*model);

		assert(m_mem_tracker.remove(model));
		rdr12::Delete<Model>(model);
	}
	
	// Return a render nugget to the allocator
	void ResourceManager::Delete(Nugget* nugget)
	{
		if (nugget == nullptr)
			return;

		assert(m_mem_tracker.remove(nugget));
		rdr12::Delete<Nugget>(nugget);
	}

	// Return a texture to the allocator.
	void ResourceManager::Delete(TextureBase* tex)
	{
		if (tex == nullptr)
			return;

		// Find 'tex' in the map of RdrIds to texture instances
		// We'll remove this, but first use it as a non-const reference
		auto iter = m_lookup_tex.find(tex->m_id);
		assert("Texture not found" && iter != end(m_lookup_tex));

		// If the DX texture will be released when we clean up this texture
		// then check whether it is in the 'fname' lookup table and remove it if it is.
		if (tex->m_uri != 0 && tex->m_res.RefCount() == 1)
		{
			auto jter = m_lookup_res.find(tex->m_uri);
			if (jter != end(m_lookup_res))
				m_lookup_res.erase(jter);
		}

		// Delete the texture and remove the entry from the RdrId lookup map
		assert(m_mem_tracker.remove(iter->second));
		rdr12::Delete<TextureBase>(iter->second);
		m_lookup_tex.erase(iter);
	}

	// Return a sampler to the allocator.
	void ResourceManager::Delete(Sampler* sam)
	{
		if (sam == nullptr)
			return;

		// Find 'sam' in the map of RdrIds to sampler instances
		// We'll remove this, but first use it as a non-const reference
		auto iter = m_lookup_sam.find(sam->m_id);
		assert("Sampler not found" && iter != end(m_lookup_sam));

		// Delete the texture and remove the entry from the RdrId lookup map
		assert(m_mem_tracker.remove(iter->second));
		rdr12::Delete<Sampler>(iter->second);
		m_lookup_sam.erase(iter);
	}
}
