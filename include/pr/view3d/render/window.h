﻿//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/view3d/forward.h"
#include "pr/view3d/config/config.h"

namespace pr::rdr
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
		bool             m_use_w_buffer;     // Use W-Buffer depth rather than Z-Buffer
		bool             m_allow_alt_enter;  // Allow switching to full screen with alt-enter
		string32         m_name;             // A debugging name for the window

		// Notes:
		// - VSync has different meaning for the swap effect modes.
		//   BitBlt modes: 0 = present immediately, 1,2,3,.. present after the nth vertical blank (has the effect of locking the frame rate to a fixed multiple of the VSync rate)
		//   Flip modes (Sequential): 0 = drop this frame if there is a new frame waiting, n > 0 = same as bitblt case

		WndSettings(HWND hwnd = 0, bool windowed = true, bool gdi_compatible_bb = false, iv2 const& client_area = iv2Zero, bool w_buffer = true);
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
		D3DPtr<IDXGISwapChain>           m_swap_chain_dbg;   // A swap chain bound to the dummy window handle for debugging
		D3DPtr<IDXGISwapChain>           m_swap_chain;       // The swap chain bound to the window handle
		D3DPtr<ID3D11RenderTargetView>   m_main_rtv;         // Render target view of the render target
		D3DPtr<ID3D11ShaderResourceView> m_main_srv;         // Shader resource view of the render target
		D3DPtr<ID3D11DepthStencilView>   m_main_dsv;         // Depth buffer
		D3DPtr<ID2D1DeviceContext>       m_d2d_dc;           // The device context for D2D
		D3DPtr<ID3D11Query>              m_query;            // The interface for querying the GPU
		Texture2DPtr                     m_main_rt;          // The render target as a texture
		bool                             m_idle;             // True while the window is occluded
		string32                         m_name;             // A debugging name for the window
		pr::iv2                          m_dbg_area;         // The size of the render target last set (for debugging only)

		Window(Renderer& rdr, WndSettings const& settings);
		~Window();

		// Access the renderer manager classes
		Renderer& rdr() const;
		ModelManager& mdl_mgr() const;
		ShaderManager& shdr_mgr() const;
		TextureManager& tex_mgr() const;
		BlendStateManager& bs_mgr() const;
		DepthStateManager& ds_mgr() const;
		RasterStateManager& rs_mgr() const;

		// Return the current DPI for this window. Use DIPtoPhysical(pt, Dpi()) for converting points
		v2 Dpi() const
		{
			// Support old windows by dynamically looking for the new DPI functions
			// and falling back to the GDI functions if not available.
			auto user32 = Scope<HMODULE>(
				[=] { return ::LoadLibraryW(L"user32.dll"); }, 
				[=](HMODULE m) {::FreeLibrary(m); });

			// Look for the new windows functions for DPI
			auto GetDpiForWindowFunc = reinterpret_cast<UINT(far __stdcall*)(HWND)>(GetProcAddress(user32.m_state, "GetDpiForWindow"));
			if (m_hwnd != nullptr && GetDpiForWindowFunc != nullptr)
			{
				auto dpi = (float)GetDpiForWindowFunc(m_hwnd);
				return v2(dpi, dpi);
			}

			// Fallback to the system DPI function
			auto GetDpiForSystemFunc = reinterpret_cast<UINT(far __stdcall*)()>(GetProcAddress(user32.m_state, "GetDpiForSystem"));
			if (GetDpiForSystemFunc != nullptr)
			{
				auto dpi = (float)GetDpiForSystemFunc();
				return v2(dpi, dpi);
			}

			// Fallback to GDI+
			gdi::Graphics g(m_hwnd);
			auto dpi = v2(g.GetDpiX(), g.GetDpiY());
			return dpi;
		}

		// Create the render target and depth buffer
		void InitRT();

		// Binds the render target and depth buffer to the OM
		void RestoreRT();

		// Binds the given render target and depth buffer views to the OM
		void SetRT(ID3D11RenderTargetView* rtv, ID3D11DepthStencilView* dsv, bool is_new_main_rt);

		// Render this window into 'render_target;
		// 'render_target' is the texture that is rendered onto
		// 'depth_buffer' is an optional texture that will receive the depth information (can be null)
		// 'is_new_main_rt' if true, makes the provided targets the main render target (those restored by RestoreRT)
		void SetRT(ID3D11Texture2D* render_target, ID3D11Texture2D* depth_buffer, bool is_new_main_rt);

		// Draw text directly to the back buffer
		void DrawString(wchar_t const* text, float x, float y);

		// Set the viewport to all of the render target
		void RestoreFullViewport();

		// Get/Set full screen mode
		// Don't use the automatic alt-enter system, it's too uncontrollable
		// Handle WM_SYSKEYDOWN for VK_RETURN, then call FullScreenMode
		bool FullScreenMode() const;
		void FullScreenMode(bool on, rdr::DisplayMode mode);

		// The display mode of the main render target
		DXGI_FORMAT DisplayFormat() const;

		// Returns the size of the current render target
		iv2 RenderTargetSize() const;

		// Get/Set the size of the swap chain back buffer.
		// Passing iv2.Zero will cause the RT to get its size from the associated window
		// Call when the window size changes (e.g. from a WM_SIZE message)
		iv2 BackBufferSize() const;
		void BackBufferSize(iv2 const& size, bool force = false);

		// Get/Set the multi-sampling used
		// Changing the multi-sampling mode is a bit like resizing the back buffer
		MultiSamp MultiSampling() const;
		void MultiSampling(MultiSamp ms);

		// Release all references to the swap chain to allow it to be created or resized.
		void RebuildRT(std::function<void(ID3D11Device*)> work);

		// Signal the start and end of a frame.
		// A frame can be any number of scenes rendered into the back buffer.
		void FrameBeg();
		void FrameEnd();
		auto FrameScope()
		{
			return Scope<void>(
			[this] { FrameBeg(); },
			[this] { FrameEnd(); });
		}

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