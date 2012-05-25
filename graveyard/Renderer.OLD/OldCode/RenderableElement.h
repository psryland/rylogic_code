//***********************************************************************************
//
// RenderableElement - Small geometry that will be modified by client code
//	This objects are copied to a larger buffer in the renderer
// 
//***********************************************************************************

#ifndef RENDERABLEELEMENT_H
#define RENDERABLEELEMENT_H

#include "PR/Common/StdList.h"
#include "PR/Renderer/RenderNugget.h"
#include "PR/Renderer/RenderableBase.h"
#include "PR/Renderer/Attribute.h"
#include "PR/Renderer/RenderableParams.h"
#include "PR/Renderer/Forward.h"

namespace pr
{
	namespace rdr
	{
		//*****
		// This type of renderable is used for dynamic vertex/index data.
		// It contains a vertex and index buffer that is memcopyed into
		// dynamic vertex and index buffers within a viewport for each frame. 
		class RenderableElement : public RenderableBase
		{
		public:
			RenderableElement();
			~RenderableElement();

			// Overrides
			ERenderableType	Type() const { return ERenderableType_RendererableElement; }
			void			Release();

			// Access to the vertex/index/attribute buffer
			Index*			LockIBuffer(DWORD offset = 0, DWORD = 0, DWORD = 0) { return m_element_ibuffer + offset; }
			vf::Iter		LockVBuffer(DWORD offset = 0, DWORD = 0, DWORD = 0) { return vf::Iter(m_element_vbuffer + vf::GetSize(m_vertex_type) * offset, m_vertex_type); }
			Attribute*		LockABuffer(DWORD offset = 0, DWORD = 0, DWORD = 0) { return m_attribute_buffer + offset; }
			void			UnlockIBuffer() {}
			void			UnlockVBuffer() {}
			void			UnlockABuffer() {}

			// Creation
			bool			Create(const RenderableParams& params);
			bool			Create(RenderableParams params, const Geometry& geometry, uint frame_number);

			// The model data
			Index*		m_element_ibuffer;		// The indices that describe the faces in this model
			char*		m_element_vbuffer;		// The vertices of the model
		};

		//********************************************************
		// Inline Implementation
		//*****
		// Constructor
		inline RenderableElement::RenderableElement()
		:m_element_ibuffer(0)
		,m_element_vbuffer(0)
		{}

		//*****
		// Destructor
		inline RenderableElement::~RenderableElement()
		{
			Release();
		}
	}//namespace rdr
}//namespace pr

#endif//RENDERABLEELEMENT_H
