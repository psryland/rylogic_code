//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

// An object that wraps a vertex buffer and an index buffer

#pragma once
#ifndef PR_RDR_MODEL_BUFFER_H
#define PR_RDR_MODEL_BUFFER_H

#include "pr/renderer/models/types.h"
#include "pr/renderer/vertexformats/vertexformat.h"

namespace pr
{
	namespace rdr
	{
		struct ModelBuffer :pr::RefCount<ModelBuffer>
		{
			vf::Type                        m_vertex_type;  // Vertex format of vbuffer
			D3DPtr<IDirect3DVertexBuffer9>  m_Vbuffer;      // D3D vertex buffer
			D3DPtr<IDirect3DIndexBuffer9>   m_Ibuffer;      // D3D index buffer
			ModelManager*                   m_mdl_mgr;      // The model manager that created this model buffer
			model::Range                    m_Vrange;       // The maximum number of vertices in this buffer
			model::Range                    m_Irange;       // The maximum number of indices in this buffer
			model::Range                    m_Vused;        // The number of vertices used in the vertex buffer
			model::Range                    m_Iused;        // The number of indices used in the index buffer
			
			ModelBuffer();
			
			// Access to the vertex/index buffers. Unlock occurs when 'lock' goes out of scope
			vf::iterator LockVBuffer(model::VLock& lock, model::Range v_range = model::RangeZero, std::size_t flags = 0);
			Index*       LockIBuffer(model::ILock& lock, model::Range i_range = model::RangeZero, std::size_t flags = 0);
			
			bool         IsCompatible(model::Settings const& settings) const;
			bool         IsRoomFor(std::size_t Vcount, std::size_t Icount) const;
			vf::Type     GetVertexType() const { return m_vertex_type; }
			
			model::Range AllocateVertices(std::size_t Vcount);
			model::Range AllocateIndices(std::size_t Icount);
			
			// Refcounting cleanup function
			static void RefCountZero(pr::RefCount<ModelBuffer>* doomed);
		};
	}
}

#endif
