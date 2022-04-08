//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
//#include "pr/view3d/render/state_block.h"
#include "pr/view3d-12/render/sortkey.h"
//#include "pr/view3d/render/drawlist_element.h"
//#include "pr/view3d/shaders/shader_set.h"
//#include "pr/view3d/models/model_buffer.h"
#include "pr/view3d-12/texture/texture_2d.h"
//#include "pr/view3d/textures/texture_cube.h"

namespace pr::rdr12
{
	// Notes:
	//  - Shader/Nugget Requirements:
	//    There is some data that is model specific and used by multiple shaders (e.g. topo, geom type, diffuse texture),
	//     these data might as well be in the nuggets to prevent duplication in each shader.
	//    Usability requires that we can add a model (i.e. a collection of nuggets) to any/all render steps automatically.
	//    Normally, render steps have a shader they want to use but sometimes we need to override the shader a render step uses.
	//    We don't want to have to resolve the shaders per frame
	//
	//  - Render Steps:
	//    Nuggets may be referenced in the drawlists of several render steps. i.e. each render step has
	//    its own drawlist, so the same nugget can be pointed to from multiple drawlists.
	//    This leads to the conclusion that a nugget shouldn't contain shader specific data (e.g. why should all nuggets have a
	//    variable only used in one shader from one render step? This wouldn't scale as more shaders/render steps are added)
	//    Shader derived objects are light weight instances of dx shaders. These shader instances contain per-nugget data
	//    (such as line width, projection texture, etc). They can be duplicated as needed.
	//    
	//    Drawlist Sorting and sort keys:
	//    Since there is a drawlist per render step, each nugget needs a sort key per drawlist. These are composed on demand
	//    when the nuggets are added to the render steps:
	//     - nugget sort key has sort group, alpha, and diff texture id set
	//     - per render step (aka drawlist)
	//       - hash the sort ids of all shaders together into a shader id and set that in the sort key
	//       - apply sort key overrides from the owning instance (these are needed because the instance might tint with alpha)
	//
	//  - ShaderMap:
	//    A nugget contains a collection of ShaderPtrs as well as model specific data. The shader map contains the pointers
	//    to the shaders to be used by each render step. Users can set these pointers as needed for specific functionally or
	//    leave them as null. When a nugget is added to a render step, the render step ensures that there are appropriate
	//    shaders in the shader map for it to be rendered by that render step. If they're missing it adds them.
	//
	//  - ModelBufferPtr:
	//    Nuggets can only reference the model buffer, not the model, because if they contained
	//    ModelPtr's that could mean models contain nuggets which contain references to them-
	//    selves, meaning the reference count will not automatically clean up the model.

	// Flags for nuggets
	enum class ENuggetFlag :int
	{
		None = 0,

		// Exclude this nugget when rendering a model
		Hidden = 1 << 0,

		// Set if the geometry data for the nugget contains alpha colours
		GeometryHasAlpha = 1 << 1,

		// Set if the tint colour contains alpha
		TintHasAlpha = 1 << 2,

		// Excluded from shadow map render steps
		ShadowCastExclude = 1 << 3,

		// Can overlap with other nuggets.
		// Set this flag to true if you want to add a nugget that overlaps the range
		// of an existing nugget. For simple models, overlapping nugget ranges is
		// usually an error, but in advanced cases it isn't.
		RangesCanOverlap = 1 << 4,

		_flags_enum,
	};

	// Nugget data. Common base for NuggetProps and Nugget
	struct NuggetData
	{
		ETopo          m_topo;                  // The primitive topology for this nugget
		EGeom          m_geom;                  // The valid geometry components within this range
		//ShaderMap      m_smap;                  // The shaders to use (optional, some render steps use their own shaders)
		Texture2DPtr   m_tex_diffuse;           // Diffuse texture
		Colour32       m_tint;                  // Per-nugget tint
		//BSBlock        m_bsb;                   // Rendering states
		//DSBlock        m_dsb;                   // Rendering states
		//RSBlock        m_rsb;                   // Rendering states
		SortKey        m_sort_key;              // A base sort key for this nugget
		float          m_relative_reflectivity; // How reflective this nugget is, relative to the instance. Note: 1.0 means the same as the instance (which might be 0)
		ENuggetFlag    m_nflags;                // Flags for boolean properties of the nugget

		// When passed in to Model->CreateNugget(), these ranges should be relative to the model.
		// If the ranges are zero length, they are assume to mean the entire model
		Range m_vrange;
		Range m_irange;

		NuggetData(ETopo topo = ETopo::Invalid, EGeom geom = EGeom::Invalid, ShaderMap* smap = nullptr, Range vrange = Range(), Range irange = Range());
	};

	// A nugget is a sub range within a model buffer containing any data needed to render
	// that sub range. Not all data is necessarily needed to render each nugget (depends on
	// the shader that the render step uses), but each nugget can be rendered with a single
	// DrawIndexed call for any possible shader.
	struct Nugget :pr::chain::link<Nugget, ChainGroupNugget>, NuggetData
	{
		static constexpr RdrId AlphaNuggetId = hash::HashCT("AlphaNugget");
		
		Model*       m_model;         // The model that owns this nugget.
		size_t       m_prim_count;    // The number of primitives in this nugget.
		TNuggetChain m_nuggets;       // The dependent nuggets associated with this nugget.
		EFillMode    m_fill_mode;     // Fill mode for this nugget
		ECullMode    m_cull_mode;     // Cull mode for this nugget
		bool         m_alpha_enabled; // Alpha blending is enabled for this nugget
		RdrId        m_id;            // An id to allow identification of procedurally added nuggets

		Nugget(NuggetData const& ndata, Model* model);
		~Nugget();

		// Renderer access
		Renderer& rdr() const;
		ResourceManager& res_mgr() const;

		// True if this nugget requires alpha blending
		bool RequiresAlpha() const;
		void UpdateAlphaStates();

		// Get/Set the fill mode for this nugget
		EFillMode FillMode() const;
		void FillMode(EFillMode fill_mode);

		// Get/Set the cull mode for this nugget
		ECullMode CullMode() const;
		void CullMode(ECullMode fill_mode);

		// True if this nugget should be rendered
		bool Visible() const;

		// Delete any dependent nuggets based on 'pred'
		template <typename Pred>
		void DeleteDependent(Pred pred)
		{
			auto nuggets = chain::filter(m_nuggets, pred);
			for (;!nuggets.empty();)
				nuggets.front().Delete();
		}

		// Delete this nugget, removing it from the owning model
		void Delete();

	private:

		// Enable/Disable alpha for this nugget.
		// Alpha can be enabled or disabled independently to the geometry colours or diffuse texture colour.
		// When setting 'Alpha(enable)' be sure to consider all sources of alpha.
		void Alpha(bool enable);
	};
}
