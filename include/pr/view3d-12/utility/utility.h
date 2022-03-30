//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	/// <summary>Set the name on a DX resource (debug only)</summary>
	void NameResource(ID3D12Object* res, char const* name);
	void NameResource(IDXGIObject* res, char const* name);

	// Choose a default for the client area
	iv2 DefaultClientArea(HWND hwnd, iv2 const& area);

	// Helper for getting the ref count of a COM pointer.
	ULONG RefCount(IUnknown* ptr);

	// The number of supported quality levels for the given format and sample count
	UINT MultisampleQualityLevels(ID3D12Device* device, DXGI_FORMAT format, UINT sample_count);
}

