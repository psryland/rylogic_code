//*******************************************************
// GDI+ helpers
//  Copyright (c) Rylogic Limited 2009
//*******************************************************
#pragma once

#ifndef NOGDI

#ifdef _GDIPLUS_H
#error _GDIPLUS_H already included
#endif

#include <vector>
#include <gdiplus.h>
#include "pr/common/to.h"
#include "pr/filesys/filesys.h"

#pragma comment(lib, "gdiplus.lib")

namespace pr
{
	namespace gdi
	{
		// Import the 'Gdiplus' namespace into 'pr::gdi'
		using namespace Gdiplus;

		// Singleton for accessing image codecs
		class ImageCodec
		{
			std::vector<ImageCodecInfo> m_codecs;
			ImageCodec()
			{
				UINT num = 0;  // number of image encoders
				UINT size = 0; // size of the image encoder array in bytes
				if (GetImageEncodersSize(&num, &size) != pr::gdi::Status::Ok)
					throw std::exception("GDI+ Image encoders not available");

				if (size != 0)
				{
					m_codecs.resize(size);
					GetImageEncoders(num, size, m_codecs.data());
				}
			}
			static ImageCodec const& This()
			{
				static ImageCodec inst;
				return inst;
			}

		public:
			static ImageCodecInfo const& Info(wchar_t const* mime)
			{
				for (auto& codec : This().m_codecs)
				{
					if (wcscmp(codec.MimeType, mime) != 0) continue;
					return codec;
				}
				throw std::exception("Image codec not found");
			}
			static CLSID const& Clsid(wchar_t const* mime)
			{
				return Info(mime).Clsid;
			}
		};

		// Helper for saving GDI Images that infers the codec from the file extension
		inline Status Save(Image const& image, wchar_t const* filepath)
		{
			auto extn = pr::filesys::GetExtensionInPlace(filepath);
			if (*extn == 0)
				throw std::exception("Image save could not infer the image format from the file extension");

			auto mime = std::wstring(L"image/").append(extn);
			return const_cast<Image&>(image).Save(filepath, &ImageCodec::Clsid(mime.c_str()));
		}
	}

	// RAII object for initialised/shutting down the GdiPlus framework
	struct GdiPlus
	{
		ULONG_PTR m_token;
		gdi::GdiplusStartupInput  m_startup_input;
		gdi::GdiplusStartupOutput m_startup_output;

		GdiPlus()  { gdi::GdiplusStartup(&m_token, &m_startup_input, &m_startup_output); }
		~GdiPlus() { gdi::GdiplusShutdown(m_token); }
	};

	namespace convert_gdi
	{
		template <typename Str, typename Char = typename Str::value_type>
		struct GdiToString
		{
		};
		struct ToGdiColor
		{
			static gdi::Color To(COLORREF col)
			{
				gdi::Color c;
				c.SetFromCOLORREF(col);
				return c;
			}
		};
		struct ToGdiRect
		{
			static gdi::Rect To(RECT const& r)
			{
				return gdi::Rect(r.left, r.top, r.right - r.left, r.bottom - r.top);
			}
			static gdi::Rect To(gdi::RectF const& r)
			{
				return gdi::Rect(int(r.X), int(r.Y), int(r.Width), int(r.Height));
			}
		};
		struct ToGdiRectF
		{
			static gdi::RectF To(RECT const& r)
			{
				return gdi::RectF(float(r.left), float(r.top), float(r.right - r.left), float(r.bottom - r.top));
			}
		};
		struct ToRECT
		{
			static RECT To(gdi::RectF const& r, DummyType<4> = 0)
			{
				return RECT{int(r.GetLeft()), int(r.GetTop()), int(r.GetRight()), int(r.GetBottom())};
			}
			static RECT To(gdi::Rect const& r, DummyType<5> = 0)
			{
				return RECT{r.GetLeft(), r.GetTop(), r.GetRight(), r.GetBottom()};
			}
		};
	}
	template <typename TFrom> struct Convert<gdi::Color, TFrom> :convert_gdi::ToGdiColor {};
	template <typename TFrom> struct Convert<gdi::Rect , TFrom> :convert_gdi::ToGdiRect {};
	template <typename TFrom> struct Convert<gdi::RectF, TFrom> :convert_gdi::ToGdiRectF {};
	template <> struct Convert<RECT, gdi::RectF> :convert_gdi::ToRECT {};
	template <> struct Convert<RECT, gdi::Rect> :convert_gdi::ToRECT {};
}

#endif // NOGDI