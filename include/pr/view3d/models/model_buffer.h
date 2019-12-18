//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/util/lock.h"

namespace pr::rdr
{
	// A container of buffers for one or more models
	struct ModelBuffer :pr::RefCount<ModelBuffer>
	{
		struct VBuf :D3DPtr<ID3D11Buffer>
		{
			pr::rdr::Range m_range;  // The maximum number of vertices in this buffer
			pr::rdr::Range m_used;   // The number of vertices used in the vertex buffer
			UINT           m_stride; // The size of each element in the buffer
		};
		struct IBuf :D3DPtr<ID3D11Buffer>
		{
			pr::rdr::Range m_range;  // The maximum number of vertices in this buffer
			pr::rdr::Range m_used;   // The number of vertices used in the vertex buffer
			DXGI_FORMAT    m_format; // The DXGI_FORMAT of the elements in the buffer
		};
		VBuf          m_vb;      // The vertex buffer
		IBuf          m_ib;      // The index buffer
		ModelManager* m_mdl_mgr; // The model manager that created this model buffer

		ModelBuffer();
		ModelBuffer(const ModelBuffer&) = delete;
		ModelBuffer& operator =(const ModelBuffer&) = delete;

		// Renderer access
		Renderer& rdr() const;
		ModelManager& mdl_mgr() const;

		// Returns true if 'settings' describe a model format that is compatible with this model buffer
		bool IsCompatible(MdlSettings const& settings) const;

		// Returns true if there is enough free space in this model for 'vcount' verts and 'icount' indices
		bool IsRoomFor(size_t vcount, size_t icount) const;

		// Reserve 'vcount' verts from this model
		Range ReserveVerts(size_t vcount);

		// Reserve 'icount' indices from this model
		Range ReserveIndices(size_t icount);

		// Access to the vertex/index buffers
		// Only returns false if 'D3D11_MAP_FLAG_DO_NOT_WAIT' flag is set, all other fail cases throw
		bool MapVerts  (Lock& lock, EMap map_type = EMap::Write, EMapFlags flags = EMapFlags::None, Range vrange = RangeZero);
		bool MapIndices(Lock& lock, EMap map_type = EMap::Write, EMapFlags flags = EMapFlags::None, Range irange = RangeZero);

		// Ref-counting clean up function
		static void RefCountZero(pr::RefCount<ModelBuffer>* doomed);
	};
}
