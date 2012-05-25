//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_VIEWPORT_H
#define PR_RDR_VIEWPORT_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/configuration/settings.h"
#include "pr/renderer/viewport/drawlist.h"
#include "pr/renderer/renderstates/renderstate.h"

#ifndef PR_ASSERT
#	define PR_ASSERT_DEFINED
#	define PR_ASSERT(grp, exp)
#endif

namespace pr
{
	namespace rdr
	{
		class Viewport
			:public pr::chain::link<Viewport, viewport::RdrViewportChain>
			,public pr::events::IRecv<pr::rdr::Evt_DeviceLost>
			,public pr::events::IRecv<pr::rdr::Evt_DeviceRestored>
		{
			VPSettings   m_settings;      // Settings for the viewport
			D3DVIEWPORT9 m_d3d_viewport;  // The viewport in screen co-ords
			rs::Block    m_render_state;  // The viewport render states.
			Drawlist     m_drawlist;      // The thing that tells us what to draw
			
			void OnEvent(pr::rdr::Evt_DeviceLost const&)     {}
			void OnEvent(pr::rdr::Evt_DeviceRestored const&) { ViewRect(ViewRect()); }
			void RenderDrawListElement(const DrawListElement& element);
			void GenerateShadowMap();
			
		public:
			Viewport(const VPSettings& settings);
			~Viewport();
			
			// Return the unique id for this viewport
			ViewportId GetViewportId() const                                        { return m_settings.m_identifier; }
			
			// Reset the drawlist to empty. 
			void ClearDrawlist()                                                    { m_drawlist.Clear(); }
			
			// Add an instance. The instance must be resident for the entire
			// time that it is in the drawlist, i.e. until 'RemoveInstance'
			// or 'ClearDrawlist' is called.
			void AddInstance(instance::Base const& instance)                        { m_drawlist.AddInstance(instance); }
			template <typename Inst> void AddInstance(Inst const& instance)         { m_drawlist.AddInstance(instance.m_base); }
			
			// Remove an instance from the drawlist
			void RemoveInstance(instance::Base const& instance)                     { m_drawlist.RemoveInstance(instance); }
			template <typename Inst> void RemoveInstance(Inst const& instance)      { m_drawlist.RemoveInstance(instance.m_base); }
			
			// Send the drawlist
			void Render(bool clear_bb, rs::Block const& rsb_override);
			void Render(bool clear_bb)                                              { Render(clear_bb, rs::Block()); }
			void Render()                                                           { Render(true, rs::Block()); }
			
			// Set the view (i.e. the camera to screen projection or 'View' matrix in dx speak)
			void SetView(float fovY, float aspect, float centre_dist, bool orthographic);
			void SetView(pr::Camera const& cam)                                     { SetView(cam.m_fovY, cam.m_aspect, cam.m_focus_dist, cam.m_orthographic); CameraToWorld(cam.CameraToWorld()); }
			
			// Rendering methods
			Renderer const&     Rdr() const                                         { return *m_settings.m_renderer; }
			Renderer&           Rdr()                                               { return *m_settings.m_renderer; }
			m4x4 const&         CameraToScreen() const                              { return m_settings.m_camera_to_screen; }
			m4x4 const&         CameraToWorld() const                               { return m_settings.m_camera_to_world; }
			void                CameraToWorld(m4x4 const& c2w)                      { PR_ASSERT(PR_DBG, FEql(c2w.w.w,1.0f), ""); m_settings.m_camera_to_world = c2w; }
			m4x4                WorldToCamera() const                               { return GetInverseFast(m_settings.m_camera_to_world); }
			float               CentreDist() const                                  { return m_settings.m_centre_dist; }
			v4                  Centre() const                                      { m4x4 const& c2w = CameraToWorld(); return c2w.pos - c2w.z * CentreDist(); }
			FRect const&        ViewRect() const                                    { return m_settings.m_view_rect; }
			void                ViewRect(FRect const& rect);
			pr::Frustum         Frustum() const                                     { return pr::Frustum::makeFA(m_settings.m_fovY, m_settings.m_aspect, m_settings.FarPlane()); }
			pr::Frustum         ShadowFrustum() const                               { return pr::Frustum::makeFA(m_settings.m_fovY, m_settings.m_aspect, m_settings.m_centre_dist * 2.0f); }
			m4x4                WorldToFrustum() const                              { return pr::Translation(0,0,m_settings.FarPlane()) * WorldToCamera(); }
			uint                RenderState(D3DRENDERSTATETYPE type) const          { return m_render_state[type].m_state; }
			void                RenderState(D3DRENDERSTATETYPE type, uint state)    { m_render_state.SetRenderState(type, state); }
		};

		// Helper object for performing viewport actions on multiple viewports
		class ViewportGroup
		{
			typedef pr::Array<Viewport*,4> ViewCont;
			ViewCont m_views;

		public:
			void Add(Viewport& viewport) { m_views.push_back(&viewport); }

			#define ForEachView(action) for (ViewCont::iterator i = m_views.begin(), iend = m_views.end(); i != iend; ++i) { (*i)->action; }
			void ClearDrawlist()                                                         { ForEachView(ClearDrawlist()); }
			void AddInstance(instance::Base const& instance)                             { ForEachView(AddInstance(instance)); }
			void RemoveInstance(instance::Base const& instance)                          { ForEachView(RemoveInstance(instance)); }
			template <typename Inst> void AddInstance(Inst const& instance)              { ForEachView(AddInstance(instance)); }
			template <typename Inst> void RemoveInstance(Inst const& instance)           { ForEachView(RemoveInstance(instance)); }
			void Render(bool clear_bb, rs::Block const& rsb_override)                    { ForEachView(Render(clear_bb, rsb_override)); }
			void Render(bool clear_bb)                                                   { ForEachView(Render(clear_bb)); }
			void Render()                                                                { ForEachView(Render()); }
			void SetView(float fovY, float aspect, float centre_dist, bool orthographic) { ForEachView(SetView(fovY, aspect, centre_dist, orthographic)); }
			void SetView(pr::Camera const& cam)                                          { ForEachView(SetView(cam)); }
			void CameraToWorld(m4x4 const& c2w)                                          { ForEachView(CameraToWorld(c2w)); }
			void ViewRect(FRect const& rect)                                             { ForEachView(ViewRect(rect)); }
			void RenderState(D3DRENDERSTATETYPE type, uint state)                        { ForEachView(RenderState(type, state)); }
			#undef ForEachView 
		};
	}
}

#ifdef PR_ASSERT_DEFINED
#	undef PR_ASSERT_DEFINED
#	undef PR_ASSERT
#endif

#endif
