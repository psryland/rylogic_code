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
		// 'Text' is just a 2d texture with support for rendering text into itself
		struct Text :Texture2D
		{
			std::wstring                 m_text;    // The string to be renderered into the texture
			TextLayoutPtr                m_layout;  // The layout properties of the text
			pr::Colour                   m_colour;  // Font colour
			TextManager*                 m_mgr;     // The text manager that created this text
			D3DPtr<ID2D1SolidColorBrush> m_brush;   // The brush to draw with
			D2D1_DRAW_TEXT_OPTIONS       m_options; // Text drawing options
			D3DPtr<ID2D1RenderTarget>    m_rt;      // d2d render target
			D3DPtr<IDXGIKeyedMutex>      m_keyed_mutex11;
			D3DPtr<IDXGIKeyedMutex>      m_keyed_mutex10;

			Text(TextManager* mgr, std::wstring text, TextLayoutPtr layout, pr::Colour const& colour = pr::ColourBlack);

			// Render a string into the texture
			void RenderText(std::wstring text, pr::Colour const& fore);
			void RenderText() { RenderText(m_text, m_colour); }

			// Refcounting cleanup function
			protected: void Delete() override;
		};

		typedef pr::RefPtr<Text> TextPtr;
	}
}
