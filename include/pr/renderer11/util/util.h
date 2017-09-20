//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/lock.h"

namespace pr
{
	namespace rdr
	{
		// Returns an incrementing id with each call
		inline RdrId MonotonicId()
		{
			static RdrId s_id = 0;
			return ++s_id;
		}

		// Make a RdrId from a pointer
		// Careful, don't make a templated MakeId(T const& obj) function. It will be selected
		// in preference to this function.
		inline RdrId MakeId(void const* ptr)
		{
			return static_cast<RdrId>(byte_ptr(ptr) - byte_ptr(nullptr));
		}

		// Make an RdrId from a string
		inline RdrId MakeId(wchar_t const* str)
		{
			return pr::hash::Hash(str);
		}
		inline RdrId MakeId(char const* str)
		{
			return pr::hash::Hash(str);
		}

		// Compile time type to 'DXGI_FORMAT' conversion
		template <typename Idx> struct DxFormat { static const DXGI_FORMAT value = DXGI_FORMAT_UNKNOWN; };
		template <> struct DxFormat<pr::uint16> { static const DXGI_FORMAT value = DXGI_FORMAT_R16_UINT; };
		template <> struct DxFormat<pr::uint32> { static const DXGI_FORMAT value = DXGI_FORMAT_R32_UINT; };
		template <> struct DxFormat<pr::v2>     { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32_FLOAT; };
		template <> struct DxFormat<pr::v3>     { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32B32_FLOAT; };
		template <> struct DxFormat<pr::v4>     { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32B32A32_FLOAT; };
		template <> struct DxFormat<pr::Colour> { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32B32A32_FLOAT; };

		// Shader type to enum map
		template <typename D3DShaderType> struct ShaderTypeId { static const EShaderType::Enum_ value = EShaderType::Invalid; };
		template <> struct ShaderTypeId<ID3D11VertexShader  > { static const EShaderType::Enum_ value = EShaderType::VS; };
		template <> struct ShaderTypeId<ID3D11PixelShader   > { static const EShaderType::Enum_ value = EShaderType::PS; };
		template <> struct ShaderTypeId<ID3D11GeometryShader> { static const EShaderType::Enum_ value = EShaderType::GS; };
		template <> struct ShaderTypeId<ID3D11HullShader    > { static const EShaderType::Enum_ value = EShaderType::HS; };
		template <> struct ShaderTypeId<ID3D11DomainShader  > { static const EShaderType::Enum_ value = EShaderType::DS; };

		// Shader enum to dx type map
		template <EShaderType::Enum_ ST> struct DxShaderType {};
		template <> struct DxShaderType<EShaderType::VS> { typedef ID3D11VertexShader   type; };
		template <> struct DxShaderType<EShaderType::PS> { typedef ID3D11PixelShader    type; };
		template <> struct DxShaderType<EShaderType::GS> { typedef ID3D11GeometryShader type; };
		template <> struct DxShaderType<EShaderType::HS> { typedef ID3D11HullShader     type; };
		template <> struct DxShaderType<EShaderType::DS> { typedef ID3D11DomainShader   type; };
		
		// The number of supported quality levels for the given format and sample count
		UINT MultisampleQualityLevels(ID3D11Device* device, DXGI_FORMAT format, UINT sample_count);

		// Returns the number of primitives implied by an index count and geometry topology
		size_t PrimCount(size_t icount, EPrim topo);

		// Returns the number of indices implied by a primitive count and geometry topology
		size_t IndexCount(size_t pcount, EPrim topo);

		// Returns the number of bits per pixel for a given dx format
		size_t BitsPerPixel(DXGI_FORMAT fmt);
		inline size_t BytesPerPixel(DXGI_FORMAT fmt) { return BitsPerPixel(fmt) >> 3; }

		// Returns the expected row and slice pitch for a given image width*height and format
		iv2 Pitch(iv2 size, DXGI_FORMAT fmt);
		iv2 Pitch(TextureDesc const& tdesc);

		// Returns the number of expected mip levels for a given width x height texture
		size_t MipCount(iv2 size);

		// Returns the dimensions of a mip level 'levels' below the given texture size
		iv2 MipDimensions(iv2 size, size_t levels);

		// Returns the number of pixels needed contain the data for a mip chain with 'levels' levels
		// If 'levels' is 0, all mips down to 1x1 are assumed
		// Note, size.x should be the pitch rather than width of the texture
		size_t MipChainSize(iv2 size, size_t levels);

		// Return information about a surface determined from its dimensions and format. Any of the pointer parameters can be null
		void GetSurfaceInfo(UINT width, UINT height, DXGI_FORMAT fmt, UINT* num_bytes, UINT* row_bytes, UINT* num_rows);

		// Helper for checking values are not overwritten in a lookup table
		template <class Table, typename Key, typename Value> inline void AddLookup(Table& table, Key const& key, Value const& value)
		{
			PR_ASSERT(PR_DBG_RDR, table.count(key) == 0, "Overwriting an existing lookup table item");
			table[key] = value;
		}

		// Set the name on a dx resource (debug only)
		template <typename T> inline void NameResource(T* res, char const* name)
		{
			#if PR_DBG_RDR
			char existing[256]; UINT size(sizeof(existing) - 1);
			if (res->GetPrivateData(WKPDID_D3DDebugObjectName, &size, existing) != DXGI_ERROR_NOT_FOUND)
			{
				existing[size] = 0;
				PR_ASSERT(PR_DBG_RDR, false, FmtS("Resource is already named: %s", existing));
			}

			string32 res_name = name;
			Throw(res->SetPrivateData(WKPDID_D3DDebugObjectName, UINT(res_name.size()), res_name.c_str()));
			#endif
		}

		// Performs a bunch of checks to ensure the system that the renderer is running supports the necessary features
		bool TestSystemCompatibility();
	}
}
