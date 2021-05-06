//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/lights/light.h"
#include "pr/view3d/render/scene_view.h"
#include "pr/view3d/render/state_block.h"
#include "pr/view3d/steps/ray_cast.h"
#include "pr/view3d/util/stock_resources.h"
#include "pr/view3d/util/event_args.h"
#include "pr/view3d/util/wrappers.h"
#include "pr/view3d/util/diagnostic.h"

namespace pr::rdr
{
	// A scene is a view into the 3D world. Typically most applications only have one scene.
	// Examples of multiple scenes are: the rear vision mirror in a car, map view, etc.
	// A scene contains an ordered collection of render steps.
	struct alignas(16) Scene
	{
		// Fixed container of render steps. Doesn't really need to be fixed,
		// but non-fixed means we need the pr::rdr::Allocator to construct it.
		// Conceptually, 'InstCont' should be an unordered_set, but using an array
		// is way faster, due to the lack of allocations. This means RemoveInstance
		// is O(n) however.
		using RenderStepCont = pr::vector<RenderStepPtr, 16, true>;
		using InstCont = pr::vector<BaseInstance const*, 1024, false>;
		using RayCastStepPtr = std::unique_ptr<RayCastStep>;

		Window*        m_wnd;           // The controlling window
		SceneView      m_view;          // Represents the camera properties used to project onto the screen
		Viewport       m_viewport;      // Represents the rectangular area on the back buffer that this scene covers
		InstCont       m_instances;     // Instances added to this scene for rendering.
		RenderStepCont m_render_steps;  // The stages of rendering the scene
		RayCastStepPtr m_ht_immediate;  // A ray cast render step for performing immediate hit tests
		Colour         m_bkgd_colour;   // The background colour for the scene
		Light          m_global_light;  // The global light settings
		TextureCubePtr m_global_envmap; // A global environment map
		DSBlock        m_dsb;           // Scene-wide depth states
		RSBlock        m_rsb;           // Scene-wide render states
		BSBlock        m_bsb;           // Scene-wide blend states
		DiagState      m_diag;          // Diagnostic variables
		AutoSub        m_eh_resize;     // RT resize event handler subscription

		Scene(Window& wnd, std::initializer_list<ERenderStep> rsteps = {ERenderStep::ForwardRender}, SceneView const& view = SceneView());

		// Renderer access
		Renderer& rdr() const;
		Window& wnd() const;

		// Raised just before the drawlist is sorted. Handlers should add/remove instances from the scene, or add/remove render steps as required
		EventHandler<Scene&, EmptyArgs const&> OnUpdateScene;

		// Set the render steps to use for rendering the scene
		void SetRenderSteps(std::initializer_list<ERenderStep> rsteps);

		// Perform an immediate hit test on the instances provided by coroutine 'instances'
		// Successive calls to 'instances' should return instances to be hit tested. Return nullptr when complete.
		void HitTest(HitTestRay const* rays, int count, float snap_distance, EHitTestFlags flags, RayCastStep::Instances instances, RayCastStep::ResultsOut const& results);

		// Set the collection of rays to cast into the scene for continuous hit testing.
		// Setting a non-zero number of rays enables a RayCast render step. Zero rays disables.
		void HitTestContinuous(HitTestRay const* rays, int count, float snap_distance, EHitTestFlags flags, RayCastStep::InstFilter const& include);
			
		// Read the hit test results from the continuous ray cast render step.
		void HitTestGetResults(RayCastStep::ResultsOut const& results);

		// Get/Set the view (i.e. the camera to screen projection or 'View' matrix in dx speak)
		void SetView(SceneView const& view) { m_view = view; }
		void SetView(Camera const& cam) { SetView(SceneView(cam)); }

		// Access the render step by Id
		RenderStep* FindRStep(ERenderStep id) const;
		RenderStep& operator[](ERenderStep id) const;

		// Render step specific accessors
		template <typename TRStep, typename = enable_if_render_step<TRStep>> TRStep* FindRStep() const
		{
			return static_cast<TRStep*>(FindRStep(TRStep::Id));
		}
		template <typename TRStep> TRStep& RStep() const
		{
			auto rs = FindRStep<TRStep>();
			if (rs != nullptr) return *rs;
			throw std::runtime_error(Fmt("RenderStep %s is not part of this scene", Enum<ERenderStep>::ToStringA(TRStep::Id)));
		}

		// Enable/Disable shadow casting
		void ShadowCasting(bool enable, int shadow_map_size);

		// Clear/Populate the drawlists for each render step.
		// Drawlists can be used in two ways, one is to clear the draw sets with each frame
		// and rebuild them from scratch (useful for scenes that change frequently).
		// The other is to NOT clear the draw sets and add/remove instances between frames
		void ClearDrawlists();
		void UpdateDrawlists();

		// Rendering multi-pass models:
		// To render a model that needs to be done in multiple passes, add additional nuggets to
		// the model that overlap with existing nuggets but have different render states/shaders
		// e.g. To render back faces first, then front faces: Add a nugget for the whole model
		// with front face culling, then another nugget for the whole model with back face culling.

		// Add an instance. The instance must be resident for the entire time that it is
		// in the scene, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
		// This method will add the instance to all render steps for which the model has appropriate nuggets.
		// Instances can be added to render steps directly if finer control is needed
		void AddInstance(BaseInstance const& inst, EInstFlags flags = EInstFlags::None);
		template <typename Inst> void AddInstance(Inst const& inst, EInstFlags flags = EInstFlags::None)
		{
			AddInstance(inst.m_base, flags);
		}

		// Remove an instance from the drawlist
		void RemoveInstance(BaseInstance const& inst);
		template <typename Inst> void RemoveInstance(Inst const& inst)
		{
			RemoveInstance(inst.m_base);
		}

		// Render the scene
		void Render();

		// Some render step pre-sets
		static std::vector<ERenderStep> ForwardRendering()
		{
			return {ERenderStep::ForwardRender};
		}
		static std::vector<ERenderStep> DeferredRendering()
		{
			return {ERenderStep::GBuffer, ERenderStep::DSLighting};
		}

	private:

		// Resize the viewport on back buffer resize
		void HandleBackBufferSizeChanged(Window& wnd, BackBufferSizeChangedEventArgs const& evt);
	};
}
