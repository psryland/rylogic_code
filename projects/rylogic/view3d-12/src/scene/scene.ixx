//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************

module;

#include "src/forward.h"

export module View3d:Scene;

namespace pr::rdr12
{
	// Create an instance of this object to enumerate the adapters and their outputs on the current system.
	// Note: modes are not enumerated because they depend on DXGI_FORMAT. Users should create a SystemConfig,
	// then call 'GetDisplayModes' for the format needed.
	export struct Scene
	{
	};
}

