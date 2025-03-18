//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2025
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12::ldraw
{
	enum class ECommandId
	{
		None,
		Camera,
	};

	// An instruction to do something with ldraw objects
	struct Command
	{
		using CommandData = pr::vector<std::byte, sizeof(m4x4), false, alignof(m4x4)>;

		CommandData m_data;
		ECommandId m_id;
	};
}
