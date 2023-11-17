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
//#include "view3d-12/src/render/state_stack.h"
//#include "pr/view3d/instances/instance.h"
//#include "pr/view3d/steps/gbuffer.h"
//#include "pr/view3d/steps/dslighting.h"
//#include "pr/view3d/steps/ray_cast.h"

namespace pr::rdr12
{
	// Make a scene
	Scene::Scene(Window& wnd, std::initializer_list<ERenderStep> rsteps, SceneCamera const& cam)
		: m_wnd(&wnd)
		, m_cam(cam)
		, m_viewport(wnd.BackBufferSize())
		, m_instances()
		, m_render_steps()
		, m_bkgd_colour(Colour32Black)
		//, m_ht_immediate()
		, m_global_light()
		, m_global_envmap()
		, m_eh_resize()
	{
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
	ResourceManager& Scene::res() const
	{
		return rdr().res();
	}

	// Reset the drawlist for each render step
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
				//case ERenderStep::RayCast:       m_render_steps.emplace_back(new RayCastStep(*this, true)); break;
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

	// Render the scene
	pr::vector<ID3D12CommandList*> Scene::Render(BackBuffer& bb)
	{
		// Make sure the scene is up to date
		OnUpdateScene(*this);

		// Don't call 'm_wnd->RestoreRT();' here because we might be rendering to
		// an off-screen texture. However, if the app contains multiple windows
		// each window will need to call 'm_wnd->RestoreRT()' before rendering.
		#if PR_DBG_RDR
		{
			#if 0 // todo
			// Check a render target has been set
			// Note: if you've called GetDC() you need to call ReleaseDC() and Window.RestoreRT() or RTV will be null
			D3DPtr<ID3D11RenderTargetView> rtv;
			D3DPtr<ID3D11DepthStencilView> dsv;
			dc->OMGetRenderTargets(1, &rtv.m_ptr, &dsv.m_ptr);
			if (rtv == nullptr) throw std::runtime_error("Render target is null."); // Ensure RestoreRT has been called
			if (dsv == nullptr) throw std::runtime_error("Depth buffer is null."); // Ensure RestoreRT has been called
			#endif
		}
		#endif

		//todo: run this in a background thread - the thread will need to create and own the cmd_alloc until list->Close is called.
		//todo: Each render step might be able to run in parallel? So 'Scene::Render' would return a list of command lists to execute.
		pr::vector<ID3D12CommandList*> cmd_lists;
		{
			// Invoke each render step in order
			for (auto& rs : m_render_steps)
				cmd_lists.push_back(rs->Execute(bb));
		}

		// Return the command list to the caller who will batch up calls to execute.
		// When multi-threading, we can return this before we're finished
		return std::move(cmd_lists);
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