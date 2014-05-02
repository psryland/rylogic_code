//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
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
			D3DPtr<IDXGISurface1> m_surf;

			TextureGdi(TextureManager* mgr, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc, SortKeyId sort_id, ShaderResViewDesc const* srvdesc = nullptr);

			// A scoped device context to allow GDI+ edits of the texture
			class Gfx :public Gdiplus::Graphics
			{
				TextureGdiPtr m_tex;
				Gfx(Gfx const&);
				Gfx& operator=(Gfx const&);
			public:
				Gfx(TextureGdiPtr& tex);
				~Gfx();
			};

		private:

			HDC GetDC();
			void ReleaseDC();

			// Refcounting cleanup function
			protected: void Delete() override;
		};
	}
}
