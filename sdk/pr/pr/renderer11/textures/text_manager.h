#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/textures/text.h"

namespace pr
{
	namespace rdr
	{
		#define PR_ENUM(x)\
			x(Unknown)\
			x(Gabriola)
		PR_DEFINE_ENUM1(EFont, PR_ENUM);
		#undef PR_ENUM

		// Wraps a texture containing text
		class TextManager
		{
			typedef pr::Array<FontPtr> FontCont;

			Allocator<Text>        m_alex_text;
			D3DPtr<ID3D11Device>   m_device;
			D3DPtr<ID3D10Device1>  m_device10_1;
			TextureManager&        m_tex_mgr;
			D3DPtr<ID2D1Factory>   m_d2dfactory;
			D3DPtr<IDWriteFactory> m_dwfactory;
			FontCont               m_fonts;

			TextManager(TextManager const&);
			TextManager& operator=(TextManager const&);

			friend struct Text;
			void Delete(Text* text);

		public:

			TextManager(MemFuncs& mem, D3DPtr<ID3D11Device>& device, D3DPtr<ID3D10Device1>& device10_1, TextureManager& tex_mgr);

			// Create a layout object for some text
			// 'sx','sy' are the size of the bounding rectangle in which to layout
			TextLayoutPtr CreateLayout(std::wstring text, EFont font, size_t sx = 256, size_t sy = 256);

			// Create a new text instance
			// sx,sy are the max size of the text texture
			TextPtr CreateText(std::wstring text, TextLayoutPtr layout);

			// Create text texture instance with default layout and size 'sx','sy'
			TextPtr CreateText(std::wstring text, EFont font, size_t sx = 256, size_t sy = 256);
			TextPtr CreateText(wchar_t const *text, EFont font) { return CreateText(std::wstring(text), font); }
		};
	}
}
