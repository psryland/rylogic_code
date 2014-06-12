//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "view3d/forward.h"
#include "pr/view3d/view3d.h"

namespace view3d
{
	struct Drawset :pr::AlignTo<16>
	{
		View3DWindow    m_window;                   // The owning window

		Drawset(View3DWindow window)
			:m_window(window)
			
		{
		}
	};
}
