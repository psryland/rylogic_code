//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	// A D2D1 wrapper around a Dx12 resource for 2D drawing.
	struct D2D1Context
	{
		ID3D11On12Device* m_dx11;
		D3DPtr<ID3D11Resource> m_dx11_res;
		D3DPtr<ID2D1DeviceContext> m_dc;

		D2D1Context(Renderer& rdr, ID3D12Resource* res);
		D2D1Context(D2D1Context&&) = default;
		D2D1Context(D2D1Context const&) = delete;
		D2D1Context& operator=(D2D1Context&&) = default;
		D2D1Context& operator=(D2D1Context const&) = delete;
		~D2D1Context();

		ID2D1DeviceContext* operator ->() const { return m_dc.get(); }
	};
}