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
#include "pr/view3d-12/model/vertex_layout.h"
#include "pr/view3d-12/model/model_desc.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/view3d-12/utility/map_resource.h"
#include "pr/view3d-12/utility/utility.h"
#include "view3d-12/src/utility/root_signature.h"

namespace pr::rdr12
{
	constexpr int HeapCapacityView = 12;
	enum class EMipMapParam { Constants, SrcTexture, DstTexture };
	enum class EMipMapSamp { Samp0 };

	ResourceManager::ResourceManager(Renderer& rdr)
		:m_mem_tracker()
		,m_rdr(rdr)
		,m_gsync(rdr.D3DDevice())
		,m_gfx_cmd_alloc_pool(m_gsync)
		,m_gfx_cmd_alloc(m_gfx_cmd_alloc_pool.Get())
		,m_gfx_cmd_list()
		//,m_com_cmd_alloc_pool(m_gsync)
		//,m_com_cmd_alloc(m_com_cmd_alloc_pool.Get())
		//,m_com_cmd_list()
		,m_heap_view(HeapCapacityView, &m_gsync)
		,m_lookup_res()
		,m_lookup_tex()
		,m_upload_buffer(m_gsync, 1ULL * 1024 * 1024)
		,m_descriptor_store(rdr.D3DDevice())
		,m_mipmap_sig()
		,m_mipmap_pso()
		,m_gdiplus()
		,m_eh_resize()
		,m_gdi_dc_ref_count()
		,m_stock_models()
		,m_stock_textures()
		,m_flush_required()
	{
		auto device = m_rdr.D3DDevice();

		// Create an open command list for use by the resource manager, since it is independent of any windows or scenes.
		Throw(device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, m_gfx_cmd_alloc, nullptr, __uuidof(ID3D12GraphicsCommandList), (void**)&m_gfx_cmd_list.m_ptr));
		//Throw(device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_COMPUTE, m_com_cmd_alloc, nullptr, __uuidof(ID3D12CommandList), (void**)&m_com_cmd_list.m_ptr));
		m_gfx_cmd_list->SetName(L"ResManCmdListGfx");
		//m_com_cmd_list->SetName(L"ResManCmdListCom");

		// Initialise the mip map generator
		{
			// Create a root signature for the MipMap generator compute shader
			RootSig<EMipMapParam, EMipMapSamp> sig;
			sig.Flags = {
				D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT |
				D3D12_ROOT_SIGNATURE_FLAG_DENY_VERTEX_SHADER_ROOT_ACCESS |
				D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
				D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
				D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS |
				D3D12_ROOT_SIGNATURE_FLAG_DENY_PIXEL_SHADER_ROOT_ACCESS |
				D3D12_ROOT_SIGNATURE_FLAG_DENY_AMPLIFICATION_SHADER_ROOT_ACCESS |
				D3D12_ROOT_SIGNATURE_FLAG_DENY_MESH_SHADER_ROOT_ACCESS |
				D3D12_ROOT_SIGNATURE_FLAG_NONE };
			sig.U32(EMipMapParam::Constants, ECBufReg::b0, 2);
			sig.Tex(EMipMapParam::SrcTexture, ETexReg::t0);
			sig.Uav(EMipMapParam::DstTexture, EUAVReg::u0);
			sig.Samp(EMipMapSamp::Samp0, D3D12_STATIC_SAMPLER_DESC{
				.Filter = D3D12_FILTER_MIN_MAG_LINEAR_MIP_POINT,
				.AddressU = D3D12_TEXTURE_ADDRESS_MODE_CLAMP,
				.AddressV = D3D12_TEXTURE_ADDRESS_MODE_CLAMP,
				.AddressW = D3D12_TEXTURE_ADDRESS_MODE_CLAMP,
				.MipLODBias = 0.0f,
				.MaxAnisotropy = 0,
				.ComparisonFunc = D3D12_COMPARISON_FUNC_NEVER,
				.BorderColor = D3D12_STATIC_BORDER_COLOR_OPAQUE_BLACK,
				.MinLOD = 0.0f,
				.MaxLOD = D3D12_FLOAT32_MAX,
				.ShaderRegister = 0,
				.RegisterSpace = 0,
				.ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
				});
			m_mipmap_sig = sig.Create(D3DDevice());

			// Create pipeline state object for the compute shader using the root signature.
			D3D12_COMPUTE_PIPELINE_STATE_DESC desc = {
				.pRootSignature = m_mipmap_sig.get(),
				.CS = shader_code::mipmap_generator_cs,
				.NodeMask = 0,
				.CachedPSO = D3D12_CACHED_PIPELINE_STATE{},
				.Flags = D3D12_PIPELINE_STATE_FLAG_NONE,
			};
			Throw(D3DDevice()->CreateComputePipelineState(&desc, __uuidof(ID3D12PipelineState), (void**)&m_mipmap_pso.m_ptr));
		}

		// Create stock resources
		CreateStockTextures();
		CreateStockModels();

		// Wait till stock resources are created
		FlushToGpu(true);

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
		m_stock_textures.resize(0);
		m_stock_models.resize(0);
	}

	// Renderer access
	Renderer& ResourceManager::rdr() const
	{
		return m_rdr;
	}
	ID3D12Device* ResourceManager::D3DDevice() const
	{
		return rdr().D3DDevice();
	}

	// Ensure stock Start/Stop creating resources
	uint64_t ResourceManager::FlushToGpu(bool block)
	{
		if (!m_flush_required)
			return m_gsync.LastAddedSyncPoint();

		// Close the command list
		Throw(m_gfx_cmd_list->Close());

		// Execute the command list
		auto cmd_lists = {static_cast<ID3D12CommandList*>(m_gfx_cmd_list.get())};
		rdr().GfxQueue()->ExecuteCommandLists(s_cast<UINT>(cmd_lists.size()), cmd_lists.begin());
		m_flush_required = false;

		// Add a sync point
		auto sync_point = m_gsync.AddSyncPoint(rdr().GfxQueue());
		m_gfx_cmd_alloc.m_sync_point = sync_point; // Can't use this allocator until the GPU has completed 'sync_point'

		// Reset the command list
		m_gfx_cmd_alloc = m_gfx_cmd_alloc_pool.Get();
		Throw(m_gfx_cmd_list->Reset(m_gfx_cmd_alloc, nullptr));

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
		auto resource_state = has_init_data ? D3D12_RESOURCE_STATE_COPY_DEST : desc.FinalState;

		// Buffer resources specify the Width as the size in bytes, even though for textures width is the pixel count.
		auto rd = static_cast<D3D12_RESOURCE_DESC const&>(desc);
		if (desc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
			rd.Width *= desc.ElemStride;
		if (desc.MipLevels != 1)
			rd.Flags |= D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;

		// Create a GPU visible resource that will hold the created texture/verts/indices/etc.
		Throw(device->CreateCommittedResource(
			&desc.HeapProps, desc.HeapFlags, &rd, resource_state,
			desc.ClearValue ? &*desc.ClearValue : nullptr,
			__uuidof(ID3D12Resource), (void**)&res.m_ptr));

		// If initialisation data is provided, initialise using an UploadBuffer
		if (has_init_data)
		{
			// Copy the initialisation data into the resource
			UpdateSubresource(res.get(), desc.Data, 0, desc.DataAlignment);
			
			// Generate mip maps for the texture (if needed)
			if (desc.MipLevels != 1)
				GenerateMipMaps(res.get(), desc, resource_state);

			// Transition the resource to the final state
			auto barrier = ResourceBarrier::Transition(res.get(), resource_state, desc.FinalState);
			m_gfx_cmd_list->ResourceBarrier(1, &barrier);
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

		// Add the texture instance pointer (not ref counted) to the lookup table.
		// The caller owns the texture, when released it will be removed from this lookup.
		AddLookup(m_lookup_tex, inst->m_id, inst.get());
		return inst;
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

			// Return a clone of the stock texture
			auto stock_tex = FindTexture(*stock);
			if (stock_tex == nullptr)
				throw std::runtime_error(Fmt("Stock texture '%s' not found", resource_path.string().c_str() + 1));

			return stock_tex;
		}

		// Create a texture from embedded resource
		if (*resource_path.c_str() == '@')
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

		// Add a pointer (not ref counted) to the texture instance to the lookup table.
		// The caller owns the texture, when released it will be removed from this lookup.
		AddLookup(m_lookup_tex, inst->m_id, inst.get());
		return inst;
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

				// The faces of the cube
				pr::vector<std::span<uint8_t const>> source_images;

				// Read each face of the cube
				auto idx = res_name.find(L"??");
				if (idx == std::wstring::npos)
					throw std::runtime_error("Cube map texture URI pattern should contain '??'");

				for (auto face : { L"+x", L"-x", L"+y", L"-y", L"+z", L"-z" })
				{
					res_name[idx + 0] = face[0];
					res_name[idx + 1] = face[1];
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
		TextureCubePtr inst(rdr12::New<TextureCube>(*this, res.get(), desc), true);
		assert(m_mem_tracker.add(inst.get()));

		// Add a pointer (not ref counted) to the texture instance to the lookup table.
		// The caller owns the texture, when released it will be removed from this lookup.
		AddLookup(m_lookup_tex, inst->m_id, inst.get());
		return inst;
	}

	// Generate mip maps for a texture
	void ResourceManager::GenerateMipMaps(ID3D12Resource* texture, ResDesc const& desc, D3D12_RESOURCE_STATES& current_state)
	{
		auto dim = iv2(s_cast<int>(desc.Width), s_cast<int>(desc.Height));
		GenerateMipMaps(texture, desc.Format, dim, 1, MipCount(dim), current_state);
	}
	void ResourceManager::GenerateMipMaps(ID3D12Resource* texture, DXGI_FORMAT format, iv2 dim, int mip0, int mip_count, D3D12_RESOURCE_STATES& current_state)
	{
		// Mip 0 is the texture itself, we're not generating that
		if (mip0 <= 0)
			throw std::runtime_error("Mip0 should be >= 1");

		// Get a new empty command list in recording state
		auto cmd_list = m_gfx_cmd_list.get();
		auto heaps = { m_heap_view.get() };

		// Set root signature, PSO, and descriptor heap
		cmd_list->SetComputeRootSignature(m_mipmap_sig.get());
		cmd_list->SetPipelineState(m_mipmap_pso.get());
		cmd_list->SetDescriptorHeaps(s_cast<UINT>(heaps.size()), heaps.begin());

		// Prepare the SRV/UAV view descriptions (changed for each mip).
		D3D12_SHADER_RESOURCE_VIEW_DESC srv_desc = {
			.Format = format,
			.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D,
			.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
			.Texture2D = {
				.MostDetailedMip = 0,
				.MipLevels = 1,
				.PlaneSlice = 0,
				.ResourceMinLODClamp = 0.f,
			},
		};
		D3D12_UNORDERED_ACCESS_VIEW_DESC uav_desc = {
			.Format = format,
			.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2D,
			.Texture2D = {
				.MipSlice = 0,
				.PlaneSlice = 0,
			},
		};


		//?? Transition all subresources?
		//?? Only the dst texture is a uav, source should be srv

		// Transition from the current resource state to unordered access
		auto next_state = D3D12_RESOURCE_STATE_UNORDERED_ACCESS;
		auto barrier0 = ResourceBarrier::Transition(texture, current_state, next_state);
		cmd_list->ResourceBarrier(1, &barrier0);
		current_state = next_state;

		// Loop through the mip-maps copying from the bigger mip-map to the smaller one with down-sampling in a compute shader
		for (auto mip = mip0; mip != mip0 + mip_count; ++mip)
		{
			// Get the dimensions at 'mip'
			auto dst_w = std::max(dim.x >> mip, 1);
			auto dst_h = std::max(dim.y >> mip, 1);

			// Pass the destination texture pixel size to the shader as constants
			using F32U32 = union { float f32; uint32_t u32; };
			cmd_list->SetComputeRoot32BitConstant(s_cast<UINT>(EMipMapParam::Constants), F32U32(1.0f / dst_w).u32, 0);
			cmd_list->SetComputeRoot32BitConstant(s_cast<UINT>(EMipMapParam::Constants), F32U32(1.0f / dst_h).u32, 1);

			// Create shader resource view for the source texture in the descriptor heap
			srv_desc.Texture2D.MostDetailedMip = mip - 1;
			auto srv = m_heap_view.Add(texture, srv_desc);

			//Create unordered access view for the destination texture in the descriptor heap
			uav_desc.Texture2D.MipSlice = mip;
			auto uav = m_heap_view.Add(texture, uav_desc);

			//Pass the source and destination texture views to the shader via descriptor tables
			cmd_list->SetComputeRootDescriptorTable(s_cast<UINT>(EMipMapParam::SrcTexture), srv);
			cmd_list->SetComputeRootDescriptorTable(s_cast<UINT>(EMipMapParam::DstTexture), uav);

			// Dispatch the compute shader with one thread per 8x8 pixels
			cmd_list->Dispatch(
				s_cast<UINT>(std::max(dst_w / 8, 1)),
				s_cast<UINT>(std::max(dst_h / 8, 1)),
				1u);

			// Wait for all accesses to the destination texture UAV to be finished before generating
			// the next mip-map, as it will be the source texture for the next mip-map
			auto barrier = ResourceBarrier::UAV(texture);
			cmd_list->ResourceBarrier(1, &barrier);
		}

		//// When done with the texture, transition it back to the original state
		//auto barrier1 = ResourceBarrier::Transition(texture, D3D12_RESOURCE_STATE_UNORDERED_ACCESS, current_state);
		//cmd_list->ResourceBarrier(1, &barrier1);

		//// Close and submit the command list
		//Throw(cmd_list->Close());
		//auto cmd_lists = { static_cast<ID3D12CommandList*>(cmd_list) };
		//rdr().GfxQueue()->ExecuteCommandLists(1, cmd_lists.begin());
		m_flush_required = true;
	}

	// Create a new nugget
	Nugget* ResourceManager::CreateNugget(NuggetData const& ndata, Model* model, RdrId id)
	{
		auto ptr = rdr12::New<Nugget>(ndata, model, id);
		assert(m_mem_tracker.add(ptr));
		return ptr;
	}

	// Return a stock texture
	Texture2DPtr ResourceManager::FindTexture(EStockTexture id) const
	{
		if (s_cast<size_t>(id) > m_stock_textures.size())
			throw std::runtime_error(FmtS("Stock texture %s does not exist", Enum<EStockTexture>::ToStringA(id)));

		return m_stock_textures[s_cast<int>(id)];
	}

	// Return a pointer to a stock model
	ModelPtr ResourceManager::FindModel(EStockModel id) const
	{
		if (s_cast<size_t>(id) > m_stock_models.size())
			throw std::runtime_error(FmtS("Stock model %s does not exist", Enum<EStockModel>::ToStringA(id)));

		return m_stock_models[s_cast<int>(id)];
	}

	// Create the basic textures that exist from startup
	void ResourceManager::CreateStockTextures()
	{
		// Create the stock textures
		m_stock_textures.resize(EStockTexture_::NumberOf);
		{// EStockTexture::Black
			uint32_t const data[] = {0xFF000000};
			Image src(1, 1, data, DXGI_FORMAT_B8G8R8A8_UNORM);
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::Black), ResDesc::Tex2D(src, 1), false, 0, "#black");
			m_stock_textures[s_cast<int>(EStockTexture::Black)] = CreateTexture2D(tdesc);
		}
		{// EStockTexture::White:
			uint32_t const data[] = {0xFFFFFFFF};
			Image src(1, 1, data, DXGI_FORMAT_B8G8R8A8_UNORM);
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::White), ResDesc::Tex2D(src, 1), false, 0, "#white");
			m_stock_textures[s_cast<int>(EStockTexture::White)] = CreateTexture2D(tdesc);
		}
		{// EStockTexture::Gray:
			uint32_t const data[] = {0xFF808080};
			Image src(1, 1, data, DXGI_FORMAT_B8G8R8A8_UNORM);
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::Gray), ResDesc::Tex2D(src, 1), false, 0, "#gray");
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
			//auto sam = SamDesc::LinearWrap(); sam.Filter = D3D12_FILTER_MIN_MAG_MIP_POINT;
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::Checker), ResDesc::Tex2D(src, 0), false, 0, "#checker");
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
			//auto sam = SamDesc::LinearWrap(); sam.Filter = D3D12_FILTER_MIN_MAG_MIP_POINT;
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::Checker2), ResDesc::Tex2D(src, 0), false, 0, "#checker2");
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
			//auto sam = SamDesc::LinearWrap(); sam.Filter = D3D12_FILTER_MIN_MAG_MIP_POINT;
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::Checker3), ResDesc::Tex2D(src, 0), false, 0, "#checker3");
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
					data[size_t(j * sz + i)] = c.argb;
				}
			}

			Image src(sz, sz, data.data(), DXGI_FORMAT_B8G8R8A8_UNORM);
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::WhiteSpot), ResDesc::Tex2D(src, 0), true, 0, "#whitespot");
			m_stock_textures[s_cast<int>(EStockTexture::WhiteSpot)] = CreateTexture2D(tdesc);
		}
		{// EStockTexture::WhiteTriangle:
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
						(x1 * dy < y* dx) ? 0.0f :  // inside the triangle
						(x0 * dy > y * dx) ? 1.0f :  // outside the triangle
						(Frac(x0 * dy, y * dx, x1 * 0.75f)); // Spanning the edge

					auto c = Colour32White;
					c.a = uint8_t(Lerp(0xFF, 0x00, SmoothStep(0.0f, 1.0f, t)));

					data[size_t(j * sz + hsz - i)] = c.argb;
					data[size_t(j * sz + hsz + i)] = c.argb;
				}
			}

			Image src(sz, sz, data.data(), DXGI_FORMAT_B8G8R8A8_UNORM);
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::WhiteTriangle), ResDesc::Tex2D(src, 0), true, 0, "#whitetriangle");
			m_stock_textures[s_cast<int>(EStockTexture::WhiteTriangle)] = CreateTexture2D(tdesc);
		}
		{// EStockTexture::EnvMapProjection:
			uint32_t const data[] = {0};
			Image src(1, 1, data, DXGI_FORMAT_B8G8R8A8_UNORM);
			TextureDesc tdesc(s_cast<RdrId>(EStockTexture::EnvMapProjection), ResDesc::Tex2D(src, 0), false, 0, "#envmapproj");
			m_stock_textures[s_cast<int>(EStockTexture::EnvMapProjection)] = CreateTexture2D(tdesc);
		}
	}

	// Create stock models
	void ResourceManager::CreateStockModels()
	{
		m_stock_models.resize(EStockModel_::NumberOf);
		{// Basis/focus point model
			constexpr Vert verts[] = {
				{v4( 0.0f,  0.0f,  0.0f, 1.0f), Colour(0xFFFF0000), v4::Zero(), v2::Zero()},
				{v4( 1.0f,  0.0f,  0.0f, 1.0f), Colour(0xFFFF0000), v4::Zero(), v2::Zero()},
				{v4( 0.0f,  0.0f,  0.0f, 1.0f), Colour(0xFF00FF00), v4::Zero(), v2::Zero()},
				{v4( 0.0f,  1.0f,  0.0f, 1.0f), Colour(0xFF00FF00), v4::Zero(), v2::Zero()},
				{v4( 0.0f,  0.0f,  0.0f, 1.0f), Colour(0xFF0000FF), v4::Zero(), v2::Zero()},
				{v4( 0.0f,  0.0f,  1.0f, 1.0f), Colour(0xFF0000FF), v4::Zero(), v2::Zero()},
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

			NuggetData nug(ETopo::LineList, EGeom::Vert|EGeom::Colr);
			nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::ShadowCastExclude, true);
			ptr->CreateNugget(nug);

			m_stock_models[s_cast<int>(EStockModel::Basis)] = ptr;
		}
		{// Unit quad in Z = 0 plane
			constexpr Vert verts[] = {
				{v4(-0.5f,-0.5f, 0, 1), Colour(0xFFFFFFFF), v4::ZAxis(), v2(0.0000f,0.9999f)},
				{v4( 0.5f,-0.5f, 0, 1), Colour(0xFFFFFFFF), v4::ZAxis(), v2(0.9999f,0.9999f)},
				{v4( 0.5f, 0.5f, 0, 1), Colour(0xFFFFFFFF), v4::ZAxis(), v2(0.9999f,0.0000f)},
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

			m_stock_models[s_cast<int>(EStockModel::UnitQuad)] = ptr;
		}
		{// Bounding box cube
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

			m_stock_models[s_cast<int>(EStockModel::BBoxModel)] = ptr;
		}
		{// Selection box
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

			m_stock_models[s_cast<int>(EStockModel::SelectionBox)] = ptr;
		}
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
			m_gfx_cmd_list->CopyBufferRegion(dest, 0U, staging.m_buf, staging.m_ofs, layout[0].Footprint.Width);
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
				m_gfx_cmd_list->CopyTextureRegion(&dst, 0, 0, 0, &src, nullptr);
				m_flush_required = true;
			}
		}
	}
	void ResourceManager::UpdateSubresource(ID3D12Resource* dest, Image const& image, int sub0, int alignment)
	{
		UpdateSubresource(dest, {&image, 1}, sub0, alignment);
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

		// Release any views
		if (tex->m_srv)
			m_descriptor_store.Release(tex->m_srv);
		if (tex->m_uav)
			m_descriptor_store.Release(tex->m_uav);
		if (tex->m_rtv)
			m_descriptor_store.Release(tex->m_rtv);

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

	//// Return a shader to the allocator
	//void ResourceManager::Delete(Shader* shader)
	//{
	//	if (shader == nullptr)
	//		return;

	//	Renderer::Lock lock(rdr());
	//	assert(m_mem_tracker.remove(shader));
	//	rdr12::Delete<Shader>(shader);
	//}
}


// Generating mip maps... 
#if 0
//MipMapTextures is an array containing texture objects that need mip-maps to be generated. It needs a texture resource with mip-maps in D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE state.
//Textures are expected to be POT and in a format supporting unordered access, as well as the D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS set during creation.
//_device is the ID3D12Device
//GetNewCommandList() is supposed to return a new command list in recording state
//SubmitCommandList(commandList) is supposed to submit the command list to the command queue
//MipMapComputeShader is an ID3DBlob of the compiled mip-map compute shader
void D3D12Renderer::Createmip-maps()
{
	//Union used for shader constants
	struct DWParam
	{
		DWParam(FLOAT f) : Float(f) {}
		DWParam(UINT u) : Uint(u) {}

		void operator= (FLOAT f) { Float = f; }
		void operator= (UINT u) { Uint = u; }

		union
		{
			FLOAT Float;
			UINT Uint;
		};
	};

	//Calculate heap size
	uint32 requiredHeapSize = 0;
	MipMapTextures->Enumerate<D3D12Texture>([&](D3D12Texture *texture, size_t index, bool &stop) {
		if(texture->mip-maps > 1)
			requiredHeapSize += texture->mip-maps - 1;
	});

	//No heap size, means that there was either no texture or none that requires any mip-maps
	if(requiredHeapSize == 0)
	{
		MipMapTextures->RemoveAllObjects();
		return;
	}

	//The compute shader expects 2 floats, the source texture and the destination texture
	CD3DX12_DESCRIPTOR_RANGE srvCbvRanges[2];
	CD3DX12_ROOT_PARAMETER rootParameters[3];
	srvCbvRanges[0].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, 0, 0);
	srvCbvRanges[1].Init(D3D12_DESCRIPTOR_RANGE_TYPE_UAV, 1, 0, 0);
	rootParameters[0].InitAsConstants(2, 0);
	rootParameters[1].InitAsDescriptorTable(1, &srvCbvRanges[0]);
	rootParameters[2].InitAsDescriptorTable(1, &srvCbvRanges[1]);

	//Static sampler used to get the linearly interpolated color for the mip-maps
	D3D12_STATIC_SAMPLER_DESC samplerDesc = {};
	samplerDesc.Filter = D3D12_FILTER_MIN_MAG_LINEAR_MIP_POINT;
	samplerDesc.AddressU = D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
	samplerDesc.AddressV = D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
	samplerDesc.AddressW = D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
	samplerDesc.MipLODBias = 0.0f;
	samplerDesc.ComparisonFunc = D3D12_COMPARISON_FUNC_NEVER;
	samplerDesc.MinLOD = 0.0f;
	samplerDesc.MaxLOD = D3D12_FLOAT32_MAX;
	samplerDesc.MaxAnisotropy = 0;
	samplerDesc.BorderColor = D3D12_STATIC_BORDER_COLOR_OPAQUE_BLACK;
	samplerDesc.ShaderRegister = 0;
	samplerDesc.RegisterSpace = 0;
	samplerDesc.ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL;

	//Create the root signature for the mip-map compute shader from the parameters and sampler above
	ID3DBlob *signature;
	ID3DBlob *error;
	CD3DX12_ROOT_SIGNATURE_DESC rootSignatureDesc;
	rootSignatureDesc.Init(_countof(rootParameters), rootParameters, 1, &samplerDesc, D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT);
	D3D12SerializeRootSignature(&rootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, &signature, &error);
	ID3D12RootSignature *mip-mapRootSignature;
	_device->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), IID_PPV_ARGS(&mip-mapRootSignature));

	//Create the descriptor heap with layout: source texture - destination texture
	D3D12_DESCRIPTOR_HEAP_DESC heapDesc = {};
	heapDesc.NumDescriptors = 2*requiredHeapSize;
	heapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
	heapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;
	ID3D12DescriptorHeap *descriptorHeap;
	_device->CreateDescriptorHeap(&heapDesc, IID_PPV_ARGS(&descriptorHeap));
	UINT descriptorSize = _device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);

	//Create pipeline state object for the compute shader using the root signature.
	D3D12_COMPUTE_PIPELINE_STATE_DESC psoDesc = {};
	psoDesc.pRootSignature = mip-mapRootSignature;
	psoDesc.CS = { reinterpret_cast<UINT8*>(MipMapComputeShader->GetBufferPointer()), MipMapComputeShader->GetBufferSize() };
	ID3D12PipelineState *psomip-maps;
	_device->CreateComputePipelineState(&psoDesc, IID_PPV_ARGS(&psomip-maps));


	//Prepare the shader resource view description for the source texture
	D3D12_SHADER_RESOURCE_VIEW_DESC srcTextureSRVDesc = {};
	srcTextureSRVDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
	srcTextureSRVDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;

	//Prepare the unordered access view description for the destination texture
	D3D12_UNORDERED_ACCESS_VIEW_DESC destTextureUAVDesc = {};
	destTextureUAVDesc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2D;

	//Get a new empty command list in recording state
	ID3D12GraphicsCommandList *commandList = GetNewCommandList();

	//Set root signature, pso and descriptor heap
	commandList->SetComputeRootSignature(mip-mapRootSignature);
	commandList->SetPipelineState(psomip-maps);
	commandList->SetDescriptorHeaps(1, &descriptorHeap);

	//CPU handle for the first descriptor on the descriptor heap, used to fill the heap
	CD3DX12_CPU_DESCRIPTOR_HANDLE currentCPUHandle(descriptorHeap->GetCPUDescriptorHandleForHeapStart(), 0, descriptorSize);

	//GPU handle for the first descriptor on the descriptor heap, used to initialize the descriptor tables
	CD3DX12_GPU_DESCRIPTOR_HANDLE currentGPUHandle(descriptorHeap->GetGPUDescriptorHandleForHeapStart(), 0, descriptorSize);

	MipMapTextures->Enumerate<D3D12Texture>([&](D3D12Texture *texture, size_t index, bool &stop) {
		//Skip textures without mip-maps
		if(texture->mip-maps <= 1)
			return;

		//Transition from pixel shader resource to unordered access
		commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(texture->_resource, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE, D3D12_RESOURCE_STATE_UNORDERED_ACCESS));

		//Loop through the mip-maps copying from the bigger mip-map to the smaller one with downsampling in a compute shader
		for(uint32_t TopMip = 0; TopMip < texture->mip-maps-1; TopMip++)
		{
			//Get mip-map dimensions
			uint32_t dstWidth = std::max(texture->width >> (TopMip+1), 1);
			uint32_t dstHeight = std::max(texture->height >> (TopMip+1), 1);

			//Create shader resource view for the source texture in the descriptor heap
			srcTextureSRVDesc.Format = texture->_format;
			srcTextureSRVDesc.Texture2D.MipLevels = 1;
			srcTextureSRVDesc.Texture2D.MostDetailedMip = TopMip;
			_device->CreateShaderResourceView(texture->_resource, &srcTextureSRVDesc, currentCPUHandle);
			currentCPUHandle.Offset(1, descriptorSize);

			//Create unordered access view for the destination texture in the descriptor heap
			destTextureUAVDesc.Format = texture->_format;
			destTextureUAVDesc.Texture2D.MipSlice = TopMip+1;
			_device->CreateUnorderedAccessView(texture->_resource, nullptr, &destTextureUAVDesc, currentCPUHandle);
			currentCPUHandle.Offset(1, descriptorSize);

			//Pass the destination texture pixel size to the shader as constants
			commandList->SetComputeRoot32BitConstant(0, DWParam(1.0f/dstWidth).Uint, 0);
			commandList->SetComputeRoot32BitConstant(0, DWParam(1.0f/dstHeight).Uint, 1);
			
			//Pass the source and destination texture views to the shader via descriptor tables
			commandList->SetComputeRootDescriptorTable(1, currentGPUHandle);
			currentGPUHandle.Offset(1, descriptorSize);
			commandList->SetComputeRootDescriptorTable(2, currentGPUHandle);
			currentGPUHandle.Offset(1, descriptorSize);

			//Dispatch the compute shader with one thread per 8x8 pixels
			commandList->Dispatch(std::max(dstWidth / 8, 1u), std::max(dstHeight / 8, 1u), 1);

			//Wait for all accesses to the destination texture UAV to be finished before generating the next mip-map, as it will be the source texture for the next mip-map
			commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::UAV(texture->_resource));
		}

		//When done with the texture, transition it's state back to be a pixel shader resource
		commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(texture->_resource, D3D12_RESOURCE_STATE_UNORDERED_ACCESS, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE));
	});

	//Close and submit the command list
	commandList->Close();
	SubmitCommandList(commandList);

	MipMapTextures->RemoveAllObjects();
}

// Shader
{
Texture2D<float4> SrcTexture : register(t0);
RWTexture2D<float4> DstTexture : register(u0);
SamplerState BilinearClamp : register(s0);

cbuffer CB : register(b0)
{
	float2 TexelSize;	// 1.0 / destination dimension
}

[numthreads( 8, 8, 1 )]
void Generatemip-maps(uint3 DTid : SV_DispatchThreadID)
{
	//DTid is the thread ID * the values from numthreads above and in this case correspond to the pixels location in number of pixels.
	//As a result texcoords (in 0-1 range) will point at the center between the 4 pixels used for the mip-map.
	float2 texcoords = TexelSize * (DTid.xy + 0.5);

	//The samplers linear interpolation will mix the four pixel values to the new pixels color
	float4 color = SrcTexture.SampleLevel(BilinearClamp, texcoords, 0);

	//Write the final color into the destination texture.
	DstTexture[DTid.xy] = color;
}
}
#endif