//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_TEXTURES_IMAGE_H
#define PR_RDR_TEXTURES_IMAGE_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/util.h"

namespace pr
{
	namespace rdr
	{
		// A basic image description
		struct Image
		{
			pr::ISize   m_dim;
			pr::ISize   m_pitch;
			DXGI_FORMAT m_format;
			void const* m_pixels;

			// Use to construct an instance of an Image.
			// 'pixels' should point to data in the format 'fmt' for the base image
			// (dimensions should be m_row_pitch x m_slice_pitch, use pr::rdr::Pitch())
			static Image make(size_t w, size_t h, void const* pixels = nullptr, DXGI_FORMAT fmt = DXGI_FORMAT_R8G8B8A8_UNORM)
			{
				Image img;
				img.m_dim.set(w, h);
				img.m_pitch = pr::rdr::Pitch(img.m_dim, fmt);
				img.m_format = fmt;
				img.m_pixels = pixels;
				return img;
			}
		};
	}
}

#endif
