//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
// Function for loading a WIC image and creating a Direct3D 11 runtime texture for it
// (auto-generating mipmaps if possible)
//
// Note: Assumes application has already called CoInitializeEx
//
// Warning: CreateWICTexture* functions are not thread-safe if given a d3dContext instance for
//          auto-gen mipmap support.
//
// Note these functions are useful for images created as simple 2D textures. For
// more complex resources, DDSTextureLoader is an excellent light-weight runtime loader.
// For a full-featured DDS file reader, writer, and texture processing pipeline see
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

// We could load multi-frame images (TIFF/GIF) into a texture array.
// For now, we just load the first frame (note: DirectXTex supports multi-frame images)

#include "pr/view3d/forward.h"
#include "pr/view3d/textures/texture_loader.h"
#include "pr/view3d/util/wrappers.h"
#include <memory>
#include <dxgiformat.h>
#include <assert.h>
#include <wincodec.h>
#include <stdint.h>

namespace pr::rdr
{
	namespace wic
	{
		// WIC Pixel Format Translation Data
		struct Translate
		{
			WICPixelFormatGUID wic;
			DXGI_FORMAT format;
		};
		static Translate const g_formats[] =
		{
			{ GUID_WICPixelFormat128bppRGBAFloat, DXGI_FORMAT_R32G32B32A32_FLOAT },

			{ GUID_WICPixelFormat64bppRGBAHalf, DXGI_FORMAT_R16G16B16A16_FLOAT },
			{ GUID_WICPixelFormat64bppRGBA, DXGI_FORMAT_R16G16B16A16_UNORM },

			{ GUID_WICPixelFormat32bppRGBA, DXGI_FORMAT_R8G8B8A8_UNORM },
			{ GUID_WICPixelFormat32bppBGRA, DXGI_FORMAT_B8G8R8A8_UNORM }, // DXGI 1.1
			{ GUID_WICPixelFormat32bppBGR, DXGI_FORMAT_B8G8R8X8_UNORM }, // DXGI 1.1

			{ GUID_WICPixelFormat32bppRGBA1010102XR, DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM }, // DXGI 1.1
			{ GUID_WICPixelFormat32bppRGBA1010102, DXGI_FORMAT_R10G10B10A2_UNORM },
			{ GUID_WICPixelFormat32bppRGBE, DXGI_FORMAT_R9G9B9E5_SHAREDEXP },

			{ GUID_WICPixelFormat16bppBGRA5551, DXGI_FORMAT_B5G5R5A1_UNORM },
			{ GUID_WICPixelFormat16bppBGR565, DXGI_FORMAT_B5G6R5_UNORM },

			{ GUID_WICPixelFormat32bppGrayFloat, DXGI_FORMAT_R32_FLOAT },
			{ GUID_WICPixelFormat16bppGrayHalf, DXGI_FORMAT_R16_FLOAT },
			{ GUID_WICPixelFormat16bppGray, DXGI_FORMAT_R16_UNORM },
			{ GUID_WICPixelFormat8bppGray, DXGI_FORMAT_R8_UNORM },

			{ GUID_WICPixelFormat8bppAlpha, DXGI_FORMAT_A8_UNORM },

			#if _WIN32_WINNT >= _WIN32_WINNT_WIN8
			{ GUID_WICPixelFormat96bppRGBFloat, DXGI_FORMAT_R32G32B32_FLOAT },
			#endif
		};

		// WIC Pixel Format nearest conversion table
		struct Convert
		{
			GUID source;
			GUID target;
		};
		static Convert const g_convert[] =
		{
			// Note target GUID in this conversion table must be one of those directly supported formats (above).
			{ GUID_WICPixelFormatBlackWhite, GUID_WICPixelFormat8bppGray }, // DXGI_FORMAT_R8_UNORM

			{ GUID_WICPixelFormat1bppIndexed, GUID_WICPixelFormat32bppRGBA }, // DXGI_FORMAT_R8G8B8A8_UNORM 
			{ GUID_WICPixelFormat2bppIndexed, GUID_WICPixelFormat32bppRGBA }, // DXGI_FORMAT_R8G8B8A8_UNORM 
			{ GUID_WICPixelFormat4bppIndexed, GUID_WICPixelFormat32bppRGBA }, // DXGI_FORMAT_R8G8B8A8_UNORM 
			{ GUID_WICPixelFormat8bppIndexed, GUID_WICPixelFormat32bppRGBA }, // DXGI_FORMAT_R8G8B8A8_UNORM 

			{ GUID_WICPixelFormat2bppGray, GUID_WICPixelFormat8bppGray }, // DXGI_FORMAT_R8_UNORM 
			{ GUID_WICPixelFormat4bppGray, GUID_WICPixelFormat8bppGray }, // DXGI_FORMAT_R8_UNORM 

			{ GUID_WICPixelFormat16bppGrayFixedPoint, GUID_WICPixelFormat16bppGrayHalf }, // DXGI_FORMAT_R16_FLOAT 
			{ GUID_WICPixelFormat32bppGrayFixedPoint, GUID_WICPixelFormat32bppGrayFloat }, // DXGI_FORMAT_R32_FLOAT 

			{ GUID_WICPixelFormat16bppBGR555, GUID_WICPixelFormat16bppBGRA5551 }, // DXGI_FORMAT_B5G5R5A1_UNORM

			{ GUID_WICPixelFormat32bppBGR101010, GUID_WICPixelFormat32bppRGBA1010102 }, // DXGI_FORMAT_R10G10B10A2_UNORM

			{ GUID_WICPixelFormat24bppBGR, GUID_WICPixelFormat32bppRGBA }, // DXGI_FORMAT_R8G8B8A8_UNORM 
			{ GUID_WICPixelFormat24bppRGB, GUID_WICPixelFormat32bppRGBA }, // DXGI_FORMAT_R8G8B8A8_UNORM 
			{ GUID_WICPixelFormat32bppPBGRA, GUID_WICPixelFormat32bppRGBA }, // DXGI_FORMAT_R8G8B8A8_UNORM 
			{ GUID_WICPixelFormat32bppPRGBA, GUID_WICPixelFormat32bppRGBA }, // DXGI_FORMAT_R8G8B8A8_UNORM 

			{ GUID_WICPixelFormat48bppRGB, GUID_WICPixelFormat64bppRGBA }, // DXGI_FORMAT_R16G16B16A16_UNORM
			{ GUID_WICPixelFormat48bppBGR, GUID_WICPixelFormat64bppRGBA }, // DXGI_FORMAT_R16G16B16A16_UNORM
			{ GUID_WICPixelFormat64bppBGRA, GUID_WICPixelFormat64bppRGBA }, // DXGI_FORMAT_R16G16B16A16_UNORM
			{ GUID_WICPixelFormat64bppPRGBA, GUID_WICPixelFormat64bppRGBA }, // DXGI_FORMAT_R16G16B16A16_UNORM
			{ GUID_WICPixelFormat64bppPBGRA, GUID_WICPixelFormat64bppRGBA }, // DXGI_FORMAT_R16G16B16A16_UNORM

			{ GUID_WICPixelFormat48bppRGBFixedPoint, GUID_WICPixelFormat64bppRGBAHalf }, // DXGI_FORMAT_R16G16B16A16_FLOAT 
			{ GUID_WICPixelFormat48bppBGRFixedPoint, GUID_WICPixelFormat64bppRGBAHalf }, // DXGI_FORMAT_R16G16B16A16_FLOAT 
			{ GUID_WICPixelFormat64bppRGBAFixedPoint, GUID_WICPixelFormat64bppRGBAHalf }, // DXGI_FORMAT_R16G16B16A16_FLOAT 
			{ GUID_WICPixelFormat64bppBGRAFixedPoint, GUID_WICPixelFormat64bppRGBAHalf }, // DXGI_FORMAT_R16G16B16A16_FLOAT 
			{ GUID_WICPixelFormat64bppRGBFixedPoint, GUID_WICPixelFormat64bppRGBAHalf }, // DXGI_FORMAT_R16G16B16A16_FLOAT 
			{ GUID_WICPixelFormat64bppRGBHalf, GUID_WICPixelFormat64bppRGBAHalf }, // DXGI_FORMAT_R16G16B16A16_FLOAT 
			{ GUID_WICPixelFormat48bppRGBHalf, GUID_WICPixelFormat64bppRGBAHalf }, // DXGI_FORMAT_R16G16B16A16_FLOAT 

			{ GUID_WICPixelFormat96bppRGBFixedPoint, GUID_WICPixelFormat128bppRGBAFloat }, // DXGI_FORMAT_R32G32B32A32_FLOAT 
			{ GUID_WICPixelFormat128bppPRGBAFloat, GUID_WICPixelFormat128bppRGBAFloat }, // DXGI_FORMAT_R32G32B32A32_FLOAT 
			{ GUID_WICPixelFormat128bppRGBFloat, GUID_WICPixelFormat128bppRGBAFloat }, // DXGI_FORMAT_R32G32B32A32_FLOAT 
			{ GUID_WICPixelFormat128bppRGBAFixedPoint, GUID_WICPixelFormat128bppRGBAFloat }, // DXGI_FORMAT_R32G32B32A32_FLOAT 
			{ GUID_WICPixelFormat128bppRGBFixedPoint, GUID_WICPixelFormat128bppRGBAFloat }, // DXGI_FORMAT_R32G32B32A32_FLOAT 

			{ GUID_WICPixelFormat32bppCMYK, GUID_WICPixelFormat32bppRGBA }, // DXGI_FORMAT_R8G8B8A8_UNORM 
			{ GUID_WICPixelFormat64bppCMYK, GUID_WICPixelFormat64bppRGBA }, // DXGI_FORMAT_R16G16B16A16_UNORM
			{ GUID_WICPixelFormat40bppCMYKAlpha, GUID_WICPixelFormat64bppRGBA }, // DXGI_FORMAT_R16G16B16A16_UNORM
			{ GUID_WICPixelFormat80bppCMYKAlpha, GUID_WICPixelFormat64bppRGBA }, // DXGI_FORMAT_R16G16B16A16_UNORM

			#if _WIN32_WINNT >= _WIN32_WINNT_WIN8
			{ GUID_WICPixelFormat32bppRGB, GUID_WICPixelFormat32bppRGBA }, // DXGI_FORMAT_R8G8B8A8_UNORM
			{ GUID_WICPixelFormat64bppRGB, GUID_WICPixelFormat64bppRGBA }, // DXGI_FORMAT_R16G16B16A16_UNORM
			{ GUID_WICPixelFormat64bppPRGBAHalf, GUID_WICPixelFormat64bppRGBAHalf }, // DXGI_FORMAT_R16G16B16A16_FLOAT 
			#endif
			// We don't support n-channel formats
		};
	}

	// Get the WIC imaging factory instance
	IWICImagingFactory* GetWIC()
	{
		static IWICImagingFactory* s_factory = []
		{
			IWICImagingFactory* factory;
			pr::Check(CoCreateInstance(CLSID_WICImagingFactory, nullptr, CLSCTX_INPROC_SERVER, __uuidof(IWICImagingFactory), (void**)&factory));
			return factory;
		}();
		return s_factory;
	}

	// Convert a WIC format GUID to a DXGI format id.
	DXGI_FORMAT WICToDXGI(WICPixelFormatGUID const& pf, bool include_convertible, WICPixelFormatGUID* convert_guid)
	{
		// Find a direct match for pixel format
		for (size_t i = 0; i < _countof(wic::g_formats); ++i)
		{
			if (memcmp(&wic::g_formats[i].wic, &pf, sizeof(WICPixelFormatGUID)) != 0) continue;
			if (convert_guid != nullptr) *convert_guid = pf;
			return wic::g_formats[i].format;
		}

		// Fall back to formats that are convertible to 'pf'
		if (include_convertible)
		{
			for (size_t i = 0; i < _countof(wic::g_convert); ++i)
			{
				if (memcmp(&wic::g_convert[i].source, &pf, sizeof(WICPixelFormatGUID)) != 0) continue;
				if (convert_guid != nullptr) *convert_guid = wic::g_convert[i].target;
				return WICToDXGI(wic::g_convert[i].target, false, nullptr);
			}
		}

		return DXGI_FORMAT_UNKNOWN;
	}

	// Return the number of bits per pixel for the given WIC pixel format
	size_t WICBitsPerPixel(WICPixelFormatGUID const& guid)
	{
		auto wic = GetWIC();

		RefPtr<IWICComponentInfo> cinfo;
		Check(wic->CreateComponentInfo(guid, &cinfo.m_ptr));

		WICComponentType type;
		Check(cinfo->GetComponentType(&type));
		if (type != WICPixelFormat)
			return 0;

		RefPtr<IWICPixelFormatInfo> pfinfo;
		Check(cinfo->QueryInterface(__uuidof(IWICPixelFormatInfo), reinterpret_cast<void**>(&pfinfo)));

		UINT bpp;
		Check(pfinfo->GetBitsPerPixel(&bpp));
		return bpp;
	}

	// Create a DX Texture using WIC
	void CreateTextureFromWIC(ID3D11Device* d3d_device, vector<D3DPtr<IWICBitmapFrameDecode>> const& frames, UINT mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension)
	{
		if (d3d_device == nullptr)
			throw std::runtime_error("D3D device pointer is null");
		if (frames.empty())
			throw std::runtime_error("No image frames provided");
		
		// Get the supported feature level
		auto feature_level = d3d_device->GetFeatureLevel();

		// Determine the maximum texture dimension
		// This is a bit conservative because the hardware could support larger textures than
		// the Feature Level defined minimums, but doing it this way is much easier and more
		// performant for WIC than the 'fail and retry' model used by DDSTextureLoader
		max_dimension =
			max_dimension != 0 ? max_dimension :
			feature_level == D3D_FEATURE_LEVEL_9_1 ? D3D_FL9_1_REQ_TEXTURE2D_U_OR_V_DIMENSION :
			feature_level == D3D_FEATURE_LEVEL_9_2 ? D3D_FL9_1_REQ_TEXTURE2D_U_OR_V_DIMENSION :
			feature_level == D3D_FEATURE_LEVEL_9_3 ? D3D_FL9_3_REQ_TEXTURE2D_U_OR_V_DIMENSION :
			feature_level == D3D_FEATURE_LEVEL_10_0 ? D3D10_REQ_TEXTURE2D_U_OR_V_DIMENSION :
			feature_level == D3D_FEATURE_LEVEL_10_1 ? D3D10_REQ_TEXTURE2D_U_OR_V_DIMENSION :
			D3D11_REQ_TEXTURE2D_U_OR_V_DIMENSION;

		// Assume the image properties are the same for all images in the array
		auto& first = frames[0];

		// Read the image dimensions
		UINT width, height;
		Check(first->GetSize(&width, &height));
		assert(width > 0 && height > 0);

		// Clamp the texture dimensions to the maximum, maintaining aspect ratio
		size_t twidth = width, theight = height;
		if (width > max_dimension || height > max_dimension)
		{
			auto ar = static_cast<double>(height) / static_cast<double>(width);
			if (width > height)
			{
				twidth = max_dimension;
				theight = static_cast<size_t>(max_dimension * ar);
			}
			else
			{
				theight = max_dimension;
				twidth = static_cast<size_t>(max_dimension / ar);
			}
			assert(twidth <= max_dimension && theight <= max_dimension);
		}

		// Determine the pixel format
		WICPixelFormatGUID src_format, dst_format;
		Check(first->GetPixelFormat(&src_format));
		auto format = WICToDXGI(src_format, true, &dst_format);
		if (format == DXGI_FORMAT_UNKNOWN)
			throw std::runtime_error("Pixel format is not supported");

		// Determine the bits per pixel
		auto bpp = WICBitsPerPixel(dst_format);
		if (bpp == 0)
			throw std::runtime_error("Could not determine bits per pixel from the pixel format");

		// Verify our target format is supported by the current device
		// (handles WDDM 1.0 or WDDM 1.1 device driver cases as well as DirectX 11.0 Runtime without 16bpp format support)
		if (UINT support = 0; Failed(d3d_device->CheckFormatSupport(format, &support)) || !AllSet(support, D3D11_FORMAT_SUPPORT_TEXTURE2D))
		{
			// Fallback to RGBA 32-bit format which is supported by all devices
			dst_format = GUID_WICPixelFormat32bppRGBA;
			format = DXGI_FORMAT_R8G8B8A8_UNORM;
			bpp = 32;
		}

		// See if format is supported for auto-gen mipmaps (varies by feature level)
		// Must have context and shader-view to auto generate mipmaps
		auto mip_autogen = mips != 1;
		if (mip_autogen)
		{
			UINT fmt_support = 0;
			mip_autogen &= Succeeded(d3d_device->CheckFormatSupport(format, &fmt_support)) && (fmt_support & D3D11_FORMAT_SUPPORT_MIP_AUTOGEN) != 0;
		}

		auto pitch = (twidth * bpp + 7) / 8;
		auto image_size = pitch * theight;
		auto conversion_needed = src_format != dst_format;
		auto resize_needed = twidth != width || theight != height;
		mips = mips == 0 ? int(MipCount(twidth, theight)) : mips;
		
		vector<SubResourceData> images;
		vector<std::unique_ptr<uint8_t[]>> image_data;

		// Load image data.
		for (auto& frame : frames)
		{
			auto buf = std::unique_ptr<uint8_t[]>(new uint8_t[image_size]);
			if (!conversion_needed && !resize_needed)
			{
				// No format conversion or resize needed
				Check(frame->CopyPixels(0, static_cast<UINT>(pitch), static_cast<UINT>(image_size), buf.get()));
			}
			else
			{
				RefPtr<IWICBitmapScaler> scaler;
				RefPtr<IWICFormatConverter> converter;
				auto wic = GetWIC();

				// Create a resizer if needed
				if (resize_needed)
				{
					WICPixelFormatGUID pf;
					Check(wic->CreateBitmapScaler(&scaler.m_ptr));
					Check(scaler->Initialize(frame.get(), s_cast<UINT>(twidth), s_cast<UINT>(theight), WICBitmapInterpolationModeFant));
					Check(scaler->GetPixelFormat(&pf));
					conversion_needed = pf != dst_format;
				}

				// Create a format converter if needed
				if (conversion_needed)
				{
					Check(wic->CreateFormatConverter(&converter.m_ptr));
					Check(converter->Initialize(frame.get(), dst_format, WICBitmapDitherTypeErrorDiffusion, 0, 0, WICBitmapPaletteTypeCustom));
				}

				// Copy the data with optional reformat and resize
				Check(converter->CopyPixels(0, s_cast<UINT>(pitch), s_cast<UINT>(image_size), buf.get()));
			}
			images.push_back(SubResourceData(buf.get(), s_cast<UINT>(pitch), s_cast<UINT>(image_size)));
			image_data.emplace_back(std::move(buf));
		}

		// Create the texture description.
		tdesc.dim = D3D11_RESOURCE_DIMENSION_TEXTURE2D;
		tdesc.Tex2D.Format = format;
		tdesc.Tex2D.Width = s_cast<UINT>(twidth);
		tdesc.Tex2D.Height = s_cast<UINT>(theight);
		tdesc.Tex2D.MipLevels = s_cast<UINT>(mips);
		tdesc.Tex2D.ArraySize = s_cast<UINT>(images.size());
		tdesc.Tex2D.SampleDesc.Count = 1;
		tdesc.Tex2D.SampleDesc.Quality = 0;
		tdesc.Tex2D.Usage = D3D11_USAGE_DEFAULT;
		tdesc.Tex2D.BindFlags = D3D11_BIND_SHADER_RESOURCE | (mip_autogen ? D3D11_BIND_RENDER_TARGET : 0);
		tdesc.Tex2D.CPUAccessFlags = 0;
		tdesc.Tex2D.MiscFlags = (is_cube_map ? D3D11_RESOURCE_MISC_TEXTURECUBE : 0) | (mip_autogen ? D3D11_RESOURCE_MISC_GENERATE_MIPS : 0);

		// Create texture
		ID3D11Texture2D* tex;
		Check(d3d_device->CreateTexture2D(&tdesc.Tex2D, mip_autogen ? nullptr : images.data(), &tex));
		res = D3DPtr<ID3D11Resource>(tex, false);
		
		// Create the SRV
		ID3D11ShaderResourceView* srv2;
		ShaderResourceViewDesc srv_desc(format);
		if (is_cube_map && images.size() > 6)
		{
			srv_desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURECUBEARRAY;
			srv_desc.TextureCubeArray.NumCubes= UINT(images.size() / 6);
			srv_desc.TextureCubeArray.MipLevels = tdesc.Tex2D.MipLevels;
		}
		else if (is_cube_map)
		{
			srv_desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURECUBE;
			srv_desc.TextureCube.MipLevels = tdesc.Tex2D.MipLevels;
		}
		else if (images.size() > 1)
		{
			srv_desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2DARRAY;
			srv_desc.Texture2DArray.ArraySize = UINT(images.size());
			srv_desc.Texture2DArray.MipLevels = tdesc.Tex2D.MipLevels;
		}
		else
		{
			srv_desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
			srv_desc.Texture2D.MipLevels = tdesc.Tex2D.MipLevels;
		}
		Check(d3d_device->CreateShaderResourceView(res.get(), &srv_desc, &srv2));
		srv = D3DPtr<ID3D11ShaderResourceView>(srv2, false);

		// Generate mips
		if (mip_autogen)
		{
			D3DPtr<ID3D11DeviceContext> dc;
			d3d_device->GetImmediateContext(&dc.m_ptr);
			for (int i = 0, iend = int(images.size()); i != iend; ++i)
				dc->UpdateSubresource(res.get(), i*mips, nullptr, images[i].pSysMem, images[i].SysMemPitch, images[i].SysMemSlicePitch);
			dc->GenerateMips(srv.get());
		}
	}

	// Create a DX texture from one or more image files in memory.
	void CreateWICTextureFromMemory(ID3D11Device* d3d_device, std::span<ImageBytes> images, int mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension)
	{
		if (!d3d_device)
			throw std::runtime_error("D3D Device is null");
		if (images.empty())
			throw std::runtime_error("Texture file data is invalid");

		auto wic = GetWIC();

		// Load each image frame
		vector<RefPtr<IWICBitmapFrameDecode>> frames;
		for (int i = 0, iend = int(images.size()); i != iend; ++i)
		{
			auto& img = images[i];

			// Create input stream for memory
			RefPtr<IWICStream> stream;
			Check(wic->CreateStream(&stream.m_ptr));
			Check(stream->InitializeFromMemory(static_cast<uint8_t*>(const_cast<void*>(img.data)), static_cast<DWORD>(img.size)));

			// Initialize WIC image decoder
			RefPtr<IWICBitmapDecoder> decoder;
			Check(wic->CreateDecoderFromStream(stream.get(), 0, WICDecodeMetadataCacheOnDemand, &decoder.m_ptr));

			// Get the first frame in the image
			RefPtr<IWICBitmapFrameDecode> frame;
			Check(decoder->GetFrame(0, &frame.m_ptr));
			frames.emplace_back(std::move(frame));
		}

		// Create the texture
		CreateTextureFromWIC(d3d_device, frames, mips, is_cube_map, tdesc, res, srv, max_dimension);
	}

	// Create a DX texture from one or more image files
	void CreateWICTextureFromFiles(ID3D11Device* d3d_device, vector<std::filesystem::path> const& filepaths, int mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension)
	{
		if (!d3d_device)
			throw std::runtime_error("D3D Device is null");

		auto wic = GetWIC();

		// Load each image
		vector<RefPtr<IWICBitmapFrameDecode>> frames;
		for (auto& path : filepaths)
		{
			// Initialize WIC image decoder
			RefPtr<IWICBitmapDecoder> decoder;
			Check(wic->CreateDecoderFromFilename(path.c_str(), 0, GENERIC_READ, WICDecodeMetadataCacheOnDemand, &decoder.m_ptr));

			// Get the first frame in the image
			RefPtr<IWICBitmapFrameDecode> frame;
			Check(decoder->GetFrame(0, &frame.m_ptr));

			frames.emplace_back(frame);
		}

		// Create the texture
		CreateTextureFromWIC(d3d_device, frames, mips, is_cube_map, tdesc, res, srv, max_dimension);
	}
}

