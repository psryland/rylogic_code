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
		enum class EDbgFlags
		{
			None                  = 0,
			WarnedNoRenderNuggets = 1 << 0,
			NormalsVisible        = 1 << 1,
			_flags_enum,
		};

		ResourceManager*         m_mgr;       // The manager that created this model
		size_t                   m_vcount;    // The count of elements in the V-buffer
		size_t                   m_icount;    // The count of elements in the I-buffer
		int                      m_vstride;   // The size (in bytes) of a single V-element
		int                      m_istride;   // The size (in bytes) of a single I-element
		D3DPtr<ID3D12Resource>   m_vb;        // The vertex buffer
		D3DPtr<ID3D12Resource>   m_ib;        // The index buffer
		D3D12_VERTEX_BUFFER_VIEW m_vb_view;   // Buffer views for shader binding
		D3D12_INDEX_BUFFER_VIEW  m_ib_view;   // Buffer views for shader binding
		TNuggetChain             m_nuggets;   // The nuggets for this model
		BBox                     m_bbox;      // A bounding box for the model. Set by the client
		string32                 m_name;      // A human readable name for the model
		mutable EDbgFlags        m_dbg_flags; // Flags used by PR_DBG_RDR to output info once only

		Model(ResourceManager& mgr, size_t vcount, size_t icount, int vstride, int istride, ID3D12Resource* vb, ID3D12Resource* ib, BBox const& bbox, char const* name);
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
		void CreateNugget(NuggetData const& props, RdrId id = 0);

		// Call to release the nuggets that this model has been
		// divided into. Nuggets are the contiguous sub groups
		// of the model geometry that use the same data.
		void DeleteNuggets();

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<Model>* doomed);
	};
}
