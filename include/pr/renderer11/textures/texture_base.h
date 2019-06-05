//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/config/config.h"

namespace pr::rdr
{
	/*
	struct TextureBase
		:pr::RefCount<TextureBase>
	{
		// Notes:
		//   - A base class for all renderer texture instances.
		//   - Each time MatMgr.CreateTexture is called, a new texture instance is allocated.
		//     However, the resources associated with the texture may be shared with other textures.
		pr::m4x4                         m_t2s;       // Texture to surface transform
		//D3DPtr<ID3D11Texture2D>          m_tex;       // The texture resource
		//D3DPtr<ID3D11ShaderResourceView> m_srv;       // A shader resource view of the texture
		//D3DPtr<ID3D11SamplerState>       m_samp;      // The sampler state to use with this texture
		RdrId                            m_id;        // Id for this texture in the texture managers lookup map
		RdrId                            m_src_id;    // An id identifying the source this texture was created from (needed when deleting the last ref to a dx tex)
		SortKeyId                        m_sort_id;   // A sort key component for this texture
		bool                             m_has_alpha; // True if the texture contains alpha pixels
		TextureManager*                  m_mgr;       // The texture manager that created this texture
		string32                         m_name;      // Human readable id for the texture

		TextureBase(TextureManager* mgr, RdrId id, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name);
	};
	*/
}