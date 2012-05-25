//********************************************************
//
//	ImageCube
//
//********************************************************
#ifndef PR_IMAGE_CUBE_H
#define PR_IMAGE_CUBE_H

#include "pr/common/D3DPtr.h"
#include "pr/Image/ImageInfo.h"

namespace pr
{
	namespace image
	{
		// The image type represents a CubeMap image
		struct ImageCube
		{
			ImageCube(const ImageInfo& info, D3DPtr<IDirect3DCubeTexture9> image) : m_info(info), m_image(image) {}
		
			ImageInfo						m_info;
			D3DPtr<IDirect3DCubeTexture9>	m_image;
		};
	}//namespace image
}//namespace pr

#endif//PR_IMAGE_CUBE_H