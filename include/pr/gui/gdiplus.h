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
	// RAII object for initialised/shutting down the gdiplus framework
	struct GdiPlus
	{
		ULONG_PTR m_token;
		Gdiplus::GdiplusStartupInput  m_startup_input;
		Gdiplus::GdiplusStartupOutput m_startup_output;

		GdiPlus()  { Gdiplus::GdiplusStartup(&m_token, &m_startup_input, &m_startup_output); }
		~GdiPlus() { Gdiplus::GdiplusShutdown(m_token); }
	};

	// Conversion
	template <typename TFrom> struct Convert<Gdiplus::Color, TFrom>
	{
		static Gdiplus::Color To(COLORREF col) { Gdiplus::Color c; c.SetFromCOLORREF(col); return c; }
	};

	// To<Gdiplus::Rect>
	template <typename TFrom> struct Convert<Gdiplus::Rect, TFrom>
	{
		static Gdiplus::Rect To(RECT const& r)           { return Gdiplus::Rect(r.left, r.top, r.right - r.left, r.bottom - r.top); }
		static Gdiplus::Rect To(Gdiplus::RectF const& r) { return Gdiplus::Rect(int(r.X), int(r.Y), int(r.Width), int(r.Height)); }
	};

	// To<Gdiplus::RectF>
	template <typename TFrom> struct Convert<Gdiplus::RectF,TFrom>
	{
		static Gdiplus::RectF To(RECT const& x) { return Gdiplus::RectF(float(rect.left), float(rect.top), float(rect.right - rect.left), float(rect.bottom - rect.top)); }
	};

	// To<RECT>
	template <> struct Convert<RECT, Gdiplus::Rect>
	{
		static RECT To(Gdiplus::Rect const& r) { return RECT{r.GetLeft(), r.GetTop(), r.GetRight(), r.GetBottom()}; }
	};
	template <> struct Convert<RECT, Gdiplus::RectF>
	{
		static RECT To(Gdiplus::RectF const& r) { return RECT{int(r.GetLeft()), int(r.GetTop()), int(r.GetRight()), int(r.GetBottom())}; }
	};
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

	// Helper for saving Gdi Images that infers the codec from the file extension
	inline Status Save(Image const& image, wchar_t const* filepath)
	{
		auto extn = PathFindExtensionW(filepath);
		if (*extn == 0) throw std::exception("Image save could not infer the image format from the file extension");
		auto mime = std::wstring(L"image/").append(++extn);
		return const_cast<Image&>(image).Save(filepath, &ImageCodec::Clsid(mime.c_str()));
	}
}

#endif