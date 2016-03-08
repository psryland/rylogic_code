//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/window.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/steps/forward_render.h"
#include "pr/renderer11/steps/gbuffer.h"
#include "pr/renderer11/steps/dslighting.h"
#include "pr/renderer11/steps/shadow_map.h"
#include "renderer11/render/state_stack.h"

namespace pr
{
	namespace rdr
	{
		// Make a scene
		Scene::Scene(Window& wnd, std::vector<ERenderStep>&& rsteps, SceneView const& view)
			:m_wnd(&wnd)
			,m_view(view)
			,m_viewport(wnd.RenderTargetSize())
			,m_render_steps()
			,m_bkgd_colour()
			,m_global_light()
			,m_dsb()
			,m_rsb()
			,m_bsb()
		{
			SetRenderSteps(std::move(rsteps));

			// Set default scene render states
			m_rsb = RSBlock::SolidCullBack();

			// Use line antialiasing if multi-sampling is enabled
			if (wnd.m_multisamp.Count != 1)
				m_rsb.Set(ERS::MultisampleEnable, TRUE);
		}
		Scene::~Scene()
		{
			pr::events::Send(Evt_SceneDestroy(*this));
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
				case ERenderStep::GBuffer:       m_render_steps.push_back(std::make_shared<GBuffer      >(*this)); break;
				case ERenderStep::DSLighting:    m_render_steps.push_back(std::make_shared<DSLighting   >(*this)); break;
				case ERenderStep::ShadowMap:     m_render_steps.push_back(std::make_shared<ShadowMap    >(*this, m_global_light, pr::iv2::make(4096,4096))); break;
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

			PR_ASSERT(PR_DBG_RDR, false, Fmt("RenderStep %s is not part of this scene", ERenderStep::ToStringA(id)).c_str());
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
			// Don't call 'm_wnd->RestoreRT();' here because we might be rendering to
			// an off-screen texture. However, if the app contains multiple windows
			// each window will need to call 'm_wnd->RestoreRT()' before rendering.
			#if PR_DBG_RDR
			{
				// Check a render target has been set
				// Note: if you've called GetDC() you need to call ReleaseDC() and Window.RestoreRT() or rtv will be null
				D3DPtr<ID3D11RenderTargetView> rtv;
				D3DPtr<ID3D11DepthStencilView> dsv;
				m_wnd->ImmediateDC()->OMGetRenderTargets(1, &rtv.m_ptr, &dsv.m_ptr);
				PR_ASSERT(PR_DBG_RDR, rtv != nullptr, "Render target is null."); // Ensure RestoreRT has been called
				PR_ASSERT(PR_DBG_RDR, dsv != nullptr, "Depth buffer is null."); // Ensure RestoreRT has been called
			}
			#endif

			// Invoke each render step in order
			StateStack ss(m_wnd->ImmediateDC(), *this);
			for (auto& rs : m_render_steps)
				rs->Execute(ss);
		}

		// Resize the viewport on back buffer resize
		void Scene::OnEvent(Evt_Resize const& evt)
		{
			if (evt.m_done && evt.m_window == m_wnd)
			{
				// Only adjust the width/height of the viewport to the new area.
				// If an application is using a different viewport region they'll
				// have to adjust it after this (and before the next frame is drawn)
				m_viewport.Width = float(evt.m_area.x);
				m_viewport.Height = float(evt.m_area.y);
			}
		}
	}
}