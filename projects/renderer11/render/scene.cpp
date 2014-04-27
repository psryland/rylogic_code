//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/instances/instance.h"
//#include "pr/renderer11/render/draw_method.h"
//#include "pr/renderer11/util/lock.h"
//#include "renderer11/shaders/cbuffer.h"
//#include "renderer11/render/stereo.h"

namespace pr
{
	namespace rdr
	{
		// Make a scene
		Scene::Scene(pr::Renderer& rdr, SceneView const& view)
			:m_rdr(&rdr)
			,m_view(view)
			,m_viewport(rdr.RenderTargetSize())
			,m_render_steps()
		{
			m_render_steps.push_back(std::make_shared<ForwardRender>(*this));
		}

		// Access the render step by Id
		RenderStep& Scene::operator[](ERenderStep::Enum_ id) const
		{
			for (auto& rs : m_render_steps)
				if (rs->Id() == id)
					return *rs.get();

			PR_ASSERT(PR_DBG_RDR, false, Fmt("RenderStep %s is not part of this scene", ERenderStep::ToString(id)).c_str());
			throw std::exception("Render step not part of this scene");
		}

		// Reset the drawlist for each render step
		void Scene::ClearDrawlists()
		{
			for (auto& rs : m_render_steps)
				rs->ClearDrawlist();
		}

		// Populate the drawlist for each render step
		void Scene::UpdateDrawlists()
		{
			for (auto& rs : m_render_steps)
				rs->UpdateDrawlist();
		}

		// Add an instance. The instance must be resident for the entire time that it is
		// in the drawlist, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
		// This method will add the instance to all render steps for which the model has appropriate nuggets.
		// Instances can be added to render steps directly if finer control is needed
		void Scene::AddInstance(BaseInstance const& inst)
		{
			ModelPtr const& model = GetModel(inst);
			PR_ASSERT(PR_DBG_RDR, model != nullptr, "Null model pointer");
			PR_ASSERT(PR_DBG_RDR, !model->m_nmap.empty(), FmtS("This model ('%s') has no render nuggets and won't be added to any render steps", model->m_name.c_str()));

			PR_EXPAND(PR_DBG_RDR, bool at_least_one = false);
			for (auto& rs : m_render_steps)
			{
				// Only add the model if has nuggets for the render step
				if (model->m_nmap.count(rs->Id()) != 0)
				{
					rs->AddInstance(inst);
					PR_EXPAND(PR_DBG_RDR, at_least_one = true);
				}
			}

			#if PR_DBG_RDR
			if (!at_least_one && !AllSet(model->m_dbg_flags, EDbgRdrFlags::WarnedNoRenderNuggets))
			{
				PR_INFO(PR_DBG_RDR, FmtS("This model ('%s') was not added to any render steps. No appropriate render nuggets\n", model->m_name.c_str()));
				model->m_dbg_flags = SetBits(model->m_dbg_flags, EDbgRdrFlags::WarnedNoRenderNuggets, true);
			}
			#endif
		}

		// Remove an instance from the scene
		void Scene::RemoveInstance(BaseInstance const& inst)
		{
			ModelPtr const& model = GetModel(inst);
			PR_ASSERT(PR_DBG_RDR, model != nullptr, "Null model pointer");
			PR_ASSERT(PR_DBG_RDR, !model->m_nmap.empty(), FmtS("This model ('%s') has no render nuggets and won't be added to any render steps", model->m_name.c_str()));

			for (auto& rs : m_render_steps)
			{
				// Should only need to remove from this render step if there are appropriate nuggets
				if (model->m_nmap.count(rs->Id()) != 0)
					rs->RemoveInstance(inst);
			}
		}

		// Render the scene
		void Scene::Render()
		{
			// Invoke each render step in order
			for (auto& rs : m_render_steps)
				rs->Execute();
		}

		// Resize the viewport on back buffer resize
		void Scene::OnEvent(Evt_Resize const& evt)
		{
			// Todo, this is assuming the viewport covers the entire back buffer
			// it won't work for viewports that are sub regions of the screen
			if (evt.m_done)
				m_viewport = evt.m_area;
		}
	}
}