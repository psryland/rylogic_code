//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/raster_state.h"
#include "pr/renderer11/render/depth_state.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/util/stock_resources.h"

namespace pr
{
	namespace rdr
	{
		// Base class for render steps
		struct RenderStep
		{
			typedef pr::Array<DrawListElement, 1024, false, pr::rdr::Allocator<DrawListElement>> TDrawList;

			Scene*         m_scene;       // The scene this render step is owned by
			ShaderManager* m_shdr_mgr;    // Convenience pointer to the shader manager
			TDrawList      m_drawlist;    // The drawlist for this render step
			bool           m_sort_needed; // True when the list needs sorting
			BSBlock        m_bsb;         // Blend states
			RSBlock        m_rsb;         // Raster states
			DSBlock        m_dsb;         // Depth buffer states

			RenderStep(Scene& scene);
			virtual ~RenderStep() {}

			// The type of render step this is
			virtual ERenderStep::Enum_ GetId() const = 0;
			template <typename RStep> typename std::enable_if<std::is_base_of<RenderStep,RStep>::value, RStep>::type const& as() const { return *static_cast<RStep const*>(this); }
			template <typename RStep> typename std::enable_if<std::is_base_of<RenderStep,RStep>::value, RStep>::type&       as()       { return *static_cast<RStep*>(this); }

			// Reset the drawlist
			void ClearDrawlist();

			// Sort the drawlist based on sortkey
			void Sort();
			void SortIfNeeded();

			// Add an instance. The instance,model,and nuggets must be resident for the entire time
			// that it is in the drawlist, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
			void AddInstance(BaseInstance const& inst);
			template <typename Inst> void AddInstance(Inst const& inst) { AddInstance(inst.m_base); }

			// Remove an instance from the scene
			void RemoveInstance(BaseInstance const& inst);
			template <typename Inst> void RemoveInstance(Inst const& inst) { RemoveInstance(inst.m_base); }

			// Remove a batch of instances. Optimised by a single past through the drawlist
			void RemoveInstances(BaseInstance const** inst, std::size_t count);

			// Perform the render step
			void Execute(StateStack& ss);

		protected:

			// Add model nuggets to the draw list for this render step
			// The nuggets contain model specific data (such as diffuse texture) as well as
			// a collection of shader instances (each containing shader specific data such
			// as projection texture, line width, etc). This method needs to ensure the
			// nugget's shader collection contains the appropriate shaders.
			virtual void AddNuggets(BaseInstance const& inst, TNuggetChain& nuggets) = 0;

			// Dervied render steps perform their action
			virtual void ExecuteInternal(StateStack& ss) = 0;

		private:
			RenderStep(RenderStep const&);
			RenderStep& operator = (RenderStep const&);
		};
	}
}
