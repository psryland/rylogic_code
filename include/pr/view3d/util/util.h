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

	// Compile time type to 'DXGI_FORMAT' conversion
	template <typename Type> struct DxFormat { static const DXGI_FORMAT value = DXGI_FORMAT_UNKNOWN;            static const int size = sizeof(char    ); };
	template <> struct DxFormat<uint16_t>    { static const DXGI_FORMAT value = DXGI_FORMAT_R16_UINT;           static const int size = sizeof(uint16  ); };
	template <> struct DxFormat<uint32_t>    { static const DXGI_FORMAT value = DXGI_FORMAT_R32_UINT;           static const int size = sizeof(uint32  ); };
	template <> struct DxFormat<v2      >    { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32_FLOAT;       static const int size = sizeof(v2      ); };
	template <> struct DxFormat<v3      >    { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32B32_FLOAT;    static const int size = sizeof(v3      ); };
	template <> struct DxFormat<v4      >    { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32B32A32_FLOAT; static const int size = sizeof(v4      ); };
	template <> struct DxFormat<Colour  >    { static const DXGI_FORMAT value = DXGI_FORMAT_R32G32B32A32_FLOAT; static const int size = sizeof(Colour  ); };
	template <> struct DxFormat<Colour32>    { static const DXGI_FORMAT value = DXGI_FORMAT_R8G8B8A8_UNORM;     static const int size = sizeof(Colour32); };

	// Compile 'DXGI_FORMAT' to pixel type conversion
	template <DXGI_FORMAT Fmt> struct type_for                 { using type = void;     };
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

	// Returns the number of bits per pixel for a given dx format
	int BitsPerPixel(DXGI_FORMAT fmt);
	inline int BytesPerPixel(DXGI_FORMAT fmt)
	{
		return BitsPerPixel(fmt) >> 3;
	}

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
