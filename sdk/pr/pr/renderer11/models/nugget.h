//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/raster_state.h"
#include "pr/renderer11/render/depth_state.h"
#include "pr/renderer11/textures/texture2d.h"
#include "pr/renderer11/models/model_buffer.h"

namespace pr
{
	namespace rdr
	{
		// Notes:
		// Nuggets can only reference the model buffer, not the model, because if they contained
		// ModelPtr's that could mean models contain nuggets which contain references to them-
		// selves, meaning the reference count will not automatically clean up the model.

		// Nugget construction data
		struct NuggetProps
		{
			EPrim          m_topo;         // The primitive topology for this nugget
			EGeom          m_geom;         // The valid geometry components within this range
			BaseShader*    m_shader;       // The shader to use (optional, some render steps use their own shaders)
			Texture2DPtr   m_tex_diffuse;  // Base diffuse texture
			BSBlock        m_bsb;          // Rendering states
			DSBlock        m_dsb;          // Rendering states
			RSBlock        m_rsb;          // Rendering states

			NuggetProps(EPrim topo = EPrim::Invalid, EGeom geom = EGeom::Invalid, BaseShader* shader = nullptr)
				:m_topo(topo)
				,m_geom(geom)
				,m_shader(shader)
				,m_tex_diffuse()
				,m_bsb()
				,m_dsb()
				,m_rsb()
			{}
		};

		// A nugget is a sub range within a model buffer containing any data needed to render
		// that sub range. Not all data is necessarily needed to render each nugget (depends on
		// the shader that the render step uses), but each nugget can be rendered with a single
		// DrawIndexed call for any possible shader.
		struct Nugget :pr::chain::link<Nugget, ChainGroupNugget> ,NuggetProps
		{
			ModelBufferPtr m_model_buffer; // The vertex and index buffers.
			Range          m_vrange;       // The index offset into the vertex buffer and the number of vertices for this nugget (relative to model buffer, not model)
			Range          m_irange;       // The index offset into the index buffer and the number of indices for this nugget (relative to model buffer, not model)
			size_t         m_prim_count;   // The number of primitives in this nugget
			SortKey        m_sort_key;     // A cached sort key derived from this nugget
			Model*         m_owner;        // The model that this nugget belongs to (for debugging mainly)

			Nugget(NuggetProps const& props)
				:NuggetProps(props)
				,m_model_buffer()
				,m_vrange()
				,m_irange()
				,m_prim_count()
				,m_sort_key()
				,m_owner()
			{}
		};
	}
}
