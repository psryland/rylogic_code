//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/util/lock.h"

namespace pr::rdr
{
	// Helper for getting the ref count of a COM pointer.
	ULONG RefCount(IUnknown* ptr);
	template <typename T> ULONG RefCount(RefPtr<T> ptr)
	{
		if (ptr == nullptr) return 0;
		return RegCount(ptr.m_ptr);
	}

	// Helper for allocating and constructing a type using 'alloc_traits'
	template <typename T, typename... Args>
	[[nodiscard]] inline T* New(Args&&... args)
	{
		Allocator<T> alex;
		auto ptr = alloc_traits<T>::allocate(alex, sizeof(T));
		alloc_traits<T>::construct(alex, ptr, std::forward<Args>(args)...);
		return ptr;
	}
	template <typename T>
	inline void Delete(T* ptr)
	{
		Allocator<T> alex;
		alloc_traits<T>::destroy(alex, ptr);
		alloc_traits<T>::deallocate(alex, ptr, 1);
	}

	// Returns an incrementing id with each call
	inline RdrId MonotonicId()
	{
		static RdrId s_id = 0;
		return ++s_id;
	}

	// Make a RdrId from a pointer.
	inline RdrId MakeId(void const* ptr)
	{
		// Careful, don't make a templated MakeId(T const& obj) function. It will be selected in preference to this function.
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

	// Create a 4-byte CC code
	constexpr uint32_t MakeFourCC(uint8_t ch0, uint8_t ch1, uint8_t ch2, uint8_t ch3)
	{
		return static_cast<uint32_t>(ch0) | (static_cast<uint32_t>(ch1) << 8) | (static_cast<uint32_t>(ch2) << 16) | (static_cast<uint32_t>(ch3) << 24);
	}

	// Return the number of bits per pixel for a given dx format
	constexpr int BitsPerPixel(DXGI_FORMAT fmt)
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
		case DXGI_FORMAT_B4G4R4A4_UNORM:
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
		case DXGI_FORMAT_BC4_TYPELESS:
		case DXGI_FORMAT_BC4_UNORM:
		case DXGI_FORMAT_BC4_SNORM:
			return 4;

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
			return 8;

		default:
			throw std::runtime_error(FmtS("Unsupported DXGI format: %d", fmt));
		}
	}
	constexpr int BytesPerPixel(DXGI_FORMAT fmt)
	{
		return BitsPerPixel(fmt) >> 3;
	}

	// Compile time type to 'DXGI_FORMAT' conversion
	template <typename Type> struct dx_format { static const DXGI_FORMAT value = DXGI_FORMAT_UNKNOWN;            static const int size = sizeof(char    ); };
	template <> struct dx_format<uint8_t >    { static const DXGI_FORMAT value = DXGI_FORMAT_R8_UINT;            static const int size = sizeof(uint8_t ); };
	template <> struct dx_format<uint16_t>    { static const DXGI_FORMAT value = DXGI_FORMAT_R16_UINT;           static const int size = sizeof(uint16_t); };
	template <> struct dx_format<uint32_t>    { static const DXGI_FORMAT value = DXGI_FORMAT_R32_UINT;           static const int size = sizeof(uint32_t); };
	template <> struct dx_format<v2      >    { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32_FLOAT;       static const int size = sizeof(v2      ); };
	template <> struct dx_format<v3      >    { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32B32_FLOAT;    static const int size = sizeof(v3      ); };
	template <> struct dx_format<v4      >    { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32B32A32_FLOAT; static const int size = sizeof(v4      ); };
	template <> struct dx_format<Colour  >    { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32B32A32_FLOAT; static const int size = sizeof(Colour  ); };
	template <> struct dx_format<Colour32>    { static const DXGI_FORMAT value = DXGI_FORMAT_R8G8B8A8_UNORM;     static const int size = sizeof(Colour32); };
	template <typename Type> static constexpr DXGI_FORMAT dx_format_v = dx_format<Type>::value;
	static_assert(dx_format_v<uint32_t> == DXGI_FORMAT_R32_UINT);

	// Compile 'DXGI_FORMAT' to pixel type conversion
	template <DXGI_FORMAT Fmt> struct type_for                 { using type = void;     };
	template <> struct type_for<DXGI_FORMAT_R8_UINT           >{ using type = uint8_t;  };
	template <> struct type_for<DXGI_FORMAT_R16_UINT          >{ using type = uint16_t; };
	template <> struct type_for<DXGI_FORMAT_R32_UINT          >{ using type = uint32_t; };
	template <> struct type_for<DXGI_FORMAT_R32G32_FLOAT      >{ using type = v2;       };
	template <> struct type_for<DXGI_FORMAT_R32G32B32_FLOAT   >{ using type = v3;       };
	template <> struct type_for<DXGI_FORMAT_R32G32B32A32_FLOAT>{ using type = Colour;   };
	template <> struct type_for<DXGI_FORMAT_R8G8B8A8_UNORM    >{ using type = Colour32; };
	template <DXGI_FORMAT Fmt> using type_for_t = typename type_for<Fmt>::type;
	static_assert(std::is_same_v<type_for_t<DXGI_FORMAT_R32G32B32A32_FLOAT>, Colour>) ;

	// Shader type to enum map
	template <typename D3DShaderType> struct ShaderTypeId { static const EShaderType value = EShaderType::Invalid; };
	template <> struct ShaderTypeId<ID3D11VertexShader  > { static const EShaderType value = EShaderType::VS; };
	template <> struct ShaderTypeId<ID3D11PixelShader   > { static const EShaderType value = EShaderType::PS; };
	template <> struct ShaderTypeId<ID3D11GeometryShader> { static const EShaderType value = EShaderType::GS; };
	template <> struct ShaderTypeId<ID3D11ComputeShader > { static const EShaderType value = EShaderType::CS; };
	template <> struct ShaderTypeId<ID3D11HullShader    > { static const EShaderType value = EShaderType::HS; };
	template <> struct ShaderTypeId<ID3D11DomainShader  > { static const EShaderType value = EShaderType::DS; };

	// Shader enum to dx type map
	template <EShaderType ST> struct DxShaderType {};
	template <> struct DxShaderType<EShaderType::VS> { typedef ID3D11VertexShader   type; };
	template <> struct DxShaderType<EShaderType::PS> { typedef ID3D11PixelShader    type; };
	template <> struct DxShaderType<EShaderType::GS> { typedef ID3D11GeometryShader type; };
	template <> struct DxShaderType<EShaderType::CS> { typedef ID3D11ComputeShader  type; };
	template <> struct DxShaderType<EShaderType::HS> { typedef ID3D11HullShader     type; };
	template <> struct DxShaderType<EShaderType::DS> { typedef ID3D11DomainShader   type; };

	// The number of supported quality levels for the given format and sample count
	UINT MultisampleQualityLevels(ID3D11Device* device, DXGI_FORMAT format, UINT sample_count);

	// Returns the number of primitives implied by an index count and geometry topology
	size_t PrimCount(size_t icount, EPrim topo);

	// Returns the number of indices implied by a primitive count and geometry topology
	size_t IndexCount(size_t pcount, EPrim topo);

	// Returns the expected row and slice pitch for a given image width*height and format
	iv2 Pitch(iv2 size, DXGI_FORMAT fmt);
	iv2 Pitch(Texture2DDesc const& tdesc);

	// Returns the number of expected mip levels for a given width x height texture
	size_t MipCount(size_t w, size_t h);
	size_t MipCount(iv2 size);

	// Returns the dimensions of a mip level 'levels' below the given texture size
	iv2 MipDimensions(iv2 size, size_t levels);

	// Returns the number of pixels needed contain the data for a mip chain with 'levels' levels.
	// If 'levels' is 0, all mips down to 1x1 are assumed. Note, size.x should be the pitch rather than width of the texture
	size_t MipChainSize(iv2 size, size_t levels);

	// Helper for checking values are not overwritten in a lookup table
	template <class Table, typename Key, typename Value>
	inline void AddLookup(Table& table, Key const& key, Value const& value)
	{
		PR_ASSERT(PR_DBG_RDR, table.count(key) == 0, "Overwriting an existing lookup table item");
		table[key] = value;
	}

	// Helper for reading values from an unordered map, returning 'def' if not found
	template <class Map, typename Key, typename Value>
	inline Value const& GetOrDefault(Map const& map, Key const& key, Value const& def = Value())
	{
		auto iter = map.find(key);
		return iter == end(map) ? def : static_cast<Value const&>(iter->second);
	}

	// Set the name on a DX resource (debug only)
	template <typename T> inline void NameResource(T* res, char const* name)
	{
		#if PR_DBG_RDR
		char existing[256]; UINT size(sizeof(existing) - 1);
		if (res->GetPrivateData(WKPDID_D3DDebugObjectName, &size, existing) != DXGI_ERROR_NOT_FOUND)
		{
			existing[size] = 0;
			if (!str::Equal(existing, name))
				OutputDebugStringA(FmtS("Resource is already named '%s'. New name '%s' ignored", existing, name));
			return;
		}

		string32 res_name = name;
		Throw(res->SetPrivateData(WKPDID_D3DDebugObjectName, UINT(res_name.size()), res_name.c_str()));
		#endif
	}
}
