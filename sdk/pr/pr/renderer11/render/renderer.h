//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_RENDERER_H
#define PR_RDR_RENDER_RENDERER_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/config/config.h"
#include "pr/renderer11/render/gbuffer.h"
#include "pr/renderer11/models/model_manager.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/textures/texture_manager.h"
#include "pr/renderer11/render/raster_state_manager.h"
#include "pr/renderer11/util/allocator.h"

namespace pr
{
	namespace rdr
	{
		// Settings for constructing the renderer
		struct RdrSettings
		{
			pr::rdr::MemFuncs            m_mem;                // The manager of allocations/deallocations
			HWND                         m_hwnd;               // The associated window
			BOOL                         m_windowed;           // Windowed mode or full screen
			DisplayMode                  m_mode;               // Display mode to use (note: must be valid for the adapter, use FindClosestMatchingMode if needed)
			MultiSamp                    m_multisamp;          // AA/Multisampling
			UINT                         m_buffer_count;       // Number of buffers in the chain, 1 = front only, 2 = front and back, 3 = triple buffering, etc
			DXGI_USAGE                   m_usage;              // Render target, shader resource view
			DXGI_SWAP_EFFECT             m_swap_effect;        // How to swap the back buffer to the front buffer
			UINT                         m_swap_chain_flags;   // Options to allow GDI and DX together (see DXGI_SWAP_CHAIN_FLAG)
			D3DPtr<IDXGIAdapter>         m_adapter;            // The adapter to use. 0 means use the default
			D3D_DRIVER_TYPE              m_driver_type;        // HAL, REF, etc
			UINT                         m_device_layers;      // Add layers over the basic device (see D3D11_CREATE_DEVICE_FLAG)
			pr::Array<D3D_FEATURE_LEVEL> m_feature_levels;     // Features to support. Empty implies 9.1 -> 11.0
			UINT                         m_vsync;              // Present SyncInterval value
			
			RdrSettings(HWND hwnd = 0, BOOL windowed = TRUE, pr::iv2 const& client_area = pr::iv2::make(1024,768))
			:m_mem()
			,m_hwnd(hwnd)
			,m_windowed(windowed)
			,m_mode(client_area)
			,m_multisamp()
			,m_buffer_count(2)
			,m_usage(DXGI_USAGE_RENDER_TARGET_OUTPUT)
			,m_swap_effect(DXGI_SWAP_EFFECT_SEQUENTIAL)
			,m_swap_chain_flags(0)
			,m_adapter(0)
			,m_driver_type(D3D_DRIVER_TYPE_HARDWARE)
			,m_device_layers(0)
			,m_feature_levels()
			,m_vsync(1)
			{
				// Notes:
				// - vsync has different meaning for the swap effect modes.
				//   BitBlt modes: 0 = present immediately, 1,2,3,.. present after the nth vertical blank (has the effect of locking the frame rate to a fixed multiple of the vsync rate)
				//   Flip modes (Sequential): 0 = drop this frame if there is a new frame waiting, n > 0 = same as bitblt case
				// Add the debug layer in debug mode
				PR_EXPAND(PR_DBG_RDR, m_device_layers = D3D11_CREATE_DEVICE_DEBUG);
			}
		};

		// Renderer state variables
		struct RdrState
		{
			pr::rdr::RdrSettings           m_settings;
			D3D_FEATURE_LEVEL              m_feature_level;
			D3DPtr<ID3D11Device>           m_device;
			D3DPtr<IDXGISwapChain>         m_swap_chain;
			D3DPtr<ID3D11DeviceContext>    m_immediate;
			D3DPtr<ID3D11RenderTargetView> m_main_rtv;
			D3DPtr<ID3D11DepthStencilView> m_main_dsv;
			pr::rdr::ERenderMethod::Type   m_rdr_method;
			pr::iv2                        m_area;            // The size of the back buffer
			bool                           m_idle;            // True while the window is occluded
		
			RdrState(pr::rdr::RdrSettings const& settings);
			virtual ~RdrState() {}
			void InitMainRT();
		};
	}
	
	// The main renderer object
	class Renderer :rdr::RdrState
	{
	public:
		// These manager classes from part of the public interface of the renderer
		pr::rdr::ModelManager       m_mdl_mgr;
		pr::rdr::ShaderManager      m_shdr_mgr;
		pr::rdr::TextureManager     m_tex_mgr;
		pr::rdr::RasterStateManager m_rs_mgr;

		Renderer(pr::rdr::RdrSettings const& settings);
		~Renderer();
		
		// Return the d3d device
		D3DPtr<ID3D11Device> Device() const { return m_device; }
		
		// Return the immediate device context
		D3DPtr<ID3D11DeviceContext> ImmediateDC() const { return m_immediate; }
		
		// Create a new deferred device context
		D3DPtr<ID3D11DeviceContext> DeferredDC() const { return 0; };
		
		// Returns an allocator object suitable for allocating instances of 'T'
		template <class Type> pr::rdr::Allocator<Type> Allocator() const { return pr::rdr::Allocator<Type>(m_settings.m_mem); }
		
		// Returns the size of the displayable area as known by the renderer
		pr::iv2 DisplayArea() const;
		
		// Call when the window size changes (e.g. from a WM_SIZE message)
		void Resize(pr::iv2 const& size);
		
		// Rendering:
		//  For each scene to be rendered:
		//     Build/Update the draw list for that scene
		//     Set the scene viewport
		//     Render the drawlist
		//
		// Rendering a Drawlist:
		//   Deferred using: http://www.catalinzima.com/tutorials/deferred-rendering-in-xna/creating-the-g-buffer/
		//
		// Drawlist Order:
		//   opaques
		//   skybox
		//   alphas
		//
		// Observations:
		//   Only immediate context needed for normal rendering,
		//   Deferred context might be useful for generating shadow data (dunno yet)
		//
		// Call Present() to present the scene to the display.
		//   From DirectX docs: To enable maximal parallelism between the CPU and the graphics
		//   accelerator, it is advantageous to call RenderEnd() as far ahead of calling Present()
		//   as possible. 'BltBackBuffer()' can be used to redraw the display from the last back
		//   buffer but this only works for D3DSWAPEFFECT_COPY.
		void Present();
	};
}

#endif
