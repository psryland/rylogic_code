//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/render/state_block.h"
#include "pr/view3d/render/drawlist_element.h"
#include "pr/view3d/util/stock_resources.h"

namespace pr
{
	namespace rdr
	{
		// EnableIf for RenderStep derived types
		template <typename T> using enable_if_render_step = typename std::enable_if<std::is_base_of<RenderStep,T>::value>::type;

		// Base class for render steps
		struct RenderStep :AlignTo<16>
		{
			// Draw list element container
			using TDrawList = pr::vector<DrawListElement, 1024, false, pr::rdr::Allocator<DrawListElement>>;

			// A lock context for the drawlist
			struct Lock :threads::Synchronise<RenderStep, std::recursive_mutex>
			{
				Lock(RenderStep const& rs)
					:base(rs, rs.m_mutex)
				{}

				TDrawList const& drawlist() const { return get().m_impl_drawlist; }
				TDrawList&       drawlist()       { return get().m_impl_drawlist; }
			};

			Scene*                       m_scene;            // The scene this render step is owned by
			ShaderManager*               m_shdr_mgr;         // Convenience pointer to the shader manager
			TDrawList                    m_impl_drawlist;    // The drawlist for this render step. Access via 'Lock'
			bool                         m_sort_needed;      // True when the list needs sorting
			BSBlock                      m_bsb;              // Blend states
			RSBlock                      m_rsb;              // Raster states
			DSBlock                      m_dsb;              // Depth buffer states
			AutoSub                      m_evt_model_delete; // Event subscription for model deleted notification
			std::recursive_mutex mutable m_mutex;            // Sync access to the drawlist

			explicit RenderStep(Scene& scene);
			virtual ~RenderStep() {}
			RenderStep(RenderStep const&) = delete;
			RenderStep& operator = (RenderStep const&) = delete;

			// The type of render step this is
			virtual ERenderStep GetId() const = 0;
			template <typename RStep, typename = enable_if_render_step<RStep>> RStep const& as() const { return *static_cast<RStep const*>(this); }
			template <typename RStep, typename = enable_if_render_step<RStep>> RStep        as()       { return *static_cast<RStep*>(this); }

			// Update the provided shader set appropriate for this render step
			virtual void ConfigShaders(ShaderSet1& ss, EPrim topo) const = 0;

			// Reset the drawlist
			void ClearDrawlist();

			// Sort the drawlist based on sort key
			void Sort();
			void SortIfNeeded();

			// Add an instance. The instance,model,and nuggets must be resident for the entire time
			// that it is in the drawlist, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
			void AddInstance(BaseInstance const& inst);
			template <typename Inst> void AddInstance(Inst const& inst)
			{
				AddInstance(inst.m_base);
			}

			// Remove an instance from the scene
			void RemoveInstance(BaseInstance const& inst);
			template <typename Inst> void RemoveInstance(Inst const& inst)
			{
				RemoveInstance(inst.m_base);
			}

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
			virtual void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets) = 0;

			// Derived render steps perform their action
			virtual void ExecuteInternal(StateStack& ss) = 0;

		private:

			// Notification of a model being destroyed
			void OnModelDeleted(Model& model, EmptyArgs const&) const;
		};
	}
}
