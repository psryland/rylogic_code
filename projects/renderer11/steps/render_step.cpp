//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/steps/render_step.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/window.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/instances/instance.h"
#include "renderer11/util/forward_private.h"
#include "renderer11/render/state_stack.h"

namespace pr
{
	namespace rdr
	{
		RenderStep::RenderStep(Scene& scene)
			:m_scene(&scene)
			,m_shdr_mgr(&scene.m_wnd->shdr_mgr())
			,m_drawlist(scene.m_wnd->m_rdr->Allocator<DrawListElement>())
			,m_sort_needed(true)
			,m_bsb()
			,m_rsb()
			,m_dsb()
		{}

		// Reset/Populate the drawlist
		void RenderStep::ClearDrawlist()
		{
			m_drawlist.resize(0);
		}

		// Sort the drawlist based on sortkey
		void RenderStep::Sort()
		{
			std::sort(std::begin(m_drawlist), std::end(m_drawlist));
			m_sort_needed = false;
		}
		void RenderStep::SortIfNeeded()
		{
			if (!m_sort_needed) return;
			Sort();
		}

		// Add an instance. The instance, model, and nuggets must be resident for the entire time
		// that the instance is in the drawlist, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
		void RenderStep::AddInstance(BaseInstance const& inst)
		{
			// Get the model associated with the isntance
			ModelPtr const& model = GetModel(inst);
			PR_ASSERT(PR_DBG_RDR, model != nullptr, "Null model pointer");

			// Get the nuggets for this render step
			auto& nuggets = model->m_nuggets;
			#if PR_DBG_RDR
			if (nuggets.empty() && !AllSet(model->m_dbg_flags, EDbgRdrFlags::WarnedNoRenderNuggets))
			{
				PR_INFO(PR_DBG_RDR, FmtS("This model ('%s') has no nuggets, you need to call CreateNugget() on the model first\n", model->m_name.c_str()));
				model->m_dbg_flags = SetBits(model->m_dbg_flags, EDbgRdrFlags::WarnedNoRenderNuggets, true);
			}
			#endif

			// Check the instance transform is valid
			PR_ASSERT(PR_DBG_RDR, FEql(GetO2W(inst).w.w, 1.0f), "Invalid instance transform");

			// Add to the derived objects drawlist
			AddNuggets(inst, nuggets);
		}

		// Remove an instance from the scene
		void RenderStep::RemoveInstance(BaseInstance const& inst)
		{
			auto new_end = std::remove_if(std::begin(m_drawlist), std::end(m_drawlist), [&](DrawListElement const& dle){ return dle.m_instance == &inst; });
			m_drawlist.resize(new_end - std::begin(m_drawlist));
		}

		// Remove a batch of instances. Optimised by a single past through the drawlist
		void RenderStep::RemoveInstances(BaseInstance const** inst, std::size_t count)
		{
			// Make a sorted list from the batch to remove
			BaseInstance const** doomed = PR_ALLOCA_POD(BaseInstance const* , count);
			BaseInstance const** doomed_end = doomed + count;
			std::copy(inst, inst + count, doomed);
			std::sort(doomed, doomed_end);

			// Remove instances
			auto new_end = std::remove_if(std::begin(m_drawlist), std::end(m_drawlist), [&](DrawListElement const& dle)
			{
				auto iter = std::lower_bound(doomed, doomed_end, dle.m_instance);
				return iter != doomed_end && *iter == dle.m_instance;
			});
			m_drawlist.resize(new_end - std::begin(m_drawlist));
		}

		// Perform the render step
		void RenderStep::Execute(StateStack& ss)
		{
			// Notify that this render step is about to execute
			pr::events::Send(Evt_RenderStepExecute(*this, false));

			{
				PR_EXPAND(PR_DBG_RDR, auto dbg = pr::CreateScope(
					[&]{ ss.m_dbg->BeginEvent(ERenderStep::ToStringW(GetId())); },
					[&]{ ss.m_dbg->EndEvent(); }));

				// Commit before the start of a render step to ensure changes
				// are flushed before the render steps tries to clear back buffers, etc
				StateStack::RSFrame frame(ss, *this);
				ss.Commit();

				ExecuteInternal(ss);
			}

			// Notify that the render step has finished
			pr::events::Send(Evt_RenderStepExecute(*this, false));
		}
	}
}

