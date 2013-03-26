//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/util/util.h"
#include "pr/renderer11/util/wrappers.h"

using namespace pr::rdr;

// Returns the number of primitives implied by an index count and geometry topology
size_t pr::rdr::PrimCount(size_t icount, D3D11_PRIMITIVE_TOPOLOGY topo)
{
	switch (topo)
	{
	default: PR_ASSERT(PR_DBG_RDR, false, "Unknown primitive type"); return 0;
	case D3D11_PRIMITIVE_TOPOLOGY_POINTLIST:     return icount;
	case D3D11_PRIMITIVE_TOPOLOGY_LINELIST:      PR_ASSERT(PR_DBG_RDR, (icount%2) == 0, "Incomplete primitive implied by icount"); return icount / 2;
	case D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP:     PR_ASSERT(PR_DBG_RDR,  icount    >= 2, "Incomplete primitive implied by icount"); return icount - 1;
	case D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST:  PR_ASSERT(PR_DBG_RDR, (icount%3) == 0, "Incomplete primitive implied by icount"); return icount / 3;
	case D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP: PR_ASSERT(PR_DBG_RDR,  icount    >= 3, "Incomplete primitive implied by icount"); return icount - 2;
	}
}

// Returns the number of indices implied by a primitive count and geometry topology
size_t pr::rdr::IndexCount(size_t pcount, D3D11_PRIMITIVE_TOPOLOGY topo)
{
	if (pcount == 0) return 0;
	switch (topo)
	{
	default: PR_ASSERT(PR_DBG_RDR, false, "Unknown primitive type"); return 0;
	case D3D11_PRIMITIVE_TOPOLOGY_POINTLIST:     return pcount;
	case D3D11_PRIMITIVE_TOPOLOGY_LINELIST:      return pcount * 2;
	case D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP:     return pcount + 1;
	case D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST:  return pcount * 3;
	case D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP: return pcount + 2;
	}
}

// Return the number of bits per pixel for a given d3d format
size_t pr::rdr::BitsPerPixel(DXGI_FORMAT fmt)
{
	switch (fmt)
	{
	case DXGI_FORMAT_R32G32B32A32_TYPELESS:
	case DXGI_FORMAT_R32G32B32A32_FLOAT:
	case DXGI_FORMAT_R32G32B32A32_UINT:
	case DXGI_FORMAT_R32G32B32A32_SINT:
		return 128;
	case DXGI_FORMAT_R32G32B32_TYPELESS:
	case DXGI_FORMAT_R32G32B32_FLOAT:
	case DXGI_FORMAT_R32G32B32_UINT:
	case DXGI_FORMAT_R32G32B32_SINT:
		return 96;
	case DXGI_FORMAT_R16G16B16A16_TYPELESS:
	case DXGI_FORMAT_R16G16B16A16_FLOAT:
	case DXGI_FORMAT_R16G16B16A16_UNORM:
	case DXGI_FORMAT_R16G16B16A16_UINT:
	case DXGI_FORMAT_R16G16B16A16_SNORM:
	case DXGI_FORMAT_R16G16B16A16_SINT:
	case DXGI_FORMAT_R32G32_TYPELESS:
	case DXGI_FORMAT_R32G32_FLOAT:
	case DXGI_FORMAT_R32G32_UINT:
	case DXGI_FORMAT_R32G32_SINT:
	case DXGI_FORMAT_R32G8X24_TYPELESS:
	case DXGI_FORMAT_D32_FLOAT_S8X24_UINT:
	case DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS:
	case DXGI_FORMAT_X32_TYPELESS_G8X24_UINT:
		return 64;
	case DXGI_FORMAT_R10G10B10A2_TYPELESS:
	case DXGI_FORMAT_R10G10B10A2_UNORM:
	case DXGI_FORMAT_R10G10B10A2_UINT:
	case DXGI_FORMAT_R11G11B10_FLOAT:
	case DXGI_FORMAT_R8G8B8A8_TYPELESS:
	case DXGI_FORMAT_R8G8B8A8_UNORM:
	case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
	case DXGI_FORMAT_R8G8B8A8_UINT:
	case DXGI_FORMAT_R8G8B8A8_SNORM:
	case DXGI_FORMAT_R8G8B8A8_SINT:
	case DXGI_FORMAT_R16G16_TYPELESS:
	case DXGI_FORMAT_R16G16_FLOAT:
	case DXGI_FORMAT_R16G16_UNORM:
	case DXGI_FORMAT_R16G16_UINT:
	case DXGI_FORMAT_R16G16_SNORM:
	case DXGI_FORMAT_R16G16_SINT:
	case DXGI_FORMAT_R32_TYPELESS:
	case DXGI_FORMAT_D32_FLOAT:
	case DXGI_FORMAT_R32_FLOAT:
	case DXGI_FORMAT_R32_UINT:
	case DXGI_FORMAT_R32_SINT:
	case DXGI_FORMAT_R24G8_TYPELESS:
	case DXGI_FORMAT_D24_UNORM_S8_UINT:
	case DXGI_FORMAT_R24_UNORM_X8_TYPELESS:
	case DXGI_FORMAT_X24_TYPELESS_G8_UINT:
	case DXGI_FORMAT_R9G9B9E5_SHAREDEXP:
	case DXGI_FORMAT_R8G8_B8G8_UNORM:
	case DXGI_FORMAT_G8R8_G8B8_UNORM:
	case DXGI_FORMAT_B8G8R8A8_UNORM:
	case DXGI_FORMAT_B8G8R8X8_UNORM:
	case DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM:
	case DXGI_FORMAT_B8G8R8A8_TYPELESS:
	case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
	case DXGI_FORMAT_B8G8R8X8_TYPELESS:
	case DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
		return 32;
	case DXGI_FORMAT_R8G8_TYPELESS:
	case DXGI_FORMAT_R8G8_UNORM:
	case DXGI_FORMAT_R8G8_UINT:
	case DXGI_FORMAT_R8G8_SNORM:
	case DXGI_FORMAT_R8G8_SINT:
	case DXGI_FORMAT_R16_TYPELESS:
	case DXGI_FORMAT_R16_FLOAT:
	case DXGI_FORMAT_D16_UNORM:
	case DXGI_FORMAT_R16_UNORM:
	case DXGI_FORMAT_R16_UINT:
	case DXGI_FORMAT_R16_SNORM:
	case DXGI_FORMAT_R16_SINT:
	case DXGI_FORMAT_B5G6R5_UNORM:
	case DXGI_FORMAT_B5G5R5A1_UNORM:
		return 16;
	case DXGI_FORMAT_R8_TYPELESS:
	case DXGI_FORMAT_R8_UNORM:
	case DXGI_FORMAT_R8_UINT:
	case DXGI_FORMAT_R8_SNORM:
	case DXGI_FORMAT_R8_SINT:
	case DXGI_FORMAT_A8_UNORM:
		return 8;
	case DXGI_FORMAT_R1_UNORM:
		return 1;
	case DXGI_FORMAT_BC1_TYPELESS:
	case DXGI_FORMAT_BC1_UNORM:
	case DXGI_FORMAT_BC1_UNORM_SRGB:
		return 4;
	case DXGI_FORMAT_BC2_TYPELESS:
	case DXGI_FORMAT_BC2_UNORM:
	case DXGI_FORMAT_BC2_UNORM_SRGB:
	case DXGI_FORMAT_BC3_TYPELESS:
	case DXGI_FORMAT_BC3_UNORM:
	case DXGI_FORMAT_BC3_UNORM_SRGB:
	case DXGI_FORMAT_BC4_TYPELESS:
	case DXGI_FORMAT_BC4_UNORM:
	case DXGI_FORMAT_BC4_SNORM:
	case DXGI_FORMAT_BC5_TYPELESS:
	case DXGI_FORMAT_BC5_UNORM:
	case DXGI_FORMAT_BC5_SNORM:
	case DXGI_FORMAT_BC6H_TYPELESS:
	case DXGI_FORMAT_BC6H_UF16:
	case DXGI_FORMAT_BC6H_SF16:
	case DXGI_FORMAT_BC7_TYPELESS:
	case DXGI_FORMAT_BC7_UNORM:
	case DXGI_FORMAT_BC7_UNORM_SRGB:
		return 8;
	default:
		throw pr::Exception<HRESULT>(E_FAIL, "Unknown format");
	}
}

// Return information about a surface determined from its dimensions as format
void pr::rdr::GetSurfaceInfo(UINT width, UINT height, DXGI_FORMAT fmt, UINT* num_bytes, UINT* row_bytes, UINT* num_rows)
{
	UINT num_bytes_ = 0; if (num_bytes == 0) num_bytes = &num_bytes_;
	UINT row_bytes_ = 0; if (row_bytes == 0) row_bytes = &row_bytes_;
	UINT num_rows_  = 0; if (num_rows  == 0) num_rows  = &num_rows_;
	
	int bc_num_bytes_per_block;
	switch (fmt)
	{
	default:
		bc_num_bytes_per_block = 0;
		break;
	case DXGI_FORMAT_BC1_TYPELESS:
	case DXGI_FORMAT_BC1_UNORM:
	case DXGI_FORMAT_BC1_UNORM_SRGB:
	case DXGI_FORMAT_BC4_TYPELESS:
	case DXGI_FORMAT_BC4_UNORM:
	case DXGI_FORMAT_BC4_SNORM:
		bc_num_bytes_per_block = 8;
		break;
	case DXGI_FORMAT_BC2_TYPELESS:
	case DXGI_FORMAT_BC2_UNORM:
	case DXGI_FORMAT_BC2_UNORM_SRGB:
	case DXGI_FORMAT_BC3_TYPELESS:
	case DXGI_FORMAT_BC3_UNORM:
	case DXGI_FORMAT_BC3_UNORM_SRGB:
	case DXGI_FORMAT_BC5_TYPELESS:
	case DXGI_FORMAT_BC5_UNORM:
	case DXGI_FORMAT_BC5_SNORM:
	case DXGI_FORMAT_BC6H_TYPELESS:
	case DXGI_FORMAT_BC6H_UF16:
	case DXGI_FORMAT_BC6H_SF16:
	case DXGI_FORMAT_BC7_TYPELESS:
	case DXGI_FORMAT_BC7_UNORM:
	case DXGI_FORMAT_BC7_UNORM_SRGB:
		bc_num_bytes_per_block = 16;
		break;
	}
	
	if (bc_num_bytes_per_block)
	{
		int num_blocks_wide = width  > 0 ? std::max(1U, width  / 4) : 0;
		int num_blocks_high = height > 0 ? std::max(1U, height / 4) : 0;
		*row_bytes = num_blocks_wide * bc_num_bytes_per_block;
		*num_rows  = num_blocks_high;
	}
	else
	{
		UINT bpp = UINT(BitsPerPixel(fmt));
		*row_bytes = (width * bpp + 7) / 8;   // round up to nearest byte
		*num_rows  = height;
	}
	*num_bytes = *row_bytes * *num_rows;
}

