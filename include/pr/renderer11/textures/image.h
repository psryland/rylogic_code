//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/util.h"

namespace pr
{
	namespace rdr
	{
		// A basic image description
		struct Image
		{
			iv2   m_dim;
			iv2   m_pitch;
			DXGI_FORMAT m_format;
			void const* m_pixels;

			Image() = default;

			// Use to construct an instance of an Image.
			// 'pixels' should point to data in the format 'fmt' for the base image
			// (dimensions should be m_row_pitch x m_slice_pitch, use pr::rdr::Pitch())
			Image(int w, int h, void const* pixels = nullptr, DXGI_FORMAT fmt = DXGI_FORMAT_R8G8B8A8_UNORM)
				:m_dim(w, h)
				,m_pitch(Pitch(m_dim, fmt))
				,m_format(fmt)
				,m_pixels(pixels)
			{}
		};
	}
}
