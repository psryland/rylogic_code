//-------------------------------------------------------------------------------------
// DirectXTexUtil.cpp
//  
// DirectX Texture Library - Utilities
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// http://go.microsoft.com/fwlink/?LinkId=248926
//-------------------------------------------------------------------------------------

#include "pr/view3d/forward.h"
#include "directxtexp.h"

//-------------------------------------------------------------------------------------
// WIC Pixel Format Translation Data
//-------------------------------------------------------------------------------------
struct WICTranslate
{
    GUID        wic;
    DXGI_FORMAT format;
};

static WICTranslate g_WICFormats[] = 
{
    { GUID_WICPixelFormat128bppRGBAFloat,       DXGI_FORMAT_R32G32B32A32_FLOAT },

    { GUID_WICPixelFormat64bppRGBAHalf,         DXGI_FORMAT_R16G16B16A16_FLOAT },
    { GUID_WICPixelFormat64bppRGBA,             DXGI_FORMAT_R16G16B16A16_UNORM },

    { GUID_WICPixelFormat32bppRGBA,             DXGI_FORMAT_R8G8B8A8_UNORM },
    { GUID_WICPixelFormat32bppBGRA,             DXGI_FORMAT_B8G8R8A8_UNORM }, // DXGI 1.1
    { GUID_WICPixelFormat32bppBGR,              DXGI_FORMAT_B8G8R8X8_UNORM }, // DXGI 1.1

    { GUID_WICPixelFormat32bppRGBA1010102XR,    DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM }, // DXGI 1.1
    { GUID_WICPixelFormat32bppRGBA1010102,      DXGI_FORMAT_R10G10B10A2_UNORM },
    { GUID_WICPixelFormat32bppRGBE,             DXGI_FORMAT_R9G9B9E5_SHAREDEXP },

    { GUID_WICPixelFormat16bppBGRA5551,         DXGI_FORMAT_B5G5R5A1_UNORM },
    { GUID_WICPixelFormat16bppBGR565,           DXGI_FORMAT_B5G6R5_UNORM },

    { GUID_WICPixelFormat32bppGrayFloat,        DXGI_FORMAT_R32_FLOAT },
    { GUID_WICPixelFormat16bppGrayHalf,         DXGI_FORMAT_R16_FLOAT },
    { GUID_WICPixelFormat16bppGray,             DXGI_FORMAT_R16_UNORM },
    { GUID_WICPixelFormat8bppGray,              DXGI_FORMAT_R8_UNORM },

    { GUID_WICPixelFormat8bppAlpha,             DXGI_FORMAT_A8_UNORM },

    { GUID_WICPixelFormatBlackWhite,            DXGI_FORMAT_R1_UNORM },

#if (_WIN32_WINNT >= 0x0602 /*_WIN32_WINNT_WIN8*/)
    { GUID_WICPixelFormat96bppRGBFloat,         DXGI_FORMAT_R32G32B32_FLOAT },
#endif
};

namespace DirectX
{

//=====================================================================================
// WIC Utilities
//=====================================================================================

DXGI_FORMAT _WICToDXGI( const GUID& guid )
{
    for( size_t i=0; i < _countof(g_WICFormats); ++i )
    {
        if ( memcmp( &g_WICFormats[i].wic, &guid, sizeof(GUID) ) == 0 )
            return g_WICFormats[i].format;
    }

    return DXGI_FORMAT_UNKNOWN;
}

bool _DXGIToWIC( DXGI_FORMAT format, GUID& guid )
{
    for( size_t i=0; i < _countof(g_WICFormats); ++i )
    {
        if ( g_WICFormats[i].format == format )
        {
            memcpy( &guid, &g_WICFormats[i].wic, sizeof(GUID) );
            return true;
        }
    }

    // Special cases
    switch( format )
    {
    case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
        memcpy( &guid, &GUID_WICPixelFormat32bppRGBA, sizeof(GUID) );
        return true;

    case DXGI_FORMAT_D32_FLOAT:
        memcpy( &guid, &GUID_WICPixelFormat32bppGrayFloat, sizeof(GUID) );
        return true;

    case DXGI_FORMAT_D16_UNORM:
        memcpy( &guid, &GUID_WICPixelFormat16bppGray, sizeof(GUID) );
        return true;    

    case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
        memcpy( &guid, &GUID_WICPixelFormat32bppBGRA, sizeof(GUID) );
        return true;

    case DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
        memcpy( &guid, &GUID_WICPixelFormat32bppBGR, sizeof(GUID) );
        return true;
    }

    memcpy( &guid, &GUID_NULL, sizeof(GUID) );
    return false;
}

size_t _WICBitsPerPixel( REFGUID targetGuid )
{
    IWICImagingFactory* pWIC = _GetWIC();
    if ( !pWIC )
        return 0;
 
    ScopedObject<IWICComponentInfo> cinfo;
    if ( FAILED( pWIC->CreateComponentInfo( targetGuid, &cinfo ) ) )
        return 0;

    WICComponentType type;
    if ( FAILED( cinfo->GetComponentType( &type ) ) )
        return 0;

    if ( type != WICPixelFormat )
        return 0;

    ScopedObject<IWICPixelFormatInfo> pfinfo;
    if ( FAILED( cinfo->QueryInterface( __uuidof(IWICPixelFormatInfo), reinterpret_cast<void**>( &pfinfo )  ) ) )
        return 0;

    UINT bpp;
    if ( FAILED( pfinfo->GetBitsPerPixel( &bpp ) ) )
        return 0;

    return bpp;
}

IWICImagingFactory* _GetWIC()
{
	//todo: fix this, it should return a CComPtr and be cleaned up properly...
    static IWICImagingFactory* s_Factory = nullptr;

    if ( s_Factory )
        return s_Factory;

    HRESULT hr = CoCreateInstance(CLSID_WICImagingFactory, nullptr, CLSCTX_INPROC_SERVER, __uuidof(IWICImagingFactory), (LPVOID*)&s_Factory);
    if ( FAILED(hr) )
    {
        s_Factory = nullptr;
        return nullptr;
    }

    return s_Factory;
}


//-------------------------------------------------------------------------------------
// Public helper function to get common WIC codec GUIDs
//-------------------------------------------------------------------------------------
REFGUID GetWICCodec( _In_ WICCodecs codec )
{
    switch( codec )
    {
    case WIC_CODEC_BMP:
        return GUID_ContainerFormatBmp;

    case WIC_CODEC_JPEG:
        return GUID_ContainerFormatJpeg;

    case WIC_CODEC_PNG:
        return GUID_ContainerFormatPng;

    case WIC_CODEC_TIFF:
        return GUID_ContainerFormatTiff;

    case WIC_CODEC_GIF:
        return GUID_ContainerFormatGif;

    case WIC_CODEC_WMP:
        return GUID_ContainerFormatWmp;

    case WIC_CODEC_ICO:
        return GUID_ContainerFormatIco;

    default:
        return GUID_NULL;
    }
}


//=====================================================================================
// DXGI Format Utilities
//=====================================================================================

//-------------------------------------------------------------------------------------
// Returns bits-per-pixel for a given DXGI format, or 0 on failure
//-------------------------------------------------------------------------------------
size_t BitsPerPixel( DXGI_FORMAT fmt ) // Todo: refactor, replace with pr::rdr::BitsPerPixel in util.cpp
{
    switch( fmt )
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

#ifdef DXGI_1_2_FORMATS
    case DXGI_FORMAT_B4G4R4A4_UNORM:
        return 16;

    // We don't support the video formats ( see IsVideo function )

#endif // DXGI_1_2_FORMATS

    default:
        return 0;
    }
}


//-------------------------------------------------------------------------------------
// Computes the image row pitch in bytes, and the slice ptich (size in bytes of the image)
// based on DXGI format, width, and height
//-------------------------------------------------------------------------------------
void ComputePitch( DXGI_FORMAT fmt, size_t width, size_t height,
                   size_t& rowPitch, size_t& slicePitch, DWORD flags )
{
    assert( IsValid(fmt) && !IsVideo(fmt) );

    if ( IsCompressed(fmt) )
    {
        size_t bpb = ( fmt == DXGI_FORMAT_BC1_TYPELESS
                     || fmt == DXGI_FORMAT_BC1_UNORM
                     || fmt == DXGI_FORMAT_BC1_UNORM_SRGB
                     || fmt == DXGI_FORMAT_BC4_TYPELESS
                     || fmt == DXGI_FORMAT_BC4_UNORM
                     || fmt == DXGI_FORMAT_BC4_SNORM) ? 8 : 16;
        size_t nbw = std::max<size_t>( 1, (width + 3) / 4 );
        size_t nbh = std::max<size_t>( 1, (height + 3) / 4 );
        rowPitch = nbw * bpb;

        slicePitch = rowPitch * nbh;
    }
    else if ( IsPacked(fmt) )
    {
        rowPitch = ( ( width + 1 ) >> 1) * 4;

        slicePitch = rowPitch * height;
    }
    else
    {
        size_t bpp;

        if ( flags & CP_FLAGS_24BPP )
            bpp = 24;
        else if ( flags & CP_FLAGS_16BPP )
            bpp = 16;
        else if ( flags & CP_FLAGS_8BPP )
            bpp = 8;
        else
            bpp = BitsPerPixel( fmt );

        if ( flags & CP_FLAGS_LEGACY_DWORD )
        {
            // Special computation for some incorrectly created DDS files based on
            // legacy DirectDraw assumptions about pitch alignment
            rowPitch = ( ( width * bpp + 31 ) / 32 ) * sizeof(uint32_t);
            slicePitch = rowPitch * height;
        }
        else
        {
            rowPitch = ( width * bpp + 7 ) / 8;
            slicePitch = rowPitch * height;
        }
    }
}


//-------------------------------------------------------------------------------------
// Converts to an SRGB equivalent type if available
//-------------------------------------------------------------------------------------
DXGI_FORMAT MakeSRGB( _In_ DXGI_FORMAT fmt )
{
    switch( fmt )
    {
    case DXGI_FORMAT_R8G8B8A8_UNORM:
        return DXGI_FORMAT_R8G8B8A8_UNORM_SRGB;

    case DXGI_FORMAT_BC1_UNORM:
        return DXGI_FORMAT_BC1_UNORM_SRGB;

    case DXGI_FORMAT_BC2_UNORM:
        return DXGI_FORMAT_BC2_UNORM_SRGB;

    case DXGI_FORMAT_BC3_UNORM:
        return DXGI_FORMAT_BC3_UNORM_SRGB;

    case DXGI_FORMAT_B8G8R8A8_UNORM:
        return DXGI_FORMAT_B8G8R8A8_UNORM_SRGB;

    case DXGI_FORMAT_B8G8R8X8_UNORM:
        return DXGI_FORMAT_B8G8R8X8_UNORM_SRGB;

    case DXGI_FORMAT_BC7_UNORM:
        return DXGI_FORMAT_BC7_UNORM_SRGB;

    default:
        return fmt;
    }
}


//=====================================================================================
// TexMetadata
//=====================================================================================

size_t TexMetadata::ComputeIndex( _In_ size_t mip, _In_ size_t item, _In_ size_t slice ) const
{
    if ( mip >= mipLevels )
        return size_t(-1);

    switch( dimension )
    {
    case TEX_DIMENSION_TEXTURE1D:
    case TEX_DIMENSION_TEXTURE2D:
        if ( slice > 0 )
            return size_t(-1);

        if ( item >= arraySize )
            return size_t(-1);

        return (item*( mipLevels ) + mip);

    case TEX_DIMENSION_TEXTURE3D:
        if ( item > 0 )
        {
            // No support for arrays of volumes
            return size_t(-1);
        }
        else
        {
            size_t index = 0;
            size_t d = depth;

            for( size_t level = 0; level < mip; ++level )
            {
                index += d;
                if ( d > 1 )
                    d >>= 1;
            }

            if ( slice >= d )
                return size_t(-1);

            index += slice;

            return index;
        }
        break;

    default:
        return size_t(-1);
    }
}


//=====================================================================================
// Blob - Bitmap image container
//=====================================================================================

void Blob::Release()
{
    if ( _buffer )
    {
        _aligned_free( _buffer );
        _buffer = nullptr;
    }

    _size = 0;
}

HRESULT Blob::Initialize( size_t size )
{
    if ( !size )
        return E_INVALIDARG;

    Release();

    _buffer = _aligned_malloc( size, 16 );
    if ( !_buffer )
    {
        Release();
        return E_OUTOFMEMORY;
    }

    _size = size;

    return S_OK;
}

}; // namespace
