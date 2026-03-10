#pragma once
#include "src/forward.h"

namespace physics_sandbox
{
	// Menu command IDs for the sandbox application
	namespace MenuID
	{
		static constexpr int OpenFile = 1001;
		static constexpr int RecentFileBase = 2000; // 2000..2000+MaxRecentFiles-1
	}
}