//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr
{
	template <> struct Convert<D3D12_RANGE, rdr12::Range>
	{
		static D3D12_RANGE To_(rdr12::Range const& r)
		{
			return reinterpret_cast<D3D12_RANGE const&>(r);
		}
	};
	template <> struct Convert<rdr12::Range, D3D12_RANGE>
	{
		static rdr12::Range To_(D3D12_RANGE const& r)
		{
			return reinterpret_cast<rdr12::Range const&>(r);
		}
	};
}