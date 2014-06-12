//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
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
		// Todo, make a Texture base class
		// A 2D texture
		// Each time MatMgr.CreateTexture is called, a new Texture2D instance is allocated.
		// However, the resources associated with this texture may be shared with other Textures.
		struct Texture2D
			:pr::RefCount<Texture2D>
		{
			pr::m4x4                         m_t2s;       // Texture to surface transform
			D3DPtr<ID3D11Texture2D>          m_tex;       // The texture resource
			D3DPtr<ID3D11ShaderResourceView> m_srv;       // A shader resource view of the texture
			D3DPtr<ID3D11SamplerState>       m_samp;      // The sampler state to use with this texture
			RdrId                            m_id;        // Id for this texture in the texture managers lookup map
			RdrId                            m_src_id;    // An id identifying the source this texture was created from (needed when deleting the last ref to a dx tex)
			SortKeyId                        m_sort_id;   // A sort key component for this texture
			bool                             m_has_alpha; // True if the texture contains alpha pixels
			TextureManager*                  m_mgr;       // The texture manager that created this texture
			string32                         m_name;      // Human readable id for the texture

			Texture2D(TextureManager* mgr, D3DPtr<ID3D11Texture2D> tex, D3DPtr<ID3D11ShaderResourceView> srv, SamplerDesc const& sam_desc, SortKeyId sort_id);
			Texture2D(TextureManager* mgr, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc, SortKeyId sort_id, ShaderResViewDesc const* srvdesc = nullptr);
			Texture2D(TextureManager* mgr, Texture2D const& existing, SortKeyId sort_id);

			// Get the description of the current texture pointed to by 'm_tex'
			TextureDesc TexDesc() const;

			// Set a new texture description and re-create/reinitialise the texture and the srv.
			// 'all_instances' - if true, all Texture2D objects that refer to the same underlying
			//  dx texture get updated as well. If false, then this texture becomes a unique instance
			//  and 'm_id' is changed.
			// 'perserve' - if true, the content of the current texture is stretch blted to the new texture
			//  if possible. If not possible, an exception is thrown
			// 'srvdesc' - if not null, causes the new shader resourve view to be created using this description
			void TexDesc(Image const& src, TextureDesc const& tdesc, bool all_instances, bool preserve, ShaderResViewDesc const* srvdesc = nullptr);

			// Get/Set the description of the current sampler state pointed to by 'm_samp'
			// Setting a new sampler description, re-creates the sampler state
			SamplerDesc SamDesc() const;
			void SamDesc(SamplerDesc const& desc);

			// Resize this texture to 'size' optionally applying the resize to all instances of this
			// texture and optionally preserving the current content of the texture
			void Resize(size_t width, size_t height, bool all_instances, bool preserve);

			// Get/Release the DC (prefer the Gfx class for RAII)
			// Note: Only works for textures created with GDI compatibility
			HDC GetDC();
			void ReleaseDC();

			// A scoped device context to allow GDI+ edits of the texture
			class Gfx :public Gdiplus::Graphics
			{
				Texture2DPtr m_tex;
				Gfx(Gfx const&);
				Gfx& operator=(Gfx const&);
			public:
				Gfx(Texture2DPtr& tex) :Gdiplus::Graphics(tex->GetDC()) ,m_tex(tex) {}
				~Gfx() { m_tex->ReleaseDC(); }
			};

			// Ref counting cleanup
			static void RefCountZero(pr::RefCount<Texture2D>* doomed);
			protected: virtual void Delete();
		};
	}
}

#endif
