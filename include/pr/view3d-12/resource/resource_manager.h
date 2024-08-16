//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/stock_resources.h"
#include "pr/view3d-12/resource/descriptor_store.h"
#include "pr/view3d-12/resource/resource_state_store.h"
#include "pr/view3d-12/resource/gpu_transfer_buffer.h"
#include "pr/view3d-12/resource/gpu_descriptor_heap.h"
#include "pr/view3d-12/resource/mipmap_generator.h"
#include "pr/view3d-12/utility/lookup.h"
#include "pr/view3d-12/utility/gpu_sync.h"
#include "pr/view3d-12/utility/cmd_alloc.h"
#include "pr/view3d-12/utility/cmd_list.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	struct ResourceManager
	{
		// Notes:
		//  - The resource manager is used by all windows and scenes and therefore has it's own cmd allocator and list.
		//  - When resources are created, the commands are added to the internal command list. Callers need to 
		//  - Maintains resource heaps and allocation of resources (i.e. vertex buffers, index buffers, textures, etc)
		//  - Use 'GetPrivateData(WKPDID_D3DDebugObjectNameW,..)' to get names of resources.
		//  - In Dx12, samplers are separate from textures. Callers should create and store them separately.
		//  - Compiler complaints about Model not being defined here means you have forgotten to include model.h in some cpp.
	private:

		using SignaturePtr       = D3DPtr<ID3D12RootSignature>;
		using PipelineStatePtr   = D3DPtr<ID3D12PipelineState>;
		using TextureLookup      = Lookup<RdrId, TextureBase*>;
		using SamplerLookup      = Lookup<RdrId, Sampler*>;
		using DxResLookup        = Lookup<RdrId, ID3D12Resource*>;
		using AllocationsTracker = AllocationsTracker<void>;
		using GpuViewHeap        = GpuDescriptorHeap<D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV>;
		using GpuSamplerHeap     = GpuDescriptorHeap<D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER>;
		using ModelCont          = pr::vector<ModelPtr>;
		using TextureCont        = pr::vector<Texture2DPtr>;
		using SamplerCont        = pr::vector<SamplerPtr>;
		using GdiPlus            = pr::GdiPlus;

		AllocationsTracker m_mem_tracker;        // Resource allocation tracker
		Renderer&          m_rdr;                // The owning renderer instance
		GpuSync            m_gsync;              // Sync with GPU
		KeepAlive          m_keep_alive;         // Keep alive for the resource manager
		GfxCmdAllocPool    m_gfx_cmd_alloc_pool; // A pool of command allocators.
		GfxCmdList         m_gfx_cmd_list;       // Command list for resource manager operations.
		GpuViewHeap        m_heap_view;          // GPU visible descriptor heap for CBV/SRV/UAV
		GpuSamplerHeap     m_heap_sampler;       // GPU visible descriptor heap for samplers
		DxResLookup        m_lookup_res;         // A map from hash of resource URI to existing Dx12 resource pointer.
		TextureLookup      m_lookup_tex;         // A map from texture id to existing texture instances.
		SamplerLookup      m_lookup_sam;         // A map from sampler id to existing sampler instances.
		GpuUploadBuffer    m_upload_buffer;      // Upload memory buffer for initialising resources
		DescriptorStore    m_descriptor_store;   // Manager of resource descriptors
		MipMapGenerator    m_mipmap_gen;         // Utility class for generating mip maps for a texture
		TextureCont        m_stock_textures;     // Stock textures
		SamplerCont        m_stock_samplers;     // Stock samplers
		GdiPlus            m_gdiplus;            // Context scope for GDI
		AutoSub            m_eh_resize;          // Event handler subscription for the RT resize event
		int                m_gdi_dc_ref_count;   // Used to detect outstanding DC references
		bool               m_flush_required;     // True if commands have been added to the command list and need sending to the GPU

	public:

		explicit ResourceManager(Renderer& rdr);
		ResourceManager(ResourceManager const&) = delete;
		ResourceManager& operator = (ResourceManager const&) = delete;
		~ResourceManager();

		// Renderer access
		ID3D12Device4* d3d() const;
		Renderer& rdr() const;

		// Flush creation commands to the GPU. Returns the sync point for when they've been executed
		uint64_t FlushToGpu(EGpuFlush flush);
		void Wait(uint64_t sync_point) const;
		
		// Create and initialise a resource
		D3DPtr<ID3D12Resource> CreateResource(ResDesc const& desc, char const* name);

		// Create a model.
		ModelPtr CreateModel(ModelDesc const& mdesc, D3DPtr<ID3D12Resource> vb, D3DPtr<ID3D12Resource> ib);
		ModelPtr CreateModel(ModelDesc const& desc);
		ModelPtr CreateModel(EStockModel id);

		// Create a new texture instance.
		Texture2DPtr CreateTexture2D(TextureDesc const& desc);
		Texture2DPtr CreateTexture2D(std::filesystem::path const& resource_path, TextureDesc const& desc);
		TextureCubePtr CreateTextureCube(std::filesystem::path const& resource_path, TextureDesc const& desc);
		Texture2DPtr CreateTexture(EStockTexture id);

		// Create a new sampler instance
		SamplerPtr GetSampler(SamplerDesc const& desc);
		SamplerPtr GetSampler(EStockSampler id);

		// Return one of the stock textures. These should be considered immutable.
		Texture2DPtr StockTexture(EStockTexture id) const;
		SamplerPtr StockSampler(EStockSampler id) const;

		// Create a new nugget
		Nugget* CreateNugget(NuggetDesc const& ndata, Model* model);

		// Return a pointer to an existing texture
		template <typename TextureType> requires (std::is_base_of_v<TextureBase, TextureType>)
		RefPtr<TextureType> FindTexture(RdrId id) const
		{
			auto tex = GetOrDefault(m_lookup_tex, id, (TextureBase*)nullptr);
			return RefPtr<TextureType>(static_cast<TextureType*>(tex), true);
		}

		// Convenience method for cached textures
		template <typename TextureType> requires(std::is_base_of_v<TextureBase, TextureType>)
		RefPtr<TextureType> FindTexture(RdrId id, std::function<RefPtr<TextureType>()> factory)
		{
			auto tex = FindTexture<TextureType>(id);
			return tex != nullptr ? tex : factory();
		}

		// An event that is called when a texture filepath cannot be resolved.
		EventHandler<ResourceManager const&, ResolvePathArgs&, true> ResolveFilepath;

		// Raised when a model is deleted
		EventHandler<Model&, EmptyArgs const&, true> ModelDeleted;

	private:

		friend struct Model;
		friend struct Nugget;
		friend struct TextureBase;
		friend struct Sampler;
		friend struct Shader;
		friend struct UpdateSubresourceScope;

		// Use the 'ResolveFilepath' event to resolve a filepath
		std::filesystem::path ResolvePath(std::string_view path) const;

		// Delete objects created by the resource manager.
		// The objects themselves call this when their last reference is dropped.
		void Delete(Model* model);
		void Delete(Nugget* nugget);
		void Delete(TextureBase* tex);
		void Delete(Sampler* sam);
	};
}
