//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_MATERIALS_TEXTURE_2D_H
#define PR_RDR_MATERIALS_TEXTURE_2D_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/config/config.h"

namespace pr
{
	namespace rdr
	{
		// A 2D texture
		// Each time MatMgr.CreateTexture is called, a new Texture2D instance is allocated.
		// However, the resources associated with this texture may be shared with other Textures.
		// A copy of the TextureDesc is kept locally in the texture instance for per-instance modification
		struct Texture2D :pr::RefCount<Texture2D>
		{
			pr::m4x4                  m_t2s;             // Texture to surface transform
			D3DPtr<ID3D11Texture2D>   m_tex;             // The texture resource
			TextureDesc               m_info;            // A description of the texture
			//rs::Block                 m_rsb;             // Texture specific render states
			//TextureFilter             m_filter;          // Mip,Min,Mag filtering to use with this texture
			//TextureAddrMode           m_addr_mode;       // The addressing mode to use with this texture
			RdrId                     m_id;              // Id for this texture in the material managers lookup map
			MaterialManager*          m_mat_mgr;         // The material manager that created this texture
			string32                  m_name;            // Human readable id for the texture
			//VideoPtr                  m_video;           // Non-null if this texture is the output of a video
			
			Texture2D();
			
			// Refcounting cleanup function
			static void RefCountZero(pr::RefCount<Texture2D>* doomed);
		};
	}
}

#endif
