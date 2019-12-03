//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"

namespace pr::rdr
{
	// Event Args for the Window.BackBufferSizeChanged event
	struct BackBufferSizeChangedEventArgs
	{
		pr::iv2 m_area;   // The back buffer size before (m_done == false) or after (m_done == true) the swap chain buffer resize
		bool    m_done;   // True when the swap chain has resized it's buffers
			
		BackBufferSizeChangedEventArgs(pr::iv2 const& area, bool done)
			:m_area(area)
			,m_done(done)
		{}
	};
}
