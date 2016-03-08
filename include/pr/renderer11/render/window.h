//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/config/config.h"

namespace pr
{
	namespace rdr
	{
		struct WndSettings
		{
			HWND             m_hwnd;
			BOOL             m_windowed;         // Windowed mode or full screen
			DisplayMode      m_mode;             // Display mode to use (note: must be valid for the adapter, use FindClosestMatchingMode if needed)
			MultiSamp        m_multisamp;        // Number of samples per pixel (AA/Multi-sampling)
			UINT             m_buffer_count;     // Number of buffers in the chain, 1 = front only, 2 = front and back, 3 = triple buffering, etc
			DXGI_SWAP_EFFECT m_swap_effect;      // How to swap the back buffer to the front buffer
			UINT             m_swap_chain_flags; // Options to allow GDI and DX together (see DXGI_SWAP_CHAIN_FLAG)
			DXGI_FORMAT      m_depth_format;     // Depth buffer format
			DXGI_USAGE       m_usage;            // Usage flags for the swap chain buffer
			UINT             m_vsync;            // Present SyncInterval value
			bool             m_allow_alt_enter;  // Allow switching to full screen with alt-enter
			string32         m_name;             // A debugging name for the window

			// Notes:
			// - VSync has different meaning for the swap effect modes.
			//   BitBlt modes: 0 = present immediately, 1,2,3,.. present after the nth vertical blank (has the effect of locking the frame rate to a fixed multiple of the VSync rate)
			//   Flip modes (Sequential): 0 = drop this frame if there is a new frame waiting, n > 0 = same as bitblt case

			WndSettings(HWND hwnd = 0, bool windowed = true, bool gdi_compat = false, pr::iv2 const& client_area = pr::iv2::make(1024,768));
		};

		// Renderer window.
		struct Window
		{
			Renderer*                        m_rdr;              // The owning renderer
			HWND                             m_hwnd;             // The window handle this window is bound to
			DXGI_FORMAT                      m_db_format;        // The format of the depth buffer
			MultiSamp                        m_multisamp;        // Number of samples per pixel (AA/Multi-sampling)
			UINT                             m_swap_chain_flags; // Options to allow GDI and DX together (see DXGI_SWAP_CHAIN_FLAG)
			UINT                             m_vsync;            // Present SyncInterval value
			D3DPtr<IDXGISwapChain>           m_swap_chain;       // The swap chain bound to the window handle
			D3DPtr<ID3D11RenderTargetView>   m_main_rtv;         // Render target view of the render target
			D3DPtr<ID3D11ShaderResourceView> m_main_srv;         // Shader resource view of the render target
			D3DPtr<ID3D11DepthStencilView>   m_main_dsv;         // Depth buffer
			Texture2DPtr                     m_main_tex;         // The render target as a texture
			bool                             m_idle;             // True while the window is occluded
			string32                         m_name;             // A debugging name for the window
			pr::iv2                          m_area;             // The size of the render target last set (for debugging only)

			Window(Renderer& rdr, WndSettings const& settings);
			~Window();

			// Return the DX device
			D3DPtr<ID3D11Device> Device() const;

			// Return the immediate device context
			D3DPtr<ID3D11DeviceContext> ImmediateDC() const;

			// Access the renderer manager classes
			ModelManager& mdl_mgr();
			ShaderManager& shdr_mgr();
			TextureManager& tex_mgr();
			BlendStateManager& bs_mgr();
			DepthStateManager& ds_mgr();
			RasterStateManager& rs_mgr();

			// Create the render target and depth buffer
			void InitRT();

			// Binds the render target and depth buffer to the OM
			void RestoreRT();

			// Binds the given render target and depth buffer views to the OM
			void SetRT(D3DPtr<ID3D11RenderTargetView>& rtv, D3DPtr<ID3D11DepthStencilView>& dsv);

			// Set the viewport to all of the render target
			void RestoreFullViewport();

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
			//   sky box
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
}