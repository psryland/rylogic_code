//*******************************************************
// GDI+ helpers
//  Copyright (c) Rylogic Limited 2009
//*******************************************************

#pragma once
#ifndef NOGDI

#pragma warning(disable:4458)
#include <vector>
#include <windows.h>
#include <gdiplus.h>
#include <objidl.h>
#include <shlwapi.h>
#pragma warning(default:4458)

#include "pr/common/to.h"

#pragma comment(lib, "gdiplus.lib")

namespace pr
{
	// RAII object for initialised/shutting down the GdiPlus framework
	struct GdiPlus
	{
		ULONG_PTR m_token;
		Gdiplus::GdiplusStartupInput  m_startup_input;
		Gdiplus::GdiplusStartupOutput m_startup_output;

		GdiPlus()  { Gdiplus::GdiplusStartup(&m_token, &m_startup_input, &m_startup_output); }
		~GdiPlus() { Gdiplus::GdiplusShutdown(m_token); }
	};

	namespace convert
	{
		template <typename Str, typename Char = typename Str::value_type>
		struct GdiToString
		{
		};
		struct ToGdiColor
		{
			static Gdiplus::Color To(COLORREF col)
			{
				Gdiplus::Color c;
				c.SetFromCOLORREF(col);
				return c;
			}
		};
		struct ToGdiRect
		{
			static Gdiplus::Rect To(RECT const& r)
			{
				return Gdiplus::Rect(r.left, r.top, r.right - r.left, r.bottom - r.top);
			}
			static Gdiplus::Rect To(Gdiplus::RectF const& r)
			{
				return Gdiplus::Rect(int(r.X), int(r.Y), int(r.Width), int(r.Height));
			}
		};
		struct ToGdiRectF
		{
			static Gdiplus::RectF To(RECT const& r)
			{
				return Gdiplus::RectF(float(r.left), float(r.top), float(r.right - r.left), float(r.bottom - r.top));
			}
		};
	}
	template <typename TFrom> struct Convert<Gdiplus::Color, TFrom> :convert::ToGdiColor {};
	template <typename TFrom> struct Convert<Gdiplus::Rect , TFrom> :convert::ToGdiRect {};
	template <typename TFrom> struct Convert<Gdiplus::RectF, TFrom> :convert::ToGdiRectF {};
}

namespace Gdiplus
{
	// Singleton for accessing image codecs
	class ImageCodec
	{
		std::vector<ImageCodecInfo> m_codecs;
		ImageCodec()
		{
			UINT num = 0;  // number of image encoders
			UINT size = 0; // size of the image encoder array in bytes
			if (GetImageEncodersSize(&num, &size) != Gdiplus::Status::Ok)
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
		auto extn = PathFindExtensionW(filepath);
		if (*extn == 0) throw std::exception("Image save could not infer the image format from the file extension");
		auto mime = std::wstring(L"image/").append(++extn);
		return const_cast<Image&>(image).Save(filepath, &ImageCodec::Clsid(mime.c_str()));
	}
}

#endif