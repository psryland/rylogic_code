//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/render/render_step.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/instance/instance.h"
#include "pr/view3d-12/render/drawlist_element.h"

namespace pr::rdr12
{
	RenderStep::RenderStep(Scene& scene)
		: m_scene(&scene)
		, m_drawlist()
		, m_sort_needed(true)
		,m_shader_sig()
		,m_pso()
		//, m_evt_model_delete(scene.wnd().mdl_mgr().ModelDeleted += std::bind(&RenderStep::OnModelDeleted, this, _1, _2))
		,m_mutex()
	{}

	// Access the renderer
	Renderer& RenderStep::rdr() const
	{
		return wnd().rdr();
	}
	Window& RenderStep::wnd() const
	{
		return scn().wnd();
	}
	Scene& RenderStep::scn() const
	{
		return *m_scene;
	}

	// Reset/Populate the drawlist
	void RenderStep::ClearDrawlist()
	{
		Lock lock(*this);
		lock.drawlist().resize(0);
	}

	// Sort the drawlist based on sort key
	void RenderStep::Sort()
	{
		Lock lock(*this);
		auto& dl = lock.drawlist();

		// Sort by sort key
		pr::sort(dl);

		// Sorting done
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
		// Get the model associated with the instance
		auto const& model = GetModel(inst);
		if (model == nullptr)
			throw std::runtime_error("Null model pointer");

		// Get the nuggets for this render step
		auto& nuggets = model->m_nuggets;

		// Debug checks
		#if PR_DBG_RDR
		{
			if (nuggets.empty() && !AllSet(model->m_dbg_flags, Model::EDbgFlags::WarnedNoRenderNuggets))
			{
				PR_INFO(PR_DBG_RDR, FmtS("This model (%s) has no nuggets, you need to call CreateNugget() on the model first\n", model->m_name.c_str()));
				model->m_dbg_flags = SetBits(model->m_dbg_flags, Model::EDbgFlags::WarnedNoRenderNuggets, true);
			}

			// Check the instance transform is valid
			auto& o2w = GetO2W(inst);
			auto flags = GetFlags(inst);
			if (!IsFinite(o2w))
				throw std::runtime_error("Invalid instance transform");
			if (!AllSet(flags, EInstFlag::NonAffine) && !IsAffine(o2w))
				throw std::runtime_error("Invalid instance transform");
		}
		#endif

		// Add the model nuggets to the drawlist
		Lock lock(*this);
		AddNuggets(inst, nuggets, lock.drawlist());

		// Flag the drawlist as changed
		m_sort_needed = true;
	}

	// Remove an instance from the scene
	void RenderStep::RemoveInstance(BaseInstance const& inst)
	{
		Lock lock(*this);
		pr::erase_if(lock.drawlist(), [&](DrawListElement const& dle){ return dle.m_instance == &inst; });
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
		Lock lock(*this);
		pr::erase_if(lock.drawlist(), [&](DrawListElement const& dle)
		{
			auto iter = std::lower_bound(doomed, doomed_end, dle.m_instance);
			return iter != doomed_end && *iter == dle.m_instance;
		});
	}

	// Perform the render step
	void RenderStep::Execute(BackBuffer& bb, ID3D12GraphicsCommandList* cmd_list)
	{
		#if 0 // todo
		PR_EXPAND(PR_DBG_RDR, auto dbg = Scope<void>(
			[&]{ ss.m_dbg->BeginEvent(Enum<ERenderStep>::ToStringW(GetId())); },
			[&]{ ss.m_dbg->EndEvent(); }));

		// Commit before the start of a render step to ensure changes
		// are flushed before the render steps try to clear back buffers, etc
		StateStack::RSFrame frame(ss, *this);
		ss.Commit();
		#endif

		ExecuteInternal(bb, cmd_list);
	}

	// Notification of a model being destroyed
	void RenderStep::OnModelDeleted(Model& model, EmptyArgs const&) const
	{
		(void)model;
		#if 0 // todo
		#if PR_DBG_RDR

		// Check the model is not current in a drawlist
		Lock lock(*this);
		for (auto& dle : lock.drawlist())
		{
			if (&model == dle.m_nugget->m_owner)
				throw std::runtime_error("Model being deleted is still in the drawlist");
		}

		#endif
		#endif
	}
}