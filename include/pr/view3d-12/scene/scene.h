//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/scene/scene_camera.h"
#include "pr/view3d-12/instance/instance.h"
#include "pr/view3d-12/lighting/light.h"
#include "pr/view3d-12/texture/texture_cube.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/view3d-12/utility/cmd_list.h"

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
		//	
		// Rendering multi-pass models:
		//    To render a model that needs to be done in multiple passes, add additional nuggets to
		//    the model that overlap with existing nuggets but have different render states/shaders
		//    e.g. To render back faces first, then front faces: Add a nugget for the whole model
		//    with front face culling, then another nugget for the whole model with back face culling.

		// Fixed container of render steps. Doesn't really need to be fixed,
		// but non-fixed means we need the pr::rdr::Allocator to construct it.
		// Conceptually, 'InstCont' should be an unordered_set, but using an array is way
		// faster due to the lack of allocations. This means RemoveInstance is O(n) however.
		using RenderStepCont = pr::vector<std::unique_ptr<RenderStep>, 16, true>;
		using InstCont = pr::vector<BaseInstance const*, 1024, false>;
		//using RayCastStepPtr = std::unique_ptr<RayCastStep>;
		
		Window*          m_wnd;          // The controlling window
		SceneCamera      m_cam;          // Represents the camera properties used to project onto the screen
		Viewport         m_viewport;     // Represents the rectangular area on the back buffer that this scene covers (modify this variable if you want. Use the methods tho. Remember clip regions)
		InstCont         m_instances;    // Instances added to this scene for rendering.
		RenderStepCont   m_render_steps; // The stages of rendering the scene
		Colour           m_bkgd_colour;  // The background colour for the scene. Set to ColourZero to disable clear bb
		//RayCastStepPtr m_ht_immediate;  // A ray cast render step for performing immediate hit tests
		Light            m_global_light;  // The global light settings
		TextureCubePtr   m_global_envmap; // A global environment map
		PipeStates       m_pso;           // Scene-wide pipe state overrides
		AutoSub          m_eh_resize;     // RT resize event handler subscription

		Scene(Window& wnd, std::initializer_list<ERenderStep> rsteps = {ERenderStep::RenderForward}, SceneCamera const& cam = SceneCamera{});
		~Scene();

		// Renderer access
		ID3D12Device4* d3d() const;
		Renderer& rdr() const;
		Window& wnd() const;
		ResourceManager& res() const;

		// Clear/Populate the drawlists for each render step.
		// Drawlists can be used in two ways, one is to clear the draw sets with each frame
		// and rebuild them from scratch (useful for scenes that change frequently).
		// The other is to NOT clear the draw sets and add/remove instances between frames.
		void ClearDrawlists();

		// Add an instance. The instance must be resident for the entire time that it is
		// in the scene, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
		// This method will add the instance to all render steps for which the model has appropriate nuggets.
		// Instances can be added to render steps directly if finer control is needed
		template <InstanceType Inst>
		void AddInstance(Inst const& inst)
		{
			AddInstance(inst.m_base);
		}

		// Remove an instance from the drawlist
		template <InstanceType Inst>
		void RemoveInstance(Inst const& inst)
		{
			RemoveInstance(inst.m_base);
		}

		// Raised just before the drawlist is sorted. Handlers should add/remove
		// instances from the scene, or add/remove render steps as required.
		EventHandler<Scene&, EmptyArgs const&> OnUpdateScene;

		// Set the render steps to use for rendering the scene
		void SetRenderSteps(std::span<ERenderStep const> rsteps);

		// Access the render step by type
		template <RenderStepType TRenderStep>
		TRenderStep const* FindRStep() const
		{
			return static_cast<TRenderStep const*>(FindRStep(TRenderStep::Id));
		}
		template <RenderStepType TRenderStep>
		TRenderStep* FindRStep()
		{
			return static_cast<TRenderStep*>(FindRStep(TRenderStep::Id));
		}

		// Enable/Disable shadow casting
		void ShadowCasting(bool enable, int shadow_map_size);

		#if 0 // todo
		// Perform an immediate hit test on the instances provided by coroutine 'instances'
		// Successive calls to 'instances' should return instances to be hit tested. Return nullptr when complete.
		void HitTest(HitTestRay const* rays, int count, float snap_distance, EHitTestFlags flags, RayCastStep::Instances instances, RayCastStep::ResultsOut const& results);

		// Set the collection of rays to cast into the scene for continuous hit testing.
		// Setting a non-zero number of rays enables a RayCast render step. Zero rays disables.
		void HitTestContinuous(HitTestRay const* rays, int count, float snap_distance, EHitTestFlags flags, RayCastStep::InstFilter const& include);
			
		// Read the hit test results from the continuous ray cast render step.
		void HitTestGetResults(RayCastStep::ResultsOut const& results);

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

		// Some render step pre-sets
		static std::vector<ERenderStep> ForwardRendering()
		{
			return {ERenderStep::ForwardRender};
		}
		static std::vector<ERenderStep> DeferredRendering()
		{
			return {ERenderStep::GBuffer, ERenderStep::DSLighting};
		}
		#endif

	private:

		friend struct Window;

		// Return a render step from this scene (if present)
		RenderStep const* FindRStep(ERenderStep id) const;
		RenderStep* FindRStep(ERenderStep id);

		// Add/Remove an instance from this scene
		void AddInstance(BaseInstance const& inst);
		void RemoveInstance(BaseInstance const& inst);

		// Render the scene
		pr::vector<ID3D12CommandList*> Render(BackBuffer& bb);

		// Resize the viewport on back buffer resize
		void HandleBackBufferSizeChanged(Window& wnd, BackBufferSizeChangedEventArgs const& evt);
	};
}

