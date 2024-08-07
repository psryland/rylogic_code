//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	// Event Args for the Window.BackBufferSizeChanged event
	struct BackBufferSizeChangedEventArgs
	{
		iv2 m_area;  // The back buffer size before (m_done == false) or after (m_done == true) the swap chain buffer resize
		bool m_done; // True when the swap chain has resized it's buffers

		BackBufferSizeChangedEventArgs(iv2 const& area, bool done)
			:m_area(area)
			,m_done(done)
		{}
	};

	// Event args for resolving a filepath
	struct ResolvePathArgs
	{
		std::filesystem::path filepath;
		bool handled;
	};
}

