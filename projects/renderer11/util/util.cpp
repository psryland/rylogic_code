//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/util/util.h"
#include "pr/renderer11/util/wrappers.h"
#include "pr/renderer11/util/event_types.h"
#include "pr/renderer11/models/nugget.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/depth_state.h"
#include "pr/renderer11/render/raster_state.h"
#include "renderer11/directxtex/directxtex.h"

namespace pr
{
	namespace rdr
	{
		// Check enumerations agree with dx11
		static_assert(EPrim::Invalid   == D3D11_PRIMITIVE_TOPOLOGY_UNDEFINED    , "EPrim::Invalid   value out of sync with dx11");
		static_assert(EPrim::PointList == D3D11_PRIMITIVE_TOPOLOGY_POINTLIST    , "EPrim::PointList value out of sync with dx11");
		static_assert(EPrim::LineList  == D3D11_PRIMITIVE_TOPOLOGY_LINELIST     , "EPrim::LineList  value out of sync with dx11");
		static_assert(EPrim::LineStrip == D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP    , "EPrim::LineStrip value out of sync with dx11");
		static_assert(EPrim::TriList   == D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST , "EPrim::TriList   value out of sync with dx11");
		static_assert(EPrim::TriStrip  == D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP, "EPrim::TriStrip  value out of sync with dx11");
		
		// The number of supported quality levels for the given format and sample count
		UINT MultisampleQualityLevels(D3DPtr<ID3D11Device>& device, DXGI_FORMAT format, UINT sample_count)
		{
			UINT num_quality_levels;
			pr::Throw(device->CheckMultisampleQualityLevels(format, sample_count, &num_quality_levels));
			return num_quality_levels;
		}

		// Returns the number of primitives implied by an index count and geometry topology
		size_t PrimCount(size_t icount, EPrim topo)
		{
			switch (topo)
			{
			default: PR_ASSERT(PR_DBG_RDR, false, "Unknown primitive type"); return 0;
			case EPrim::PointList: return icount;
			case EPrim::LineList:  PR_ASSERT(PR_DBG_RDR, (icount%2) == 0, "Incomplete primitive implied by icount"); return icount / 2;
			case EPrim::LineStrip: PR_ASSERT(PR_DBG_RDR,  icount    >= 2, "Incomplete primitive implied by icount"); return icount - 1;
			case EPrim::TriList:   PR_ASSERT(PR_DBG_RDR, (icount%3) == 0, "Incomplete primitive implied by icount"); return icount / 3;
			case EPrim::TriStrip:  PR_ASSERT(PR_DBG_RDR,  icount    >= 3, "Incomplete primitive implied by icount"); return icount - 2;
			}
		}

		// Returns the number of indices implied by a primitive count and geometry topology
		size_t IndexCount(size_t pcount, EPrim topo)
		{
			if (pcount == 0) return 0;
			switch (topo)
			{
			default: PR_ASSERT(PR_DBG_RDR, false, "Unknown primitive type"); return 0;
			case EPrim::PointList: return pcount;
			case EPrim::LineList:  return pcount * 2;
			case EPrim::LineStrip: return pcount + 1;
			case EPrim::TriList:   return pcount * 3;
			case EPrim::TriStrip:  return pcount + 2;
			}
		}

		// Return the number of bits per pixel for a given dx format
		size_t BitsPerPixel(DXGI_FORMAT fmt)
		{
			size_t bbp = DirectX::BitsPerPixel(fmt);
			if (bbp == 0) throw pr::Exception<HRESULT>(E_FAIL, "Unknown format");
			return bbp;
		}

		// Returns the expected row pitch for a given image width and format
		pr::ISize Pitch(ISize size, DXGI_FORMAT fmt)
		{
			ISize pitch;
			DirectX::ComputePitch(fmt, size.x, size.y, pitch.x, pitch.y, DirectX::CP_FLAGS_NONE);
			return pitch;
		}
		pr::ISize Pitch(TextureDesc const& tdesc)
		{
			return Pitch(ISize::make(tdesc.Width, tdesc.Height), tdesc.Format);
		}

		// Returns the number of expected mip levels for a given width x height texture
		size_t MipCount(pr::ISize size)
		{
			size_t count, largest = std::max(size.x, size.y);
			for (count = 1; largest /= 2; ++count) {}
			return count;
		}

		// Returns the dimensions of a mip level 'levels' lower than the given size
		pr::ISize MipDimensions(pr::ISize size, size_t levels)
		{
			PR_ASSERT(PR_DBG_RDR, levels > 0, "A specific mip level must be given");
			PR_ASSERT(PR_DBG_RDR, levels <= MipCount(size), "The number of mip levels provided exceeds the expected number for this texture dimension");
			for (;levels-- != 0;)
			{
				size.x = std::max<size_t>(size.x/2, 1U);
				size.y = std::max<size_t>(size.y/2, 1U);
			}
			return size;
		}

		// Returns the number of pixels needed to contain the data for a mip chain with 'levels' levels
		// If 'levels' is 0, all mips down to 1x1 are assumed
		// Note, size.x should be the pitch rather than width of the texture
		size_t MipChainSize(pr::ISize size, size_t levels)
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

		// Return information about a surface determined from its dimensions as format
		void GetSurfaceInfo(UINT width, UINT height, DXGI_FORMAT fmt, UINT* num_bytes, UINT* row_bytes, UINT* num_rows)
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

		// Helper for setting alpha blending states
		void SetAlphaBlending(BSBlock& bsb, DSBlock& dsb, RSBlock& rsb, bool on, int render_target, D3D11_BLEND_OP blend_op, D3D11_BLEND src_blend, D3D11_BLEND dst_blend)
		{
			if (on)
			{
				// ZWrites need to be disabled too
				bsb.Set(EBS::BlendEnable    ,TRUE      ,render_target);
				bsb.Set(EBS::BlendOp        ,blend_op  ,render_target);
				bsb.Set(EBS::SrcBlend       ,src_blend ,render_target);
				bsb.Set(EBS::DestBlend      ,dst_blend ,render_target);
				dsb.Set(EDS::DepthWriteMask ,D3D11_DEPTH_WRITE_MASK_ZERO);
				rsb.Set(ERS::CullMode       ,D3D11_CULL_NONE);
			}
			else
			{
				bsb.Clear(EBS::BlendEnable ,render_target);
				bsb.Clear(EBS::BlendOp     ,render_target);
				bsb.Clear(EBS::SrcBlend    ,render_target);
				bsb.Clear(EBS::DestBlend   ,render_target);
				dsb.Clear(EDS::DepthWriteMask);
				rsb.Clear(ERS::CullMode);
			}
		}
		void SetAlphaBlending(NuggetProps& ddata, bool on, int render_target, D3D11_BLEND_OP blend_op, D3D11_BLEND src_blend, D3D11_BLEND dst_blend)
		{
			SetAlphaBlending(ddata.m_bsb, ddata.m_dsb, ddata.m_rsb, on, render_target, blend_op, src_blend, dst_blend);
		}

		// Performs a bunch of checks to ensure the system that the renderer is running supports the necessary features
		bool TestSystemCompatibility()
		{
			try
			{
				pr::events::Send(Evt_CompatibilityTest());
				return true;
			}
			catch (std::exception const&)
			{
				return false;
			}
		}
	}
}