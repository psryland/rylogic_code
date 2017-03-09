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
#include "pr/renderer11/util/event_types.h"

namespace pr
{
	namespace rdr
	{
		// Base class for render steps
		struct RenderStep
			:pr::events::IRecv<Evt_ModelDestroy>
		{
			// Draw list element container
			using TDrawList = pr::vector<DrawListElement, 1024, false, pr::rdr::Allocator<DrawListElement>>;

			// A lock context for the drawlist
			class Lock
			{
				RenderStep& m_rs;
				std::lock_guard<std::recursive_mutex> m_lock;
			
			public:

				Lock(RenderStep& rs) :m_rs(rs) ,m_lock(rs.m_mutex) {}
				Lock(Lock const&) = delete;
				Lock& operator =(Lock const&) = delete;

				TDrawList const& drawlist() const
				{
					return m_rs.m_impl_drawlist;
				}
				TDrawList& drawlist()
				{
					return m_rs.m_impl_drawlist;
				}
			};

			Scene*               m_scene;         // The scene this render step is owned by
			ShaderManager*       m_shdr_mgr;      // Convenience pointer to the shader manager
			TDrawList            m_impl_drawlist; // The drawlist for this render step. Access via 'Lock'
			bool                 m_sort_needed;   // True when the list needs sorting
			BSBlock              m_bsb;           // Blend states
			RSBlock              m_rsb;           // Raster states
			DSBlock              m_dsb;           // Depth buffer states
			std::recursive_mutex m_mutex;         // Sync access to the drawlist

			RenderStep(Scene& scene);
			virtual ~RenderStep() {}
			RenderStep(RenderStep const&) = delete;
			RenderStep& operator = (RenderStep const&) = delete;

			// The type of render step this is
			virtual ERenderStep::Enum_ GetId() const = 0;
			template <typename RStep> typename std::enable_if<std::is_base_of<RenderStep,RStep>::value, RStep>::type const& as() const { return *static_cast<RStep const*>(this); }
			template <typename RStep> typename std::enable_if<std::is_base_of<RenderStep,RStep>::value, RStep>::type&       as()       { return *static_cast<RStep*>(this); }

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
			virtual void AddNuggets(BaseInstance const& inst, TNuggetChain& nuggets) = 0;

			// Derived render steps perform their action
			virtual void ExecuteInternal(StateStack& ss) = 0;

		private:

			// Notification of a model being destroyed
			void OnEvent(Evt_ModelDestroy const& evt) override;
		};
	}
}
