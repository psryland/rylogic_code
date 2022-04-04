//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/scene/scene_camera.h"
#include "pr/view3d-12/utility/wrappers.h"

namespace pr::rdr12
{
	struct alignas(16) Scene
	{
		// Notes:
		//  - A scene is a view into the 3D world, containing a camera and collection of instances.
		//  - A scene contains an ordered collection of render steps. Each render step has it's own drawlist.
		//  - Multiple scenes can contribute to the content of a window.
		//    e.g.
		//      A window could have separate scenes for; world geometry, player graphics, HUD, rear view mirror, etc
		//      Typically, most applications only have one scene.
	
		// Fixed container of render steps. Doesn't really need to be fixed,
		// but non-fixed means we need the pr::rdr::Allocator to construct it.
		// Conceptually, 'InstCont' should be an unordered_set, but using an array is way
		// faster due to the lack of allocations. This means RemoveInstance is O(n) however.
		using RenderStepCont = pr::vector<RenderStep*, 16, true>;
		using InstCont = pr::vector<BaseInstance const*, 1024, false>;
		//using RayCastStepPtr = std::unique_ptr<RayCastStep>;

		Window*        m_wnd;           // The controlling window
		SceneCamera    m_cam;           // Represents the camera properties used to project onto the screen
		Viewport       m_viewport;      // Represents the rectangular area on the back buffer that this scene covers
		InstCont       m_instances;     // Instances added to this scene for rendering.
		RenderStepCont m_render_steps;  // The stages of rendering the scene
		//RayCastStepPtr m_ht_immediate;  // A ray cast render step for performing immediate hit tests
		//Colour         m_bkgd_colour;   // The background colour for the scene
		//Light          m_global_light;  // The global light settings
		//TextureCubePtr m_global_envmap; // A global environment map
		//DSBlock        m_dsb;           // Scene-wide depth states
		//RSBlock        m_rsb;           // Scene-wide render states
		//BSBlock        m_bsb;           // Scene-wide blend states
		//DiagState      m_diag;          // Diagnostic variables
		AutoSub        m_eh_resize;     // RT resize event handler subscription

		Scene(Window& wnd, std::initializer_list<ERenderStep> rsteps = {ERenderStep::ForwardRender}, SceneCamera const& cam = SceneCamera{});
		~Scene();

		// Renderer access
		Renderer& rdr() const;
		Window& wnd() const;

		// Raised just before the drawlist is sorted. Handlers should add/remove instances from the scene, or add/remove render steps as required
		EventHandler<Scene&, EmptyArgs const&> OnUpdateScene;

		// Set the render steps to use for rendering the scene
		void SetRenderSteps(std::initializer_list<ERenderStep> rsteps);

	private:

		// Resize the viewport on back buffer resize
		void HandleBackBufferSizeChanged(Window& wnd, BackBufferSizeChangedEventArgs const& evt);
	};
}

