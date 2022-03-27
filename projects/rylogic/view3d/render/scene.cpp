//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/scene.h"
#include "pr/view3d/render/window.h"
#include "pr/view3d/render/renderer.h"
#include "pr/view3d/instances/instance.h"
#include "pr/view3d/steps/forward_render.h"
#include "pr/view3d/steps/gbuffer.h"
#include "pr/view3d/steps/dslighting.h"
#include "pr/view3d/steps/shadow_map.h"
#include "pr/view3d/steps/ray_cast.h"
#include "view3d/render/state_stack.h"

namespace pr::rdr
{
	// Make a scene
	Scene::Scene(Window& wnd, std::initializer_list<ERenderStep> rsteps, SceneView const& view)
		:m_wnd(&wnd)
		,m_view(view)
		,m_viewport(wnd.RenderTargetSize())
		,m_instances()
		,m_render_steps()
		,m_ht_immediate()
		,m_bkgd_colour()
		,m_global_light()
		,m_global_envmap()
		,m_dsb()
		,m_rsb()
		,m_bsb()
		,m_diag(wnd.rdr())
		,m_eh_resize()
	{
		SetRenderSteps(rsteps);

		// Set default scene render states
		m_rsb = RSBlock::SolidCullBack();

		// Use line antialiasing if multi-sampling is enabled
		if (wnd.m_multisamp.Count != 1)
			m_rsb.Set(ERS::MultisampleEnable, TRUE);

		// Sign up for back buffer resize events
		m_eh_resize = wnd.m_rdr->BackBufferSizeChanged += std::bind(&Scene::HandleBackBufferSizeChanged, this, _1, _2);
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
				case ERenderStep::ForwardRender: m_render_steps.emplace_back(new ForwardRender(*this)); break;
				case ERenderStep::GBuffer:       m_render_steps.emplace_back(new GBuffer(*this)); break;
				case ERenderStep::DSLighting:    m_render_steps.emplace_back(new DSLighting(*this)); break;
				case ERenderStep::ShadowMap:     m_render_steps.emplace_back(new ShadowMap(*this, m_global_light)); break;
				case ERenderStep::RayCast:       m_render_steps.emplace_back(new RayCastStep(*this, true)); break;
				default: throw std::exception("Unknown render step");
			}
		}
	}

	// Perform an immediate hit test
	void Scene::HitTest(HitTestRay const* rays, int count, float snap_distance, EHitTestFlags flags, RayCastStep::Instances instances, RayCastStep::ResultsOut const& results)
	{
		if (rays == nullptr || count == 0)
			return;

		// Lazy create the ray cast step
		// Note: I've noticed that with runtime shaders enabled, reusing the same RayCastStep
		// doesn't seem to work, I never figured out why though. I had to create a new RayCastStep
		// for each hit test.
		if (m_ht_immediate == nullptr)
			m_ht_immediate.reset(new RayCastStep(*this, false));
			
		auto& rs = *m_ht_immediate.get();

		// Set the rays to cast
		rs.SetRays(rays, count, snap_distance, flags, [=](auto) { return true; });

		// Create a ray cast render step and populate its draw list.
		// Note: don't look for and reuse an existing RayCastStep because callers may want
		// to invoke immediate ray casts without interfering with existing continuous ray casts.
		if (instances != nullptr)
		{
			for (BaseInstance const* inst; (inst = instances()) != nullptr;)
				rs.AddInstance(*inst);
		}
		else
		{
			for (auto& inst : m_instances)
				rs.AddInstance(*inst);
		}

		// Render just this step
		Renderer::Lock lock(m_wnd->rdr());
		StateStack ss(lock.ImmediateDC(), *this);
		rs.Execute(ss);

		// Read (blocking) the hit test results
		rs.ReadOutput(results);

		// Reset ready for next time
		rs.ClearDrawlist();
	}

	// Set the collection of rays to cast into the scene for continuous hit testing.
	void Scene::HitTestContinuous(HitTestRay const* rays, int count, float snap_distance, EHitTestFlags flags, RayCastStep::InstFilter const& include)
	{
		// Look for an existing RayCast render step
		if (rays == nullptr || count != 0)
		{
			// Ensure there is a ray cast render step, add if not.
			auto rs = static_cast<RayCastStep*>(FindRStep(ERenderStep::RayCast));
			if (rs == nullptr)
			{
				// Add the ray cast step first so that 'CopyResource' can happen while we render the rest of the scene
				RenderStepPtr step(new RayCastStep(*this, true));
				auto iter = m_render_steps.insert(begin(m_render_steps), std::move(step));
				rs = static_cast<RayCastStep*>(iter->get());
			}

			// Set the rays to cast.
			// Results will be available in 'm_ht_results' after Render() has been called a few times (due to multi-buffering)
			rs->SetRays(rays, count, snap_distance, flags, include);
		}
		else
		{
			// Remove the ray cast step if there are no rays to cast
			pr::erase_if(m_render_steps, [](auto& rs){ return rs->GetId() == ERenderStep::RayCast; });
		}
	}

	// Read the hit test results from the continuous ray cast render step
	void Scene::HitTestGetResults(RayCastStep::ResultsOut const& results)
	{
		auto rs = static_cast<RayCastStep*>(FindRStep(ERenderStep::RayCast));
		if (rs == nullptr)
			return;

		// Read the hit test results
		rs->ReadOutput(results);
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
		throw std::runtime_error(Fmt("RenderStep %s is not part of this scene", Enum<ERenderStep>::ToStringA(id)));
	}

	// Enable/Disable shadow casting
	void Scene::ShadowCasting(bool enable, int shadow_map_size)
	{
		if (enable)
		{
			// Ensure there is a shadow map render step
			auto shadow_map_step = FindRStep(ERenderStep::ShadowMap);
			if (shadow_map_step == nullptr)
			{
				RenderStepPtr step(new ShadowMap(*this, m_global_light, shadow_map_size));

				// Insert the shadow map step before the main render step
				auto iter = begin(m_render_steps);
				for (; iter != end(m_render_steps) && (**iter).GetId() != ERenderStep::ForwardRender && (**iter).GetId() != ERenderStep::DSLighting; ++iter) {}
				m_render_steps.insert(iter, std::move(step));
			}
		}
		else
		{
			// Remove the shadow map render step
			pr::erase_if(m_render_steps, [](auto& rs) { return rs->GetId() == ERenderStep::ShadowMap; });
		}
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
		OnUpdateScene(*this);
	}

	// Add an instance. The instance must be resident for the entire time that it is
	// in the drawlist, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
	// This method will add the instance to all render steps for which the model has appropriate nuggets.
	// Instances can be added to render steps directly if finer control is needed
	void Scene::AddInstance(BaseInstance const& inst)
	{
		m_instances.push_back(&inst);
		for (auto& rs : m_render_steps)
			rs->AddInstance(inst);
	}

	// Remove an instance from the scene
	void Scene::RemoveInstance(BaseInstance const& inst)
	{
		auto iter = pr::find(m_instances, &inst);
		if (iter != std::end(m_instances))
			m_instances.erase_fast(iter);

		for (auto& rs : m_render_steps)
			rs->RemoveInstance(inst);
	}

	// Render the scene
	void Scene::Render()
	{
		Renderer::Lock lock(rdr());
		auto dc = lock.ImmediateDC();

		// Don't call 'm_wnd->RestoreRT();' here because we might be rendering to
		// an off-screen texture. However, if the app contains multiple windows
		// each window will need to call 'm_wnd->RestoreRT()' before rendering.
		#if PR_DBG_RDR
		{
			// Check a render target has been set
			// Note: if you've called GetDC() you need to call ReleaseDC() and Window.RestoreRT() or RTV will be null
			D3DPtr<ID3D11RenderTargetView> rtv;
			D3DPtr<ID3D11DepthStencilView> dsv;
			dc->OMGetRenderTargets(1, &rtv.m_ptr, &dsv.m_ptr);
			if (rtv == nullptr) throw std::runtime_error("Render target is null."); // Ensure RestoreRT has been called
			if (dsv == nullptr) throw std::runtime_error("Depth buffer is null."); // Ensure RestoreRT has been called
		}
		#endif

		// Invoke each render step in order
		StateStack ss(dc, *this);
		for (auto& rs : m_render_steps)
			rs->Execute(ss);
	}

	// Resize the viewport on back buffer resize
	void Scene::HandleBackBufferSizeChanged(Window& wnd, BackBufferSizeChangedEventArgs const& args)
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