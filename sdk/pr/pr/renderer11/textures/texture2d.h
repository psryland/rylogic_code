//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_MATERIALS_TEXTURE_2D_H
#define PR_RDR_MATERIALS_TEXTURE_2D_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/config/config.h"
//#include "pr/renderer11/textures/video.h"

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
			pr::m4x4                         m_t2s;       // Texture to surface transform
			D3DPtr<ID3D11Texture2D>          m_tex;       // The texture resource
			D3DPtr<ID3D11ShaderResourceView> m_srv;       // A shader resource view of the texture
			D3DPtr<ID3D11SamplerState>       m_samp;      // The sampler state to use with this texture
			RdrId                            m_id;        // Id for this texture in the texture managers lookup map
			RdrId                            m_src_id;    // An id identifying the source this texture was created from (needed when deleting the last ref to a d3d tex)
			SortKeyId                        m_sort_id;   // A sort key component for this texture
			bool                             m_has_alpha; // True if the texture contains alpha pixels
			TextureManager*                  m_mgr;       // The texture manager that created this texture
			string32                         m_name;      // Human readable id for the texture
			//VideoPtr                         m_video;     // Non-null if this texture is the output of a video

			Texture2D(TextureManager* mgr, D3DPtr<ID3D11Texture2D>& tex, D3DPtr<ID3D11ShaderResourceView>& srv, SamplerDesc const& sam_desc, SortKeyId sort_id);
			Texture2D(TextureManager* mgr, TextureDesc const& tex_desc, SamplerDesc const& sam_desc, void const* data, SortKeyId sort_id);
			Texture2D(TextureManager* mgr, Texture2D const& existing, SortKeyId sort_id);

			// Get/Set the description of the current texture pointed to by 'm_tex'
			// Setting a new texture description, re-creates the texture and the srv
			TextureDesc TexDesc() const;
			void TexDesc(TextureDesc const& desc, void const* data);

			// Get/Set the description of the current sampler state pointed to by 'm_samp'
			// Setting a new sampler description, re-creates the sampler state
			SamplerDesc SamDesc() const;
			void SamDesc(SamplerDesc const& desc);

			static void RefCountZero(pr::RefCount<Texture2D>* doomed);
		};
	}
}

#endif
