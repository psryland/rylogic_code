//***********************************************************************************
//
// RenderableBase - A base class for geometry used by the renderer
//	This header is exposed to client code
// 
//***********************************************************************************
// Base class for all renderable objects.
// Notes:
//	Use "Rdr::Instance" to draw instances of Renderables
//	Do not support heirarchy in Renderables, if you want general heirarchy
//	write a layer to wrap a renderable, m_model_to_root can be used for this
//
#ifndef RENDERABLEBASE_H
#define RENDERABLEBASE_H

#include "PR/Geometry/PRGeometry.h"
#include "PR/Renderer/RenderState.h"
#include "PR/Renderer/RenderNugget.h"
#include "PR/Renderer/Materials/MaterialMap.h"
#include "PR/Renderer/VertexFormat.h"
#include "PR/Renderer/Models/RenderableParams.h"
#include "PR/Renderer/Forward.h"

namespace pr
{
	namespace rdr
	{
        typedef pr::Index Index;

		enum ERenderableType
		{
			ERenderableType_Rendererable,
			ERenderableType_NumberOf
		};

		// Renderable Base class
		class RenderableBase
		{
		public:
			RenderableBase();
			virtual ~RenderableBase() {}

			// Overrides
			virtual ERenderableType	Type() const = 0;
			virtual void			Release() = 0;

			// Access to the vertex/index/attribute buffer
			virtual Index*			LockIBuffer(uint offset = 0, uint num_to_lock = 0, uint flags = 0) = 0;	// 0 = All
			virtual vf::Iter		LockVBuffer(uint offset = 0, uint num_to_lock = 0, uint flags = 0) = 0;	// 0 = All
			virtual Attribute*		LockABuffer(uint offset = 0, uint num_to_lock = 0, uint flags = 0) = 0;	// 0 = All
			virtual void			UnlockIBuffer() = 0;
			virtual void			UnlockVBuffer() = 0;
			virtual void			UnlockABuffer() = 0;
			
			// Creation methods
			virtual bool			Create(const RenderableParams& params) = 0;
			virtual bool			Create(RenderableParams params, const Geometry& geometry, uint frame_number) = 0;

			// Operations
			void	SetRenderState(D3DRENDERSTATETYPE type, uint state)		{ m_render_state.SetRenderState(type, state); }
			void	ClearRenderState(D3DRENDERSTATETYPE type)				{ m_render_state.ClearRenderState(type); }
			void	GenerateRenderNuggets();
			void	GenerateNormals();
			
			// Public members
			m4x4*							m_model_to_root;			// Transform from this model to an instance position. 0 means identity
			m4x4*							m_camera_to_screen;			// Projection transform for this model. 0 means use the viewport one
			D3DPRIMITIVETYPE				m_primitive_type;			// The type of primitives in the renderable
			int								m_indices_per_primitive;	// The number of indices needed to make a primitive
			MaterialMap						m_material_map;				// Maps material indices in the attribute buffer to Material's
			uint							m_num_indices;				// The length of the index buffer
			uint							m_num_vertices;				// The length of the vertex buffer
			uint							m_num_attribs;				// The length of the attribute buffer
			vf::Type						m_vertex_type;				// The vertex type for this renderable
			D3DPtr<IDirect3DIndexBuffer9>	m_index_buffer;				// The index buffer containing the indices of this model
			D3DPtr<IDirect3DVertexBuffer9>	m_vertex_buffer;			// The vertex buffer containing the vertices of this model
			Attribute*						m_attribute_buffer;			// The attributes of each face
			uint							m_render_bin;				// The render bin that this renderable is in
			RenderStateBlock				m_render_state;				// Render states for the model
			TNuggetList						m_render_nugget;			// The atomic renderable elements of this model
			std::string						m_name;						// A human readable name for the model

		protected:
			void	SetPrimitiveType(D3DPRIMITIVETYPE type);
			bool	LoadGeometry(Renderer& renderer, const Mesh& mesh);
		};
	}//namespace rdr
}//namespace pr

#endif//RENDERABLEBASE_H
