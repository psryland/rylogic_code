//********************************************************
//
//	ImageManipulator / Image
//
//********************************************************

#ifndef PR_IMAGE_H
#define PR_IMAGE_H

#include "pr/common/D3DPtr.h"
#include "pr/Image/Types.h"
#include "pr/Image/Image2D.h"
#include "pr/Image/ImageCube.h"
#include "pr/Image/ImageAssertEnable.h"

namespace pr
{
	namespace image
	{
		struct Context
		{
			// Use this if you don't have a d3d device. A d3d interface and device will be created. 
			// If you're running from a console app use GetConsoleWindow(). You'll need
			// to include windows and define _WIN32_WINNT >= 0x0500 in your project settings
			static Context make(HWND hwnd);

			D3DPtr<IDirect3D9>			m_d3d;
			D3DPtr<IDirect3DDevice9>	m_d3d_device;
		};

		struct Lock
		{
			Lock() : m_image(0), m_mip_level(0) {}
			~Lock();
			D3DPtr<IDirect3DTexture9>	m_image;
			int							m_mip_level;
		};

		// Image2D methods
		Image2D Create2DImage(Context& context, const ImageInfo& image_info);
		Image2D Load2DImage  (Context& context, const ImageInfo& image_info);
		bool    Save2DImage  (const Image2D& image);

	}//namespace image
}//namespace pr

#endif//PR_IMAGE_H
