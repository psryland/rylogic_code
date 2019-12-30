//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_UTIL_DEMO_SCENE_H
#define PR_RDR_UTIL_DEMO_SCENE_H

#include "pr/view3d/forward.h"

namespace pr
{
	namespace rdr
	{
		// Make a demo scene.
		// This function acts as a unit test of sorts.
		// It also allows new apps to quickly get something on screen
		inline void DemoScene(Renderer& rdr, pr::v4 const& pos, pr::v4 const& up, float time)
		{
			(void)rdr;
			(void)pos;
			(void)up;
			(void)time;
		}
	}
}

#endif
