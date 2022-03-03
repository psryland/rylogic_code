//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
// Function for loading a DDS texture and creating a Direct3D 11 runtime resource for it
//
// Note these functions are useful as a light-weight runtime loader for DDS files. For
// a full-featured DDS file reader, writer, and texture processing pipeline see
// the 'Texconv' sample and the 'DirectXTex' library.
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// http://go.microsoft.com/fwlink/?LinkId=248926
// http://go.microsoft.com/fwlink/?LinkId=248929

#include "pr/view3d/forward.h"
#include "pr/view3d/textures/texture_loader.h"
#include "pr/view3d/util/wrappers.h"
#include "pr/view3d/util/util.h"
#include <memory>
#include <filesystem>
#include <algorithm>
#include <assert.h>
#include <stdint.h>
#include <dxgiformat.h>
#include <d3d9types.h>

namespace pr::rdr
{
	// DDS file structure definitions
	// See DDS.h in the 'Texconv' sample and the 'DirectXTex' library
	namespace dds
	{
		// "DDS "
		static uint32_t const Sentinal = 0x20534444;

		enum class EHeaderFlags
		{
			PIXELFORMAT = 0x00000001, // DDSD_PIXELFORMAT 
			HEIGHT      = 0x00000002, // DDSD_HEIGHT
			WIDTH       = 0x00000004, // DDSD_WIDTH
			PITCH       = 0x00000008, // DDSD_PITCH
			CAPS        = 0x00001000, // DDSD_CAPS 
			TEXTURE     = 0x00001007, // DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT 
			MIPMAP      = 0x00020000, // DDSD_MIPMAPCOUNT
			LINEARSIZE  = 0x00080000, // DDSD_LINEARSIZE
			VOLUME      = 0x00800000, // DDSD_DEPTH
			_bitwise_operators_allowed,
		};
		enum class EPixelFormatFlags
		{
			ALPHAPIXELS = 0x00000001, // DDPF_ALPHAPIXELS
			ALPHA       = 0x00000002, // DDPF_ALPHA
			FOURCC      = 0x00000004, // DDPF_FOURCC
			PAL8        = 0x00000020, // DDPF_PALETTEINDEXED8
			RGB         = 0x00000040, // DDPF_RGB
			RGBA        = 0x00000041, // DDPF_RGB | DDPF_ALPHAPIXELS
			LUMINANCE   = 0x00020000, // DDPF_LUMINANCE
			LUMINANCEA  = 0x00020001, // DDPF_LUMINANCE | DDPF_ALPHAPIXELS
			_bitwise_operators_allowed,
		};
		enum class ECaps
		{
			CUBEMAP = 0x00000008, // DDSCAPS_COMPLEX
			TEXTURE = 0x00001000, // DDSCAPS_TEXTURE
			MIPMAP  = 0x00400008, // DDSCAPS_COMPLEX | DDSCAPS_MIPMAP
			_bitwise_operators_allowed,
		};
		enum class ECaps2
		{
			CUBEMAP = 0x00000200, // DDSCAPS2_CUBEMAP
			CUBEMAP_POSITIVEX = 0x00000600, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEX
			CUBEMAP_NEGATIVEX = 0x00000a00, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEX
			CUBEMAP_POSITIVEY = 0x00001200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEY
			CUBEMAP_NEGATIVEY = 0x00002200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEY
			CUBEMAP_POSITIVEZ = 0x00004200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEZ
			CUBEMAP_NEGATIVEZ = 0x00008200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEZ
			CUBEMAP_ALLFACES = CUBEMAP_POSITIVEX | CUBEMAP_NEGATIVEX | CUBEMAP_POSITIVEY | CUBEMAP_NEGATIVEY | CUBEMAP_POSITIVEZ | CUBEMAP_NEGATIVEZ,
			VOLUME = 0x00200000, // DDSCAPS2_VOLUME
			_bitwise_operators_allowed,
		};

		struct PixelFormat
		{
			uint32_t    size;
			uint32_t    flags;
			uint32_t    fourCC;
			uint32_t    RGBBitCount;
			uint32_t    RBitMask;
			uint32_t    GBitMask;
			uint32_t    BBitMask;
			uint32_t    ABitMask;
		};
		struct Header
		{
			uint32_t    size;
			uint32_t    flags;
			uint32_t    height;
			uint32_t    width;
			uint32_t    pitchOrLinearSize;
			uint32_t    depth; // only if DDS_HEADER_FLAGS_VOLUME is set in flags
			uint32_t    mipMapCount;
			uint32_t    reserved1[11];
			PixelFormat ddspf;
			uint32_t    caps;
			uint32_t    caps2;
			uint32_t    caps3;
			uint32_t    caps4;
			uint32_t    reserved2;
		};
		struct HeaderDXT10
		{
			DXGI_FORMAT dxgiFormat;
			uint32_t    resourceDimension;
			uint32_t    miscFlag; // D3D11_RESOURCE_MISC_FLAG
			uint32_t    array_size;
			uint32_t    reserved;
		};
	}

	// True if 'data' points at DDS data (probably)
	bool IsDDSData(ImageBytes img)
	{
		return img.size >= 4 && *static_cast<uint32_t const*>(img.data) == dds::Sentinal;
	}

	// Convert a DDS pixel format to a DXGI format
	DXGI_FORMAT GetDXGIFormat(dds::PixelFormat const& ddpf)
	{
		auto IsBitmask = [&ddpf](uint32_t r, uint32_t g, uint32_t b, uint32_t a)
		{
			return ddpf.RBitMask == r && ddpf.GBitMask == g && ddpf.BBitMask == b && ddpf.ABitMask == a;
		};

		if (AllSet(ddpf.flags, dds::EPixelFormatFlags::RGB))
		{
			// Note that sRGB formats are written using the "DX10" extended header
			switch (ddpf.RGBBitCount)
			{
			case 32:
				{
					// No DXGI format maps to IsBitmask(0x000000ff,0x0000ff00,0x00ff0000,0x00000000) aka D3DFMT_X8B8G8R8

					// Note that many common DDS reader/writers (including D3DX) swap the
					// the RED/BLUE masks for 10:10:10:2 formats. We assumme
					// below that the 'backwards' header mask is being used since it is most
					// likely written by D3DX. The more robust solution is to use the 'DX10'
					// header extension and specify the DXGI_FORMAT_R10G10B10A2_UNORM format directly

					if (IsBitmask(0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000))
						return DXGI_FORMAT_R8G8B8A8_UNORM;

					if (IsBitmask(0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000))
						return DXGI_FORMAT_B8G8R8A8_UNORM;

					if (IsBitmask(0x00ff0000, 0x0000ff00, 0x000000ff, 0x00000000))
						return DXGI_FORMAT_B8G8R8X8_UNORM;

					// For 'correct' writers, this should be 0x000003ff,0x000ffc00,0x3ff00000 for RGB data
					if (IsBitmask(0x3ff00000, 0x000ffc00, 0x000003ff, 0xc0000000))
						return DXGI_FORMAT_R10G10B10A2_UNORM;

					// No DXGI format maps to IsBitmask(0x000003ff,0x000ffc00,0x3ff00000,0xc0000000) aka D3DFMT_A2R10G10B10
					if (IsBitmask(0x0000ffff, 0xffff0000, 0x00000000, 0x00000000))
						return DXGI_FORMAT_R16G16_UNORM;

					// Only 32-bit color channel format in D3D9 was R32F. D3DX writes this out as a FourCC of 114
					if (IsBitmask(0xffffffff, 0x00000000, 0x00000000, 0x00000000))
						return DXGI_FORMAT_R32_FLOAT;
					break;
				}
			case 24:
				{
					// No 24bpp DXGI formats aka D3DFMT_R8G8B8
					break;
				}
			case 16:
				{
					// No DXGI format maps to IsBitmask(0x7c00,0x03e0,0x001f,0x0000) aka D3DFMT_X1R5G5B5
					// No DXGI format maps to IsBitmask(0x0f00,0x00f0,0x000f,0x0000) aka D3DFMT_X4R4G4B4
					// No 3:3:2, 3:3:2:8, or paletted DXGI formats aka D3DFMT_A8R3G3B2, D3DFMT_R3G3B2, D3DFMT_P8, D3DFMT_A8P8, etc.

					if (IsBitmask(0x7c00, 0x03e0, 0x001f, 0x8000))
						return DXGI_FORMAT_B5G5R5A1_UNORM;

					if (IsBitmask(0xf800, 0x07e0, 0x001f, 0x0000))
						return DXGI_FORMAT_B5G6R5_UNORM;

					if (IsBitmask(0x0f00, 0x00f0, 0x000f, 0xf000))
						return DXGI_FORMAT_B4G4R4A4_UNORM;

					break;
				}
			}
		}
		else if (AllSet(ddpf.flags, dds::EPixelFormatFlags::LUMINANCE))
		{
			switch (ddpf.RGBBitCount)
			{
			case 8:
				{
					// No DXGI format maps to IsBitmask(0x0f,0x00,0x00,0xf0) aka D3DFMT_A4L4

					// D3DX10/11 writes this out as DX10 extension
					if (IsBitmask(0x000000ff, 0x00000000, 0x00000000, 0x00000000))
						return DXGI_FORMAT_R8_UNORM;

					break;
				}
			case 16:
				{
					// D3DX10/11 writes this out as DX10 extension
					if (IsBitmask(0x0000ffff, 0x00000000, 0x00000000, 0x00000000))
						return DXGI_FORMAT_R16_UNORM;

					// D3DX10/11 writes this out as DX10 extension
					if (IsBitmask(0x000000ff, 0x00000000, 0x00000000, 0x0000ff00))
						return DXGI_FORMAT_R8G8_UNORM;
					
					break;
				}
			}
		}
		else if (AllSet(ddpf.flags, dds::EPixelFormatFlags::ALPHA))
		{
			switch (ddpf.RGBBitCount)
			{
			case 8:
				{
					return DXGI_FORMAT_A8_UNORM;
				}
			}
		}
		else if (AllSet(ddpf.flags, dds::EPixelFormatFlags::FOURCC))
		{
			switch (ddpf.fourCC)
			{
			case MakeFourCC('D', 'X', 'T', '1'):
				return DXGI_FORMAT_BC1_UNORM;
			case MakeFourCC('D', 'X', 'T', '3'):
				return DXGI_FORMAT_BC2_UNORM;
			case MakeFourCC('D', 'X', 'T', '5'):
				return DXGI_FORMAT_BC3_UNORM;

			// While pre-mulitplied alpha isn't directly supported by the DXGI formats,
			// they are basically the same as these BC formats so they can be mapped
			case MakeFourCC('D', 'X', 'T', '2'):
				return DXGI_FORMAT_BC2_UNORM;
			case MakeFourCC('D', 'X', 'T', '4'):
				return DXGI_FORMAT_BC3_UNORM;
			case MakeFourCC('A', 'T', 'I', '1'):
				return DXGI_FORMAT_BC4_UNORM;
			case MakeFourCC('B', 'C', '4', 'U'):
				return DXGI_FORMAT_BC4_UNORM;
			case MakeFourCC('B', 'C', '4', 'S'):
				return DXGI_FORMAT_BC4_SNORM;
			case MakeFourCC('A', 'T', 'I', '2'):
				return DXGI_FORMAT_BC5_UNORM;
			case MakeFourCC('B', 'C', '5', 'U'):
				return DXGI_FORMAT_BC5_UNORM;
			case MakeFourCC('B', 'C', '5', 'S'):
				return DXGI_FORMAT_BC5_SNORM;

			// BC6H and BC7 are written using the "DX10" extended header
			case MakeFourCC('R', 'G', 'B', 'G'):
				return DXGI_FORMAT_R8G8_B8G8_UNORM;
			case MakeFourCC('G', 'R', 'G', 'B'):
				return DXGI_FORMAT_G8R8_G8B8_UNORM;

			case D3DFMT_A16B16G16R16:
				return DXGI_FORMAT_R16G16B16A16_UNORM;
			case D3DFMT_Q16W16V16U16:
				return DXGI_FORMAT_R16G16B16A16_SNORM;
			case D3DFMT_R16F:
				return DXGI_FORMAT_R16_FLOAT;
			case D3DFMT_G16R16F:
				return DXGI_FORMAT_R16G16_FLOAT;
			case D3DFMT_A16B16G16R16F:
				return DXGI_FORMAT_R16G16B16A16_FLOAT;
			case D3DFMT_R32F:
				return DXGI_FORMAT_R32_FLOAT;
			case D3DFMT_G32R32F:
				return DXGI_FORMAT_R32G32_FLOAT;
			case D3DFMT_A32B32G32R32F:
				return DXGI_FORMAT_R32G32B32A32_FLOAT;
			}
		}

		return DXGI_FORMAT_UNKNOWN;
	}

	// Load the DDS file data from 'filepath' into 'dds_data' and return pointers to the
	// header and data + size. Also, performs validation on the file can contained data.
	void LoadTextureDataFromFile(std::filesystem::path const& filepath, std::unique_ptr<uint8_t[]>& dds_data, dds::Header*& header, std::span<uint8_t const>& bits)
	{
		using namespace std::literals;

		// Sanity checks
		if (!std::filesystem::exists(filepath))
			throw std::runtime_error("File '"s + filepath.string() + "' does not exist");

		// Get the file size
		auto size = size_t(std::filesystem::file_size(filepath));

		// Need at least enough data to fill the header and magic number to be a valid DDS
		if (size < sizeof(dds::Header) + sizeof(uint32_t))
			throw std::runtime_error("File '"s + filepath.string() + "' is not a valid DDS file. Size is too small");

		// create enough space for the file data
		dds_data.reset(new uint8_t[size]);

		// Open the file (shared read)
		std::ifstream file(filepath, std::ios::binary, _SH_DENYWR);
		if (!file.good())
			throw std::runtime_error("Failed to open file: "s + filepath.string());

		// Read the data in
		if (!file.read(reinterpret_cast<char*>(dds_data.get()), size).good())
			throw std::runtime_error("Read error reading data from "s + filepath.string());

		// DDS files always start with the same magic number ("DDS ")
		auto magic_sentinal = *reinterpret_cast<uint32_t const*>(dds_data.get());
		if (magic_sentinal != dds::Sentinal)
			throw std::runtime_error("DDS file '"s + filepath.string() + "' is invalid. Sentinal not found");

		// Verify header to validate DDS file
		auto hdr = reinterpret_cast<dds::Header*>(dds_data.get() + sizeof(uint32_t));
		if (hdr->size != sizeof(dds::Header) || hdr->ddspf.size != sizeof(dds::PixelFormat))
			throw std::runtime_error("DDS file '"s + filepath.string() + "' is invalid. Header corrupt");

		// Check for DX10 extension
		auto is_DXT10 = false;
		if (AllSet(hdr->ddspf.flags, dds::EPixelFormatFlags::FOURCC) && MakeFourCC('D', 'X', '1', '0') == hdr->ddspf.fourCC)
		{
			// Must be long enough for both headers and magic value
			if (size < sizeof(dds::Header) + sizeof(uint32_t) + sizeof(dds::HeaderDXT10) )
				throw std::runtime_error("DDS file '"s + filepath.string() + "' is invalid. Header claims DX10 but file size is too small");

			is_DXT10 = true;
		}

		// Offset to the start of the file data
		auto offset = sizeof(uint32_t) + sizeof(dds::Header) + is_DXT10 ? sizeof(dds::HeaderDXT10) : 0;

		// Return the pointers
		header = hdr;
		bits = std::span<uint8_t const>{ dds_data.get() + offset, size - offset };
	}

	// Populates 'images' from the given 'bit_data' and image dimensions
	void FillInitData(size_t width, size_t height, size_t depth, size_t mip_count, size_t array_size, DXGI_FORMAT format, size_t max_dimension, std::span<uint8_t const> bits, size_t& twidth, size_t& theight, size_t& tdepth, vector<SubResourceData>& images)
	{
		twidth = 0;
		theight = 0;
		tdepth = 0;
		
		images.resize(0);
		images.reserve(mip_count * array_size);

		auto bits_beg = bits.begin();
		auto bits_end = bits.end();

		// Generate mips for each texture in the array
		for (int j = 0, jend = int(array_size); j != jend; ++j)
		{
			auto w = width;
			auto h = height;
			auto d = depth;

			// Generate each mip level
			for (int i = 0, iend = int(mip_count); i != iend; ++i)
			{
				// Get the image dimensions for the given width and height
				auto pitch = Pitch(iv2{static_cast<int>(w), static_cast<int>(h)}, format);
				if (bits_beg + (pitch.y * d) > bits_end)
					throw std::runtime_error("Insufficient image data provided");

				SubResourceData data;

				// Only generate the mip if it's mip level 1 or the dimensions are valid
				if (mip_count <= 1 || max_dimension == 0 || (w <= max_dimension && h <= max_dimension && d <= max_dimension))
				{
					// Record the texture dimensions of the highest level mip that is valid
					if (twidth == 0)
					{
						twidth = w;
						theight = h;
						tdepth = d;
					}

					data.pSysMem = bits_beg;
					data.SysMemPitch = static_cast<UINT>(pitch.x);
					data.SysMemSlicePitch = static_cast<UINT>(pitch.y);
				}

				// Add the mip initialisation data
				images.push_back(data);

				// Do the next mip
				w = std::max<size_t>(w >> 1, 1ULL);
				h = std::max<size_t>(h >> 1, 1ULL);
				d = std::max<size_t>(d >> 1, 1ULL);
				bits_beg += pitch.y * d;
			}
		}
	}

	// Create a DX texture resource
	void CreateD3DResources(ID3D11Device* d3d_device, D3D11_RESOURCE_DIMENSION resource_dimension, size_t width, size_t height, size_t depth, size_t mip_count, DXGI_FORMAT format, bool is_cube_map, vector<SubResourceData> const& images, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv)
	{
		auto array_size = images.size() / mip_count;

		switch (resource_dimension) 
		{
		case D3D11_RESOURCE_DIMENSION_TEXTURE1D:
			{
				tdesc.dim = D3D11_RESOURCE_DIMENSION_TEXTURE1D;
				tdesc.Tex1D.Format = format;
				tdesc.Tex1D.Width = static_cast<UINT>(width); 
				tdesc.Tex1D.MipLevels = static_cast<UINT>(mip_count);
				tdesc.Tex1D.ArraySize = static_cast<UINT>(array_size);
				tdesc.Tex1D.Usage = D3D11_USAGE_DEFAULT;
				tdesc.Tex1D.BindFlags = D3D11_BIND_SHADER_RESOURCE;
				tdesc.Tex1D.CPUAccessFlags = 0;
				tdesc.Tex1D.MiscFlags = 0;

				// Create the 1D texture
				ID3D11Texture1D* tex;
				Throw(d3d_device->CreateTexture1D(&tdesc.Tex1D, images.data(), &tex));
				res = D3DPtr<ID3D11Resource>(tex, false);

				// Create the SRV
				ID3D11ShaderResourceView* srv2;
				ShaderResourceViewDesc srv_desc(format);
				if (array_size > 1)
				{
					srv_desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE1DARRAY;
					srv_desc.Texture1DArray.MipLevels = tdesc.Tex1D.MipLevels;
					srv_desc.Texture1DArray.ArraySize = static_cast<UINT>(array_size);
				}
				else
				{
					srv_desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE1D;
					srv_desc.Texture1D.MipLevels = tdesc.Tex1D.MipLevels;
				}
				Throw(d3d_device->CreateShaderResourceView(res.get(), &srv_desc, &srv2));
				srv = D3DPtr<ID3D11ShaderResourceView>(srv2, false);
				break;
			}
		case D3D11_RESOURCE_DIMENSION_TEXTURE2D:
			{
				tdesc.dim = D3D11_RESOURCE_DIMENSION_TEXTURE2D;
				tdesc.Tex2D.Format = format;
				tdesc.Tex2D.Width = static_cast<UINT>(width);
				tdesc.Tex2D.Height = static_cast<UINT>(height);
				tdesc.Tex2D.MipLevels = static_cast<UINT>(mip_count);
				tdesc.Tex2D.ArraySize = static_cast<UINT>(array_size);
				tdesc.Tex2D.SampleDesc.Count = 1;
				tdesc.Tex2D.SampleDesc.Quality = 0;
				tdesc.Tex2D.Usage = D3D11_USAGE_DEFAULT;
				tdesc.Tex2D.BindFlags = D3D11_BIND_SHADER_RESOURCE;
				tdesc.Tex2D.CPUAccessFlags = 0;
				tdesc.Tex2D.MiscFlags = (is_cube_map) ? D3D11_RESOURCE_MISC_TEXTURECUBE : 0;

				// Create the 2D texture
				ID3D11Texture2D* tex;
				Throw(d3d_device->CreateTexture2D(&tdesc.Tex2D, images.data(), &tex));

				// Create the SRV
				ID3D11ShaderResourceView* srv2;
				ShaderResourceViewDesc srv_desc(format);
				if (is_cube_map && array_size > 6)
				{
					srv_desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURECUBEARRAY;
					srv_desc.TextureCubeArray.MipLevels = tdesc.Tex2D.MipLevels;
					srv_desc.TextureCubeArray.NumCubes = static_cast<UINT>(array_size / 6);
				}
				else if (is_cube_map)
				{
					srv_desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURECUBE;
					srv_desc.TextureCube.MipLevels = tdesc.Tex2D.MipLevels;
				}
				else if (array_size > 1)
				{
					srv_desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2DARRAY;
					srv_desc.Texture2DArray.MipLevels = tdesc.Tex2D.MipLevels;
					srv_desc.Texture2DArray.ArraySize = static_cast<UINT>(array_size);
				}
				else
				{
					srv_desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
					srv_desc.Texture2D.MipLevels = tdesc.Tex2D.MipLevels;
				}
				Throw(d3d_device->CreateShaderResourceView(res.get(), &srv_desc, &srv2));
				srv = D3DPtr<ID3D11ShaderResourceView>(srv2, false);
				break;
			}
		case D3D11_RESOURCE_DIMENSION_TEXTURE3D:
			{
				tdesc.dim = D3D11_RESOURCE_DIMENSION_TEXTURE3D;
				tdesc.Tex3D.Format = format;
				tdesc.Tex3D.Width = static_cast<UINT>(width);
				tdesc.Tex3D.Height = static_cast<UINT>(height);
				tdesc.Tex3D.Depth = static_cast<UINT>(depth);
				tdesc.Tex3D.MipLevels = static_cast<UINT>(mip_count);
				tdesc.Tex3D.Usage = D3D11_USAGE_DEFAULT;
				tdesc.Tex3D.BindFlags = D3D11_BIND_SHADER_RESOURCE;
				tdesc.Tex3D.CPUAccessFlags = 0;
				tdesc.Tex3D.MiscFlags = 0;

				// Create the 3D texture
				ID3D11Texture3D* tex;
				Throw(d3d_device->CreateTexture3D(&tdesc.Tex3D, images.data(), &tex));
				res = D3DPtr<ID3D11Resource>(tex, false);

				// Create the SRV
				ID3D11ShaderResourceView* srv2;
				ShaderResourceViewDesc srv_desc(format);
				srv_desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE3D;
				srv_desc.Texture3D.MipLevels = tdesc.Tex3D.MipLevels;
				Throw(d3d_device->CreateShaderResourceView(res.get(), &srv_desc, &srv2));
				srv = D3DPtr<ID3D11ShaderResourceView>(srv2, false);
				break; 
			}
		default:
			{
				throw std::runtime_error("Unknown resource dimension");
			}
		}
	}

	// Create a DX texture from DDS data
	void CreateTextureFromDDS(ID3D11Device* d3d_device, dds::Header const& header, std::span<uint8_t const> bits, int mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension)
	{
		auto resource_dimension = D3D11_RESOURCE_DIMENSION_UNKNOWN;
		auto format = DXGI_FORMAT_UNKNOWN;
		auto array_size = 1U;

		// Look for a DX10 header
		if (AllSet(header.ddspf.flags, dds::EPixelFormatFlags::FOURCC) && MakeFourCC('D', 'X', '1', '0') == header.ddspf.fourCC)
		{
			auto& d3d10ext = *reinterpret_cast<dds::HeaderDXT10 const *>(&header + 1);
			resource_dimension = static_cast<D3D11_RESOURCE_DIMENSION>(d3d10ext.resourceDimension);
			array_size = d3d10ext.array_size;
			format = d3d10ext.dxgiFormat;

			// Sanity checks
			if (array_size == 0)
				throw std::runtime_error("Corrupt DDS image. DXT10 Header claims array size of 0");
			if (BitsPerPixel(format) == 0)
				throw std::runtime_error(FmtS("DDS image format (%d) not supported", format));

			switch (resource_dimension)
			{
			case D3D11_RESOURCE_DIMENSION_TEXTURE1D:
				{
					if (AllSet(header.flags, dds::EHeaderFlags::HEIGHT) && header.height != 1)
						throw std::runtime_error(FmtS("Corrupt DDS image. 1D textures should have a height of 1. Height was %d", header.height));
					
					break;
				}
			case D3D11_RESOURCE_DIMENSION_TEXTURE2D:
				{
					if (AllSet(d3d10ext.miscFlag, D3D11_RESOURCE_MISC_TEXTURECUBE) != is_cube_map)
						throw std::runtime_error(FmtS("Image %s a cube map but %s expected to be", (is_cube_map?"is":"was not"), (is_cube_map?"is not":"was")));

					array_size *= 6;
					break;
				}
			case D3D11_RESOURCE_DIMENSION_TEXTURE3D:
				{
					if (!AllSet(header.flags, dds::EHeaderFlags::VOLUME))
						throw std::runtime_error("Unsupported DDS format. 3D volume textures not supported");

					if (array_size > 1)
						throw std::runtime_error("Unsupported DDS format. 3D texture arrays are not supported");
					
					break;
				}
			default:
				throw std::runtime_error(FmtS("DDS image with resource dimension %d not supported.", resource_dimension));
			}
		}
		else
		{
			format = GetDXGIFormat(header.ddspf);
			if (format == DXGI_FORMAT_UNKNOWN)
				throw std::runtime_error(FmtS("Unsupported DDS format. Pixel format %d cannot be converted to a DXGI format", header.ddspf));
			if (BitsPerPixel(format) == 0)
				throw std::runtime_error(FmtS("DDS image format (%d) not supported", format));

			// Determine texture type and perform sanity checks
			// Note: there's no way for a legacy Direct3D 9 DDS to express a '1D' texture
			if (AllSet(header.flags, dds::EHeaderFlags::VOLUME))
			{
				resource_dimension = D3D11_RESOURCE_DIMENSION_TEXTURE3D;
			}
			else
			{
				resource_dimension = D3D11_RESOURCE_DIMENSION_TEXTURE2D;

				if (AllSet(header.caps2, dds::ECaps2::CUBEMAP) != is_cube_map)
					throw std::runtime_error(FmtS("Image %s a cube map but %s expected to be", (is_cube_map?"is":"was not"), (is_cube_map?"is not":"was")));

				// We require all six faces to be defined
				if (is_cube_map && !AllSet(header.caps2, dds::ECaps2::CUBEMAP_ALLFACES))
					throw std::runtime_error("Unsupported DDS format. Cube-map texture does not include all 6 faces");

				if (is_cube_map)
					array_size = 6;
			}
		}

		// Bound sizes (for security purposes we don't trust DDS file metadata larger than the D3D 11.x hardware requirements)
		auto mip_count = std::max(std::min(uint32_t(mips), header.mipMapCount), 1U);
		if (mip_count > D3D11_REQ_MIP_LEVELS)
			throw std::runtime_error(FmtS("Unsupported DDS format. Texture contains (%d) mip levels which exceeds the DX11 limit (%d).", mip_count, D3D11_REQ_MIP_LEVELS));

		switch (resource_dimension)
		{
		case D3D11_RESOURCE_DIMENSION_TEXTURE1D:
			{
				if (array_size > D3D11_REQ_TEXTURE1D_ARRAY_AXIS_DIMENSION)
					throw std::runtime_error(FmtS("Unsupported DDS format. 1D texture array size (%d) exceeds array size limit (%d)", array_size, D3D11_REQ_TEXTURE1D_ARRAY_AXIS_DIMENSION));
				if (header.width > D3D11_REQ_TEXTURE1D_U_DIMENSION)
					throw std::runtime_error(FmtS("Unsupported DDS format. 1D texture size (%d) exceeds dimension limit (%d)", header.width, D3D11_REQ_TEXTURE1D_U_DIMENSION));
				break;
			}
		case D3D11_RESOURCE_DIMENSION_TEXTURE2D:
			{
				if (is_cube_map)
				{
					if (array_size > D3D11_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION)
						throw std::runtime_error(FmtS("Unsupported DDS format. Cube map texture array size (%d) exceeds array size limit (%d)", array_size, D3D11_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION));
					if (header.width > D3D11_REQ_TEXTURECUBE_DIMENSION || header.height > D3D11_REQ_TEXTURECUBE_DIMENSION)
						throw std::runtime_error(FmtS("Unsupported DDS format. Cube map texture dimensions (%dx%d) exceeds size limits (%dx%d)", header.width, header.height, D3D11_REQ_TEXTURECUBE_DIMENSION, D3D11_REQ_TEXTURECUBE_DIMENSION));
				}
				else if (array_size > D3D11_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION)
				{
					throw std::runtime_error(FmtS("Unsupported DDS format. 2D texture array size (%d) exceeds array size limit (%d)", array_size, D3D11_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION));
				}
				else if (header.width > D3D11_REQ_TEXTURE2D_U_OR_V_DIMENSION || header.height > D3D11_REQ_TEXTURE2D_U_OR_V_DIMENSION)
				{
					throw std::runtime_error(FmtS("Unsupported DDS format. 2D texture dimensions (%dx%d) exceeds size limits (%dx%d)", header.width, header.height, D3D11_REQ_TEXTURE2D_U_OR_V_DIMENSION, D3D11_REQ_TEXTURE2D_U_OR_V_DIMENSION));
				}
				break;
			}
		case D3D11_RESOURCE_DIMENSION_TEXTURE3D:
			{
				if (array_size > 1)
					throw std::runtime_error(FmtS("Unsupported DDS format. 3D texture array size (%d) exceeds array size limit (%d)", array_size, 1));
				if (header.width > D3D11_REQ_TEXTURE3D_U_V_OR_W_DIMENSION || header.height > D3D11_REQ_TEXTURE3D_U_V_OR_W_DIMENSION || header.depth > D3D11_REQ_TEXTURE3D_U_V_OR_W_DIMENSION)
					throw std::runtime_error(FmtS("Unsupported DDS format. 3D texture dimensions (%dx%dx%d) exceeds size limits (%dx%dx%d)", header.width, header.height, header.depth, D3D11_REQ_TEXTURE3D_U_V_OR_W_DIMENSION, D3D11_REQ_TEXTURE3D_U_V_OR_W_DIMENSION, D3D11_REQ_TEXTURE3D_U_V_OR_W_DIMENSION));
				break;
			}
		}

		// Create the texture resource
		// Start with the requested size, and reduce if unsupported on the current hardware.
		vector<SubResourceData> images;
		for (;;)
		{
			try
			{
				// Create the texture initialisation data
				size_t twidth, theight, tdepth;
				FillInitData(header.width, header.height, header.depth, mip_count, array_size, format, max_dimension, bits, twidth, theight, tdepth, images);
				CreateD3DResources(d3d_device, resource_dimension, twidth, theight, tdepth, mip_count, format, is_cube_map, images, tdesc, res, srv);
				break;
			}
			catch (std::exception const&)
			{
				if (max_dimension != 0 || mip_count <= 1)
					throw;

				// Retry with a max_dimension determined by feature level
				switch (d3d_device->GetFeatureLevel())
				{
				case D3D_FEATURE_LEVEL_9_1:
				case D3D_FEATURE_LEVEL_9_2:
					{
						max_dimension = 
							is_cube_map ? 512 : // D3D_FL9_1_REQ_TEXTURECUBE_DIMENSION
							resource_dimension == D3D11_RESOURCE_DIMENSION_TEXTURE3D ? D3D_FL9_1_REQ_TEXTURE3D_U_V_OR_W_DIMENSION :
							resource_dimension == D3D11_RESOURCE_DIMENSION_TEXTURE2D ? D3D_FL9_1_REQ_TEXTURE2D_U_OR_V_DIMENSION :
							resource_dimension == D3D11_RESOURCE_DIMENSION_TEXTURE1D ? D3D_FL9_1_REQ_TEXTURE1D_U_DIMENSION :
							throw;
						break;
					}
				case D3D_FEATURE_LEVEL_9_3:
					{
						max_dimension =
							resource_dimension == D3D11_RESOURCE_DIMENSION_TEXTURE3D ? D3D_FL9_1_REQ_TEXTURE3D_U_V_OR_W_DIMENSION :
							resource_dimension == D3D11_RESOURCE_DIMENSION_TEXTURE2D ? D3D_FL9_3_REQ_TEXTURE2D_U_OR_V_DIMENSION :
							resource_dimension == D3D11_RESOURCE_DIMENSION_TEXTURE1D ? D3D_FL9_3_REQ_TEXTURE1D_U_DIMENSION :
							throw;
						break;
					}
				case D3D_FEATURE_LEVEL_10_0:
				case D3D_FEATURE_LEVEL_10_1:
				default:
					{
						max_dimension =
							resource_dimension == D3D11_RESOURCE_DIMENSION_TEXTURE3D ? D3D10_REQ_TEXTURE3D_U_V_OR_W_DIMENSION :
							resource_dimension == D3D11_RESOURCE_DIMENSION_TEXTURE2D ? D3D10_REQ_TEXTURE2D_U_OR_V_DIMENSION :
							resource_dimension == D3D11_RESOURCE_DIMENSION_TEXTURE1D ? D3D10_REQ_TEXTURE1D_U_DIMENSION :
							throw;
						break;
					}
				}
			}
		}
	}

	// Create a DX texture from a DDS file in memory
	void CreateDDSTextureFromMemory(ID3D11Device* d3d_device, ImageBytes img, int mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension)
	{
		if (d3d_device == nullptr)
			throw std::runtime_error("D3D device pointer is null");
		if (img.data == nullptr || img.size == 0)
			throw std::runtime_error("Texture data must be provided");

		if (img.size < sizeof(dds::Header) + sizeof(uint32_t))
			throw std::runtime_error("Texture data is too small to be a DDS image");
		if (*static_cast<uint32_t const*>(img.data) != dds::Sentinal)
			throw std::runtime_error("Texture data is not DDS image data");

		auto& header = *reinterpret_cast<dds::Header const*>(img.bytes + sizeof(uint32_t));
		if (header.size != sizeof(dds::Header) || header.ddspf.size != sizeof(dds::PixelFormat))
			throw std::runtime_error("DDS data is corrupt. Header size is invalid");

		// Check for DX10 extension
		auto is_DXT10 = AllSet(header.ddspf.flags, dds::EPixelFormatFlags::FOURCC) && MakeFourCC('D', 'X', '1', '0') == header.ddspf.fourCC;
		if (is_DXT10 && img.size < sizeof(dds::Header) + sizeof(uint32_t) + sizeof(dds::HeaderDXT10))
			throw std::runtime_error("DDS data is corrupt. Header indicates DX10 but the data size is too small");

		auto offset = sizeof(dds::Header) + sizeof(uint32_t) + (is_DXT10 ? sizeof(dds::HeaderDXT10) : 0);
		auto bits = std::span{ img.bytes + offset, img.size - offset };

		CreateTextureFromDDS(d3d_device, header, bits, mips, is_cube_map, tdesc, res, srv, max_dimension);
	}

	// Create a DX texture from a DDS file
	void CreateDDSTextureFromFile(ID3D11Device* d3d_device, std::filesystem::path const& filepath, int mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension)
	{
		if (d3d_device == nullptr)
			throw std::runtime_error("D3D device pointer is null");
		if (filepath.empty())
			throw std::runtime_error("Texture filepath must be provided");
		if (!std::filesystem::exists(filepath))
			throw std::runtime_error(FmtS("Texture file '%S' does not exist", filepath.c_str()));

		std::unique_ptr<uint8_t[]> dds_data;
		dds::Header* header = nullptr;
		std::span<uint8_t const> bits = {nullptr, 0};

		LoadTextureDataFromFile(filepath, dds_data, header, bits);
		CreateTextureFromDDS(d3d_device, *header, bits, mips, is_cube_map, tdesc, res, srv, max_dimension);
	}
}
