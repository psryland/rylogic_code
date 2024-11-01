//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/stock_resources.h"
#include "pr/view3d-12/resource/descriptor_store.h"
#include "pr/view3d-12/resource/gpu_descriptor_heap.h"
#include "pr/view3d-12/utility/lookup.h"

namespace pr::rdr12
{
	struct ResourceStore
	{
		// Notes:
		// - The resource store is a thread synchronised database of models, textures, samplers, etc.
		// - The resource store is unique and owned by the renderer instance.
		// - The resource manager is an instance-able object that is used to create resources.
		//   It interacts with the resource store to store and retrieve resources.

	private:

		using Mutex          = std::recursive_mutex;
		using ModelCont      = pr::vector<ModelPtr>;
		using TextureCont    = pr::vector<Texture2DPtr>;
		using SamplerCont    = pr::vector<SamplerPtr>;
		using TextureLookup  = Lookup<RdrId, TextureBase*>;
		using SamplerLookup  = Lookup<RdrId, Sampler*>;
		using DxResLookup    = Lookup<RdrId, ID3D12Resource*>;
		//using GpuViewHeap    = GpuDescriptorHeap<D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV>;
		//using GpuSamplerHeap = GpuDescriptorHeap<D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER>;

		Renderer&       m_rdr;              // The owning renderer instance
		Mutex           m_mutex;            // Main mutex for store access
		//GpuViewHeap     m_heap_view;        // GPU visible descriptor heap for CBV/SRV/UAV
		//GpuSamplerHeap  m_heap_sampler;     // GPU visible descriptor heap for samplers
		DxResLookup     m_lookup_res;       // A map from hash of resource URI to existing Dx12 resource pointer.
		TextureLookup   m_lookup_tex;       // A map from texture id to existing texture instances.
		SamplerLookup   m_lookup_sam;       // A map from sampler id to existing sampler instances.
		DescriptorStore m_descriptor_store; // Manager of resource descriptors
		TextureCont     m_stock_textures;   // Stock textures
		SamplerCont     m_stock_samplers;   // Stock samplers
		GdiPlus         m_gdiplus;          // Context scope for GDI

	public:

		ResourceStore(Renderer& rdr);

		// Return one of the stock textures. These should be considered immutable.
		Texture2DPtr StockTexture(EStockTexture id) const;
		SamplerPtr StockSampler(EStockSampler id) const;

		// Raised when a model is deleted (note: on any thread)
		EventHandler<Model&, EmptyArgs const&, true> ModelDeleted;

		// Access to the resource store
		struct Access
		{
		private:
			std::lock_guard<Mutex> m_lock;
			ResourceStore& m_store;

		public:

			Access(Renderer& rdr);
			Access(Access&&) = delete;
			Access(Access const&) = delete;
			Access& operator = (Access&&) = delete;
			Access& operator = (Access const&) = delete;

			// Access to the descriptor store for creating descriptors
			DescriptorStore& Descriptors();

			// Find a resource by it's URI hash
			D3DPtr<ID3D12Resource> FindRes(RdrId id) const;

			// Return a pointer to an existing texture
			template <typename TextureType> requires (std::is_base_of_v<TextureBase, TextureType>)
			RefPtr<TextureType> FindTexture(RdrId id) const;

			// Convenience method for cached textures
			template <typename TextureType> requires(std::is_base_of_v<TextureBase, TextureType>)
			RefPtr<TextureType> FindTexture(RdrId id, std::function<RefPtr<TextureType>()> factory);

			// Find an existing sampler by it's id
			SamplerPtr FindSampler(RdrId id) const;

			// Add a resource to the store
			void Add(RdrId id, ID3D12Resource* res);

			// Add a texture to the store
			void Add(TextureBase* tex);

			// Add a sampler to the store
			void Add(Sampler* sam);

		private:

			friend struct Model;
			friend struct Nugget;
			friend struct TextureBase;
			friend struct Sampler;

			// Delete objects within this store. The objects themselves
			// call these methods when their last reference is dropped.
			void Delete(Model* model);
			void Delete(Nugget* nugget);
			void Delete(TextureBase* tex);
			void Delete(Sampler* sam);
		};
	};
}
