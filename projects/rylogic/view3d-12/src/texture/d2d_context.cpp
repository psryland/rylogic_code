//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/texture/d2d_context.h"
#include "pr/view3d-12/main/renderer.h"

namespace pr::rdr12
{
	// RAII Scope for a wrapped Dx12 resource
	D2D1Context::D2D1Context(Renderer& rdr, ID3D12Resource* res)
		:m_dx11(rdr.Dx11Device())
		,m_dx11_res()
	{
		// Create a device context
		Throw(rdr.D2DDevice()->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_NONE, &m_dc.m_ptr));

		v2 dpi; m_dc->GetDpi(&dpi.x, &dpi.y);
		auto bmp_props = D2D1::BitmapProperties1(D2D1_BITMAP_OPTIONS_TARGET | D2D1_BITMAP_OPTIONS_CANNOT_DRAW, D2D1::PixelFormat(DXGI_FORMAT_UNKNOWN, D2D1_ALPHA_MODE_PREMULTIPLIED), dpi.x, dpi.y);

		// Create a Dx11 resource that wraps the Dx12 resource
		D3D11_RESOURCE_FLAGS flags = {
			.BindFlags = D3D11_BIND_RENDER_TARGET,
			.MiscFlags = 0,
			.CPUAccessFlags = 0U,
			.StructureByteStride = 0U,
		};
		auto before_state = DefaultResState(res);
		Throw(m_dx11->CreateWrappedResource(res, &flags, before_state, D3D12_RESOURCE_STATE_COMMON, __uuidof(IDXGISurface), (void**)&m_dx11_res.m_ptr));

		// Get a DXGI surface from the wrapped resource
		D3DPtr<IDXGISurface> surf;
		Throw(m_dx11_res->QueryInterface<IDXGISurface>(&surf.m_ptr));
		NameResource(surf.get(), FmtS("%s-dx11", NameResource(res).c_str()));

		// Create a bitmap wrapper for 'surf'
		D3DPtr<ID2D1Bitmap1> target;
		Throw(m_dc->CreateBitmapFromDxgiSurface(surf.get(), bmp_props, &target.m_ptr));

		// Set the render target
		m_dc->SetTarget(target.get());
	}

	D2D1Context::~D2D1Context()
	{
		if (m_dx11_res != nullptr)
		{
			ID3D11Resource* arr[] = {m_dx11_res.get()};
			m_dx11->ReleaseWrappedResources(arr, 1U);
		}

		// todo, need to call dx11_dc->Flush() here
	}
}