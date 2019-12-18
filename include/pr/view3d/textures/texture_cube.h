//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/config/config.h"

namespace pr::rdr
{
	struct TextureCube :TextureBase
	{
		// Notes:
		//  - A cube texture is basically just a special case 2d texture .
		//  - The cube texture should look like:
		//            Top
		//     Left  Front  Right  Back
		//           Bottom
		//   - Each time MatMgr.CreateTexture is called, a new Texture2D instance is allocated.
		//     However, the resources associated with this texture may be shared with other Textures.
		
		// Cube map to world transform
		m4x4 m_cube2w;

		TextureCube(TextureManager* mgr, RdrId id, ID3D11Texture2D* tex, ID3D11ShaderResourceView* srv, SamplerDesc const& sdesc, char const* name);

		// Get the DirectX texture cube resource
		ID3D11Texture2D* dx_tex() const
		{
			return static_cast<ID3D11Texture2D*>(m_res.get());
		}
	};
}