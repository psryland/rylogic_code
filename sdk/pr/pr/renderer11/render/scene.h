//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_SCENE_H
#define PR_RDR_RENDER_SCENE_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/drawlist.h"
#include "pr/renderer11/render/gbuffer.h"
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/util/wrappers.h"

namespace pr
{
	namespace rdr
	{
		// A scene is a view into the 3D world that can be rendered. Typically most applications only have
		// one scene however, examples of multiple scenes are: the rear vision mirror in a car, or two views
		// of the same objects in a magic-eye style stereo view.
		// A scene has a viewport which defines the area of the window that the scene draws into.
		// A scene contains a drawlist
		struct Scene
			:pr::events::IRecv<pr::rdr::Evt_Resize>
		{
			pr::Renderer*        m_rdr;               // The renderer
			pr::rdr::Viewport    m_viewport;          // Represents the rectangular area on screen
			pr::rdr::SceneView   m_view;              // Represents the camera properties used to project onto the screen
			pr::rdr::Drawlist    m_drawlist;          // The list of elements to draw
			pr::Colour           m_background_colour; // The colour to clear the background to
			D3DPtr<ID3D11Buffer> m_cbuf_frame;        // A constant buffer for the frame constant shader variables
			
			Scene(pr::Renderer& rdr, SceneView const& view = SceneView());
			
			// Get/Set the view (i.e. the camera to screen projection or 'View' matrix in dx speak)
			void SetView(SceneView const& view) { m_view = view; }
			void SetView(pr::Camera const& cam) { SetView(pr::rdr::SceneView(cam)); }
			
			// Get/Set the on-screen visible area of this scene when rendered
			pr::rdr::Viewport const& Viewport() const        { return m_viewport; }
			void Viewport(pr::rdr::Viewport const& viewport) { m_viewport = viewport; }
			
			// Drawlist management
			// Drawlists can be used in two ways, one is to clear the drawset with each frame
			// and rebuild it from scratch (useful for scenes that change frequently), the other
			// is to NOT clear the drawset and add and remove instances between frames
			void ClearDrawlist()  { m_drawlist.Clear(); }
			void UpdateDrawlist() { pr::events::Send(pr::rdr::Evt_SceneRender(this)); m_drawlist.SortIfNeeded(); }
			
			// Add an instance. The instance must be resident for the entire time that it is
			// in the drawlist, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
			void AddInstance(BaseInstance const& inst)                     { m_drawlist.Add(inst); }
			template <typename Inst> void AddInstance(Inst const& inst)    { m_drawlist.Add(inst.m_base); }
			
			// Remove an instance from the drawlist
			void RemoveInstance(BaseInstance const& inst)                  { m_drawlist.Remove(inst); }
			template <typename Inst> void RemoveInstance(Inst const& inst) { m_drawlist.Remove(inst.m_base); }
			
			// Render the current drawlist into 'dc'
			void Render(bool clear_bb = true) const;
			void Render(D3DPtr<ID3D11DeviceContext>& dc, bool clear_bb = true) const;
		
		protected:
			// Resize the viewport on back buffer resize
			void OnEvent(pr::rdr::Evt_Resize const& evt);
			
			// Implementation of rendering for the derived scene type
			virtual void DoRender(D3DPtr<ID3D11DeviceContext>& dc, bool clear_bb) const = 0;
			
			// Setup the input assembler for a drawlist element
			void SetupIA(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle) const;
		};
		
		
		// A scene rendered using forward rendering techniques
		struct SceneForward :Scene
		{
			SceneForward(pr::Renderer& rdr, SceneView const& view = SceneView());
			
		private:
			// Implementation of rendering for the derived scene type
			void DoRender(D3DPtr<ID3D11DeviceContext>& dc, bool clear_bb) const;
		};
		
		
		// A scene rendered using deferred rendering techniques
		struct SceneDeferred :Scene
		{
			pr::rdr::GBuffer m_gbuffer; // GBuffer containing visible scene geometry
			
			SceneDeferred(pr::Renderer& rdr, SceneView const& view = SceneView());
			
			// Implementation of rendering for the derived scene type
			void DoRender(D3DPtr<ID3D11DeviceContext>& dc, bool clear_bb) const;
		};
	}
}

#endif
