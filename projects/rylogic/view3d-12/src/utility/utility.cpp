//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/utility/utility.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/view3d-12/utility/map_resource.h"

namespace pr::rdr12
{
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
	UINT MultisampleQualityLevels(ID3D12Device* device, DXGI_FORMAT format, UINT sample_count)
	{
		D3D12_FEATURE_DATA_MULTISAMPLE_QUALITY_LEVELS opts = {format, sample_count, D3D12_MULTISAMPLE_QUALITY_LEVELS_FLAG_NONE};
		auto hr = device->CheckFeatureSupport(D3D12_FEATURE_MULTISAMPLE_QUALITY_LEVELS, &opts, sizeof(opts));
		if (hr == E_INVALIDARG)
			return 0;

		Throw(hr);
		return opts.NumQualityLevels;
	}

	// Returns the number of primitives implied by an index count and geometry topology
	size_t PrimCount(size_t icount, ETopo topo)
	{
		// https://docs.microsoft.com/en-us/windows/win32/direct3d11/d3d10-graphics-programming-guide-primitive-topologies
		switch (topo)
		{
			case ETopo::PointList: return icount;
			case ETopo::LineList:    PR_ASSERT(PR_DBG_RDR, (icount % 2) == 0, "Incomplete primitive implied by i-count"); return icount / 2;
			case ETopo::LineStrip:   PR_ASSERT(PR_DBG_RDR, icount >= 2, "Incomplete primitive implied by i-count"); return icount - 1;
			case ETopo::TriList:     PR_ASSERT(PR_DBG_RDR, (icount % 3) == 0, "Incomplete primitive implied by i-count"); return icount / 3;
			case ETopo::TriStrip:    PR_ASSERT(PR_DBG_RDR, icount >= 3, "Incomplete primitive implied by i-count"); return icount - 2;
			case ETopo::LineListAdj: PR_ASSERT(PR_DBG_RDR, (icount % 4) == 0, "Incomplete primitive implied by i-count"); return icount / 4;
			case ETopo::LineStripAdj:PR_ASSERT(PR_DBG_RDR, icount >= 4, "Incomplete primitive implied by i-count"); return (icount - 2) - 1;
			case ETopo::TriListAdj:  PR_ASSERT(PR_DBG_RDR, (icount % 6) == 0, "Incomplete primitive implied by i-count"); return icount / 6;
			case ETopo::TriStripAdj: PR_ASSERT(PR_DBG_RDR, icount >= 3, "Incomplete primitive implied by i-count"); return (icount - 4) / 2;
			default: throw std::runtime_error("Unknown primitive type");
		}
	}

	// Returns the number of indices implied by a primitive count and geometry topology
	size_t IndexCount(size_t pcount, ETopo topo)
	{
		if (pcount == 0) return 0;
		switch (topo)
		{
			case ETopo::PointList:   return pcount;
			case ETopo::LineList:    return pcount * 2;
			case ETopo::LineStrip:   return pcount + 1;
			case ETopo::TriList:     return pcount * 3;
			case ETopo::TriStrip:    return pcount + 2;
			case ETopo::LineListAdj: return pcount * 4;
			case ETopo::LineStripAdj:return (pcount + 1) + 2;
			case ETopo::TriListAdj:  return pcount * 6;
			case ETopo::TriStripAdj: return (pcount * 2) + 4;
			default: throw std::runtime_error("Unknown primitive type");
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
	iv3 Pitch(iv3 size, DXGI_FORMAT fmt)
	{
		// x = row pitch = number of bytes per row
		// y = slice pitch = number of bytes per 2D image.
		// z = block pitch = number of bytes per 3D image.
		auto width = size.x;
		auto height = size.y;
		auto depth = size.z;

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
		return iv3(row_bytes, row_bytes * num_rows, row_bytes * num_rows * depth);
	}
	iv2 Pitch(iv2 size, DXGI_FORMAT fmt)
	{
		return Pitch(iv3(size.x, size.y, 1), fmt).xy;
	}
	iv2 Pitch(D3D12_RESOURCE_DESC const& desc)
	{
		return Pitch(iv2(s_cast<int>(desc.Width), s_cast<int>(desc.Height)), desc.Format);
	}

	// Returns the number of expected mip levels for a given width x height texture
	int MipCount(int w, int h)
	{
		int count, largest = std::max(w, h);
		for (count = 1; largest >>= 1; ++count) {}
		return count;
	}
	int MipCount(iv2 size)
	{
		return MipCount(size.x, size.y);
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

	template <typename T>
	concept HasPrivateData = requires(T v, Guid const& guid, UINT* mdata_size, UINT cdata_size, void* mdata, void const* cdata)
	{
		{ v.GetPrivateData(guid, mdata_size, mdata) } -> std::same_as<HRESULT>;
		{ v.SetPrivateData(guid, cdata_size, cdata) } -> std::same_as<HRESULT>;
	};

	/// <summary>Set the name on a DX resource (debug only)</summary>
	template <typename T>
	void NameResource(T* res, char const* name) requires HasPrivateData<T>
	{
		#if PR_DBG_RDR

		char existing[256];
		UINT size(sizeof(existing) - 1);
		if (res->GetPrivateData(WKPDID_D3DDebugObjectName, &size, existing) != DXGI_ERROR_NOT_FOUND)
		{
			existing[size] = 0;
			if (!str::Equal(existing, name))
				OutputDebugStringA(FmtS("Resource is already named '%s'. New name '%s' ignored", existing, name));
			return;
		}

		std::string_view res_name(name);
		Throw(res->SetPrivateData(WKPDID_D3DDebugObjectName, s_cast<UINT>(res_name.size()), res_name.data()));
		#else
		(void)res,name;
		#endif
	}
	void NameResource(ID3D12Object* res, char const* name)
	{
		NameResource<ID3D12Object>(res, name);
	}
	void NameResource(IDXGIObject* res, char const* name)
	{
		NameResource<IDXGIObject>(res, name);
	}

	// Parse an embedded resource string of the form: "@<hmodule|module_name>:<res_type>:<res_name>"
	void ParseEmbeddedResourceUri(std::wstring const& uri, HMODULE& hmodule, wstring32& res_type, wstring32& res_name)
	{
		if (uri.empty() || uri[0] != '@')
			throw std::runtime_error("Not an embedded resource URI");

		hmodule = nullptr;
		res_type.resize(0);
		res_name.resize(0);

		auto div0 = uri.c_str();
		auto div1 = *div0 != 0 ? str::FindChar(div0 + 1, ':') : div0;
		auto div2 = *div1 != 0 ? str::FindChar(div1 + 1, ':') : div1;
		if (*div2 == 0)
			throw std::runtime_error(FmtS("Embedded resource URI (%S) invalid. Expected format \"@<hmodule|module_name>:<res_type>:<res_name>\"", uri.c_str()));
		
		// Read the HMODULE handle from a string name or 
		auto HModule = [=](wchar_t const* s, wchar_t const* e)
		{
			wstring32 name(s, e);
			if (name.empty())
				return HMODULE();

			if (auto h = GetModuleHandleW(name.c_str()); h != nullptr)
				return h;

			auto end = (wchar_t*)nullptr;
			auto address = std::wcstoll(s, &end, 16);
			if (auto h = reinterpret_cast<HMODULE>((uint8_t*)nullptr + (end == e ? address : 0)); h != nullptr)
				return h;

			throw std::runtime_error(FmtS("Embedded resource URI (%S) not found. HMODULE could not be determined", uri.c_str()));
		};

		res_name.append(div2 + 1);
		res_type.append(div1 + 1, div2);
		hmodule = HModule(div0 + 1, div1);

		// Both name and type are required
		if (res_name.empty() || res_type.empty())
			throw std::runtime_error(FmtS("Embedded resource URI (%S) not found. Resource name and type could not be determined", uri.c_str()));
	}

	// Return an ordered list of filepaths based on 'pattern'
	vector<std::filesystem::path> PatternToPaths(std::filesystem::path const& dir, char8_t const* pattern)
	{
		using namespace std::filesystem;

		vector<path> paths;

		// Assume the pattern is in the filename only
		auto pat = std::regex(char_ptr(pattern), std::regex::flag_type::icase);
		for (auto& entry : directory_iterator(dir))
		{
			if (!std::regex_match(entry.path().filename().string(), pat)) continue;
			paths.push_back(entry.path());
		}

		// Sort the paths lexically
		pr::sort(paths);
		return std::move(paths);
	}






	#if 0 // in reosurce manager now
	// Copy a resource by rows
	void MemcpySubresource(D3D12_MEMCPY_DEST const& dest, D3D12_SUBRESOURCE_DATA const& src, size_t RowSizeInBytes, int NumRows, int NumSlices)
	{
		for (auto z = 0; z != NumSlices; ++z)
		{
			auto dest_slice = byte_ptr(dest.pData) + dest.SlicePitch * z;
			auto src_slice = byte_ptr(src.pData) + src.SlicePitch * z;

			for (auto y = 0; y != NumRows; ++y)
				memcpy(dest_slice + dest.RowPitch * y, src_slice + src.RowPitch * y, RowSizeInBytes);
		}
	}

	// Copy data to an upload resource, then add commands to copy it to a GPU resource.
	void UpdateSubresource(ID3D12GraphicsCommandList* cmds, ID3D12Resource* destination, ID3D12Resource* staging, D3D12_SUBRESOURCE_DATA image, int sub0)
	{
		UpdateSubresource(cmds, destination, staging, &image, sub0, 1);
	}
	void UpdateSubresource(ID3D12GraphicsCommandList* cmds, ID3D12Resource* destination, ID3D12Resource* staging, D3D12_SUBRESOURCE_DATA const* images, int sub0, int subN)
	{
		if (subN == 0)
			return;

		// Check buffer types
		auto sdesc = staging->GetDesc();
		auto ddesc = destination->GetDesc();
		if (sdesc.Dimension != D3D12_RESOURCE_DIMENSION_BUFFER)
			throw std::runtime_error("Staging resource must be a buffer");
		if (ddesc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER && (sub0 != 0 || subN != 1))
			throw std::runtime_error("Destination resource is a buffer, but sub-resource range is given");

		// Get the device associated with the command list
		D3DPtr<ID3D12Device> device;
		Throw(cmds->GetDevice(__uuidof(ID3D12Device), (void**)(&device.m_ptr)));

		// Get the sizes for copying
		UINT64 total_size;
		auto strides     = PR_ALLOCA(strides, UINT64, subN);
		auto row_counts  = PR_ALLOCA(row_counts, UINT, subN);
		auto footprints  = PR_ALLOCA(footprints, D3D12_PLACED_SUBRESOURCE_FOOTPRINT, subN);
		device->GetCopyableFootprints(&ddesc, sub0, subN, 0ULL, &footprints[0], &row_counts[0], &strides[0], &total_size);

		// Copy the 'data' into the staging buffer
		{
			MapResource lock(staging, 0, 1);
			for (auto i = 0; i != subN; ++i)
			{
				D3D12_MEMCPY_DEST dst =
				{
					lock.data() + footprints[i].Offset,
					footprints[i].Footprint.RowPitch,
					s_cast<size_t>(footprints[i].Footprint.RowPitch) * row_counts[i],
				};
				MemcpySubresource(dst, images[i], strides[i], row_counts[i], footprints[i].Footprint.Depth);
			}
		}

		// Add the command to copy the staging resource to the destination
		if (ddesc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
		{
			cmds->CopyBufferRegion(destination, 0, staging, footprints[0].Offset, footprints[0].Footprint.Width);
		}
		else
		{
			for (auto i = 0; i != subN; ++i)
			{
				D3D12_TEXTURE_COPY_LOCATION dst =
				{
					.pResource = destination,
					.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
					.SubresourceIndex = s_cast<UINT>(i + sub0),
				};
				D3D12_TEXTURE_COPY_LOCATION src =
				{
					.pResource = staging,
					.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
					.PlacedFootprint = footprints[i],
				};
				cmds->CopyTextureRegion(&dst, 0, 0, 0, &src, nullptr);
			}
		}
	}
	#endif
}
