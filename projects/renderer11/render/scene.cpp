//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/steps/forward_render.h"
#include "pr/renderer11/steps/gbuffer.h"
#include "pr/renderer11/steps/dslighting.h"
#include "renderer11/render/state_stack.h"

namespace pr
{
	namespace rdr
	{
		// Make a scene
		Scene::Scene(pr::Renderer& rdr, std::vector<ERenderStep>&& rsteps, SceneView const& view)
			:m_rdr(&rdr)
			,m_view(view)
			,m_viewport(rdr.RenderTargetSize())
			,m_render_steps()
			,m_bkgd_colour()
			,m_global_light()
		{
			SetRenderSteps(std::move(rsteps));
		}

		// Set the render steps to use for rendering the scene
		void Scene::SetRenderSteps(std::vector<ERenderStep>&& rsteps)
		{
			m_render_steps.clear();
			for (auto rs : rsteps)
			{
				switch (rs)
				{
				default: throw std::exception("Unknown render step");
				case ERenderStep::ForwardRender: m_render_steps.push_back(std::make_shared<ForwardRender>(*this)); break;
				case ERenderStep::GBuffer:       m_render_steps.push_back(std::make_shared<GBuffer>(*this));
				case ERenderStep::DSLighting:    m_render_steps.push_back(std::make_shared<DSLighting>(*this));
				}
			}
		}

		// Find a render step by id
		RenderStep* Scene::FindRStep(ERenderStep::Enum_ id) const
		{
			for (auto& rs : m_render_steps)
				if (rs->GetId() == id)
					return rs.get();

			return nullptr;
		}

		// Access the render step by Id
		RenderStep& Scene::operator[](ERenderStep::Enum_ id) const
		{
			auto rs = FindRStep(id);
			if (rs) return *rs;

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
			// Raise the Update scene event.
			// Observers should add/remove instances from the scene
			// or specific render steps as required
			pr::events::Send(Evt_UpdateScene(*this));
		}

		// Add an instance. The instance must be resident for the entire time that it is
		// in the drawlist, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
		// This method will add the instance to all render steps for which the model has appropriate nuggets.
		// Instances can be added to render steps directly if finer control is needed
		void Scene::AddInstance(BaseInstance const& inst)
		{
			for (auto& rs : m_render_steps)
				rs->AddInstance(inst);
		}

		// Remove an instance from the scene
		void Scene::RemoveInstance(BaseInstance const& inst)
		{
			for (auto& rs : m_render_steps)
				rs->RemoveInstance(inst);
		}

		// Render the scene
		void Scene::Render()
		{
			// Invoke each render step in order
			StateStack ss(m_rdr->ImmediateDC(), *this);
			for (auto& rs : m_render_steps)
				rs->Execute(ss);
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