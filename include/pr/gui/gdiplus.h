//*******************************************************
// GDI+ helpers
//  Copyright (c) Rylogic Limited 2009
//*******************************************************

#pragma once

#include <vector>
#include <windows.h>
#include <objidl.h>
#include <shlwapi.h>
#include <gdiplus.h>

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
