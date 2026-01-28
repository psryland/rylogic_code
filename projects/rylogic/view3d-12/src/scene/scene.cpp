//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/render/render_step.h"
#include "pr/view3d-12/texture/texture_cube.h"
#include "pr/view3d-12/utility/eventargs.h"
#include "view3d-12/src/render/render_forward.h"
#include "view3d-12/src/render/render_smap.h"
#include "view3d-12/src/render/render_raycast.h"

namespace pr::rdr12
{
	// Make a scene
	Scene::Scene(Window& wnd, std::initializer_list<ERenderStep> rsteps, SceneCamera const& cam)
		: m_wnd(&wnd)
		, m_cam(cam)
		, m_viewport(wnd.BackBufferSize())
		, m_instances()
		, m_render_steps()
		, m_raycast_immed()
		, m_global_light()
		, m_global_envmap()
		, m_eh_resize()
	{
		// Initialise the scene camera to match the full window
		auto bb_size = m_wnd->BackBufferSize();
		if (bb_size != iv2::Zero())
			m_cam.Aspect(1.0f * bb_size.x / bb_size.y);

		// Set the render steps for the scene
		SetRenderSteps({ rsteps.begin(), rsteps.size() });

		//// Set default scene render states
		//m_rsb = RSBlock::SolidCullBack();

		//// Use line antialiasing if multi-sampling is enabled
		//if (wnd.m_multisamp.Count != 1)
		//	m_rsb.Set(ERS::MultisampleEnable, TRUE);

		// Sign up for back buffer resize events
		m_eh_resize = wnd.m_rdr->BackBufferSizeChanged += std::bind(&Scene::HandleBackBufferSizeChanged, this, _1, _2);
	}
	Scene::~Scene()
	{
		SetRenderSteps({});
	}

	// Access the renderer
	ID3D12Device4* Scene::d3d() const
	{
		return rdr().d3d();
	}
	Renderer& Scene::rdr() const
	{
		return wnd().rdr();
	}
	Window& Scene::wnd() const
	{
		return *m_wnd;
	}

	// Reset the draw list for each render step
	void Scene::ClearDrawlists()
	{
		m_instances.clear();
		for (auto& rs : m_render_steps)
			rs->ClearDrawlist();
	}

	// Return a render step from this scene (if present)
	RenderStep const* Scene::FindRStep(ERenderStep id) const
	{
		for (auto& step : m_render_steps)
		{
			if (step->m_step_id != id) continue;
			return step.get();
		}
		return nullptr;
	}
	RenderStep* Scene::FindRStep(ERenderStep id)
	{
		for (auto& step : m_render_steps)
		{
			if (step->m_step_id != id) continue;
			return step.get();
		}
		return nullptr;
	}

	// Add an instance. The instance must be resident for the entire time that it is
	// in the draw list, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
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
		// Remove from our collection
		auto iter = pr::find(m_instances, &inst);
		if (iter != std::end(m_instances))
			m_instances.erase_fast(iter);

		// Remove from each render step
		for (auto& rs : m_render_steps)
			rs->RemoveInstance(inst);
	}

	// Set the render steps to use for rendering the scene
	void Scene::SetRenderSteps(std::span<ERenderStep const> rsteps)
	{
		// Can't use unique_ptr without #including 'render_step' in the header
		m_render_steps.clear();

		for (auto rs : rsteps)
		{
			switch (rs)
			{
				case ERenderStep::RenderForward: m_render_steps.emplace_back(new RenderForward(*this)); break;
				//case ERenderStep::GBuffer:       m_render_steps.emplace_back(new GBuffer(*this)); break;
				//case ERenderStep::DSLighting:    m_render_steps.emplace_back(new DSLighting(*this)); break;
				case ERenderStep::ShadowMap:     m_render_steps.emplace_back(new RenderSmap(*this, m_global_light)); break;
				case ERenderStep::RayCast:       m_render_steps.emplace_back(new RenderRayCast(*this, true)); break;
				default: throw std::runtime_error("Unknown render step");
			}
		}
	}

	// Enable/Disable shadow casting
	void Scene::ShadowCasting(bool enable, int shadow_map_size)
	{
		if (enable && FindRStep<RenderSmap>() == nullptr)
		{
			m_render_steps.emplace(std::begin(m_render_steps), new RenderSmap(*this, m_global_light, shadow_map_size));
		}
		if (!enable && FindRStep<RenderSmap>() != nullptr)
		{
			pr::erase_if(m_render_steps, [](auto& rs) { return rs->m_step_id == ERenderStep::ShadowMap; });
		}
	}

	// Perform an immediate hit test
	std::future<void> Scene::HitTest(std::span<HitTestRay const> rays, ESnapMode snap_mode, float snap_distance, RayCastInstancesCB instances, RayCastResultsOut const& out)
	{
		// Notes:
		//  - The immediate ray cast should be completely separate from the continuous ray cast.
		//    It should be possible to have both used within a single frame.
		//  - 'snap_mode' defines the features that the ray can hit. It shouldn't be zero.
		assert(snap_mode != ESnapMode::NoSnap && "HitTest will not hit anything because no snap mode is set");

		if (rays.empty())
			return {};

		// Lazy create the ray cast render step
		if (m_raycast_immed == nullptr)
			m_raycast_immed.reset(new RenderRayCast(*this, false));
			
		auto& rs = *m_raycast_immed.get();

		// Set the rays to cast
		rs.SetRays(rays, snap_mode, snap_distance, [=](auto) { return true; });

		// Populate the draw list.
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

		auto result = m_raycast_immed->ExecuteImmediate(out);

		// Reset ready for next time
		rs.ClearDrawlist();

		return result;
	}

	// Set the collection of rays to cast into the scene for continuous hit testing.
	void Scene::HitTestContinuous(std::span<HitTestRay const> rays, ESnapMode snap_mode, float snap_distance, RayCastFilter const& include)
	{
		// Look for an existing RayCast render step
		if (rays.empty())
		{
			// Ensure there is a ray cast render step, add if not.
			auto rs = static_cast<RenderRayCast*>(FindRStep(ERenderStep::RayCast));
			if (rs == nullptr)
			{
				// Add the ray cast step first so that 'CopyResource' can happen while we render the rest of the scene
				RenderRayCastPtr step(new RenderRayCast(*this, true));
				auto iter = m_render_steps.insert(begin(m_render_steps), std::move(step));
				rs = static_cast<RenderRayCast*>(iter->get());
			}

			// Set the rays to cast.
			// Results will be available in 'm_ht_results' after Render() has been called a few times (due to multi-buffering)
			rs->SetRays(rays, snap_mode, snap_distance, include);
		}
		else
		{
			// Remove the ray cast step if there are no rays to cast
			pr::erase_if(m_render_steps, [](auto& rs){ return rs->m_step_id == ERenderStep::RayCast; });
		}
	}

	// Read the hit test results from the continuous ray cast render step
	void Scene::HitTestGetResults(RayCastResultsOut const& out)
	{
		auto rs = static_cast<RenderRayCast*>(FindRStep(ERenderStep::RayCast));
		if (rs == nullptr)
			return;

		(void)out;
		#if 0
		// Read the hit test results
		rs->ReadOutput(results);
		#endif
	}

	// Render the scene, recording the command lists in 'frame'
	void Scene::Render(Frame& frame)
	{
		// Notes:
		//  - Start rendering 'scene'. Remember, this is only recording commands into command lists so "drawing" on a back buffer doesn't
		//    actually happen until 'Present' is called (which executes the command lists). This means a HUD scene can render to 'swap_chain_bb'
		//    at the same time as a main view scene renders to 'msaa_bb'. Present composites the scene by executing the msaa command lists,
		//    then resolving the msaa render target into the swap chain back buffer, then executing the swap chain command lists.
		//  - 'rs->Execute(frame)' could start a background thread and return immediately. It should add it's not-yet-closed command lists
		//    to the frame from the main thread before starting.

		// Make sure the scene is up to date
		OnUpdateScene(*this);

		// Invoke each render step in order
		for (auto& rs : m_render_steps)
			rs->Execute(frame);
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