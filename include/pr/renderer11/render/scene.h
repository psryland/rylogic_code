//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/lights/light.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/raster_state.h"
#include "pr/renderer11/render/depth_state.h"
#include "pr/renderer11/util/stock_resources.h"
#include "pr/renderer11/util/event_types.h"
#include "pr/renderer11/util/wrappers.h"

namespace pr
{
	namespace rdr
	{
		// A scene is a view into the 3D world. Typically most applications only have
		// one scene however, examples of multiple scenes are: the rear vision mirror
		// in a car, map view, etc.
		// A scene contains an ordered collection of render steps.
		struct Scene
			:pr::events::IRecv<Evt_Resize>
			,pr::AlignTo<16>
		{
			// Fixed container of render steps. Doesn't really need to be fixed,
			// but non-fixed means we need the pr::rdr::Allocator to construct it.
			typedef pr::vector<RenderStepPtr, 16, true> RenderStepCont;

			Window*        m_wnd;          // The controlling window
			SceneView      m_view;         // Represents the camera properties used to project onto the screen
			Viewport       m_viewport;     // Represents the rectangular area on the back buffer that this scene covers
			RenderStepCont m_render_steps; // The stages of rendering the scene
			pr::Colour     m_bkgd_colour;  // The background colour for the scene
			Light          m_global_light; // The global light settings
			DSBlock        m_dsb;          // Scene-wide states
			RSBlock        m_rsb;          // Scene-wide states
			BSBlock        m_bsb;          // Scene-wide states

			Scene(Window& wnd, std::vector<ERenderStep>&& rsteps = {ERenderStep::ForwardRender}, SceneView const& view = SceneView());
			~Scene();

			// Set the render steps to use for rendering the scene
			void SetRenderSteps(std::vector<ERenderStep>&& rsteps);
			
			// Some render step presets
			static std::vector<ERenderStep> ForwardRendering() { return {ERenderStep::ForwardRender}; }
			static std::vector<ERenderStep> DeferredRendering() { return {ERenderStep::GBuffer, ERenderStep::DSLighting}; }

			// Get/Set the view (i.e. the camera to screen projection or 'View' matrix in dx speak)
			void SetView(SceneView const& view) { m_view = view; }
			void SetView(pr::Camera const& cam) { SetView(SceneView(cam)); }

			// Access the render step by Id
			RenderStep* FindRStep(ERenderStep::Enum_ id) const;
			RenderStep& operator[](ERenderStep::Enum_ id) const;

			// Render step specific accessors
			template <typename TRStep> typename std::enable_if<std::is_base_of<RenderStep,TRStep>::value, TRStep>::type* FindRStep() const
			{
				return static_cast<TRStep*>(FindRStep(TRStep::Id));
			}
			template <typename TRStep> TRStep& RStep() const
			{
				auto rs = FindRStep<TRStep>();
				if (rs != nullptr) return *rs;
				PR_ASSERT(PR_DBG_RDR, false, Fmt("RenderStep %s is not part of this scene", ERenderStep::ToStringA(TRStep::Id)).c_str());
				throw std::exception("Render step not part of this scene");
			}

			// Clear/Populate the drawlists for each render step.
			// Drawlists can be used in two ways, one is to clear the drawsets with each frame
			// and rebuild them from scratch (useful for scenes that change frequently).
			// The other is to NOT clear the drawsets and add/remove instances between frames
			void ClearDrawlists();
			void UpdateDrawlists();

			// Rendering multi-pass models:
			// To render a model that needs to be done in multiple passes, add addition nuggets to
			// the model that overlap with existing nuggets but have different render states/shaders
			// e.g. To render back faces first, then front faces: Add a nugget for the whole model
			// with front face culling, then another nugget for the whole model with back face culling.

			// Add an instance. The instance must be resident for the entire time that it is
			// in the scene, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
			// This method will add the instance to all render steps for which the model has appropriate nuggets.
			// Instances can be added to render steps directly if finer control is needed
			void AddInstance(BaseInstance const& inst);
			template <typename Inst> void AddInstance(Inst const& inst) { AddInstance(inst.m_base); }

			// Remove an instance from the drawlist
			void RemoveInstance(BaseInstance const& inst);
			template <typename Inst> void RemoveInstance(Inst const& inst) { RemoveInstance(inst.m_base); }

			// Render the scene
			void Render();

		private:

			// Resize the viewport on back buffer resize
			void OnEvent(Evt_Resize const& evt) override;
		};
	}
}
