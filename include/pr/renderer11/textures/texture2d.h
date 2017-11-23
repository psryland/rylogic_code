//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

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

			Texture2D(TextureManager* mgr, RdrId id, ID3D11Texture2D* tex, SamplerDesc const& sam_desc, SortKeyId sort_id, bool has_alpha, char const* name);
			Texture2D(TextureManager* mgr, RdrId id, ID3D11Texture2D* tex, ID3D11ShaderResourceView* srv, SamplerDesc const& sam_desc, SortKeyId sort_id, bool has_alpha, char const* name);
			Texture2D(TextureManager* mgr, RdrId id, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name, ShaderResourceViewDesc const* srvdesc = nullptr);
			Texture2D(TextureManager* mgr, RdrId id, Texture2D const& existing, SortKeyId sort_id, char const* name);

			// Get the description of the current texture pointed to by 'm_tex'
			TextureDesc TexDesc() const;

			// Set a new texture description and re-create/reinitialise the texture and the SRV.
			// 'all_instances' - if true, all Texture2D objects that refer to the same underlying
			//  dx texture get updated as well. If false, then this texture becomes a unique instance
			//  and 'm_id' is changed.
			// 'perserve' - if true, the content of the current texture is stretch copied to the new texture
			//  if possible. If not possible, an exception is thrown
			// 'srvdesc' - if not null, causes the new shader resource view to be created using this description
			void TexDesc(Image const& src, TextureDesc const& tdesc, bool all_instances, bool preserve, ShaderResourceViewDesc const* srvdesc = nullptr);

			// Get/Set the description of the current sampler state pointed to by 'm_samp'
			// Setting a new sampler description, re-creates the sampler state
			SamplerDesc SamDesc() const;
			void SamDesc(SamplerDesc const& desc);

			// Set the filtering and address mode for this texture
			void SetFilterAndAddrMode(D3D11_FILTER filter, D3D11_TEXTURE_ADDRESS_MODE addrU, D3D11_TEXTURE_ADDRESS_MODE addrV);

			// Resize this texture to 'size' optionally applying the resize to all instances of this
			// texture and optionally preserving the current content of the texture
			void Resize(size_t width, size_t height, bool all_instances, bool preserve);

			// Get/Release the DC (prefer the Gfx class for RAII)
			// Note: Only works for textures created with GDI compatibility
			HDC GetDC(bool discard);
			void ReleaseDC();

			// A scope object for the device context
			struct DC
			{
				Texture2D* m_tex;
				HDC m_hdc;

				DC(Texture2D* tex, bool discard)
					:m_tex(tex)
					,m_hdc(tex->GetDC(discard))
				{}
				~DC()
				{
					m_tex->ReleaseDC();
				}
				DC(DC const&) = delete;
				DC& operator=(DC const&) = delete;
			};

			// A scoped device context to allow GDI+ edits of the texture
			#ifdef _GDIPLUS_H
			class Gfx :public gdi::Graphics
			{
				Texture2D* m_tex;

			public:
				Gfx(Texture2D* tex, bool discard)
					:gdi::Graphics(tex->GetDC(discard))
					,m_tex(tex)
				{}
				~Gfx()
				{
					m_tex->ReleaseDC();
				}
				Gfx(Gfx const&) = delete;
				Gfx& operator=(Gfx const&) = delete;
			};
			#endif

			// Get the DXGI surface within this texture
			D3DPtr<IDXGISurface> GetSurface();

			// Get a d2d render target for the DXGI surface within this texture
			D3DPtr<ID2D1RenderTarget> GetD2DRenderTarget();

			// Get a D2D device context for the DXGI surface within this texture
			D3DPtr<ID2D1DeviceContext> GetD2DeviceContext();

			// Ref counting clean up
			static void RefCountZero(pr::RefCount<Texture2D>* doomed);
			protected: virtual void Delete();
		};
	}
}
