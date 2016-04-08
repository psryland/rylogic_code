//*****************************************
// Font Helper
//	(c)opyright Paul Ryland 2009
//*****************************************
#pragma once
#include <windows.h>

namespace pr
{
	namespace gui
	{
		namespace font
		{
			enum Types { Raster, Vector, TypeType, NumberOf };
			enum EFamily
			{
				CourierNew,
				Tahoma,
			};
			inline TCHAR const* FamilyStr(EFamily fam)
			{
				switch (fam)
				{
				default:         return _T("");
				case CourierNew: return _T("couriernew");
				case Tahoma:     return _T("tahoma");
				}
			}
		}

		// Wrapper around the ::CreateFont function
		inline HFONT CreateFontSimple(font::EFamily family, int height, int width = 0)
		{
			if (width == 0) width = height/4;
			return ::CreateFont(height, width, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE, ANSI_CHARSET, OUT_TT_PRECIS, CLIP_DEFAULT_PRECIS, ANTIALIASED_QUALITY, DEFAULT_PITCH, font::FamilyStr(family));
		}
	}
}
