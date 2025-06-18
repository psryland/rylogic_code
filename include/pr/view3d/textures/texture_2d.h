//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/config/config.h"
#include "pr/view3d/textures/texture_base.h"
//#include "pr/view3d/textures/video.h"

namespace pr::rdr
{
	struct Texture2D :TextureBase
	{
		// Notes:
		//   - Each time MatMgr.CreateTexture is called, a new Texture2D instance is allocated.
		//     However, the resources associated with this texture may be shared with other Textures.

		m4x4      m_t2s;       // Texture to surface transform
		SortKeyId m_sort_id;   // A sort key component for this texture
		bool      m_has_alpha; // True if the texture contains alpha pixels

		Texture2D(TextureManager* mgr, RdrId id, ID3D11Texture2D* tex, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name);
		Texture2D(TextureManager* mgr, RdrId id, ID3D11Texture2D* tex, ID3D11ShaderResourceView* srv, SamplerDesc const& sam_desc, SortKeyId sort_id, bool has_alpha, char const* name);
		Texture2D(TextureManager* mgr, RdrId id, IUnknown* shared_resource, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name);
		Texture2D(TextureManager* mgr, RdrId id, HANDLE shared_handle, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name);
		Texture2D(TextureManager* mgr, RdrId id, Image const& src, Texture2DDesc const& tdesc, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name, ShaderResourceViewDesc const* srvdesc = nullptr);
		Texture2D(TextureManager* mgr, RdrId id, Texture2D& existing, char const* name);

		// Get the DirectX texture 2D resource
		ID3D11Texture2D const* dx_tex() const
		{
			return static_cast<ID3D11Texture2D const*>(m_res.get());
		}
		ID3D11Texture2D* dx_tex()
		{
			return const_call(dx_tex());
		}

		// Get the description of the current texture pointed to by 'm_tex'
		Texture2DDesc TexDesc() const;

		// Set a new texture description and re-create/reinitialise the texture and the SRV.
		// 'all_instances' - if true, all Texture2D objects that refer to the same underlying
		//  dx texture get updated as well. If false, then this texture becomes a unique instance
		//  and 'm_id' is changed.
		// 'perserve' - if true, the content of the current texture is stretch copied to the new texture
		//  if possible. If not possible, an exception is thrown
		// 'srvdesc' - if not null, causes the new shader resource view to be created using this description
		void TexDesc(Image const& src, Texture2DDesc const& tdesc, bool all_instances, bool preserve, ShaderResourceViewDesc const* srvdesc = nullptr);

		// Resize this texture to 'size' optionally applying the resize to all instances of this
		// texture and optionally preserving the current content of the texture
		void Resize(size_t width, size_t height, bool all_instances, bool preserve);

		// Access the raw pixel data of this texture.
		// If EMapFlags::DoNotWait is used, the returned image may contain a null
		// pointer for the pixel data. This is because the resource is not available.
		Image GetPixels(Lock& lock, UINT sub = 0, EMap map_type = EMap::WriteDiscard, EMapFlags flags = EMapFlags::None, Range range = Range::Zero());

		// Get/Release the DC (prefer the Gfx class for RAII)
		// Note: Only works for textures created with GDI compatibility
		HDC GetDC(bool discard);
		void ReleaseDC();

		// Get the DXGI surface within this texture
		D3DPtr<IDXGISurface> GetSurface();

		// Get a d2d render target for the DXGI surface within this texture.
		// 'wnd' is optional, used to get the DPI scaling for the window that the render target is used in.
		D3DPtr<ID2D1RenderTarget> GetD2DRenderTarget(Window const* wnd = nullptr);

		// Get a D2D device context for the DXGI surface within this texture
		D3DPtr<ID2D1DeviceContext> GetD2DeviceContext();

		// Unique identifiers for data attached to the private data of this texture
		static GUID const Surface0Pointer;

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
	};
}
