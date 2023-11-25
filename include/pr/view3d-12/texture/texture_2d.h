//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/texture/texture_base.h"
#include "pr/view3d-12/texture/d2d_context.h"

namespace pr::rdr12
{
	struct Texture2D :TextureBase
	{
		m4x4 m_t2s; // Texture to surface transform

		Texture2D(ResourceManager& mgr, ID3D12Resource* res, TextureDesc const& desc);

		// Get/Release the DC (prefer the Gfx class for RAII)
		// Note: Only works for textures created with GDI compatibility
		HDC GetDC(bool discard);
		void ReleaseDC();

		#if 0
		// Get a d2d render target for the DXGI surface within this texture.
		// 'wnd' is optional, used to get the DPI scaling for the window that the render target is used in.
		D3DPtr<ID2D1RenderTarget> GetD2DRenderTarget(Window const* wnd = nullptr);
		#endif

		// Get a D2D device context for drawing on this texture. Note: the texture must have
		// been created with: EUsage::RenderTarget|EUsage::SimultaneousAccess and D3D12_HEAP_FLAG_SHARED
		D2D1Context GetD2DeviceContext() { return D2D1Context(rdr(), m_res.get()); }

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
			DC(DC&&) = default;
			DC(DC const&) = delete;
			DC& operator=(DC&&) = default;
			DC& operator=(DC const&) = delete;
			~DC() { m_tex->ReleaseDC(); }
		};

		// A scoped device context to allow GDI+ edits of the texture
		#ifdef _GDIPLUS_H
		struct Gfx :gdi::Graphics
		{
			Texture2D* m_tex;

			Gfx(Texture2D* tex, bool discard)
				:gdi::Graphics(tex->GetDC(discard))
				,m_tex(tex)
			{}
			Gfx(Gfx&&) = default;
			Gfx(Gfx const&) = delete;
			Gfx& operator=(Gfx&&) = default;
			Gfx& operator=(Gfx const&) = delete;
			~Gfx() { m_tex->ReleaseDC(); }
		};
		#endif
	};
}

