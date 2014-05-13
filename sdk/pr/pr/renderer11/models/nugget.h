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
#include "pr/renderer11/shaders/shader_set.h"

namespace pr
{
	namespace rdr
	{
		// Notes:
		// Nuggets can only reference the model buffer, not the model, because if they contained
		// ModelPtr's that could mean models contain nuggets which contain references to them-
		// selves, meaning the reference count will not automatically clean up the model.
		//
		// Nuggets may be referenced in the drawlists of several render steps.
		// This means they shouldn't contain shader specific data (e.g. why should all nuggets have a 'line width'?)
		// There is some data that is model specific and used by multiple shaders (e.g. topo, geom type, diffuse texture)
		// We need an automatic way of adding a model to an arbitrary renderstep, i.e. sensible defaults
		// Render steps have a shader they want to use but require extra data like geom type, textures, line width, etc
		// We don't want to have to resolve the shader per frame
		// ShaderBase derived objects are instances of dx shaders. We can have Shaders that contain per-nugget data (line width, projection texture, etc)
		//
		// A nugget contains a collection of ShaderPtrs as well as model specific data. There might be none, or multiple VSs,
		// PSs, etc in the collection. When a nugget is added to a render step, it ensures that there are appropriate shaders
		// in the collection for it to be rendererd by that render step. If they're missing it adds them.

		// Nugget construction data
		struct NuggetProps
		{
			EPrim          m_topo;        // The primitive topology for this nugget
			EGeom          m_geom;        // The valid geometry components within this range
			ShaderSet      m_sset;        // The shaders to use (optional, some render steps use their own shaders)
			Texture2DPtr   m_tex_diffuse; // Diffuse texture
			BSBlock        m_bsb;         // Rendering states
			DSBlock        m_dsb;         // Rendering states
			RSBlock        m_rsb;         // Rendering states

			NuggetProps(EPrim topo = EPrim::Invalid, EGeom geom = EGeom::Invalid, ShaderSet* sset = nullptr)
				:m_topo(topo)
				,m_geom(geom)
				,m_sset(sset ? *sset : ShaderSet())
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
