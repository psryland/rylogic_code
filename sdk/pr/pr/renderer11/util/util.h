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
		// To<D3DCOLORVALUE>
		template <typename TFrom> struct Convert<D3DCOLORVALUE,TFrom>
		{
			static D3DCOLORVALUE To(pr::Colour const& c) { D3DCOLORVALUE cv = {c.r, c.g, c.b, c.a}; return cv; }
			static D3DCOLORVALUE To(pr::Colour32 c)      { return To((pr::Colour)c); }
		};

		// Make a RdrId from a pointer
		template <typename T> RdrId MakeId(T const* ptr)
		{
			return static_cast<RdrId>(ptr - (T const*)0);
		}

		// Make a RdrId from 'unique_thing'
		template <typename T> inline RdrId MakeId(T const& unique_thing)
		{
			return pr::hash::HashData(&unique_thing, sizeof(unique_thing));
		}

		// Make an RdrId from a string
		inline RdrId MakeId(wchar_t const* str)
		{
			return pr::hash::HashC(str);
		}
		inline RdrId MakeId(char const* str)
		{
			return pr::hash::HashC(str);
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

		// The number of supported quality levels for the given format and sample count
		UINT MultisampleQualityLevels(D3DPtr<ID3D11Device>& device, DXGI_FORMAT format, UINT sample_count);

		// Returns the number of primitives implied by an index count and geometry topology
		size_t PrimCount(size_t icount, EPrim topo);

		// Returns the number of indices implied by a primitive count and geometry topology
		size_t IndexCount(size_t pcount, EPrim topo);

		// Returns the number of bits per pixel for a given d3d format
		size_t BitsPerPixel(DXGI_FORMAT fmt);
		inline size_t BytesPerPixel(DXGI_FORMAT fmt) { return BitsPerPixel(fmt) >> 3; }

		// Returns the expected row and slice pitch for a given image width*height and format
		pr::ISize Pitch(pr::ISize size, DXGI_FORMAT fmt);

		// Returns the number of expected mip levels for a given width x height texture
		size_t MipCount(pr::ISize size);

		// Returns the dimensions of a mip level 'levels' below the given texture size
		pr::ISize MipDimensions(pr::ISize size, size_t levels);

		// Returns the number of pixels needed contain the data for a mip chain with 'levels' levels
		// If 'levels' is 0, all mips down to 1x1 are assumed
		// Note, size.x should be the pitch rather than width of the texture
		size_t MipChainSize(pr::ISize size, size_t levels);

		// Return information about a surface determined from its dimensions and format. Any of the pointer parameters can be null
		void GetSurfaceInfo(UINT width, UINT height, DXGI_FORMAT fmt, UINT* num_bytes, UINT* row_bytes, UINT* num_rows);

		// Helper for setting alpha blending states
		void SetAlphaBlending(BSBlock& bsb, DSBlock& dsb, RSBlock& rsb, bool on, int render_target = 0, D3D11_BLEND_OP blend_op = D3D11_BLEND_OP_ADD, D3D11_BLEND src_blend = D3D11_BLEND_SRC_ALPHA, D3D11_BLEND dst_blend = D3D11_BLEND_INV_SRC_ALPHA);
		void SetAlphaBlending(NuggetProps& ddata, bool on, int render_target = 0, D3D11_BLEND_OP blend_op = D3D11_BLEND_OP_ADD, D3D11_BLEND src_blend = D3D11_BLEND_SRC_ALPHA, D3D11_BLEND dst_blend = D3D11_BLEND_INV_SRC_ALPHA);

		// Helper for checking values are not overwritten in a lookup table
		template <class Table, typename Key, typename Value> inline void AddLookup(Table& table, Key key, Value value)
		{
			PR_ASSERT(PR_DBG_RDR, table.count(key) == 0, "Overwriting an existing lookup table item");
			table[key] = value;
		}

		// Lock and write 'cb' into 'cbuf'. The set 'cbuf' as the constants for the shaders
		template <typename TCBuf> void WriteConstants(D3DPtr<ID3D11DeviceContext>& dc, D3DPtr<ID3D11Buffer>& cbuf, TCBuf const& cb, EShaderType shdr_type = EShaderType::VS|EShaderType::PS)
		{
			{// Copy the buffer to the dx buffer
				LockT<TCBuf> lock(dc, cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0);
				*lock.ptr() = cb;
			}

			// Bind the constants to the shaders
			if (shdr_type & EShaderType::VS) dc->VSSetConstantBuffers(TCBuf::Slot, 1, &cbuf.m_ptr);
			if (shdr_type & EShaderType::PS) dc->PSSetConstantBuffers(TCBuf::Slot, 1, &cbuf.m_ptr);
			if (shdr_type & EShaderType::GS) dc->GSSetConstantBuffers(TCBuf::Slot, 1, &cbuf.m_ptr);
			if (shdr_type & EShaderType::HS) dc->HSSetConstantBuffers(TCBuf::Slot, 1, &cbuf.m_ptr);
			if (shdr_type & EShaderType::DS) dc->DSSetConstantBuffers(TCBuf::Slot, 1, &cbuf.m_ptr);
		}

		// Set the name on a d3d resource (debug only)
		template <typename T> inline void NameResource(D3DPtr<T>& res, char const* name)
		{
			#if PR_DBG_RDR
			char existing[256]; size_t size = sizeof(existing) - 1;
			if (res->GetPrivateData(WKPDID_D3DDebugObjectName, &size, existing) != DXGI_ERROR_NOT_FOUND)
			{
				existing[size] = 0;
				PR_ASSERT(PR_DBG_RDR, false, pr::FmtS("Resource is already named: %s", existing));
			}

			string32 res_name = name;
			pr::Throw(res->SetPrivateData(WKPDID_D3DDebugObjectName, UINT(res_name.size()), res_name.c_str()));
			#endif
		}

		// Performs a bunch of checks to ensure the system that the renderer is running supports the necessary features
		bool TestSystemCompatibility();
	}
}

#endif
