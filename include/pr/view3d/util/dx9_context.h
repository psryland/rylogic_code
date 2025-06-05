//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

// For debug info, D3D_DEBUG_INFO must be defined before d3d9.h is included
//#define D3D_DEBUG_INFO
//#if defined(D3D_DEBUG_INFO)
//#    if defined(_D3D9_H_)
//#    error "d3d9.h has already been included"
//#    endif
//#    pragma message(PR_LINK "WARNING: ************************************************** D3D_DEBUG_INFO defined")
//#endif

#include <d3d9.h>
#pragma comment(lib, "d3d9.lib")
#include "pr/view3d/forward.h"

namespace pr::rdr
{
	struct Dx9Context
	{
		D3DPtr<IDirect3D9Ex> m_d3d9;
		D3DPtr<IDirect3DDevice9Ex> m_device;

		explicit Dx9Context(HWND hwnd)
			:m_d3d9()
			,m_device()
		{
			// Create a d3d9 dll context
			Check(Direct3DCreate9Ex(D3D_SDK_VERSION, m_d3d9.address_of()));

			// Get a pointer to the d3d9 device
			auto pp = D3DPRESENT_PARAMETERS{};
			pp.Windowed = TRUE;
			pp.SwapEffect = D3DSWAPEFFECT_DISCARD;
			pp.hDeviceWindow = nullptr;
			pp.PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE;
			Check(m_d3d9->CreateDeviceEx(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, hwnd,
				D3DCREATE_HARDWARE_VERTEXPROCESSING | D3DCREATE_MULTITHREADED | D3DCREATE_FPU_PRESERVE,
				&pp, nullptr, m_device.address_of()));

			// Check it's good to go
			Check(m_device->CheckDeviceState(nullptr));
		}

		// Convert a DXGI format into the nearest equivalent Dx9 format.
		// Returns D3DFMT_UNKNOWN if there is not suitable conversion.
		static D3DFORMAT ConvertFormat(DXGI_FORMAT fmt)
		{
			switch (fmt)
			{
			default:
				return D3DFMT_UNKNOWN;
			case DXGI_FORMAT_B8G8R8A8_UNORM:
			case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
				return D3DFMT_A8R8G8B8;
			case DXGI_FORMAT_R8G8B8A8_UNORM:
			case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
				return D3DFMT_A8B8G8R8;
			case DXGI_FORMAT_B8G8R8X8_UNORM:
				return D3DFMT_X8R8G8B8;
			case DXGI_FORMAT_R10G10B10A2_UNORM:
				return D3DFMT_A2B10G10R10;
			case DXGI_FORMAT_R16G16B16A16_FLOAT:
				return D3DFMT_A16B16G16R16F;
			};
		}

		// Create a Dx9 texture
		D3DPtr<IDirect3DTexture9> CreateTexture(UINT width, UINT height, UINT levels = 0, DWORD usage = 0, D3DFORMAT format = D3DFMT_A8R8G8B8, D3DPOOL pool = D3DPOOL_DEFAULT, HANDLE* shared_handle = nullptr)
		{
			// Notes:
			//  - The behaviour of CreateTexture depends on the value of 'shared_handle'.
			//    If 'shared_handle == nullptr', the created texture is not shared.
			//    If '*shared_handle == nullptr', the created texture is shareable.
			//    If '*shared_handle != nullptr', the created texture uses the resource associated with the shared handle
			D3DPtr<IDirect3DTexture9> tex;
			Check(m_device->CreateTexture(width, height, levels, usage, format, pool, tex.address_of(), shared_handle));
			return tex;
		}
	};

}
