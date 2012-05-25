//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_UTIL_UTIL_H
#define PR_RDR_UTIL_UTIL_H

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		// Make a RdrId from a pointer
		template <typename T> RdrId MakeId(T const* ptr)
		{
			return static_cast<RdrId>(ptr - (T const*)0);
		}
		
		// Make a RdrId from 'unique_name'
		template <typename T> inline RdrId MakeId(T const& unique_name)
		{
			::std::hash<T> hasher;
			return hasher(unique_name);
		}

		// Return the immediate device context for a device
		inline D3DPtr<ID3D11DeviceContext> ImmediateDC(D3DPtr<ID3D11Device>& device)
		{
			D3DPtr<ID3D11DeviceContext> dc;
			device->GetImmediateContext(&dc.m_ptr);
			return dc;
		}
		
		// Compile time type to dxgi_format conversion
		template <typename Idx> struct DxFormat { static const DXGI_FORMAT value = DXGI_FORMAT_UNKNOWN; };
		template <> struct DxFormat<pr::uint16> { static const DXGI_FORMAT value = DXGI_FORMAT_R16_UINT; };
		template <> struct DxFormat<pr::uint32> { static const DXGI_FORMAT value = DXGI_FORMAT_R32_UINT; };
		template <> struct DxFormat<pr::v2>     { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32_FLOAT; };
		template <> struct DxFormat<pr::v3>     { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32B32_FLOAT; };
		template <> struct DxFormat<pr::v4>     { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32B32A32_FLOAT; };
		template <> struct DxFormat<pr::Colour> { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32B32A32_FLOAT; };
		
		// Returns the number of primitives implied by an index count and geometry topology
		size_t PrimCount(size_t icount, D3D11_PRIMITIVE_TOPOLOGY topo);
		
		// Returns the number of indices implied by a primitive count and geometry topology
		size_t IndexCount(size_t pcount, D3D11_PRIMITIVE_TOPOLOGY topo);

		// Returns the number of bits per pixel for a given d3d format
		UINT BitsPerPixel(DXGI_FORMAT fmt);
		
		// Return information about a surface determined from its dimensions and format
		// Any of the pointer parameters can be null
		void GetSurfaceInfo(UINT width, UINT height, DXGI_FORMAT fmt, UINT* num_bytes, UINT* row_bytes, UINT* num_rows);
	}
}

#endif
