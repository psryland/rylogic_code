//********************************************************
//
//	ImageInfo
//
//********************************************************
#ifndef PR_IMAGE_INFO_H
#define PR_IMAGE_INFO_H

#include <string>
#include <d3dx9.h>
#include "pr/maths/maths.h"
#include "pr/common/colour.h"

namespace pr
{
	namespace image
	{
		// This contains the data for loading/saving/creating images
		struct ImageInfo : public D3DXIMAGE_INFO
		{
			ImageInfo()
			{
				m_filename			= "";
				Width				= D3DX_DEFAULT | D3DX_DEFAULT_NONPOW2;
				Height				= D3DX_DEFAULT | D3DX_DEFAULT_NONPOW2;
				Depth				= D3DX_DEFAULT | D3DX_DEFAULT_NONPOW2;
				MipLevels			= 1;
				Format				= D3DFMT_UNKNOWN;
				ResourceType		= D3DRTYPE_TEXTURE;
				ImageFileFormat		= D3DXIFF_BMP;
				m_usage				= 0;
				m_pool				= D3DPOOL_MANAGED;
				m_filter			= D3DX_FILTER_NONE;
				m_mip_filter		= D3DX_FILTER_NONE;
				m_color_key			= 0xFF000000;
				ZeroMemory(m_palette, sizeof(m_palette));
			}
			ImageInfo(const char* filename);

			std::string		m_filename;
			uint			m_usage;
			D3DPOOL			m_pool;
			uint			m_filter;
			uint			m_mip_filter;
			Colour32 		m_color_key;
			PALETTEENTRY	m_palette[256];
		};
	}//namespace image
}//namespace pr

#endif//PR_IMAGE_INFO_H
