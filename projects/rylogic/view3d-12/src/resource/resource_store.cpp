//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/resource/resource_store.h"
#include "pr/view3d-12/resource/resource_factory.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/texture/texture_base.h"
#include "pr/view3d-12/texture/texture_2d.h"
#include "pr/view3d-12/texture/texture_cube.h"
#include "pr/view3d-12/sampler/sampler.h"

namespace pr::rdr12
{
	constexpr int HeapCapacityView = 12;

	ResourceStore::ResourceStore(Renderer& rdr)
		: m_rdr(rdr)
		, m_mutex()
		//, m_heap_view(HeapCapacityView, m_gsync)
		//, m_heap_sampler(HeapCapacityView, m_gsync)
		, m_lookup_res()
		, m_lookup_tex()
		, m_lookup_sam()
		, m_descriptor_store(rdr.D3DDevice())
		, m_stock_textures(EStockTexture_::NumberOf)
		, m_stock_samplers(EStockSampler_::NumberOf)
		, m_gdiplus()
	{
		ResourceFactory factory(rdr);

		// Create the stock textures
		for (auto id : EStockTexture_::Members())
		{
			if (id == EStockTexture::Invalid) continue;
			m_stock_textures[s_cast<int>(id)] = factory.CreateTexture(id);
		}

		// Create the stock samplers
		for (auto id : EStockSampler_::Members())
		{
			if (id == EStockSampler::Invalid) continue;
			m_stock_samplers[s_cast<int>(id)] = factory.GetSampler(id);
		}
	}
	
	// Stock resources
	Texture2DPtr ResourceStore::StockTexture(EStockTexture id) const
	{
		return m_stock_textures[s_cast<int>(id)];
	}
	SamplerPtr ResourceStore::StockSampler(EStockSampler id) const
	{
		return m_stock_samplers[s_cast<int>(id)];
	}

	// Synchronous access to the store
	ResourceStore::Access::Access(Renderer& rdr)
		:m_lock(rdr.store().m_mutex)
		,m_store(rdr.store())
	{
	}

	// Access to the descriptor store for creating descriptors
	DescriptorStore& ResourceStore::Access::Descriptors()
	{
		return m_store.m_descriptor_store;
	}

	// Find a resource by it's URI hash
	D3DPtr<ID3D12Resource> ResourceStore::Access::FindRes(RdrId id) const
	{
		auto iter = m_store.m_lookup_res.find(id);
		if (iter == end(m_store.m_lookup_res))
			return nullptr;

		return D3DPtr<ID3D12Resource>(iter->second, true);
	}

	// Return a pointer to an existing texture
	template <typename TextureType> requires (std::is_base_of_v<TextureBase, TextureType>)
	RefPtr<TextureType> ResourceStore::Access::FindTexture(RdrId id) const
	{
		auto iter = m_store.m_lookup_tex.find(id);
		return iter != end(m_store.m_lookup_tex)
			? RefPtr<TextureType>(static_cast<TextureType*>(iter->second), true)
			: nullptr;
	}
	template RefPtr<Texture2D> ResourceStore::Access::FindTexture<Texture2D>(RdrId id) const;
	template RefPtr<TextureCube> ResourceStore::Access::FindTexture<TextureCube>(RdrId id) const;

	// Convenience method for cached textures
	template <typename TextureType> requires(std::is_base_of_v<TextureBase, TextureType>)
	RefPtr<TextureType> ResourceStore::Access::FindTexture(RdrId id, std::function<RefPtr<TextureType>()> factory)
	{
		auto tex = FindTexture<TextureType>(id);
		return tex != nullptr ? tex : factory();
	}
	template RefPtr<Texture2D> ResourceStore::Access::FindTexture<Texture2D>(RdrId id, std::function<RefPtr<Texture2D>()> factory);
	template RefPtr<TextureCube> ResourceStore::Access::FindTexture<TextureCube>(RdrId id, std::function<RefPtr<TextureCube>()> factory);

	// Find an existing sampler by it's id
	SamplerPtr ResourceStore::Access::FindSampler(RdrId id) const
	{
		auto iter = m_store.m_lookup_sam.find(id);
		return iter != end(m_store.m_lookup_sam)
			? SamplerPtr(iter->second, true)
			: nullptr;
	}

	// Add a resource to the store
	void ResourceStore::Access::Add(RdrId id, ID3D12Resource* res)
	{
		AddLookup(m_store.m_lookup_res, id, res);
	}

	// Add a texture to the store
	void ResourceStore::Access::Add(TextureBase* tex)
	{
		// Check whether 'id' already exists, if so, throw. Users should use FindTexture first.
		if (m_store.m_lookup_tex.find(tex->m_id) != end(m_store.m_lookup_tex))
			throw std::runtime_error(FmtS("Texture Id '%d' is already in use", tex->m_id));

		// Add the texture instance pointer (not ref counted) to the lookup table.
		// The caller owns the texture, when released it will be removed from this lookup.
		AddLookup(m_store.m_lookup_tex, tex->m_id, tex);
	}

	// Add a sampler to the store
	void ResourceStore::Access::Add(Sampler* sam)
	{
		// Check whether 'id' already exists, if so, throw. Users should use FindTexture first.
		if (m_store.m_lookup_sam.find(sam->m_id) != end(m_store.m_lookup_sam))
			throw std::runtime_error(FmtS("Sampler Id '%d' is already in use", sam->m_id));

		// Add the texture instance pointer (not ref counted) to the lookup table.
		// The caller owns the texture, when released it will be removed from this lookup.
		AddLookup(m_store.m_lookup_sam, sam->m_id, sam);
	}

	// Return a model to the allocator
	void ResourceStore::Access::Delete(Model* model)
	{
		assert(model != nullptr);

		// Notify model deleted
		m_store.ModelDeleted(*model);

		assert(m_store.m_rdr.mem_tracker().remove(model));
		rdr12::Delete<Model>(model);
	}
	
	// Return a render nugget to the allocator
	void ResourceStore::Access::Delete(Nugget* nugget)
	{
		assert(nugget != nullptr);

		assert(m_store.m_rdr.mem_tracker().remove(nugget));
		rdr12::Delete<Nugget>(nugget);
	}

	// Return a texture to the allocator.
	void ResourceStore::Access::Delete(TextureBase* tex)
	{
		assert(tex != nullptr);

		// Find 'tex' in the map of RdrIds to texture instances
		// We'll remove this, but first use it as a non-const reference
		auto iter = m_store.m_lookup_tex.find(tex->m_id);
		assert("Texture not found" && iter != end(m_store.m_lookup_tex));

		// If the DX texture will be released when we clean up this texture
		// then check whether it is in the 'fname' lookup table and remove it if it is.
		if (tex->m_uri != 0 && tex->m_res.RefCount() == 1)
		{
			// Remove the DX resource from our lookup
			auto jter = m_store.m_lookup_res.find(tex->m_uri);
			if (jter != end(m_store.m_lookup_res))
				m_store.m_lookup_res.erase(jter);
		}

		// Delete the texture and remove the entry from the RdrId lookup map
		assert(m_store.m_rdr.mem_tracker().remove(iter->second));
		rdr12::Delete<TextureBase>(iter->second);
		m_store.m_lookup_tex.erase(iter);
	}

	// Return a sampler to the allocator.
	void ResourceStore::Access::Delete(Sampler* sam)
	{
		assert(sam != nullptr);

		// Find 'sam' in the map of RdrIds to sampler instances
		// We'll remove this, but first use it as a non-const reference
		auto iter = m_store.m_lookup_sam.find(sam->m_id);
		assert("Sampler not found" && iter != end(m_store.m_lookup_sam));

		// Delete the texture and remove the entry from the RdrId lookup map
		assert(m_store.m_rdr.mem_tracker().remove(iter->second));
		rdr12::Delete<Sampler>(iter->second);
		m_store.m_lookup_sam.erase(iter);
	}
}
