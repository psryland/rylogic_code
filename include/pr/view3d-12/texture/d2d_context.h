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
		D3DPtr<ID2D1DeviceContext> m_dc;
		D3DPtr<ID3D11Resource> m_dx11_res;
		ID3D11DeviceContext* m_dx11_dc;
		ID3D11On12Device* m_dx11;

		D2D1Context(Renderer& rdr, ID3D12Resource* res);
		D2D1Context(D2D1Context&& rhs) noexcept
			: m_dc(std::move(rhs.m_dc))
			, m_dx11_res(std::move(rhs.m_dx11_res))
			, m_dx11_dc(std::move(rhs.m_dx11_dc))
			, m_dx11(rhs.m_dx11)
		{
			rhs.m_dc = nullptr;
			rhs.m_dx11_res = nullptr;
			rhs.m_dx11_dc = nullptr;
			rhs.m_dx11 = nullptr;
		}
		D2D1Context(D2D1Context const&) = delete;
		D2D1Context& operator=(D2D1Context&& rhs) noexcept
		{
			if (this == &rhs) return *this;
			std::swap(m_dc, rhs.m_dc);
			std::swap(m_dx11_res, rhs.m_dx11_res);
			std::swap(m_dx11_dc, rhs.m_dx11_dc);
			std::swap(m_dx11, rhs.m_dx11);
			return *this;
		}
		D2D1Context& operator=(D2D1Context const&) = delete;
		~D2D1Context();

		ID2D1DeviceContext* operator ->() const
		{
			return m_dc.get();
		}
	};
}