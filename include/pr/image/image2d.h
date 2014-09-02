//********************************************************
//
//	Image2D
//
//********************************************************
#ifndef PR_IMAGE_2D_H
#define PR_IMAGE_2D_H

#include "pr/common/D3DPtr.h"
#include "pr/maths/maths.h"
#include "pr/Image/Types.h"
#include "pr/Image/ImageInfo.h"
#include "pr/Image/Image2DIter.h"

namespace pr
{
	namespace image
	{
		struct Image2D
		{
			typedef impl::Image2DIter iterator;

			Image2D(const ImageInfo& info, D3DPtr<IDirect3DTexture9> image) : m_info(info), m_image(image) {}
			iterator Lock(image::Lock& lock, uint mip_level = 0, const IRect* area = 0, uint flags = 0);

			ImageInfo					m_info;
			D3DPtr<IDirect3DTexture9>	m_image;
		};

	}//namespace image
}//namespace pr

#endif//PR_IMAGE_2D_H