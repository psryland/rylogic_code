//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
module;

#include "src/forward.h"

export module View3d:Wrappers;

namespace pr::rdr12
{
	// Display mode description
	export struct DisplayMode :DXGI_MODE_DESC
	{
		DisplayMode(UINT width = 1024, UINT height = 768, DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM)
			:DXGI_MODE_DESC()
		{
			Width                   = width  ? width  : 8;
			Height                  = height ? height : 8;
			Format                  = format;
			RefreshRate.Numerator   = 0; // let dx choose
			RefreshRate.Denominator = 0;
			ScanlineOrdering        = DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED;
			Scaling                 = DXGI_MODE_SCALING_UNSPECIFIED;
		}
		DisplayMode(pr::iv2 const& area, DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM)
			:DXGI_MODE_DESC()
		{
			Width                   = area.x ? area.x : 8;
			Height                  = area.y ? area.y : 8;
			Format                  = format;
			RefreshRate.Numerator   = 0; // let dx choose
			RefreshRate.Denominator = 0;
			ScanlineOrdering        = DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED;
			Scaling                 = DXGI_MODE_SCALING_UNSPECIFIED;
		}
	};
}

