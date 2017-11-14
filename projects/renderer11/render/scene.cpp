//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/window.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/steps/forward_render.h"
#include "pr/renderer11/steps/gbuffer.h"
#include "pr/renderer11/steps/dslighting.h"
#include "pr/renderer11/steps/shadow_map.h"
#include "pr/renderer11/steps/ray_cast.h"
#include "renderer11/render/state_stack.h"

namespace pr
{
	namespace rdr
	{
		// Make a scene
		Scene::Scene(Window& wnd, std::initializer_list<ERenderStep> rsteps, SceneView const& view)
			:m_wnd(&wnd)
			,m_view(view)
			,m_viewport(wnd.RenderTargetSize())
			,m_instances()
			,m_render_steps()
			,m_ht_rays()
			,m_ht_results()
			,m_bkgd_colour()
			,m_global_light()
			,m_dsb()
			,m_rsb()
			,m_bsb()
			,m_eh_resize()
		{
			SetRenderSteps(rsteps);

			// Set default scene render states
			m_rsb = RSBlock::SolidCullBack();

			// Use line antialiasing if multi-sampling is enabled
			if (wnd.m_multisamp.Count != 1)
				m_rsb.Set(ERS::MultisampleEnable, TRUE);

			// Sign up for render target resize events
			m_eh_resize = wnd.m_rdr->RenderTargetSizeChanged += std::bind(&Scene::HandleRenderTargetSizeChanged, this, _1, _2);
		}

		// Access the renderer
		Renderer& Scene::rdr() const
		{
			return m_wnd->rdr();
		}
		Window& Scene::wnd() const
		{
			return *m_wnd;
		}

		// Set the render steps to use for rendering the scene
		void Scene::SetRenderSteps(std::initializer_list<ERenderStep> rsteps)
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
				case ERenderStep::ShadowMap:     m_render_steps.push_back(std::make_shared<ShadowMap    >(*this, m_global_light, iv2(4096,4096))); break;
				case ERenderStep::RayCast:       m_render_steps.push_back(std::make_shared<RayCastStep  >(*this, true)); break;
				}
			}
		}

		// Set the collection of rays to cast into the scene for hit testing.
		// If 'immediate' is true, the scene is rendered with just the 'RayCastStep'.
		// The function blocks until 'm_ht_results' are up to date (note: this stalls the gfx pipeline).
		// If 'immediate' is false, the 'RayCastStep' render step is added to the steps for
		// this scene, and triple buffering is used to update the 'm_ht_results' after each
		// Render() call. Note, this mode is intended for continuous frame rate rendering
		// and means the hit test results are 3 frames behind.
		// Setting an empty set of rays disables hit testing ('immediate' is ignored).
		void Scene::SetHitTestRays(HitTestRay const* rays, int count, float snap_distance, EHitTestFlags flags, bool immediate)
		{
			// Resize the buffers
			m_ht_rays.assign(rays, rays + count);
			m_ht_results.resize(count);

			// If there are no rays, remove the RayCastStep (if there)
			if (count == 0)
			{
				pr::erase_if(m_render_steps, [=](auto& rs){ return rs->GetId() == ERenderStep::RayCast; });
			}
			// If 'immediate', do a ray cast now, using only the RayCastStep
			else if (immediate)
			{
				// Create a ray cast render step and populate its draw list.
				// Note: don't look for and reuse an existing RayCastStep because callers may want
				// to invoke immediate ray casts without interfering with existing continuous ray casts.
				RayCastStep rs(*this, false);
				for (auto& inst : m_instances)
					rs.AddInstance(*inst);

				// Set the rays to cast
				rs.SetRays(m_ht_rays.data(), int(m_ht_rays.size()), snap_distance, flags);

				// Render just this step
				Renderer::Lock lock(m_wnd->rdr());
				StateStack ss(lock.ImmediateDC(), *this);
				rs.Execute(ss);

				// Read (blocking) the hit test results
				rs.ReadOutput(m_ht_results.data(), int(m_ht_results.size()));
			}
			// Otherwise, add the ray cast step to the render sets
			else
			{
				// Ensure there is a ray cast render step, add if not.
				auto rs = static_cast<RayCastStep*>(FindRStep(ERenderStep::RayCast));
				if (rs == nullptr)
				{
					// Add the ray cast step first so that 'CopyResource' can happen while we render the rest of the scene
					auto step = std::make_shared<RayCastStep>(*this, true);
					m_render_steps.insert(std::begin(m_render_steps), step);
					rs = step.get();
				}

				// Set the rays to cast.
				// Results will be available in 'm_ht_results' after Render() has been called a few times (due to triple buffering)
				rs->SetRays(m_ht_rays.data(), int(m_ht_rays.size()), snap_distance, flags);
			}
		}

		// Find a render step by id
		RenderStep* Scene::FindRStep(ERenderStep id) const
		{
			for (auto& rs : m_render_steps)
				if (rs->GetId() == id)
					return rs.get();

			return nullptr;
		}

		// Access the render step by Id
		RenderStep& Scene::operator[](ERenderStep id) const
		{
			auto rs = FindRStep(id);
			if (rs) return *rs;

			PR_ASSERT(PR_DBG_RDR, false, Fmt("RenderStep %s is not part of this scene", ToStringA(id)).c_str());
			throw std::exception("Render step not part of this scene");
		}

		// Reset the drawlist for each render step
		void Scene::ClearDrawlists()
		{
			m_instances.clear();
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
			m_instances.insert(&inst);
			for (auto& rs : m_render_steps)
				rs->AddInstance(inst);
		}

		// Remove an instance from the scene
		void Scene::RemoveInstance(BaseInstance const& inst)
		{
			m_instances.erase(&inst);
			for (auto& rs : m_render_steps)
				rs->RemoveInstance(inst);
		}

		// Render the scene
		void Scene::Render()
		{
			Renderer::Lock lock(rdr());

			// Don't call 'm_wnd->RestoreRT();' here because we might be rendering to
			// an off-screen texture. However, if the app contains multiple windows
			// each window will need to call 'm_wnd->RestoreRT()' before rendering.
			#if PR_DBG_RDR
			{
				// Check a render target has been set
				// Note: if you've called GetDC() you need to call ReleaseDC() and Window.RestoreRT() or RTV will be null
				D3DPtr<ID3D11RenderTargetView> rtv;
				D3DPtr<ID3D11DepthStencilView> dsv;
				lock.ImmediateDC()->OMGetRenderTargets(1, &rtv.m_ptr, &dsv.m_ptr);
				PR_ASSERT(PR_DBG_RDR, rtv != nullptr, "Render target is null."); // Ensure RestoreRT has been called
				PR_ASSERT(PR_DBG_RDR, dsv != nullptr, "Depth buffer is null."); // Ensure RestoreRT has been called
			}
			#endif

			// Invoke each render step in order
			StateStack ss(lock.ImmediateDC(), *this);
			for (auto& rs : m_render_steps)
				rs->Execute(ss);
		}

		// Resize the viewport on back buffer resize
		void Scene::HandleRenderTargetSizeChanged(Window& wnd, RenderTargetSizeChangedEventArgs const& args)
		{
			if (args.m_done && &wnd == m_wnd)
			{
				// Only adjust the width/height of the viewport to the new area.
				// If an application is using a different viewport region they'll
				// have to adjust it after this (and before the next frame is drawn)
				m_viewport.Width = float(args.m_area.x);
				m_viewport.Height = float(args.m_area.y);
			}
		}
	}
}