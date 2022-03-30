//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/render/render_step.h"
//#include "pr/view3d/instances/instance.h"
//#include "pr/view3d/steps/forward_render.h"
//#include "pr/view3d/steps/gbuffer.h"
//#include "pr/view3d/steps/dslighting.h"
//#include "pr/view3d/steps/shadow_map.h"
//#include "pr/view3d/steps/ray_cast.h"
#include "pr/view3d-12/utility/eventargs.h"
//#include "view3d/render/state_stack.h"

namespace pr::rdr12
{
	// Make a scene
	Scene::Scene(Window& wnd, std::initializer_list<ERenderStep> rsteps, SceneCamera const& cam)
		: m_wnd(&wnd)
		, m_cam(cam)
		, m_viewport(wnd.RenderTargetSize())
		, m_instances()
		, m_render_steps()
		//, m_ht_immediate()
		//, m_bkgd_colour()
		//, m_global_light()
		//, m_global_envmap()
		//, m_dsb()
		//, m_rsb()
		//, m_bsb()
		//, m_diag(wnd.rdr())
		, m_eh_resize()
	{
		SetRenderSteps(rsteps);

		//// Set default scene render states
		//m_rsb = RSBlock::SolidCullBack();

		//// Use line antialiasing if multi-sampling is enabled
		//if (wnd.m_multisamp.Count != 1)
		//	m_rsb.Set(ERS::MultisampleEnable, TRUE);

		// Sign up for back buffer resize events
		m_eh_resize = wnd.m_rdr->BackBufferSizeChanged += std::bind(&Scene::HandleBackBufferSizeChanged, this, _1, _2);
	}

	// Access the renderer
	Renderer& Scene::rdr() const
	{
		return wnd().rdr();
	}
	Window& Scene::wnd() const
	{
		return *m_wnd;
	}

	// Set the render steps to use for rendering the scene
	void Scene::SetRenderSteps(std::initializer_list<ERenderStep> rsteps)
	{
		(void)rsteps;
		m_render_steps.clear();
		//for (auto rs : rsteps)
		//{
		//	//switch (rs)
		//	//{
		//	//	case ERenderStep::ForwardRender: m_render_steps.emplace_back(new ForwardRender(*this)); break;
		//	//	case ERenderStep::GBuffer:       m_render_steps.emplace_back(new GBuffer(*this)); break;
		//	//	case ERenderStep::DSLighting:    m_render_steps.emplace_back(new DSLighting(*this)); break;
		//	//	case ERenderStep::ShadowMap:     m_render_steps.emplace_back(new ShadowMap(*this, m_global_light)); break;
		//	//	case ERenderStep::RayCast:       m_render_steps.emplace_back(new RayCastStep(*this, true)); break;
		//	//	default: throw std::runtime_error("Unknown render step");
		//	//}
		//}
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