//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/util/util.h"
#include "pr/view3d/util/wrappers.h"
#include "pr/view3d/util/event_args.h"
#include "pr/view3d/models/nugget.h"
#include "pr/view3d/render/state_block.h"
#include "view3d/directxtex/directxtex.h"

namespace pr::rdr
{
	// Check enumerations agree with dx11
	static_assert(int(EPrim::Invalid  ) == int(D3D11_PRIMITIVE_TOPOLOGY_UNDEFINED    ), "EPrim::Invalid   value out of sync with dx11");
	static_assert(int(EPrim::PointList) == int(D3D11_PRIMITIVE_TOPOLOGY_POINTLIST    ), "EPrim::PointList value out of sync with dx11");
	static_assert(int(EPrim::LineList ) == int(D3D11_PRIMITIVE_TOPOLOGY_LINELIST     ), "EPrim::LineList  value out of sync with dx11");
	static_assert(int(EPrim::LineStrip) == int(D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP    ), "EPrim::LineStrip value out of sync with dx11");
	static_assert(int(EPrim::TriList  ) == int(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST ), "EPrim::TriList   value out of sync with dx11");
	static_assert(int(EPrim::TriStrip ) == int(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP), "EPrim::TriStrip  value out of sync with dx11");

	// Helper for getting the ref count of a COM pointer.
	ULONG RefCount(IUnknown* ptr)
	{
		// Don't inline this function so that it can be used in the Immediate window during debugging
		if (ptr == nullptr) return 0;
		auto count = ptr->AddRef();
		ptr->Release();
		return count - 1;
	}

	// The number of supported quality levels for the given format and sample count
	UINT MultisampleQualityLevels(ID3D11Device* device, DXGI_FORMAT format, UINT sample_count)
	{
		UINT num_quality_levels;
		pr::Throw(device->CheckMultisampleQualityLevels(format, sample_count, &num_quality_levels));
		return num_quality_levels;
	}

	// Returns the number of primitives implied by an index count and geometry topology
	size_t PrimCount(size_t icount, EPrim topo)
	{
		// https://docs.microsoft.com/en-us/windows/win32/direct3d11/d3d10-graphics-programming-guide-primitive-topologies
		switch (topo)
		{
		default: PR_ASSERT(PR_DBG_RDR, false, "Unknown primitive type"); return 0;
		case EPrim::PointList: return icount;
		case EPrim::LineList:    PR_ASSERT(PR_DBG_RDR, (icount%2) == 0, "Incomplete primitive implied by i-count"); return icount / 2;
		case EPrim::LineStrip:   PR_ASSERT(PR_DBG_RDR,  icount    >= 2, "Incomplete primitive implied by i-count"); return icount - 1;
		case EPrim::TriList:     PR_ASSERT(PR_DBG_RDR, (icount%3) == 0, "Incomplete primitive implied by i-count"); return icount / 3;
		case EPrim::TriStrip:    PR_ASSERT(PR_DBG_RDR,  icount    >= 3, "Incomplete primitive implied by i-count"); return icount - 2;
		case EPrim::LineListAdj: PR_ASSERT(PR_DBG_RDR, (icount%4) == 0, "Incomplete primitive implied by i-count"); return icount / 4;
		case EPrim::LineStripAdj:PR_ASSERT(PR_DBG_RDR,  icount    >= 4, "Incomplete primitive implied by i-count"); return (icount - 2) - 1;
		case EPrim::TriListAdj:  PR_ASSERT(PR_DBG_RDR, (icount%6) == 0, "Incomplete primitive implied by i-count"); return icount / 6;
		case EPrim::TriStripAdj: PR_ASSERT(PR_DBG_RDR,  icount    >= 3, "Incomplete primitive implied by i-count"); return (icount - 4) / 2;
		}
	}

	// Returns the number of indices implied by a primitive count and geometry topology
	size_t IndexCount(size_t pcount, EPrim topo)
	{
		if (pcount == 0) return 0;
		switch (topo)
		{
		default: PR_ASSERT(PR_DBG_RDR, false, "Unknown primitive type"); return 0;
		case EPrim::PointList:   return pcount;
		case EPrim::LineList:    return pcount * 2;
		case EPrim::LineStrip:   return pcount + 1;
		case EPrim::TriList:     return pcount * 3;
		case EPrim::TriStrip:    return pcount + 2;
		case EPrim::LineListAdj: return pcount * 4;
		case EPrim::LineStripAdj:return (pcount + 1) + 2;
		case EPrim::TriListAdj:  return pcount * 6;
		case EPrim::TriStripAdj: return (pcount * 2) + 4;
		}
	}

	// True if 'fmt' is a compression image format
	bool IsCompressed(DXGI_FORMAT fmt)
	{
		switch (fmt)
		{
		case DXGI_FORMAT_BC1_TYPELESS:
		case DXGI_FORMAT_BC1_UNORM:
		case DXGI_FORMAT_BC1_UNORM_SRGB:
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
			return true;
		default:
			return false;
		}
	}

	// Returns the expected row pitch for a given image width and format
	iv2 Pitch(iv2 size, DXGI_FORMAT fmt)
	{
		// x = row pitch = number of bytes per row
		// y = slice pitch = number of bytes per 2D image.
		auto width = size.x;
		auto height = size.y;

		auto bc = false;
		auto packed = false;
		auto num_bytes_per_block = 0;
		switch (fmt)
		{
		case DXGI_FORMAT_BC1_TYPELESS:
		case DXGI_FORMAT_BC1_UNORM:
		case DXGI_FORMAT_BC1_UNORM_SRGB:
		case DXGI_FORMAT_BC4_TYPELESS:
		case DXGI_FORMAT_BC4_UNORM:
		case DXGI_FORMAT_BC4_SNORM:
			{
				bc = true;
				num_bytes_per_block = 8;
				break;
			}
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
			{
				bc = true;
				num_bytes_per_block = 16;
				break;
			}
		case DXGI_FORMAT_R8G8_B8G8_UNORM:
		case DXGI_FORMAT_G8R8_G8B8_UNORM:
			{
				packed = true;
				break;
			}
		}

		auto num_rows = 0;
		auto row_bytes = 0;
		if (bc)
		{
			auto blocks_wide = width > 0 ? std::max<int>(1, (width + 3) / 4) : 0;
			auto blocks_high = height > 0 ? std::max<int>(1, (height + 3) / 4) : 0;
			row_bytes = blocks_wide * num_bytes_per_block;
			num_rows = blocks_high;
		}
		else if (packed)
		{
			row_bytes = ((width + 1) >> 1) * 4;
			num_rows = height;
		}
		else
		{
			auto bpp = BitsPerPixel(fmt);
			row_bytes = (width * bpp + 7) / 8; // round up to nearest byte
			num_rows = height;
		}
		return iv2(row_bytes, row_bytes * num_rows);
	}
	iv2 Pitch(Texture2DDesc const& tdesc)
	{
		return Pitch(iv2(s_cast<int>(tdesc.Width), s_cast<int>(tdesc.Height)), tdesc.Format);
	}

	// Returns the number of expected mip levels for a given width x height texture
	size_t MipCount(size_t w, size_t h)
	{
		size_t count, largest = std::max(w, h);
		for (count = 1; largest >>= 1; ++count) {}
		return count;
	}
	size_t MipCount(iv2 size)
	{
		return MipCount(size_t(size.x), size_t(size.y));
	}

	// Returns the dimensions of a mip level 'levels' lower than the given size
	iv2 MipDimensions(iv2 size, size_t levels)
	{
		PR_ASSERT(PR_DBG_RDR, levels > 0, "A specific mip level must be given");
		PR_ASSERT(PR_DBG_RDR, levels <= MipCount(size), "The number of mip levels provided exceeds the expected number for this texture dimension");
		for (;levels-- != 0;)
		{
			size.x = std::max(size.x/2, 1);
			size.y = std::max(size.y/2, 1);
		}
		return size;
	}

	// Returns the number of pixels needed to contain the data for a mip chain with 'levels' levels
	// If 'levels' is 0, all mips down to 1x1 are assumed
	// Note, size.x should be the pitch rather than width of the texture
	size_t MipChainSize(iv2 size, size_t levels)
	{
		PR_ASSERT(PR_DBG_RDR, levels <= MipCount(size), "Number of mip levels provided exceeds the expected number for this texture dimension");

		if (levels == 0)
			levels = MipCount(size);

		size_t pixel_count = 0;
		for (;levels-- != 0;)
		{
			pixel_count += size.x * size.y;
			size = MipDimensions(size, 1);
		}
		return pixel_count;
	}

	// GetSurfaceInfo() => Pitch
}