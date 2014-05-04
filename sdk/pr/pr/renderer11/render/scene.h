//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/render/render_step.h"
#include "pr/renderer11/util/event_types.h"
//#include "pr/renderer11/render/drawlist.h"
//#include "pr/renderer11/render/gbuffer.h"
//#include "pr/renderer11/render/scene_view.h"
//#include "pr/renderer11/render/blend_state.h"
//#include "pr/renderer11/render/raster_state.h"
//#include "pr/renderer11/lights/light.h"

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
			pr::Renderer*  m_rdr;          // The controlling renderer
			SceneView      m_view;         // Represents the camera properties used to project onto the screen
			Viewport       m_viewport;     // Represents the rectangular area on the back buffer that this scene covers
			RenderStepCont m_render_steps; // The stages of rendering the scene
			pr::Colour     m_bkgd_colour;  // The background colour for the scene
			Light          m_global_light; // The global light settings

			Scene(pr::Renderer& rdr, SceneView const& view = SceneView());

			// Get/Set the view (i.e. the camera to screen projection or 'View' matrix in dx speak)
			void SetView(SceneView const& view) { m_view = view; }
			void SetView(pr::Camera const& cam) { SetView(SceneView(cam)); }

			// Get/Set the on-screen visible area of this scene when rendered
			Viewport const& Viewport() const                 { return m_viewport; }
			void Viewport(pr::rdr::Viewport const& viewport) { m_viewport = viewport; }

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
				PR_ASSERT(PR_DBG_RDR, false, Fmt("RenderStep %s is not part of this scene", ERenderStep::ToString(TRStep::Id)).c_str());
				throw std::exception("Render step not part of this scene");
			}

			// Clear/Populate the drawlists for each render step.
			// Drawlists can be used in two ways, one is to clear the drawsets with each frame
			// and rebuild them from scratch (useful for scenes that change frequently).
			// The other is to NOT clear the drawsets and add/remove instances between frames
			void ClearDrawlists();
			void UpdateDrawlists();

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

		//	// Note: scenes are copyable
		//	pr::Renderer*        m_rdr;               // The renderer
		//	pr::rdr::Viewport    m_viewport;          // Represents the rectangular area on screen
		//	pr::rdr::SceneView   m_view;              // Represents the camera properties used to project onto the screen
		//	pr::rdr::Drawlist    m_drawlist;          // The list of elements to draw
		//	pr::Colour           m_background_colour; // The colour to clear the background to
		//	Light                m_global_light;      // The global light to use
		//	D3DPtr<ID3D11Buffer> m_cbuf_frame;        // A constant buffer for the frame constant shader variables
		//	BSBlock              m_bsb;               // Blend states for the scene
		//	RSBlock              m_rsb;               // Raster states for the scene
		//	DSBlock              m_dsb;               // Depth buffer states for the scene
		//	std::shared_ptr<Stereo> m_stereo;         // Helper used when renderering in stereoscopic mode

		//	Scene(pr::Renderer& rdr, SceneView const& view = SceneView());

		//	// Get/Set the view (i.e. the camera to screen projection or 'View' matrix in dx speak)
		//	void SetView(SceneView const& view) { m_view = view; }
		//	void SetView(pr::Camera const& cam) { SetView(pr::rdr::SceneView(cam)); }

		//	// Set stereoscopic rendering mode
		//	bool Stereoscopic() const { return m_stereo != nullptr; }
		//	void Stereoscopic(bool state, float eye_separation, bool swap_eyes);

		//	// Get/Set the on-screen visible area of this scene when rendered
		//	pr::rdr::Viewport const& Viewport() const        { return m_viewport; }
		//	void Viewport(pr::rdr::Viewport const& viewport) { m_viewport = viewport; }

		//protected:
		//	// Resize the viewport on back buffer resize
		//	void OnEvent(pr::rdr::Evt_Resize const& evt) override;

		//	// Implementation of rendering for the derived scene type
		//	virtual void DoRender(D3DPtr<ID3D11DeviceContext>& dc) const = 0;
		//};

		//// A scene rendered using forward rendering techniques
		//struct SceneForward :Scene
		//{
		//	SceneForward(pr::Renderer& rdr, SceneView const& view = SceneView());

		//private:
		//	// Implementation of rendering for the derived scene type
		//	void DoRender(D3DPtr<ID3D11DeviceContext>& dc) const;
		//};

		//// A scene rendered using deferred rendering techniques
		//struct SceneDeferred :Scene
		//{
		//	pr::rdr::GBuffer m_gbuffer; // GBuffer containing visible scene geometry

		//	SceneDeferred(pr::Renderer& rdr, SceneView const& view = SceneView());

		//	// Implementation of rendering for the derived scene type
		//	void DoRender(D3DPtr<ID3D11DeviceContext>& dc) const;
		//};
	}
}
