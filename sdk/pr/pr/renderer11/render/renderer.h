//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_RENDERER_H
#define PR_RDR_RENDER_RENDERER_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/config/config.h"
#include "pr/renderer11/models/model_manager.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/textures/texture_manager.h"
#include "pr/renderer11/textures/text_manager.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/depth_state.h"
#include "pr/renderer11/render/raster_state.h"
#include "pr/renderer11/util/allocator.h"

namespace pr
{
	namespace rdr
	{
		// Settings for constructing the renderer
		struct RdrSettings
		{
			MemFuncs                     m_mem;              // The manager of allocations/deallocations
			HWND                         m_hwnd;             // The associated window
			BOOL                         m_windowed;         // Windowed mode or full screen
			DisplayMode                  m_mode;             // Display mode to use (note: must be valid for the adapter, use FindClosestMatchingMode if needed)
			MultiSamp                    m_multisamp;        // Number of samples per pixel (AA/Multisampling)
			UINT                         m_buffer_count;     // Number of buffers in the chain, 1 = front only, 2 = front and back, 3 = triple buffering, etc
			DXGI_SWAP_EFFECT             m_swap_effect;      // How to swap the back buffer to the front buffer
			UINT                         m_swap_chain_flags; // Options to allow GDI and DX together (see DXGI_SWAP_CHAIN_FLAG)
			DXGI_FORMAT                  m_depth_format;     // Depth buffer format
			D3DPtr<IDXGIAdapter>         m_adapter;          // The adapter to use. 0 means use the default
			D3D_DRIVER_TYPE              m_driver_type;      // HAL, REF, etc
			UINT                         m_device_layers;    // Add layers over the basic device (see D3D11_CREATE_DEVICE_FLAG)
			pr::Array<D3D_FEATURE_LEVEL> m_feature_levels;   // Features to support. Empty implies 9.1 -> 11.0
			UINT                         m_vsync;            // Present SyncInterval value
			bool                         m_allow_alt_enter;  // Allow switching to full screen with alt-enter

			RdrSettings(HWND hwnd = 0, BOOL windowed = TRUE, pr::iv2 const& client_area = pr::iv2::make(1024,768))
				:m_mem()
				,m_hwnd(hwnd)
				,m_windowed(windowed)
				,m_mode(client_area)
				,m_multisamp(4, ~0U)
				,m_buffer_count(2)
				,m_swap_effect(DXGI_SWAP_EFFECT_SEQUENTIAL)
				,m_swap_chain_flags(DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH)
				,m_depth_format(DXGI_FORMAT_D24_UNORM_S8_UINT)
				,m_adapter(0)
				,m_driver_type(D3D_DRIVER_TYPE_HARDWARE)
				,m_device_layers(D3D11_CREATE_DEVICE_BGRA_SUPPORT)
				,m_feature_levels()
				,m_vsync(1)
				,m_allow_alt_enter(false)
			{
				// Notes:
				// - vsync has different meaning for the swap effect modes.
				//   BitBlt modes: 0 = present immediately, 1,2,3,.. present after the nth vertical blank (has the effect of locking the frame rate to a fixed multiple of the vsync rate)
				//   Flip modes (Sequential): 0 = drop this frame if there is a new frame waiting, n > 0 = same as bitblt case
				// Add the debug layer in debug mode
				PR_EXPAND(PR_DBG_RDR, m_device_layers |= D3D11_CREATE_DEVICE_DEBUG);

				// Disable multisampling when debugging as pix can't handle it
				//PR_EXPAND(PR_DBG_RDR, m_multisamp = pr::rdr::MultiSamp());
			}
		};

		// Renderer state variables
		struct RdrState
		{
			RdrSettings                    m_settings;
			D3D_FEATURE_LEVEL              m_feature_level;
			D3DPtr<ID3D11Device>           m_device;
			D3DPtr<ID3D10Device1>          m_device10_1;
			D3DPtr<IDXGISwapChain>         m_swap_chain;
			D3DPtr<ID3D11DeviceContext>    m_immediate;
			D3DPtr<ID3D11RenderTargetView> m_main_rtv;
			D3DPtr<ID3D11DepthStencilView> m_main_dsv;
			TextureDesc                    m_bbdesc;  // The texture description of the back buffer
			bool                           m_idle;    // True while the window is occluded

			RdrState(RdrSettings const& settings);
			virtual ~RdrState() {}
			void InitMainRT();
		};
	}

	// The main renderer object
	class Renderer :rdr::RdrState
	{
	public:
		// These manager classes form part of the public interface of the renderer
		rdr::ModelManager       m_mdl_mgr;
		rdr::ShaderManager      m_shdr_mgr;
		rdr::TextureManager     m_tex_mgr;
		rdr::TextManager        m_text_mgr;
		rdr::BlendStateManager  m_bs_mgr;
		rdr::DepthStateManager  m_ds_mgr;
		rdr::RasterStateManager m_rs_mgr;

		Renderer(rdr::RdrSettings const& settings);
		~Renderer();

		// Return the d3d device
		D3DPtr<ID3D11Device> Device() const { return m_device; }
		D3DPtr<ID3D10Device1> Device10_1() const { return m_device10_1; }

		// Return the immediate device context
		D3DPtr<ID3D11DeviceContext> ImmediateDC() const { return m_immediate; }

		// Create a new deferred device context
		D3DPtr<ID3D11DeviceContext> DeferredDC() const { return 0; };

		// Returns an allocator object suitable for allocating instances of 'T'
		template <class Type> rdr::Allocator<Type> Allocator() const { return rdr::Allocator<Type>(m_settings.m_mem); }

		// Read access to the initialisation settings
		rdr::RdrSettings const& Settings() const { return m_settings; }

		// Get/Set full screen mode
		// Don't use the automatic alt-enter system, it's too uncontrollable
		// Handle WM_SYSKEYDOWN for VK_RETURN, then call FullScreenMode
		bool FullScreenMode() const;
		void FullScreenMode(bool on, rdr::DisplayMode mode);

		// The display mode of the main render target
		DXGI_FORMAT DisplayFormat() const;

		// Returns the size of the render target
		pr::iv2 RenderTargetSize() const;

		// Set the render target size.
		// Passing iv2.Zero will cause the RT to get its size from the associated window
		// Call when the window size changes (e.g. from a WM_SIZE message)
		void RenderTargetSize(pr::iv2 const& size);

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
