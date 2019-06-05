//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/config/config.h"

namespace pr::rdr
{
	// A cube texture
	// Each time MatMgr.CreateTexture is called, a new Texture2D instance is allocated.
	// However, the resources associated with this texture may be shared with other Textures.
	struct TextureCube
		:pr::RefCount<TextureCube>
	{

	};
}