//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/texture/texture_base.h"

namespace pr::rdr12
{
	struct Texture2D :TextureBase
	{
		// Notes:
		//   - Texture2D (and derived objects) are lightweight, they are basically reference
		//     counted pointers to D3D resources.
		//   - Textures have value semantics (i.e. copyable)
		//   - Each time CreateTexture is called, a new texture instance is allocated.
		//     However, the resources associated with the texture may be shared with other textures.

		m4x4 m_t2s; // Texture to surface transform

		Texture2D(ResourceManager& mgr, ID3D12Resource* res, TextureDesc const& desc);

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
