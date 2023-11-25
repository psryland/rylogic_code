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
		:m_dc()
		,m_dx11_res()
		,m_dx11_dc(rdr.Dx11DeviceContext())
		,m_dx11(rdr.Dx11Device())
	{
		// Create a d2d device context to access the d2d drawing commands
		Throw(rdr.D2DDevice()->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_NONE, &m_dc.m_ptr));

		// Need to use blend source over to use ClearType fonts
		m_dc->SetPrimitiveBlend(D2D1_PRIMITIVE_BLEND_SOURCE_OVER);

		// Set the properties of the bitmap surface we will get 
		v2 dpi; m_dc->GetDpi(&dpi.x, &dpi.y);
		auto bmp_props = D2D1::BitmapProperties1(D2D1_BITMAP_OPTIONS_TARGET | D2D1_BITMAP_OPTIONS_CANNOT_DRAW, D2D1::PixelFormat(DXGI_FORMAT_UNKNOWN, D2D1_ALPHA_MODE_PREMULTIPLIED /*IGNORE*/), dpi.x, dpi.y);

		// Create a Dx11 resource that wraps the Dx12 resource
		D3D11_RESOURCE_FLAGS flags = {
			.BindFlags = D3D11_BIND_RENDER_TARGET,
			.MiscFlags = 0,
			.CPUAccessFlags = 0U,
			.StructureByteStride = 0U,
		};
		auto default_state = DefaultResState(res);
		Throw(m_dx11->CreateWrappedResource(res, &flags, D3D12_RESOURCE_STATE_RENDER_TARGET, default_state, __uuidof(IDXGISurface), (void**)&m_dx11_res.m_ptr));

		// Get a DXGI surface from the wrapped resource
		D3DPtr<IDXGISurface> surf;
		Throw(m_dx11_res->QueryInterface<IDXGISurface>(&surf.m_ptr));
		NameResource(surf.get(), FmtS("%s-dx11", NameResource(res).c_str()));

		// Create a bitmap wrapper for 'surf'
		D3DPtr<ID2D1Bitmap1> target;
		Throw(m_dc->CreateBitmapFromDxgiSurface(surf.get(), &bmp_props, &target.m_ptr));

		// Acquire the texture as the current render target.
		// Transitions the texture from the 'InState' to a render target state
		ID3D11Resource* arr[] = {m_dx11_res.get()};
		m_dx11->AcquireWrappedResources(&arr[0], 1U);

		// Set 'target' as the d2d render target
		m_dc->SetTarget(target.get());
	}
	D2D1Context::~D2D1Context()
	{
		if (m_dx11_res == nullptr)
			return;
	
		// Return the texture to the "OutState"
		ID3D11Resource* arr[] = {m_dx11_res.get()};
		m_dx11->ReleaseWrappedResources(&arr[0], 1U);

		// Push the commands to the dx12 command queue
		m_dx11_dc->Flush();
	}
}