//**********************************************************************************
//
// A DirectX Renderer
//
//**********************************************************************************
#ifndef PR_RENDERER_H
#define PR_RENDERER_H

#include "PR/Common/PRAssert.h"
#include "PR/Common/StdString.h"
#include "PR/Common/D3DPtr.h"
#include "PR/Common/Chain.h"
#include "PR/Maths/Maths.h"
#include "PR/Geometry/PRColour.h"
#include "PR/Renderer/RendererAssertEnable.h"
#include "PR/Renderer/Errors.h"
#include "PR/Renderer/D3DHeaders.h"
#include "PR/Renderer/VertexFormat.h"
#include "PR/Renderer/Attribute.h"
#include "PR/Renderer/Viewport.h"
#include "PR/Renderer/RenderState.h"
#include "PR/Renderer/LightingManager.h"
#include "PR/Renderer/RenderStateManager.h"
#include "PR/Renderer/Materials/MaterialManager.h"
#include "PR/Renderer/VertexFormat.h"
#include "PR/Renderer/Effects/EffectBase.h"
#include "PR/Renderer/Forward.h"
#include "PR/Renderer/Settings.h"

#ifndef NDEBUG
#pragma comment(lib, "RendererD.lib")
#else//NDEBUG
#pragma comment(lib, "Renderer.lib")
#endif//NDEBUG

namespace pr
{
	class Renderer
	{
	public:
		enum EState { EState_Idle, EState_BuildingScene, EState_PresentPending };

		Renderer(const rdr::RdrSettings& settings);
		void Resize(const IRect& client_area, const IRect& window_bounds);
		
		// Accessor methods
		D3DPtr<IDirect3D9>			GetD3DInterface() const									{ return m_d3d; }
		D3DPtr<IDirect3DDevice9>	GetD3DDevice() const									{ return m_d3d_device; }
		IRect						GetClientArea() const									{ return m_settings.m_client_area; }
		IRect						GetWindowBounds() const									{ return m_settings.m_window_bounds; }
		Colour32					GetBackgroundColour() const								{ return m_settings.m_background_colour; }
		void						SetBackgroundColour(Colour32 colour)					{ m_settings.m_background_colour = colour; }
		void						SetRenderState(D3DRENDERSTATETYPE type, uint state)		{ m_global_render_states.SetRenderState(type, state); }
		uint						GetRenderState(D3DRENDERSTATETYPE type) const			{ return m_global_render_states[type].m_state; }
		uint						GetCurrentRenderState(D3DRENDERSTATETYPE type) const	{ return m_render_state_manager.GetCurrentRenderState(type); }
		const rdr::RendererState&	GetCurrentState() const									{ return m_render_state_manager.GetCurrentState(); }
		
		// Rendering - Call RenderStart(), RenderViewport(), RenderEnd() to build the scene.
		// Call Render() to present the scene to the display.
		// To enable maximal parallelism between the CPU and the graphics accelerator, it is
		// advantageous to call RenderEnd() as far ahead of calling Render() as possible.
		// 'BltBackBuffer()' can be used to redraw the display from the last back buffer
		// but this only works for D3DSWAPEFFECT_COPY.
		rdr::EResult	RenderStart();
		void			RenderViewport(rdr::Viewport& viewport);
		void			RenderEnd();
		rdr::EResult	Render();
		rdr::EResult	BltBackBuffer();
		void			ClearBackBuffer();

		// Manager Interface Access
		      rdr::MaterialManager&	GetMaterialManager()		{ return m_material_manager; }
		const rdr::MaterialManager&	GetMaterialManager() const	{ return m_material_manager; }
		      rdr::LightingManager&	GetLightingManager()		{ return m_lighting_manager; }
		const rdr::LightingManager&	GetLightingManager() const	{ return m_lighting_manager; }
		//rdr::Light& GetLight(uint which) { return m_lighting_manager.GetLight(which); }

	private:
		void CreateDeviceDependentObjects();
		void ReleaseDeviceDependentObjects();
		rdr::EResult TestCooperativeLevel();
		rdr::EResult ResetDevice();
		
		// Viewport only methods
		friend class rdr::Viewport;
		void RegisterViewport  (rdr::Viewport& viewport);
		void UnregisterViewport(rdr::Viewport& viewport);
		
	private:
		typedef chain::head<rdr::Viewport, rdr::RendererViewportChain> TViewportChain;

		rdr::RdrSettings			m_settings;
		D3DPtr<IDirect3D9>			m_d3d;
		D3DPRESENT_PARAMETERS		m_pp;
		D3DPtr<IDirect3DDevice9>	m_d3d_device;
		D3DPtr<IDirect3DSurface9>	m_back_buffer;		
		D3DPtr<IDirect3DSurface9>	m_depth_buffer;
		TViewportChain				m_viewport;					// All the viewports that we know about
		uint						m_clear_flags;				// Whether to clear the back and depth buffers
		EState						m_renderer_state;			// The phase of rendering that the renderer is in
		bool						m_device_lost;				// True while we've lost the device
		rdr::RenderStateBlock		m_global_render_states;		// Global render state changes
		rdr::vf::Manager			m_vertex_manager;			// The thing that declares the vertex types
		rdr::RenderStateManager		m_render_state_manager;		// The thing that manages the state of d3d during rendering
		rdr::LightingManager		m_lighting_manager;			// The thing that remembers the state of lights
		rdr::MaterialManager		m_material_manager;			// The thing that knows about loaded materials
	};
}//namespace pr

#endif//PR_RENDERER_H

