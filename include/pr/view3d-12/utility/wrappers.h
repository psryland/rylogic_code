//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	// Display mode description
	struct DisplayMode :DXGI_MODE_DESC
	{
		// Notes:
		//  - Credit: https://www.rastertek.com/dx12tut03.html
		//    Before we can initialize the swap chain we have to get the refresh rate from the video card/monitor.
		//    Each computer may be slightly different so we will need to query for that information. We query for
		//    the numerator and denominator values and then pass them to DirectX during the setup and it will calculate
		//    the proper refresh rate. If we don't do this and just set the refresh rate to a default value which may
		//    not exist on all computers then DirectX will respond by performing a buffer copy instead of a buffer flip
		//    which will degrade performance and give us annoying errors in the debug output.
		//  - For gamma-correct rendering to standard 8-bit per channel UNORM formats, you’ll want to create the Render
		//    Target using an sRGB format. The new flip modes, however, do not allow you to create a swap chain back buffer
		//    using an sRGB format. In this case, you create one using the non-sRGB format (i.e. DXGI_SWAP_CHAIN_DESC1.Format
		//    = DXGI_FORMAT_B8G8R8A8_UNORM) and use sRGB for the Render Target View (i.e. D3D12_RENDER_TARGET_VIEW_DESC.Format
		//    = DXGI_FORMAT_B8G8R8A8_UNORM_SRGB).
		DisplayMode(int width = 1024, int height = 768, DXGI_FORMAT format = DXGI_FORMAT_B8G8R8A8_UNORM)
			:DXGI_MODE_DESC()
		{
			Width                   = width  ? s_cast<UINT>(width) : 16;
			Height                  = height ? s_cast<UINT>(height) : 16;
			Format                  = format;
			RefreshRate.Numerator   = 0;
			RefreshRate.Denominator = 1;
			ScanlineOrdering        = DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED;
			Scaling                 = DXGI_MODE_SCALING_UNSPECIFIED;
		}
		DisplayMode(iv2 const& area, DXGI_FORMAT format = DXGI_FORMAT_B8G8R8A8_UNORM)
			:DisplayMode(area.x, area.y, format)
		{}
	};

	// Multi sampling description
	struct MultiSamp :DXGI_SAMPLE_DESC
	{
		constexpr MultiSamp()
			:DXGI_SAMPLE_DESC()
		{
			Count   = 1;
			Quality = 0;
		}
		constexpr MultiSamp(UINT count, UINT quality = ~UINT())
			:DXGI_SAMPLE_DESC()
		{
			Count = count;
			Quality = quality;
		}
		MultiSamp& Validate(ID3D12Device* device, DXGI_FORMAT format)
		{
			UINT quality = 0;
			for (; Count > 1 && (quality = MultisampleQualityLevels(device, format, Count)) == 0; Count >>= 1) {}
			if (quality != 0 && Quality >= quality) Quality = quality - 1;
			return *this;
		}
		friend bool operator == (MultiSamp const& lhs, MultiSamp const& rhs)
		{
			return lhs.Count == rhs.Count && lhs.Quality == rhs.Quality;
		}
	};

	// Viewport description
	struct Viewport :D3D12_VIEWPORT
	{
		// Notes:
		//  - Viewports represent an area on the backbuffer, *not* the target HWND.
		//  - Viewports are in render target space
		//    e.g.
		//     x,y          = 0,0 (not -0.5f,-0.5f)
		//     width,height = 800,600 (not 1.0f,1.0f)
		//     depth is normalised from 0.0f -> 1.0f
		//  - Viewports are measured in render target pixels not DIP or window pixels.
		//    i.e.  generally, the viewport is not in client space coordinates.
		//  - ScreenW/H should be in DIP

		int ScreenW; // The screen width (in DIP) that the render target will be mapped to.
		int ScreenH; // The screen height (in DIP) that the render target will be mapped to.

		Viewport()
			:Viewport(0.0f, 0.0f, 16.0f, 16.0f, 16, 16, 0.0f, 1.0f)
		{}
		Viewport(pr::iv2 const& area)
			:Viewport(0.0f, 0.0f, float(area.x), float(area.y), area.x, area.y, 0.0f, 1.0f)
		{}
		Viewport(float x, float y, float width, float height, int screen_w, int screen_h, float min_depth, float max_depth)
			:D3D12_VIEWPORT()
		{
			Set(x, y, width, height, screen_w, screen_h, min_depth, max_depth);
		}
		Viewport& Set(float x, float y, float width, float height, int screen_w, int screen_h, float min_depth, float max_depth)
		{
			#if PR_DBG_RDR
			Throw(x >= D3D12_VIEWPORT_BOUNDS_MIN && x <= D3D12_VIEWPORT_BOUNDS_MAX , "X value out of range");
			Throw(y >= D3D12_VIEWPORT_BOUNDS_MIN && y <= D3D12_VIEWPORT_BOUNDS_MAX , "Y value out of range");
			Throw(width >= 0.0f                                                    , "Width value invalid");
			Throw(height >= 0.0f                                                   , "Height value invalid");
			Throw(x + width  <= D3D12_VIEWPORT_BOUNDS_MAX                          , "Width value out of range");
			Throw(y + height <= D3D12_VIEWPORT_BOUNDS_MAX                          , "Height value out of range");
			Throw(min_depth >= 0.0f && min_depth <= 1.0f                           , "Min depth value out of range");
			Throw(max_depth >= 0.0f && max_depth <= 1.0f                           , "Max depth value out of range");
			Throw(min_depth <= max_depth                                           , "Min and max depth values invalid");
			Throw(screen_w >= 0                                                    , "Screen Width value invalid");
			Throw(screen_h >= 0                                                    , "Screen Height value invalid");
			#endif

			TopLeftX = x;
			TopLeftY = y;
			Width    = width;
			Height   = height;
			MinDepth = min_depth;
			MaxDepth = max_depth;
			ScreenW  = screen_w;
			ScreenH  = screen_h;
			return *this;
		}
		Viewport& Set(float x, float y, float width, float height)
		{
			return Set(x, y, width, height, (int)width, (int)height, 0.0f, 1.0f);
		}

		// The viewport rectangle, in render target pixels
		FRect AsFRect() const
		{
			return FRect(TopLeftX, TopLeftY, TopLeftX + Width, TopLeftY + Height);
		}
		IRect AsIRect() const
		{
			return IRect(int(TopLeftX), int(TopLeftY), int(TopLeftX + Width), int(TopLeftY + Height));
		}
		RECT AsRECT() const
		{
			return RECT{LONG(TopLeftX), LONG(TopLeftY), LONG(TopLeftX + Width), LONG(TopLeftY + Height)};
		}

		// Convert a screen space point to normalised screen space
		// 'ss_point' must be in screen pixels, not logical pixels (DIP).
		v2 SSPointToNSSPoint(v2 const& ss_point) const
		{
			return NormalisePoint(IRect(0, 0, ScreenW, ScreenH), ss_point, 1.0f, -1.0f);
		}
		v2 NSSPointToSSPoint(v2 const& nss_point) const
		{
			return ScalePoint(IRect(0, 0, ScreenW, ScreenH), nss_point, 1.0f, -1.0f);
		}
	};
}

