//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_RENDERER_H
#define PR_RDR_RENDERER_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/configuration/settings.h"
#include "pr/renderer/renderstates/renderstate.h"
#include "pr/renderer/renderstates/stackframes.h"
#include "pr/renderer/vertexformats/vertexformatmanager.h"
#include "pr/renderer/renderstates/renderstatemanager.h"
#include "pr/renderer/lighting/lightingmanager.h"
#include "pr/renderer/materials/material_manager.h"
#include "pr/renderer/models/modelmanager.h"
#include "pr/renderer/utility/globalfunctions.h"

namespace pr
{
	class Renderer
		:public pr::events::IRecv<pr::rdr::Evt_DeviceLost>
		,public pr::events::IRecv<pr::rdr::Evt_DeviceRestored>
	{
		typedef chain::head<rdr::Viewport, rdr::viewport::RdrViewportChain> TViewportChain;
		typedef pr::Imposter<rdr::rs::stack_frame::RSB> RSBStackFrameImpost;

		rdr::RdrSettings          m_settings;
		D3DPtr<IDirect3D9>        m_d3d;
		D3DPRESENT_PARAMETERS     m_pp;
		D3DPtr<IDirect3DDevice9>  m_d3d_device;
		D3DPtr<IDirect3DSurface9> m_back_buffer;
		D3DPtr<IDirect3DSurface9> m_depth_buffer;
		TViewportChain            m_viewport;                     // All the viewports that we know about
		rdr::rs::Block            m_global_render_states;         // Global render state changes
		RSBStackFrameImpost       m_global_rsb_sf;                // A stack frame for the global render states
		rdr::EState::Type         m_rendering_phase;              // The phase of rendering that the renderer is in
		bool                      m_device_lost;                  // True while we've lost the device

		void OnEvent(pr::rdr::Evt_DeviceLost const&);
		void OnEvent(pr::rdr::Evt_DeviceRestored const&);
		rdr::EResult::Type TestCooperativeLevel();
		rdr::EResult::Type ResetDevice();

		friend class rdr::Viewport;
		void RegisterViewport  (rdr::Viewport& viewport);
		void UnregisterViewport(rdr::Viewport& viewport);
		void ClearBackBuffer();

	public:                                                       // Manager classes. These form part of the interface to the renderer
		rdr::VertexFormatManager  m_vert_mgr;               // The thing that declares the vertex types
		rdr::RenderStateManager   m_rdrstate_mgr;         // The thing that manages the state of d3d during rendering
		rdr::LightingManager      m_light_mgr;             // The thing that remembers the state of lights
		rdr::MaterialManager      m_mat_mgr;             // The thing that creates and loads materials
		rdr::ModelManager         m_mdl_mgr;                // The thing that creates and loads models

		Renderer(const rdr::RdrSettings& settings);
		void Resize(const IRect& client_area);
		
		// Accessor methods
		pr::rdr::IAllocator*     Allocator()                                      { return m_settings.m_allocator; }
		D3DPtr<IDirect3D9>       D3DInterface() const                             { return m_d3d; }
		D3DPtr<IDirect3DDevice9> D3DDevice() const                                { return m_d3d_device; }
		D3DPRESENT_PARAMETERS    PP() const                                       { return m_pp; }
		pr::rdr::EState::Type    RenderingPhase() const                           { return m_rendering_phase; }
		HWND                     HWnd() const                                     { return m_settings.m_window_handle; }
		pr::IRect                ClientArea() const                               { return m_settings.m_client_area; }
		pr::Colour32             BackgroundColour() const                         { return m_settings.m_background_colour; }
		void                     BackgroundColour(Colour32 colour)                { m_settings.m_background_colour = colour; }
		pr::uint                 RenderState(D3DRENDERSTATETYPE type) const       { return m_global_render_states[type].m_state; }
		void                     RenderState(D3DRENDERSTATETYPE type, uint state) { m_global_render_states.SetRenderState(type, state); }
		void                     AntiAliasingLevel(rdr::EQuality::Type quality)   { m_settings.m_geometry_quality = quality; m_pp.MultiSampleType = rdr::GetAntiAliasingLevel(m_d3d, m_settings.m_device_config, m_pp.BackBufferFormat, quality); }
		pr::rdr::EQuality::Type  GeometryQuality() const                          { return m_settings.m_geometry_quality; }
		pr::rdr::EQuality::Type  TextureQuality() const                           { return m_settings.m_texture_quality; }

		// Rendering:
		//	1) Call RenderStart() - if render start returns EResult_Success then continue to
		//		build the scene.
		//	2) Call Viewport::Render() on each viewport you wish to draw
		//	3) Then call RenderEnd() to finish off the scene.
		//	4) Call Present() to present the scene to the display.
		// From DirectX docs: To enable maximal parallelism between the CPU and the graphics
		// accelerator, it is advantageous to call RenderEnd() as far ahead of calling Present()
		// as possible. 'BltBackBuffer()' can be used to redraw the display from the last back
		// buffer but this only works for D3DSWAPEFFECT_COPY.
		rdr::EResult::Type RenderStart();
		void               RenderEnd();
		rdr::EResult::Type Present();
		rdr::EResult::Type BltBackBuffer();
	};
}

#endif

