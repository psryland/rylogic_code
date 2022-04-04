//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	// A basic image description
	struct Image
	{
		// Notes:
		//  - row pitch is the number of bytes per row of the image
		//  - slice pitch is the number of bytes per 2D plane (i.e. normally the image size in bytes,
		//    but if the image is an array, then this is the size of one image in the array)
		//  - 3D images should be represented with arrays of 'Image's.

		iv2 m_dim;            // x = width, y = height
		iv2 m_pitch;          // x = row pitch, y = slice pitch
		DXGI_FORMAT m_format; // The pixel format of the image
		void const* m_pixels; // A const pointer to the image data

		Image() = default;

		// Use to construct an instance of an Image.
		// 'pixels' should point to data in the format 'fmt' for the base image
		// (dimensions should be m_row_pitch x m_slice_pitch, use pr::rdr::Pitch())
		Image(int w, int h, void const* pixels = nullptr, DXGI_FORMAT fmt = DXGI_FORMAT_B8G8R8A8_UNORM)
			:m_dim(w, h)
			,m_pitch(Pitch(m_dim, fmt))
			,m_format(fmt)
			,m_pixels(pixels)
		{}

		// Return a pointer to the pixels of a type suitable for the format
		template <typename PixelType> PixelType const* Pixels(int row) const
		{
			PR_ASSERT(PR_DBG_RDR, sizeof(PixelType) == BitsPerPixel(m_format) / 8, "Pointer type is not the correct size for the image format");
			auto ptr = byte_ptr(m_pixels) + m_pitch.x * row;
			return type_ptr<PixelType>(ptr);
		}
		template <typename PixelType> PixelType* Pixels(int row)
		{
			PR_ASSERT(PR_DBG_RDR, sizeof(PixelType) == BitsPerPixel(m_format) / 8, "Pointer type is not the correct size for the image format");
			auto ptr = const_cast<uint8_t*>(byte_ptr(m_pixels) + m_pitch.x * row);
			return type_ptr<PixelType>(ptr);
		}

		// Convert to Dx12 types
		operator D3D12_SUBRESOURCE_DATA() const
		{
			D3D12_SUBRESOURCE_DATA data = {};
			data.pData = m_pixels;
			data.RowPitch = s_cast<LONG_PTR>(m_pitch.x);
			data.SlicePitch = s_cast<LONG_PTR>(m_pitch.y);
			return data;
		}
		operator D3D12_MEMCPY_DEST()
		{
			D3D12_MEMCPY_DEST data = {};
			data.pData = const_cast<void*>(m_pixels);
			data.RowPitch = s_cast<LONG_PTR>(m_pitch.x);
			data.SlicePitch = s_cast<LONG_PTR>(m_pitch.y);
			return data;
		}
	};
}
