//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	struct Model :RefCounted<Model>
	{
		ResourceManager*       m_mgr;     // The manager that created this model
		size_t                 m_vcount;  // The count of elements in the V-buffer
		size_t                 m_icount;  // The count of elements in the I-buffer
		TNuggetChain           m_nuggets; // The nuggets for this model
		D3DPtr<ID3D12Resource> m_vb;      // The vertex buffer
		D3DPtr<ID3D12Resource> m_ib;      // The index buffer

		Model(ResourceManager& mgr, size_t vcount, size_t icount, ID3D12Resource* vb, ID3D12Resource* ib);
		Model(Model const&) = delete;
		Model& operator =(Model const&) = delete;
		~Model();

		// Renderer access
		Renderer& rdr() const;
		ResourceManager& res_mgr() const;

		// Access to the vertex/index buffers
		// Only returns false if 'D3D11_MAP_FLAG_DO_NOT_WAIT' flag is set, all other fail cases throw
		//todo bool MapVerts  (Lock& lock, EMap map_type = EMap::Write, EMapFlags flags = EMapFlags::None, Range vrange = RangeZero);
		//todo bool MapIndices(Lock& lock, EMap map_type = EMap::Write, EMapFlags flags = EMapFlags::None, Range irange = RangeZero);

		// Create a nugget from a range within this model
		// Ranges are model relative, i.e. the first vert in the model is range [0,1)
		// Remember you might need to delete render nuggets first
		void CreateNugget(NuggetData const& props);

		// Call to release the nuggets that this model has been
		// divided into. Nuggets are the contiguous sub groups
		// of the model geometry that use the same data.
		void DeleteNuggets();

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<Model>* doomed);
	};
}
