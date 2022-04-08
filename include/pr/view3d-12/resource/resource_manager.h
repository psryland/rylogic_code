//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/stock_resources.h"
#include "pr/view3d-12/utility/lookup.h"
#include "pr/view3d-12/utility/gpu_sync.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	struct ResourceManager
	{
		// Notes:
		//  - Maintains resource heaps and allocation of resources (i.e. vertex buffers, index buffers, textures, etc)
		//  - Use 'GetPrivateData(WKPDID_D3DDebugObjectNameW,..)' to get names of resources.
	private:

		using DxResPointers = struct { ID3D12Resource* res; };//ID3D11ShaderResourceView* srv;
		using DxStageBuffer = struct { D3DPtr<ID3D12Resource> buf; uint64_t locked; uint64_t size; };
		using TextureLookup      = Lookup<RdrId, TextureBase*>;
		using DxResLookup        = Lookup<RdrId, DxResPointers>;
		using AllocationsTracker = AllocationsTracker<void>;

		AllocationsTracker                m_mem_tracker;        // Resource allocation tracker
		Renderer&                         m_rdr;                // The owning renderer instance
		D3DPtr<ID3D12CommandAllocator>    m_cmd_alloc;          // Command list allocator for resource manager operations
		D3DPtr<ID3D12GraphicsCommandList> m_cmd_list;           // Command list for resource manager operations.
		GpuSync                           m_gpu_sync;           // Sync with GPU
		DxResLookup                       m_lookup_dxres;       // A map from hash of resource URI to existing Dx12 resource pointers.
		TextureLookup                     m_lookup_tex;         // A map from texture id to existing texture instances.
		pr::vector<DxStageBuffer>         m_staging_buffers;    // A pool of staging buffers
		pr::GdiPlus                       m_gdiplus;            // Context scope for GDI
		AutoSub                           m_eh_resize;          // Event handler subscription for the RT resize event
		int                               m_gdi_dc_ref_count;   // Used to detect outstanding DC references

		// Stock models
		ModelPtr m_stock_models[EStockModel_::NumberOf];

		// Stock textures
		Texture2DPtr m_stock_textures[EStockTexture_::NumberOf];

	public:

		ResourceManager(Renderer& rdr);
		ResourceManager(ResourceManager const&) = delete;
		ResourceManager& operator = (ResourceManager const&) = delete;
		~ResourceManager();

		// Renderer access
		Renderer& rdr();

		// An event that is called when a texture filepath cannot be resolved.
		EventHandler<ResourceManager&, ResolvePathArgs&, true> ResolveFilepath;

		// Flush creation commands to the GPU
		void FlushToGpu();

		// Create a model.
		ModelPtr CreateModel(ModelDesc const& mdesc);

		// Create a new texture instance.
		Texture2DPtr CreateTexture2D(TextureDesc const& tdesc);

		// Create a new nugget
		Nugget* CreateNugget(NuggetData const& ndata, Model* model);

		// Return a pointer to an existing texture
		template <typename TextureType, typename = std::enable_if_t<std::is_base_of_v<TextureBase, TextureType>>>
		RefPtr<TextureType> FindTexture(RdrId id) const
		{
			auto tex = GetOrDefault(m_lookup_tex, id, (TextureBase*)nullptr);
			return pr::RefPtr<TextureType>(static_cast<TextureType*>(tex), true);
		}
		
		// Return a pointer to a stock texture
		Texture2DPtr FindTexture(EStockTexture stock) const;

		// Return a pointer to a stock model
		ModelPtr FindModel(EStockModel model) const;

		//// Convenience method for cached textures
		//template <typename TextureType, typename Factory, typename = std::enable_if_t<std::is_base_of_v<TextureBase, TextureType>>>
		//RefPtr<TextureType> GetTexture(RdrId id, Factory factory)
		//{
		//	auto tex = FindTexture<TextureType>(id);
		//	return tex != nullptr ? tex : factory();
		//}

		// Raised when a model is deleted
		EventHandler<Model&, EmptyArgs const&, true> ModelDeleted;

	private:

		friend struct Model;
		friend struct Nugget;
		friend struct TextureBase;
		
		// Create the basic textures that exist from startup
		void CreateStockTextures();

		// Create stock models
		void CreateStockModels();

		// Return a staging buffer that is at least 'size' big
		DxStageBuffer& GetStagingBuffer(ID3D12Device* device, uint64_t size);

		// Delete objects created by the resource manager.
		// The objects themselves call this when their last reference is dropped.
		void Delete(Model* model);
		void Delete(TextureBase* tex);
		void Delete(Nugget* nugget);
	};
}
