//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/render/back_buffer.h"
#include "pr/view3d-12/resource/gpu_descriptor_heap.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/view3d-12/utility/cmd_alloc.h"
#include "pr/view3d-12/utility/cmd_list.h"
#include "pr/view3d-12/utility/pipe_state.h"
#include "pr/view3d-12/utility/diagnostics.h"

namespace pr::rdr12
{
	struct Window
	{
		// Notes:
		//  - A window wraps an HWND and contains a SwapChain.
		//  - The stuff visible in a window is governed by one or more scenes.
		//  - A window where HWND = nullptr, is used for rendering to off-screen render targets only.
		//    So parallel command list building requires multiple command allocators.
		//  - Command allocators can only be reset when they are not used by the GPU any more.
		//  - The swap chain does not have a depth stencil resource, it's managed by the window.

		using BackBuffers = pr::vector<BackBuffer, 4, false>;
		using CmdLists = pr::vector<ID3D12CommandList*, 4, false>;
		using GpuViewHeap = GpuDescriptorHeap<D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV>;
		using GpuSampHeap = GpuDescriptorHeap<D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER>;

		Renderer*                    m_rdr;              // The owning renderer
		HWND                         m_hwnd;             // The window handle this window is bound to
		DXGI_FORMAT                  m_db_format;        // The format of the depth buffer
		MultiSamp                    m_multisamp;        // Number of samples per pixel (AA/Multi-sampling)
		DXGI_SWAP_CHAIN_FLAG         m_swap_chain_flags; // Options to allow GDI and DX together (see DXGI_SWAP_CHAIN_FLAG)
		D3DPtr<IDXGISwapChain>       m_swap_chain_dbg;   // A swap chain bound to the dummy window handle for debugging
		D3DPtr<IDXGISwapChain3>      m_swap_chain;       // The swap chain bound to the window handle
		D3DPtr<ID3D12Resource>       m_depth_stencil;    // The depth stencil buffer
		D3DPtr<ID3D12DescriptorHeap> m_rtv_heap;         // Render target view descriptors for the swapchain
		D3DPtr<ID3D12DescriptorHeap> m_dsv_heap;         // Depth stencil view descriptor for the swapchain
		D3DPtr<ID2D1DeviceContext>   m_d2d_dc;           // The device context for D2D
		GpuSync                      m_gsync;            // GPU fence for frames
		BackBuffers                  m_bb;               // Back buffer render targets from the swap chain.
		GfxCmdAllocPool              m_cmd_alloc_pool;   // A pool of command allocators
		GfxCmdListPool               m_cmd_list_pool;    // A pool of command lists
		CmdLists                     m_cmd_lists;        // Command lists (in order) to be executed
		GpuViewHeap                  m_heap_view;        // Shader visible heap for CBV/SRV/UAV
		GpuSampHeap                  m_heap_samp;        // Shader visible heap for Samplers
		DiagState                    m_diag;             // Diagnostic variables
		int64_t                      m_frame_number;     // The number of times 'RenderFrame' has been called.
		UINT                         m_vsync;            // Present SyncInterval value
		bool                         m_idle;             // True while the window is occluded
		string32                     m_name;             // A debugging name for the window

		Window(Renderer& rdr, WndSettings const& settings);
		~Window();

		// Access the renderer manager classes
		Renderer& rdr() const;
		ResourceManager& res_mgr() const;

		// Return the current DPI for this window. Use DIPtoPhysical(pt, Dpi()) for converting points
		v2 Dpi() const;

		// The current back buffer index
		int BBIndex() const;
		int BBCount() const;

		// The number of times 'RenderFrame' has been called
		int64_t FrameNumber() const;

		// Get/Set the size of the back buffer render target
		iv2 BackBufferSize() const;
		void BackBufferSize(iv2 size, bool force);

		// Render a frame
		struct Frame
		{
			BackBuffer& m_bb;     // The BackBuffer frame to render to
			CmdLists& m_cmd_lists; // Command lists (in order) to be executed

			Frame(BackBuffer& bb, CmdLists& cmd_lists);
			Frame(Frame&&) = default;
			Frame(Frame const&) = delete;
			Frame& operator =(Frame&&) = default;
			Frame& operator =(Frame const&) = delete;
			void Render(Scene& scene);
			void Present();
		};
		Frame RenderFrame();
		
		// Flip buffers in the swap chain.
		// Users shouldn't call this, it's done by Frame::Present().
		void Flip();
	};
}

		#if 0 // todo
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
		void FullScreenMode(bool on, DisplayMode mode);

		// The display mode of the main render target
		DXGI_FORMAT DisplayFormat() const;
		#endif

		#if 0 // todo
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
			return Scope(
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
		#endif
