//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/stock_resources.h"
#include "pr/view3d-12/resource/descriptor_store.h"
#include "pr/view3d-12/resource/gpu_upload_buffer.h"
#include "pr/view3d-12/resource/gpu_descriptor_heap.h"
#include "pr/view3d-12/resource/mipmap_generator.h"
#include "pr/view3d-12/utility/lookup.h"
#include "pr/view3d-12/utility/gpu_sync.h"
#include "pr/view3d-12/utility/cmd_alloc.h"
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

		using GfxCmdList         = D3DPtr<ID3D12GraphicsCommandList>;
		using SignaturePtr       = D3DPtr<ID3D12RootSignature>;
		using PipelineStatePtr   = D3DPtr<ID3D12PipelineState>;
		using TextureLookup      = Lookup<RdrId, TextureBase*>;
		using DxResLookup        = Lookup<RdrId, ID3D12Resource*>;
		using AllocationsTracker = AllocationsTracker<void>;
		using GpuViewHeap        = GpuDescriptorHeap<D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV>;
		using ModelCont          = pr::vector<ModelPtr>;
		using TextureCont        = pr::vector<Texture2DPtr>;
		using GdiPlus            = pr::GdiPlus;

		AllocationsTracker m_mem_tracker;        // Resource allocation tracker
		Renderer&          m_rdr;                // The owning renderer instance
		GpuSync            m_gsync;              // Sync with GPU
		GfxCmdAllocPool    m_gfx_cmd_alloc_pool; // A pool of command allocators.
		GfxCmdAlloc        m_gfx_cmd_alloc;      // Command list allocator for resource manager operations
		GfxCmdList         m_gfx_cmd_list;       // Command list for resource manager operations.
		GpuViewHeap        m_heap_view;          // GPU visible descriptor heap for CBV/SRV/UAV
		DxResLookup        m_lookup_res;         // A map from hash of resource URI to existing Dx12 resource pointer.
		TextureLookup      m_lookup_tex;         // A map from texture id to existing texture instances.
		GpuUploadBuffer    m_upload_buffer;      // Upload memory buffer for initialising resources
		DescriptorStore    m_descriptor_store;   // Manager of resource descriptors
		MipMapGenerator    m_mipmap_gen;         // Utility class for generating mip maps for a texture
		GdiPlus            m_gdiplus;            // Context scope for GDI
		AutoSub            m_eh_resize;          // Event handler subscription for the RT resize event
		int                m_gdi_dc_ref_count;   // Used to detect outstanding DC references
		ModelCont          m_stock_models;       // Stock models
		TextureCont        m_stock_textures;     // Stock textures
		bool               m_flush_required;     // True if commands have been added to the command list and need sending to the GPU

	public:

		explicit ResourceManager(Renderer& rdr);
		ResourceManager(ResourceManager const&) = delete;
		ResourceManager& operator = (ResourceManager const&) = delete;
		~ResourceManager();

		// Renderer access
		Renderer& rdr() const;
		ID3D12Device* D3DDevice() const;

		// Flush creation commands to the GPU. Returns the sync point for when they've been executed
		uint64_t FlushToGpu(bool block);
		void Wait(uint64_t sync_point) const;

		// Create a model.
		ModelPtr CreateModel(ModelDesc const& desc);

		// Create a new texture instance.
		Texture2DPtr CreateTexture2D(TextureDesc const& desc);
		Texture2DPtr CreateTexture2D(std::filesystem::path const& resource_path, TextureDesc const& desc);
		TextureCubePtr CreateTextureCube(std::filesystem::path const& resource_path, TextureDesc const& desc);

		// Create a new nugget
		Nugget* CreateNugget(NuggetData const& ndata, Model* model, RdrId id = 0);

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

		// Return a pointer to a stock texture
		Texture2DPtr FindTexture(EStockTexture stock) const;

		// Return a pointer to a stock model
		ModelPtr FindModel(EStockModel model) const;

		// An event that is called when a texture filepath cannot be resolved.
		EventHandler<ResourceManager&, ResolvePathArgs&, true> ResolveFilepath;

		// Raised when a model is deleted
		EventHandler<Model&, EmptyArgs const&, true> ModelDeleted;

	private:

		friend struct Model;
		friend struct Nugget;
		friend struct TextureBase;
		friend struct Shader;
		
		// Create the basic textures that exist from startup
		void CreateStockTextures();

		// Create stock models
		void CreateStockModels();

		// Create and initialise a resource
		D3DPtr<ID3D12Resource> CreateResource(ResDesc const& desc);

		// Update the data in 'dest' using a staging buffer
		void UpdateSubresource(ID3D12Resource* dest, std::span<Image const> images, int sub0, int alignment);
		void UpdateSubresource(ID3D12Resource* dest, Image const& image, int sub0, int alignment);

		// Delete objects created by the resource manager.
		// The objects themselves call this when their last reference is dropped.
		void Delete(Model* model);
		void Delete(Nugget* nugget);
		void Delete(TextureBase* tex);
	};
}
