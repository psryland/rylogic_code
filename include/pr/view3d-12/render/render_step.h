//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/render/drawlist_element.h"
#include "pr/view3d-12/shaders/shader.h"
#include "pr/view3d-12/utility/pipe_state.h"

namespace pr::rdr12
{
	// Base class for render steps
	struct RenderStep
	{
		// Draw list element container
		using drawlist_t = pr::vector<DrawListElement, 1024, false, alignof(DrawListElement), Allocator<DrawListElement>>;
		using dl_mutex_t = std::recursive_mutex;

		// A lock context for the drawlist
		struct Lock :threads::Synchronise<RenderStep, std::recursive_mutex>
		{
			Lock(RenderStep const& rs)
				:base(rs, rs.m_mutex)
			{}
			drawlist_t const& drawlist() const
			{
				return get().m_drawlist;
			}
			drawlist_t& drawlist()
			{
				return get().m_drawlist;
			}
		};

		ERenderStep const Id;
		Scene*                      m_scene;              // The scene this render step is owned by
		drawlist_t                  m_drawlist;           // The drawlist for this render step. Access via 'Lock'
		bool                        m_sort_needed;        // True when the list needs sorting
		GpuUploadBuffer             m_cbuf_upload;        // Shared upload buffer for shaders to used to upload parameters
		PipeStateDesc               m_default_pipe_state; // Default settings for the pipeline state
		PipeStatePool               m_pipe_state_pool;    // Pool of pipeline state objects
		AutoSub                     m_evt_model_delete;   // Event subscription for model deleted notification
		dl_mutex_t mutable          m_mutex;              // Sync access to the drawlist

		RenderStep(ERenderStep id, Scene& scene);
		RenderStep(RenderStep&&) = default;
		RenderStep(RenderStep const&) = delete;
		RenderStep& operator = (RenderStep&&) = default;
		RenderStep& operator = (RenderStep const&) = delete;
		virtual ~RenderStep() {}

		// Renderer access
		Renderer& rdr() const;
		Window& wnd() const;
		Scene& scn() const;

		// The type of render step this is
		template <RenderStepType RStep> RStep const& as() const
		{
			return *static_cast<RStep const*>(this);
		}
		template <RenderStepType RStep> RStep as()
		{
			return *static_cast<RStep*>(this);
		}

		// Reset the drawlist
		virtual void ClearDrawlist();

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
		void Execute(BackBuffer& bb, ID3D12GraphicsCommandList* cmd_list);

	protected:

		// Add model nuggets to the draw list for this render step.
		// The nuggets contain model specific data (such as diffuse texture) as well as
		// a collection of shader instances (each containing shader specific data such
		// as projection texture, line width, etc). This method needs to ensure the
		// nugget's shader collection contains the appropriate shaders.
		virtual void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets, drawlist_t& drawlist) = 0;

		// Derived render steps perform their action
		virtual void ExecuteInternal(BackBuffer& bb, ID3D12GraphicsCommandList* cmd_list) = 0;

	private:

		// Notification of a model being destroyed
		void OnModelDeleted(Model& model, EmptyArgs const&) const;
	};
}