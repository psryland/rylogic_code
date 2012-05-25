//***********************************************************************************
//
// Viewport - A place where rendererables get drawn
//
//***********************************************************************************
#ifndef PR_RDR_VIEWPORT_H
#define PR_RDR_VIEWPORT_H

#include "PR/Common/Chain.h"
#include "PR/Maths/Maths.h"
#include "PR/Renderer/D3DHeaders.h"
#include "PR/Renderer/Settings.h"
#include "PR/Renderer/Forward.h"
#include "PR/Renderer/VertexFormat.h"
#include "PR/Renderer/RenderNugget.h"
#include "PR/Renderer/Models/RenderableBase.h"
#include "PR/Renderer/Instance.h"
#include "PR/Renderer/DrawListElement.h"
#include "PR/Renderer/Drawlist.h"

namespace pr
{
	namespace rdr
	{
		class Viewport : public pr::chain::link<Viewport, RendererViewportChain>
		{
		public:
			Viewport(const VPSettings& settings);
			~Viewport();

			//******************************************
			// Reset the drawlist to empty. 
			void ClearDrawlist()												{ m_drawlist.Clear(); }
			// Add an instance derived from rdr::InstanceBase. The instance
			// must be resident for the entire time that it is in the drawlist,
			// i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
			void AddInstance(const InstanceBase& instance)						{ m_drawlist.AddInstance(instance); }
			// Remove an instance from the drawlist
			void RemoveInstance(const InstanceBase& instance)					{ m_drawlist.RemoveInstance(instance); }
			//******************************************
			
			// Rendering methods
			const Renderer&	GetRenderer() const									{ return *m_settings.m_renderer; }
			const m4x4&		GetWorldToCamera() const							{ return m_settings.m_world_to_camera; }
			const m4x4&		GetCameraToScreen() const							{ return m_settings.m_camera_to_screen; }
			uint			GetRenderState(D3DRENDERSTATETYPE type) const		{ return m_render_state[type].m_state; }
			const FRect&	GetViewportRect() const								{ return m_settings.m_viewport_rect; }
			void			SetWorldToCamera(const m4x4& matrix)				{ m_settings.m_world_to_camera = matrix; }
			void			SetCameraToScreen(const m4x4& matrix)				{ m_settings.m_camera_to_screen = matrix; }
			void			SetRenderState(D3DRENDERSTATETYPE type, uint state)	{ m_render_state.SetRenderState(type, state); }
			void			SetViewportRect(const FRect& viewport_rect);
			void			Render();

		private:
			// Renderer only methods
			friend class Renderer;
			HRESULT			CreateDeviceDependentObjects();
			void			ReleaseDeviceDependentObjects();
			void			RenderDrawListElement(const DrawListElement& element);

			// RenderStateManager only methods
			friend class RenderStateManager;
			
		private:
			VPSettings			m_settings;					// Settings for the viewport
			D3DVIEWPORT9		m_d3d_viewport;				// The viewport in screen co-ords
			RenderStateBlock	m_render_state;				// The viewport render states.
			Drawlist			m_drawlist;					// The thing that tells us what to draw
		};
	}//namespace rdr
}//namespace pr

#endif//PR_RDR_VIEWPORT_H
