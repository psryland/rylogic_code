//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
// This file contains a set of helper wrappers for initialising some d3d11 structures
#pragma once
#ifndef PR_RDR_UTIL_WRAPPERS_H
#define PR_RDR_UTIL_WRAPPERS_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/util.h"

namespace pr
{
	namespace rdr
	{
		// Standard buffer description
		struct BufferDesc :D3D11_BUFFER_DESC
		{
			void const* Data;      // ByteWidth is the size of the data
			size_t      ElemCount; // The number of elements in this buffer (verts, indices, whatever)
			size_t      SizeInBytes() const { return ElemCount * StructureByteStride; }

			BufferDesc()
			:D3D11_BUFFER_DESC()
			,Data(0)
			,ElemCount(0)
			{}
			BufferDesc(D3DPtr<ID3D11Buffer> const& buf)
			:D3D11_BUFFER_DESC()
			,Data(0) // Would need to 'Map' to get this
			,ElemCount(0)
			{
				buf->GetDesc(this);
			}
			template <typename Elem> BufferDesc(size_t count, Elem const* data, D3D11_USAGE usage, D3D11_BIND_FLAG bind_flags, D3D11_CPU_ACCESS_FLAG cpu_access, D3D11_RESOURCE_MISC_FLAG res_flag)
			:D3D11_BUFFER_DESC()
			,Data(data)
			,ElemCount(count)
			{
				ByteWidth           = UINT(sizeof(Elem) * count); // Size of the buffer in bytes
				Usage               = usage;                      // How the buffer will be used
				BindFlags           = bind_flags;                 // How the buffer will be bound (i.e. can it be a render target too?)
				CPUAccessFlags      = cpu_access;                 // What access the CPU needs. (if data provided, assume none)
				MiscFlags           = res_flag;                   // General flags for the resource
				StructureByteStride = sizeof(Elem);               // For structured buffers
			}
			template <typename Elem, size_t Sz> BufferDesc(Elem const (&data)[Sz], D3D11_USAGE usage, D3D11_BIND_FLAG bind_flags, D3D11_CPU_ACCESS_FLAG cpu_access, D3D11_RESOURCE_MISC_FLAG res_flag)
			:D3D11_BUFFER_DESC()
			,Data(data)
			,ElemCount(Sz)
			{
				ByteWidth           = UINT(sizeof(Elem) * Sz); // Size of the buffer in bytes
				Usage               = usage;                   // How the buffer will be used
				BindFlags           = bind_flag;               // How the buffer will be bound (i.e. can it be a render target too?)
				CPUAccessFlags      = 0;                       // What access the CPU needs. (if data provided, assume none)
				MiscFlags           = res_flag;                // General flags for the resource
				StructureByteStride = sizeof(Elem);            // For structured buffers
			}
		};

		// Vertex buffer flavour of a buffer description
		struct VBufferDesc :BufferDesc
		{
			VBufferDesc() :BufferDesc() {}
			template <typename Elem> VBufferDesc(size_t count, Elem const* data, D3D11_USAGE usage = D3D11_USAGE_DEFAULT, D3D11_BIND_FLAG bind_flags = D3D11_BIND_VERTEX_BUFFER, D3D11_CPU_ACCESS_FLAG cpu_access = D3D11_CPU_ACCESS_FLAG(0), D3D11_RESOURCE_MISC_FLAG res_flag = D3D11_RESOURCE_MISC_FLAG(0))
			:BufferDesc(count, data, usage, bind_flags, cpu_access, res_flag)
			{}
			template <typename Elem, size_t Sz> VBufferDesc(Elem const (&data)[Sz], D3D11_USAGE usage = D3D11_USAGE_DEFAULT, D3D11_BIND_FLAG bind_flags = D3D11_BIND_VERTEX_BUFFER, D3D11_CPU_ACCESS_FLAG cpu_access = D3D11_CPU_ACCESS_FLAG(0), D3D11_RESOURCE_MISC_FLAG res_flag = D3D11_RESOURCE_MISC_FLAG(0))
			:BufferDesc(Sz, data, usage, bind_flags, cpu_access, res_flag)
			{}
		};

		// Index buffer flavour of a buffer description
		struct IBufferDesc :BufferDesc
		{
			DXGI_FORMAT Format;    // The buffer format
			
			IBufferDesc() :BufferDesc() {}
			template <typename Elem> IBufferDesc(size_t count, Elem const* data, DXGI_FORMAT format = pr::rdr::DxFormat<Elem>::value, D3D11_USAGE usage = D3D11_USAGE_DEFAULT, D3D11_BIND_FLAG bind_flags = D3D11_BIND_INDEX_BUFFER, D3D11_CPU_ACCESS_FLAG cpu_access = D3D11_CPU_ACCESS_FLAG(0), D3D11_RESOURCE_MISC_FLAG res_flag = D3D11_RESOURCE_MISC_FLAG(0))
			:BufferDesc(count, data, usage, bind_flags, cpu_access, res_flag)
			,Format(format)
			{}
			template <typename Elem, size_t Sz> IBufferDesc(Elem const (&data)[Sz], DXGI_FORMAT format = pr::rdr::DxFormat<Elem>::value, D3D11_USAGE usage = D3D11_USAGE_DEFAULT, D3D11_BIND_FLAG bind_flags = D3D11_BIND_INDEX_BUFFER, D3D11_CPU_ACCESS_FLAG cpu_access = D3D11_CPU_ACCESS_FLAG(0), D3D11_RESOURCE_MISC_FLAG res_flag = D3D11_RESOURCE_MISC_FLAG(0))
			:BufferDesc(Sz, data, usage, bind_flags, cpu_access, res_flag)
			,Format(format)
			{}
		};

		// Constants buffer flavour of a buffer description
		struct CBufferDesc :BufferDesc
		{
			CBufferDesc() :BufferDesc() {}
			CBufferDesc(size_t size, D3D11_USAGE usage = D3D11_USAGE_DYNAMIC, D3D11_BIND_FLAG bind_flags = D3D11_BIND_CONSTANT_BUFFER, D3D11_CPU_ACCESS_FLAG cpu_access = D3D11_CPU_ACCESS_WRITE, D3D11_RESOURCE_MISC_FLAG res_flag = D3D11_RESOURCE_MISC_FLAG(0))
			:BufferDesc(size, (byte*)0, usage, bind_flags, cpu_access, res_flag)
			{}
		};

		// Multi sampling description
		struct MultiSamp :DXGI_SAMPLE_DESC
		{
			MultiSamp()
			:DXGI_SAMPLE_DESC()
			{
				Count   = 1;
				Quality = 0;
			}
			MultiSamp(UINT count, UINT quality)
			:DXGI_SAMPLE_DESC()
			{
				Count   = count;
				Quality = quality;
			}
		};

		// Texture buffer description
		struct TextureDesc :D3D11_TEXTURE2D_DESC
		{
			TextureDesc()
			:D3D11_TEXTURE2D_DESC()
			{
				InitDefaults();
			}
			TextureDesc(UINT pitch, UINT height, UINT mips = 0, DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM)
			:D3D11_TEXTURE2D_DESC(TextureDesc())
			{
				InitDefaults();
				Width          = pitch;
				Height         = height;
				MipLevels      = mips;
				Format         = format;
			}
			void InitDefaults()
			{
				Width          = 0;
				Height         = 0;
				MipLevels      = 1;
				ArraySize      = 1;
				Format         = DXGI_FORMAT_R8G8B8A8_UNORM;
				SampleDesc     = MultiSamp();
				Usage          = D3D11_USAGE_DEFAULT;
				BindFlags      = D3D11_BIND_UNORDERED_ACCESS;
				CPUAccessFlags = 0;
				MiscFlags      = 0;
			}
		};

		// Texture sampler description
		struct SamplerDesc :D3D11_SAMPLER_DESC
		{
			static SamplerDesc ClampSampler() { return SamplerDesc(); }
			static SamplerDesc WrapSampler()  { return SamplerDesc(D3D11_TEXTURE_ADDRESS_WRAP, D3D11_TEXTURE_ADDRESS_WRAP, D3D11_TEXTURE_ADDRESS_WRAP); }
			
			SamplerDesc(
				D3D11_TEXTURE_ADDRESS_MODE addrU = D3D11_TEXTURE_ADDRESS_CLAMP,
				D3D11_TEXTURE_ADDRESS_MODE addrV = D3D11_TEXTURE_ADDRESS_CLAMP,
				D3D11_TEXTURE_ADDRESS_MODE addrW = D3D11_TEXTURE_ADDRESS_CLAMP,
				D3D11_FILTER filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR)
				:D3D11_SAMPLER_DESC()
			{
				InitDefaults();
				Filter         = filter;
				AddressU       = addrU;
				AddressV       = addrV;
				AddressW       = addrW;
			}
			void InitDefaults()
			{
				Filter         = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
				AddressU       = D3D11_TEXTURE_ADDRESS_CLAMP;
				AddressV       = D3D11_TEXTURE_ADDRESS_CLAMP;
				AddressW       = D3D11_TEXTURE_ADDRESS_CLAMP;
				MipLODBias     = 0.0f;
				MaxAnisotropy  = 1;
				ComparisonFunc = D3D11_COMPARISON_ALWAYS;
				BorderColor[0] = 0.0f;
				BorderColor[1] = 0.0f;
				BorderColor[2] = 0.0f;
				BorderColor[3] = 0.0f;
				MinLOD         = 0.0f;
				MaxLOD         = D3D11_FLOAT32_MAX;
			}
		};

		// Initialisation data
		struct SubResourceData :D3D11_SUBRESOURCE_DATA
		{
			SubResourceData(void const* init_data, UINT pitch, UINT pitch_per_slice)
			:D3D11_SUBRESOURCE_DATA()
			{
				pSysMem          = init_data;       // Initialisation data for a resource
				SysMemPitch      = pitch;           // used for 2D texture initialisation
				SysMemSlicePitch = pitch_per_slice; // used for 3D texture initialisation
			}
			template <typename InitType> SubResourceData(InitType const& init)
			:D3D11_SUBRESOURCE_DATA()
			{
				pSysMem          = &init;
				SysMemPitch      = 0;
				SysMemSlicePitch = sizeof(InitType);
			}
		};

		// Rasterizer description (render states)
		struct RasterizerDesc :D3D11_RASTERIZER_DESC
		{
			RasterizerDesc
			(
				D3D11_FILL_MODE fill          = D3D11_FILL_SOLID,
				D3D11_CULL_MODE cull          = D3D11_CULL_BACK,
				bool depth_clip_enable        = true,
				bool front_ccw                = true,
				bool multisample_enable       = false,
				bool antialiased_line_enable  = false,
				bool scissor_enable           = false,
				int depth_bias                = 0,
				float depth_bias_clamp        = 0.0f,
				float slope_scaled_depth_bias = 0.0f
			)
			:D3D11_RASTERIZER_DESC()
			{
				FillMode              = fill;
				CullMode              = cull;
				FrontCounterClockwise = front_ccw;
				DepthBias             = depth_bias;
				MultisampleEnable     = multisample_enable;
				SlopeScaledDepthBias  = slope_scaled_depth_bias;
				DepthClipEnable       = depth_clip_enable;
				ScissorEnable         = scissor_enable;
				DepthBiasClamp        = depth_bias_clamp;
				AntialiasedLineEnable = antialiased_line_enable;
			}
		};

		// Shader resource view description
		struct ShaderResViewDesc :D3D11_SHADER_RESOURCE_VIEW_DESC
		{
			ShaderResViewDesc()
			:D3D11_SHADER_RESOURCE_VIEW_DESC()
			{}
			ShaderResViewDesc(DXGI_FORMAT format, D3D11_SRV_DIMENSION view_dim)
			:D3D11_SHADER_RESOURCE_VIEW_DESC()
			{
				Format = format;
				ViewDimension = view_dim;
			}
		};

		// Render target view description
		struct RenderTargetViewDesc :D3D11_RENDER_TARGET_VIEW_DESC
		{
			RenderTargetViewDesc()
			:D3D11_RENDER_TARGET_VIEW_DESC()
			{}
			explicit RenderTargetViewDesc(DXGI_FORMAT format, D3D11_RTV_DIMENSION view_dim = D3D11_RTV_DIMENSION_TEXTURE2D)
			:D3D11_RENDER_TARGET_VIEW_DESC()
			{
				Format = format;
				ViewDimension = view_dim;
			}
		};

		// Depth stencil view description
		struct DepthStencilViewDesc :D3D11_DEPTH_STENCIL_VIEW_DESC
		{
			DepthStencilViewDesc()
			:D3D11_DEPTH_STENCIL_VIEW_DESC()
			{}
			explicit DepthStencilViewDesc(DXGI_FORMAT format, D3D11_DSV_DIMENSION view_dim = D3D11_DSV_DIMENSION_TEXTURE2D)
			:D3D11_DEPTH_STENCIL_VIEW_DESC()
			{
				Format = format;
				ViewDimension = view_dim;
			}
		};
		
		// Display mode description
		struct DisplayMode :DXGI_MODE_DESC
		{
			DisplayMode(UINT width = 1024, UINT height = 768, DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM)
			:DXGI_MODE_DESC()
			{
				Width                   = width;
				Height                  = height;
				Format                  = format;
				RefreshRate.Numerator   = 60;
				RefreshRate.Denominator = 1;
				ScanlineOrdering        = DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED;
				Scaling                 = DXGI_MODE_SCALING_UNSPECIFIED;
			}
			DisplayMode(pr::iv2 const& area, DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM)
			:DXGI_MODE_DESC()
			{
				Width                   = area.x;
				Height                  = area.y;
				Format                  = format;
				RefreshRate.Numerator   = 60;
				RefreshRate.Denominator = 1;
				ScanlineOrdering        = DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED;
				Scaling                 = DXGI_MODE_SCALING_UNSPECIFIED;
			}
		};

		// Viewport description
		struct Viewport :D3D11_VIEWPORT
		{
			Viewport& set(float x, float y, float width, float height, float min_depth = 0.0f, float max_depth = 1.0f)
			{
				PR_ASSERT(PR_DBG_RDR, x >= D3D11_VIEWPORT_BOUNDS_MIN && x <= D3D11_VIEWPORT_BOUNDS_MAX, "X value out of range");
				PR_ASSERT(PR_DBG_RDR, y >= D3D11_VIEWPORT_BOUNDS_MIN && y <= D3D11_VIEWPORT_BOUNDS_MAX, "X value out of range");
				PR_ASSERT(PR_DBG_RDR, width >= 0.0f , "Width value invalid");
				PR_ASSERT(PR_DBG_RDR, height >= 0.0f, "Height value invalid");
				PR_ASSERT(PR_DBG_RDR, x + width  <= D3D11_VIEWPORT_BOUNDS_MAX, "Width value out of range");
				PR_ASSERT(PR_DBG_RDR, y + height <= D3D11_VIEWPORT_BOUNDS_MAX, "Height value out of range");
				PR_ASSERT(PR_DBG_RDR, min_depth >= 0.0f && min_depth <= 1.0f, "Min depth value out of range");
				PR_ASSERT(PR_DBG_RDR, max_depth >= 0.0f && max_depth <= 1.0f, "Max depth value out of range");
				PR_ASSERT(PR_DBG_RDR, min_depth <= max_depth, "Min and max depth values invalid");
				
				TopLeftX = x;
				TopLeftY = y;
				Width    = width;
				Height   = height;
				MinDepth = min_depth;
				MaxDepth = max_depth;
				return *this;
			}
			
			Viewport(float width, float height)
			:D3D11_VIEWPORT()
			{
				set(0.0f, 0.0f, width, height);
			}
			Viewport(UINT width, UINT height)
			:D3D11_VIEWPORT()
			{
				set(0.0f, 0.0f, static_cast<float>(width), static_cast<float>(height));
			}
			Viewport(float x, float y, float width, float height)
			:D3D11_VIEWPORT()
			{
				set(x, y, width, height);
			}
			Viewport(float x, float y, float width, float height, float min_depth, float max_depth)
			:D3D11_VIEWPORT()
			{
				set(x, y, width, height, min_depth, max_depth);
			}
			Viewport(pr::iv2 const& area)
			:D3D11_VIEWPORT()
			{
				set(0.0f, 0.0f, float(area.x), float(area.y));
			}
			Viewport(pr::IRect const& rect)
			:D3D11_VIEWPORT()
			{
				pr::FRect r = pr::FRect::make(rect);
				set(r.X(), r.Y(), r.SizeX(), r.SizeY());
			}

			size_t WidthUI() const  { return static_cast<size_t>(Width); }
			size_t HeightUI() const { return static_cast<size_t>(Height); }
		};
	}
}

#endif
