//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/textures/texture2d.h"

namespace pr
{
	namespace rdr
	{
		// 'TextureGdi' is just a 2d texture with support for rendering GDI+ stuff into itself
		struct TextureGdi :Texture2D
		{
			TextureGdi(TextureManager* mgr, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc, SortKeyId sort_id, ShaderResViewDesc const* srvdesc = nullptr);

		private:

			// Refcounting cleanup function
			protected: void Delete() override;
		};
	}
}
