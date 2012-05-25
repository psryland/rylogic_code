//***********************************************************************************
//
// Renderable
// 
//***********************************************************************************
//
// This type of renderable is used for static or dynamic geometry.
//
#ifndef RENDERABLE_H
#define RENDERABLE_H

#include "PR/Geometry/PRGeometry.h"
#include "PR/Renderer/Errors.h"
#include "PR/Renderer/RenderNugget.h"
#include "PR/Renderer/RenderState.h"
#include "PR/Renderer/Models/RenderableBase.h"
#include "PR/Renderer/Attribute.h"
#include "PR/Renderer/Models/RenderableParams.h"
#include "PR/Renderer/Forward.h"

namespace pr
{
	namespace rdr
	{
		class Renderable : public RenderableBase
		{
		public:
			Renderable();
			~Renderable();
			
			// Overrides
			ERenderableType	Type() const	{ return ERenderableType_Rendererable; }
			void			Release();

			// Access tothe vertex/index/attribute
			Index*			LockIBuffer(uint offset = 0, uint num_to_lock = 0, uint flags = 0);
			vf::Iter		LockVBuffer(uint offset = 0, uint num_to_lock = 0, uint flags = 0);
			Attribute*		LockABuffer(uint offset = 0, uint num_to_lock = 0, uint flags = 0);
			void			UnlockIBuffer() { if( m_index_buffer  ) m_index_buffer->Unlock(); }
			void			UnlockVBuffer() { if( m_vertex_buffer ) m_vertex_buffer->Unlock(); }
			void			UnlockABuffer() {}

			// Creation
			bool			Create(const RenderableParams& params);
			bool			Create(RenderableParams params, const Geometry& geometry, uint frame_number);
		};

		//********************************************************
		// Inline Implementation
		//*****
		// Constructor
		inline Renderable::Renderable()
		{}

		//*****
		// Destructor
		inline Renderable::~Renderable()
		{
			Release();
		}

		//*****
		// Get a pointer to the index buffer. Remember to call unlock afterwards
		inline Index* Renderable::LockIBuffer(uint offset, uint num_to_lock, uint flags)
		{
			Index* ibuffer = 0;
			if( m_index_buffer ) Verify(m_index_buffer->Lock(offset, num_to_lock * sizeof(Index), (void**)&ibuffer, flags));
			return ibuffer;
		}

		//*****
		// Get a pointer to the vertex buffer. Remember to call unlock afterwards
		inline vf::Iter Renderable::LockVBuffer(uint offset, uint num_to_lock, uint flags)
		{
			void* vbuffer = 0;
			if( m_vertex_buffer ) Verify(m_vertex_buffer->Lock(offset, num_to_lock * vf::GetSize(m_vertex_type), &vbuffer, flags));
			return vf::Iter(vbuffer, m_vertex_type);
		}

		//*****
		// Get a pointer to the attribute buffer.
		inline Attribute* Renderable::LockABuffer(uint offset, uint, uint)
		{
			return m_attribute_buffer + offset;
		}

	}//namespace rdr
}//namespace pr
#endif//RENDERABLE_H
